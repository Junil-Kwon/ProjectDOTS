using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

#if UNITY_EDITOR
using UnityEditor;
#endif



// Connection States

public enum ServiceState : byte {
	Uninitialized,
	Initializing,
	Unsigned,
	Signing,
	Ready,
}

public enum NetworkState : byte {
	Disconnected,
	Allocating,
	SceneLoading,
	Connecting,
	ConnectionSucceeded,
	ConnectionFailed,
	ConnectedAsRelayHost,
	ConnectedAsRelayClient,
	ConnectedAsLocalHost,
	ConnectedAsLocalClient,
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/Network Manager")]
public sealed class NetworkManager : MonoSingleton<NetworkManager> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(NetworkManager))]
	class NetworkManagerEditor : EditorExtensions {
		NetworkManager I => target as NetworkManager;
		public override void OnInspectorGUI() {
			Begin("Network Manager");

			LabelField("Event", EditorStyles.boldLabel);
			PropertyField("m_OnChatReceived");
			Space();
			LabelField("Debug", EditorStyles.boldLabel);
			BeginDisabledGroup();
			TextField("Service State", $"{ServiceState}");
			TextField("Network State", $"{NetworkState}");
			EndDisabledGroup();
			Space();

			End();
		}
	}
	#endif



	// Constants

	public const float Tickrate = 60f;
	public const float Ticktime = 0.0166667f;

	const float ConnectionTimeOut = 5f;
	const float BroadcastInterval = 1f;

	const ushort DiscoverPort = 7777;



	class RelayDriverConstructor : INetworkStreamDriverConstructor {
		RelayServerData serverData;
		RelayServerData clientData;

		public RelayDriverConstructor(RelayServerData serverData, RelayServerData clientData) {
			this.serverData = serverData;
			this.clientData = clientData;
		}
		public void CreateServerDriver(World world, ref NetworkDriverStore driver, NetDebug debug) {
			DefaultDriverBuilder.RegisterServerDriver(world, ref driver, debug, ref serverData);
		}
		public void CreateClientDriver(World world, ref NetworkDriverStore driver, NetDebug debug) {
			DefaultDriverBuilder.RegisterClientDriver(world, ref driver, debug, ref clientData);
		}
	}



	// Fields

	ServiceState m_ServiceState;
	NetworkState m_NetworkState;

	ushort m_Port;
	int m_MaxPlayers;

	CancellationTokenSource m_BroadcastCancellation;
	CancellationTokenSource m_DiscoverCancellation;

	Queue<string> m_ChatQueue = new();
	[SerializeField] UnityEvent<string> m_OnChatReceived = new();



	// Properties

	public static ServiceState ServiceState {
		get => Instance.m_ServiceState;
		private set => Instance.m_ServiceState = value;
	}
	public static NetworkState NetworkState {
		get => Instance.m_NetworkState;
		private set => Instance.m_NetworkState = value;
	}

	public static bool IsHost => NetworkState switch {
		NetworkState.ConnectedAsRelayHost => true,
		NetworkState.ConnectedAsLocalHost => true,
		_ => false,
	};
	public static bool IsClient => NetworkState switch {
		NetworkState.ConnectedAsRelayClient => true,
		NetworkState.ConnectedAsLocalClient => true,
		_ => false,
	};
	public static bool IsRelay => NetworkState switch {
		NetworkState.ConnectedAsRelayHost   => true,
		NetworkState.ConnectedAsRelayClient => true,
		_ => false,
	};
	public static bool IsLocal => NetworkState switch {
		NetworkState.ConnectedAsLocalHost   => true,
		NetworkState.ConnectedAsLocalClient => true,
		_ => false,
	};

	public static ushort Port {
		get => Instance.m_Port;
		private set => Instance.m_Port = value;
	}
	public static int MaxPlayers {
		get => Instance.m_MaxPlayers;
		private set => Instance.m_MaxPlayers = value;
	}



	static CancellationTokenSource BroadcastCancellation {
		get => Instance.m_BroadcastCancellation;
		set => Instance.m_BroadcastCancellation = value;
	}
	static CancellationTokenSource DiscoverCancellation {
		get => Instance.m_DiscoverCancellation;
		set => Instance.m_DiscoverCancellation = value;
	}
	static bool IsBroadcasting => BroadcastCancellation != null;
	static bool IsDiscovering => DiscoverCancellation != null;

	public static Queue<string> ChatQueue => Instance.m_ChatQueue;
	public static UnityEvent<string> OnChatReceived => Instance.m_OnChatReceived;



	// Authentication Methods

	public static void Initialize(Action<bool> onInitialized = null) {
		InitializeAsync().ContinueWith(_ => {
			onInitialized?.Invoke(ServiceState == ServiceState.Unsigned);
		}, TaskScheduler.FromCurrentSynchronizationContext());
	}
	static async Task InitializeAsync() {
		if (ServiceState == ServiceState.Uninitialized) {
			ServiceState = ServiceState.Initializing;
			var task = UnityServices.InitializeAsync();
			await task;
			ServiceState = task.IsCompletedSuccessfully switch {
				true  => ServiceState.Unsigned,
				false => ServiceState.Uninitialized,
			};
		}
	}

	public static void SignInAnonymously(Action<bool> onSignedIn = null) {
		SignInAnonymouslyAsync().ContinueWith(_ => {
			onSignedIn?.Invoke(ServiceState == ServiceState.Ready);
		}, TaskScheduler.FromCurrentSynchronizationContext());
	}
	static async Task SignInAnonymouslyAsync() {
		if (ServiceState == ServiceState.Unsigned) {
			ServiceState = ServiceState.Signing;
			var task = AuthenticationService.Instance.SignInAnonymouslyAsync();
			await task;
			ServiceState = task.IsCompletedSuccessfully switch {
				true  => ServiceState.Ready,
				false => ServiceState.Unsigned,
			};
		}
	}



	// Relay Connection Methods

	public static void CreateRelayHost(int maxPlayers, Action<bool> onConnected = null) {
		CreateRelayHostAsync(maxPlayers).ContinueWith(_ => {
			onConnected?.Invoke(NetworkState == NetworkState.ConnectedAsRelayHost);
		}, TaskScheduler.FromCurrentSynchronizationContext());
	}
	static async Task CreateRelayHostAsync(int maxPlayers) {
		if (ServiceState == ServiceState.Uninitialized) await InitializeAsync();
		if (ServiceState == ServiceState.Unsigned) await SignInAnonymouslyAsync();
		if (NetworkState != NetworkState.Disconnected) Disconnect();
		if (ServiceState == ServiceState.Ready && NetworkState == NetworkState.Disconnected) {

			NetworkState = NetworkState.Allocating;
			MaxPlayers = Mathf.Max(1, maxPlayers);
			RelayServerData relayServerData;
			RelayServerData relayClientData;
			try {
				var data     = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);
				var joinCode = await RelayService.Instance.GetJoinCodeAsync(data.AllocationId);
				var joinData = await RelayService.Instance.JoinAllocationAsync(joinCode);
				GUIUtility.systemCopyBuffer = joinCode;
				relayServerData = GetRelayServerData(data);
				relayClientData = GetRelayClientData(joinData);
			} catch {
				NetworkState = NetworkState.ConnectionFailed;
				return;
			}
			NetworkState = NetworkState.SceneLoading;
			var prevConstructor = NetworkStreamReceiveSystem.DriverConstructor;
			var nextConstructor = new RelayDriverConstructor(relayServerData, relayClientData);
			NetworkStreamReceiveSystem.DriverConstructor = nextConstructor;
			var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
			var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
			NetworkStreamReceiveSystem.DriverConstructor = prevConstructor;
			DestroyLocalSimulationWorld();
			await SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);

			NetworkState = NetworkState.Connecting;
			Listen (server, NetworkEndpoint.AnyIpv4);
			Connect(client, relayClientData.Endpoint);
			float timeStartConnect = Time.realtimeSinceStartup;
			while (NetworkState == NetworkState.Connecting) {
				await Task.Yield();
				if (ConnectionTimeOut < Time.realtimeSinceStartup - timeStartConnect) {
					NetworkState = NetworkState.ConnectionFailed;
					return;
				};
			}
			NetworkState = (NetworkState == NetworkState.ConnectionSucceeded) switch {
				true  => NetworkState.ConnectedAsRelayHost,
				false => NetworkState.ConnectionFailed,
			};
		}
	}

	public static void JoinRelayServer(string joinCode, Action<bool> onConnected = null) {
		JoinRelayServerAsync(joinCode).ContinueWith(_ => {
			onConnected?.Invoke(NetworkState == NetworkState.ConnectedAsRelayClient);
		}, TaskScheduler.FromCurrentSynchronizationContext());
	}
	static async Task JoinRelayServerAsync(string joinCode) {
		if (ServiceState == ServiceState.Uninitialized) await InitializeAsync();
		if (ServiceState == ServiceState.Unsigned) await SignInAnonymouslyAsync();
		if (NetworkState != NetworkState.Disconnected) Disconnect();
		if (ServiceState == ServiceState.Ready && NetworkState == NetworkState.Disconnected) {

			NetworkState = NetworkState.Allocating;
			RelayServerData relayClientData;
			try {
				var joinData = await RelayService.Instance.JoinAllocationAsync(joinCode);
				relayClientData = GetRelayClientData(joinData);
			} catch {
				NetworkState = NetworkState.ConnectionFailed;
				return;
			}
			NetworkState = NetworkState.SceneLoading;
			var prevConstructor = NetworkStreamReceiveSystem.DriverConstructor;
			var nextConstructor = new RelayDriverConstructor(default, relayClientData);
			NetworkStreamReceiveSystem.DriverConstructor = nextConstructor;
			var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
			NetworkStreamReceiveSystem.DriverConstructor = prevConstructor;
			DestroyLocalSimulationWorld();
			await SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);

			NetworkState = NetworkState.Connecting;
			Connect(client, relayClientData.Endpoint);
			float timeStartConnect = Time.realtimeSinceStartup;
			while (NetworkState == NetworkState.Connecting) {
				await Task.Yield();
				if (ConnectionTimeOut < Time.realtimeSinceStartup - timeStartConnect) {
					NetworkState = NetworkState.ConnectionFailed;
					return;
				};
			}
			NetworkState = (NetworkState == NetworkState.ConnectionSucceeded) switch {
				true  => NetworkState.ConnectedAsRelayClient,
				false => NetworkState.ConnectionFailed,
			};
		}
	}



	// Local Connection Methods

	public static void CreateLocalHost(ushort port, int maxPlayers, Action<bool> onConnected = null) {
		CreateLocalHostAsync(port, maxPlayers).ContinueWith(_ => {
			onConnected?.Invoke(NetworkState == NetworkState.ConnectedAsRelayHost);
		}, TaskScheduler.FromCurrentSynchronizationContext());
	}
	static async Task CreateLocalHostAsync(ushort port, int maxPlayers) {
		if (NetworkState != NetworkState.Disconnected) Disconnect();
		if (NetworkState == NetworkState.Disconnected) {

			NetworkState = NetworkState.SceneLoading;
			Port = port;
			MaxPlayers = Mathf.Max(1, maxPlayers);
			var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
			var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
			DestroyLocalSimulationWorld();
			await SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);

			NetworkState = NetworkState.Connecting;
			Listen (server, NetworkEndpoint.AnyIpv4.WithPort(Port));
			Connect(client, NetworkEndpoint.LoopbackIpv4.WithPort(Port));
			float timeStartConnect = Time.realtimeSinceStartup;
			while (NetworkState == NetworkState.Connecting) {
				await Task.Yield();
				if (ConnectionTimeOut < Time.realtimeSinceStartup - timeStartConnect) {
					NetworkState = NetworkState.ConnectionFailed;
					return;
				};
			}
			NetworkState = (NetworkState == NetworkState.ConnectionSucceeded) switch {
				true  => NetworkState.ConnectedAsLocalHost,
				false => NetworkState.ConnectionFailed,
			};
		}
	}

	public static void JoinLocalServer(string address, ushort port, Action<bool> onConnected = null) {
		JoinLocalServerAsync(address, port).ContinueWith(_ => {
			onConnected?.Invoke(NetworkState == NetworkState.ConnectedAsRelayClient);
		}, TaskScheduler.FromCurrentSynchronizationContext());
	}
	static async Task JoinLocalServerAsync(string address, ushort port) {
		if (NetworkState != NetworkState.Disconnected) Disconnect();
		if (NetworkState == NetworkState.Disconnected) {

			NetworkState = NetworkState.SceneLoading;
			Port = port;
			var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
			DestroyLocalSimulationWorld();
			await SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);

			NetworkState = NetworkState.Connecting;
			Connect(client, NetworkEndpoint.Parse(address, Port));
			float timeStartConnect = Time.realtimeSinceStartup;
			while (NetworkState == NetworkState.Connecting) {
				await Task.Yield();
				if (ConnectionTimeOut < Time.realtimeSinceStartup - timeStartConnect) {
					NetworkState = NetworkState.ConnectionFailed;
					return;
				};
			}
			NetworkState = (NetworkState == NetworkState.ConnectionSucceeded) switch {
				true  => NetworkState.ConnectedAsLocalClient,
				false => NetworkState.ConnectionFailed,
			};
		}
	}



	// Connection Methods

	public static void Connect() {
		CreateLocalHost(7979, 1);
	}

	public static void Disconnect() {
		bool match = true;
		match &= NetworkState != NetworkState.Allocating;
		match &= NetworkState != NetworkState.SceneLoading;
		match &= NetworkState != NetworkState.Connecting;
		if (match) {
			NetworkState = NetworkState.Disconnected;
			ClientServerBootstrap.CreateLocalWorld("DefaultWorld");
			DestroyServerClientSimulationWorld();
		}
	}



	// Discovery Methods

	public static void StartBroadcast() {
		StartBroadcastAsync().ContinueWith(_ => {
		}, TaskScheduler.FromCurrentSynchronizationContext());
	}
	static async Task StartBroadcastAsync() {
		if (!IsBroadcasting && NetworkState == NetworkState.ConnectedAsLocalHost) {
			using var udpClient = new UdpClient() { EnableBroadcast = true };
			BroadcastCancellation = new CancellationTokenSource();
			BroadcastCancellation.Token.Register(udpClient.Close);
			try {
				var endpoint = new IPEndPoint(IPAddress.Broadcast, DiscoverPort);
				var payload = Encoding.UTF8.GetBytes($"MyServer;{Port}");
				while (!BroadcastCancellation.IsCancellationRequested) {
					await udpClient.SendAsync(payload, payload.Length, endpoint);
					await Task.Delay((int)(BroadcastInterval * 1000f), BroadcastCancellation.Token);
				}
			} catch { }
		}
	}
	public static void StopBroadcast() {
		BroadcastCancellation?.Cancel();
		BroadcastCancellation?.Dispose();
		BroadcastCancellation = null;
	}

	public static void StartDiscover(List<(string ipAddress, ushort port)> list) {
		StartDiscoverAsync(list).ContinueWith(_ => {
		}, TaskScheduler.FromCurrentSynchronizationContext());
	}
	static async Task StartDiscoverAsync(List<(string ipAddress, ushort port)> list) {
		if (!IsDiscovering) {
			list ??= new();
			list.Clear();
			using var udpClient = new UdpClient(DiscoverPort);
			DiscoverCancellation = new CancellationTokenSource();
			DiscoverCancellation.Token.Register(udpClient.Close);
			try {
				while (!DiscoverCancellation.IsCancellationRequested) {
					var receive = await udpClient.ReceiveAsync();
					var message = Encoding.UTF8.GetString(receive.Buffer).Split(';');
					string ipAddress = receive.RemoteEndPoint.Address.ToString();
					ushort port = ushort.TryParse(message[^1], out var p) ? p : default;
					if (!list.Exists(x => x.ipAddress == ipAddress && x.port == port)) {
						list.Add((ipAddress, port));
					}
				}
			} catch { }
		}
	}
	public static void StopDiscover() {
		DiscoverCancellation?.Cancel();
		DiscoverCancellation?.Dispose();
		DiscoverCancellation = null;
	}



	// Chat Methods

	public static void SendChatMessage(string message) {
		ChatQueue.Enqueue(message);
	}



	// Utility Methods

	static RelayServerData GetRelayServerData(Allocation data, string type = "dtls") {
		var endpoint = data.ServerEndpoints.FirstOrDefault(x => x.ConnectionType.Equals(type));
		return new RelayServerData(endpoint.Host, (ushort)endpoint.Port, data.AllocationIdBytes,
			data.ConnectionData, data.ConnectionData, data.Key, type.Equals("dtls"));
	}
	static RelayServerData GetRelayClientData(JoinAllocation data, string type = "dtls") {
		var endpoint = data.ServerEndpoints.FirstOrDefault(x => x.ConnectionType.Equals(type));
		return new RelayServerData(endpoint.Host, (ushort)endpoint.Port, data.AllocationIdBytes,
			data.ConnectionData, data.HostConnectionData, data.Key, type.Equals("dtls"));
	}

	static void DestroyLocalSimulationWorld() {
		for (int i = World.All.Count - 1; -1 < i; i--) {
			if (World.All[i].Flags == WorldFlags.Game) {
				World.All[i].Dispose();
			}
		}
	}
	static void DestroyServerClientSimulationWorld() {
		for (int i = World.All.Count - 1; -1 < i; i--) {
			if (World.All[i].IsServer() || World.All[i].IsClient()) {
				World.All[i].Dispose();
			}
		}
	}

	static void Listen(World server, NetworkEndpoint endpoint) {
		using var query = server.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver));
		if (query.HasSingleton<NetworkStreamDriver>()) {
			var driver = query.GetSingletonRW<NetworkStreamDriver>();
			driver.ValueRW.RequireConnectionApproval = true;
			driver.ValueRW.Listen(endpoint);
		}
	}
	static void Connect(World client, NetworkEndpoint endpoint) {
		using var query = client.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver));
		if (query.HasSingleton<NetworkStreamDriver>()) {
			var driver = query.GetSingletonRW<NetworkStreamDriver>();
			driver.ValueRW.RequireConnectionApproval = true;
			driver.ValueRW.Connect(client.EntityManager, endpoint);
		}
	}

	public static void ConnectionSucceeded() {
		if (NetworkState == NetworkState.Connecting) {
			NetworkState = NetworkState.ConnectionSucceeded;
		}
	}
	public static void ConnectionFailed() {
		if (NetworkState == NetworkState.Connecting) {
			NetworkState = NetworkState.ConnectionFailed;
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager Approval System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct RequestApprovalRpc : IApprovalRpcCommand {
	public FixedString512Bytes payload;
}



[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(DOTSSimulationSystemGroup), OrderLast = true)]
public partial class NetworkManagerServerApprovalSystem : SystemBase {
	EndSimulationEntityCommandBufferSystem System;
	EntityQuery QueryApproved;

	protected override void OnCreate() {
		System = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
		QueryApproved = GetEntityQuery(ComponentType.ReadOnly<ConnectionApproved>());
		RequireForUpdate<NetworkStreamDriver>();
		RequireForUpdate<PrefabContainer>();
	}

	protected override void OnUpdate() {
		var buffer = System.CreateCommandBuffer();
		var numApproved = QueryApproved.CalculateEntityCount();
		foreach (var (rpc, approval, entity) in SystemAPI
			.Query<RefRO<ReceiveRpcCommandRequest>, RefRW<RequestApprovalRpc>>()
			.WithEntityAccess()) {
			var connectionEntity = rpc.ValueRO.SourceConnection;
			var match = true;
			match &= approval.ValueRO.payload.Equals("0000");
			match &= numApproved < NetworkManager.MaxPlayers;
			if (match) {
				numApproved++;
				buffer.AddComponent(connectionEntity, new ConnectionApproved());
				buffer.DestroyEntity(entity);
			} else {
				buffer.AddComponent(connectionEntity, new NetworkStreamRequestDisconnect());
				buffer.DestroyEntity(entity);
			}
		}
		var connections = SystemAPI.GetSingleton<NetworkStreamDriver>().ConnectionEventsForTick;
		foreach (var connection in connections) switch (connection.State) {
			case ConnectionState.State.Connected:
				var connectionEntity = connection.ConnectionEntity;
				buffer.AddComponent(connectionEntity, new NetworkStreamInGame());
				var prefabContainer = SystemAPI.GetSingletonBuffer<PrefabContainer>(true);
				var position = new float3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
				var networkId = SystemAPI.GetComponent<NetworkId>(connectionEntity).Value;
				var player = buffer.Instantiate(prefabContainer[(int)Prefab.Player].Prefab);
				buffer.SetComponent(player, LocalTransform.FromPosition(position));
				buffer.SetComponent(player, new GhostOwner { NetworkId = networkId });
				buffer.AppendToBuffer(connectionEntity, new LinkedEntityGroup { Value = player });
				break;
			case ConnectionState.State.Disconnected:
				break;
		}
	}
}



[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(DOTSSimulationSystemGroup), OrderLast = true)]
public partial class NetworkManagerClientApprovalSystem : SystemBase {
	EndSimulationEntityCommandBufferSystem System;

	protected override void OnCreate() {
		System = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
		RequireForUpdate<NetworkStreamDriver>();
	}

	protected override void OnUpdate() {
		var buffer = System.CreateCommandBuffer();
		var connections = SystemAPI.GetSingleton<NetworkStreamDriver>().ConnectionEventsForTick;
		foreach (var connection in connections) switch (connection.State) {
			case ConnectionState.State.Approval:
				var approvalEntity = buffer.CreateEntity();
				buffer.AddComponent(approvalEntity, new SendRpcCommandRequest());
				buffer.AddComponent(approvalEntity, new RequestApprovalRpc { payload = "0000" });
				break;
			case ConnectionState.State.Connected:
				var connectionEntity = connection.ConnectionEntity;
				buffer.AddComponent(connectionEntity, new NetworkStreamInGame());
				NetworkManager.ConnectionSucceeded();
				break;
			case ConnectionState.State.Disconnected:
				NetworkManager.ConnectionFailed();
				break;
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager Message System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct ChatMessageRpc : IRpcCommand {
	public FixedString512Bytes text;
}



[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(DOTSSimulationSystemGroup), OrderLast = true)]
public partial class NetworkManagerServerMessageSystem : SystemBase {
	EndSimulationEntityCommandBufferSystem System;

	protected override void OnCreate() {
		System = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
		RequireForUpdate<ReceiveRpcCommandRequest>();
		RequireForUpdate<ChatMessageRpc>();
	}

	protected override void OnUpdate() {
		var buffer = System.CreateCommandBuffer();
		foreach (var (rpc, message, entity) in SystemAPI
			.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<ChatMessageRpc>>()
			.WithEntityAccess()) {
			var text = message.ValueRO.text;
			var messageEntity = buffer.CreateEntity();
			buffer.AddComponent(messageEntity, new SendRpcCommandRequest());
			buffer.AddComponent(messageEntity, new ChatMessageRpc { text = text });
			buffer.DestroyEntity(entity);
		}
	}
}



[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(DOTSSimulationSystemGroup), OrderLast = true)]
public partial class NetworkManagerClientMessageSystem : SystemBase {
	EndSimulationEntityCommandBufferSystem System;

	protected override void OnCreate() {
		System = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
	}

	protected override void OnUpdate() {
		var buffer = System.CreateCommandBuffer();
		while (NetworkManager.ChatQueue.TryDequeue(out var text)) {
			var messageEntity = buffer.CreateEntity();
			buffer.AddComponent(messageEntity, new SendRpcCommandRequest());
			buffer.AddComponent(messageEntity, new ChatMessageRpc { text = text });
		}
		foreach (var (rpc, message, entity) in SystemAPI
			.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<ChatMessageRpc>>()
			.WithEntityAccess()) {
			var text = message.ValueRO.text.ToString();
			NetworkManager.OnChatReceived.Invoke(text);
			buffer.DestroyEntity(entity);
		}
	}
}

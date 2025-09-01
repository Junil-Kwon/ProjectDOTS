using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Linq;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
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



public enum ServiceState {
	Uninitialized,
	Initializing,
	Unsigned,
	Signing,
	Ready,
}

public enum NetworkState {
	Disconnected,
	Allocating,
	SceneLoading,
	Connecting,
	ConnectionFailed,
	ConnectionSucceeded,
	ConnectedAsRelayHost,
	ConnectedAsRelayClient,
	ConnectedAsLocalHost,
	ConnectedAsLocalClient,
}

public enum NetworkError {
	NoError,
	InitializationFailed,
	SignInFailed,
	RelayConnectionFailed,
	ConnectionTimedOut,
	ServerConnectionClosed,
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Singleton Manager/Network Manager")]
public sealed class NetworkManager : MonoSingleton<NetworkManager> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(NetworkManager))]
	class NetworkManagerEditor : EditorExtensions {
		NetworkManager I => target as NetworkManager;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Scene", EditorStyles.boldLabel);
			DefaultScene = SceneField("Default Scene", DefaultScene);
			ConnectScene = SceneField("Connect Scene", ConnectScene);
			Space();

			LabelField("Chat Message", EditorStyles.boldLabel);
			PropertyField("m_OnMessageReceived");
			Space();

			LabelField("Network Status", EditorStyles.boldLabel);
			LabelField("Network Tick Rate", $"{TickRate} Hz");
			var serviceState = ServiceState.ToString();
			var networkState = NetworkState.ToString();
			var networkError = NetworkError.ToString();
			LabelField("Service State", $"{Regex.Replace(serviceState, @"(?<!^)(?=[A-Z])", " ")}");
			LabelField("Network State", $"{Regex.Replace(networkState, @"(?<!^)(?=[A-Z])", " ")}");
			LabelField("Network Error", $"{Regex.Replace(networkError, @"(?<!^)(?=[A-Z])", " ")}");
			Space();

			End();
		}
	}
	#endif



	// Constants

	public const float TickRate = 60f;
	public const float TickInterval = 1f / TickRate;

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

	[SerializeField] int m_DefaultScene;
	[SerializeField] int m_ConnectScene;

	ServiceState m_ServiceState;
	NetworkState m_NetworkState;
	NetworkError m_NetworkError;
	int m_MaxPlayers;
	ushort m_Port;

	CancellationTokenSource m_BroadcastCancellation;
	CancellationTokenSource m_DiscoverCancellation;

	Stack<StringBuilder> m_BuilderPool = new();
	Queue<StringBuilder> m_MessageQueue = new();
	[SerializeField] UnityEvent<StringBuilder> m_OnMessageReceived = new();



	// Properties

	static int DefaultScene {
		get => Instance.m_DefaultScene;
		set => Instance.m_DefaultScene = value;
	}
	static int ConnectScene {
		get => Instance.m_ConnectScene;
		set => Instance.m_ConnectScene = value;
	}



	public static ServiceState ServiceState {
		get         => Instance.m_ServiceState;
		private set => Instance.m_ServiceState = value;
	}
	public static NetworkState NetworkState {
		get         => Instance.m_NetworkState;
		private set => Instance.m_NetworkState = value;
	}
	public static NetworkError NetworkError {
		get         => Instance.m_NetworkError;
		private set => Instance.m_NetworkError = value;
	}
	public static int MaxPlayers {
		get         => Instance.m_MaxPlayers;
		private set => Instance.m_MaxPlayers = value;
	}
	static ushort Port {
		get => Instance.m_Port;
		set => Instance.m_Port = value;
	}

	public static bool IsSinglePlayer {
		get => MaxPlayers == 1;
	}
	public static bool IsMultiPlayer {
		get => !IsSinglePlayer;
	}



	static CancellationTokenSource BroadcastCancellation {
		get => Instance.m_BroadcastCancellation;
		set => Instance.m_BroadcastCancellation = value;
	}
	static CancellationTokenSource DiscoverCancellation {
		get => Instance.m_DiscoverCancellation;
		set => Instance.m_DiscoverCancellation = value;
	}

	public static bool IsBroadcasting {
		get => BroadcastCancellation != null;
	}
	public static bool IsDiscovering {
		get => DiscoverCancellation != null;
	}



	static Stack<StringBuilder> BuilderPool {
		get => Instance.m_BuilderPool;
	}
	static Queue<StringBuilder> MessageQueue {
		get => Instance.m_MessageQueue;
	}
	static UnityEvent<StringBuilder> OnMessageReceived {
		get => Instance.m_OnMessageReceived;
	}



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
			if (task.IsFaulted) {
				ServiceState = ServiceState.Uninitialized;
				NetworkError = NetworkError.InitializationFailed;
			} else {
				ServiceState = ServiceState.Unsigned;
				NetworkError = NetworkError.NoError;
			}
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
			if (task.IsFaulted) {
				ServiceState = ServiceState.Unsigned;
				NetworkError = NetworkError.SignInFailed;
			} else {
				ServiceState = ServiceState.Ready;
				NetworkError = NetworkError.NoError;
			}
		}
	}



	// Connection Methods

	public static void CreateRelayHost(
		int maxPlayers, Action<bool> onConnected = null) {
		CreateRelayHostAsync(maxPlayers).ContinueWith(_ => {
			onConnected?.Invoke(NetworkState == NetworkState.ConnectedAsRelayHost);
		}, TaskScheduler.FromCurrentSynchronizationContext());
	}

	static async Task CreateRelayHostAsync(int maxPlayers) {
		if (ServiceState == ServiceState.Uninitialized) await InitializeAsync();
		if (ServiceState == ServiceState.Unsigned) await SignInAnonymouslyAsync();
		if (NetworkState != NetworkState.Disconnected) await DisconnectAsync();
		if (ServiceState == ServiceState.Ready && NetworkState == NetworkState.Disconnected) {

			NetworkState = NetworkState.Allocating;
			MaxPlayers = Mathf.Max(1, maxPlayers);
			RelayServerData relayServerData;
			RelayServerData relayClientData;
			try {
				var hostData = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);
				var joinCode = await RelayService.Instance.GetJoinCodeAsync(hostData.AllocationId);
				var joinData = await RelayService.Instance.JoinAllocationAsync(joinCode);
				GUIUtility.systemCopyBuffer = joinCode;
				relayServerData = GetRelayServerData(hostData);
				relayClientData = GetRelayClientData(joinData);
			} catch {
				NetworkState = NetworkState.ConnectionFailed;
				NetworkError = NetworkError.RelayConnectionFailed;
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
			await SceneManager.LoadSceneAsync(ConnectScene);
			await Task.Delay(1000);

			NetworkState = NetworkState.Connecting;
			Listen(server, NetworkEndpoint.AnyIpv4);
			Connect(client, relayClientData.Endpoint);
			float endTime = Time.realtimeSinceStartup + ConnectionTimeOut;
			while (NetworkState == NetworkState.Connecting) {
				await Task.Yield();
				if (endTime < Time.realtimeSinceStartup) {
					NetworkState = NetworkState.ConnectionFailed;
					NetworkError = NetworkError.ConnectionTimedOut;
					return;
				};
			}
			if (NetworkState == NetworkState.ConnectionSucceeded) {
				NetworkState = NetworkState.ConnectedAsRelayHost;
				NetworkError = NetworkError.NoError;
			}
		}
	}



	public static void JoinRelayServer(
		string joinCode, Action<bool> onConnected = null) {
		JoinRelayServerAsync(joinCode).ContinueWith(_ => {
			onConnected?.Invoke(NetworkState == NetworkState.ConnectedAsRelayClient);
		}, TaskScheduler.FromCurrentSynchronizationContext());
	}

	static async Task JoinRelayServerAsync(string joinCode) {
		if (ServiceState == ServiceState.Uninitialized) await InitializeAsync();
		if (ServiceState == ServiceState.Unsigned) await SignInAnonymouslyAsync();
		if (NetworkState != NetworkState.Disconnected) await DisconnectAsync();
		if (ServiceState == ServiceState.Ready && NetworkState == NetworkState.Disconnected) {

			NetworkState = NetworkState.Allocating;
			RelayServerData relayClientData;
			try {
				var joinData = await RelayService.Instance.JoinAllocationAsync(joinCode);
				relayClientData = GetRelayClientData(joinData);
			} catch {
				NetworkState = NetworkState.ConnectionFailed;
				NetworkError = NetworkError.RelayConnectionFailed;
				return;
			}

			NetworkState = NetworkState.SceneLoading;
			var prevConstructor = NetworkStreamReceiveSystem.DriverConstructor;
			var nextConstructor = new RelayDriverConstructor(default, relayClientData);
			NetworkStreamReceiveSystem.DriverConstructor = nextConstructor;
			var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
			NetworkStreamReceiveSystem.DriverConstructor = prevConstructor;
			DestroyLocalSimulationWorld();
			await SceneManager.LoadSceneAsync(ConnectScene);
			await Task.Delay(1000);

			NetworkState = NetworkState.Connecting;
			Connect(client, relayClientData.Endpoint);
			float endTime = Time.realtimeSinceStartup + ConnectionTimeOut;
			while (NetworkState == NetworkState.Connecting) {
				await Task.Yield();
				if (endTime < Time.realtimeSinceStartup) {
					NetworkState = NetworkState.ConnectionFailed;
					NetworkError = NetworkError.ConnectionTimedOut;
					return;
				};
			}
			if (NetworkState == NetworkState.ConnectionSucceeded) {
				NetworkState = NetworkState.ConnectedAsRelayClient;
				NetworkError = NetworkError.NoError;
			}
		}
	}



	public static void CreateLocalHost(
		ushort port, int maxPlayers, Action<bool> onConnected = null) {
		CreateLocalHostAsync(port, maxPlayers).ContinueWith(_ => {
			onConnected?.Invoke(NetworkState == NetworkState.ConnectedAsLocalHost);
		}, TaskScheduler.FromCurrentSynchronizationContext());
	}

	static async Task CreateLocalHostAsync(ushort port, int maxPlayers) {
		if (NetworkState != NetworkState.Disconnected) await DisconnectAsync();
		if (NetworkState == NetworkState.Disconnected) {

			NetworkState = NetworkState.SceneLoading;
			MaxPlayers = Mathf.Max(1, maxPlayers);
			var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
			var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
			DestroyLocalSimulationWorld();
			await SceneManager.LoadSceneAsync(ConnectScene);
			await Task.Delay(1000);

			NetworkState = NetworkState.Connecting;
			Listen(server, NetworkEndpoint.AnyIpv4.WithPort(Port = port));
			Connect(client, NetworkEndpoint.LoopbackIpv4.WithPort(port));
			float endTime = Time.realtimeSinceStartup + ConnectionTimeOut;
			while (NetworkState == NetworkState.Connecting) {
				await Task.Yield();
				if (endTime < Time.realtimeSinceStartup) {
					NetworkState = NetworkState.ConnectionFailed;
					NetworkError = NetworkError.ConnectionTimedOut;
					return;
				};
			}
			if (NetworkState == NetworkState.ConnectionSucceeded) {
				NetworkState = NetworkState.ConnectedAsLocalHost;
				NetworkError = NetworkError.NoError;
			}
		}
	}



	public static void JoinLocalServer(
		string address, ushort port, Action<bool> onConnected = null) {
		JoinLocalServerAsync(address, port).ContinueWith(_ => {
			onConnected?.Invoke(NetworkState == NetworkState.ConnectedAsLocalClient);
		}, TaskScheduler.FromCurrentSynchronizationContext());
	}

	static async Task JoinLocalServerAsync(string address, ushort port) {
		if (NetworkState != NetworkState.Disconnected) await DisconnectAsync();
		if (NetworkState == NetworkState.Disconnected) {

			NetworkState = NetworkState.SceneLoading;
			var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
			DestroyLocalSimulationWorld();
			await SceneManager.LoadSceneAsync(ConnectScene);
			await Task.Delay(1000);

			NetworkState = NetworkState.Connecting;
			Connect(client, NetworkEndpoint.Parse(address, port));
			float endTime = Time.realtimeSinceStartup + ConnectionTimeOut;
			while (NetworkState == NetworkState.Connecting) {
				await Task.Yield();
				if (endTime < Time.realtimeSinceStartup) {
					NetworkState = NetworkState.ConnectionFailed;
					NetworkError = NetworkError.ConnectionTimedOut;
					return;
				};
			}
			if (NetworkState == NetworkState.ConnectionSucceeded) {
				NetworkState = NetworkState.ConnectedAsLocalClient;
				NetworkError = NetworkError.NoError;
			}
		}
	}



	public static void Connect() {
		var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		var port = (ushort)((IPEndPoint)listener.LocalEndpoint).Port;
		listener.Stop();
		CreateLocalHost(port, 1);
	}

	public static void Disconnect(
		Action<bool> onDisconnected = null) {
		DisconnectAsync().ContinueWith(_ => {
			onDisconnected?.Invoke(NetworkState == NetworkState.Disconnected);
		}, TaskScheduler.FromCurrentSynchronizationContext());
	}

	static async Task DisconnectAsync() {
		while (true) {
			bool isConnecting = false;
			isConnecting = isConnecting || NetworkState == NetworkState.Allocating;
			isConnecting = isConnecting || NetworkState == NetworkState.SceneLoading;
			isConnecting = isConnecting || NetworkState == NetworkState.Connecting;
			isConnecting = isConnecting || NetworkState == NetworkState.ConnectionSucceeded;
			if (!isConnecting) break;
			else await Task.Yield();
		}
		NetworkState = NetworkState.Disconnected;
		DestroyLocalSimulationWorld();
		DestroyServerClientSimulationWorld();
		ClientServerBootstrap.CreateLocalWorld("DefaultWorld");
		await SceneManager.LoadSceneAsync(DefaultScene);
		await Task.Delay(1000);
		NetworkState = NetworkState.Disconnected;
		NetworkError = NetworkError.NoError;
	}



	// Broadcast Methods

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
				var payload = Encoding.UTF8.GetBytes($"MyServer;{MaxPlayers};{Port}");
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



	// Instance Methods

	static StringBuilder GetOrCreateInstance() {
		if (!BuilderPool.TryPop(out var instance)) {
			instance = new StringBuilder();
		}
		return instance;
	}

	static void RemoveInstance(StringBuilder instance) {
		instance.Clear();
		BuilderPool.Push(instance);
	}



	// Chat Methods

	public static void SendChatMessage(string text) {
		var builder = GetOrCreateInstance();
		MessageQueue.Enqueue(builder.Append(text));
	}

	public static void SendChatMessage(StringBuilder builder) {
		MessageQueue.Enqueue(builder);
	}

	public static bool TryDequeueMessage(out FixedString512Bytes message) {
		if (MessageQueue.TryDequeue(out var builder)) {
			message = new FixedString512Bytes();
			for (int i = 0; i < builder.Length; i++) message.Append(builder[i]);
			RemoveInstance(builder);
			return true;
		}
		message = default;
		return false;
	}

	public static void InvokeOnMessageReceived(FixedString512Bytes message) {
		var builder = GetOrCreateInstance();
		for (int i = 0; i < message.Length; i++) builder.Append(message[i]);
		OnMessageReceived.Invoke(builder);
		RemoveInstance(builder);
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
			NetworkError = NetworkError.NoError;
		}
	}

	public static void ConnectionFailed() {
		if (NetworkState == NetworkState.Connecting) {
			NetworkState = NetworkState.ConnectionFailed;
			NetworkError = NetworkError.ServerConnectionClosed;
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Request Approval RPC
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct RequestApprovalRpc : IApprovalRpcCommand {

	// Fields

	public FixedString32Bytes Payload;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager Approval System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(DOTSServerSimulationSystemGroup), OrderFirst = true)]
public partial class NetworkManagerServerApprovalSystem : SystemBase {
	BeginInitializationEntityCommandBufferSystem System;
	EntityQuery QueryApproved;

	protected override void OnCreate() {
		System = World.GetOrCreateSystemManaged<BeginInitializationEntityCommandBufferSystem>();
		QueryApproved = GetEntityQuery(ComponentType.ReadOnly<ConnectionApproved>());
		RequireForUpdate<NetworkStreamDriver>();
		RequireForUpdate<CharacterContainer>();
	}

	protected override void OnUpdate() {
		var buffer = System.CreateCommandBuffer();
		var connections = SystemAPI.GetSingleton<NetworkStreamDriver>().ConnectionEventsForTick;
		foreach (var connection in connections) switch (connection.State) {

			case ConnectionState.State.Connected: {
				var connectionEntity = connection.ConnectionEntity;
				buffer.AddComponent(connectionEntity, new NetworkStreamInGame());
				var characterContainer = SystemAPI.GetSingletonBuffer<CharacterContainer>(true);
				var prefab = characterContainer.GetPrefab(Character.Player);
				var networkId = SystemAPI.GetComponent<NetworkId>(connectionEntity).Value;

				var entity = buffer.Instantiate(prefab);
				buffer.SetComponent(entity, LocalTransform.FromPosition(float3.zero));
				buffer.SetComponent(entity, new GhostOwner { NetworkId = networkId });
				buffer.AppendToBuffer(connectionEntity, new LinkedEntityGroup { Value = entity });
			} break;

			case ConnectionState.State.Disconnected: {
				var connectionEntity = connection.ConnectionEntity;
				// ...
			} break;
		}

		var numApproved = QueryApproved.CalculateEntityCount();
		foreach (var (rpc, approval, entity) in
			SystemAPI.Query<
				RefRO<ReceiveRpcCommandRequest>,
				RefRO<RequestApprovalRpc>
			>().WithEntityAccess()) {

			var connectionEntity = rpc.ValueRO.SourceConnection;
			var match = true;
			match = match && approval.ValueRO.Payload.Equals("0000");
			match = match && numApproved < NetworkManager.MaxPlayers;
			if (match) {
				numApproved++;
				buffer.AddComponent(connectionEntity, new ConnectionApproved());
				buffer.DestroyEntity(entity);
			} else {
				buffer.AddComponent(connectionEntity, new NetworkStreamRequestDisconnect());
				buffer.DestroyEntity(entity);
			}
		}
	}
}



[UpdateInGroup(typeof(DOTSClientSimulationSystemGroup), OrderFirst = true)]
public partial class NetworkManagerClientApprovalSystem : SystemBase {
	BeginInitializationEntityCommandBufferSystem System;

	protected override void OnCreate() {
		System = World.GetOrCreateSystemManaged<BeginInitializationEntityCommandBufferSystem>();
		RequireForUpdate<NetworkStreamDriver>();
	}

	protected override void OnUpdate() {
		var buffer = System.CreateCommandBuffer();
		var connections = SystemAPI.GetSingleton<NetworkStreamDriver>().ConnectionEventsForTick;
		foreach (var connection in connections) switch (connection.State) {

			case ConnectionState.State.Approval: {
				var approvalEntity = buffer.CreateEntity();
				buffer.AddComponent(approvalEntity, new SendRpcCommandRequest());
				buffer.AddComponent(approvalEntity, new RequestApprovalRpc { Payload = "0000" });
			} break;

			case ConnectionState.State.Connected: {
				var connectionEntity = connection.ConnectionEntity;
				buffer.AddComponent(connectionEntity, new NetworkStreamInGame());
				NetworkManager.ConnectionSucceeded();
			} break;

			case ConnectionState.State.Disconnected: {
				NetworkManager.ConnectionFailed();
			} break;
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Chat Message RPC
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct ChatMessageRpc : IRpcCommand {

	// Fields

	public FixedString512Bytes Message;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager Message System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(DOTSServerSimulationSystemGroup), OrderFirst = true)]
public partial class NetworkManagerServerMessageSystem : SystemBase {
	BeginInitializationEntityCommandBufferSystem System;

	protected override void OnCreate() {
		System = World.GetOrCreateSystemManaged<BeginInitializationEntityCommandBufferSystem>();
		RequireForUpdate<ReceiveRpcCommandRequest>();
		RequireForUpdate<ChatMessageRpc>();
	}

	protected override void OnUpdate() {
		var buffer = System.CreateCommandBuffer();
		foreach (var (receiveRpc, messageRpc, entity) in
			SystemAPI.Query<
				RefRO<ReceiveRpcCommandRequest>,
				RefRO<ChatMessageRpc>
			>().WithEntityAccess()) {

			var messageEntity = buffer.CreateEntity();
			buffer.AddComponent(messageEntity, new SendRpcCommandRequest());
			buffer.AddComponent(messageEntity, messageRpc.ValueRO);
			buffer.DestroyEntity(entity);
		}
	}
}



[UpdateInGroup(typeof(DOTSClientSimulationSystemGroup), OrderFirst = true)]
public partial class NetworkManagerClientMessageSystem : SystemBase {
	BeginInitializationEntityCommandBufferSystem System;

	protected override void OnCreate() {
		System = World.GetOrCreateSystemManaged<BeginInitializationEntityCommandBufferSystem>();
	}

	protected override void OnUpdate() {
		var buffer = System.CreateCommandBuffer();
		while (NetworkManager.TryDequeueMessage(out var message)) {
			var messageEntity = buffer.CreateEntity();
			buffer.AddComponent(messageEntity, new SendRpcCommandRequest());
			buffer.AddComponent(messageEntity, new ChatMessageRpc { Message = message });
		}
		foreach (var (receiveRpc, messageRpc, entity) in
			SystemAPI.Query<
				RefRO<ReceiveRpcCommandRequest>,
				RefRO<ChatMessageRpc>	
			>().WithEntityAccess()) {

			var message = messageRpc.ValueRO.Message;
			NetworkManager.InvokeOnMessageReceived(message);
			buffer.DestroyEntity(entity);
		}
	}
}

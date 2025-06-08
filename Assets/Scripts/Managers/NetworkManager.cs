using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using Random = UnityEngine.Random;

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
	ConnectedAsHost,
	ConnectedAsClient,
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

				LabelField("Debug", EditorStyles.boldLabel);
				BeginDisabledGroup();
				var serviceState = Regex.Replace($"{ServiceState}", "(?<=[a-z])(?=[A-Z])", " ");
				var networkState = Regex.Replace($"{NetworkState}", "(?<=[a-z])(?=[A-Z])", " ");
				TextField("Service State", serviceState);
				TextField("Network State", networkState);
				Space();
				TextField("Players", $"{Players.Count} / {MaxPlayers}");
				foreach (var player in Players) TextField(" ", $"{player}");
				EndDisabledGroup();
				Space();

				End();
			}
		}
	#endif



	// Constants

	public const float Tickrate = 60f;
	public const float Ticktime = 1f / Tickrate;

	const float ConnectionTimeOut = 5f;



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

	int m_MaxPlayers;
	readonly List<Entity> m_Players = new();



	// Properties

	public static ServiceState ServiceState {
		get         => Instance.m_ServiceState;
		private set => Instance.m_ServiceState = value;
	}
	public static NetworkState NetworkState {
		get         => Instance.m_NetworkState;
		private set => Instance.m_NetworkState = value;
	}
	public static bool IsHost   => NetworkState == NetworkState.ConnectedAsHost;
	public static bool IsClient => NetworkState == NetworkState.ConnectedAsClient;



	public static int MaxPlayers {
		get         => Instance.m_MaxPlayers;
		private set => Instance.m_MaxPlayers = value;
	}
	public static List<Entity> Players => Instance.m_Players;



	// Authentication Methods

	public static void Initialize() {
		_ = InitializeAsync();
	}
	public static async Task InitializeAsync() {
		if (ServiceState == ServiceState.Uninitialized) {
			ServiceState  = ServiceState.Initializing;
			var task = UnityServices.InitializeAsync();
			await task;
			if (task.IsFaulted) {
				ServiceState = ServiceState.Uninitialized;
				throw task.Exception;
			}
			ServiceState = ServiceState.Unsigned;
		}
	}

	public static void SignInAnonymously() {
		_ = SignInAnonymouslyAsync();
	}
	public static async Task SignInAnonymouslyAsync() {
		if (ServiceState == ServiceState.Unsigned) {
			ServiceState  = ServiceState.Signing;
			var task = AuthenticationService.Instance.SignInAnonymouslyAsync();
			await task;
			if (task.IsFaulted) {
				ServiceState = ServiceState.Unsigned;
				throw task.Exception;
			}
			ServiceState = ServiceState.Ready;
		}
	}



	// Relay Connection Methods

	public static void CreateRelayHost(int maxPlayers) {
		_ = CreateRelayHostAsync(maxPlayers);
	}
	public static async Task CreateRelayHostAsync(int maxPlayers) {
		if (ServiceState == ServiceState.Uninitialized) await InitializeAsync();
		if (ServiceState == ServiceState.Unsigned     ) await SignInAnonymouslyAsync();
		if (NetworkState != NetworkState.Disconnected ) Disconnect();
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
			}
			catch (RelayServiceException) { NetworkState = NetworkState.ConnectionFailed; return; }
			catch (ArgumentNullException) { NetworkState = NetworkState.ConnectionFailed; return; }

			NetworkState = NetworkState.SceneLoading;
			var prevConstructor = NetworkStreamReceiveSystem.DriverConstructor;
			var nextConstructor = new RelayDriverConstructor(relayServerData, relayClientData);
			NetworkStreamReceiveSystem.DriverConstructor = nextConstructor;
			var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
			var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
			NetworkStreamReceiveSystem.DriverConstructor = prevConstructor;
			DestroyLocalSimulationWorld();
			await SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);

			NetworkState = NetworkState.Connecting;
			Listen (server, NetworkEndpoint.AnyIpv4);
			Connect(client, relayClientData.Endpoint);
			float timeStartConnecting = Time.realtimeSinceStartup;
			while (NetworkState == NetworkState.Connecting) {
				await Task.Yield();
				if (ConnectionTimeOut < Time.realtimeSinceStartup - timeStartConnecting) {
					NetworkState = NetworkState.ConnectionFailed;
					return;
				};
			}
			if (NetworkState == NetworkState.ConnectionSucceeded) {
				NetworkState = NetworkState.ConnectedAsHost;
			}
		}
	}

	public static void JoinRelayServer(string joinCode) {
		_ = JoinRelayServerAsync(joinCode);
	}
	public static async Task JoinRelayServerAsync(string joinCode) {
		if (ServiceState == ServiceState.Uninitialized) await InitializeAsync();
		if (ServiceState == ServiceState.Unsigned     ) await SignInAnonymouslyAsync();
		if (NetworkState != NetworkState.Disconnected ) Disconnect();
		if (ServiceState == ServiceState.Ready && NetworkState == NetworkState.Disconnected) {

			NetworkState = NetworkState.Allocating;
			RelayServerData relayClientData;
			try {
				var joinData = await RelayService.Instance.JoinAllocationAsync(joinCode);
				relayClientData = GetRelayClientData(joinData);
			}
			catch (RelayServiceException) { NetworkState = NetworkState.ConnectionFailed; return; }
			catch (ArgumentNullException) { NetworkState = NetworkState.ConnectionFailed; return; }

			NetworkState = NetworkState.SceneLoading;
			var prevConstructor = NetworkStreamReceiveSystem.DriverConstructor;
			var nextConstructor = new RelayDriverConstructor(default, relayClientData);
			NetworkStreamReceiveSystem.DriverConstructor = nextConstructor;
			var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
			NetworkStreamReceiveSystem.DriverConstructor = prevConstructor;
			DestroyLocalSimulationWorld();
			await SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);

			NetworkState = NetworkState.Connecting;
			Connect(client, relayClientData.Endpoint);
			float timeStartConnecting = Time.realtimeSinceStartup;
			while (NetworkState == NetworkState.Connecting) {
				await Task.Yield();
				if (ConnectionTimeOut < Time.realtimeSinceStartup - timeStartConnecting) {
					NetworkState = NetworkState.ConnectionFailed;
					return;
				};
			}
			if (NetworkState == NetworkState.ConnectionSucceeded) {
				NetworkState = NetworkState.ConnectedAsHost;
			}
		}
	}



	// Local Connection Methods

	public static void CreateLocalHost(ushort port, int maxPlayers) {
		_ = CreateLocalHostAsync(port, maxPlayers);
	}
	public static async Task CreateLocalHostAsync(ushort port, int maxPlayers) {
		if (NetworkState != NetworkState.Disconnected) Disconnect();
		if (NetworkState == NetworkState.Disconnected) {

			NetworkState = NetworkState.SceneLoading;
			MaxPlayers = Mathf.Max(1, maxPlayers);
			var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
			var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
			DestroyLocalSimulationWorld();
			await SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);

			NetworkState = NetworkState.Connecting;
			Listen (server, NetworkEndpoint.AnyIpv4.WithPort(port));
			Connect(client, NetworkEndpoint.LoopbackIpv4.WithPort(port));
			float timeStartConnecting = Time.realtimeSinceStartup;
			while (NetworkState == NetworkState.Connecting) {
				await Task.Yield();
				if (ConnectionTimeOut < Time.realtimeSinceStartup - timeStartConnecting) {
					NetworkState = NetworkState.ConnectionFailed;
					return;
				};
			}
			if (NetworkState == NetworkState.ConnectionSucceeded) {
				NetworkState = NetworkState.ConnectedAsHost;
			}
		}
	}

	public static void JoinLocalServer(string address, ushort port) {
		_ = JoinLocalServerAsync(address, port);
	}
	public static async Task JoinLocalServerAsync(string address, ushort port) {
		if (NetworkState != NetworkState.Disconnected) Disconnect();
		if (NetworkState == NetworkState.Disconnected) {

			NetworkState = NetworkState.SceneLoading;
			var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
			DestroyLocalSimulationWorld();
			await SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);

			NetworkState = NetworkState.Connecting;
			Connect(client, NetworkEndpoint.Parse(address, port));
			float timeStartConnecting = Time.realtimeSinceStartup;
			while (NetworkState == NetworkState.Connecting) {
				await Task.Yield();
				if (ConnectionTimeOut < Time.realtimeSinceStartup - timeStartConnecting) {
					NetworkState = NetworkState.ConnectionFailed;
					return;
				};
			}
			if (NetworkState == NetworkState.ConnectionSucceeded) {
				NetworkState = NetworkState.ConnectedAsHost;
			}
		}
	}



	// Connection Methods

	public static void Connect() {
		CreateLocalHost(7979, 1);
	}

	public static void Disconnect() {
		bool disconnectable = true;
		disconnectable &= NetworkState != NetworkState.Allocating;
		disconnectable &= NetworkState != NetworkState.SceneLoading;
		disconnectable &= NetworkState != NetworkState.Connecting;
		if (disconnectable) {
			NetworkState = NetworkState.Disconnected;
			ClientServerBootstrap.CreateLocalWorld("DefaultWorld");
			DestroyServerClientSimulationWorld();
		}
	}



	// Broadcast Methods

	public static void BroadcastLocalServer() {
		
	}

	public List<(string, ushort)> LookupLocalServer() {
		var servers = new List<(string, ushort)>();
		
		return servers;
	}



	// Chat Methods

	public static void SendChatMessage(string message) {
		var fixedString = Encoding.UTF8.GetByteCount(message) switch {
			<   32 => new FixedString32Bytes  (message),
			<   64 => new FixedString64Bytes  (message),
			<  128 => new FixedString128Bytes (message),
			<  512 => new FixedString512Bytes (message),
			< 4096 => new FixedString4096Bytes(message),
			_      => new FixedString4096Bytes(message),
		};
	}



	// Methods

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
		var worlds = new List<World>();
		foreach (var world in World.All) if (world.Flags == WorldFlags.Game) {
			worlds.Add(world);
		}
		foreach (var world in worlds) world.Dispose();
	}

	static void DestroyServerClientSimulationWorld() {
		var worlds = new List<World>();
		foreach (var world in World.All) if (world.IsServer() || world.IsClient()) {
			worlds.Add(world);
		}
		foreach (var world in worlds) world.Dispose();
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
		if (NetworkState == NetworkState.Connecting) NetworkState = NetworkState.ConnectionSucceeded;
	}

	public static void ConnectionFailed() {
		if (NetworkState == NetworkState.Connecting) NetworkState = NetworkState.ConnectionFailed;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager Server System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct RequestApproval : IApprovalRpcCommand {
	public FixedString512Bytes payload;
}



[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(DOTSSimulationSystemGroup), OrderLast = true)]
public partial class NetworkManagerServerSystem : SystemBase {
	BeginInitializationEntityCommandBufferSystem System;
	EntityQuery QueryApproved;

	protected override void OnCreate() {
		System = World.GetOrCreateSystemManaged<BeginInitializationEntityCommandBufferSystem>();
		QueryApproved = GetEntityQuery(ComponentType.ReadOnly<ConnectionApproved>());
		RequireForUpdate<NetworkStreamDriver>();
		RequireForUpdate<PrefabContainer>();
	}

	protected override void OnUpdate() {
		var buffer = System.CreateCommandBuffer();

		var connections = SystemAPI.GetSingleton<NetworkStreamDriver>().ConnectionEventsForTick;
		foreach (var connection in connections) switch (connection.State) {
			case ConnectionState.State.Handshake:
				break;
			case ConnectionState.State.Approval:
				break;
			case ConnectionState.State.Connected:
				var connectionEntity = connection.ConnectionEntity;
				buffer.AddComponent(connectionEntity, new NetworkStreamInGame());

				var prefabContainer = SystemAPI.GetSingletonBuffer<PrefabContainer>(true);
				var player = buffer.Instantiate(prefabContainer[(int)Prefab.Player].Prefab);
				var position  = new float3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
				var networkId = SystemAPI.GetComponent<NetworkId>(connectionEntity).Value;
				buffer.SetComponent(player, LocalTransform.FromPosition(position));
				buffer.SetComponent(player, new GhostOwner { NetworkId = networkId });
				buffer.AppendToBuffer(connectionEntity, new LinkedEntityGroup { Value = player });
				break;
			case ConnectionState.State.Disconnected:
				break;
		}

		var numApproved = QueryApproved.CalculateEntityCount();
		foreach (var (rpc, approval, entity) in SystemAPI
			.Query<RefRO<ReceiveRpcCommandRequest>, RefRW<RequestApproval>>()
			.WithEntityAccess()) {

			var connectionEntity = rpc.ValueRO.SourceConnection;
			var match = true;
			match &= approval.ValueRO.payload.Equals("0000");
			match &= numApproved < NetworkManager.MaxPlayers;
			if (match) {
				numApproved++;
				buffer.AddComponent(connectionEntity, new ConnectionApproved());
			} else {
				buffer.AddComponent(connectionEntity, new NetworkStreamRequestDisconnect());
			}
			buffer.DestroyEntity(entity); 
		}

		NetworkManager.Players.Clear();
		foreach (var (_, entity) in SystemAPI.Query<NetworkStreamInGame>().WithEntityAccess()) {
			NetworkManager.Players.Add(entity);
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager Client System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(DOTSSimulationSystemGroup), OrderLast = true)]
public partial class NetworkManagerClientSystem : SystemBase {
	EndInitializationEntityCommandBufferSystem System;

	protected override void OnCreate() {
		System = World.GetOrCreateSystemManaged<EndInitializationEntityCommandBufferSystem>();
		RequireForUpdate<NetworkStreamDriver>();
	}

	protected override void OnUpdate() {
		var buffer = System.CreateCommandBuffer();

		var connections = SystemAPI.GetSingleton<NetworkStreamDriver>().ConnectionEventsForTick;
		foreach (var connection in connections) switch (connection.State) {
			case ConnectionState.State.Connecting:
				break;
			case ConnectionState.State.Handshake:
				break;
			case ConnectionState.State.Approval:
				var approvalEntity = buffer.CreateEntity();
				buffer.AddComponent(approvalEntity, new SendRpcCommandRequest());
				buffer.AddComponent(approvalEntity, new RequestApproval { payload = "0000" });
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

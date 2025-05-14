using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
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
	using UnityEditor.Compilation;
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
	Ready,
	RelayAllocating,
	SceneLoading,
	Connecting,
	ConnectionSucceeded,
	ConnectionFailed,
	Connected,
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/Network Manager")]
public class NetworkManager : MonoSingleton<NetworkManager> {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(NetworkManager))]
		class NetworkManagerEditor : EditorExtensions {
			NetworkManager I => target as NetworkManager;
			public override void OnInspectorGUI() {
				Begin("Network Manager");

				LabelField("Scenes", EditorStyles.boldLabel);
				LobbyScene = TextField("Lobby Scene", LobbyScene);
				StageScene = TextField("Stage Scene", StageScene);
				Space();

				LabelField("Editor", EditorStyles.boldLabel);
				BeginDisabledGroup(Application.isPlaying);
				AutoConnect = Toggle("Auto Connect", AutoConnect);
				EndDisabledGroup();
				BeginDisabledGroup(AutoConnect);
				EnableGUI = Toggle("Enable GUI", EnableGUI);
				EndDisabledGroup();
				Space();

				if (Application.isPlaying) {
					LabelField("Debug", EditorStyles.boldLabel);
					var serviceState = Regex.Replace($"{ServiceState}", "(?<=[a-z])(?=[A-Z])", " ");
					var networkState = Regex.Replace($"{NetworkState}", "(?<=[a-z])(?=[A-Z])", " ");
					LabelField("Service State", serviceState);
					LabelField("Network State", networkState);
					LabelField("Connection Entity", $"{ConnectionEntity.Count}");
					BeginDisabledGroup(true);
					foreach (var entity in ConnectionEntity) LabelField(" ", $"{entity}");
					EndDisabledGroup();
					Space();
				}
				End();
			}
		}
	#endif



	// Definitions

	public const float Tickrate = 60f;
	public const float Ticktime = 1f / Tickrate;

	const int   RelayMaxPlayer    = 5;
	const int   LocalMaxPlayer    = 5;
	const float ConnectionTimeOut = 8f;

	class RelayDriverConstructor : INetworkStreamDriverConstructor {
		RelayServerData serverData;
		RelayServerData clientData;

		public RelayDriverConstructor(RelayServerData serverData, RelayServerData clientData) {
			this.serverData = serverData;
			this.clientData = clientData;
		}
		public void CreateServerDriver(World world, ref NetworkDriverStore driver, NetDebug debug)
			=> DefaultDriverBuilder.RegisterServerDriver(world, ref driver, debug, ref serverData);
		public void CreateClientDriver(World world, ref NetworkDriverStore driver, NetDebug debug)
			=> DefaultDriverBuilder.RegisterClientDriver(world, ref driver, debug, ref clientData);
	}



	// Fields

	[SerializeField] string m_LobbyScene  = "";
	[SerializeField] string m_StageScene  = "";
	[SerializeField] bool   m_AutoConnect = false;
	[SerializeField] bool   m_EnableGUI   = false;

	ServiceState m_ServiceState = ServiceState.Uninitialized;
	NetworkState m_NetworkState = NetworkState.Ready;
	int m_MaxPlayer;
	List<Entity> m_ConnectionEntity = new();



	// Properties

	public static string LobbyScene {
		get => Instance.m_LobbyScene;
		set => Instance.m_LobbyScene = value;
	}
	public static string StageScene {
		get => Instance.m_StageScene;
		set => Instance.m_StageScene = value;
	}

	public static bool AutoConnect {
		get => Instance.m_AutoConnect;
		set {
			var flag = AutoConnect != value;
			Instance.m_AutoConnect  = value;
			#if UNITY_EDITOR
				if (flag) CompilationPipeline.RequestScriptCompilation();
			#endif
		}
	}
	static bool EnableGUI {
		get => Instance.m_EnableGUI;
		set => Instance.m_EnableGUI = value;
	}

	public static ServiceState ServiceState {
		get         => Instance.m_ServiceState;
		private set => Instance.m_ServiceState = value;
	}
	public static NetworkState NetworkState {
		get         => Instance.m_NetworkState;
		private set => Instance.m_NetworkState = value;
	}
	public static int MaxPlayer {
		get         => Instance.m_MaxPlayer;
		private set => Instance.m_MaxPlayer = value;
	}

	public static List<Entity> ConnectionEntity {
		get => Instance.m_ConnectionEntity;
		set => Instance.m_ConnectionEntity = value;
	}



	// GUI Methods

	bool   guiUseRelay = false;
	string guiJoinCode = "";
	string guiAddress  = "127.0.0.1";
	ushort guiPort     = 7979;

	void OnGUI() {
		if (AutoConnect || !EnableGUI) return;
		var rect0 = new Rect(10, 50, 160, 40);
		var rect1 = new Rect(10, 90,  80, 40);
		var style = new GUIStyle(GUI.skin.textField) { alignment = TextAnchor.MiddleCenter };

		switch (m_NetworkState) {
			case NetworkState.Ready:
				var text = guiUseRelay ? "Relay" : "Local";
				guiUseRelay = GUI.Toggle(new Rect(10, 10, 200, 40), guiUseRelay, $" {text}");
				if (guiUseRelay) {
					if (GUI.Button(rect0, "Create Host")) CreateRelayHost(RelayMaxPlayer);
					if (GUI.Button(rect1, "Join"       )) JoinRelayServer(guiJoinCode);
					guiJoinCode = GUI.TextField(new Rect(90, 90, 80, 40), guiJoinCode, 6, style);
				}
				else {
					if (GUI.Button(rect0, "Create Host")) CreateLocalHost(LocalMaxPlayer, guiPort);
					if (GUI.Button(rect1, "Join"       )) JoinLocalServer(guiAddress, guiPort);
					string strPort = guiPort.ToString();
					strPort    = GUI.TextField(new Rect(170, 50,  60, 40), strPort,     4, style);
					guiAddress = GUI.TextField(new Rect( 90, 90, 140, 40), guiAddress, 15, style);
					guiPort    = ushort.Parse(strPort);
				}
				break;

			case NetworkState.RelayAllocating:
				GUI.Label(new Rect(10, 90, 160, 40), "Allocating...", style);
				break;

			case NetworkState.SceneLoading:
				GUI.Label(new Rect(10, 90, 160, 40), "Scene Loading...", style);
				break;

			case NetworkState.Connecting:
				GUI.Label(new Rect(10, 90, 160, 40), "Connecting...", style);
				break;

			case NetworkState.Connected:
				if (GUI.Button(rect0, "Leave")) Leave();
				var info = guiUseRelay ? guiJoinCode : $"{guiAddress}:{guiPort}";
				GUI.Label(new Rect(10, 90, 160, 40), info, style);
				break;

			case NetworkState.ConnectionFailed:
				if (GUI.Button(rect0, "Leave")) Leave();
				GUI.Label(new Rect(10, 90, 160, 40), "Failed", style);
				break;
		}
	}



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

	public static void CreateRelayHost(int maxPlayer = RelayMaxPlayer) {
		_ = CreateRelayHostAsync(maxPlayer);
	}
	public static async Task CreateRelayHostAsync(int maxPlayer = RelayMaxPlayer) {
		if (ServiceState == ServiceState.Uninitialized) await InitializeAsync();
		if (ServiceState == ServiceState.Unsigned     ) await SignInAnonymouslyAsync();
		if (ServiceState == ServiceState.Ready && NetworkState == NetworkState.Ready) {

			NetworkState = NetworkState.RelayAllocating;
			MaxPlayer = Mathf.Max(maxPlayer, RelayMaxPlayer);
			RelayServerData relayServerData;
			RelayServerData relayClientData;
			try {
				var data     = await RelayService.Instance.CreateAllocationAsync(MaxPlayer);
				var joinCode = await RelayService.Instance.GetJoinCodeAsync(data.AllocationId);
				var joinData = await RelayService.Instance.JoinAllocationAsync(joinCode);
				GUIUtility.systemCopyBuffer = Instance.guiJoinCode = joinCode;
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
			World.DefaultGameObjectInjectionWorld = client;
			await SceneManager.LoadSceneAsync(StageScene);
			ConnectionEntity.Clear();

			NetworkState = NetworkState.Connecting;
			Listen (server, NetworkEndpoint.AnyIpv4 );
			Connect(client, relayClientData.Endpoint);
			var timer = ConnectionTimeOut;
			while (NetworkState == NetworkState.Connecting) {
				await Task.Yield();
				if ((timer -= Time.deltaTime) <= 0f) {
					NetworkState = NetworkState.ConnectionFailed;
					return;
				};
			}
			NetworkState = NetworkState.Connected;
		}
	}

	public static void JoinRelayServer(string joinCode) {
		_ = JoinRelayServerAsync(joinCode);
	}
	public static async Task JoinRelayServerAsync(string joinCode) {
		if (ServiceState == ServiceState.Uninitialized) await InitializeAsync();
		if (ServiceState == ServiceState.Unsigned     ) await SignInAnonymouslyAsync();
		if (ServiceState == ServiceState.Ready && NetworkState == NetworkState.Ready) {

			NetworkState = NetworkState.RelayAllocating;
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
			World.DefaultGameObjectInjectionWorld = client;
			await SceneManager.LoadSceneAsync(StageScene);
			ConnectionEntity.Clear();

			NetworkState = NetworkState.Connecting;
			Connect(client, relayClientData.Endpoint);
			float timer = ConnectionTimeOut;
			while (NetworkState == NetworkState.Connecting) {
				await Task.Yield();
				if ((timer -= Time.deltaTime) <= 0f) {
					NetworkState = NetworkState.ConnectionFailed;
					return;
				};
			}
			NetworkState = NetworkState.Connected;
		}
	}



	// Local Connection Methods

	public static void CreateLocalHost(ushort port = 7979, int maxPlayer = LocalMaxPlayer) {
		_ = CreateLocalHostAsync(port, maxPlayer);
	}
	public static async Task CreateLocalHostAsync(ushort port = 7979, int maxPlayer = LocalMaxPlayer) {
		if (NetworkState == NetworkState.Ready) {

			NetworkState = NetworkState.SceneLoading;
			MaxPlayer = Mathf.Max(maxPlayer, LocalMaxPlayer);
			var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
			var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
			DestroyLocalSimulationWorld();
			World.DefaultGameObjectInjectionWorld = client;
			await SceneManager.LoadSceneAsync(StageScene);
			ConnectionEntity.Clear();

			NetworkState = NetworkState.Connecting;
			Listen (server, NetworkEndpoint.AnyIpv4     .WithPort(port));
			Connect(client, NetworkEndpoint.LoopbackIpv4.WithPort(port));
			var timer = ConnectionTimeOut;
			while (NetworkState == NetworkState.Connecting) {
				await Task.Yield();
				if ((timer -= Time.deltaTime) <= 0f) {
					NetworkState = NetworkState.ConnectionFailed;
					return;
				};
			}
			NetworkState = NetworkState.Connected;
		}
	}

	public static void JoinLocalServer(string address = "127.0.0.1", ushort port = 7979) {
		_ = JoinLocalServerAsync(address, port);
	}
	public static async Task JoinLocalServerAsync(string address = "127.0.0.1", ushort port = 7979) {
		if (NetworkState == NetworkState.Ready) {

			NetworkState = NetworkState.SceneLoading;
			var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
			DestroyLocalSimulationWorld();
			World.DefaultGameObjectInjectionWorld = client;
			await SceneManager.LoadSceneAsync(StageScene);
			ConnectionEntity.Clear();

			NetworkState = NetworkState.Connecting;
			Connect(client, NetworkEndpoint.Parse(address, port));
			var timer = ConnectionTimeOut;
			while (NetworkState == NetworkState.Connecting) {
				await Task.Yield();
				if ((timer -= Time.deltaTime) <= 0f) {
					NetworkState = NetworkState.ConnectionFailed;
					return;
				};
			}
			NetworkState = NetworkState.Connected;
		}
	}



	// Disconnection Methods

	public static async void Leave() {
		await LeaveAsync();
	}
	public static async Task LeaveAsync() {
		if (NetworkState == NetworkState.Connected || NetworkState == NetworkState.ConnectionFailed) {

			NetworkState = NetworkState.SceneLoading;
			var world = ClientServerBootstrap.CreateLocalWorld("DefaultWorld");
			DestroyServerClientSimulationWorld();
			World.DefaultGameObjectInjectionWorld = world;
			await SceneManager.LoadSceneAsync(LobbyScene);
			ConnectionEntity.Clear();

			NetworkState = NetworkState.Ready;
		}
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
		foreach (var world in World.All) if (world.Flags == WorldFlags.Game) {
			world.Dispose();
			break;
		}
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
// Auto Connect Bootstrap
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

#if UNITY_EDITOR
	[UnityEngine.Scripting.Preserve]
	public class AutoConnectBootstrap : ClientServerBootstrap {
		public override bool Initialize(string defaultWorldName) {
			if (NetworkManager.AutoConnect) {
				AutoConnectPort = 7979;
				return base.Initialize(defaultWorldName);
			}
			return false;
		}
	}
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager Server System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct RequestApproval : IApprovalRpcCommand {
    public FixedString512Bytes payload;
}



[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class NetworkManagerServerSystem : SystemBase {
	BeginInitializationEntityCommandBufferSystem system;
	EntityQuery queryApproved;

	[BurstCompile]
	protected override void OnCreate() {
		system = World.GetOrCreateSystemManaged<BeginInitializationEntityCommandBufferSystem>();
		queryApproved = GetEntityQuery(ComponentType.ReadOnly<ConnectionApproved>());
		RequireForUpdate<NetworkStreamDriver>();
		RequireForUpdate<PrefabContainer>();
	}

	[BurstDiscard]
	protected override void OnUpdate() {
		var buffer = system.CreateCommandBuffer();

		var connections = SystemAPI.GetSingleton<NetworkStreamDriver>().ConnectionEventsForTick;
		foreach (var connection in connections) switch (connection.State) {
			case ConnectionState.State.Handshake:
				break;

			case ConnectionState.State.Approval:
				break;

			case ConnectionState.State.Connected:
				var connectionEntity = connection.ConnectionEntity;
				NetworkManager.ConnectionEntity.Add(connectionEntity);
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
				NetworkManager.ConnectionEntity.Remove(connection.ConnectionEntity);
				break;
		}

		var numApproved = queryApproved.CalculateEntityCount();
		foreach (var (rpc, approval, entity) in SystemAPI
			.Query<RefRO<ReceiveRpcCommandRequest>, RefRW<RequestApproval>>()
			.WithEntityAccess()) {

            var connectionEntity = rpc.ValueRO.SourceConnection;
			var match = true;
			match &= approval.ValueRO.payload.Equals("0000");
			match &= numApproved < NetworkManager.MaxPlayer;
			if (match) numApproved++;
			if (match) buffer.AddComponent(connectionEntity, new ConnectionApproved());
			else       buffer.AddComponent(connectionEntity, new NetworkStreamRequestDisconnect());
			buffer.DestroyEntity(entity); 
        }
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager Client System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class NetworkManagerClientSystem : SystemBase {
	EndInitializationEntityCommandBufferSystem system;

	[BurstCompile]
	protected override void OnCreate() {
		system = World.GetOrCreateSystemManaged<EndInitializationEntityCommandBufferSystem>();
		RequireForUpdate<NetworkStreamDriver>();
	}

	[BurstDiscard]
	protected override void OnUpdate() {
		var buffer = system.CreateCommandBuffer();

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

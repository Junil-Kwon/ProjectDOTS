using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

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



// Network States

public enum Authentication : byte {
	Uninitialized,
	Initializing,
	Unsigned,
	Signing,
	Signed,
}

public enum Connection : byte {
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

				LabelField("Debug Mode", EditorStyles.boldLabel);
				BeginDisabledGroup(Application.isPlaying);
				AutoConnect = Toggle("Auto Connect", AutoConnect);
				EndDisabledGroup();
				BeginDisabledGroup(AutoConnect);
				EnableGUI = Toggle("Enable GUI", EnableGUI);
				EndDisabledGroup();
				Space();

				if (Application.isPlaying) {
					LabelField("Network States", EditorStyles.boldLabel);
					LabelField("Authentication",    $"{Authentication}");
					LabelField("Connection",        $"{Connection}");
					LabelField("Connection Entity", $"{ConnectionEntity.Count}");
					BeginDisabledGroup(true);
					foreach (var entity in ConnectionEntity) {
						LabelField($"{entity}", $"{entity}");
					}
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

	public const int RelayMaxPlayers = 4;
	public const int LocalMaxPlayers = 8;

	const float ConnectionTimeOut = 3f;



	// Fields

	[SerializeField] string m_LobbyScene;
	[SerializeField] string m_StageScene;

	[SerializeField] bool m_AutoConnect = false;
	[SerializeField] bool m_EnableGUI   = false;

	int m_MaxPlayers = 4;

	Authentication m_Authentication   = Authentication.Uninitialized;
	Connection     m_Connection       = Connection.Ready;
	List<Entity>   m_ConnectionEntity = new();



	// Properties

	static string LobbyScene {
		get => Instance.m_LobbyScene;
		set => Instance.m_LobbyScene = value;
	}
	static string StageScene {
		get => Instance.m_StageScene;
		set => Instance.m_StageScene = value;
	}

	public static bool AutoConnect {
		get => Instance.m_AutoConnect;
		set => Instance.m_AutoConnect = value;
	}
	public static bool EnableGUI {
		get => Instance.m_EnableGUI;
		set => Instance.m_EnableGUI = value;
	}

	public static int MaxPlayers {
		get => Instance.m_MaxPlayers;
		set => Instance.m_MaxPlayers = value;
	}

	public static Authentication Authentication {
		get         => Instance.m_Authentication;
		private set => Instance.m_Authentication = value;
	}
	public static Connection Connection {
		get         => Instance.m_Connection;
		private set => Instance.m_Connection = value;
	}
	public static List<Entity> ConnectionEntity {
		get => Instance.m_ConnectionEntity;
		set => Instance.m_ConnectionEntity = value;
	}



	// Debug Mode Methods

	bool   guiUseRelay = false;
	string guiJoinCode = "";
	string guiAddress  = "127.0.0.1";
	ushort guiPort     = 7979;

	void OnGUI() {
		if (AutoConnect || !EnableGUI) return;
		var rect0 = new Rect(10, 50, 160, 40);
		var rect1 = new Rect(10, 90,  80, 40);
		var style = new GUIStyle(GUI.skin.textField) { alignment = TextAnchor.MiddleCenter };

		switch (m_Connection) {
			case Connection.Ready:
				var text = guiUseRelay ? "Relay" : "Local";
				guiUseRelay = GUI.Toggle(new Rect(10, 10, 200, 40), guiUseRelay, $" {text}");
				if (guiUseRelay) {
					if (GUI.Button(rect0, "Create Host")) CreateRelayHost(RelayMaxPlayers);
					if (GUI.Button(rect1, "Join"       )) JoinRelayServer(guiJoinCode);
					guiJoinCode = GUI.TextField(new Rect(90, 90, 80, 40), guiJoinCode, 6, style);
				}
				else {
					if (GUI.Button(rect0, "Create Host")) CreateLocalHost(LocalMaxPlayers, guiPort);
					if (GUI.Button(rect1, "Join"       )) JoinLocalServer(guiAddress, guiPort);
					string strPort = guiPort.ToString();
					strPort    = GUI.TextField(new Rect(170, 50,  60, 40), strPort,     4, style);
					guiAddress = GUI.TextField(new Rect( 90, 90, 140, 40), guiAddress, 15, style);
					guiPort    = ushort.Parse(strPort);
				}
				break;

			case Connection.RelayAllocating:
				GUI.Label(new Rect(10, 90, 160, 40), "Allocating...", style);
				break;

			case Connection.SceneLoading:
				GUI.Label(new Rect(10, 90, 160, 40), "Scene Loading...", style);
				break;

			case Connection.Connecting:
				GUI.Label(new Rect(10, 90, 160, 40), "Connecting...", style);
				break;

			case Connection.Connected:
				if (GUI.Button(rect0, "Leave")) Leave();
				var info = guiUseRelay ? guiJoinCode : $"{guiAddress}:{guiPort}";
				GUI.Label(new Rect(10, 90, 160, 40), info, style);
				break;

			case Connection.ConnectionFailed:
				if (GUI.Button(rect0, "Leave")) Leave();
				GUI.Label(new Rect(10, 90, 160, 40), "Failed", style);
				break;
		}
	}



	// Lifecycle

	void Start() {
		if (Instance == this) {
			Authentication = Authentication.Uninitialized;
			Connection     = Connection.Ready;
			if (!AutoConnect) Initialize();
		}
	}



	// Authentication Methods

	public static async void Initialize() {
		await InitializeAsync();
	}
	public static async Task InitializeAsync() {
		if (Authentication == Authentication.Uninitialized) {
			Authentication  = Authentication.Initializing;
			var task = UnityServices.InitializeAsync();
			await task;
			if (task.IsFaulted) {
				Authentication = Authentication.Uninitialized;
				throw task.Exception;
			}
			Authentication = Authentication.Unsigned;
			await SignInAsync();
		}
	}

	public static async void SignIn() {
		await SignInAsync();
	}
	public static async Task SignInAsync() {
		if (Authentication == Authentication.Unsigned) {
			Authentication  = Authentication.Signing;
			var task = AuthenticationService.Instance.SignInAnonymouslyAsync();
			await task;
			if (task.IsFaulted) {
				Authentication = Authentication.Unsigned;
				throw task.Exception;
			}
			Authentication = Authentication.Signed;
		}
	}



	// Connection Methods

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

	class RelayDriverConstructor : INetworkStreamDriverConstructor {
		RelayServerData clientData;
		RelayServerData serverData;

		public RelayDriverConstructor(RelayServerData serverData, RelayServerData clientData) {
			this.serverData = serverData;
			this.clientData = clientData;
		}
		public void CreateClientDriver(World world, ref NetworkDriverStore driver, NetDebug debug) {
			DefaultDriverBuilder.RegisterClientDriver(world, ref driver, debug, ref clientData);
		}
		public void CreateServerDriver(World world, ref NetworkDriverStore driver, NetDebug debug) {
			DefaultDriverBuilder.RegisterServerDriver(world, ref driver, debug, ref serverData);
		}
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
		var driver = query.GetSingletonRW<NetworkStreamDriver>();
		if (!AutoConnect) driver.ValueRW.RequireConnectionApproval = true;
		driver.ValueRW.Listen(endpoint);
	}

	static void Connect(World client, NetworkEndpoint endpoint) {
		using var query = client.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver));
		var driver = query.GetSingletonRW<NetworkStreamDriver>();
		if (!AutoConnect) driver.ValueRW.RequireConnectionApproval = true;
		driver.ValueRW.Connect(client.EntityManager, endpoint);
	}

	public static void OnConnectionSucceeded() => Connection = Connection.ConnectionSucceeded;
	public static void OnConnectionFailed   () => Connection = Connection.ConnectionFailed;



	// Relay Connection Methods

	public static async void CreateRelayHost(int maxPlayers) {
		await CreateRelayHostAsync(maxPlayers);
	}

	public static async Task CreateRelayHostAsync(int maxPlayers) {
		if (Authentication == Authentication.Uninitialized) await InitializeAsync();
		if (Authentication == Authentication.Unsigned     ) await SignInAsync();
		if (Authentication == Authentication.Signed       && Connection == Connection.Ready) {

			Connection = Connection.RelayAllocating;
			MaxPlayers = Mathf.Max(maxPlayers, RelayMaxPlayers);
			RelayServerData relayServerData;
			RelayServerData relayClientData;
			try {
				var data     = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);
				var joinCode = await RelayService.Instance.GetJoinCodeAsync(data.AllocationId);
				var joinData = await RelayService.Instance.JoinAllocationAsync(joinCode);
				GUIUtility.systemCopyBuffer = Instance.guiJoinCode = joinCode;
				relayServerData = GetRelayServerData(data);
				relayClientData = GetRelayClientData(joinData);
			}
			catch (RelayServiceException) { Connection = Connection.ConnectionFailed; return; }
			catch (ArgumentNullException) { Connection = Connection.ConnectionFailed; return; }

			Connection = Connection.SceneLoading;
			var prevConstructor = NetworkStreamReceiveSystem.DriverConstructor;
			var nextConstructor = new RelayDriverConstructor(relayServerData, relayClientData);
			NetworkStreamReceiveSystem.DriverConstructor = nextConstructor;
			var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
			var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
			NetworkStreamReceiveSystem.DriverConstructor = prevConstructor;
			DestroyLocalSimulationWorld();
			World.DefaultGameObjectInjectionWorld ??= server;
			await SceneManager.LoadSceneAsync(StageScene);

			Connection = Connection.Connecting;
			Listen (server, NetworkEndpoint.AnyIpv4 );
			Connect(client, relayClientData.Endpoint);
			float timer = ConnectionTimeOut;
			while (Connection == Connection.Connecting) {
				if ((timer -= Time.deltaTime) <= 0f) {
					Connection = Connection.ConnectionFailed;
					return;
				};
				await Task.Yield();
			}

			Connection = Connection.Connected;
		}
	}

	public static async void JoinRelayServer(string joinCode) {
		await Instance.JoinRelayServerAsync(joinCode);
	}

	public async Task JoinRelayServerAsync(string joinCode) {
		if (Authentication == Authentication.Uninitialized) await InitializeAsync();
		if (Authentication == Authentication.Unsigned     ) await SignInAsync();
		if (Authentication == Authentication.Signed       && Connection == Connection.Ready) {

			Connection = Connection.RelayAllocating;
			RelayServerData relayClientData;
			try {
				var joinData = await RelayService.Instance.JoinAllocationAsync(joinCode);
				relayClientData = GetRelayClientData(joinData);
			}
			catch (RelayServiceException) { Connection = Connection.ConnectionFailed; return; }
			catch (ArgumentNullException) { Connection = Connection.ConnectionFailed; return; }

			Connection = Connection.SceneLoading;
			var prevConstructor = NetworkStreamReceiveSystem.DriverConstructor;
			var nextConstructor = new RelayDriverConstructor(default, relayClientData);
			NetworkStreamReceiveSystem.DriverConstructor = nextConstructor;
			var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
			NetworkStreamReceiveSystem.DriverConstructor = prevConstructor;
			DestroyLocalSimulationWorld();
			World.DefaultGameObjectInjectionWorld ??= client;
			await SceneManager.LoadSceneAsync(StageScene);

			Connection = Connection.Connecting;
			Connect(client, relayClientData.Endpoint);
			float timer = ConnectionTimeOut;
			while (Connection == Connection.Connecting) {
				if ((timer -= Time.deltaTime) <= 0f) {
					Connection = Connection.ConnectionFailed;
					return;
				};
				await Task.Yield();
			}

			Connection = Connection.Connected;
		}
	}



	// Local Connection Methods

	public static async void CreateLocalHost(int maxPlayers, ushort port) {
		await Instance.CreateLocalHostAsync(maxPlayers, port);
	}

	public async Task CreateLocalHostAsync(int maxPlayers, ushort port) {
		if (Connection == Connection.Ready) {

			Connection = Connection.SceneLoading;
			MaxPlayers = Mathf.Max(maxPlayers, LocalMaxPlayers);
			var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
			var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
			DestroyLocalSimulationWorld();
			World.DefaultGameObjectInjectionWorld ??= server;
			await SceneManager.LoadSceneAsync(StageScene);

			Connection = Connection.Connecting;
			Listen (server, NetworkEndpoint.AnyIpv4     .WithPort(port));
			Connect(client, NetworkEndpoint.LoopbackIpv4.WithPort(port));
			float timer = ConnectionTimeOut;
			while (Connection == Connection.Connecting) {
				if ((timer -= Time.deltaTime) <= 0f) {
					Connection = Connection.ConnectionFailed;
					return;
				};
				await Task.Yield();
			}

			Connection = Connection.Connected;
		}
	}

	public static async void JoinLocalServer(string address, ushort port) {
		await JoinLocalServerAsync(address, port);
	}

	public static async Task JoinLocalServerAsync(string address, ushort port) {
		if (Connection == Connection.Ready) {

			Connection = Connection.SceneLoading;
			var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
			DestroyLocalSimulationWorld();
			World.DefaultGameObjectInjectionWorld ??= client;
			await SceneManager.LoadSceneAsync(StageScene);

			Connection = Connection.Connecting;
			Connect(client, NetworkEndpoint.Parse(address, port));
			float timer = ConnectionTimeOut;
			while (Connection == Connection.Connecting) {
				if ((timer -= Time.deltaTime) <= 0f) {
					Connection = Connection.ConnectionFailed;
					return;
				};
				await Task.Yield();
			}

			Connection = Connection.Connected;
		}
	}



	// Disconnection Methods

	public static async void Leave() {
		await LeaveAsync();
	}

	public static async Task LeaveAsync() {
		if (Connection == Connection.Connected || Connection == Connection.ConnectionFailed) {

			Connection = Connection.SceneLoading;
			ClientServerBootstrap.CreateLocalWorld("DefaultWorld");
			DestroyServerClientSimulationWorld();
			await SceneManager.LoadSceneAsync(LobbyScene);

			Connection = Connection.Ready;
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Auto Connect Bootstrap
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UnityEngine.Scripting.Preserve]
public class AutoConnectBootstrap : ClientServerBootstrap {
	public override bool Initialize(string defaultWorldName) {
		var local = false;
		#if UNITY_EDITOR
			local = NetworkManager.AutoConnect;
		#endif
		if (local) {
			AutoConnectPort = 7979;
			return base.Initialize(defaultWorldName);
		}
		else {
			CreateLocalWorld(defaultWorldName);
			return true;
		}
	}
}



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
		var numApproved = queryApproved.CalculateEntityCount();
		foreach (var (rpc, approval, entity) in SystemAPI
			.Query<RefRO<ReceiveRpcCommandRequest>, RefRW<RequestApproval>>()
			.WithEntityAccess()) {

            var connectionEntity = rpc.ValueRO.SourceConnection;
			var match = true;
			match &= approval.ValueRO.payload.Equals("ABC");
			match &= numApproved < NetworkManager.MaxPlayers;
			if (match) numApproved++;
			if (match) buffer.AddComponent(connectionEntity, new ConnectionApproved());
			else       buffer.AddComponent(connectionEntity, new NetworkStreamRequestDisconnect());
			buffer.DestroyEntity(entity); 
        }

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

				// Temp Player Spawn Code
				var prefabContainer = SystemAPI.GetSingletonBuffer<PrefabContainer>(true);
				var player = buffer.Instantiate(prefabContainer[(int)Prefab.Player].Prefab);
				var position = new float3(0, 0, 0);
				var networkId = SystemAPI.GetComponent<NetworkId>(connectionEntity).Value;
				position.x = UnityEngine.Random.Range(-2f, 2f);
				position.z = UnityEngine.Random.Range(-2f, 2f);
				buffer.SetComponent(player, LocalTransform.FromPosition(position));
				buffer.SetComponent(player, new GhostOwner { NetworkId = networkId });
				buffer.AppendToBuffer(connectionEntity, new LinkedEntityGroup { Value = player });
				break;

			case ConnectionState.State.Disconnected:
				NetworkManager.ConnectionEntity.Remove(connection.ConnectionEntity);
				break;
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager Client System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class NetworkManagerClientSystem : SystemBase {
	EndInitializationEntityCommandBufferSystem bufferSystem;

	[BurstCompile]
	protected override void OnCreate() {
		bufferSystem = World.GetOrCreateSystemManaged<EndInitializationEntityCommandBufferSystem>();
		RequireForUpdate<NetworkStreamDriver>();
	}

	[BurstDiscard]
	protected override void OnUpdate() {
		var buffer = bufferSystem.CreateCommandBuffer();

		var connections = SystemAPI.GetSingleton<NetworkStreamDriver>().ConnectionEventsForTick;
        foreach (var connection in connections) switch (connection.State) {
			case ConnectionState.State.Connecting:
				break;

			case ConnectionState.State.Handshake:
				break;

			case ConnectionState.State.Approval:
				var approvalEntity = buffer.CreateEntity();
				buffer.AddComponent(approvalEntity, new SendRpcCommandRequest());
				buffer.AddComponent(approvalEntity, new RequestApproval { payload = "ABC" });
				break;

			case ConnectionState.State.Connected:
				var connectionEntity = connection.ConnectionEntity;
				buffer.AddComponent(connectionEntity, new NetworkStreamInGame());
				NetworkManager.OnConnectionSucceeded();
				break;

			case ConnectionState.State.Disconnected:
				NetworkManager.OnConnectionFailed();
				break;
		}
	}
}

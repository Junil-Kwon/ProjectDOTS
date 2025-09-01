using UnityEngine;
using System.Collections.Generic;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.NetCode;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
#endif



public enum GameState {
	Gameplay,
	Cutscene,
	Paused,
}

public struct PlayerData {
	public CharacterCoreBlobData CoreBlob;
	public CharacterCoreData CoreData;
	public CharacterStatusBlobData StatusBlob;
	public CharacterStatusData StatusData;
	public CharacterEffectBlobData EffectBlob;
	public FixedList128Bytes<CharacterEffectData> EffectData;
	public LocalTransform Transform;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Singleton Manager/Game Manager")]
public sealed class GameManager : MonoSingleton<GameManager> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(GameManager))]
	class GameManagerEditor : EditorExtensions {
		GameManager I => target as GameManager;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Bootstrap", EditorStyles.boldLabel);
			UseDebugMode = Toggle("Use Debug Mode", UseDebugMode);
			Space();

			LabelField("Event Instance", EditorStyles.boldLabel);
			LabelField("Event Template", "None (Event Graph SO)");
			int num = EventInstance.Count;
			int den = 0;
			LabelField("Event Pool", $"{num} / {den}");
			Space();

			LabelField("Game Status", EditorStyles.boldLabel);
			LabelField("Game State", $"{GameState}");
			LabelField("Time Scale", $"{TimeScale:F2}");
			Space();

			End();
		}
	}
	#endif



	// Constants

	const string MaxFrameRateK = "MaxFrameRate";
	const float MaxFrameRateD = 60f;



	// Fields

	[SerializeField] bool m_UseDebugMode;
	GameState m_GameState = GameState.Paused;
	float? m_MaxFrameRate;

	PlayerData? m_LocalPlayer;
	List<PlayerData> m_RemotePlayers = new();
	int m_Ghosts;

	Dictionary<uint, byte> m_EventInstance = new();
	List<(uint, EventBase, float)> m_EventList = new();
	List<EventBase> m_EventBuffer = new();
	uint m_NextID;



	// Properties

	static bool UseDebugMode {
		get => Instance.m_UseDebugMode;
		set {
			if (Instance.m_UseDebugMode != value) {
				Instance.m_UseDebugMode = value;
				#if UNITY_EDITOR
				CompilationPipeline.RequestScriptCompilation();
				#endif
			}
		}
	}

	public static GameState GameState {
		get => Instance.m_GameState;
		set {
			if (Instance.m_GameState != value) {
				Instance.m_GameState = value;
				InputManager.LockCursor(value != GameState.Paused);
				InputManager.SwitchActionMap(value switch {
					GameState.Gameplay => ActionMap.Player,
					GameState.Cutscene => ActionMap.UI,
					GameState.Paused   => ActionMap.UI,
					_ => default,
				});
			}
		}
	}
	public static float TimeScale {
		get => Time.timeScale;
		set => Time.timeScale = Mathf.Clamp(value, 0f, 10f);
	}

	public static float MaxFrameRate {
		get => Instance.m_MaxFrameRate ??= PlayerPrefs.GetFloat(MaxFrameRateK, MaxFrameRateD);
		set {
			value = Mathf.Clamp(value, 60f, 360f);
			PlayerPrefs.SetFloat(MaxFrameRateK, (Instance.m_MaxFrameRate = value).Value);
			if (Application.isPlaying) Application.targetFrameRate = (int)value;
		}
	}



	public static ref PlayerData? LocalPlayer {
		get => ref Instance.m_LocalPlayer;
	}
	public static List<PlayerData> RemotePlayers {
		get => Instance.m_RemotePlayers;
	}
	public static int Ghosts {
		get => Instance.m_Ghosts;
		set => Instance.m_Ghosts = value;
	}



	static Dictionary<uint, byte> EventInstance {
		get => Instance.m_EventInstance;
	}
	static List<(uint, EventBase, float)> EventList {
		get => Instance.m_EventList;
	}
	static List<EventBase> EventBuffer {
		get => Instance.m_EventBuffer;
	}
	static uint NextID {
		get => Instance.m_NextID;
		set => Instance.m_NextID = value;
	}



	// Instance Methods

	static uint AddInstance(EventBase eventBase) {
		if (eventBase == null) return default;
		while (++NextID == default || EventInstance.ContainsKey(NextID));
		EventInstance.Add(NextID, 1);
		EventList.Add((NextID, eventBase, 0f));
		return NextID;
	}

	static void RemoveInstances(uint id) {
		byte numEvents = EventInstance[id];
		EventInstance.Remove(id);
		for (int i = EventList.Count; 0 < i--;) {
			var (eventID, eventBase, startTime) = EventList[i];
			if (eventID == id) {
				EventList.RemoveAt(i);
				if (--numEvents == 0) break;
			}
		}
	}

	static void UpdateInstances() {
		if (GameState == GameState.Paused) return;
		int i = 0;
		while (i < EventList.Count) {
			var (eventID, eventBase, startTime) = EventList[i];
			if (startTime == 0f) {
				eventBase.Start();
				EventList[i] = (eventID, eventBase, Time.time);
				continue;
			}
			if (eventBase.Update() == false) {
				i++;
				continue;
			}
			eventBase.End();
			eventBase.GetNexts(EventBuffer);
			int numNexts = EventBuffer.Count;
			if (numNexts == 0) {
				if (--EventInstance[eventID] == 0) EventInstance.Remove(eventID);
				EventList.RemoveAt(i);
			} else {
				if (1 < numNexts) EventInstance[eventID] += (byte)(numNexts - 1);
				EventList[i] = (eventID, EventBuffer[0], 0f);
				for (int j = 1; j < numNexts; j++) EventList.Add((eventID, EventBuffer[j], 0f));
				EventBuffer.Clear();
			}
		}
	}



	// Event Methods

	public static uint PlayEvent(EventGraphSO eventGraph) {
		uint eventID = AddInstance(eventGraph?.Entry);
		return eventID;
	}

	public static bool IsEventPlaying(uint eventID = default) {
		return eventID == default ? 0 < EventInstance.Count : EventInstance.ContainsKey(eventID);
	}

	public static void StopEvent(uint eventID) {
		if (EventInstance.ContainsKey(eventID)) RemoveInstances(eventID);
	}



	// Methods

	public static void QuitGame() {
		#if UNITY_EDITOR
		EditorApplication.isPlaying = false;
		#else
		Application.Quit();
		#endif
	}



	// Lifecycle

	void Start() {
		//MaxFrameRate = MaxFrameRate;
		bool useDebugMode = false;
		#if UNITY_EDITOR
		useDebugMode = UseDebugMode;
		#endif
		if (useDebugMode) {
			UIManager.OpenScreen(Screen.Game);
			UIManager.OpenScreen(Screen.Debug);
		} else {
			UIManager.OpenScreen(Screen.MainMenu);
			NetworkManager.Disconnect();
		}
	}

	#if UNITY_EDITOR
	[UnityEngine.Scripting.Preserve]
	class AutoConnectBootstrap : ClientServerBootstrap {
		public override bool Initialize(string defaultWorldName) {
			if (UseDebugMode) {
				AutoConnectPort = 7979;
				return base.Initialize(defaultWorldName);
			}
			return false;
		}
	}
	#endif

	void LateUpdate() {
		UpdateInstances();
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game Manager System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(DOTSClientSimulationSystemGroup), OrderFirst = true)]
public partial class GameManagerClientSystem : SystemBase {
	EntityQuery GhostQuery;

	protected override void OnCreate() {
		GhostQuery = GetEntityQuery(
			ComponentType.ReadOnly<GhostInstance>(),
			ComponentType.ReadOnly<LocalTransform>());
	}

	protected override void OnUpdate() {
		GameManager.LocalPlayer = null;
		GameManager.RemotePlayers.Clear();
		GameManager.Ghosts = GhostQuery.CalculateEntityCount();
		foreach (var (coreBlob, coreData, transform, entity) in
			SystemAPI.Query<
				RefRO<CharacterCoreBlob>,
				RefRO<CharacterCoreData>,
				RefRO<LocalTransform>
			>().WithAll<GhostOwnerIsLocal>().WithEntityAccess()) {

			var statusBlob = SystemAPI.GetComponentRO<CharacterStatusBlob>(entity);
			var statusData = SystemAPI.GetComponentRO<CharacterStatusData>(entity);
			var effectBlob = SystemAPI.GetComponentRO<CharacterEffectBlob>(entity);
			var effectData = SystemAPI.GetBuffer<CharacterEffectData>(entity);
			var status = new PlayerData {
				CoreBlob   = coreBlob.ValueRO.Value.Value,
				CoreData   = coreData.ValueRO,
				StatusBlob = statusBlob.ValueRO.Value.Value,
				StatusData = statusData.ValueRO,
				EffectBlob = effectBlob.ValueRO.Value.Value,
				EffectData = new(),
				Transform  = transform.ValueRO,
			};
			int length = math.min(status.EffectData.Capacity, effectData.Length);
			for (int i = 0; i < length; i++) {
				status.EffectData.AddNoResize(effectData[i]);
			}
			if (SystemAPI.IsComponentEnabled<GhostOwnerIsLocal>(entity)) {
				GameManager.LocalPlayer = status;
			} else GameManager.RemotePlayers.Add(status);
		}
	}
}

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

using Unity.Entities;
using Unity.NetCode;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
#endif



// ━

public enum GameState : byte {
	Gameplay,
	Cutscene,
	Paused,
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/Game Manager")]
public sealed class GameManager : MonoSingleton<GameManager> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(GameManager))]
	class GameManagerEditor : EditorExtensions {
		GameManager I => target as GameManager;
		public override void OnInspectorGUI() {
			Begin("Game Manager");

			LabelField("Setup", EditorStyles.boldLabel);
			BeginDisabledGroup(Application.isPlaying);
			GameScene = SceneField("Game Scene", GameScene);
			StartDirectly = Toggle("Start Directly", StartDirectly);
			PropertyField("m_OnDirectStarted");
			EndDisabledGroup();
			Space();
			LabelField("Debug", EditorStyles.boldLabel);
			BeginDisabledGroup();
			TextField("Game State", $"{GameState}");
			EndDisabledGroup();
			Space();

			End();
		}
	}
	#endif



	// Constants

	const string MaxFrameRateKey = "MaxFrameRate";
	const int MaxFrameRateValue = 60;



	// Fields

	int m_MaxFrameRate;

	[SerializeField] int m_GameScene;
	[SerializeField] bool m_StartDirectly;
	[SerializeField] UnityEvent m_OnDirectStarted;
	GameState m_GameState;

	readonly List<CreatureCore> m_Players = new();
	int m_NumCreatures;

	readonly List<BaseEvent> m_Temp = new();
	readonly List<BaseEvent> m_ActiveEvents = new();
	readonly List<float> m_EventElapsed = new();



	// Properties

	public static int MaxFrameRate {
		get => Instance.m_MaxFrameRate == default ?
			Instance.m_MaxFrameRate = PlayerPrefs.GetInt(MaxFrameRateKey, MaxFrameRateValue) :
			Instance.m_MaxFrameRate;
		set {
			PlayerPrefs.SetInt(MaxFrameRateKey, Instance.m_MaxFrameRate = Mathf.Max(60, value));
			if (Application.isPlaying) Application.targetFrameRate = Instance.m_MaxFrameRate;
		}
	}

	static int GameScene {
		get => Instance.m_GameScene;
		set => Instance.m_GameScene = value;
	}
	static bool StartDirectly {
		get => Instance.m_StartDirectly;
		set {
			if (Instance.m_StartDirectly != value) {
				Instance.m_StartDirectly = value;
				#if UNITY_EDITOR
					CompilationPipeline.RequestScriptCompilation();
				#endif
			}
		}
	}
	public static UnityEvent OnDirectStarted => Instance.m_OnDirectStarted;

	public static GameState GameState {
		get => Instance.m_GameState;
		set {
			if (Instance.m_GameState != value) {
				Instance.m_GameState = value;
				InputManager.SwitchActionMap(value switch {
					GameState.Gameplay => ActionMap.Player,
					GameState.Cutscene => ActionMap.UI,
					GameState.Paused   => ActionMap.UI,
					_ => default,
				}, value != GameState.Paused);
			}
		}
	}

	public static List<CreatureCore> Players => Instance.m_Players;

	public static int NumCreatures {
		get => Instance.m_NumCreatures;
		set => Instance.m_NumCreatures = value;
	}



	static List<BaseEvent> Temp => Instance.m_Temp;
	static List<BaseEvent> ActiveEvents => Instance.m_ActiveEvents;
	static List<float> EventElapsed => Instance.m_EventElapsed;



	// Methods

	public static void PlayEvent(EventGraphSO graph) {
		ActiveEvents.Add(graph.Entry);
		EventElapsed.Add(-1f);
	}

	static void SimulateEvents() {
		if (GameState == GameState.Paused) return;
		int i = 0;
		while (i < ActiveEvents.Count) {
			while (true) {
				if (ActiveEvents[i] == null) {
					ActiveEvents.RemoveAt(i);
					EventElapsed.RemoveAt(i);
					break;
				}
				if (EventElapsed[i] < 0f) {
					EventElapsed[i] = 0f;
					ActiveEvents[i].Start();
				}
				if (ActiveEvents[i].Update() == false) {
					EventElapsed[i] += Time.deltaTime;
					i++;
					break;
				} else {
					ActiveEvents[i].End();
					ActiveEvents[i].GetNext(Temp);
					if (Temp.Count == 0) ActiveEvents[i] = null;
					else {
						ActiveEvents[i] = Temp[0];
						EventElapsed[i] = -1f;
						for (int j = 1; j < Temp.Count; j++) {
							ActiveEvents.Add(Temp[j]);
							EventElapsed.Add(-1f);
						}
					}
				}
			}
		}
	}



	// Lifecycle

	void Start() {
		MaxFrameRate = MaxFrameRate;
		var startDirectly = false;
		#if UNITY_EDITOR
		startDirectly = StartDirectly;
		#endif
		if (startDirectly) OnDirectStarted.Invoke();
		else SceneManager.LoadSceneAsync(GameScene, LoadSceneMode.Single);
	}

	#if UNITY_EDITOR
	[UnityEngine.Scripting.Preserve]
	public class AutoConnectBootstrap : ClientServerBootstrap {
		public override bool Initialize(string defaultWorldName) {
			if (StartDirectly) {
				AutoConnectPort = 7979;
				return base.Initialize(defaultWorldName);
			}
			return false;
		}
	}
	#endif



	void Update() {
		SimulateEvents();
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game Manager Server System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(DOTSServerSimulationSystemGroup), OrderLast = true)]
public partial class GameManagerServerSystem : SystemBase {

	protected override void OnCreate() {
	}

	protected override void OnUpdate() {
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game Manager Client System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(DOTSClientSimulationSystemGroup), OrderLast = true)]
public partial class GameManagerClientSystem : SystemBase {

	protected override void OnCreate() {
		RequireForUpdate<PlayerHead>();
	}

	protected override void OnUpdate() {
		GameManager.Players.Clear();
		GameManager.Players.Add(default);
		foreach (var core in SystemAPI.Query<RefRO<CreatureCore>>()
			.WithAll<PlayerHead>().WithAll<GhostOwnerIsLocal>()) {
			GameManager.Players[0] = core.ValueRO;
		}
		foreach (var core in SystemAPI.Query<RefRO<CreatureCore>>()
			.WithAll<PlayerHead>().WithNone<GhostOwnerIsLocal>()) {
			GameManager.Players.Add(core.ValueRO);
		}
		var query = SystemAPI.QueryBuilder().WithAll<CreatureCore>().Build();
		GameManager.NumCreatures = query.CalculateEntityCount();
	}
}

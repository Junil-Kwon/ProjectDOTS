using UnityEngine;
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
				GameScene     = SceneField("Game Scene",     GameScene);
				StartDirectly = Toggle    ("Start Directly", StartDirectly);
				EndDisabledGroup();
				Space();
				LabelField("Debug", EditorStyles.boldLabel);
				BeginDisabledGroup();
				TextField("Game State", $"{(Application.isPlaying ? GameState : "None")}");
				EndDisabledGroup();
				Space();

				End();
			}
		}
	#endif



	// Fields

	[SerializeField] int  m_GameScene;
	[SerializeField] bool m_StartDirectly;
	GameState m_GameState;

	readonly List<CreatureCore> m_Players = new();

	readonly List<EventGraphSO> m_ActiveGraphs = new();
	readonly List<BaseEvent   > m_ActiveEvents = new();
	readonly List<float       > m_EventElapsed = new();



	// Properties

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
					if (value) CompilationPipeline.RequestScriptCompilation();
				#endif
			}
		}
	}
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



	static List<EventGraphSO> ActiveGraphs => Instance.m_ActiveGraphs;
	static List<BaseEvent   > ActiveEvents => Instance.m_ActiveEvents;
	static List<float       > EventElapsed => Instance.m_EventElapsed;



	// Methods

	public static void PlayEvent(EventGraphSO graph) {
		graph.OnEventBegin.Invoke();
		ActiveGraphs.Add(graph);
		ActiveEvents.Add(graph.Entry);
		EventElapsed.Add(-1f);
	}

	static void SimulateEvents() {
		if (GameState != GameState.Paused) {
			int i = 0;
			while (i < ActiveEvents.Count) {
				while (true) {
					if (ActiveEvents[i] == null) {
						ActiveGraphs[i].OnEventEnd.Invoke();
						ActiveGraphs.RemoveAt(i);
						ActiveEvents.RemoveAt(i);
						EventElapsed.RemoveAt(i);
						break;
					}
					if (EventElapsed[i] < 0f) {
						EventElapsed[i] = 0f;
						ActiveEvents[i].Start();
						if (ActiveEvents[i].async) {
							ActiveEvents.Add(ActiveEvents[i]);
							EventElapsed.Add(EventElapsed[i]);
						}
					}
					if (ActiveEvents[i].Update() == false) {
						EventElapsed[i] += Time.deltaTime;
						i++;
						break;
					} else {
						ActiveEvents[i].End();
						ActiveEvents[i] = ActiveEvents[i].GetNext();
						EventElapsed[i] = -1f;
					}
				}
			}
		}
	}



	// Lifecycle

	void Start() {
		var startDirectly = false;
		#if UNITY_EDITOR
			startDirectly = StartDirectly;
		#endif
		if (startDirectly == false) {
			SceneManager.LoadSceneAsync(GameScene, LoadSceneMode.Single);
		} else {
			UIManager.ShowGame();
		}
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
[UpdateInGroup(typeof(DOTSSimulationSystemGroup), OrderLast = true)]
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
[UpdateInGroup(typeof(DOTSSimulationSystemGroup), OrderLast = true)]
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
	}
}

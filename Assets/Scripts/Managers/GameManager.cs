using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;

#if UNITY_EDITOR
	using UnityEditor;
	using UnityEditor.Compilation;
#endif



// ━

public enum GameState : byte {
	None,
	Gameplay,
	Cutscene,
	Paused,
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/Game Manager")]
public class GameManager : MonoSingleton<GameManager> {

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
				var gameState = Regex.Replace($"{GameState}", "(?<=[a-z])(?=[A-Z])", " ");
				TextField("Game State", $"{gameState}");
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

	readonly List<BaseEvent> m_ActiveEvents = new();
	readonly List<float    > m_EventElapsed = new();



	// Properties

	static int GameScene {
		get => Instance.m_GameScene;
		set => Instance.m_GameScene = value;
	}
	static bool StartDirectly {
		get => Instance.m_StartDirectly;
		set {
			var flag = StartDirectly != value;
			Instance.m_StartDirectly  = value;
			#if UNITY_EDITOR
				if (flag) CompilationPipeline.RequestScriptCompilation();
			#endif
		}
	}
	public static GameState GameState {
		get => Instance.m_GameState;
		private set {
			var flag = GameState != value;
			Instance.m_GameState  = value;
			if (flag) InputManager.SwitchActionMap(value switch {
				GameState.Gameplay => ActionMap.Player,
				GameState.Cutscene => ActionMap.UI,
				GameState.Paused   => ActionMap.UI,
				_ => default,
			}, value != GameState.Paused);
		}
	}
	public static List<CreatureCore> Players => Instance.m_Players;

	static List<BaseEvent> ActiveEvents => Instance.m_ActiveEvents;
	static List<float    > EventElapsed => Instance.m_EventElapsed;



	// Methods

	public static void PlayEvent(EventGraphSO graph) {
		ActiveEvents.Add(graph.Entry);
		EventElapsed.Add(-1f);
	}

	static void SimulateEvents() {
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
					if (ActiveEvents[i].async) {
						ActiveEvents.Add(ActiveEvents[i]);
						EventElapsed.Add(EventElapsed[i]);
					}
				}
				if (ActiveEvents[i].Update() == false && EventElapsed[i] < 20f) {
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



	// Lifecycle

	void Start() {
		var startDirectly = false;
		#if UNITY_EDITOR
			startDirectly = StartDirectly;
		#endif
		if (startDirectly == false) {
			SceneManager.LoadSceneAsync(GameScene, LoadSceneMode.Single);
			GameState = GameState.Paused;
			UIManager.Initialize();
			UIManager.ShowTitle();
		} else {
			GameState = GameState.Gameplay;
			UIManager.Initialize();
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
		switch (GameState) {
			case GameState.Gameplay:
				SimulateEvents();
				if (InputManager.GetKeyUp(KeyAction.Menu) || !Application.isFocused) {
					GameState = GameState.Paused;
					UIManager.Back();
				}
				break;
			case GameState.Paused:
				if (!UIManager.IsUIActive) {
					GameState = GameState.Gameplay;
				}
				break;
		}
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

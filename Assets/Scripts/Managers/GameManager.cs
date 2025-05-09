using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// Game State

public enum GameState : byte {
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

				LabelField("Game Settings", EditorStyles.boldLabel);
				TargetFrameRate = IntSlider("Target Frame Rate", TargetFrameRate, 0, 999);
				DisplayFPS      = Toggle   ("Display FPS",       DisplayFPS);
				Space();

				LabelField("Game", EditorStyles.boldLabel);
				GameState = EnumField("Game State", GameState);
				Space();

				End();
			}
		}

		float delta = 0f;
		void OnGUI() {
			delta += (Time.unscaledDeltaTime - delta) * 0.1f;
			if (!DisplayFPS) return;
			string text = string.Format("{0:0.} FPS ({1:0.0} ms)", 1.0f / delta, delta * 1000.0f);
			GUI.Label(new Rect(20, 20, Screen.width, Screen.height), text, new GUIStyle() {
				normal = new GUIStyleState() { textColor = Color.white }, fontSize = 16,
			});
		}
	#endif



	// Fields

	[SerializeField] int  m_TargetFrameRate;
	[SerializeField] bool m_DisplayFPS;

	[SerializeField] GameState m_GameState;
	List<BaseEvent> m_ActiveEvents = new();



	// Properties

	static int TargetFrameRate {
		get => Instance.m_TargetFrameRate;
		set => Instance.m_TargetFrameRate = value;
	}
	static bool DisplayFPS {
		get => Instance.m_DisplayFPS;
		set => Instance.m_DisplayFPS = value;
	}



	public static GameState GameState {
		get => Instance.m_GameState;
		set {
			Instance.m_GameState = value;
			if (Application.isPlaying) InputManager.SwitchActionMap(value switch {
				GameState.Gameplay => ActionMap.Player,
				GameState.Paused   => ActionMap.UI,
				_ => ActionMap.None,
			});
		}
	}

	static List<BaseEvent> ActiveEvents => Instance.m_ActiveEvents;



	// Methods

	public static void PlayEvent(EventGraphSO graph) {
		if (0 < ActiveEvents.Count) return;
		ActiveEvents.Add(graph.Entry);
	}

	static void SimulateEvents() {
		if (ActiveEvents.Count == 0) return;
		//Simulate
	}



	// Lifecycle

	void Start() {
		if (0 < TargetFrameRate) Application.targetFrameRate = TargetFrameRate;
		GameState = GameState;
	}

	void Update() {
		SimulateEvents();
	}
}

using UnityEngine;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
using static EditorVisualElement;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game Manager | Set Game State
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Game Manager/Set Game State")]
public sealed class SetGameStateEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class SetGameStateEventNode : EventNodeBase {
		SetGameStateEvent I => target as SetGameStateEvent;

		public SetGameStateEventNode() : base() {
			mainContainer.style.width = Node1U;
			var cyan = new Color(180f, 0.75f, 0.60f).ToRGB();
			titleContainer.style.backgroundColor = cyan;
		}

		public override void ConstructData() {
			var gameState = EnumField(I.GameState, value => I.GameState = value);
			mainContainer.Add(gameState);
		}
	}
	#endif



	// Fields

	#if UNITY_EDITOR
	[SerializeField] string m_GameStateName;
	#endif

	[SerializeField] GameState m_GameState;



	// Properties

	#if UNITY_EDITOR
	public GameState GameState {
		get => !Enum.TryParse(m_GameStateName, out GameState gameState) ?
			Enum.Parse<GameState>(m_GameStateName = m_GameState.ToString()) :
			m_GameState = gameState;
		set => m_GameStateName = (m_GameState = value).ToString();
	}
	#else
	public GameState GameState {
		get => m_GameState;
		set => m_GameState = value;
	}
	#endif



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetGameStateEvent setGameStateEvent) {
			GameState = setGameStateEvent.GameState;
		}
	}

	public override void End() {
		GameManager.GameState = GameState;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game Manager | Set Time Scale
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Game Manager/Set Time Scale")]
public sealed class SetTimeScaleEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class SetTimeScaleEventNode : EventNodeBase {
		SetTimeScaleEvent I => target as SetTimeScaleEvent;

		public SetTimeScaleEventNode() : base() {
			mainContainer.style.width = Node1U;
			var cyan = new Color(180f, 0.75f, 0.60f).ToRGB();
			titleContainer.style.backgroundColor = cyan;
		}

		public override void ConstructData() {
			var timeScale = Slider(I.TimeScale, 0f, 10f, value => I.TimeScale = value);
			mainContainer.Add(timeScale);
		}
	}
	#endif



	// Fields

	[SerializeField] float m_TimeScale = 1f;



	// Properties

	public float TimeScale {
		get => m_TimeScale;
		set => m_TimeScale = Mathf.Clamp(value, 0f, 10f);
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetTimeScaleEvent setTimeScaleEvent) {
			TimeScale = setTimeScaleEvent.TimeScale;
		}
	}

	public override void End() {
		GameManager.TimeScale = TimeScale;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game Manager | Play Event
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Game Manager/Play Event")]
public sealed class PlayEventEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class PlayEventEventNode : EventNodeBase {
		PlayEventEvent I => target as PlayEventEvent;

		public PlayEventEventNode() : base() {
			mainContainer.style.width = Node1U;
		}

		public override void ConstructData() {
			var eventGraph = ObjectField(I.EventGraph, value => I.EventGraph = value);
			mainContainer.Add(eventGraph);
		}

		public override void ConstructPort() {
			CreatePort(Direction.Input);
			CreatePort(Direction.Output);
			CreatePort(Direction.Output, PortType.DataID);
			RefreshExpandedState();
			RefreshPorts();
		}
	}
	#endif



	// Fields

	[SerializeField] EventGraphSO m_EventGraph;

	uint m_EventID;



	// Properties

	public EventGraphSO EventGraph {
		get => m_EventGraph;
		set => m_EventGraph = value;
	}

	ref uint EventID {
		get => ref m_EventID;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is PlayEventEvent playEventEvent) {
			EventGraph = playEventEvent.EventGraph;
		}
	}

	public override void Start() {
		EventID = default;
	}

	public override void End() {
		if (EventID == default) {
			EventID = GameManager.PlayEvent(EventGraph);
		}
	}

	protected override void GetDataID(ref uint eventID) {
		End();
		if (eventID != default) eventID = EventID;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game Manager | Is Event Playing
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Game Manager/Is Event Playing")]
public sealed class IsEventPlayingEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class IsEventPlayingEventNode : EventNodeBase {
		IsEventPlayingEvent I => target as IsEventPlayingEvent;

		public IsEventPlayingEventNode() : base() {
			mainContainer.style.width = Node1U;
		}

		public override void ConstructPort() {
			CreatePort(Direction.Input);
			CreatePort(Direction.Input, PortType.DataID);
			CreatePort(Direction.Output).portName = "True";
			CreatePort(Direction.Output).portName = "False";
			CreatePort(Direction.Output, PortType.DataID);
			RefreshExpandedState();
			RefreshPorts();
		}
	}
	#endif



	// Fields

	uint m_EventID;



	// Properties

	ref uint EventID {
		get => ref m_EventID;
	}



	// Methods

	public override void Start() {
		EventID = default;
	}

	public override void GetNexts(List<EventBase> list) {
		if (EventID == default) base.GetDataID(ref EventID);
		int index = GameManager.IsEventPlaying(EventID) ? 0 : 1;
		foreach (var next in Nexts) if (next.oPortType == PortType.Default) {
			if (next.oPort == index) list.Add(next.eventBase);
		}
	}

	protected override void GetDataID(ref uint eventID) {
		if (eventID == default) base.GetDataID(ref eventID);
		if (eventID != default) eventID = EventID;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game Manager | Stop Event
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Game Manager/Stop Event")]
public sealed class StopEventEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class StopEventEventNode : EventNodeBase {
		StopEventEvent I => target as StopEventEvent;

		public StopEventEventNode() : base() {
			mainContainer.style.width = Node1U;
		}

		public override void ConstructPort() {
			CreatePort(Direction.Input);
			CreatePort(Direction.Input, PortType.DataID);
			CreatePort(Direction.Output);
			RefreshExpandedState();
			RefreshPorts();
		}
	}
	#endif



	// Methods

	public override void End() {
		uint eventID = default;
		base.GetDataID(ref eventID);
		if (eventID != default) GameManager.StopEvent(eventID);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game Manager | Quit
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Game Manager/Quit")]
public sealed class QuitEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class QuitEventNode : EventNodeBase {
		QuitEvent I => target as QuitEvent;

		public QuitEventNode() : base() {
			mainContainer.style.width = Node1U;
			var cyan = new Color(180f, 0.75f, 0.60f).ToRGB();
			titleContainer.style.backgroundColor = cyan;
		}

		public override void ConstructPort() {
			CreatePort(Direction.Input);
			RefreshExpandedState();
			RefreshPorts();
		}
	}
	#endif



	// Methods

	public override void End() {
		GameManager.QuitGame();
	}
}

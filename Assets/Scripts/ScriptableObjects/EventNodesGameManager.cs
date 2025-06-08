using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

using Unity.Mathematics;

#if UNITY_EDITOR
	using UnityEditor;
	using UnityEditor.UIElements;
	using UnityEditor.Experimental.GraphView;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game Manager | Set Game State
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Game Manager/Set Game State")]
public class SetGameStateEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
		public class SetGameStateEventNode : BaseEventNode {
			SetGameStateEvent I => target as SetGameStateEvent;

			public SetGameStateEventNode() : base() {
				mainContainer.style.width = DefaultNodeWidth;
				var cyan = new StyleColor(color.HSVtoRGB(180f, 0.75f, 0.60f));
				titleContainer.style.backgroundColor = cyan;
			}

			public override void ConstructData() {
				var state = new EnumField(GameState.Gameplay) { value = I.state };
				state.RegisterValueChangedCallback(evt => I.state = (GameState)evt.newValue);
				mainContainer.Add(state);
			}
		}
	#endif



	// Fields

	public GameState state = GameState.Gameplay;



	// Methods

	public override void CopyFrom(BaseEvent data) {
		base.CopyFrom(data);
		if (data is SetGameStateEvent setGameState) {
			state = setGameState.state;
		}
	}

	public override void End() => GameManager.GameState = state;
}

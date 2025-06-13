using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game Canvas
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI/Game Canvas")]
public class GameCanvas : BaseCanvas {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(GameCanvas))]
	class GameCanvasEditor : EditorExtensions {
		GameCanvas I => target as GameCanvas;
		public override void OnInspectorGUI() {
			Begin("Game Canvas");

			if (I.Raycaster && I.Raycaster.enabled) {
				LabelField("Selected", EditorStyles.boldLabel);
				I.FirstSelected = ObjectField("First Selected", I.FirstSelected);
				Space();
			}
			LabelField("Status", EditorStyles.boldLabel);
			I.PlayerStatus = ObjectField("Player Status", I.PlayerStatus);
			I.MemberStatus = ObjectField("Member Status", I.MemberStatus);
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] GameObject m_PlayerStatus;
	[SerializeField] GameObject m_MemberStatus;



	// Properties

	GameObject PlayerStatus {
		get => m_PlayerStatus;
		set => m_PlayerStatus = value;
	}
	GameObject MemberStatus {
		get => m_MemberStatus;
		set => m_MemberStatus = value;
	}



	// Methods

	public override void Back() {
		UIManager.OpenMenu();
	}



	// Lifecycle

	protected override void Update(){
		if (InputManager.GetKeyUp(KeyAction.Menu) || !Application.isFocused) Back();
	}
}

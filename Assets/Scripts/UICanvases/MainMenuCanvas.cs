using UnityEngine;
using UnityEngine.Localization;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Main Menu Canvas
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI/Main Menu Canvas")]
public class MainMenuCanvas : BaseCanvas {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(MainMenuCanvas))]
	class MainMenuCanvasEditor : EditorExtensions {
		MainMenuCanvas I => target as MainMenuCanvas;
		public override void OnInspectorGUI() {
			Begin("Main Menu Canvas");

			if (I.Raycaster && I.Raycaster.enabled) {
				LabelField("Selected", EditorStyles.boldLabel);
				I.FirstSelected = ObjectField("First Selected", I.FirstSelected);
				Space();
			}
			LabelField("Confirm Quit Game", EditorStyles.boldLabel);
			PropertyField("m_QuitGameHeader");
			PropertyField("m_QuitGameContent");
			PropertyField("m_QuitGameConfirm");
			PropertyField("m_QuitGameCancel");
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] LocalizedString m_QuitGameHeader = new();
	[SerializeField] LocalizedString m_QuitGameContent = new();
	[SerializeField] LocalizedString m_QuitGameConfirm = new();
	[SerializeField] LocalizedString m_QuitGameCancel = new();



	// Properties

	LocalizedString QuitGameHeader  => m_QuitGameHeader;
	LocalizedString QuitGameContent => m_QuitGameContent;
	LocalizedString QuitGameConfirm => m_QuitGameConfirm;
	LocalizedString QuitGameCancel  => m_QuitGameCancel;



	// Methods

	public override void Back() {
		UIManager.ConfirmationHeaderReference  = QuitGameHeader;
		UIManager.ConfirmationContentReference = QuitGameContent;
		UIManager.ConfirmationConfirmReference = QuitGameConfirm;
		UIManager.ConfirmationCancelReference  = QuitGameCancel;
		UIManager.OpenConfirmation();
		#if UNITY_EDITOR
		UIManager.OnConfirmationConfirmed += () => EditorApplication.isPlaying = false;
		#else
		UIManager.OnConfirmationConfirmed += () => Application.Quit();
		#endif
	}
}

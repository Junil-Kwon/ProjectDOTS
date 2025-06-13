using UnityEngine;
using UnityEngine.Localization;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Menu Canvas
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI/Menu Canvas")]
public class MenuCanvas : BaseCanvas {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(MenuCanvas))]
	class MenuCanvasEditor : EditorExtensions {
		MenuCanvas I => target as MenuCanvas;
		public override void OnInspectorGUI() {
			Begin("Menu Canvas");

			if (I.Raycaster && I.Raycaster.enabled) {
				LabelField("Selected", EditorStyles.boldLabel);
				I.FirstSelected = ObjectField("First Selected", I.FirstSelected);
				Space();
			}
			LabelField("Confirm Return To Main Menu", EditorStyles.boldLabel);
			PropertyField("m_ReturnToMainMenuHeader");
			PropertyField("m_ReturnToMainMenuContent");
			PropertyField("m_ReturnToMainMenuConfirm");
			PropertyField("m_ReturnToMainMenuCancel");
			Space();
			LabelField("Confirm Return To Lobby", EditorStyles.boldLabel);
			PropertyField("m_ReturnToLobbyHeader");
			PropertyField("m_ReturnToLobbyContent");
			PropertyField("m_ReturnToLobbyConfirm");
			PropertyField("m_ReturnToLobbyCancel");
			Space();
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

	[SerializeField] LocalizedString m_ReturnToMainMenuHeader = new();
	[SerializeField] LocalizedString m_ReturnToMainMenuContent = new();
	[SerializeField] LocalizedString m_ReturnToMainMenuConfirm = new();
	[SerializeField] LocalizedString m_ReturnToMainMenuCancel = new();

	[SerializeField] LocalizedString m_ReturnToLobbyHeader = new();
	[SerializeField] LocalizedString m_ReturnToLobbyContent = new();
	[SerializeField] LocalizedString m_ReturnToLobbyConfirm = new();
	[SerializeField] LocalizedString m_ReturnToLobbyCancel = new();

	[SerializeField] LocalizedString m_QuitGameHeader = new();
	[SerializeField] LocalizedString m_QuitGameContent = new();
	[SerializeField] LocalizedString m_QuitGameConfirm = new();
	[SerializeField] LocalizedString m_QuitGameCancel = new();



	// Properties

	LocalizedString ReturnToMainMenuHeader  => m_ReturnToMainMenuHeader;
	LocalizedString ReturnToMainMenuContent => m_ReturnToMainMenuContent;
	LocalizedString ReturnToMainMenuConfirm => m_ReturnToMainMenuConfirm;
	LocalizedString ReturnToMainMenuCancel  => m_ReturnToMainMenuCancel;

	LocalizedString ReturnToLobbyHeader  => m_ReturnToLobbyHeader;
	LocalizedString ReturnToLobbyContent => m_ReturnToLobbyContent;
	LocalizedString ReturnToLobbyConfirm => m_ReturnToLobbyConfirm;
	LocalizedString ReturnToLobbyCancel  => m_ReturnToLobbyCancel;

	LocalizedString QuitGameHeader  => m_QuitGameHeader;
	LocalizedString QuitGameContent => m_QuitGameContent;
	LocalizedString QuitGameConfirm => m_QuitGameConfirm;
	LocalizedString QuitGameCancel  => m_QuitGameCancel;



	// Methods

	public void OpenAchievement() {
		//UIManager.OpenAchievement();
	}

	public void OpenSettings() {
		UIManager.OpenSettings();
	}

	public void ConfirmReturnToMainMenu() {
		UIManager.ConfirmationHeaderReference  = ReturnToMainMenuHeader;
		UIManager.ConfirmationContentReference = ReturnToMainMenuContent;
		UIManager.ConfirmationConfirmReference = ReturnToMainMenuConfirm;
		UIManager.ConfirmationCancelReference  = ReturnToMainMenuCancel;
		UIManager.OpenConfirmation();
		UIManager.OnConfirmationConfirmed += () => {
			NetworkManager.Disconnect();
			UIManager.OpenMainMenu();
		};
	}

	public void ConfirmReturnToLobby() {
		UIManager.ConfirmationHeaderReference  = ReturnToLobbyHeader;
		UIManager.ConfirmationContentReference = ReturnToLobbyContent;
		UIManager.ConfirmationConfirmReference = ReturnToLobbyConfirm;
		UIManager.ConfirmationCancelReference  = ReturnToLobbyCancel;
		UIManager.OpenConfirmation();
		UIManager.OnConfirmationConfirmed += () => {
			NetworkManager.Connect();
			UIManager.OpenGame();
		};
	}

	public void ConfirmQuitGame() {
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

	public override void Back() {
		UIManager.PopOverlay();
	}
}

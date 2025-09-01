using UnityEngine;
using UnityEngine.Localization;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Menu Screen
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI Screen/Menu Screen")]
public sealed class MenuScreen : ScreenBase {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(MenuScreen))]
	class MenuScreenEditor : EditorExtensions {
		MenuScreen I => target as MenuScreen;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Selected", EditorStyles.boldLabel);
			I.DefaultSelected = ObjectField("Default Selected", I.DefaultSelected);
			Space();

			LabelField("Confirm Back to Main Menu", EditorStyles.boldLabel);
			PropertyField("m_BackToMainMenuHeader");
			PropertyField("m_BackToMainMenuContent");
			PropertyField("m_BackToMainMenuConfirm");
			PropertyField("m_BackToMainMenuCancel");
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

	[SerializeField] LocalizedString m_BackToMainMenuHeader = new();
	[SerializeField] LocalizedString m_BackToMainMenuContent = new();
	[SerializeField] LocalizedString m_BackToMainMenuConfirm = new();
	[SerializeField] LocalizedString m_BackToMainMenuCancel = new();

	[SerializeField] LocalizedString m_QuitGameHeader = new();
	[SerializeField] LocalizedString m_QuitGameContent = new();
	[SerializeField] LocalizedString m_QuitGameConfirm = new();
	[SerializeField] LocalizedString m_QuitGameCancel = new();



	// Properties

	public override bool IsPrimary => false;
	public override bool IsOverlay => true;
	public override bool UseScreenBlur => true;



	LocalizedString BackToMainMenuHeader  => m_BackToMainMenuHeader;
	LocalizedString BackToMainMenuContent => m_BackToMainMenuContent;
	LocalizedString BackToMainMenuConfirm => m_BackToMainMenuConfirm;
	LocalizedString BackToMainMenuCancel  => m_BackToMainMenuCancel;

	LocalizedString QuitGameHeader  => m_QuitGameHeader;
	LocalizedString QuitGameContent => m_QuitGameContent;
	LocalizedString QuitGameConfirm => m_QuitGameConfirm;
	LocalizedString QuitGameCancel  => m_QuitGameCancel;



	// Methods

	public void Resume() {
		Back();
	}

	public void Bestiary() {
		UIManager.OpenScreen(Screen.Bestiary);
	}

	public void Achievements() {
		UIManager.OpenScreen(Screen.Achievements);
	}

	public void Options() {
		UIManager.OpenScreen(Screen.Options);
	}

	public void BackToMainMenu() {
		UIManager.ConfirmationHeaderReference  = BackToMainMenuHeader;
		UIManager.ConfirmationContentReference = BackToMainMenuContent;
		UIManager.ConfirmationConfirmReference = BackToMainMenuConfirm;
		UIManager.ConfirmationCancelReference  = BackToMainMenuCancel;
		UIManager.OpenScreen(Screen.Confirmation);
		UIManager.OnConfirmationConfirmed += () => {
			NetworkManager.Disconnect();
			UIManager.OpenScreen(Screen.MainMenu);
		};
	}

	public void QuitGame() {
		UIManager.ConfirmationHeaderReference  = QuitGameHeader;
		UIManager.ConfirmationContentReference = QuitGameContent;
		UIManager.ConfirmationConfirmReference = QuitGameConfirm;
		UIManager.ConfirmationCancelReference  = QuitGameCancel;
		UIManager.OpenScreen(Screen.Confirmation);
		UIManager.OnConfirmationConfirmed += () => {
			GameManager.QuitGame();
		};
	}
}

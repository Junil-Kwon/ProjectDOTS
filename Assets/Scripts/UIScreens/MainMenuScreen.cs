using UnityEngine;
using UnityEngine.Localization;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Title Screen
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI Screen/Title Screen")]
public sealed class MainMenuScreen : ScreenBase {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(MainMenuScreen))]
	class TitleScreenEditor : EditorExtensions {
		MainMenuScreen I => target as MainMenuScreen;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Selected", EditorStyles.boldLabel);
			I.DefaultSelected = ObjectField("Default Selected", I.DefaultSelected);
			Space();

			LabelField("Main Menu", EditorStyles.boldLabel);
			I.VersionText = ObjectField("Version Text", I.VersionText);
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

	[SerializeField] TextMeshProUGUI m_VersionText;

	[SerializeField] LocalizedString m_QuitGameHeader = new();
	[SerializeField] LocalizedString m_QuitGameContent = new();
	[SerializeField] LocalizedString m_QuitGameConfirm = new();
	[SerializeField] LocalizedString m_QuitGameCancel = new();



	// Properties

	public override bool IsPrimary => true;
	public override bool IsOverlay => true;
	public override bool UseScreenBlur => false;



	TextMeshProUGUI VersionText {
		get => m_VersionText;
		set => m_VersionText = value;
	}

	LocalizedString QuitGameHeader  => m_QuitGameHeader;
	LocalizedString QuitGameContent => m_QuitGameContent;
	LocalizedString QuitGameConfirm => m_QuitGameConfirm;
	LocalizedString QuitGameCancel  => m_QuitGameCancel;



	// Methods

	public void StoryMode() {
		UIManager.OpenScreen(Screen.Game);
		NetworkManager.Connect();
	}

	public void MultiplayerMode() {
		UIManager.OpenScreen(Screen.Multiplayer);
	}

	public void Options() {
		UIManager.OpenScreen(Screen.Options);
	}

	public void Credits() {
		UIManager.OpenScreen(Screen.Credits);
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

	public override void Back() {
		QuitGame();
	}



	// Lifecycle

	void Start() {
		VersionText.text = "ver. " + Application.version;
	}
}

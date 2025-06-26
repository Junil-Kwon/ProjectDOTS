using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// UI Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/UI Manager")]
public sealed class UIManager : MonoSingleton<UIManager> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(UIManager))]
	class UIManagerEditor : EditorExtensions {
		UIManager I => target as UIManager;
		public override void OnInspectorGUI() {
			Begin("UI Manager");

			LabelField("Debug", EditorStyles.boldLabel);
			BeginDisabledGroup();
			TextField("Current Canvas", $"{(Application.isPlaying ? CurrentCanvas : "None")}");
			EndDisabledGroup();
			Space();

			End();
		}
	}
	#endif



	// Constants

	public const string LocalizationTable = "UITable";

	public static readonly Vector2Int[] ScreenResolution = new Vector2Int[] {
		new(0640, 0360),
		new(1280, 0720),
		new(1920, 1080),
		new(2560, 1440),
		new(3840, 2160),
	};

	const string DebugScreenKey = "DebugScreen";
	const bool DebugScreenValue = false;



	// Fields

	MainMenuCanvas m_MainMenuCanvas;
	GameCanvas m_GameCanvas;
	MultiplayerCanvas m_MultiplayerCanvas;
	DialogueCanvas m_DialogueCanvas;
	MenuCanvas m_MenuCanvas;
	AchievementCanvas m_AchievementCanvas;
	SettingsCanvas m_SettingsCanvas;
	ConfirmationCanvas m_ConfirmationCanvas;
	AlertCanvas m_AlertCanvas;
	ChatCanvas m_ChatCanvas;
	FadeCanvas m_FadeCanvas;
	DebugCanvas m_DebugCanvas;

	BaseCanvas m_MainCanvas;
	Stack<BaseCanvas> m_OverlayCanvas = new();

	bool m_DebugScreen;



	// Properties

	static MainMenuCanvas MainMenuCanvas =>
		Instance.m_MainMenuCanvas || TryGetComponentInChildren(out Instance.m_MainMenuCanvas) ?
		Instance.m_MainMenuCanvas : null;

	static GameCanvas GameCanvas =>
		Instance.m_GameCanvas || TryGetComponentInChildren(out Instance.m_GameCanvas) ?
		Instance.m_GameCanvas : null;

	static MultiplayerCanvas MultiplayerCanvas =>
		Instance.m_MultiplayerCanvas || TryGetComponentInChildren(out Instance.m_MultiplayerCanvas) ?
		Instance.m_MultiplayerCanvas : null;

	static DialogueCanvas DialogueCanvas =>
		Instance.m_DialogueCanvas || TryGetComponentInChildren(out Instance.m_DialogueCanvas) ?
		Instance.m_DialogueCanvas : null;

	static MenuCanvas MenuCanvas =>
		Instance.m_MenuCanvas || TryGetComponentInChildren(out Instance.m_MenuCanvas) ?
		Instance.m_MenuCanvas : null;

	static AchievementCanvas AchievementCanvas =>
		Instance.m_AchievementCanvas || TryGetComponentInChildren(out Instance.m_AchievementCanvas) ?
		Instance.m_AchievementCanvas : null;

	static SettingsCanvas SettingsCanvas =>
		Instance.m_SettingsCanvas || TryGetComponentInChildren(out Instance.m_SettingsCanvas) ?
		Instance.m_SettingsCanvas : null;

	static ConfirmationCanvas ConfirmationCanvas =>
		Instance.m_ConfirmationCanvas || TryGetComponentInChildren(out Instance.m_ConfirmationCanvas) ?
		Instance.m_ConfirmationCanvas : null;

	static AlertCanvas AlertCanvas =>
		Instance.m_AlertCanvas || TryGetComponentInChildren(out Instance.m_AlertCanvas) ?
		Instance.m_AlertCanvas : null;

	static ChatCanvas ChatCanvas =>
		Instance.m_ChatCanvas || TryGetComponentInChildren(out Instance.m_ChatCanvas) ?
		Instance.m_ChatCanvas : null;

	static FadeCanvas FadeCanvas =>
		Instance.m_FadeCanvas || TryGetComponentInChildren(out Instance.m_FadeCanvas) ?
		Instance.m_FadeCanvas : null;

	static DebugCanvas DebugCanvas =>
		Instance.m_DebugCanvas || TryGetComponentInChildren(out Instance.m_DebugCanvas) ?
		Instance.m_DebugCanvas : null;



	static BaseCanvas MainCanvas {
		get => Instance.m_MainCanvas;
		set => Instance.m_MainCanvas = value;
	}
	static Stack<BaseCanvas> OverlayCanvas => Instance.m_OverlayCanvas;
	public static BaseCanvas CurrentCanvas =>
		OverlayCanvas.TryPeek(out var overlayCanvas) ?
		overlayCanvas : MainCanvas;

	public static bool IsUIActive => CurrentCanvas != GameCanvas;



	static GameObject Temp {
		get => EventSystem.current.currentSelectedGameObject;
		set => EventSystem.current.SetSelectedGameObject(value);
	}
	public static Selectable Selected {
		get => Temp && Temp.TryGetComponent(out Selectable selectable) ? selectable : null;
		set => Temp = value ? value.gameObject : null;
	}



	public static bool DebugScreen {
		get => Instance.m_DebugScreen == default ?
			Instance.m_DebugScreen = PlayerPrefs.GetInt(DebugScreenKey, DebugScreenValue ? 1 : 0) != 0 :
			Instance.m_DebugScreen;
		set {
			PlayerPrefs.SetInt(DebugScreenKey, (Instance.m_DebugScreen = value) ? 1 : 0);
			SetDebugScreen(value);
		}
	}



	// Methods

	static void Initialize() {
		var transform = Instance.transform;
		for (int i = 0; i < transform.childCount; i++) {
			if (transform.GetChild(i).TryGetComponent(out BaseCanvas canvas)) canvas.Hide();
		}
		MainCanvas = null;
		OverlayCanvas.Clear();
	}

	public static void Back() => CurrentCanvas?.Back();

	public static void PopOverlay() {
		if (OverlayCanvas.TryPop(out var next)) next.Hide();
		if (OverlayCanvas.TryPeek(out var prev)) prev.Show();
		OnCanvasChanged();
	}



	// Canvas Methods

	public static void OpenMainMenu() => OpenMainCanvas(MainMenuCanvas);
	public static void OpenGame()     => OpenMainCanvas(GameCanvas);

	static void OpenMainCanvas(BaseCanvas mainCanvas) {
		if (mainCanvas == CurrentCanvas) return;
		if (MainCanvas) MainCanvas.Hide();
		while (OverlayCanvas.TryPop(out var canvas)) canvas.Hide();
		MainCanvas = mainCanvas;
		mainCanvas.Show();
		OnCanvasChanged();
	}

	public static void OpenMultiplayer()  => OpenOverlayCanvas(MultiplayerCanvas);
	public static void OpenDialogue()     => OpenOverlayCanvas(DialogueCanvas);
	public static void OpenMenu()         => OpenOverlayCanvas(MenuCanvas);
	public static void OpenAchievement()  => OpenOverlayCanvas(AchievementCanvas);
	public static void OpenSettings()     => OpenOverlayCanvas(SettingsCanvas);
	public static void OpenConfirmation() => OpenOverlayCanvas(ConfirmationCanvas);
	public static void OpenAlert()        => OpenOverlayCanvas(AlertCanvas);
	public static void OpenChat()         => OpenOverlayCanvas(ChatCanvas);

	static void OpenOverlayCanvas(BaseCanvas overlayCanvas) {
		if (overlayCanvas == CurrentCanvas) return;
		if (OverlayCanvas.TryPeek(out var canvas)) canvas.Hide(true);
		OverlayCanvas.Push(overlayCanvas);
		overlayCanvas.Show();
		OnCanvasChanged();
	}

	static void OnCanvasChanged() {
		if (IsUIActive) {
			if (GameManager.GameState == GameState.Gameplay) {
				GameManager.GameState = GameState.Paused;
			}
		} else {
			if (GameManager.GameState == GameState.Paused) {
				GameManager.GameState = GameState.Gameplay;
			}
		}
	}



	// Confirmation Canvas Methods

	public static LocalizedString ConfirmationHeaderReference {
		get => ConfirmationCanvas.HeaderReference;
		set => ConfirmationCanvas.HeaderReference = value;
	}
	public static LocalizedString ConfirmationContentReference {
		get => ConfirmationCanvas.ContentReference;
		set => ConfirmationCanvas.ContentReference = value;
	}
	public static LocalizedString ConfirmationConfirmReference {
		get => ConfirmationCanvas.ConfirmReference;
		set => ConfirmationCanvas.ConfirmReference = value;
	}
	public static LocalizedString ConfirmationCancelReference {
		get => ConfirmationCanvas.CancelReference;
		set => ConfirmationCanvas.CancelReference = value;
	}

	public static Action OnConfirmationConfirmed {
		get => ConfirmationCanvas.OnConfirmed;
		set => ConfirmationCanvas.OnConfirmed = value;
	}
	public static Action OnConfirmationCancelled {
		get => ConfirmationCanvas.OnCancelled;
		set => ConfirmationCanvas.OnCancelled = value;
	}



	// Alert Canvas Methods

	public static LocalizedString AlertContentReference {
		get => AlertCanvas.ContentReference;
		set => AlertCanvas.ContentReference = value;
	}
	public static LocalizedString AlertCloseReference {
		get => AlertCanvas.CloseReference;
		set => AlertCanvas.CloseReference = value;
	}

	public static Action OnAlertClosed {
		get => AlertCanvas.OnClosed;
		set => AlertCanvas.OnClosed = value;
	}



	// Fade Canvas Methods

	public static void FadeIn() {

	}

	public static void FadeOut() {
		
	}



	// Debug Canvas Methods

	public static bool GetDebugScreen() => DebugCanvas.gameObject.activeSelf;
	public static void SetDebugScreen(bool value) => DebugCanvas.gameObject.SetActive(value);

	public static void ShowDebugScreen() => SetDebugScreen(true);
	public static void HideDebugScreen() => SetDebugScreen(false);



	// Lifecycle

	void OnEnable() {
		Initialize();
		OpenMainMenu();
	}

	void Start() {
		DebugScreen = DebugScreen;
	}
}

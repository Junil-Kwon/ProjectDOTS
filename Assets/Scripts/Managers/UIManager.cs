using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
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
		new( 640,  360),
		new(1280,  720),
		new(1920, 1080),
		new(2560, 1440),
		new(3840, 2160),
	};



	// Fields

	TitleCanvas        m_TitleCanvas;
	GameCanvas         m_GameCanvas;
	MultiplayerCanvas  m_MultiplayerCanvas;
	DialogueCanvas     m_DialogueCanvas;
	MenuCanvas         m_MenuCanvas;
	AchievementCanvas  m_AchievementCanvas;
	SettingsCanvas     m_SettingsCanvas;
	ConfirmationCanvas m_ConfirmationCanvas;
	AlertCanvas        m_AlertCanvas;
	FadeCanvas         m_FadeCanvas;

	BaseCanvas m_MainCanvas;
	readonly Stack<BaseCanvas> m_OverlayCanvas = new();
	readonly UnityEvent m_BackEvent = new();
	bool m_IsPointerClicked;



	// Properties

	static TitleCanvas TitleCanvas =>
		Instance.m_TitleCanvas || TryGetComponentInChildren(out Instance.m_TitleCanvas) ?
		Instance.m_TitleCanvas : null;

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

	static FadeCanvas FadeCanvas =>
		Instance.m_FadeCanvas || TryGetComponentInChildren(out Instance.m_FadeCanvas) ?
		Instance.m_FadeCanvas : null;



	static BaseCanvas MainCanvas {
		get => Instance.m_MainCanvas;
		set => Instance.m_MainCanvas = value;
	}
	static Stack<BaseCanvas> OverlayCanvas => Instance.m_OverlayCanvas;

	public static BaseCanvas CurrentCanvas {
		get => OverlayCanvas.TryPeek(out var overlayCanvas) ? overlayCanvas : MainCanvas;
	}
	public static bool IsUIActive => CurrentCanvas != GameCanvas;

	public static UnityEvent OnBackPressed => Instance.m_BackEvent;

	public static bool IsPointerClicked {
		get => Instance.m_IsPointerClicked;
		set => Instance.m_IsPointerClicked = value;
	}



	static GameObject Temp {
		get => EventSystem.current.currentSelectedGameObject;
		set => EventSystem.current.SetSelectedGameObject(value);
	}
	public static Selectable Selected {
		get => Temp && Temp.TryGetComponent(out Selectable selectable) ? selectable : null;
		set => Temp = value ? value.gameObject : null;
	}



	// Methods

	static void Initialize() {
		var transform = Instance.transform;
		for (int i = 0; i < transform.childCount; i++) {
			if (transform.GetChild(i).TryGetComponent(out BaseCanvas canvas)) canvas.Hide();
		}
		MainCanvas = null;
		OverlayCanvas.Clear();
		OnBackPressed.RemoveAllListeners();
		Selected = null;
	}

	public static void Back() {
		bool popOverlay = false;
		switch (CurrentCanvas) {
			case global::TitleCanvas:
				ConfirmQuitGame();
				break;
			case global::GameCanvas:
				OpenMenu();
				break;
			case global::ConfirmationCanvas:
				ConfirmationCanvas.CancelEvent.Invoke();
				popOverlay = true;
				break;
			case global::AlertCanvas:
				AlertCanvas.CloseEvent.Invoke();
				popOverlay = true;
				break;
			default:
				popOverlay = true;
				break;
		}
		if (popOverlay) {
			if (OverlayCanvas.TryPop (out var next)) next.Hide();
			if (OverlayCanvas.TryPeek(out var prev)) prev.Show();
			OnCanvasChanged();
		}
		OnBackPressed.Invoke();
		OnBackPressed.RemoveAllListeners();
	}



	// Canvas Methods

	public static void OpenTitle() => OpenMainCanvas(TitleCanvas);
	public static void OpenGame () => OpenMainCanvas(GameCanvas);

	static void OpenMainCanvas(BaseCanvas mainCanvas) {
		if (mainCanvas == CurrentCanvas) return;
		if (MainCanvas) MainCanvas.Hide();
		while (OverlayCanvas.TryPop(out var canvas)) canvas.Hide();
		MainCanvas = mainCanvas;
		mainCanvas.Show();
		OnCanvasChanged();
	}

	public static void OpenMultiplayer () => OpenOverlayCanvas(MultiplayerCanvas);
	public static void OpenDialogue    () => OpenOverlayCanvas(DialogueCanvas);
	public static void OpenMenu        () => OpenOverlayCanvas(MenuCanvas);
	public static void OpenAchievement () => OpenOverlayCanvas(AchievementCanvas);
	public static void OpenSettings    () => OpenOverlayCanvas(SettingsCanvas);
	public static void OpenConfirmation() => OpenOverlayCanvas(ConfirmationCanvas);
	public static void OpenAlert       () => OpenOverlayCanvas(AlertCanvas);

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

	public static UnityEvent GetConfirmEvent() => ConfirmationCanvas.ConfirmEvent;
	public static UnityEvent GetCancelEvent () => ConfirmationCanvas.CancelEvent;

	public static void ConfirmReturnToLobby() {
		OpenConfirmation();
		ConfirmationCanvas.HeaderKey  = "Confirmation_ReturnToLobbyHeader";
		ConfirmationCanvas.ContentKey = "Confirmation_ReturnToLobbyContent";
		ConfirmationCanvas.ConfirmKey = "Confirmation_ReturnToLobbyConfirm";
		ConfirmationCanvas.CancelKey  = "Confirmation_ReturnToLobbyCancel";
		ConfirmationCanvas.ConfirmEvent.AddListener(() => {});
	}

	public static void ConfirmQuitGame() {
		OpenConfirmation();
		ConfirmationCanvas.HeaderKey  = "Confirmation_QuitGameHeader";
		ConfirmationCanvas.ContentKey = "Confirmation_QuitGameContent";
		ConfirmationCanvas.ConfirmKey = "Confirmation_QuitGameConfirm";
		ConfirmationCanvas.CancelKey  = "Confirmation_QuitGameCancel";
		#if UNITY_EDITOR
			ConfirmationCanvas.ConfirmEvent.AddListener(() => EditorApplication.isPlaying = false);
		#else
			ConfirmationCanvas.ConfirmEvent.AddListener(() => Application.Quit());
		#endif
	}

	public static void ConfirmResetAllData() {
		OpenConfirmation();
		ConfirmationCanvas.SetSelectedCancel();
		ConfirmationCanvas.HeaderKey  = "Confirmation_ResetAllDataHeader";
		ConfirmationCanvas.ContentKey = "Confirmation_ResetAllDataContent";
		ConfirmationCanvas.ConfirmKey = "Confirmation_ResetAllDataConfirm";
		ConfirmationCanvas.CancelKey  = "Confirmation_ResetAllDataCancel";
	}



	// Alert Canvas Methods

	public static void AlertServerConnectionLost() {
		OpenAlert();
		AlertCanvas.ContentKey = "Alert_ServerConnectionLostContent";
		AlertCanvas.CloseKey   = "Alert_ServerConnectionLostClose";
	}

	public static void AlertAllDataReset() {
		OpenAlert();
		AlertCanvas.ContentKey = "Alert_AllDataResetContent";
		AlertCanvas.CloseKey   = "Alert_AllDataResetClose";
	}



	// Fade Canvas Methods

	public static void FadeIn() { }
	public static void FadeOut() { }



	// Lifecycle

	void OnEnable() {
		Initialize();
		OpenTitle();
	}

	void Update() {
		if (IsUIActive) {
			if (InputManager.GetKeyUp(KeyAction.Cancel)) Back();
			if (InputManager.Navigate != Vector2.zero && !Selected) {
				Selected = CurrentCanvas.FirstSelected;
			}
		} else {
			if (InputManager.GetKeyUp(KeyAction.Menu) || !Application.isFocused) Back();
			IsPointerClicked = true;
		}
	}
}

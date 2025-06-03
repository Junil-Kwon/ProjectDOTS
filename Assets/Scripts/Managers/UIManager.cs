using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Localization.Settings;
using System.Collections.Generic;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// UI Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/UI Manager")]
public class UIManager : MonoSingleton<UIManager> {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(UIManager))]
		class UIManagerEditor : EditorExtensions {
			UIManager I => target as UIManager;
			public override void OnInspectorGUI() {
				Begin("UI Manager");

				LabelField("Canvas", EditorStyles.boldLabel);
				if (!TitleCanvas       ) HelpBox("No title canvas found.");
				if (!GameCanvas        ) HelpBox("No game canvas found.");
				if (!MultiplayerCanvas ) HelpBox("No multiplayer canvas found.");
				if (!DialogueCanvas    ) HelpBox("No dialogue canvas found.");
				if (!MenuCanvas        ) HelpBox("No menu canvas found.");
				if (!AchievementCanvas ) HelpBox("No achievement canvas found.");
				if (!SettingsCanvas    ) HelpBox("No settings canvas found.");
				if (!ConfirmationCanvas) HelpBox("No confirmation canvas found.");
				if (!AlertCanvas	   ) HelpBox("No alert canvas found.");
				if (!FadeCanvas        ) HelpBox("No fade canvas found.");
				Space();
				LabelField("Debug", EditorStyles.boldLabel);
				BeginDisabledGroup();
				TextField("Main Canvas", MainCanvas ? MainCanvas.name : string.Empty);
				string overlayCanvas = string.Empty;
				foreach (var canvas in OverlayCanvas) overlayCanvas += $"{canvas.name} ";
				TextField("Overlay Canvas", overlayCanvas);
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
	readonly UnityEvent        m_BackEvent     = new();
	bool m_IsPointerClicked;



	// Properties

	static RectTransform Transform => Instance.transform as RectTransform;

	static TitleCanvas TitleCanvas {
		get {
			if (!Instance.m_TitleCanvas) for (int i = 0; i < Transform.childCount; i++) {
				if (Transform.GetChild(i).TryGetComponent(out Instance.m_TitleCanvas)) break;
			}
			return Instance.m_TitleCanvas;
		}
	}
	static GameCanvas GameCanvas {
		get {
			if (!Instance.m_GameCanvas) for (int i = 0; i < Transform.childCount; i++) {
				if (Transform.GetChild(i).TryGetComponent(out Instance.m_GameCanvas)) break;
			}
			return Instance.m_GameCanvas;
		}
	}
	static MultiplayerCanvas MultiplayerCanvas {
		get {
			if (!Instance.m_MultiplayerCanvas) for (int i = 0; i < Transform.childCount; i++) {
				if (Transform.GetChild(i).TryGetComponent(out Instance.m_MultiplayerCanvas)) break;
			}
			return Instance.m_MultiplayerCanvas;
		}
	}
	static DialogueCanvas DialogueCanvas {
		get {
			if (!Instance.m_DialogueCanvas) for (int i = 0; i < Transform.childCount; i++) {
				if (Transform.GetChild(i).TryGetComponent(out Instance.m_DialogueCanvas)) break;
			}
			return Instance.m_DialogueCanvas;
		}
	}
	static MenuCanvas MenuCanvas {
		get {
			if (!Instance.m_MenuCanvas) for (int i = 0; i < Transform.childCount; i++) {
				if (Transform.GetChild(i).TryGetComponent(out Instance.m_MenuCanvas)) break;
			}
			return Instance.m_MenuCanvas;
		}
	}
	static AchievementCanvas AchievementCanvas {
		get {
			if (!Instance.m_AchievementCanvas) for (int i = 0; i < Transform.childCount; i++) {
				if (Transform.GetChild(i).TryGetComponent(out Instance.m_AchievementCanvas)) break;
			}
			return Instance.m_AchievementCanvas;
		}
	}
	static SettingsCanvas SettingsCanvas {
		get {
			if (!Instance.m_SettingsCanvas) for (int i = 0; i < Transform.childCount; i++) {
				if (Transform.GetChild(i).TryGetComponent(out Instance.m_SettingsCanvas)) break;
			}
			return Instance.m_SettingsCanvas;
		}
	}
	static ConfirmationCanvas ConfirmationCanvas {
		get {
			if (!Instance.m_ConfirmationCanvas) for (int i = 0; i < Transform.childCount; i++) {
				if (Transform.GetChild(i).TryGetComponent(out Instance.m_ConfirmationCanvas)) break;
			}
			return Instance.m_ConfirmationCanvas;
		}
	}
	static AlertCanvas AlertCanvas {
		get {
			if (!Instance.m_AlertCanvas) for (int i = 0; i < Transform.childCount; i++) {
				if (Transform.GetChild(i).TryGetComponent(out Instance.m_AlertCanvas)) break;
			}
			return Instance.m_AlertCanvas;
		}
	}
	static FadeCanvas FadeCanvas {
		get {
			if (!Instance.m_FadeCanvas) for (int i = 0; i < Transform.childCount; i++) {
				if (Transform.GetChild(i).TryGetComponent(out Instance.m_FadeCanvas)) break;
			}
			return Instance.m_FadeCanvas;
		}
	}



	static BaseCanvas MainCanvas {
		get => Instance.m_MainCanvas;
		set => Instance.m_MainCanvas = value;
	}
	static Stack<BaseCanvas> OverlayCanvas => Instance.m_OverlayCanvas;

	public static BaseCanvas CurrentCanvas {
		get => OverlayCanvas.TryPeek(out var overlayCanvas) ? overlayCanvas : MainCanvas;
	}
	public static bool IsUIActive => CurrentCanvas != GameCanvas;

	public static UnityEvent BackEvent => Instance.m_BackEvent;

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

	public static void Initialize() {
		TitleCanvas.Hide();
		GameCanvas.Hide();
		MultiplayerCanvas.Hide();
		DialogueCanvas.Hide();
		MenuCanvas.Hide();
		AchievementCanvas.Hide();
		SettingsCanvas.Hide();
		ConfirmationCanvas.Hide();
		AlertCanvas.Hide();
		FadeCanvas.Hide();
	}

	public static void Back() {
		if (!OverlayCanvas.TryPeek(out var canvas)) {
			switch (MainCanvas) {
				case global::TitleCanvas:
					ConfirmQuitGame();
					break;
				case global::GameCanvas:
					ShowMenu();
					break;
			}
		} else {
			bool flag = true;
			switch (canvas) {
				case global::ConfirmationCanvas:
					ConfirmationCanvas.CancelEvent.Invoke();
					break;
				case global::AlertCanvas:
					AlertCanvas.CloseEvent.Invoke();
					break;
			}
			if (flag) {
				OverlayCanvas.Pop();
				canvas.Hide();
				if (OverlayCanvas.TryPeek(out canvas)) canvas.Show();
			}
		}
		BackEvent.Invoke();
		BackEvent.RemoveAllListeners();
	}



	// Canvas Methods

	public static void ShowTitle() => ShowMainCanvas(TitleCanvas);
	public static void ShowGame () => ShowMainCanvas(GameCanvas);

	static void ShowMainCanvas(BaseCanvas mainCanvas) {
		if (MainCanvas) {
			if (MainCanvas == mainCanvas) return;
			MainCanvas.Hide();
		}
		while (OverlayCanvas.TryPop(out var canvas)) canvas.Hide();
		MainCanvas = mainCanvas;
		mainCanvas.Show();
	}

	public static void ShowMultiplayer () => ShowOverlayCanvas(MultiplayerCanvas);
	public static void ShowDialogue    () => ShowOverlayCanvas(DialogueCanvas);
	public static void ShowMenu        () => ShowOverlayCanvas(MenuCanvas);
	public static void ShowAchievement () => ShowOverlayCanvas(AchievementCanvas);
	public static void ShowSettings    () => ShowOverlayCanvas(SettingsCanvas);
	public static void ShowConfirmation() => ShowOverlayCanvas(ConfirmationCanvas);
	public static void ShowAlert       () => ShowOverlayCanvas(AlertCanvas);

	static void ShowOverlayCanvas(BaseCanvas overlayCanvas) {
		if (OverlayCanvas.TryPeek(out var canvas)) {
			if (canvas == overlayCanvas) return;
			if (canvas is MenuCanvas     menuCanvas    ) menuCanvas    .Hide(true);
			if (canvas is SettingsCanvas settingsCanvas) settingsCanvas.Hide(true);
		}
		OverlayCanvas.Push(overlayCanvas);
		overlayCanvas.Show();
	}



	// Confirmation Canvas Methods

	public static UnityEvent GetConfirmEvent() => ConfirmationCanvas.ConfirmEvent;
	public static UnityEvent GetCancelEvent () => ConfirmationCanvas.CancelEvent;

	public static void ConfirmReturnToLobby() {
		ShowConfirmation();
		ConfirmationCanvas.HeaderKey  = "Confirmation_ReturnToLobbyHeader";
		ConfirmationCanvas.ContentKey = "Confirmation_ReturnToLobbyContent";
		ConfirmationCanvas.ConfirmKey = "Confirmation_ReturnToLobbyConfirm";
		ConfirmationCanvas.CancelKey  = "Confirmation_ReturnToLobbyCancel";
		ConfirmationCanvas.ConfirmEvent.AddListener(() => {});
	}

	public static void ConfirmQuitGame() {
		ShowConfirmation();
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
		ShowConfirmation();
		ConfirmationCanvas.SetSelectedCancel();
		ConfirmationCanvas.HeaderKey  = "Confirmation_ResetAllDataHeader";
		ConfirmationCanvas.ContentKey = "Confirmation_ResetAllDataContent";
		ConfirmationCanvas.ConfirmKey = "Confirmation_ResetAllDataConfirm";
		ConfirmationCanvas.CancelKey  = "Confirmation_ResetAllDataCancel";
	}



	// Alert Canvas Methods

	public static void AlertServerConnectionLost() {
		ShowAlert();
		AlertCanvas.ContentKey = "Alert_ServerConnectionLostContent";
		AlertCanvas.CloseKey   = "Alert_ServerConnectionLostClose";
	}

	public static void AlertAllDataReset() {
		ShowAlert();
		AlertCanvas.ContentKey = "Alert_AllDataResetContent";
		AlertCanvas.CloseKey   = "Alert_AllDataResetClose";
	}



	// Fade Canvas Methods

	public static void FadeIn () { }
	public static void FadeOut() { }



	// Lifecycle

	void Update() {
		if (IsUIActive) {
			if (InputManager.GetKeyUp(KeyAction.Cancel)) Back();
			if (InputManager.Navigate != Vector2.zero && !Selected) {
				Selected = CurrentCanvas.FirstSelected;
			}
		} else IsPointerClicked = true;
	}
}

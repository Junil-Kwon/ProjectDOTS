using UnityEngine;
using System.Collections.Generic;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.NetCode;

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

				LabelField("Main Canvas", MainCanvas ? MainCanvas.name : "None");
				LabelField("Overlay Canvas");
				foreach (var canvas in OverlayCanvas) LabelField(" ", canvas.name);

				End();
			}
		}
	#endif



	// Fields

	TitleCanvas        m_TitleCanvas;
	GameCanvas         m_GameCanvas;
	DialogueCanvas     m_DialogueCanvas;
	MenuCanvas         m_MenuCanvas;
	SettingsCanvas     m_SettingsCanvas;
	ConfirmationCanvas m_ConfirmationCanvas;
	FadeCanvas         m_FadeCanvas;

	BaseCanvas m_MainCanvas;
	readonly Queue<BaseCanvas> m_OverlayCanvas = new();



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
	static Queue<BaseCanvas> OverlayCanvas => Instance.m_OverlayCanvas;

	public static bool IsUIActive => MainCanvas != GameCanvas || 0 < OverlayCanvas.Count;



	// Methods

	public static void Initialize() {
		TitleCanvas   .Hide();
		GameCanvas    .Hide();
		DialogueCanvas.Hide();
		MenuCanvas    .Hide();
		SettingsCanvas.Hide();
		FadeCanvas    .Hide();
	}

	public static void Back() {
		if (!OverlayCanvas.TryPeek(out var canvas)) switch (MainCanvas) {
			case global::TitleCanvas:
				//ShowConfirmationScreen("Are you sure you want to quit?");
				// ConfirmationCanvas.PositiveResponse.AddListener(GameManager.Quit);
				break;
			case global::GameCanvas:
				ShowMenuScreen();
				break;
		} else switch (canvas) {
			case global::DialogueCanvas:
				ShowMenuScreen();
				break;
			default:
				OverlayCanvas.Dequeue();
				canvas.Hide();
				if (OverlayCanvas.TryPeek(out canvas)) canvas.Show();
				break;
		}
	}



	public static void ShowTitleScreen() => ShowMainCanvas(TitleCanvas);
	public static void ShowGameScreen () => ShowMainCanvas(GameCanvas);

	public static void ShowMainCanvas(BaseCanvas mainCanvas) {
		if (MainCanvas) {
			if (MainCanvas == mainCanvas) return;
			MainCanvas.Hide();
		}
		while (OverlayCanvas.TryPeek(out var overlayCanvas)) {
			OverlayCanvas.Dequeue();
			overlayCanvas.Hide();
		}
		MainCanvas = mainCanvas;
		mainCanvas.Show();
	}

	public static void ShowDialogueScreen() => ShowOverlayCanvas(DialogueCanvas);
	public static void ShowMenuScreen    () => ShowOverlayCanvas(MenuCanvas);
	public static void ShowSettingsScreen() => ShowOverlayCanvas(SettingsCanvas);

	static void ShowOverlayCanvas(BaseCanvas overlayCanvas) {
		if (OverlayCanvas.TryPeek(out var canvas)) {
			if (canvas == overlayCanvas) return;
			if (canvas is MenuCanvas     menuCanvas    ) menuCanvas    .Hide(true);
			if (canvas is SettingsCanvas settingsCanvas) settingsCanvas.Hide(true);
		}
		OverlayCanvas.Enqueue(overlayCanvas);
		overlayCanvas.Show();
	}



	public static void ShowConfirmationScreen(string message) {
		ShowOverlayCanvas(ConfirmationCanvas);
	}

	public static void FadeIn () { }
	public static void FadeOut() { }



	// Lifecycle

	void Update() {
		if (InputManager.GetKeyUp(KeyAction.Cancel)) Back();
	}
}

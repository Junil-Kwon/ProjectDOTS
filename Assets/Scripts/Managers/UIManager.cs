using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Settings;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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

				LabelField("Canvas", EditorStyles.boldLabel);
				if (!TitleCanvas       ) HelpBox("No title canvas found.");
				if (!GameCanvas        ) HelpBox("No game canvas found.");
				if (!DialogueCanvas    ) HelpBox("No dialogue canvas found.");
				if (!MenuCanvas        ) HelpBox("No menu canvas found.");
				if (!SettingsCanvas    ) HelpBox("No settings canvas found.");
				if (!ConfirmationCanvas) HelpBox("No confirmation canvas found.");
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

	const string LanguageKey = "Language";



	// Fields

	string m_Language;

	TitleCanvas        m_TitleCanvas;
	GameCanvas         m_GameCanvas;
	DialogueCanvas     m_DialogueCanvas;
	MenuCanvas         m_MenuCanvas;
	SettingsCanvas     m_SettingsCanvas;
	ConfirmationCanvas m_ConfirmationCanvas;
	FadeCanvas         m_FadeCanvas;

	BaseCanvas m_MainCanvas;
	readonly Stack<BaseCanvas> m_OverlayCanvas = new();



	// Properties

	public static string Language {
		get {
			if (string.IsNullOrEmpty(Instance.m_Language)) {
				Instance.m_Language = PlayerPrefs.GetString(LanguageKey, null);
			}
			if (string.IsNullOrEmpty(Instance.m_Language)) {
				string systemLanguage = Application.systemLanguage.ToString();
				foreach (var locale in LocalizationSettings.AvailableLocales.Locales) {
					var name = locale.Identifier.CultureInfo.EnglishName;
					if (Regex.Replace(name, "[ \\(\\)]", "").Equals(systemLanguage)) {
						Instance.m_Language = locale.Identifier.CultureInfo.NativeName;
						LocalizationSettings.SelectedLocale = locale;
						break;
					}
				}
			}
			return Instance.m_Language;
		}
		set {
			foreach (var locale in LocalizationSettings.AvailableLocales.Locales) {
				if (locale.Identifier.CultureInfo.NativeName.Equals(value)) {
					PlayerPrefs.SetString(LanguageKey, Instance.m_Language = value);
					LocalizationSettings.SelectedLocale = locale;
					break;
				}
			}
		}
	}



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
	static Stack<BaseCanvas> OverlayCanvas => Instance.m_OverlayCanvas;

	public static bool IsUIActive => MainCanvas != GameCanvas || 0 < OverlayCanvas.Count;



	// Methods

	public static void Initialize() {
		TitleCanvas       .Hide();
		GameCanvas        .Hide();
		DialogueCanvas    .Hide();
		MenuCanvas        .Hide();
		SettingsCanvas    .Hide();
		ConfirmationCanvas.Hide();
		FadeCanvas        .Hide();
	}

	public static void Back() {
		if (!OverlayCanvas.TryPeek(out var canvas)) switch (MainCanvas) {
			case global::TitleCanvas:
				ConfirmQuit();
				break;
			case global::GameCanvas:
				ShowMenu();
				break;
		} else switch (canvas) {
			case global::DialogueCanvas:
				ShowMenu();
				break;
			default:
				OverlayCanvas.Pop();
				canvas.Hide();
				if (OverlayCanvas.TryPeek(out canvas)) canvas.Show();
				break;
		}
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

	public static void ShowDialogue() => ShowOverlayCanvas(DialogueCanvas);
	public static void ShowMenu    () => ShowOverlayCanvas(MenuCanvas);
	public static void ShowSettings() => ShowOverlayCanvas(SettingsCanvas);

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

	public static void ShowConfirmation(string message) {
		ShowOverlayCanvas(ConfirmationCanvas);
	}

	public static void ConfirmQuit() {
		var action = default(UnityAction);
		#if UNITY_EDITOR
			action = () => EditorApplication.isPlaying = false;
		#else
			action = Application.Quit();
		#endif
		ShowConfirmation("Are you sure you want to quit?");
		ConfirmationCanvas.PositiveResponse.AddListener(action);
	}



	// Fade Canvas Methods

	public static void FadeIn () { }
	public static void FadeOut() { }



	// Lifecycle

	void Awake() {
		_ = Language;
	}

	void Update() {
		if (InputManager.GetKeyUp(KeyAction.Cancel)) Back();
	}
}

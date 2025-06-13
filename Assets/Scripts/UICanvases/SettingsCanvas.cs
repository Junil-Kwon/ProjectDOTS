using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Settings Canvas
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI/Settings Canvas")]
public class SettingsCanvas : BaseCanvas {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(SettingsCanvas))]
	class SettingsCanvasEditor : EditorExtensions {
		SettingsCanvas I => target as SettingsCanvas;
		public override void OnInspectorGUI() {
			Begin("Settings Canvas");

			if (I.Raycaster && I.Raycaster.enabled) {
				LabelField("Selected", EditorStyles.boldLabel);
				I.FirstSelected = ObjectField("First Selected", I.FirstSelected);
				Space();
			}
			LabelField("Confirm Reset All Data", EditorStyles.boldLabel);
			PropertyField("m_ResetAllDataHeader");
			PropertyField("m_ResetAllDataContent");
			PropertyField("m_ResetAllDataConfirm");
			PropertyField("m_ResetAllDataCancel");
			Space();
			LabelField("Alert All Data Reset", EditorStyles.boldLabel);
			PropertyField("m_AllDataResetContent");
			PropertyField("m_AllDataResetClose");
			Space();
			LabelField("Confirm Restore Defaults", EditorStyles.boldLabel);
			PropertyField("m_RestoreDefaultsHeader");
			PropertyField("m_RestoreDefaultsContent");
			PropertyField("m_RestoreDefaultsConfirm");
			PropertyField("m_RestoreDefaultsCancel");
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] LocalizedString m_ResetAllDataHeader = new();
	[SerializeField] LocalizedString m_ResetAllDataContent = new();
	[SerializeField] LocalizedString m_ResetAllDataConfirm = new();
	[SerializeField] LocalizedString m_ResetAllDataCancel = new();

	[SerializeField] LocalizedString m_AllDataResetContent = new();
	[SerializeField] LocalizedString m_AllDataResetClose = new();

	[SerializeField] LocalizedString m_RestoreDefaultsHeader = new();
	[SerializeField] LocalizedString m_RestoreDefaultsContent = new();
	[SerializeField] LocalizedString m_RestoreDefaultsConfirm = new();
	[SerializeField] LocalizedString m_RestoreDefaultsCancel = new();



	// Properties

	LocalizedString ResetAllDataHeader  => m_ResetAllDataHeader;
	LocalizedString ResetAllDataContent => m_ResetAllDataContent;
	LocalizedString ResetAllDataConfirm => m_ResetAllDataConfirm;
	LocalizedString ResetAllDataCancel  => m_ResetAllDataCancel;

	LocalizedString AllDataResetContent => m_AllDataResetContent;
	LocalizedString AllDataResetClose   => m_AllDataResetClose;

	LocalizedString RestoreDefaultsHeader  => m_RestoreDefaultsHeader;
	LocalizedString RestoreDefaultsContent => m_RestoreDefaultsContent;
	LocalizedString RestoreDefaultsConfirm => m_RestoreDefaultsConfirm;
	LocalizedString RestoreDefaultsCancel  => m_RestoreDefaultsCancel;



	// General Methods

	public void SetLanguage(int value) {
		LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[value];
	}
	public void RefreshLanguageDropdown(CustomDropdown dropdown) {
		if (dropdown.Elements.Length == 0) {
			var elements = new string[LocalizationSettings.AvailableLocales.Locales.Count];
			for (int i = 0; i < elements.Length; i++) {
				var locale = LocalizationSettings.AvailableLocales.Locales[i];
				var nativeName = locale.Identifier.CultureInfo.NativeName;
				if (nativeName.Equals("中文")) {
					if (locale.Identifier.Code.Equals("zh-Hans")) nativeName = "简体中文";
					if (locale.Identifier.Code.Equals("zh-Hant")) nativeName = "繁體中文";
				}
				elements[i] = nativeName;
			}
			dropdown.Elements = elements;
		}
		for (int i = 0; i < dropdown.Elements.Length; i++) {
			var locale = LocalizationSettings.AvailableLocales.Locales[i];
			if (locale == LocalizationSettings.SelectedLocale) {
				dropdown.CurrentValue = i;
				break;
			}
		}
	}

	public void SetFullScreen(bool value) {
		Screen.fullScreenMode = value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
	}
	public void RefreshFullScreenToggle(CustomToggle toggle) {
		StartCoroutine(RefreshFullScreenToggleCoroutine(toggle));
	}
	IEnumerator RefreshFullScreenToggleCoroutine(CustomToggle toggle) {
		float timeStartChange = Time.realtimeSinceStartup;
		while (Time.realtimeSinceStartup - timeStartChange < 0.5f) {
			toggle.CurrentValue = Screen.fullScreenMode == FullScreenMode.FullScreenWindow;
			yield return null;
		}
	}

	public void SetScreenResolution(int value) {
		var resolution = UIManager.ScreenResolution[value];
		Screen.SetResolution(resolution.x, resolution.y, Screen.fullScreen);
	}
	public void RefreshScreenResolutionStepper(CustomStepper stepper) {
		if (stepper.Options.Length == 0) {
			var elements = new string[UIManager.ScreenResolution.Length];
			for (int i = 0; i < elements.Length; i++) {
				var resolution = UIManager.ScreenResolution[i];
				elements[i] = $"{resolution.x}x{resolution.y}";
			}
			stepper.Options = elements;
		}
	}

	public void SetMusic(float value) {
		SoundManager.Music = value;
	}
	public void RefreshMusicSlider(CustomSlider slider) {
		slider.CurrentValue = SoundManager.Music;
	}

	public void SetSoundFX(float value) {
		SoundManager.SoundFX = value;
	}
	public void RefreshSoundFXSlider(CustomSlider slider) {
		slider.CurrentValue = SoundManager.SoundFX;
	}



	// Controls Methods

	public void SetMouseSensitivity(float value) {
		InputManager.MouseSensitivity = value;
	}
	public void RefreshMouseSensitivitySlider(CustomSlider slider) {
		slider.CurrentValue = InputManager.MouseSensitivity;
	}



	// Advanced Methods

	public void SetMaxFrameRate(float value) {
		GameManager.MaxFrameRate = (int)value;
	}
	public void RefreshMaxFrameRateSlider(CustomSlider slider) {
		slider.CurrentValue = GameManager.MaxFrameRate;
	}

	public void SetDisplayDebugScreen(bool value) {
		UIManager.DebugScreen = value;
	}
	public void RefreshDisplayDebugScreenToggle(CustomToggle toggle) {
		toggle.CurrentValue = UIManager.DebugScreen;
	}

	public void ConfirmResetAllData() {
		UIManager.ConfirmationHeaderReference  = ResetAllDataHeader;
		UIManager.ConfirmationContentReference = ResetAllDataContent;
		UIManager.ConfirmationConfirmReference = ResetAllDataConfirm;
		UIManager.ConfirmationCancelReference  = ResetAllDataCancel;
		UIManager.OpenConfirmation();
		UIManager.OnConfirmationConfirmed += () => {
			RestoreDefaults();
			PlayerPrefs.DeleteAll();
			UIManager.AlertContentReference = AllDataResetContent;
			UIManager.AlertCloseReference   = AllDataResetClose;
			UIManager.OpenAlert();
		};
	}



	// Methods

	public void ConfirmRestoreDefaults() {
		UIManager.ConfirmationHeaderReference  = RestoreDefaultsHeader;
		UIManager.ConfirmationContentReference = RestoreDefaultsContent;
		UIManager.ConfirmationConfirmReference = RestoreDefaultsConfirm;
		UIManager.ConfirmationCancelReference  = RestoreDefaultsCancel;
		UIManager.OpenConfirmation();
		UIManager.OnConfirmationConfirmed += RestoreDefaults;
	}

	void RestoreDefaults() {
		var stack = new Stack<Transform>();
		stack.Push(transform);
		while (0 < stack.Count) {
			var current = stack.Pop();
			for (int i = 0; i < current.childCount; i++) stack.Push(current.GetChild(i));
			if (current.gameObject.activeSelf && current.TryGetComponent(out IBaseWidget widget)) {
				widget.Restore();
			}
		}
	}

	public override void Back() {
		// -
		UIManager.PopOverlay();
	}
}

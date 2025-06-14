using UnityEngine;
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

			if (I.Raycaster) {
				LabelField("Selected", EditorStyles.boldLabel);
				I.FirstSelected = ObjectField("First Selected", I.FirstSelected);
				Space();
			}
			End();
		}
	}
	#endif



	// General Methods

	public void UpdateLanguageDropdown(CustomDropdown dropdown) {
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
				dropdown.Value = i;
				break;
			}
		}
	}
	public void SetLanguageValue(int value) {
		LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[value];
	}

	public void UpdateFullScreenToggle(CustomToggle toggle) {
		StartCoroutine(UpdateFullScreenToggleCoroutine(toggle));
	}
	IEnumerator UpdateFullScreenToggleCoroutine(CustomToggle toggle) {
		float timeStartChange = Time.realtimeSinceStartup;
		while (Time.realtimeSinceStartup - timeStartChange < 0.5f) {
			toggle.Value = Screen.fullScreenMode == FullScreenMode.FullScreenWindow;
			yield return null;
		}
	}
	public void SetFullScreenValue(bool value) {
		Screen.fullScreenMode = value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
	}

	public void UpdateScreenResolutionStepper(CustomStepper stepper) {
		if (stepper.Elements.Length == 0) {
			var elements = new string[UIManager.ScreenResolution.Length];
			for (int i = 0; i < elements.Length; i++) {
				var resolution = UIManager.ScreenResolution[i];
				elements[i] = $"{resolution.x}x{resolution.y}";
			}
			stepper.Elements = elements;
		}
	}
	public void SetScreenResolutionValue(int value) {
		var resolution = UIManager.ScreenResolution[value];
		Screen.SetResolution(resolution.x, resolution.y, Screen.fullScreen);
	}

	public void UpdateMusicSlider(CustomSlider slider) {
		slider.Value = SoundManager.Music;
	}
	public void SetMusicValue(float value) {
		SoundManager.Music = value;
	}

	public void UpdateSoundFXSlider(CustomSlider slider) {
		slider.Value = SoundManager.SoundFX;
	}
	public void SetSoundFXValue(float value) {
		SoundManager.SoundFX = value;
	}



	// Controls Methods

	public void UpdateMouseSensitivitySlider(CustomSlider slider) {
		slider.Value = InputManager.MouseSensitivity;
	}
	public void SetMouseSensitivityValue(float value) {
		InputManager.MouseSensitivity = value;
	}



	// Advanced Methods

	public void UpdateMaxFrameRateSlider(CustomSlider slider) {
		slider.Value = GameManager.MaxFrameRate;
	}
	public void SetMaxFrameRateValue(float value) {
		GameManager.MaxFrameRate = (int)value;
	}

	public void UpdateDisplayDebugScreenToggle(CustomToggle toggle) {
		toggle.Value = UIManager.DisplayDebugScreen;
	}
	public void SetDisplayDebugScreenValue(bool value) {
		UIManager.DisplayDebugScreen = value;
	}

	public void ResetAllData() {
		UIManager.ConfirmResetAllData();
		UIManager.OnConfirmationConfirmed.AddListener(() => {
			RestoreDefaults();
			PlayerPrefs.DeleteAll();
			UIManager.OnBackCompleted.AddListener(UIManager.AlertAllDataReset);
		});
	}



	// Methods

	public void RestoreDefaults() {
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
}

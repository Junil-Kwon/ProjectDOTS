using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Options Screen
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI Screen/Options Screen")]
public sealed class OptionsScreen : ScreenBase {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(OptionsScreen))]
	class OptionsScreenEditor : EditorExtensions {
		OptionsScreen I => target as OptionsScreen;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Selected", EditorStyles.boldLabel);
			I.DefaultSelected = ObjectField("Default Selected", I.DefaultSelected);
			Space();

			End();
		}
	}
	#endif



	// Properties

	public override bool IsPrimary => false;
	public override bool IsOverlay => true;
	public override bool UseScreenBlur => true;



	// General Methods

	public void Language(int value) {
		LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[value];
	}

	public void Language(CustomDropdown dropdown) {
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

	public void FullScreen(bool value) {
		var mode = value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
		UnityEngine.Screen.fullScreenMode = mode;
	}

	public void FullScreen(CustomToggle toggle) {
		StartCoroutine(FullScreenCoroutine(toggle));
	}

	IEnumerator FullScreenCoroutine(CustomToggle toggle) {
		float timeStartChange = Time.realtimeSinceStartup;
		while (Time.realtimeSinceStartup - timeStartChange < 1f) {
			bool value = UnityEngine.Screen.fullScreenMode == FullScreenMode.FullScreenWindow;
			toggle.CurrentValue = value;
			yield return null;
		}
	}

	public void ScreenResolution(int value) {
		var resolution = UIManager.ResolutionPresets[value];
		var fullScreen = UnityEngine.Screen.fullScreen;
		UnityEngine.Screen.SetResolution(resolution.x, resolution.y, fullScreen);
	}

	public void ScreenResolution(CustomStepper stepper) {
		if (stepper.Elements.Length == 0) {
			var elements = new string[UIManager.ResolutionPresets.Length];
			for (int i = 0; i < elements.Length; i++) {
				var resolution = UIManager.ResolutionPresets[i];
				elements[i] = $"{resolution.x}x{resolution.y}";
			}
			stepper.Elements = elements;
		}
	}

	public void MusicVolume(float value) {
		AudioManager.MusicVolume = value;
	}

	public void MusicVolume(CustomSlider slider) {
		slider.CurrentValue = AudioManager.MusicVolume;
	}

	public void SoundFXVolume(float value) {
		AudioManager.SoundFXVolume = value;
	}

	public void SoundFXVolume(CustomSlider slider) {
		slider.CurrentValue = AudioManager.SoundFXVolume;
	}



	// Controls Methods



	// Advanced Methods

}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Button Prompt
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI Widget/Button Prompt")]
[ExecuteAlways]
public class ButtonPrompt : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(ButtonPrompt))]
	class ButtonPromptEditor : EditorExtensions {
		ButtonPrompt I => target as ButtonPrompt;
		public override void OnInspectorGUI() {
			Begin();

			I.PromptImage = ObjectField("Prompt Image", I.PromptImage);
			I.PromptKey   = ObjectField("Prompt Key",   I.PromptKey);
			I.PromptText  = ObjectField("Prompt Text",  I.PromptText);
			I.Padding = Slider("Padding", I.Padding, 0f, 16f);
			I.Margin  = Slider("Margin",  I.Margin,  0f, 16f);
			BeginHorizontal();
			PrefixLabel("Refresh Anchor");
			if (Button("Refresh")) I.Refresh();
			EndHorizontal();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] Image m_PromptImage;
	[SerializeField] TextMeshProUGUI m_PromptKey;
	[SerializeField] TextMeshProUGUI m_PromptText;
	[SerializeField] float m_Padding;
	[SerializeField] float m_Margin;



	// Properties

	Image PromptImage {
		get => m_PromptImage;
		set => m_PromptImage = value;
	}
	TextMeshProUGUI PromptKey {
		get => m_PromptKey;
		set => m_PromptKey = value;
	}
	TextMeshProUGUI PromptText {
		get => m_PromptText;
		set => m_PromptText = value;
	}

	float Padding {
		get => m_Padding;
		set => m_Padding = value;
	}
	float Margin {
		get => m_Margin;
		set => m_Margin = value;
	}



	// Methods

	void OnLocaleChanged(Locale locale) => Refresh();

	void Refresh() {
		var textTransform = PromptText.rectTransform;
		float keyWidth = PromptKey.GetPreferredValues(PromptKey.text).x;
		float textWidth = PromptText.GetPreferredValues(PromptText.text).x;
		float textCenter = textTransform.anchoredPosition.x;
		float textLeft = PromptText.alignment switch {
			TextAlignmentOptions.TopLeft or TextAlignmentOptions.Left or
			TextAlignmentOptions.BottomLeft or TextAlignmentOptions.BaselineLeft or
			TextAlignmentOptions.MidlineLeft or TextAlignmentOptions.CaplineLeft =>
				textCenter - (textTransform.rect.width * 0.5f),
			TextAlignmentOptions.TopRight or TextAlignmentOptions.Right or
			TextAlignmentOptions.BottomRight or TextAlignmentOptions.BaselineRight or
			TextAlignmentOptions.MidlineRight or TextAlignmentOptions.CaplineRight =>
				textCenter + (textTransform.rect.width * 0.5f) - textWidth,
			_ => textCenter - (textWidth * 0.5f),
		};
		var imageTransform = PromptImage.rectTransform;
		float imageWidth = keyWidth + Padding * 2f;
		float imageCenter = textLeft - Margin - (imageWidth * 0.5f);
		imageTransform.sizeDelta = new Vector2(imageWidth, imageTransform.sizeDelta.y);
		imageTransform.anchoredPosition = new(imageCenter, imageTransform.anchoredPosition.y);
		var keyTransform = PromptKey.rectTransform;
		keyTransform.anchoredPosition = new(imageCenter, keyTransform.anchoredPosition.y);
	}



	// Lifecycle

	void OnEnable() => LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
	void OnDisable() => LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using System;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Alert Screen
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI Screen/Alert Screen")]
public sealed class AlertScreen : ScreenBase {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(AlertScreen))]
	class AlertScreenEditor : EditorExtensions {
		AlertScreen I => target as AlertScreen;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Selected", EditorStyles.boldLabel);
			I.DefaultSelected = ObjectField("Default Selected", I.DefaultSelected);
			Space();

			LabelField("Alert", EditorStyles.boldLabel);
			I.CloseText  = ObjectField("Close Text",  I.CloseText);
			I.CloseImage = ObjectField("Close Image", I.CloseImage);
			I.Margin = FloatField("Margin", I.Margin);
			BeginHorizontal();
			PrefixLabel("Refresh Anchor");
			if (Button("Refresh")) I.RefreshCloseImage();
			EndHorizontal();
			Space();

			LabelField("Localize Event", EditorStyles.boldLabel);
			I.ContentStringEvent = ObjectField("Content String Event", I.ContentStringEvent);
			I.CloseStringEvent   = ObjectField("Close String Event",   I.CloseStringEvent);
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] TextMeshProUGUI m_CloseText;
	[SerializeField] Image m_CloseImage;
	[SerializeField] float m_Margin;

	[SerializeField] LocalizeStringEvent m_ContentStringEvent;
	[SerializeField] LocalizeStringEvent m_CloseStringEvent;

	Action m_OnClosed;



	// Properties

	public override bool IsPrimary => false;
	public override bool IsOverlay => true;
	public override bool UseScreenBlur => true;



	TextMeshProUGUI CloseText {
		get => m_CloseText;
		set => m_CloseText = value;
	}
	Image CloseImage {
		get => m_CloseImage;
		set => m_CloseImage = value;
	}
	float Margin {
		get => m_Margin;
		set => m_Margin = value;
	}



	LocalizeStringEvent ContentStringEvent {
		get => m_ContentStringEvent;
		set => m_ContentStringEvent = value;
	}
	LocalizeStringEvent CloseStringEvent {
		get => m_CloseStringEvent;
		set => m_CloseStringEvent = value;
	}

	public LocalizedString ContentReference {
		get => ContentStringEvent.StringReference;
		set => ContentStringEvent.StringReference = value;
	}
	public LocalizedString CloseReference {
		get => CloseStringEvent.StringReference;
		set => CloseStringEvent.StringReference = value;
	}

	public Action OnClosed {
		get => m_OnClosed;
		set => m_OnClosed = value;
	}



	// Methods

	void OnCloseStringUpdated(string value) => RefreshCloseImage();

	void RefreshCloseImage() {
		var textTransform = CloseText.rectTransform;
		var iconTransform = CloseImage.rectTransform;
		float textWidth = CloseText.GetPreferredValues(CloseText.text).x;
		float iconWidth = iconTransform.rect.width;
		float textRight = textTransform.anchoredPosition.x + (textWidth * 0.5f);
		float iconCenter = textRight + Margin + (iconWidth * 0.5f);
		iconTransform.anchoredPosition = new(iconCenter, iconTransform.anchoredPosition.y);
	}



	public void Close() {
		UIManager.CloseScreen(this);
		OnClosed?.Invoke();
		OnClosed = null;
	}

	public override void Back() {
		Close();
	}



	// Lifecycle

	void OnEnable() {
		RefreshCloseImage();
		CloseStringEvent.OnUpdateString.AddListener(OnCloseStringUpdated);
	}

	void OnDisable() {
		CloseStringEvent.OnUpdateString.RemoveListener(OnCloseStringUpdated);
	}
}

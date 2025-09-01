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
//  Confirmation Screen
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI Screen/Confirmation Screen")]
public sealed class ConfirmationScreen : ScreenBase {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(ConfirmationScreen))]
	class ConfirmationScreenEditor : EditorExtensions {
		ConfirmationScreen I => target as ConfirmationScreen;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Selected", EditorStyles.boldLabel);
			I.DefaultSelected = ObjectField("Default Selected", I.DefaultSelected);
			Space();

			LabelField("Confirmation", EditorStyles.boldLabel);
			I.ConfirmText  = ObjectField("Confirm Text",  I.ConfirmText);
			I.CancelText   = ObjectField("Cancel Text",   I.CancelText);
			I.ConfirmImage = ObjectField("Confirm Image", I.ConfirmImage);
			I.CancelImage  = ObjectField("Cancel Image",  I.CancelImage);
			I.Margin = FloatField("Margin", I.Margin);
			BeginHorizontal();
			PrefixLabel("Refresh Anchor");
			if (Button("Refresh")) {
				I.RefreshConfirmImage();
				I.RefreshCancelImage();
			}
			EndHorizontal();
			Space();

			LabelField("Localize Event", EditorStyles.boldLabel);
			I.HeaderStringEvent  = ObjectField("Header String Event",  I.HeaderStringEvent);
			I.ContentStringEvent = ObjectField("Content String Event", I.ContentStringEvent);
			I.ConfirmStringEvent = ObjectField("Confirm String Event", I.ConfirmStringEvent);
			I.CancelStringEvent  = ObjectField("Cancel String Event",  I.CancelStringEvent);
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] TextMeshProUGUI m_ConfirmText;
	[SerializeField] TextMeshProUGUI m_CancelText;
	[SerializeField] Image m_ConfirmImage;
	[SerializeField] Image m_CancelImage;
	[SerializeField] float m_Margin;

	[SerializeField] LocalizeStringEvent m_HeaderStringEvent;
	[SerializeField] LocalizeStringEvent m_ContentStringEvent;
	[SerializeField] LocalizeStringEvent m_ConfirmStringEvent;
	[SerializeField] LocalizeStringEvent m_CancelStringEvent;

	Action m_OnConfirmed;
	Action m_OnCancelled;



	// Properties

	public override bool IsPrimary => false;
	public override bool IsOverlay => true;
	public override bool UseScreenBlur => true;



	TextMeshProUGUI ConfirmText {
		get => m_ConfirmText;
		set => m_ConfirmText = value;
	}
	TextMeshProUGUI CancelText {
		get => m_CancelText;
		set => m_CancelText = value;
	}
	Image ConfirmImage {
		get => m_ConfirmImage;
		set => m_ConfirmImage = value;
	}
	Image CancelImage {
		get => m_CancelImage;
		set => m_CancelImage = value;
	}
	float Margin {
		get => m_Margin;
		set => m_Margin = value;
	}



	LocalizeStringEvent HeaderStringEvent {
		get => m_HeaderStringEvent;
		set => m_HeaderStringEvent = value;
	}
	LocalizeStringEvent ContentStringEvent {
		get => m_ContentStringEvent;
		set => m_ContentStringEvent = value;
	}
	LocalizeStringEvent ConfirmStringEvent {
		get => m_ConfirmStringEvent;
		set => m_ConfirmStringEvent = value;
	}
	LocalizeStringEvent CancelStringEvent {
		get => m_CancelStringEvent;
		set => m_CancelStringEvent = value;
	}

	public LocalizedString HeaderReference {
		get => HeaderStringEvent.StringReference;
		set => HeaderStringEvent.StringReference = value;
	}
	public LocalizedString ContentReference {
		get => ContentStringEvent.StringReference;
		set => ContentStringEvent.StringReference = value;
	}
	public LocalizedString ConfirmReference {
		get => ConfirmStringEvent.StringReference;
		set => ConfirmStringEvent.StringReference = value;
	}
	public LocalizedString CancelReference {
		get => CancelStringEvent.StringReference;
		set => CancelStringEvent.StringReference = value;
	}

	public Action OnConfirmed {
		get => m_OnConfirmed;
		set => m_OnConfirmed = value;
	}
	public Action OnCancelled {
		get => m_OnCancelled;
		set => m_OnCancelled = value;
	}



	// Methods

	void OnConfirmStringUpdated(string value) => RefreshConfirmImage();

	void RefreshConfirmImage() {
		var textTransform = ConfirmText.rectTransform;
		var iconTransform = ConfirmImage.rectTransform;
		float textWidth = ConfirmText.GetPreferredValues(ConfirmText.text).x;
		float iconWidth = iconTransform.rect.width;
		float textRight = textTransform.anchoredPosition.x + (textWidth * 0.5f);
		float iconCenter = textRight + Margin + (iconWidth * 0.5f);
		iconTransform.anchoredPosition = new(iconCenter, iconTransform.anchoredPosition.y);
	}

	void OnCancelStringUpdated(string value) => RefreshCancelImage();

	void RefreshCancelImage() {
		var textTransform = CancelText.rectTransform;
		var iconTransform = CancelImage.rectTransform;
		float textWidth = CancelText.GetPreferredValues(CancelText.text).x;
		float iconWidth = iconTransform.rect.width;
		float textRight = textTransform.anchoredPosition.x + (textWidth * 0.5f);
		float iconCenter = textRight + Margin + (iconWidth * 0.5f);
		iconTransform.anchoredPosition = new(iconCenter, iconTransform.anchoredPosition.y);
	}



	public void Confirm() {
		UIManager.CloseScreen(this);
		OnConfirmed?.Invoke();
		OnConfirmed = null;
		OnCancelled = null;
	}

	public void Cancel() {
		UIManager.CloseScreen(this);
		OnCancelled?.Invoke();
		OnConfirmed = null;
		OnCancelled = null;
	}

	public override void Show() {
		CurrentSelected = DefaultSelected;
		base.Show();
	}

	public override void Back() {
		Cancel();
	}



	// Lifecycle

	void OnEnable() {
		RefreshConfirmImage();
		RefreshCancelImage();
		ConfirmStringEvent.OnUpdateString.AddListener(OnConfirmStringUpdated);
		CancelStringEvent.OnUpdateString.AddListener(OnCancelStringUpdated);
	}

	void OnDisable() {
		ConfirmStringEvent.OnUpdateString.RemoveListener(OnConfirmStringUpdated);
		CancelStringEvent.OnUpdateString.RemoveListener(OnCancelStringUpdated);
	}
}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Custom Toggle
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI Widget/Custom Toggle")]
public class CustomToggle : Selectable, IWidgetBase, IPointerClickHandler, ISubmitHandler {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(CustomToggle))]
	class CustomToggleEditor : SelectableEditorExtensions {
		CustomToggle I => target as CustomToggle;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Selectable", EditorStyles.boldLabel);
			base.OnInspectorGUI();
			Space();

			LabelField("Custom Toggle", EditorStyles.boldLabel);
			I.PositiveImage = ObjectField("Positive Image", I.PositiveImage);
			I.NegativeImage = ObjectField("Negative Image", I.NegativeImage);
			I.PositiveText  = ObjectField("Positive Text",  I.PositiveText);
			I.NegativeText  = ObjectField("Negative Text" , I.NegativeText);
			I.RestoreButton = ObjectField("Restore Button", I.RestoreButton);
			Space();
			I.DefaultValue = Toggle("Default Value", I.DefaultValue);
			I.CurrentValue = Toggle("Current Value", I.CurrentValue);
			Space();
			PropertyField("m_OnValueChanged");
			PropertyField("m_OnRefreshed");
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] GameObject m_PositiveImage;
	[SerializeField] GameObject m_NegativeImage;
	[SerializeField] TextMeshProUGUI m_PositiveText;
	[SerializeField] TextMeshProUGUI m_NegativeText;
	[SerializeField] GameObject m_RestoreButton;

	[SerializeField] bool m_DefaultValue;
	[SerializeField] bool m_CurrentValue;

	[SerializeField] UnityEvent<bool> m_OnValueChanged = new();
	[SerializeField] UnityEvent<CustomToggle> m_OnRefreshed = new();



	// Properties

	GameObject PositiveImage {
		get => m_PositiveImage;
		set => m_PositiveImage = value;
	}
	GameObject NegativeImage {
		get => m_NegativeImage;
		set => m_NegativeImage = value;
	}
	TextMeshProUGUI PositiveText {
		get => m_PositiveText;
		set => m_PositiveText = value;
	}
	TextMeshProUGUI NegativeText {
		get => m_NegativeText;
		set => m_NegativeText = value;
	}
	GameObject RestoreButton {
		get => m_RestoreButton;
		set => m_RestoreButton = value;
	}



	public bool DefaultValue {
		get => m_DefaultValue;
		set {
			if (m_DefaultValue != value) {
				m_DefaultValue = value;
				CurrentValue = value;
			}
		}
	}
	public bool CurrentValue {
		get => m_CurrentValue;
		set {
			if (m_CurrentValue != value) {
				m_CurrentValue = value;
				OnValueChanged.Invoke(value);
				Refresh();
			}
		}
	}



	public UnityEvent<bool> OnValueChanged {
		get => m_OnValueChanged;
	}
	public UnityEvent<CustomToggle> OnRefreshed {
		get => m_OnRefreshed;
	}



	// Methods

	public void Refresh() {
		if (PositiveImage) PositiveImage.SetActive(CurrentValue);
		if (NegativeImage) NegativeImage.SetActive(!CurrentValue);
		if (PositiveText) PositiveText.gameObject.SetActive(CurrentValue);
		if (NegativeText) NegativeText.gameObject.SetActive(!CurrentValue);
		if (RestoreButton) RestoreButton.SetActive(CurrentValue != DefaultValue);
		OnRefreshed.Invoke(this);
	}

	public void Restore() {
		if (RestoreButton) CurrentValue = DefaultValue;
	}



	// Event Handlers

	public void OnPointerClick(PointerEventData eventData) {
		if (interactable) CurrentValue = !CurrentValue;
	}

	public void OnSubmit(BaseEventData eventData) {
		if (interactable) CurrentValue = !CurrentValue;
	}



	// Lifecycle

	protected override void OnEnable() {
		base.OnEnable();
		Refresh();
	}
}

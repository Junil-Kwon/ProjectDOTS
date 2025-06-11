using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Custom Dropdown
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI/Custom Dropdown")]
public class CustomDropdown : TMP_Dropdown, IBaseWidget {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(CustomDropdown))]
	class CustomDropdownEditor : EditorExtensionsSelectable {
		CustomDropdown I => target as CustomDropdown;
		public override void OnInspectorGUI() {
			Begin("Custom Dropdown");

			LabelField("Selectable", EditorStyles.boldLabel);
			base.OnInspectorGUI();
			Space();
			LabelField("Dropdown", EditorStyles.boldLabel);
			I.HighlightImage = ObjectField("HighlightImage", I.HighlightImage);
			I.RestoreButton  = ObjectField("Restore Button", I.RestoreButton);
			I.Template       = ObjectField("Template",       I.Template);
			I.FadeDuration   = FloatField ("Fade Duration",  I.FadeDuration);
			Space();
			I.CaptionUGUI = ObjectField("Caption UGUI", I.CaptionUGUI);
			I.ItemUGUI    = ObjectField("Item UGUI",    I.ItemUGUI);
			Space();
			PropertyField("m_Elements");
			Space();
			I.Default = IntField("Default", I.Default);
			I.Value   = IntField("Value",   I.Value);
			Space();
			PropertyField("m_OnStateUpdated");
			PropertyField("m_OnValueChanged");
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] GameObject m_HighlightImage;
	[SerializeField] GameObject m_RestoreButton;

	[SerializeField] string[] m_Elements = new[] { "Option 1", "Option 2", "Option 3", };

	[SerializeField] int m_Default;

	[SerializeField] UnityEvent<CustomDropdown> m_OnStateUpdated = new();



	// Properties

	GameObject HighlightImage {
		get => m_HighlightImage;
		set => m_HighlightImage = value;
	}
	GameObject RestoreButton {
		get => m_RestoreButton;
		set => m_RestoreButton = value;
	}
	RectTransform Template {
		get => template;
		set => template = value;
	}
	float FadeDuration {
		get => alphaFadeSpeed;
		set => alphaFadeSpeed = value;
	}

	TextMeshProUGUI CaptionUGUI {
		get => captionText as TextMeshProUGUI;
		set => captionText = value;
	}
	TextMeshProUGUI ItemUGUI {
		get => itemText as TextMeshProUGUI;
		set => itemText = value;
	}
	


	public string[] Elements {
		get => m_Elements;
		set {
			if (m_Elements != value) {
				m_Elements = value;
				options.Clear();
				foreach (var element in value) options.Add(new OptionData(element));
				Default = Default;
				Value = Value;
				Refresh();
			}
		}
	}

	public int Default {
		get => m_Default;
		set {
			value = Mathf.Max(0, Mathf.Min(value, Elements.Length - 1));
			if (m_Default != value) {
				m_Default = value;
				Value = value;
			}
		}
	}
	public int Value {
		get => value;
		set {
			value = Mathf.Max(0, Mathf.Min(value, Elements.Length - 1));
			if (this.value != value) {
				this.value = value;
				OnValueChanged.Invoke(value);
				Refresh();
			}
		}
	}

	public UnityEvent<CustomDropdown> OnStateUpdated => m_OnStateUpdated;
	public UnityEvent<int>            OnValueChanged => onValueChanged;



	// Methods

	public void Refresh() {
		if (RestoreButton) RestoreButton.SetActive(Value != Default);
		OnStateUpdated.Invoke(this);
	}

	public void Restore() {
		if (RestoreButton) Value = Default;
	}



	// Event Handlers

	public override void OnPointerClick(PointerEventData eventData) {
		if (HighlightImage) HighlightImage.SetActive(!IsExpanded);
		base.OnPointerClick(eventData);
	}

	public override void OnPointerEnter(PointerEventData eventData) {
		if (HighlightImage) HighlightImage.SetActive(!IsExpanded);
		base.OnPointerEnter(eventData);
	}

	public override void OnPointerExit(PointerEventData eventData) {
		if (HighlightImage) HighlightImage.SetActive(!IsExpanded);
		base.OnPointerExit(eventData);
	}

	public override void OnSelect(BaseEventData eventData) {
		if (HighlightImage) HighlightImage.SetActive(!IsExpanded);
		base.OnSelect(eventData);
	}

	public override void OnDeselect(BaseEventData eventData) {
		if (HighlightImage) HighlightImage.SetActive(!IsExpanded);
		base.OnDeselect(eventData);
	}

	public override void OnSubmit(BaseEventData eventData) {
		if (HighlightImage) HighlightImage.SetActive(!IsExpanded);
		base.OnSubmit(eventData);
	}

	public override void OnCancel(BaseEventData eventData) {
		if (HighlightImage) HighlightImage.SetActive(!IsExpanded);
		base.OnCancel(eventData);
	}

	public override void OnPointerDown(PointerEventData eventData) {
		if (HighlightImage) HighlightImage.SetActive(!IsExpanded);
		base.OnPointerDown(eventData);
	}

	public override void OnPointerUp(PointerEventData eventData) {
		if (HighlightImage) HighlightImage.SetActive(!IsExpanded);
		base.OnPointerUp(eventData);
	}



	// Lifecycle

	protected override void OnEnable() {
		base.OnEnable();
		Refresh();
	}
}

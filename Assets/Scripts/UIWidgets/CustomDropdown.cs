using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Custom Dropdown
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI Widget/Custom Dropdown")]
public class CustomDropdown : TMP_Dropdown, IWidgetBase {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(CustomDropdown))]
	class CustomDropdownEditor : EditorExtensionsSelectable {
		CustomDropdown I => target as CustomDropdown;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Selectable", EditorStyles.boldLabel);
			base.OnInspectorGUI();
			Space();

			LabelField("Custom Dropdown", EditorStyles.boldLabel);
			I.ContentText   = ObjectField("Content Text",   I.ContentText);
			I.ItemText      = ObjectField("Item Text",      I.ItemText);
			I.Template      = ObjectField("Template",       I.Template);
			I.RestoreButton = ObjectField("Restore Button", I.RestoreButton);
			Space();
			I.FadeDuration = FloatField("Fade Duration", I.FadeDuration);
			Space();
			PropertyField("m_Elements");
			I.DefaultValue = IntField("Default Value", I.DefaultValue);
			I.CurrentValue = IntField("Current Value", I.CurrentValue);
			Space();
			PropertyField("m_OnValueChanged");
			PropertyField("m_OnRefreshed");
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] GameObject m_RestoreButton;
	GameObject m_BlockerObject;
	bool m_IsHighlighting;

	[SerializeField] string[] m_Elements = new[] { "Element 1", "Element 2", "Element 3", };
	[SerializeField] int m_DefaultValue;

	[SerializeField] UnityEvent<CustomDropdown> m_OnRefreshed = new();



	// Properties

	TextMeshProUGUI ContentText {
		get => captionText as TextMeshProUGUI;
		set => captionText = value;
	}
	TextMeshProUGUI ItemText {
		get => itemText as TextMeshProUGUI;
		set => itemText = value;
	}
	RectTransform Template {
		get => template;
		set => template = value;
	}
	GameObject RestoreButton {
		get => m_RestoreButton;
		set => m_RestoreButton = value;
	}

	bool IsHighlighting {
		get => m_IsHighlighting;
		set => m_IsHighlighting = value;
	}
	float FadeDuration {
		get => alphaFadeSpeed;
		set => alphaFadeSpeed = value;
	}
	


	public string[] Elements {
		get => m_Elements;
		set {
			if (m_Elements != value) {
				m_Elements = value;
				options.Clear();
				foreach (var element in value) options.Add(new OptionData(element));
				DefaultValue = DefaultValue;
				CurrentValue = CurrentValue;
				Refresh();
			}
		}
	}
	public int DefaultValue {
		get => m_DefaultValue;
		set {
			value = Mathf.Max(0, Mathf.Min(value, Elements.Length - 1));
			if (m_DefaultValue != value) {
				m_DefaultValue = value;
				CurrentValue = value;
			}
		}
	}
	public int CurrentValue {
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



	public UnityEvent<int> OnValueChanged {
		get => onValueChanged;
	}
	public UnityEvent<CustomDropdown> OnRefreshed {
		get => m_OnRefreshed;
	}



	// Methods

	public void Refresh() {
		if (RestoreButton) RestoreButton.SetActive(CurrentValue != DefaultValue);
		OnRefreshed.Invoke(this);
	}

	public void Restore() {
		if (RestoreButton) CurrentValue = DefaultValue;
	}



	// Event Handlers

	public override void OnPointerEnter(PointerEventData eventData) {
		if (interactable) {
			if (!IsExpanded && UIManager.Selected != this) UIManager.Selected = this;
			base.OnPointerEnter(eventData);
		}
	}

	public override void OnPointerExit(PointerEventData eventData) {
		if (interactable) {
			if (!IsExpanded && UIManager.Selected == this) UIManager.Selected = null;
			base.OnPointerExit(eventData);
		}
	}



	// Lifecycle

	protected override void OnEnable() {
		base.OnEnable();
		Refresh();
	}

	void Update() {
		if (IsExpanded) {
			IsHighlighting = true;
			DoStateTransition(SelectionState.Highlighted, true);
		} else if (IsHighlighting) {
			IsHighlighting = false;
			if (InputManager.IsPointerMode) {
				var transform = (RectTransform)this.transform;
				var position = InputManager.PointPosition;
				if (!RectTransformUtility.RectangleContainsScreenPoint(transform, position, null)) {
					UIManager.Selected = null;
					DoStateTransition(SelectionState.Normal, false);
				}
			}
		}
	}
}

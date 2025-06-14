using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Custom Selector
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI/Custom Selector")]
public class CustomSelector : Selectable, IBaseWidget, IUpdateSelectedHandler {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(CustomSelector))]
	class CustomSelectorEditor : EditorExtensionsSelectable {
		CustomSelector I => target as CustomSelector;
		public override void OnInspectorGUI() {
			Begin("Custom Selector");

			LabelField("Selectable", EditorStyles.boldLabel);
			base.OnInspectorGUI();
			Space();
			LabelField("Selector", EditorStyles.boldLabel);
			I.Template      = ObjectField("Template",       I.Template);
			I.RestoreButton = ObjectField("Restore Button", I.RestoreButton);
			Space();
			PropertyField("m_Elements");
			Space();
			I.Default     = IntField("Default",      I.Default);
			I.Value       = IntField("Value",        I.Value);
			I.MultiSelect = Toggle  ("Multi Select", I.MultiSelect);
			Space();
			PropertyField("m_OnStateUpdated");
			PropertyField("m_OnValueChanged");
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] RectTransform m_Template;
	[SerializeField] GameObject    m_RestoreButton;

	[SerializeField] string[] m_Elements = new[] { "Option 1", "Option 2", "Option 3", };

	[SerializeField] int  m_Default;
	[SerializeField] int  m_Value;
	[SerializeField] bool m_MultiSelect;

	[SerializeField] UnityEvent<CustomSelector> m_OnStateUpdated = new();
	[SerializeField] UnityEvent<int>            m_OnValueChanged = new();



	// Properties

	RectTransform Template {
		get => m_Template;
		set => m_Template = value;
	}
	GameObject RestoreButton {
		get => m_RestoreButton;
		set => m_RestoreButton = value;
	}



	public string[] Elements {
		get => m_Elements;
		set {
			if (m_Elements != value) {
				m_Elements = value;
				Default = Default;
				Value = Value;
				Refresh();
			}
		}
	}

	public int Default {
		get => m_Default;
		set {
			value = MultiSelect switch {
				false => Mathf.Max(0, Mathf.Min(value, Elements.Length - 1)),
				true  => Mathf.Clamp(value, 0, (1 << Elements.Length) - 1),
			};
			if (m_Default != value) {
				m_Default = value;
				Value = value;
			}
		}
	}
	public int Value {
		get => m_Value;
		set {
			value = MultiSelect switch {
				false => Mathf.Max(0, Mathf.Min(value, Elements.Length - 1)),
				true  => Mathf.Clamp(value, 0, (1 << Elements.Length) - 1),
			};
			if (m_Value != value) {
				m_Value = value;
				OnValueChanged.Invoke(value);
				Refresh();
			}
		}
	}
	public bool MultiSelect {
		get => m_MultiSelect;
		set {
			if (m_MultiSelect != value) {
				m_MultiSelect = value;
				Value = Default = 0;
				Refresh();
			}
		}
	}

	public UnityEvent<CustomSelector> OnStateUpdated => m_OnStateUpdated;
	public UnityEvent<int>            OnValueChanged => m_OnValueChanged;



	// Methods

	bool TryGetTemplateIndex(out int index) {
		if (Template) for (int i = 0; i < transform.childCount; i++) {
			if (transform.GetChild(i).gameObject == Template.gameObject) {
				index = i;
				return true;
			}
		}
		index = -1;
		return false;
	}

	void UpdateTemplateClone(int templateIndex) {
		for (int i = templateIndex + 1 + Elements.Length; i < transform.childCount; i++) {
			DestroyImmediate(transform.GetChild(i).gameObject);
		}
		for (int i = transform.childCount - templateIndex - 1; i < Elements.Length; i++) {
			Instantiate(Template, transform).gameObject.SetActive(true);
		}
		for (int i = 0; i < Elements.Length; i++) {
			var width = (transform as RectTransform).rect.width / Elements.Length;
			var child = transform.GetChild(templateIndex + 1 + i) as RectTransform;
			var min = i * width;
			var max = (Elements.Length - 1 - i) * -width;
			child.offsetMin = new Vector2(min, 0f);
			child.offsetMax = new Vector2(max, 0f);
		}
	}

	public void Refresh() {
		if (TryGetTemplateIndex(out int templateIndex)) {
			UpdateTemplateClone(templateIndex);
			for (int i = 0; i < Elements.Length; i++) {
				var child = transform.GetChild(templateIndex + 1 + i);
				if (child.TryGetComponent(out CustomToggle toggle)) {
					toggle.interactable = interactable && (MultiSelect || i != Value);
					toggle.Value = MultiSelect switch {
						false => Value == i,
						true  => (Value & (1 << i)) != 0,
					};
					if (Application.isPlaying) {
						int index = i;
						toggle.OnValueChanged.RemoveAllListeners();
						toggle.OnValueChanged.AddListener(_ => {
							if (!MultiSelect && !UIManager.IsPointerClicked) UIManager.Selected = this;
						});
						toggle.OnValueChanged.AddListener(MultiSelect switch {
							true => value => Value = value switch {
								false => Value & ~(1 << index),
								true  => Value |  (1 << index),
							},
							false => value => {
								if (value) Value = index;
							},
						});
					}
				}
			}
		}
		if (RestoreButton) RestoreButton.SetActive(Value != Default);
		OnStateUpdated.Invoke(this);
	}

	public void Restore() {
		if (RestoreButton) Value = Default;
	}



	// Event Handlers

	public void OnUpdateSelected(BaseEventData eventData) {
		if (TryGetTemplateIndex(out int templateIndex)) {
			for (int i = 0; i < Elements.Length; i++) {
				var child = transform.GetChild(templateIndex + 1 + i) as RectTransform;
				if (child.TryGetComponent(out CustomToggle toggle) && toggle.interactable) {
					UIManager.Selected = toggle;
					break;
				}
			}
		}
	}



	// Lifecycle

	protected override void OnEnable() {
		base.OnEnable();
		Refresh();
	}
}

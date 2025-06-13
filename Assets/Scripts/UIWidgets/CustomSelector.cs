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
			I.Template = ObjectField("Template", I.Template);
			I.RestoreButton = ObjectField("Restore Button", I.RestoreButton);
			Space();
			PropertyField("m_Elements");
			Space();
			I.DefaultValue = IntField("Default Value", I.DefaultValue);
			I.CurrentValue = IntField("Current Value", I.CurrentValue);
			I.MultiSelect = Toggle("Multi Select", I.MultiSelect);
			Space();
			PropertyField("m_OnValueChanged");
			PropertyField("m_OnRefreshed");
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] RectTransform m_Template;
	[SerializeField] GameObject m_RestoreButton;

	[SerializeField] string[] m_Elements = new[] { "Element 1", "Element 2", "Element 3", };

	[SerializeField] int m_DefaultValue;
	[SerializeField] int m_CurrentValue;
	[SerializeField] bool m_MultiSelect;

	[SerializeField] UnityEvent<int> m_OnValueChanged = new();
	[SerializeField] UnityEvent<CustomSelector> m_OnRefreshed = new();



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
				DefaultValue = DefaultValue;
				CurrentValue = CurrentValue;
				Refresh();
			}
		}
	}

	public int DefaultValue {
		get => m_DefaultValue;
		set {
			value = MultiSelect switch {
				true  => Mathf.Max(0, Mathf.Min(value, (1 << Elements.Length) - 1)),
				false => Mathf.Max(0, Mathf.Min(value, Elements.Length - 1)),
			};
			if (m_DefaultValue != value) {
				m_DefaultValue = value;
				CurrentValue = value;
			}
		}
	}
	public int CurrentValue {
		get => m_CurrentValue;
		set {
			value = MultiSelect switch {
				true  => Mathf.Max(0, Mathf.Min(value, (1 << Elements.Length) - 1)),
				false => Mathf.Max(0, Mathf.Min(value, Elements.Length - 1)),
			};
			if (m_CurrentValue != value) {
				m_CurrentValue = value;
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
				DefaultValue = 0;
				CurrentValue = 0;
				Refresh();
			}
		}
	}

	public UnityEvent<int> OnValueChanged => m_OnValueChanged;
	public UnityEvent<CustomSelector> OnRefreshed => m_OnRefreshed;



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
			var child = transform.GetChild(templateIndex + 1 + i) as RectTransform;
			float width = (transform as RectTransform).rect.width / Elements.Length;
			float min = i * width;
			float max = (Elements.Length - 1 - i) * -width;
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
					toggle.interactable = interactable && (MultiSelect || i != CurrentValue);
					toggle.CurrentValue = MultiSelect switch {
						true  => (CurrentValue & (1 << i)) != 0,
						false => CurrentValue == i,
					};
					int index = i;
					toggle.OnValueChanged.RemoveAllListeners();
					toggle.OnValueChanged.AddListener(MultiSelect switch {
						true => value => {
							CurrentValue = value switch {
								true  => CurrentValue |  (1 << index),
								false => CurrentValue & ~(1 << index),
							};
						},
						false => value => {
							UIManager.Selected = this;
							if (value) CurrentValue = index;
						},
					});
				}
			}
		}
		if (RestoreButton) RestoreButton.SetActive(CurrentValue != DefaultValue);
		OnRefreshed.Invoke(this);
	}

	public void Restore() {
		if (RestoreButton) CurrentValue = DefaultValue;
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

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
public class CustomSelector : Selectable, IPointerClickHandler {

    // Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(CustomSelector))]
		class CustomSelectorEditor : SelectableEditorExtensions {
			CustomSelector I => target as CustomSelector;
			public override void OnInspectorGUI() {
				Begin("Custom Selector");

				LabelField("Selectable", EditorStyles.boldLabel);
				base.OnInspectorGUI();
				Space();
				LabelField("Selector Layout", EditorStyles.boldLabel);
                I.Template      = ObjectField("Template",       I.Template);
				I.RestoreButton = ObjectField("Restore Button", I.RestoreButton);
				Space();

				Space();
				LabelField("Selector Event", EditorStyles.boldLabel);
				PropertyField("m_Elements");
				I.MultiSelect = Toggle  ("Multi Select", I.MultiSelect);
				I.Default     = IntField("Default",      I.Default);
				I.Value       = IntField("Value",        I.Value);
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
    [SerializeField] bool m_MultiSelect = false;
    [SerializeField] int  m_Default = 0;
	[SerializeField] int  m_Value   = 0;

    [SerializeField] UnityEvent<CustomSelector> m_OnStateUpdated = new();
	[SerializeField] UnityEvent<int            > m_OnValueChanged = new();



    // Properties

    public RectTransform Transform => transform as RectTransform;

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
			m_Elements = value;
			Default = Default;
			Value = Value;
			Refresh();
		}
	}
	public bool MultiSelect {
		get => m_MultiSelect;
		set {
			if (m_MultiSelect != value) {
				m_MultiSelect = value;
				Default = 0;
				Value = 0;
				Refresh();
			}
		}
	}
	public int Default {
		get => m_Default;
		set {
			value = MultiSelect switch {
				true  => Mathf.Clamp(value, 0, (1 << Elements.Length) - 1),
				false => Mathf.Max(0, Mathf.Min(value, Elements.Length - 1)),
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
				true  => Mathf.Clamp(value, 0, (1 << Elements.Length) - 1),
				false => Mathf.Max(0, Mathf.Min(value, Elements.Length - 1)),
			};
			if (m_Value != value) {
				m_Value = value;
				OnValueChanged.Invoke(value);
				Refresh();
			}
		}
	}

    public UnityEvent<CustomSelector> OnStateUpdated => m_OnStateUpdated;
    public UnityEvent<int            > OnValueChanged => m_OnValueChanged;



    // Methods

	public void Refresh() {
		int templateIndex = -1;
		if (Template) for (int i = 0; i < transform.childCount; i++) {
			if (transform.GetChild(i).gameObject == Template.gameObject) {
				templateIndex = i;
				break;
			}
		}
		if (templateIndex != -1) {
			for (int i = transform.childCount - templateIndex - 1; i < Elements.Length; i++) {
				var transform = Instantiate(Template, this.transform);
				if (transform.TryGetComponent(out Selectable selectable)) {
					selectable.interactable = interactable;
				}
			}
			for (int i = templateIndex + 1 + Elements.Length; i < transform.childCount; i++) {
				DestroyImmediate(transform.GetChild(i).gameObject);
			}
			for (int i = 0; i < Elements.Length; i++) {
				float width = Transform.rect.width / Elements.Length;
				var transform = this.transform.GetChild(templateIndex + 1 + i) as RectTransform;
				transform.gameObject.SetActive(true);
				transform.offsetMin = new Vector2((0                   + i) *  width, 0);
				transform.offsetMax = new Vector2((Elements.Length - 1 - i) * -width, 0);
				if (transform.TryGetComponent(out CustomToggle toggle)) {
					int index = i;
					toggle.Value = MultiSelect switch {
						true  => (Value & (1 << index)) != 0,
						false => Value == index,
					};
					toggle.interactable = interactable && !MultiSelect && index != Value;
					toggle.OnValueChanged.RemoveAllListeners();
					toggle.OnValueChanged.AddListener(MultiSelect switch {
						true => value => Value = value switch {
							true  => Value |  (1 << index),
							false => Value & ~(1 << index),
						},
						false => value => {
							if (value) Value = index;
						},
					});
				}
			}
		}
		if (RestoreButton) {
			RestoreButton.SetActive(Value != Default);
		}
		OnStateUpdated.Invoke(this);
	}

	public void Restore() {
		Value = Default;
	}



    // Event Handlers

	public void OnPointerClick(PointerEventData eventData) {
		if (interactable) {
		}
	}

	public void OnSubmit() {
		if (interactable) {
			Value = (Value + 1) % Elements.Length;
		}
	}



    // Lifecycle

	protected override void OnEnable() {
		base.OnEnable();
		Refresh();
	}
}

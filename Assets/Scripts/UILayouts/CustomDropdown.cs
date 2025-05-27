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
public sealed class CustomDropdown : TMP_Dropdown, IUpdateSelectedHandler {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(CustomDropdown))]
		class CustomDropdownEditor : SelectableEditorExtensions {
			CustomDropdown I => target as CustomDropdown;
			public override void OnInspectorGUI() {
				Begin("Custom Dropdown");

				LabelField("Selectable", EditorStyles.boldLabel);
				base.OnInspectorGUI();
				Space();
				LabelField("Dropdown Layout", EditorStyles.boldLabel);
				I.Template = ObjectField("Template", I.Template);
				if (I.Template) I.FadeDuration = FloatField("Fade Duration", I.FadeDuration);
				I.RestoreButton = ObjectField("Restore Button", I.RestoreButton);
				Space();
				I.TextUGUI = ObjectField("Text UGUI", I.TextUGUI);
				I.ItemUGUI = ObjectField("Item UGUI", I.ItemUGUI);
				BeginHorizontal();
				PrefixLabel("Alignment");
				if (I.TextUGUI || I.ItemUGUI) {
					if (Button("Left")) {
						if (I.TextUGUI) I.TextUGUI.alignment = TextAlignmentOptions.Left;
						if (I.ItemUGUI) I.ItemUGUI.alignment = TextAlignmentOptions.Left;
					}
					if (Button("Center")) {
						if (I.TextUGUI) I.TextUGUI.alignment = TextAlignmentOptions.Center;
						if (I.ItemUGUI) I.ItemUGUI.alignment = TextAlignmentOptions.Center;
					}
					if (Button("Right")) {
						if (I.TextUGUI) I.TextUGUI.alignment = TextAlignmentOptions.Right;
						if (I.ItemUGUI) I.ItemUGUI.alignment = TextAlignmentOptions.Right;
					}
				}
				EndHorizontal();
				Space();
				LabelField("Dropdown Event", EditorStyles.boldLabel);
				PropertyField("m_Elements");
				I.Elements = I.Elements;
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

	[SerializeField] GameObject m_RestoreButton;

	[SerializeField] string[] m_Elements = new[] { "Option 1", "Option 2", "Option 3", };
	[SerializeField] int m_Default = 0;

	[SerializeField] UnityEvent<CustomDropdown> m_OnStateUpdated = new();



	// Properties

	public RectTransform Transform => transform as RectTransform;

	RectTransform Template {
		get => template;
		set => template = value;
	}
	float FadeDuration {
		get => alphaFadeSpeed;
		set => alphaFadeSpeed = value;
	}
	GameObject RestoreButton {
		get => m_RestoreButton;
		set => m_RestoreButton = value;
	}

	TextMeshProUGUI TextUGUI {
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
			m_Elements = value;
			options.Clear();
			foreach (var element in value) options.Add(new OptionData(element));
			Default = Default;
		}
	}
	public int Default {
		get => m_Default;
		set {
			value = Mathf.Clamp(value, 0, Elements.Length - 1);
			if (Default == value) return;
			Value = m_Default = value;
		}
	}
	public int Value {
		get => value;
		set {
			value = Mathf.Clamp(value, 0, Elements.Length - 1);
			if (Value == value) return;
			this.value = value;
			OnValueChanged.Invoke(Value);
			Refresh();
		}
	}

	public UnityEvent<CustomDropdown> OnStateUpdated => m_OnStateUpdated;
	public UnityEvent<int           > OnValueChanged => onValueChanged;



	// Methods

	public void Refresh() {
		if (RestoreButton) {
			RestoreButton.SetActive(Value != Default);
		}
		OnStateUpdated.Invoke(this);
	}

	public void Restore() {
		Value = Default;
	}



	// Lifecycle

	protected override void OnEnable() {
		base.OnEnable();
		Refresh();
	}

	public void OnUpdateSelected(BaseEventData eventData) {
		Refresh();
	}
}

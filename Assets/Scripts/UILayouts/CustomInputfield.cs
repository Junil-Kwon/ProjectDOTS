using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

using TMPro;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Custom Inputfield
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI/Custom Inputfield")]
public sealed class CustomInputfield : TMP_InputField {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(CustomInputfield))]
		class CustomInputfieldEditor : SelectableEditorExtensions {
			CustomInputfield I => target as CustomInputfield;
			public override void OnInspectorGUI() {
				Begin("Custom Inputfield");

				LabelField("Selectable", EditorStyles.boldLabel);
				base.OnInspectorGUI();
				Space();
				LabelField("Inputfield Layout", EditorStyles.boldLabel);
				I.AreaRect      = ObjectField("Area Rect",      I.AreaRect);
				I.RestoreButton = ObjectField("Restore Button", I.RestoreButton);
				Space();
				LabelField("Inputfield Event", EditorStyles.boldLabel);
				I.TextType       = EnumField ("Text Type",       I.TextType);
				I.CharacterLimit = IntField  ("Character Limit", I.CharacterLimit);
				I.SelectionColor = ColorField("Selection Color", I.SelectionColor);
				Space();
				I.TextUGUI = ObjectField("Text UGUI", I.TextUGUI);
				if (I.TextUGUI) {
					I.Default = TextField("Default", I.Default);
					I.Value   = TextField("Value",   I.Value);
					BeginHorizontal();
					PrefixLabel("Alignment");
					if (Button("Left"  )) I.TextUGUI.alignment = TextAlignmentOptions.Left;
					if (Button("Center")) I.TextUGUI.alignment = TextAlignmentOptions.Center;
					if (Button("Right" )) I.TextUGUI.alignment = TextAlignmentOptions.Right;
					EndHorizontal();
				}
				I.PlaceHolderUGUI = ObjectField("Place Holder UGUI", I.PlaceHolderUGUI);
				if (I.PlaceHolderUGUI) {
					I.PlaceHolder = TextField("Place Holder", I.PlaceHolder);
					BeginHorizontal();
					PrefixLabel("Alignment");
					if (Button("Left"  )) I.PlaceHolderUGUI.alignment = TextAlignmentOptions.Left;
					if (Button("Center")) I.PlaceHolderUGUI.alignment = TextAlignmentOptions.Center;
					if (Button("Right" )) I.PlaceHolderUGUI.alignment = TextAlignmentOptions.Right;
					EndHorizontal();
				}
				Space();
				PropertyField("m_OnStateUpdated");
				PropertyField("m_OnValueChanged");
				PropertyField("m_OnEndEdit");
				Space();

				End();
			}
		}
	#endif



	// Fields

	[SerializeField] GameObject m_RestoreButton;

	[SerializeField] TextMeshProUGUI m_TMProUGUI;

	[SerializeField] string m_Default = "";

	[SerializeField] UnityEvent<CustomInputfield> m_OnStateUpdated = new();



	// Properties

	public RectTransform Transform => transform as RectTransform;

	RectTransform AreaRect {
		get => m_TextViewport;
		set => m_TextViewport = value;
	}
	GameObject RestoreButton {
		get => m_RestoreButton;
		set => m_RestoreButton = value;
	}

	public TextMeshProUGUI TextUGUI {
		get => m_TextComponent as TextMeshProUGUI;
		set => m_TextComponent = value;
	}
	public TextMeshProUGUI PlaceHolderUGUI {
		get => m_Placeholder as TextMeshProUGUI;
		set => m_Placeholder = value;
	}



	public ContentType TextType {
		get => contentType;
		set => contentType = value;
	}
	public int CharacterLimit {
		get => characterLimit;
		set => characterLimit = value;
	}
	public Color SelectionColor {
		get => selectionColor;
		set => selectionColor = value;
	}

	public string Default {
		get => m_Default;
		set {
			if (m_Default == value) return;
			Value = m_Default = value;
		}
	}
	public string Value {
		get => text;
		set {
			if (text == value) return;
			text = value;
			Refresh();
		}
	}
	public string PlaceHolder {
		get => PlaceHolderUGUI.text;
		set => PlaceHolderUGUI.text = value;
	}

	public UnityEvent<CustomInputfield> OnStateUpdated => m_OnStateUpdated;
	public UnityEvent<string          > OnValueChanged => onValueChanged;
	public UnityEvent<string          > OnEndEdit      => onEndEdit;



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
	}

	public override void OnUpdateSelected(BaseEventData eventData) {
		base.OnUpdateSelected(eventData);
		Refresh();
	}
}

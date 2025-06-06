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
public class CustomInputfield : TMP_InputField, IBaseWidget {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(CustomInputfield))]
		class CustomInputfieldEditor : EditorExtensionsSelectable {
			CustomInputfield I => target as CustomInputfield;
			public override void OnInspectorGUI() {
				Begin("Custom Inputfield");

				LabelField("Selectable", EditorStyles.boldLabel);
				base.OnInspectorGUI();
				Space();
				LabelField("Inputfield", EditorStyles.boldLabel);
				I.AreaRect      = ObjectField("Area Rect",      I.AreaRect);
				I.HideToggle    = ObjectField("Hide Toggle",    I.HideToggle);
				I.RestoreButton = ObjectField("Restore Button", I.RestoreButton);
				Space();
				I.PlaceHolderUGUI = ObjectField("Place Holder UGUI", I.PlaceHolderUGUI);
				if (I.PlaceHolderUGUI) {
					I.PlaceHolder = TextField("Place Holder", I.PlaceHolder);
					BeginHorizontal();
					PrefixLabel("Place Holder Alignment");
					if (Button("Left"  )) I.PlaceHolderUGUI.alignment = TextAlignmentOptions.Left;
					if (Button("Center")) I.PlaceHolderUGUI.alignment = TextAlignmentOptions.Center;
					if (Button("Right" )) I.PlaceHolderUGUI.alignment = TextAlignmentOptions.Right;
					EndHorizontal();
				}
				I.TextUGUI = ObjectField("Text UGUI", I.TextUGUI);
				if (I.TextUGUI) {
					I.TextSelectionColor = ColorField("Text Selection Color", I.TextSelectionColor);
					BeginHorizontal();
					PrefixLabel("Text Alignment");
					if (Button("Left"  )) I.TextUGUI.alignment = TextAlignmentOptions.Left;
					if (Button("Center")) I.TextUGUI.alignment = TextAlignmentOptions.Center;
					if (Button("Right" )) I.TextUGUI.alignment = TextAlignmentOptions.Right;
					EndHorizontal();
				}
				Space();
				I.Default = TextField ("Default", I.Default);
				I.Value   = TextField ("Value",   I.Value);
				I.contentType = ContentType.Custom;
				I.ValueLimit      = IntField ("Value Limit",      I.ValueLimit);
				I.ValueValidation = EnumField("Value Validation", I.ValueValidation);
				if (I.ValueValidation == CharacterValidation.Regex) PropertyField("m_RegexValue");
				I.MaskValue = Toggle("Mask Value", I.MaskValue);
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

	[SerializeField] CustomToggle m_HideToggle;
	[SerializeField] GameObject m_RestoreButton;

	[SerializeField] string m_Default = string.Empty;

	[SerializeField] UnityEvent<CustomInputfield> m_OnStateUpdated = new();



	// Properties

	RectTransform AreaRect {
		get => m_TextViewport;
		set => m_TextViewport = value;
	}
	CustomToggle HideToggle {
		get => m_HideToggle;
		set => m_HideToggle = value;
	}
	GameObject RestoreButton {
		get => m_RestoreButton;
		set => m_RestoreButton = value;
	}

	TextMeshProUGUI PlaceHolderUGUI {
		get => m_Placeholder as TextMeshProUGUI;
		set => m_Placeholder = value;
	}
	public string PlaceHolder {
		get => PlaceHolderUGUI.text;
		set => PlaceHolderUGUI.text = value;
	}

	TextMeshProUGUI TextUGUI {
		get => m_TextComponent as TextMeshProUGUI;
		set => m_TextComponent = value;
	}
	Color TextSelectionColor {
		get => selectionColor;
		set => selectionColor = value;
	}



	public string Default {
		get => m_Default;
		set {
			if (m_Default != value) {
				m_Default = value;
				Value = value;
			}
		}
	}
	public string Value {
		get => text;
		set {
			if (text != value) {
				text = value;
				Refresh();
			}
		}
	}
	public int ValueLimit {
		get => characterLimit;
		set => characterLimit = value;
	}
	public CharacterValidation ValueValidation {
		get => characterValidation;
		set => characterValidation = value;
	}

	public bool MaskValue {
		get => inputType == InputType.Password;
		set {
			if (MaskValue != value) {
				inputType = value ? InputType.Password : InputType.Standard;
				UpdateLabel();
			}
		}
	}

	public UnityEvent<CustomInputfield> OnStateUpdated => m_OnStateUpdated;
	public UnityEvent<string          > OnValueChanged => onValueChanged;
	public UnityEvent<string          > OnEndEdit      => onEndEdit;



	// Methods

	public void Refresh() {
		if (HideToggle) HideToggle.Value = MaskValue;
		if (RestoreButton) RestoreButton.SetActive(Value != Default);
		OnStateUpdated.Invoke(this);
	}

	public void Restore() {
		if (RestoreButton) Value = Default;
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

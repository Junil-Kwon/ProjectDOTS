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
			I.TextViewport  = ObjectField("Text Viewport",  I.TextViewport);
			I.HideToggle    = ObjectField("Hide Toggle",    I.HideToggle);
			I.RestoreButton = ObjectField("Restore Button", I.RestoreButton);
			Space();
			I.PlaceHolderUGUI = ObjectField("Place Holder UGUI", I.PlaceHolderUGUI);
			if (I.PlaceHolderUGUI) I.PlaceHolder = TextField("Place Holder", I.PlaceHolder);
			I.TextUGUI = ObjectField("Text UGUI", I.TextUGUI);
			if (I.TextUGUI) {
				I.contentType = ContentType.Custom;
				I.TextLengthLimit = IntField ("Text Length Limit", I.TextLengthLimit);
				I.TextValidation  = EnumField("Text Validation",   I.TextValidation);
				if (I.TextValidation == CharacterValidation.Regex) PropertyField("m_RegexValue");
				I.TextSelectionColor = ColorField("Text Selection Color", I.TextSelectionColor);
			}
			Space();
			I.Default = TextField("Default", I.Default);
			I.Value   = TextField("Value",   I.Value);
			I.Mask    = Toggle   ("Mask",    I.Mask);
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
	[SerializeField] GameObject   m_RestoreButton;

	[SerializeField] string m_Default;

	[SerializeField] UnityEvent<CustomInputfield> m_OnStateUpdated = new();



	// Properties

	RectTransform TextViewport {
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
	int TextLengthLimit {
		get => characterLimit;
		set => characterLimit = value;
	}
	CharacterValidation TextValidation {
		get => characterValidation;
		set => characterValidation = value;
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
	public bool Mask {
		get => inputType == InputType.Password;
		set {
			if (Mask != value) {
				inputType = value ? InputType.Password : InputType.Standard;
				UpdateLabel();
			}
		}
	}

	public UnityEvent<CustomInputfield> OnStateUpdated => m_OnStateUpdated;
	public UnityEvent<string>           OnValueChanged => onValueChanged;
	public UnityEvent<string>           OnEndEdit      => onEndEdit;



	// Methods

	public void Refresh() {
		if (HideToggle)    HideToggle.Value = Mask;
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

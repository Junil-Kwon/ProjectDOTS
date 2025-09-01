using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Custom Inputfield
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI Widget/Custom Inputfield")]
public class CustomInputfield : TMP_InputField, IWidgetBase {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(CustomInputfield))]
	class CustomInputfieldEditor : EditorExtensionsSelectable {
		CustomInputfield I => target as CustomInputfield;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Selectable", EditorStyles.boldLabel);
			base.OnInspectorGUI();
			Space();

			I.contentType = ContentType.Custom;
			LabelField("Custom Inputfield", EditorStyles.boldLabel);
			I.TextViewport    = ObjectField("Text Viewport",     I.TextViewport);
			I.PlaceHolderUGUI = ObjectField("Place Holder UGUI", I.PlaceHolderUGUI);
			I.TextUGUI        = ObjectField("Text UGUI",         I.TextUGUI);
			I.MaskToggle      = ObjectField("Mask Toggle",       I.MaskToggle);
			I.RestoreButton   = ObjectField("Restore Button",    I.RestoreButton);
			Space();
			I.TextValidation = EnumField("Text Validation", I.TextValidation);
			if (I.TextValidation == CharacterValidation.Regex) PropertyField("m_RegexValue");
			I.TextLengthLimit = IntField("Text Length Limit", I.TextLengthLimit);
			I.TextSelectionColor = ColorField("Text Selection Color", I.TextSelectionColor);
			Space();
			if (I.PlaceHolderUGUI) I.PlaceHolder = TextField("Place Holder", I.PlaceHolder);
			I.DefaultValue = TextField("Default Value", I.DefaultValue);
			I.CurrentValue = TextField("Current Value", I.CurrentValue);
			I.Mask = Toggle("Mask", I.Mask);
			Space();
			PropertyField("m_OnValueChanged");
			PropertyField("m_OnEndEdit");
			PropertyField("m_OnRefreshed");
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] GameObject m_MaskToggle;
	[SerializeField] GameObject m_RestoreButton;

	[SerializeField] string m_DefaultValue;

	[SerializeField] UnityEvent<CustomInputfield> m_OnRefreshed = new();



	// Properties

	RectTransform TextViewport {
		get => m_TextViewport;
		set => m_TextViewport = value;
	}
	TextMeshProUGUI PlaceHolderUGUI {
		get => m_Placeholder as TextMeshProUGUI;
		set => m_Placeholder = value;
	}
	TextMeshProUGUI TextUGUI {
		get => m_TextComponent as TextMeshProUGUI;
		set => m_TextComponent = value;
	}
	GameObject MaskToggle {
		get => m_MaskToggle;
		set => m_MaskToggle = value;
	}
	GameObject RestoreButton {
		get => m_RestoreButton;
		set => m_RestoreButton = value;
	}

	CharacterValidation TextValidation {
		get => characterValidation;
		set => characterValidation = value;
	}
	int TextLengthLimit {
		get => characterLimit;
		set => characterLimit = value;
	}
	Color TextSelectionColor {
		get => selectionColor;
		set => selectionColor = value;
	}



	public string PlaceHolder {
		get => PlaceHolderUGUI.text;
		set => PlaceHolderUGUI.text = value;
	}
	public string DefaultValue {
		get => m_DefaultValue;
		set {
			if (m_DefaultValue != value) {
				m_DefaultValue = value;
				CurrentValue = value;
			}
		}
	}
	public string CurrentValue {
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



	public UnityEvent<string> OnValueChanged {
		get => onValueChanged;
	}
	public UnityEvent<string> OnEndEdit {
		get => onEndEdit;
	}
	public UnityEvent<CustomInputfield> OnRefreshed {
		get => m_OnRefreshed;
	}



	// Methods

	public void Refresh() {
		if (MaskToggle && MaskToggle.TryGetComponent(out IWidgetBase widget)) widget.Refresh();
		if (RestoreButton) RestoreButton.SetActive(CurrentValue != DefaultValue);
		OnRefreshed.Invoke(this);
	}

	public void Restore() {
		if (RestoreButton) CurrentValue = DefaultValue;
	}

	public void RefreshMaskToggle(CustomToggle toggle) {
		toggle.CurrentValue = Mask;
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

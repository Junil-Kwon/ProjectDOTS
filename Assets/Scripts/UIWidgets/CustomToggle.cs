using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

using TMPro;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Custom Toggle
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI/Custom Toggle")]
public class CustomToggle : Selectable, IBaseWidget, IPointerClickHandler, ISubmitHandler {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(CustomToggle))]
		class CustomToggleEditor : EditorExtensionsSelectable {
			CustomToggle I => target as CustomToggle;
			public override void OnInspectorGUI() {
				Begin("Custom Toggle");

				LabelField("Selectable", EditorStyles.boldLabel);
				base.OnInspectorGUI();
				Space();
				LabelField("Toggle", EditorStyles.boldLabel);
				I.PositiveImage    = ObjectField("Positive Image",     I.PositiveImage);
				I.NegativeImage    = ObjectField("Negative Image",     I.NegativeImage);
				I.PositiveTextUGUI = ObjectField("Positive Text UGUI", I.PositiveTextUGUI);
				I.NegativeTextUGUI = ObjectField("Negative Text UGUI", I.NegativeTextUGUI);
				I.RestoreButton    = ObjectField("Restore Button",     I.RestoreButton);
				Space();
				I.Default = Toggle("Default", I.Default);
				I.Value   = Toggle("Value",   I.Value);
				Space();
				PropertyField("m_OnStateUpdated");
				PropertyField("m_OnValueChanged");
				Space();

				End();
			}
		}
	#endif



	// Fields

	[SerializeField] GameObject m_PositiveImage;
	[SerializeField] GameObject m_NegativeImage;
	[SerializeField] TextMeshProUGUI m_PositiveTextUGUI;
	[SerializeField] TextMeshProUGUI m_NegativeTextUGUI;
	[SerializeField] GameObject m_RestoreButton;

	[SerializeField] bool m_Default = false;
	[SerializeField] bool m_Value   = false;

	[SerializeField] UnityEvent<CustomToggle> m_OnStateUpdated = new();
	[SerializeField] UnityEvent<bool        > m_OnValueChanged = new();



	// Properties

	GameObject PositiveImage {
		get => m_PositiveImage;
		set => m_PositiveImage = value;
	}
	GameObject NegativeImage {
		get => m_NegativeImage;
		set => m_NegativeImage = value;
	}
	TextMeshProUGUI PositiveTextUGUI {
		get => m_PositiveTextUGUI;
		set => m_PositiveTextUGUI = value;
	}
	TextMeshProUGUI NegativeTextUGUI {
		get => m_NegativeTextUGUI;
		set => m_NegativeTextUGUI = value;
	}
	GameObject RestoreButton {
		get => m_RestoreButton;
		set => m_RestoreButton = value;
	}



	public bool Default {
		get => m_Default;
		set {
			if (m_Default != value) {
				m_Default = value;
				Value = value;
			}
		}
	}
	public bool Value {
		get => m_Value;
		set {
			if (m_Value != value) {
				m_Value = value;
				OnValueChanged.Invoke(value);
				Refresh();
			}
		}
	}

	public UnityEvent<CustomToggle> OnStateUpdated => m_OnStateUpdated;
	public UnityEvent<bool        > OnValueChanged => m_OnValueChanged;



	// Methods

	public void Refresh() {
		if (PositiveImage) PositiveImage.SetActive( Value);
		if (NegativeImage) NegativeImage.SetActive(!Value);
		if (PositiveTextUGUI) PositiveTextUGUI.gameObject.SetActive( Value);
		if (NegativeTextUGUI) NegativeTextUGUI.gameObject.SetActive(!Value);
		if (RestoreButton) RestoreButton.SetActive(Value != Default);
		OnStateUpdated.Invoke(this);
	}

	public void Restore() {
		if (RestoreButton) Value = Default;
	}



	// Event Handlers

	public void OnPointerClick(PointerEventData eventData) {
		UIManager.IsPointerClicked = true;
		if (interactable) Value = !Value;
	}

	public void OnSubmit(BaseEventData eventData) {
		UIManager.IsPointerClicked = false;
		if (interactable) Value = !Value;
	}



	// Lifecycle

	protected override void OnEnable() {
		base.OnEnable();
		Refresh();
	}
}

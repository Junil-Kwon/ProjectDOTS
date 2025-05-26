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
public class CustomToggle : Selectable, IPointerClickHandler {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(CustomToggle))]
		class CustomToggleEditor : SelectableEditorExtensions {
			CustomToggle I => target as CustomToggle;
			public override void OnInspectorGUI() {
				Begin("Custom Toggle");

				LabelField("Selectable", EditorStyles.boldLabel);
				base.OnInspectorGUI();
				Space();
				LabelField("Toggle UI", EditorStyles.boldLabel);
				I.PositiveImage = ObjectField("Positive Image", I.PositiveImage);
				I.NegativeImage = ObjectField("Negative Image", I.NegativeImage);
				Space();
				LabelField("Toggle Value", EditorStyles.boldLabel);
				I.Value = Toggle("Value", I.Value);
				Space();
				LabelField("Toggle Text", EditorStyles.boldLabel);
				I.PositiveTextUGUI = ObjectField("Positive Text UGUI", I.PositiveTextUGUI);
				I.NegativeTextUGUI = ObjectField("Negative Text UGUI", I.NegativeTextUGUI);
				if (I.PositiveTextUGUI) I.PositiveText = TextField("Positive Text", I.PositiveText);
				if (I.NegativeTextUGUI) I.NegativeText = TextField("Negative Text", I.NegativeText);
				Space();
				LabelField("Toggle Event", EditorStyles.boldLabel);
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

	[SerializeField] bool m_Value = true;

	[SerializeField] UnityEvent<CustomToggle> m_OnStateUpdated;
	[SerializeField] UnityEvent<bool        > m_OnValueChanged;



	// Properties
	
	public RectTransform Transform => transform as RectTransform;

	GameObject PositiveImage {
		get => m_PositiveImage;
		set => m_PositiveImage = value;
	}
	GameObject NegativeImage {
		get => m_NegativeImage;
		set => m_NegativeImage = value;
	}

	public bool Value {
		get => m_Value;
		set {
			if (m_Value == value) return;
			m_Value = value;
			m_OnValueChanged.Invoke(m_Value);
			Refresh();
		}
	}

	TextMeshProUGUI PositiveTextUGUI {
		get => m_PositiveTextUGUI;
		set => m_PositiveTextUGUI = value;
	}
	TextMeshProUGUI NegativeTextUGUI {
		get => m_NegativeTextUGUI;
		set => m_NegativeTextUGUI = value;
	}
	public string PositiveText {
		get => PositiveTextUGUI.text;
		set => PositiveTextUGUI.text = value;
	}
	public string NegativeText {
		get => NegativeTextUGUI.text;
		set => NegativeTextUGUI.text = value;
	}

	public UnityEvent<CustomToggle> OnStateUpdated => m_OnStateUpdated;
	public UnityEvent<bool        > OnValueChanged => m_OnValueChanged;



	// Methods

	public void Refresh() {
		if (PositiveImage) PositiveImage.SetActive( Value);
		if (NegativeImage) NegativeImage.SetActive(!Value);
		if (PositiveTextUGUI) PositiveTextUGUI.gameObject.SetActive( Value);
		if (NegativeTextUGUI) NegativeTextUGUI.gameObject.SetActive(!Value);
		OnStateUpdated.Invoke(this);
	}



	public void OnPointerClick(PointerEventData eventData) {
		if (interactable) Value = !Value;
	}

	public void OnSubmit() {
		if (interactable) {
			DoStateTransition(SelectionState.Pressed, false);
			Value = !Value;
		}
	}

	public override void OnMove(AxisEventData eventData) {
		if (interactable) switch (eventData.moveDir) {
			case MoveDirection.Left:
			case MoveDirection.Right:
				DoStateTransition(SelectionState.Pressed, false);
				Value = !Value;
				return;
		}
		base.OnMove(eventData);
	}



	// Lifecycle

	protected override void OnEnable() {
		base.OnEnable();
		Refresh();
	}
}

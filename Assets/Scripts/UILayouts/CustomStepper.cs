using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

using TMPro;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Custom Stepper
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI/Custom Stepper")]
public class CustomStepper : Selectable, IPointerClickHandler {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(CustomStepper))]
		class CustomStepperEditor : SelectableEditorExtensions {
			CustomStepper I => target as CustomStepper;
			public override void OnInspectorGUI() {
				Begin("Custom Stepper");

				LabelField("Selectable", EditorStyles.boldLabel);
				base.OnInspectorGUI();
				Space();
				LabelField("Stepper UI", EditorStyles.boldLabel);
				I.LeftArrow  = ObjectField("Left Arrow ", I.LeftArrow);
				I.RightArrow = ObjectField("Right Arrow", I.RightArrow);
				Space();
				LabelField("Stepper Value", EditorStyles.boldLabel);
				PropertyField("m_TextArray");
				I.Value = IntField("Value", I.Value);
				I.Loop  = Toggle  ("Loop",  I.Loop);
				Space();
				LabelField("Stepper Text", EditorStyles.boldLabel);
				I.TextMeshProUGUI = ObjectField("TMPro UGUI", I.TextMeshProUGUI);
				if (I.TextMeshProUGUI) {
					I.Text = TextField("Text", I.Text);
					BeginHorizontal();
					PrefixLabel("Alignment");
					if (Button("Left"  )) I.TextMeshProUGUI.alignment = TextAlignmentOptions.Left;
					if (Button("Center")) I.TextMeshProUGUI.alignment = TextAlignmentOptions.Center;
					if (Button("Right" )) I.TextMeshProUGUI.alignment = TextAlignmentOptions.Right;
					EndHorizontal();
				}
				Space();
				LabelField("Stepper Event", EditorStyles.boldLabel);
				PropertyField("m_OnStateUpdated");
				PropertyField("m_OnValueChanged");
				Space();

				End();
			}
		}
	#endif



	// Fields

	[SerializeField] GameObject m_LeftArrow;
	[SerializeField] GameObject m_RightArrow;

	[SerializeField] string[] m_TextArray = new string[] { "Prev", "Next", };
	[SerializeField] int  m_Value = 0;
	[SerializeField] bool m_Loop;

	[SerializeField] TextMeshProUGUI m_TextMeshProUGUI;

	[SerializeField] UnityEvent<CustomStepper> m_OnStateUpdated;
	[SerializeField] UnityEvent<int          > m_OnValueChanged;



	// Properties

	public RectTransform Transform => transform as RectTransform;

	GameObject LeftArrow {
		get => m_LeftArrow;
		set => m_LeftArrow = value;
	}
	GameObject RightArrow {
		get => m_RightArrow;
		set => m_RightArrow = value;
	}

	public string[] TextArray {
		get => m_TextArray;
		set {
			m_TextArray = value;
			Refresh();
		}
	}
	public int Value {
		get => m_Value;
		set {
			if (Loop) value = (int)Mathf.Repeat(value, TextArray.Length);
			else      value = Mathf.Clamp(value, 0, TextArray.Length - 1);
			if (m_Value == value) return;
			m_Value = value;
			m_OnValueChanged.Invoke(m_Value);
			Refresh();
		}
	}
	public bool Loop {
		get => m_Loop;
		set {
			m_Loop = value;
			Refresh();
		}
	}



	public TextMeshProUGUI TextMeshProUGUI {
		get => m_TextMeshProUGUI;
		set => m_TextMeshProUGUI = value;
	}
	public string Text {
		get => TextMeshProUGUI.text;
		set => TextMeshProUGUI.text = value;
	}

	public UnityEvent<CustomStepper> OnStateUpdated => m_OnStateUpdated;
	public UnityEvent<int          > OnValueChanged => m_OnValueChanged;



	// Methods

	public void Refresh() {
		if (LeftArrow ) LeftArrow .SetActive(Loop || 0 < Value);
		if (RightArrow) RightArrow.SetActive(Loop || Value < TextArray.Length - 1);
		if (TextMeshProUGUI) {
			var match = TextArray != null && 0 < TextArray.Length;
			TextMeshProUGUI.text = match ? TextArray[Value] : "Null";
		}
		OnStateUpdated.Invoke(this);
	}



	public void OnPointerClick(PointerEventData eventData) {
		if (interactable) {
			var point = Transform.InverseTransformPoint(eventData.position);
			Value += (0 <= point.x && (point.x < Transform.rect.width / 3)) ? -1 : 1;
		}
	}

	public void OnSubmit() {
		if (interactable) {
			DoStateTransition(SelectionState.Pressed, false);
			Value += 1;
		}
	}

	public override void OnMove(AxisEventData eventData) {
		if (interactable) switch (eventData.moveDir) {
			case MoveDirection.Left:
			case MoveDirection.Right:
				DoStateTransition(SelectionState.Pressed, false);
				var flag = eventData.moveDir == MoveDirection.Left;
				Value += flag ? -1 : 1;
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

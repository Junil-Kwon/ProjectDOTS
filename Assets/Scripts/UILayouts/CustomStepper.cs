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
public sealed class CustomStepper : Selectable, IPointerClickHandler, ISubmitHandler {

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
				LabelField("Stepper Layout", EditorStyles.boldLabel);
				I.BodyRect      = ObjectField("Body Rect",      I.BodyRect);
				I.LeftArrow     = ObjectField("Left Arrow ",    I.LeftArrow);
				I.RightArrow    = ObjectField("Right Arrow",    I.RightArrow);
				I.RestoreButton = ObjectField("Restore Button", I.RestoreButton);
				Space();
				I.TextUGUI = ObjectField("Text UGUI", I.TextUGUI);
				if (I.TextUGUI) {
					BeginHorizontal();
					PrefixLabel("Alignment");
					if (Button("Left"  )) I.TextUGUI.alignment = TextAlignmentOptions.Left;
					if (Button("Center")) I.TextUGUI.alignment = TextAlignmentOptions.Center;
					if (Button("Right" )) I.TextUGUI.alignment = TextAlignmentOptions.Right;
					EndHorizontal();
				}
				Space();
				LabelField("Stepper Event", EditorStyles.boldLabel);
				PropertyField("m_Elements");
				I.Loop    = Toggle  ("Loop",    I.Loop);
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

	[SerializeField] RectTransform m_BodyRect;
	[SerializeField] GameObject    m_LeftArrow;
	[SerializeField] GameObject    m_RightArrow;
	[SerializeField] GameObject    m_RestoreButton;

	[SerializeField] TextMeshProUGUI m_TextUGUI;

	[SerializeField] string[] m_Elements = new[] { "Option 1", "Option 2", "Option 3", };
	[SerializeField] bool m_Loop    = false;
	[SerializeField] int  m_Default = 0;
	[SerializeField] int  m_Value   = 0;

	[SerializeField] UnityEvent<CustomStepper> m_OnStateUpdated = new();
	[SerializeField] UnityEvent<int          > m_OnValueChanged = new();



	// Properties

	public RectTransform Transform => transform as RectTransform;

	RectTransform BodyRect {
		get => m_BodyRect;
		set => m_BodyRect = value;
	}
	GameObject LeftArrow {
		get => m_LeftArrow;
		set => m_LeftArrow = value;
	}
	GameObject RightArrow {
		get => m_RightArrow;
		set => m_RightArrow = value;
	}
	GameObject RestoreButton {
		get => m_RestoreButton;
		set => m_RestoreButton = value;
	}

	public TextMeshProUGUI TextUGUI {
		get => m_TextUGUI;
		set => m_TextUGUI = value;
	}



	public string[] Elements {
		get => m_Elements;
		set {
			m_Elements = value;
			Default = Default;
		}
	}
	public bool Loop {
		get => m_Loop;
		set {
			m_Loop = value;
			Refresh();
		}
	}
	public int Default {
		get => m_Default;
		set {
			if (Loop) value = (int)Mathf.Repeat(value, Elements.Length);
			else      value = Mathf.Clamp(value, 0, Elements.Length - 1);
			if (Default == value) return;
			Value = m_Default = value;
		}
	}
	public int Value {
		get => m_Value;
		set {
			if (Loop) value = (int)Mathf.Repeat(value, Elements.Length);
			else      value = Mathf.Clamp(value, 0, Elements.Length - 1);
			if (m_Value == value) return;
			m_Value = value;
			OnValueChanged.Invoke(Value);
			Refresh();
		}
	}

	public UnityEvent<CustomStepper> OnStateUpdated => m_OnStateUpdated;
	public UnityEvent<int          > OnValueChanged => m_OnValueChanged;



	// Methods

	public void Refresh() {
		if (LeftArrow ) LeftArrow .SetActive(Loop || 0 < Value);
		if (RightArrow) RightArrow.SetActive(Loop || Value < Elements.Length - 1);
		if (RestoreButton) {
			RestoreButton.SetActive(Value != Default);
		}
		if (TextUGUI) {
			var match = Elements != null && 0 < Elements.Length;
			TextUGUI.text = match ? Elements[Value] : "Null";
		}
		OnStateUpdated.Invoke(this);
	}

	public void Restore() {
		Value = Default;
	}



	// Event Handlers

	public void OnPointerClick(PointerEventData eventData) {
		if (interactable) {
			var point = BodyRect.InverseTransformPoint(eventData.position);
			Value += (0f < point.x && point.x < BodyRect.rect.width * 0.5f) ? 1 : -1;
		}
	}

	public void OnSubmit(BaseEventData eventData) {
		if (interactable) {
			Value += 1;
		}
	}

	public override void OnMove(AxisEventData eventData) {
		if (interactable) switch (eventData.moveDir) {
			case MoveDirection.Left:
			case MoveDirection.Right:
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

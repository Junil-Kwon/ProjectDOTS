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
public class CustomStepper : Selectable, IBaseWidget, IPointerClickHandler, ISubmitHandler {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(CustomStepper))]
	class CustomStepperEditor : EditorExtensionsSelectable {
		CustomStepper I => target as CustomStepper;
		public override void OnInspectorGUI() {
			Begin("Custom Stepper");

			LabelField("Selectable", EditorStyles.boldLabel);
			base.OnInspectorGUI();
			Space();
			LabelField("Stepper", EditorStyles.boldLabel);
			I.BodyRect        = ObjectField("Body Rect",         I.BodyRect);
			I.LeftArrowImage  = ObjectField("Left Arrow Image",  I.LeftArrowImage);
			I.RightArrowImage = ObjectField("Right Arrow Image", I.RightArrowImage);
			I.RestoreButton   = ObjectField("Restore Button",    I.RestoreButton);
			Space();
			I.TextUGUI = ObjectField("Text UGUI", I.TextUGUI);
			Space();
			PropertyField("m_Elements");
			Space();
			I.Default = IntField("Default", I.Default);
			I.Value   = IntField("Value",   I.Value);
			I.Loop    = Toggle  ("Loop",    I.Loop);
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
	[SerializeField] GameObject    m_LeftArrowImage;
	[SerializeField] GameObject    m_RightArrowImage;
	[SerializeField] GameObject    m_RestoreButton;

	[SerializeField] TextMeshProUGUI m_TextUGUI;

	[SerializeField] string[] m_Elements = new[] { "Option 1", "Option 2", "Option 3", };

	[SerializeField] int  m_Default;
	[SerializeField] int  m_Value;
	[SerializeField] bool m_Loop;

	[SerializeField] UnityEvent<CustomStepper> m_OnStateUpdated = new();
	[SerializeField] UnityEvent<int>           m_OnValueChanged = new();



	// Properties

	RectTransform BodyRect {
		get => m_BodyRect;
		set => m_BodyRect = value;
	}
	GameObject LeftArrowImage {
		get => m_LeftArrowImage;
		set => m_LeftArrowImage = value;
	}
	GameObject RightArrowImage {
		get => m_RightArrowImage;
		set => m_RightArrowImage = value;
	}
	GameObject RestoreButton {
		get => m_RestoreButton;
		set => m_RestoreButton = value;
	}

	TextMeshProUGUI TextUGUI {
		get => m_TextUGUI;
		set => m_TextUGUI = value;
	}



	public string[] Elements {
		get => m_Elements;
		set {
			if (m_Elements != value) {
				m_Elements = value;
				Default = Default;
				Value = Value;
				Refresh();
			}
		}
	}

	public int Default {
		get => m_Default;
		set {
			value = Loop switch {
				false => Mathf.Max(0, Mathf.Min(value, Elements.Length - 1)),
				true  => (int)Mathf.Repeat(value, Elements.Length),
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
			value = Loop switch {
				false => Mathf.Max(0, Mathf.Min(value, Elements.Length - 1)),
				true  => (int)Mathf.Repeat(value, Elements.Length),
			};
			if (m_Value != value) {
				m_Value = value;
				OnValueChanged.Invoke(value);
				Refresh();
			}
		}
	}
	public bool Loop {
		get => m_Loop;
		set {
			if (m_Loop != value) {
				m_Loop = value;
				Refresh();
			}
		}
	}

	public UnityEvent<CustomStepper> OnStateUpdated => m_OnStateUpdated;
	public UnityEvent<int>           OnValueChanged => m_OnValueChanged;



	// Methods

	public void Refresh() {
		if (LeftArrowImage)  LeftArrowImage .SetActive(Loop || 0 < Value);
		if (RightArrowImage) RightArrowImage.SetActive(Loop || Value < Elements.Length - 1);
		if (TextUGUI)        TextUGUI.text = 0 < Elements.Length ? Elements[Value] : "";
		if (RestoreButton)   RestoreButton.SetActive(Value != Default);
		OnStateUpdated.Invoke(this);
	}

	public void Restore() {
		if (RestoreButton) Value = Default;
	}



	// Event Handlers

	public void OnPointerClick(PointerEventData eventData) {
		UIManager.IsPointerClicked = true;
		if (interactable) {
			var point = BodyRect.InverseTransformPoint(eventData.position);
			Value += (0f < point.x && point.x < BodyRect.rect.width * 0.5f) ? 1 : -1;
		}
	}

	public void OnSubmit(BaseEventData eventData) {
		UIManager.IsPointerClicked = false;
		if (interactable) Value++;
	}

	public override void OnMove(AxisEventData eventData) {
		if (interactable) switch (eventData.moveDir) {
			case MoveDirection.Left:
				UIManager.IsPointerClicked = false;
				Value--;
				return;
			case MoveDirection.Right:
				UIManager.IsPointerClicked = false;
				Value++;
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

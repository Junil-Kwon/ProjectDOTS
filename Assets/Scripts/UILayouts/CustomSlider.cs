using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

using TMPro;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Custom Slider
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI/Custom Slider")]
public class CustomSlider : Selectable, IPointerClickHandler, IDragHandler {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(CustomSlider))]
		class CustomSliderEditor : SelectableEditorExtensions {
			CustomSlider I => target as CustomSlider;
			public override void OnInspectorGUI() {
				Begin("Custom Slider");

				LabelField("Selectable", EditorStyles.boldLabel);
				base.OnInspectorGUI();
				Space();
				LabelField("Slider UI", EditorStyles.boldLabel);
				I.BodyRect   = ObjectField("Body Rect",   I.BodyRect);
				I.FillRect   = ObjectField("Fill Rect",   I.FillRect);
				I.HandleRect = ObjectField("Handle Rect", I.HandleRect);
				Space();
				LabelField("Slider Value", EditorStyles.boldLabel);
				I.MinValue = FloatField("Min Value", I.MinValue);
				I.MaxValue = FloatField("Max Value", I.MaxValue);
				I.Value    = Slider    ("Value",     I.Value,    I.MinValue, I.MaxValue);
				I.Step     = Slider    ("Step",      I.Step,     I.MinValue, I.MaxValue);
				I.Finestep = Slider    ("Fine Step", I.Finestep, I.MinValue, I.MaxValue);
				Space();
				LabelField("Slider Text", EditorStyles.boldLabel);
				I.TextMeshProUGUI = ObjectField("TMPro UGUI", I.TextMeshProUGUI);
				if (I.TextMeshProUGUI) {
					I.Format = TextField("Format", I.Format);
					LabelField(" ", "{0} = Value, {1} = Min Value, {2} = Max Value");
					BeginHorizontal();
					PrefixLabel("Alignment");
					if (Button("Left"  )) I.TextMeshProUGUI.alignment = TextAlignmentOptions.Left;
					if (Button("Center")) I.TextMeshProUGUI.alignment = TextAlignmentOptions.Center;
					if (Button("Right" )) I.TextMeshProUGUI.alignment = TextAlignmentOptions.Right;
					EndHorizontal();
				}
				Space();
				LabelField("Slider Event", EditorStyles.boldLabel);
				PropertyField("m_OnStateUpdated");
				PropertyField("m_OnValueChanged");
				Space();

				End();
			}
		}
	#endif



	// Fields

	[SerializeField] RectTransform m_BodyRect;
	[SerializeField] RectTransform m_FillRect;
	[SerializeField] RectTransform m_HandleRect;

	[SerializeField] float m_MinValue = 0.00f;
	[SerializeField] float m_MaxValue = 1.00f;
	[SerializeField] float m_Value    = 0.50f;
	[SerializeField] float m_Step     = 0.10f;
	[SerializeField] float m_Finestep = 0.02f;

	[SerializeField] TextMeshProUGUI m_TextMeshProUGUI;
	[SerializeField] string m_Format = "{0:P0}";

	[SerializeField] UnityEvent<CustomSlider> m_OnStateUpdated;
	[SerializeField] UnityEvent<float       > m_OnValueChanged;



	// Properties

	public RectTransform Transform => transform as RectTransform;

	RectTransform BodyRect {
		get => m_BodyRect;
		set => m_BodyRect = value;
	}
	RectTransform FillRect {
		get => m_FillRect;
		set => m_FillRect = value;
	}
	RectTransform HandleRect {
		get => m_HandleRect;
		set => m_HandleRect = value;
	}

	float Ratio => (Value - MinValue) / (MaxValue - MinValue);
	int   Width => Mathf.RoundToInt(Ratio * (BodyRect.rect.width - HandleRect.rect.width));
	//bool  Fine  => InputManager.GetKey(KeyAction.Control);
	bool  Fine  => false;

	public float MinValue {
		get => m_MinValue;
		set {
			m_MinValue = Mathf.Min(value, MaxValue);
			Value = Value;
		}
	}
	public float MaxValue {
		get => m_MaxValue;
		set {
			m_MaxValue = Mathf.Max(value, MinValue);
			Value = Value;
		}
	}
	public float Value {
		get => m_Value;
		set {
			value = Mathf.Clamp(value, MinValue, MaxValue);
			if (m_Value == value) return;
			m_Value = value;
			m_OnValueChanged.Invoke(Value);
			Refresh();
		}
	}

	public float Step {
		get => m_Step;
		set => m_Step = Mathf.Clamp(value, 0f, MaxValue - MinValue);
	}
	public float Finestep {
		get => m_Finestep;
		set => m_Finestep = Mathf.Clamp(value, 0f, Step);
	}



	public TextMeshProUGUI TextMeshProUGUI {
		get => m_TextMeshProUGUI;
		set => m_TextMeshProUGUI = value;
	}
	public string Format {
		get => m_Format;
		set {
			m_Format = value;
			TextMeshProUGUI.text = string.Format(Format, Value, MinValue, MaxValue);
		}
	}

	public UnityEvent<CustomSlider> OnStateUpdated => m_OnStateUpdated;
	public UnityEvent<float       > OnValueChanged => m_OnValueChanged;



	// Methods

	public void Refresh() {
		if (BodyRect && HandleRect) {
			if (FillRect) {
				var sizeDelta = FillRect.sizeDelta;
				sizeDelta.x = HandleRect.rect.width / 2 + Width;
				FillRect.sizeDelta = sizeDelta;
			}
			var anchoredPosition = HandleRect.anchoredPosition;
			anchoredPosition.x = Width;
			HandleRect.anchoredPosition = anchoredPosition;
		}
		Format = Format;
		OnStateUpdated.Invoke(this);
	}



	public void OnPointerClick(PointerEventData eventData) {
		if (interactable && !eventData.dragging) {
			var point = Transform.InverseTransformPoint(eventData.position);
			if (point.x < Width) Value -= Fine ? Finestep : Step;
			if (Width < point.x) Value += Fine ? Finestep : Step;
		}
	}

	public void OnDrag(PointerEventData eventData) {
		if (interactable) {
			var point = Transform.InverseTransformPoint(eventData.position);
			var a = HandleRect.rect.width / 2;
			var b = Transform.rect.width - HandleRect.rect.width / 2;
			Value = Mathf.Lerp(MinValue, MaxValue, Mathf.InverseLerp(a, b, point.x));
		}
	}

	public void OnSubmit() {
		if (interactable) {
			DoStateTransition(SelectionState.Pressed, false);
			Value += Fine ? Finestep : Step;
		}
	}

	public override void OnMove(AxisEventData eventData) {
		if (interactable) switch (eventData.moveDir) {
			case MoveDirection.Left:
			case MoveDirection.Right:
				DoStateTransition(SelectionState.Pressed, false);
				var flag = eventData.moveDir == MoveDirection.Left;
				var step = Fine ? Finestep : Step;
				Value += flag ? -step : step;
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

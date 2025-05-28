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
public sealed class CustomSlider : Selectable, IPointerClickHandler, IDragHandler {

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
				LabelField("Slider Layout", EditorStyles.boldLabel);
				I.BodyRect      = ObjectField("Body Rect",      I.BodyRect);
				I.FillRect      = ObjectField("Fill Rect",      I.FillRect);
				I.HandleRect    = ObjectField("Handle Rect",    I.HandleRect);
				I.RestoreButton = ObjectField("Restore Button", I.RestoreButton);
				Space();
				I.TextUGUI = ObjectField("Text UGUI", I.TextUGUI);
				if (I.TextUGUI) {
					I.Format = TextField("Format", I.Format);
					LabelField(" ", "{0} = Value, {1} = Min Value, {2} = Max Value");
					BeginHorizontal();
					PrefixLabel("Alignment");
					if (Button("Left"  )) I.TextUGUI.alignment = TextAlignmentOptions.Left;
					if (Button("Center")) I.TextUGUI.alignment = TextAlignmentOptions.Center;
					if (Button("Right" )) I.TextUGUI.alignment = TextAlignmentOptions.Right;
					EndHorizontal();
				}
				Space();
				LabelField("Slider Event", EditorStyles.boldLabel);
				I.MinValue = FloatField("Min Value", I.MinValue);
				I.MaxValue = FloatField("Max Value", I.MaxValue);
				I.Step     = Slider("Step",    I.Step,    I.MinValue, I.MaxValue);
				I.Default  = Slider("Default", I.Default, I.MinValue, I.MaxValue);
				I.Value    = Slider("Value",   I.Value,   I.MinValue, I.MaxValue);
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
	[SerializeField] RectTransform m_FillRect;
	[SerializeField] RectTransform m_HandleRect;
	[SerializeField] GameObject    m_RestoreButton;

	[SerializeField] TextMeshProUGUI m_TextUGUI;
	[SerializeField] string m_Format = "{0:P0}";

	[SerializeField] float m_MinValue = 0.00f;
	[SerializeField] float m_MaxValue = 1.00f;
	[SerializeField] float m_Step     = 0.10f;
	[SerializeField] float m_Default  = 0.50f;
	[SerializeField] float m_Value    = 0.50f;

	[SerializeField] UnityEvent<CustomSlider> m_OnStateUpdated = new();
	[SerializeField] UnityEvent<float       > m_OnValueChanged = new();



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
	float Width => Ratio * (BodyRect.rect.width - HandleRect.rect.width);

	GameObject RestoreButton {
		get => m_RestoreButton;
		set => m_RestoreButton = value;
	}

	public TextMeshProUGUI TextUGUI {
		get => m_TextUGUI;
		set => m_TextUGUI = value;
	}
	public string Format {
		get => m_Format;
		set {
			m_Format = value;
			if (TextUGUI) {
				var text = string.Format(Format, Value, MinValue, MaxValue);
				if (TextUGUI.text != text) TextUGUI.text = text;
			}
		}
	}



	public float MinValue {
		get => m_MinValue;
		set {
			m_MinValue = Mathf.Min(value, MaxValue);
			Default = Default;
		}
	}
	public float MaxValue {
		get => m_MaxValue;
		set {
			m_MaxValue = Mathf.Max(value, MinValue);
			Default = Default;
		}
	}
	public float Step {
		get => m_Step;
		set => m_Step = Mathf.Clamp(value, 0f, MaxValue - MinValue);
	}

	public float Default {
		get => m_Default;
		set {
			value = Mathf.Clamp(value, MinValue, MaxValue);
			if (Default == value) return;
			Value = m_Default = value;
		}
	}
	public float Value {
		get => m_Value;
		set {
			value = Mathf.Clamp(value, MinValue, MaxValue);
			if (Value == value) return;
			m_Value = value;
			OnValueChanged.Invoke(Value);
			Refresh();
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
			anchoredPosition.x = BodyRect.anchoredPosition.x + Width;
			HandleRect.anchoredPosition = anchoredPosition;
		}
		if (RestoreButton) {
			RestoreButton.SetActive(Value != Default);
		}
		Format = Format;
		OnStateUpdated.Invoke(this);
	}

	public void Restore() {
		Value = Default;
	}



	// Event Handlers

	public void OnPointerClick(PointerEventData eventData) {
		if (interactable && !eventData.dragging) {
			var point = HandleRect.InverseTransformPoint(eventData.position);
			point.x -= HandleRect.rect.width * 0.5f;
			if (point.x < HandleRect.rect.width * -0.25f) Value -= Step;
			if (HandleRect.rect.width * +0.25f < point.x) Value += Step;
		}
	}

	public void OnDrag(PointerEventData eventData) {
		if (interactable) {
			var point = BodyRect.InverseTransformPoint(eventData.position);
			var a = 0f                  + HandleRect.rect.width * 0.5f;
			var b = BodyRect.rect.width - HandleRect.rect.width * 0.5f;
			Value = MinValue + (MaxValue - MinValue) * Mathf.InverseLerp(a, b, point.x);
		}
	}

	public void OnSubmit() {
		if (interactable) {
			Value += Step;
		}
	}

	public override void OnMove(AxisEventData eventData) {
		if (interactable) switch (eventData.moveDir) {
			case MoveDirection.Left:
			case MoveDirection.Right:
				var flag = eventData.moveDir == MoveDirection.Left;
				Value += flag ? -Step : Step;
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

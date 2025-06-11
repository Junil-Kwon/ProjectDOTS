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
public class CustomSlider : Selectable, IBaseWidget, IPointerClickHandler, IDragHandler {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(CustomSlider))]
	class CustomSliderEditor : EditorExtensionsSelectable {
		CustomSlider I => target as CustomSlider;
		public override void OnInspectorGUI() {
			Begin("Custom Slider");

			LabelField("Selectable", EditorStyles.boldLabel);
			base.OnInspectorGUI();
			Space();
			LabelField("Slider", EditorStyles.boldLabel);
			I.BodyRect      = ObjectField("Body Rect",      I.BodyRect);
			I.FillRect      = ObjectField("Fill Rect",      I.FillRect);
			I.HandleRect    = ObjectField("Handle Rect",    I.HandleRect);
			I.RestoreButton = ObjectField("Restore Button", I.RestoreButton);
			Space();
			I.TextUGUI = ObjectField("Text UGUI", I.TextUGUI);
			if (I.TextUGUI) {
				I.TextFormat = TextField("Text Format", I.TextFormat);
				LabelField(" ", "{0} = Value, {1} = Min Value, {2} = Max Value");
			}
			Space();
			I.MinValue = FloatField("Min Value", I.MinValue);
			I.MaxValue = FloatField("Max Value", I.MaxValue);
			I.Step     = Slider("Step",    I.Step,    0f,         I.MaxValue - I.MinValue);
			I.Default  = Slider("Default", I.Default, I.MinValue, I.MaxValue);
			I.Value    = Slider("Value",   I.Value,   I.MinValue, I.MaxValue);
			I.Integer  = Toggle("Integer", I.Integer);
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
	[SerializeField] string m_TextFormat = "{0:P0}";

	[SerializeField] float m_MinValue = 0.00f;
	[SerializeField] float m_MaxValue = 1.00f;
	[SerializeField] float m_Step     = 0.10f;
	[SerializeField] float m_Default  = 0.50f;
	[SerializeField] float m_Value    = 0.50f;
	[SerializeField] bool  m_Integer  = false;

	[SerializeField] UnityEvent<CustomSlider> m_OnStateUpdated = new();
	[SerializeField] UnityEvent<float>        m_OnValueChanged = new();



	// Properties

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

	TextMeshProUGUI TextUGUI {
		get => m_TextUGUI;
		set => m_TextUGUI = value;
	}
	public string TextFormat {
		get => m_TextFormat;
		set {
			m_TextFormat = value;
			try {
				var text = string.Format(TextFormat, Value, MinValue, MaxValue);
				if (TextUGUI.text != text) TextUGUI.text = text;
			} catch { }
		}
	}



	public float MinValue {
		get => m_MinValue;
		set {
			value = Integer switch {
				false => Mathf.Min(value, MaxValue - float.Epsilon),
				true  => Mathf.Round(Mathf.Min(value, MaxValue - 1f)),
			};
			if (m_MinValue != value) {
				m_MinValue = value;
				Step = Step;
				Default = Default;
				Value = Value;
				Refresh();
			}
		}
	}
	public float MaxValue {
		get => m_MaxValue;
		set {
			value = Integer switch {
				false => Mathf.Max(value, MinValue + float.Epsilon),
				true  => Mathf.Round(Mathf.Max(value, MinValue + 1f)),
			};
			if (m_MaxValue != value) {
				m_MaxValue = value;
				Step = Step;
				Default = Default;
				Value = Value;
				Refresh();
			}
		}
	}
	public float Step {
		get => m_Step;
		set {
			value = Integer switch {
				false => Mathf.Max(float.Epsilon, Mathf.Min(value, MaxValue - MinValue)),
				true  => Mathf.Round(Mathf.Max(1f, Mathf.Min(value, MaxValue - MinValue))),
			};
			if (m_Step != value) {
				m_Step = value;
			}
		}
	}
	public float Default {
		get => m_Default;
		set {
			value = Integer switch {
				false => Mathf.Clamp(value, MinValue, MaxValue),
				true  => Mathf.Round(Mathf.Clamp(value, MinValue, MaxValue)),
			};
			if (m_Default != value) {
				m_Default = value;
				Value = value;
			}
		}
	}
	public float Value {
		get => m_Value;
		set {
			value = Integer switch {
				false => Mathf.Clamp(value, MinValue, MaxValue),
				true  => Mathf.Round(Mathf.Clamp(value, MinValue, MaxValue)),
			};
			if (m_Value != value) {
				m_Value = value;
				OnValueChanged.Invoke(value);
				Refresh();
			}
		}
	}
	public bool Integer {
		get => m_Integer;
		set {
			if (m_Integer != value) {
				m_Integer = value;
				MinValue = MinValue;
				MaxValue = MaxValue;
				Step = Step;
				Default = Default;
				Value = Value;
				Refresh();
			}
		}
	}

	public UnityEvent<CustomSlider> OnStateUpdated => m_OnStateUpdated;
	public UnityEvent<float>        OnValueChanged => m_OnValueChanged;




	// Methods

	public void Refresh() {
		if (Integer) Value = Mathf.Round(Value);
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
		if (TextUGUI) TextFormat = TextFormat;
		if (RestoreButton) RestoreButton.SetActive(Value != Default);
		OnStateUpdated.Invoke(this);
	}

	public void Restore() {
		if (RestoreButton) Value = Default;
	}



	// Event Handlers

	public void OnPointerClick(PointerEventData eventData) {
		UIManager.IsPointerClicked = true;
		if (interactable && !eventData.dragging) {
			var point = HandleRect.InverseTransformPoint(eventData.position);
			point.x -= HandleRect.rect.width * 0.5f;
			if (point.x < HandleRect.rect.width * -0.25f) Value -= Step;
			if (HandleRect.rect.width * +0.25f < point.x) Value += Step;
		}
	}

	public void OnDrag(PointerEventData eventData) {
		UIManager.IsPointerClicked = true;
		if (interactable) {
			var a = HandleRect.rect.width * +0.5f;
			var b = HandleRect.rect.width * -0.5f + BodyRect.rect.width;
			var point = BodyRect.InverseTransformPoint(eventData.position);
			Value = MinValue + (MaxValue - MinValue) * Mathf.InverseLerp(a, b, point.x);
		}
	}

	public override void OnMove(AxisEventData eventData) {
		if (interactable) switch (eventData.moveDir) {
			case MoveDirection.Left:
				UIManager.IsPointerClicked = false;
				Value -= Step;
				return;
			case MoveDirection.Right:
				UIManager.IsPointerClicked = false;
				Value += Step;
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

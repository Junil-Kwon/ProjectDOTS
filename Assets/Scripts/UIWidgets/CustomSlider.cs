using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Custom Slider
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI Widget/Custom Slider")]
public class CustomSlider : Selectable, IWidgetBase, IPointerClickHandler, IDragHandler {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(CustomSlider))]
	class CustomSliderEditor : SelectableEditorExtensions {
		CustomSlider I => target as CustomSlider;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Selectable", EditorStyles.boldLabel);
			base.OnInspectorGUI();
			Space();

			LabelField("Custom Slider", EditorStyles.boldLabel);
			I.BodyRect      = ObjectField("Body Rect",      I.BodyRect);
			I.FillRect      = ObjectField("Fill Rect",      I.FillRect);
			I.HandleRect    = ObjectField("Handle Rect",    I.HandleRect);
			I.ContentText   = ObjectField("Content Text",   I.ContentText);
			I.RestoreButton = ObjectField("Restore Button", I.RestoreButton);
			Space();
			I.TextFormat = TextField("Text Format", I.TextFormat);
			LabelField(" ", "{0} = Value, {1} = Min Value, {2} = Max Value");
			Space();
			I.MinValue = FloatField("Min Value", I.MinValue);
			I.MaxValue = FloatField("Max Value", I.MaxValue);
			I.Step = Slider("Step", I.Step, 0f, I.MaxValue - I.MinValue);
			I.DefaultValue = Slider("Default Value", I.DefaultValue, I.MinValue, I.MaxValue);
			I.CurrentValue = Slider("Current Value", I.CurrentValue, I.MinValue, I.MaxValue);
			I.Integer = Toggle("Integer", I.Integer);
			Space();
			PropertyField("m_OnValueChanged");
			PropertyField("m_OnRefreshed");
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] RectTransform m_BodyRect;
	[SerializeField] RectTransform m_FillRect;
	[SerializeField] RectTransform m_HandleRect;
	[SerializeField] TextMeshProUGUI m_ContentText;
	[SerializeField] GameObject m_RestoreButton;

	[SerializeField] string m_TextFormat = "{0:P0}";
	[SerializeField] float m_MinValue = 0f;
	[SerializeField] float m_MaxValue = 1f;
	[SerializeField] float m_Step = 0.1f;
	[SerializeField] float m_DefaultValue = 0.5f;
	[SerializeField] float m_CurrentValue = 0.5f;
	[SerializeField] bool m_Integer = false;

	[SerializeField] UnityEvent<float> m_OnValueChanged = new();
	[SerializeField] UnityEvent<CustomSlider> m_OnRefreshed = new();



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
	TextMeshProUGUI ContentText {
		get => m_ContentText;
		set => m_ContentText = value;
	}
	GameObject RestoreButton {
		get => m_RestoreButton;
		set => m_RestoreButton = value;
	}



	public string TextFormat {
		get => m_TextFormat;
		set {
			m_TextFormat = value;
			Refresh();
		}
	}
	public float MinValue {
		get => m_MinValue;
		set {
			value = Integer switch {
				true  => Mathf.Round(Mathf.Min(value, MaxValue - 1f)),
				false => Mathf.Min(value, MaxValue - float.Epsilon),
			};
			if (m_MinValue != value) {
				m_MinValue = value;
				Step = Step;
				DefaultValue = DefaultValue;
				CurrentValue = CurrentValue;
				Refresh();
			}
		}
	}
	public float MaxValue {
		get => m_MaxValue;
		set {
			value = Integer switch {
				true  => Mathf.Round(Mathf.Max(value, MinValue + 1f)),
				false => Mathf.Max(value, MinValue + float.Epsilon),
			};
			if (m_MaxValue != value) {
				m_MaxValue = value;
				Step = Step;
				DefaultValue = DefaultValue;
				CurrentValue = CurrentValue;
				Refresh();
			}
		}
	}
	public float Step {
		get => m_Step;
		set {
			value = Integer switch {
				true  => Mathf.Round(Mathf.Max(1f, Mathf.Min(value, MaxValue - MinValue))),
				false => Mathf.Max(float.Epsilon, Mathf.Min(value, MaxValue - MinValue)),
			};
			if (m_Step != value) {
				m_Step = value;
			}
		}
	}
	public float DefaultValue {
		get => m_DefaultValue;
		set {
			value = Integer switch {
				true  => Mathf.Round(Mathf.Clamp(value, MinValue, MaxValue)),
				false => Mathf.Clamp(value, MinValue, MaxValue),
			};
			if (m_DefaultValue != value) {
				m_DefaultValue = value;
				CurrentValue = value;
			}
		}
	}
	public float CurrentValue {
		get => m_CurrentValue;
		set {
			value = Integer switch {
				true  => Mathf.Round(Mathf.Clamp(value, MinValue, MaxValue)),
				false => Mathf.Clamp(value, MinValue, MaxValue),
			};
			if (m_CurrentValue != value) {
				m_CurrentValue = value;
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
				DefaultValue = DefaultValue;
				CurrentValue = CurrentValue;
				Refresh();
			}
		}
	}



	public UnityEvent<float> OnValueChanged {
		get => m_OnValueChanged;
	}
	public UnityEvent<CustomSlider> OnRefreshed {
		get => m_OnRefreshed;
	}



	// Methods

	public void Refresh() {
		if (BodyRect && HandleRect) {
			float ratio = (CurrentValue - MinValue) / (MaxValue - MinValue);
			float width = ratio * (BodyRect.rect.width - HandleRect.rect.width);
			if (FillRect) {
				var sizeDelta = FillRect.sizeDelta;
				sizeDelta.x = HandleRect.rect.width / 2 + width;
				FillRect.sizeDelta = sizeDelta;
			}
			var anchoredPosition = HandleRect.anchoredPosition;
			anchoredPosition.x = BodyRect.anchoredPosition.x + width;
			HandleRect.anchoredPosition = anchoredPosition;
		}
		if (ContentText) try {
			ContentText.text = string.Format(TextFormat, CurrentValue, MinValue, MaxValue);
		} catch { }
		if (RestoreButton) RestoreButton.SetActive(CurrentValue != DefaultValue);
		OnRefreshed.Invoke(this);
	}

	public void Restore() {
		if (RestoreButton) CurrentValue = DefaultValue;
	}



	// Event Handlers

	public void OnPointerClick(PointerEventData eventData) {
		if (interactable && !eventData.dragging) {
			var point = HandleRect.InverseTransformPoint(eventData.position);
			point.x -= HandleRect.rect.width * 0.5f;
			if (point.x < HandleRect.rect.width * -0.25f) CurrentValue -= Step;
			if (HandleRect.rect.width * +0.25f < point.x) CurrentValue += Step;
		}
	}

	public void OnDrag(PointerEventData eventData) {
		if (interactable) {
			var point = BodyRect.InverseTransformPoint(eventData.position);
			float inset = HandleRect.rect.width * 0.5f;
			float min = inset;
			float max = BodyRect.rect.width - inset;
			CurrentValue = MinValue + (MaxValue - MinValue) * Mathf.InverseLerp(min, max, point.x);
		}
	}

	public override void OnMove(AxisEventData eventData) {
		if (interactable) switch (eventData.moveDir) {
			case MoveDirection.Left:
				CurrentValue -= Step;
				return;
			case MoveDirection.Right:
				CurrentValue += Step;
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

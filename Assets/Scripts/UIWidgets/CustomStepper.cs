using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Custom Stepper
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI Widget/Custom Stepper")]
public class CustomStepper : Selectable, IWidgetBase, IPointerClickHandler, ISubmitHandler {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(CustomStepper))]
	class CustomStepperEditor : SelectableEditorExtensions {
		CustomStepper I => target as CustomStepper;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Selectable", EditorStyles.boldLabel);
			base.OnInspectorGUI();
			Space();

			LabelField("Custom Stepper", EditorStyles.boldLabel);
			I.PrevArrowImage = ObjectField("Prev Arrow Image", I.PrevArrowImage);
			I.NextArrowImage = ObjectField("Next Arrow Image", I.NextArrowImage);
			I.ContentText    = ObjectField("Content Text",     I.ContentText);
			I.RestoreButton  = ObjectField("Restore Button",   I.RestoreButton);
			Space();
			PropertyField("m_Elements");
			I.DefaultValue = IntField("Default Value", I.DefaultValue);
			I.CurrentValue = IntField("Current Value", I.CurrentValue);
			I.Loop = Toggle("Loop", I.Loop);
			Space();
			PropertyField("m_OnValueChanged");
			PropertyField("m_OnRefreshed");
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] GameObject m_PrevArrowImage;
	[SerializeField] GameObject m_NextArrowImage;
	[SerializeField] TextMeshProUGUI m_ContentText;
	[SerializeField] GameObject m_RestoreButton;

	[SerializeField] string[] m_Elements = new[] { "Element 1", "Element 2", "Element 3", };
	[SerializeField] int m_DefaultValue;
	[SerializeField] int m_CurrentValue;
	[SerializeField] bool m_Loop;

	[SerializeField] UnityEvent<int> m_OnValueChanged = new();
	[SerializeField] UnityEvent<CustomStepper> m_OnRefreshed = new();



	// Properties

	GameObject PrevArrowImage {
		get => m_PrevArrowImage;
		set => m_PrevArrowImage = value;
	}
	GameObject NextArrowImage {
		get => m_NextArrowImage;
		set => m_NextArrowImage = value;
	}
	TextMeshProUGUI ContentText {
		get => m_ContentText;
		set => m_ContentText = value;
	}
	GameObject RestoreButton {
		get => m_RestoreButton;
		set => m_RestoreButton = value;
	}



	public string[] Elements {
		get => m_Elements;
		set {
			if (m_Elements != value) {
				m_Elements = value;
				DefaultValue = DefaultValue;
				CurrentValue = CurrentValue;
				Refresh();
			}
		}
	}
	public int DefaultValue {
		get => m_DefaultValue;
		set {
			value = Loop switch {
				true  => (value + Elements.Length) % Elements.Length,
				false => Mathf.Clamp(value, 0, Elements.Length - 1),
			};
			if (m_DefaultValue != value) {
				m_DefaultValue = value;
				CurrentValue = value;
			}
		}
	}
	public int CurrentValue {
		get => m_CurrentValue;
		set {
			value = Loop switch {
				true  => (value + Elements.Length) % Elements.Length,
				false => Mathf.Clamp(value, 0, Elements.Length - 1),
			};
			if (m_CurrentValue != value) {
				m_CurrentValue = value;
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



	public UnityEvent<int> OnValueChanged {
		get => m_OnValueChanged;
	}
	public UnityEvent<CustomStepper> OnRefreshed {
		get => m_OnRefreshed;
	}



	// Methods

	public void Refresh() {
		if (PrevArrowImage) PrevArrowImage.SetActive(Loop || 0 < CurrentValue);
		if (NextArrowImage) NextArrowImage.SetActive(Loop || CurrentValue < Elements.Length - 1);
		if (ContentText) ContentText.text = 0 < Elements.Length ? Elements[CurrentValue] : null;
		if (RestoreButton) RestoreButton.SetActive(CurrentValue != DefaultValue);
		OnRefreshed.Invoke(this);
	}

	public void Restore() {
		if (RestoreButton) CurrentValue = DefaultValue;
	}



	// Event Handlers

	public override void OnPointerEnter(PointerEventData eventData) {
		if (interactable) {
			if (UIManager.Selected != this) UIManager.Selected = this;
			base.OnPointerEnter(eventData);
		}
	}

	public override void OnPointerExit(PointerEventData eventData) {
		if (interactable) {
			if (UIManager.Selected == this) UIManager.Selected = null;
			base.OnPointerExit(eventData);
		}
	}



	public void OnPointerClick(PointerEventData eventData) {
		if (interactable) {
			var transform = (RectTransform)this.transform;
			var point = transform.InverseTransformPoint(eventData.position);
			CurrentValue += (0f < point.x && point.x < transform.rect.width * 0.5f) ? +1 : -1;
		}
	}

	public void OnSubmit(BaseEventData eventData) {
		if (interactable) CurrentValue++;
	}

	public override void OnMove(AxisEventData eventData) {
		if (interactable) switch (eventData.moveDir) {
			case MoveDirection.Left:
				CurrentValue--;
				return;
			case MoveDirection.Right:
				CurrentValue++;
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

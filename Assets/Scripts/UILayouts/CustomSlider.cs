using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Components;
using UnityEngine.Events;
using UnityEngine.EventSystems;

using System;
using TMPro;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Custom Slider
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[RequireComponent(typeof(LocalizeStringEvent))]
public class CustomSlider : Selectable, IPointerClickHandler, IDragHandler {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(CustomSlider))]
		class CustomSliderEditor : SelectableEditorExtensions {
			CustomSlider I => target as CustomSlider;
			public override void OnInspectorGUI() {
				base.OnInspectorGUI();
				Begin("Custom Slider");

				LabelField("Text");
				Space();

				End();
			}
		}
	#endif



	// Constants

	[Serializable] public class SliderUpdatedEvent : UnityEvent<CustomSlider> { }
	[Serializable] public class SliderChangedEvent : UnityEvent<float> { }



	// Fields

	[SerializeField] SliderUpdatedEvent m_OnStateUpdated;
	[SerializeField] SliderChangedEvent m_OnValueChanged;
	[SerializeField] float m_Value;

	TextMeshProUGUI     m_TextMeshProUGUI;
	LocalizeStringEvent m_LocalizeStringEvent;



	// Properties

	public SliderUpdatedEvent OnStateUpdated => m_OnStateUpdated;
	public SliderChangedEvent OnValueChanged => m_OnValueChanged;
	public float Value {
		get => m_Value;
		set {
			if (m_Value == value) return;
			m_Value = value;
			m_OnValueChanged.Invoke(m_Value);
		}
	}

	public RectTransform Transform => transform as RectTransform;

	public TextMeshProUGUI TextMeshProUGUI {
		get {
			if (!m_TextMeshProUGUI) for (int i = 0; i < Transform.childCount; i++) {
				if (Transform.GetChild(i).TryGetComponent(out m_TextMeshProUGUI)) break;
			}
			return m_TextMeshProUGUI;
		}
	}
	public LocalizeStringEvent LocalizeStringEvent {
		get {
			if (!m_LocalizeStringEvent) TryGetComponent(out m_LocalizeStringEvent);
			return m_LocalizeStringEvent;
		}
	}
	public string Text {
		get => TextMeshProUGUI.text;
		set => TextMeshProUGUI.text = value;
	}



	// Methods

	public void OnPointerClick(PointerEventData eventData) { }

	public void OnDrag(PointerEventData eventData) { }

}
/*
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

using System;

using TMPro;

#if UNITY_EDITOR
	using UnityEditor;
	using UnityEditor.UI;
	using static UnityEditor.EditorGUILayout;
#endif



public class SettingsSlider : Selectable, IPointerClickHandler, IDragHandler {

	[Serializable] public class SliderUpdatedEvent : UnityEvent<SettingsSlider> {}
	[Serializable] public class SliderChangedEvent : UnityEvent<float> {}



	// ================================================================================================
	// Fields
	// ================================================================================================

	[SerializeField] RectTransform      m_BodyRect;
	[SerializeField] RectTransform      m_HandleRect;
	[SerializeField] TextMeshProUGUI    m_TextTMP;
	[SerializeField] SliderUpdatedEvent m_OnStateUpdated;
	[SerializeField] SliderChangedEvent m_OnValueChanged;

	[SerializeField] float  m_MinValue = 0;
	[SerializeField] float  m_MaxValue = 1;
	[SerializeField] float  m_Value    = 1;
	[SerializeField] float  m_Step     = 0.10f;
	[SerializeField] float  m_Finestep = 0.02f;
	[SerializeField] string m_Format   = "{0:P0}";



	RectTransform BodyRect {
		get => m_BodyRect;
		set => m_BodyRect = value;
	}

	RectTransform HandleRect {
		get => m_HandleRect;
		set => m_HandleRect = value;
	}

	TextMeshProUGUI TextTMP {
		get => m_TextTMP;
		set => m_TextTMP = value;
	}

	public SliderUpdatedEvent OnStateUpdated {
		get => m_OnStateUpdated;
		set => m_OnStateUpdated = value;
	}

	public SliderChangedEvent OnValueChanged {
		get => m_OnValueChanged;
		set => m_OnValueChanged = value;
	}



	public float MinValue {
		get => m_MinValue;
		set {
			m_MinValue = Mathf.Min(value, MaxValue);
			this.Value = this.Value;
		}
	}

	public float MaxValue {
		get => m_MaxValue;
		set {
			m_MaxValue = Mathf.Max(value, MinValue);
			this.Value = this.Value;
		}
	}

	public float Value {
		get => m_Value;
		set {
			if (m_Value == value) return;
			m_Value = Mathf.Clamp(value, MinValue, MaxValue);
			OnValueChanged?.Invoke(m_Value);
			Refresh();
		}
	}

	public float Step {
		get => m_Step;
		set => m_Step = Mathf.Clamp(value, 0, MaxValue - MinValue);
	}

	public float Finestep {
		get => m_Finestep;
		set => m_Finestep = Mathf.Clamp(value, 0, Step);
	}

	public string Format {
		get => m_Format;
		set {
			m_Format = value;
			Refresh();
		}
	}



	RectTransform Rect => transform as RectTransform;

	public string Text => string.Format(Format, Value, MinValue, MaxValue);
	


	#if UNITY_EDITOR
		[CustomEditor(typeof(SettingsSlider))] class SettingsSliderEditor : SelectableEditor {

			SerializedProperty m_BodyRect;
			SerializedProperty m_HandleRect;
			SerializedProperty m_TextTMP;
			SerializedProperty m_OnStateUpdated;
			SerializedProperty m_OnValueChanged;

			SettingsSlider i => target as SettingsSlider;

			protected override void OnEnable() {
				base.OnEnable();
				m_BodyRect       = serializedObject.FindProperty("m_BodyRect");
				m_HandleRect     = serializedObject.FindProperty("m_HandleRect");
				m_TextTMP        = serializedObject.FindProperty("m_TextTMP");
				m_OnStateUpdated = serializedObject.FindProperty("m_OnStateUpdated");
				m_OnValueChanged = serializedObject.FindProperty("m_OnValueChanged");
			}

			public override void OnInspectorGUI() {
				base.OnInspectorGUI();
				Undo.RecordObject(target, "Settings Slider Properties");
				
				PropertyField(m_BodyRect);
				PropertyField(m_HandleRect);
				PropertyField(m_TextTMP);
				Space();

				i.MinValue = FloatField("Min Value", i.MinValue);
				i.MaxValue = FloatField("Max Value", i.MaxValue);
				i.Value    = Slider    ("Value",     i.Value,    i.MinValue, i.MaxValue);
				i.Step     = Slider    ("Step",      i.Step,     i.MinValue, i.MaxValue);
				i.Finestep = Slider    ("Fine Step", i.Finestep, i.MinValue, i.MaxValue);
				i.Format   = TextField ("Format",    i.Format);
				TextField(i.Text, "{0} = Value, {1} = Min Value, {2} = Max Value");
				Space();

				PropertyField(m_OnStateUpdated);
				PropertyField(m_OnValueChanged);
				Space();

				serializedObject.ApplyModifiedProperties();
				if (GUI.changed) EditorUtility.SetDirty(target);
			}
		}
	#endif



	// ================================================================================================
	// Methods
	// ================================================================================================

	float ratio => (Value - MinValue) / (MaxValue - MinValue);
	int   width => Mathf.RoundToInt(ratio * (Rect.rect.width - m_HandleRect.rect.width));
	bool  fine  => InputManager.GetKey(KeyAction.Control);

	public void OnPointerClick(PointerEventData eventData) {
		if (interactable && !eventData.dragging) {
			Vector2 point = Rect.InverseTransformPoint(eventData.position);
			if (point.x < width) Value -= fine ? Finestep : Step;
			if (width < point.x) Value += fine ? Finestep : Step;
		}
	}
	
	public void OnDrag(PointerEventData eventData) {
		if (interactable) {
			Vector2 point = Rect.InverseTransformPoint(eventData.position);
			float a = m_HandleRect.rect.width / 2;
			float b = Rect.rect.width - m_HandleRect.rect.width / 2;
			Value = Mathf.Lerp(MinValue, MaxValue, Mathf.InverseLerp(a, b, point.x));
		}
	}

	public void OnSubmit() {
		if (interactable) {
			DoStateTransition(SelectionState.Pressed, false);
			Value += fine ? Finestep : Step;
		}
	}

	public override void OnMove(AxisEventData eventData) {
		if (interactable) switch (eventData.moveDir) {
			case MoveDirection.Left:
				DoStateTransition(SelectionState.Pressed, false);
				Value -= fine ? Finestep : Step;
				return;
			case MoveDirection.Right:
				DoStateTransition(SelectionState.Pressed, false);
				Value += fine ? Finestep : Step;
				return;
		}
		base.OnMove(eventData);
	}



	ScrollRect scrollRect;

	bool TryGetComponentInParent<T>(out T component) where T : Component {
		component = null;
		Transform parent = Rect;
		while (parent != null) {
			if  (parent.TryGetComponent(out component)) return true;
			else parent = parent.parent;
		}
		return false;
	}

	public override void OnSelect(BaseEventData eventData) {
		base.OnSelect(eventData);
		if (eventData is AxisEventData) {
			if (scrollRect || TryGetComponentInParent(out scrollRect)) {
				Vector2 anchoredPosition = scrollRect.content.anchoredPosition;
				float pivot = Rect.rect.height / 2 - Rect.anchoredPosition.y;
				anchoredPosition.y = pivot - scrollRect.viewport.rect.height / 2;
				scrollRect.content.anchoredPosition = anchoredPosition;
			}
		}
	}

	

	public void Refresh() {
		if (m_HandleRect) {
			if (m_BodyRect) {
				Vector2 sizeDelta = m_BodyRect.sizeDelta;
				sizeDelta.x = m_HandleRect.rect.width / 2 + width;
				m_BodyRect.sizeDelta = sizeDelta;
			}
			Vector2 anchoredPosition = m_HandleRect.anchoredPosition;
			anchoredPosition.x = width;
			m_HandleRect.anchoredPosition = anchoredPosition;
		}
		if (m_TextTMP) m_TextTMP.text = Text;
		OnStateUpdated?.Invoke(this);
	}



	// ================================================================================================
	// Lifecycle
	// ================================================================================================

	protected override void OnEnable() {
		base.OnEnable();
		Refresh();
	}
}
*/
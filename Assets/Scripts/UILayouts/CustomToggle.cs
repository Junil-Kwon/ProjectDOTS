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
// Custom Toggle
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[RequireComponent(typeof(LocalizeStringEvent))]
public class CustomToggle : Selectable, IPointerClickHandler {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(CustomToggle))]
		class CustomToggleEditor : SelectableEditorExtensions {
			CustomToggle I => target as CustomToggle;
			public override void OnInspectorGUI() {
				base.OnInspectorGUI();
				Begin("Custom Toggle");

				LabelField("Text");
				Space();

				End();
			}
		}
	#endif



	// Constants

	[Serializable] public class ToggleUpdatedEvent : UnityEvent<CustomToggle> { }
	[Serializable] public class ToggleChangedEvent : UnityEvent<bool> { }



	// Fields

	[SerializeField] ToggleUpdatedEvent m_OnStateUpdated;
	[SerializeField] ToggleChangedEvent m_OnValueChanged;
	[SerializeField] bool m_Value;

	TextMeshProUGUI     m_TextMeshProUGUI;
	LocalizeStringEvent m_LocalizeStringEvent;



	// Properties
	
	public ToggleUpdatedEvent OnStateUpdated => m_OnStateUpdated;
	public ToggleChangedEvent OnValueChanged => m_OnValueChanged;
	public bool Value {
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



public class SettingsToggle : Selectable, IPointerClickHandler {

	[Serializable] public class ToggleUpdatedEvent : UnityEvent<SettingsToggle> {}
	[Serializable] public class ToggleChangedEvent : UnityEvent<bool> {}



	// =================================================================================================
	// Fields
	// =================================================================================================

	[SerializeField] RectTransform      m_PositiveRect;
	[SerializeField] RectTransform      m_NegativeRect;
	[SerializeField] TextMeshProUGUI    m_PositiveTextTMP;
	[SerializeField] TextMeshProUGUI    m_NegativeTextTMP;
	[SerializeField] ToggleUpdatedEvent m_OnStateUpdated;
	[SerializeField] ToggleChangedEvent m_OnValueChanged;

	[SerializeField] bool m_Value = false;



	RectTransform PositiveRect {
		get => m_PositiveRect;
		set => m_PositiveRect = value;
	}

	RectTransform NegativeRect {
		get => m_NegativeRect;
		set => m_NegativeRect = value;
	}

	TextMeshProUGUI PositiveTextTMP {
		get => m_PositiveTextTMP;
		set => m_PositiveTextTMP = value;
	}

	TextMeshProUGUI NegativeTextTMP {
		get => m_NegativeTextTMP;
		set => m_NegativeTextTMP = value;
	}

	public ToggleUpdatedEvent OnStateUpdated {
		get => m_OnStateUpdated;
		set => m_OnStateUpdated = value;
	}

	public ToggleChangedEvent OnValueChanged {
		get => m_OnValueChanged;
		set => m_OnValueChanged = value;
	}



	public bool Value {
		get => m_Value;
		set {
			if (m_Value == value) return;
			m_Value = value;
			OnValueChanged?.Invoke(m_Value);
			Refresh();
		}
	}



	RectTransform Rect => transform as RectTransform;



	#if UNITY_EDITOR
		[CustomEditor(typeof(SettingsToggle))] class SettingsToggleEditor : SelectableEditor {

			SerializedProperty m_PositiveRect;
			SerializedProperty m_NegativeRect;
			SerializedProperty m_PositiveTextTMP;
			SerializedProperty m_NegativeTextTMP;
			SerializedProperty m_OnStateUpdated;
			SerializedProperty m_OnValueChanged;

			SettingsToggle i => target as SettingsToggle;

			protected override void OnEnable() {
				base.OnEnable();
				m_PositiveRect    = serializedObject.FindProperty("m_PositiveRect");
				m_NegativeRect    = serializedObject.FindProperty("m_NegativeRect");
				m_PositiveTextTMP = serializedObject.FindProperty("m_PositiveTextTMP");
				m_NegativeTextTMP = serializedObject.FindProperty("m_NegativeTextTMP");
				m_OnStateUpdated  = serializedObject.FindProperty("m_OnStateUpdated");
				m_OnValueChanged  = serializedObject.FindProperty("m_OnValueChanged");
			}

			public override void OnInspectorGUI() {
				base.OnInspectorGUI();
				Undo.RecordObject(target, "Settings Toggle Properties");
				
				PropertyField(m_PositiveRect);
				PropertyField(m_NegativeRect);
				PropertyField(m_PositiveTextTMP);
				PropertyField(m_NegativeTextTMP);
				Space();

				i.Value = Toggle("Value", i.Value);
				Space();
				
				PropertyField(m_OnStateUpdated);
				PropertyField(m_OnValueChanged);
				Space();

				serializedObject.ApplyModifiedProperties();
				if (GUI.changed) EditorUtility.SetDirty(target);
			}
		}
	#endif



	// =================================================================================================
	// Methods
	// =================================================================================================

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
		if (m_PositiveRect) m_PositiveRect.gameObject.SetActive( Value);
		if (m_NegativeRect) m_NegativeRect.gameObject.SetActive(!Value);
		if (m_PositiveTextTMP) m_PositiveTextTMP.gameObject.SetActive( Value);
		if (m_NegativeTextTMP) m_NegativeTextTMP.gameObject.SetActive(!Value);
		OnStateUpdated?.Invoke(this);
	}



	// =================================================================================================
	// Lifecycle
	// =================================================================================================

	protected override void OnEnable() {
		base.OnEnable();
		Refresh();
	}
}
*/
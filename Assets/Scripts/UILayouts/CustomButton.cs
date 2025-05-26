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
// Custom Button
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[RequireComponent(typeof(LocalizeStringEvent))]
public class CustomButton : Selectable, IPointerClickHandler {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(CustomButton))]
		class CustomButtonEditor : SelectableEditorExtensions {
			CustomButton I => target as CustomButton;
			public override void OnInspectorGUI() {
				base.OnInspectorGUI();
				Begin("Custom Button");

				LabelField("Text");
				Space();

				End();
			}
		}
	#endif



	// Constants

	[SerializeField] public class ButtonUpdatedEvent : UnityEvent<CustomButton> { }
	[SerializeField] public class ButtonClickedEvent : UnityEvent { }



	// Fields

	[SerializeField] ButtonUpdatedEvent m_OnStateUpdated;
	[SerializeField] ButtonClickedEvent m_OnClick;

	TextMeshProUGUI     m_TextMeshProUGUI;
	LocalizeStringEvent m_LocalizeStringEvent;



	// Properties

	public ButtonUpdatedEvent OnStateUpdated => m_OnStateUpdated;
	public ButtonClickedEvent OnClick        => m_OnClick;

	RectTransform Transform => transform as RectTransform;

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
		if (interactable) OnClick.Invoke();
	}

	public void OnSubmit() {
		if (interactable) {
			DoStateTransition(SelectionState.Pressed, false);
			OnClick.Invoke();
		}
	}
}



/*
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

using System;

using UnityEngine.Localization.Components;
using TMPro;

#if UNITY_EDITOR
	using UnityEditor;
	using UnityEditor.UI;
	using static UnityEditor.EditorGUILayout;
#endif



public class CustomButton : Selectable, IPointerClickHandler {

	[Serializable] public class ButtonUpdatedEvent : UnityEvent<CustomButton> {}
	[Serializable] public class ButtonClickedEvent : UnityEvent {}



	// ================================================================================================
	// Fields
	// ================================================================================================

	[SerializeField] TextMeshProUGUI     m_TextTMP;
	[SerializeField] LocalizeStringEvent m_LocalizeStringEvent;
	[SerializeField] ButtonUpdatedEvent  m_OnStateUpdated;
	[SerializeField] ButtonClickedEvent  m_OnClick;



	TextMeshProUGUI TextTMP {
		get => m_TextTMP;
		set => m_TextTMP = value;
	}

	LocalizeStringEvent LocalizeStringEvent {
		get => m_LocalizeStringEvent;
		set => m_LocalizeStringEvent = value;
	}

	public ButtonUpdatedEvent OnStateUpdated {
		get => m_OnStateUpdated;
		set => m_OnStateUpdated = value;
	}

	public ButtonClickedEvent OnClick {
		get => m_OnClick;
		set => m_OnClick = value;
	}



	RectTransform Rect => transform as RectTransform;

	public string Text {
		get => m_TextTMP ? m_TextTMP.text : string.Empty;
		set {
			if (LocalizeStringEvent) LocalizeStringEvent.StringReference.Clear();
			if (m_TextTMP) m_TextTMP.text = value;
		}
	}



	#if UNITY_EDITOR
		[CustomEditor(typeof(CustomButton))] class CustomButtonEditor : SelectableEditor {

			SerializedProperty m_TextTMP;
			SerializedProperty m_LocalizeStringEvent;
			SerializedProperty m_OnStateUpdated;
			SerializedProperty m_OnClick;

			CustomText i => target as CustomText;

			protected override void OnEnable() {
				base.OnEnable();
				m_TextTMP             = serializedObject.FindProperty("m_TextTMP");
				m_LocalizeStringEvent = serializedObject.FindProperty("m_LocalizeStringEvent");
				m_OnStateUpdated      = serializedObject.FindProperty("m_OnStateUpdated");
				m_OnClick             = serializedObject.FindProperty("m_OnClick");
			}

			public override void OnInspectorGUI() {
				base.OnInspectorGUI();
				Undo.RecordObject(target, "Custom Button Properties");

				PropertyField(m_TextTMP);
				PropertyField(m_LocalizeStringEvent);
				Space();
				
				PropertyField(m_OnStateUpdated);
				PropertyField(m_OnClick);
				Space();

				serializedObject.ApplyModifiedProperties();
				if (GUI.changed) EditorUtility.SetDirty(target);
			}
		}
	#endif



	// ================================================================================================
	// Methods
	// ================================================================================================

	public void OnPointerClick(PointerEventData eventData) {
		if (interactable) OnClick?.Invoke();
	}

	public void OnSubmit() {
		if (interactable) {
			DoStateTransition(SelectionState.Pressed, false);
			OnClick?.Invoke();
		}
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



	public string GetLocalizeText() {
		return LocalizeStringEvent ? LocalizeStringEvent.StringReference.GetLocalizedString() : "";
	}

	public void SetLocalizeText(string table, string tableEntry) {
		if (LocalizeStringEvent) LocalizeStringEvent.StringReference.SetReference(table, tableEntry);
	}

	public void Refresh() {
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
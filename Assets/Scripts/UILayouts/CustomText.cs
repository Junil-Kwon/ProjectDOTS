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
// Custom Text
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[RequireComponent(typeof(LocalizeStringEvent))]
public class CustomText : Selectable {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(CustomText))]
		class CustomTextEditor : SelectableEditorExtensions {
			CustomText I => target as CustomText;
			public override void OnInspectorGUI() {
				base.OnInspectorGUI();
				Begin("Custom Text");

				LabelField("Text");
				Space();

				End();
			}
		}
	#endif



	// Fields

	TextMeshProUGUI m_TextMeshProUGUI;
	LocalizeStringEvent m_LocalizeStringEvent;



	// Properties

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


}



/*
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using System;

using UnityEngine.Localization.Components;
using TMPro;

#if UNITY_EDITOR
	using UnityEditor;
	using UnityEditor.UI;
	using static UnityEditor.EditorGUILayout;
#endif


 
[RequireComponent(typeof(LocalizeStringEvent))]
public class CustomText : Selectable {

	[Serializable] public class TextUpdatedEvent : UnityEvent<CustomText> {}



	// ================================================================================================
	// Fields
	// ================================================================================================

	[SerializeField] TextMeshProUGUI     m_TextTMP;
	[SerializeField] LocalizeStringEvent m_LocalizeStringEvent;
	[SerializeField] TextUpdatedEvent    m_OnStateUpdated;



	TextMeshProUGUI TextTMP {
		get => m_TextTMP;
		set => m_TextTMP = value;
	}

	LocalizeStringEvent LocalizeStringEvent {
		get => m_LocalizeStringEvent;
		set => m_LocalizeStringEvent = value;
	}

	public TextUpdatedEvent OnStateUpdated {
		get => m_OnStateUpdated;
		set => m_OnStateUpdated = value;
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
		[CustomEditor(typeof(CustomText))] class CustomTextEditor : SelectableEditor {

			SerializedProperty m_TextTMP;
			SerializedProperty m_LocalizeStringEvent;
			SerializedProperty m_OnStateUpdated;

			CustomText i => target as CustomText;

			protected override void OnEnable() {
				base.OnEnable();
				m_TextTMP             = serializedObject.FindProperty("m_TextTMP");
				m_LocalizeStringEvent = serializedObject.FindProperty("m_LocalizeStringEvent");
				m_OnStateUpdated      = serializedObject.FindProperty("m_OnStateUpdated");
			}

			public override void OnInspectorGUI() {
				base.OnInspectorGUI();
				Undo.RecordObject(target, "Custom Text Properties");

				PropertyField(m_TextTMP);
				PropertyField(m_LocalizeStringEvent);
				Space();
				
				PropertyField(m_OnStateUpdated);
				Space();

				serializedObject.ApplyModifiedProperties();
				if (GUI.changed) EditorUtility.SetDirty(target);
			}
		}
	#endif



	// ================================================================================================
	// Methods
	// ================================================================================================

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
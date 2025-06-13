using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Alert Canvas
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI/Alert Canvas")]
public class AlertCanvas : BaseCanvas {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(AlertCanvas))]
	class AlertCanvasEditor : EditorExtensions {
		AlertCanvas I => target as AlertCanvas;
		public override void OnInspectorGUI() {
			Begin("Alert Canvas");

			if (I.Raycaster && I.Raycaster.enabled) {
				LabelField("Selected", EditorStyles.boldLabel);
				I.FirstSelected = ObjectField("First Selected", I.FirstSelected);
				Space();
			}
			LabelField("Localize Event", EditorStyles.boldLabel);
			I.ContentText = ObjectField("Content Text", I.ContentText);
			I.CloseButton = ObjectField("Close Button", I.CloseButton);
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] LocalizeStringEvent m_ContentText;
	[SerializeField] LocalizeStringEvent m_CloseButton;

	Action m_OnClosed;



	// Properties

	LocalizeStringEvent ContentText {
		get => m_ContentText;
		set => m_ContentText = value;
	}
	LocalizeStringEvent CloseButton {
		get => m_CloseButton;
		set => m_CloseButton = value;
	}

	public LocalizedString ContentReference {
		get => ContentText.StringReference;
		set => ContentText.StringReference = value;
	}
	public LocalizedString CloseReference {
		get => CloseButton.StringReference;
		set => CloseButton.StringReference = value;
	}

	public Action OnClosed {
		get => m_OnClosed;
		set => m_OnClosed = value;
	}



	// Methods

	public override void Back() => Close();

	public void Close() {
		UIManager.PopOverlay();
		OnClosed?.Invoke();
		OnClosed = null;
	}
}

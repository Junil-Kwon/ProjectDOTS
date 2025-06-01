using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;

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
		class TitleCanvasEditor : EditorExtensions {
			AlertCanvas I => target as AlertCanvas;
			public override void OnInspectorGUI() {
				Begin("Alert Canvas");

				LabelField("Selected", EditorStyles.boldLabel);
				I.FirstSelected = ObjectField("First Selected", I.FirstSelected);
				Space();
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

	readonly UnityEvent m_CloseEvent = new();



	// Properties

	LocalizeStringEvent ContentText {
		get => m_ContentText;
		set => m_ContentText = value;
	}
	LocalizeStringEvent CloseButton {
		get => m_CloseButton;
		set => m_CloseButton = value;
	}

	public string ContentKey {
		get => ContentText.StringReference.GetLocalizedString();
		set => ContentText.StringReference.SetReference(UIManager.LocalizationTable, value);
	}
	public string CloseKey {
		get => CloseButton.StringReference.GetLocalizedString();
		set => CloseButton.StringReference.SetReference(UIManager.LocalizationTable, value);
	}

	public UnityEvent CloseEvent => m_CloseEvent;



	// Methods

	public override void Hide(bool keepState = false) {
		base.Hide(keepState);
		if (!keepState) {
			CloseEvent.RemoveAllListeners();
		}
	}

	public void OnCloseButtonClick() {
		CloseEvent.Invoke();
		UIManager.Back();
	}
}

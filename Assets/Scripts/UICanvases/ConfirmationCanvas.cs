using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Localization.Components;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Confirmation Canvas
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI/Confirmation Canvas")]
public class ConfirmationCanvas : BaseCanvas {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(ConfirmationCanvas))]
		class ConfirmationCanvasEditor : EditorExtensions {
			ConfirmationCanvas I => target as ConfirmationCanvas;
			public override void OnInspectorGUI() {
				Begin("Confirmation Canvas");

				LabelField("Selected", EditorStyles.boldLabel);
				I.FirstSelected = ObjectField("First Selected", I.FirstSelected);
				I.Cancel        = ObjectField("Cancel",         I.Cancel);
				Space();
				LabelField("Localize Event", EditorStyles.boldLabel);
				I.HeaderText    = ObjectField("Header Text",    I.HeaderText);
				I.ContentText   = ObjectField("Content Text",   I.ContentText);
				I.ConfirmButton = ObjectField("Confirm Button", I.ConfirmButton);
				I.CancelButton  = ObjectField("Cancel Button",  I.CancelButton);
				Space();

				End();
			}
		}
	#endif



	// Fields

	[SerializeField] Selectable m_Cancel;

	[SerializeField] LocalizeStringEvent m_HeaderText;
	[SerializeField] LocalizeStringEvent m_ContentText;
	[SerializeField] LocalizeStringEvent m_ConfirmButton;
	[SerializeField] LocalizeStringEvent m_CancelButton;

	readonly UnityEvent m_ConfirmEvent = new();
	readonly UnityEvent m_CancelEvent  = new();



	// Properties

	Selectable Cancel {
		get => m_Cancel;
		set => m_Cancel = value;
	}

	LocalizeStringEvent HeaderText {
		get => m_HeaderText;
		set => m_HeaderText = value;
	}
	LocalizeStringEvent ContentText {
		get => m_ContentText;
		set => m_ContentText = value;
	}
	LocalizeStringEvent ConfirmButton {
		get => m_ConfirmButton;
		set => m_ConfirmButton = value;
	}
	LocalizeStringEvent CancelButton {
		get => m_CancelButton;
		set => m_CancelButton = value;
	}

	public string HeaderKey {
		get => HeaderText.StringReference.GetLocalizedString();
		set => HeaderText.StringReference.SetReference(UIManager.LocalizationTable, value);
	}
	public string ContentKey {
		get => ContentText.StringReference.GetLocalizedString();
		set => ContentText.StringReference.SetReference(UIManager.LocalizationTable, value);
	}
	public string ConfirmKey {
		get => ConfirmButton.StringReference.GetLocalizedString();
		set => ConfirmButton.StringReference.SetReference(UIManager.LocalizationTable, value);
	}
	public string CancelKey {
		get => CancelButton.StringReference.GetLocalizedString();
		set => CancelButton.StringReference.SetReference(UIManager.LocalizationTable, value);
	}

	public UnityEvent ConfirmEvent => m_ConfirmEvent;
	public UnityEvent CancelEvent  => m_CancelEvent;



	// Methods

	public override void Hide(bool keepState = false) {
		gameObject.SetActive(false);
		if (!keepState) {
			ConfirmEvent.RemoveAllListeners();
			CancelEvent .RemoveAllListeners();
		}
	}

	public void OnConfirmButtonClick() {
		ConfirmEvent.Invoke();
		UIManager.Back();
	}
	public void OnCancelButtonClick() {
		CancelEvent.Invoke();
		UIManager.Back();
	}

	public void SetSelectedCancel() {
		UIManager.Selected = Cancel;
	}
}

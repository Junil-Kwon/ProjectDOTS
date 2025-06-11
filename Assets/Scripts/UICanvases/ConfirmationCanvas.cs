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

			if (I.Raycaster) {
				LabelField("Selected", EditorStyles.boldLabel);
				I.FirstSelected = ObjectField("First Selected", I.FirstSelected);
				Space();
			}
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

	[SerializeField] LocalizeStringEvent m_HeaderText;
	[SerializeField] LocalizeStringEvent m_ContentText;
	[SerializeField] LocalizeStringEvent m_ConfirmButton;
	[SerializeField] LocalizeStringEvent m_CancelButton;

	readonly UnityEvent m_OnConfirmed = new();
	readonly UnityEvent m_OnCancelled = new();



	// Properties

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

	public UnityEvent OnConfirmed => m_OnConfirmed;
	public UnityEvent OnCancelled => m_OnCancelled;



	// Methods

	public override void Hide(bool keepState = false) {
		gameObject.SetActive(false);
		if (!keepState) {
			OnConfirmed.RemoveAllListeners();
			OnCancelled.RemoveAllListeners();
		}
	}

	public void OnConfirmButtonClick() {
		OnConfirmed.Invoke();
		UIManager.Back();
	}
	public void OnCancelButtonClick() {
		OnCancelled.Invoke();
		UIManager.Back();
	}

	public void SetSelectedCancel() {
		if (CancelButton.TryGetComponent(out Selectable cancel)) {
			if (!UIManager.IsPointerClicked) UIManager.Selected = cancel;
		}
	}
}

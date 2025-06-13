using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using System;

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

			if (I.Raycaster && I.Raycaster.enabled) {
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

	Action m_OnConfirmed;
	Action m_OnCancelled;



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

	public LocalizedString HeaderReference {
		get => HeaderText.StringReference;
		set => HeaderText.StringReference = value;
	}
	public LocalizedString ContentReference {
		get => ContentText.StringReference;
		set => ContentText.StringReference = value;
	}
	public LocalizedString ConfirmReference {
		get => ConfirmButton.StringReference;
		set => ConfirmButton.StringReference = value;
	}
	public LocalizedString CancelReference {
		get => CancelButton.StringReference;
		set => CancelButton.StringReference = value;
	}

	public Action OnConfirmed {
		get => m_OnConfirmed;
		set => m_OnConfirmed = value;
	}
	public Action OnCancelled {
		get => m_OnCancelled;
		set => m_OnCancelled = value;
	}



	// Methods

	public override void Back() => Cancel();

	public void Confirm() {
		UIManager.PopOverlay();
		OnConfirmed?.Invoke();
		OnConfirmed = null;
		OnCancelled = null;
	}
	public void Cancel() {
		UIManager.PopOverlay();
		OnCancelled?.Invoke();
		OnConfirmed = null;
		OnCancelled = null;
	}
}

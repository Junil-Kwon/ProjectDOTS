using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

using TMPro;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Custom Button
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI/Custom Button")]
public sealed class CustomButton : Selectable, IPointerClickHandler {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(CustomButton))]
		class CustomButtonEditor : SelectableEditorExtensions {
			CustomButton I => target as CustomButton;
			public override void OnInspectorGUI() {
				Begin("Custom Button");

				LabelField("Selectable", EditorStyles.boldLabel);
				base.OnInspectorGUI();
				Space();
				I.TextUGUI = ObjectField("Text UGUI", I.TextUGUI);
				if (I.TextUGUI) {
					I.Text = TextField("Text", I.Text);
					BeginHorizontal();
					PrefixLabel("Alignment");
					if (Button("Left"  )) I.TextUGUI.alignment = TextAlignmentOptions.Left;
					if (Button("Center")) I.TextUGUI.alignment = TextAlignmentOptions.Center;
					if (Button("Right" )) I.TextUGUI.alignment = TextAlignmentOptions.Right;
					EndHorizontal();
				}
				Space();
				LabelField("Button Event", EditorStyles.boldLabel);
				PropertyField("m_OnStateUpdated");
				PropertyField("m_OnClick");
				Space();

				End();
			}
		}
	#endif



	// Fields

	[SerializeField] TextMeshProUGUI m_TextUGUI;

	[SerializeField] UnityEvent<CustomButton> m_OnStateUpdated = new();
	[SerializeField] UnityEvent m_OnClick = new();



	// Properties

	public RectTransform Transform => transform as RectTransform;

	TextMeshProUGUI TextUGUI {
		get => m_TextUGUI;
		set => m_TextUGUI = value;
	}
	public string Text {
		get => TextUGUI.text;
		set => TextUGUI.text = value;
	}

	public UnityEvent<CustomButton> OnStateUpdated => m_OnStateUpdated;
	public UnityEvent OnClick => m_OnClick;



	// Methods

	public void Refresh() {
		OnStateUpdated.Invoke(this);
	}



	// Event Handlers

	public void OnPointerClick(PointerEventData eventData) {
		if (interactable) OnClick.Invoke();
	}

	public void OnSubmit() {
		if (interactable) {
			DoStateTransition(SelectionState.Pressed, false);
			OnClick.Invoke();
		}
	}



	// Lifecycle

	protected override void OnEnable() {
		base.OnEnable();
		Refresh();
	}
}

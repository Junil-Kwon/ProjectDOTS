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
public class CustomButton : Selectable, IPointerClickHandler {

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
				LabelField("Button Text", EditorStyles.boldLabel);
				I.TextMeshProUGUI = ObjectField("TMPro UGUI", I.TextMeshProUGUI);
				if (I.TextMeshProUGUI) {
					I.Text = TextField("Text", I.Text);
					BeginHorizontal();
					PrefixLabel("Alignment");
					if (Button("Left"  )) I.TextMeshProUGUI.alignment = TextAlignmentOptions.Left;
					if (Button("Center")) I.TextMeshProUGUI.alignment = TextAlignmentOptions.Center;
					if (Button("Right" )) I.TextMeshProUGUI.alignment = TextAlignmentOptions.Right;
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

	[SerializeField] TextMeshProUGUI m_TextMeshProUGUI;

	[SerializeField] UnityEvent<CustomButton> m_OnStateUpdated;
	[SerializeField] UnityEvent m_OnClick;



	// Properties

	public RectTransform Transform => transform as RectTransform;

	TextMeshProUGUI TextMeshProUGUI {
		get => m_TextMeshProUGUI;
		set => m_TextMeshProUGUI = value;
	}
	public string Text {
		get => TextMeshProUGUI.text;
		set => TextMeshProUGUI.text = value;
	}

	public UnityEvent<CustomButton> OnStateUpdated => m_OnStateUpdated;
	public UnityEvent OnClick => m_OnClick;



	// Methods

	public void Refresh() {
		OnStateUpdated.Invoke(this);
	}



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

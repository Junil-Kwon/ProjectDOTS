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
public class CustomButton : Selectable, IBaseWidget, IPointerClickHandler, ISubmitHandler {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(CustomButton))]
	class CustomButtonEditor : EditorExtensionsSelectable {
		CustomButton I => target as CustomButton;
		public override void OnInspectorGUI() {
			Begin("Custom Button");

			LabelField("Selectable", EditorStyles.boldLabel);
			base.OnInspectorGUI();
			Space();
			LabelField("Button", EditorStyles.boldLabel);
			I.TextUGUI = ObjectField("Text UGUI", I.TextUGUI);
			if (I.TextUGUI) I.Text = TextField("Text", I.Text);
			Space();
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

	public void Restore() { }



	// Event Handlers

	public void OnPointerClick(PointerEventData eventData) {
		UIManager.IsPointerClicked = true;
		if (interactable) OnClick.Invoke();
	}

	public void OnSubmit(BaseEventData eventData) {
		UIManager.IsPointerClicked = false;
		if (interactable) OnClick.Invoke();
	}



	// Lifecycle

	protected override void OnEnable() {
		base.OnEnable();
		Refresh();
	}
}

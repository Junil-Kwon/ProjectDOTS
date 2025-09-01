using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Custom Button
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI Widget/Custom Button")]
public class CustomButton : Selectable, IWidgetBase, IPointerClickHandler, ISubmitHandler {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(CustomButton))]
	class CustomButtonEditor : EditorExtensionsSelectable {
		CustomButton I => target as CustomButton;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Selectable", EditorStyles.boldLabel);
			base.OnInspectorGUI();
			Space();

			LabelField("Custom Button", EditorStyles.boldLabel);
			PropertyField("m_OnClick");
			PropertyField("m_OnRefreshed");
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] UnityEvent m_OnClick = new();
	[SerializeField] UnityEvent<CustomButton> m_OnRefreshed = new();



	// Properties

	public UnityEvent OnClick {
		get => m_OnClick;
	}
	public UnityEvent<CustomButton> OnRefreshed {
		get => m_OnRefreshed;
	}



	// Methods

	public void Refresh() {
		OnRefreshed.Invoke(this);
	}

	public void Restore() { }



	// Event Handlers

	public override void OnPointerEnter(PointerEventData eventData) {
		if (interactable) {
			if (UIManager.Selected != this) UIManager.Selected = this;
			base.OnPointerEnter(eventData);
		}
	}

	public override void OnPointerExit(PointerEventData eventData) {
		if (interactable) {
			if (UIManager.Selected == this) UIManager.Selected = null;
			base.OnPointerExit(eventData);
		}
	}



	public void OnPointerClick(PointerEventData eventData) {
		if (interactable) OnClick.Invoke();
	}

	public void OnSubmit(BaseEventData eventData) {
		if (interactable) OnClick.Invoke();
	}



	// Lifecycle

	protected override void OnEnable() {
		base.OnEnable();
		Refresh();
	}
}

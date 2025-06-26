using UnityEngine;
using UnityEngine.UI;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Base Canvas
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[RequireComponent(typeof(Canvas))]
public abstract class BaseCanvas : MonoBehaviour {

	// Fields

	[SerializeField] Selectable m_FirstSelected;
	Selectable m_LastSelected;

	GraphicRaycaster m_Raycaster;



	// Properties

	public Selectable FirstSelected {
		get => m_FirstSelected;
		set => m_FirstSelected = value;
	}
	public Selectable LastSelected {
		get => m_LastSelected;
		set => m_LastSelected = value;
	}

	protected GraphicRaycaster Raycaster =>
		m_Raycaster || TryGetComponent(out m_Raycaster) ?
		m_Raycaster : null;



	// Methods

	public virtual void Show() {
		gameObject.SetActive(true);
		if (Raycaster && Raycaster.enabled) {
			UIManager.Selected = !InputManager.IsPointing switch {
				true  => LastSelected ? LastSelected : FirstSelected,
				false => null,
			};
		}
	}

	public virtual void Hide(bool keepState = false) {
		gameObject.SetActive(false);
		if (Raycaster && Raycaster.enabled) {
			LastSelected = UIManager.Selected;
		}
	}

	public virtual void Back() {
		UIManager.PopOverlay();
	}



	// Lifecycle

	protected virtual void Update() {
		if (UIManager.CurrentCanvas != this) return;
		if (InputManager.GetKeyUp(KeyAction.Cancel)) Back();
		if (InputManager.Navigate != Vector2.zero && !UIManager.Selected) {
			UIManager.Selected = FirstSelected;
		}
	}
}

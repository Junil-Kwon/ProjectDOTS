using UnityEngine;
using UnityEngine.UI;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Screen Base
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[DisallowMultipleComponent]
public abstract class ScreenBase : MonoBehaviour {

	// Fields

	[SerializeField] Selectable m_DefaultSelected;
	Selectable m_CurrentSelected;



	// Properties

	public virtual bool IsPrimary => false;
	public virtual bool IsOverlay => true;
	public virtual bool UseScreenBlur => false;



	public Selectable DefaultSelected {
		get => m_DefaultSelected;
		set => m_DefaultSelected = value;
	}
	public Selectable CurrentSelected {
		get => m_CurrentSelected;
		set => m_CurrentSelected = value;
	}



	// Methods

	public virtual void Show() {
		var selected = CurrentSelected ?? DefaultSelected;
		UIManager.Selected = InputManager.IsPointerMode ? null : selected;
		transform.SetAsLastSibling();
		gameObject.SetActive(true);
	}

	public virtual void Hide() {
		UIManager.Selected = null;
		gameObject.SetActive(false);
	}

	public virtual void Back() {
		UIManager.CloseScreen(this);
	}



	// Lifecycle

	protected virtual void Update() {
		if (UIManager.CurrentScreen == this.ToScreen()) {
			if (UIManager.Selected == null && InputManager.Navigate != default) {
				UIManager.Selected = CurrentSelected ? CurrentSelected : DefaultSelected;
			} else CurrentSelected = UIManager.Selected;
			if (InputManager.GetKeyUp(KeyAction.Cancel)) Back();
		}
	}
}

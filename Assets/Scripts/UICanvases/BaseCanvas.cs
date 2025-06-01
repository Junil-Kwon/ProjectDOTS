using UnityEngine;
using UnityEngine.UI;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Base Canvas
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public abstract class BaseCanvas : MonoBehaviour {

	// Fields

	[SerializeField] Selectable m_FirstSelected;
	Selectable m_LastSelected;



	// Properties

	public Selectable FirstSelected {
		get => m_FirstSelected;
		set => m_FirstSelected = value;
	}
	public Selectable LastSelected {
		get => m_LastSelected;
		set => m_LastSelected = value;
	}



	// Methods

	public virtual void Show() {
		gameObject.SetActive(true);
		if (UIManager.IsPointerClicked) {
			UIManager.Selected = null;
		} else {
			UIManager.Selected = LastSelected ? LastSelected : FirstSelected;
		}
	}
	public virtual void Hide(bool keepState = false) {
		gameObject.SetActive(false);
		LastSelected = UIManager.Selected;
	}
}

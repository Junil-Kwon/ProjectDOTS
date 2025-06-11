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
		if (Raycaster.enabled) {
			UIManager.Selected = UIManager.IsPointerClicked switch {
				false => LastSelected ? LastSelected : FirstSelected,
				true  => null,
			};
		}
	}

	public virtual void Hide(bool keepState = false) {
		gameObject.SetActive(false);
		if (Raycaster.enabled) {
			LastSelected = UIManager.Selected;
		}
	}
}

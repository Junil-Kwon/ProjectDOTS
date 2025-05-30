using UnityEngine;
using UnityEngine.Events;

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
				
				LabelField("", EditorStyles.boldLabel);
				Space();

				End();
			}
		}
	#endif



	// Fields

	readonly UnityEvent m_PositiveResponse = new();
	readonly UnityEvent m_NegativeResponse = new();



	// Properties

	public UnityEvent PositiveResponse => m_PositiveResponse;
	public UnityEvent NegativeResponse => m_NegativeResponse;



	// Methods

	public override void Show() {
		base.Show();
		PositiveResponse.RemoveAllListeners();
		NegativeResponse.RemoveAllListeners();
	}

	public void OnPositiveResponse() {
		PositiveResponse.Invoke();
		UIManager.Back();
	}
	public void OnNegativeResponse() {
		NegativeResponse.Invoke();
		UIManager.Back();
	}
}

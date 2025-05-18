using UnityEngine;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Confirmation Canvas
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI/Confirmation Canvas")]
public class ConfirmationCanvas : MonoBehaviour {

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



    // Properties

}

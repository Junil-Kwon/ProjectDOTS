using UnityEngine;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Fade Canvas
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI/Fade Canvas")]
public class FadeCanvas : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(FadeCanvas))]
		class FadeCanvasEditor : EditorExtensions {
			FadeCanvas I => target as FadeCanvas;
			public override void OnInspectorGUI() {
				Begin("Fade Canvas");
				
				LabelField("", EditorStyles.boldLabel);
				Space();

				End();
			}
		}
	#endif



    // Fields



    // Properties

}

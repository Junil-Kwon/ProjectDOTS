using UnityEngine;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Title Canvas
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI/Title Canvas")]
public class TitleCanvas : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(TitleCanvas))]
		class TitleCanvasEditor : EditorExtensions {
			TitleCanvas I => target as TitleCanvas;
			public override void OnInspectorGUI() {
				Begin("Title Canvas");
				
				LabelField("", EditorStyles.boldLabel);
				Space();

				End();
			}
		}
	#endif



    // Fields



    // Properties

}

using UnityEngine;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Dialogue Canvas
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI/Dialogue Canvas")]
public class DialogueCanvas : BaseCanvas {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(DialogueCanvas))]
		class DialogueCanvasEditor : EditorExtensions {
			DialogueCanvas I => target as DialogueCanvas;
			public override void OnInspectorGUI() {
				Begin("Dialogue Canvas");
				
				LabelField("", EditorStyles.boldLabel);
				Space();

				End();
			}
		}
	#endif



	// Fields



	// Properties



	// Methods

}

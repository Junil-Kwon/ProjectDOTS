using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Title Canvas
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI/Title Canvas")]
public class TitleCanvas : BaseCanvas {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(TitleCanvas))]
	class TitleCanvasEditor : EditorExtensions {
		TitleCanvas I => target as TitleCanvas;
		public override void OnInspectorGUI() {
			Begin("Title Canvas");

			if (I.Raycaster) {
				LabelField("Selected", EditorStyles.boldLabel);
				I.FirstSelected = ObjectField("First Selected", I.FirstSelected);
				Space();
			}
			End();
		}
	}
	#endif

}

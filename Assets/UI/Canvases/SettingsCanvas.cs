using UnityEngine;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Settings Canvas
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI/Settings Canvas")]
public class SettingsCanvas : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(SettingsCanvas))]
		class SettingsCanvasEditor : EditorExtensions {
			SettingsCanvas I => target as SettingsCanvas;
			public override void OnInspectorGUI() {
				Begin("Settings Canvas");
				
				LabelField("", EditorStyles.boldLabel);
				Space();

				End();
			}
		}
	#endif



    // Fields



    // Properties

}

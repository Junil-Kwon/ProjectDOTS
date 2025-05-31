using UnityEngine;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Achievement Canvas
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI/Achievement Canvas")]
public class AchievementCanvas : BaseCanvas {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(AchievementCanvas))]
		class AchievementCanvasEditor : EditorExtensions {
			AchievementCanvas I => target as AchievementCanvas;
			public override void OnInspectorGUI() {
				Begin("Achievement Canvas");

				End();
			}
		}
	#endif



	// Fields



	// Methods

}

using UnityEngine;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Sound Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/Sound Manager")]
public class SoundManager : MonoSingleton<SoundManager> {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(SoundManager))]
		class SoundManagerEditor : EditorExtensions {
			SoundManager I => target as SoundManager;
			public override void OnInspectorGUI() {
				Begin("Sound Manager");

				End();
			}
		}
	#endif

}

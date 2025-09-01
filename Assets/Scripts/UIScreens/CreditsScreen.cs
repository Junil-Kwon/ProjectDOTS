using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Credits Screen
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI Screen/Credits Screen")]
public sealed class CreditsScreen : ScreenBase {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(CreditsScreen))]
	class CreditsScreenEditor : EditorExtensions {
		CreditsScreen I => target as CreditsScreen;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Selected", EditorStyles.boldLabel);
			I.DefaultSelected = ObjectField("Default Selected", I.DefaultSelected);
			Space();

			End();
		}
	}
#endif



	// Properties

	public override bool IsPrimary => false;
	public override bool IsOverlay => false;
	public override bool UseScreenBlur => false;
}

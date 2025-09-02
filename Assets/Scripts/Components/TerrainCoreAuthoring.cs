using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Terrain Core Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Terrain Core")]
[RequireComponent(typeof(TerrainCoreAuthoring))]
public sealed class TerrainCoreAuthoring : MonoComponent<TerrainCoreAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(TerrainCoreAuthoring))]
	class TerrainCoreAuthoringEditor : EditorExtensions {
		TerrainCoreAuthoring I => target as TerrainCoreAuthoring;
		public override void OnInspectorGUI() {
			Begin();

			if (Enum.TryParse(I.name, out Terrain terrain)) {
				BeginDisabledGroup();
				EnumField("Terrain", terrain);
				EndDisabledGroup();
				Space();
			}

			BeginDisabledGroup(I.IsPrefabConnected);
			//..
			EndDisabledGroup();

			End();
		}
	}
	#endif
}

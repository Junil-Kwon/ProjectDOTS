using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Terrain Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Terrain")]
[RequireComponent(typeof(TerrainAuthoring))]
public sealed class TerrainAuthoring : MonoComponent<TerrainAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(TerrainAuthoring))]
	class ParticleAuthoringEditor : EditorExtensions {
		TerrainAuthoring I => target as TerrainAuthoring;
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

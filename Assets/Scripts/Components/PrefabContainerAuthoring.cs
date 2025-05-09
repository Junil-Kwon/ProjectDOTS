using UnityEngine;
using System;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Prefab Container Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Prefab Container")]
public class PrefabContainerAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(PrefabContainerAuthoring))]
		class PrefabContainerAuthoringEditor : EditorExtensions {
			PrefabContainerAuthoring I => target as PrefabContainerAuthoring;
			public override void OnInspectorGUI() {
				Begin("Prefab Container Authoring Authoring");



				End();
			}
		}
	#endif



	// Baker 

	public class Baker : Baker<PrefabContainerAuthoring> {
		public override void Bake(PrefabContainerAuthoring authoring) {
			var prefabs = new GameObject[Enum.GetValues(typeof(Body)).Length];
			foreach (var prefab in Resources.LoadAll<GameObject>("Prefabs")) {
				if (Enum.TryParse(prefab.name, out Body body)) prefabs[(int)body] = prefab; 
			}
			var entity = GetEntity(TransformUsageFlags.None);
			var buffer = AddBuffer<PrefabContainer>(entity);
			for (int i = 0; i < prefabs.Length; i++) buffer.Add(new PrefabContainer {
				Prefab = GetEntity(prefabs[i], TransformUsageFlags.None),
			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Prefab Container
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[InternalBufferCapacity(1024)]
public struct PrefabContainer : IBufferElementData {

	// Fields

	public Entity Prefab;
}

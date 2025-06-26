using UnityEngine;
using System;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEditor;
#endif



// Prefab Names

public enum Prefab : uint {
	Dummy,
	Player,

	SmokeMini,
	SmokeTiny,
	Landing,
}



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
			Begin("Prefab Container Authoring");

			End();
		}
	}
	#endif



	// Baker 

	public class Baker : Baker<PrefabContainerAuthoring> {
		public override void Bake(PrefabContainerAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.None);
			var buffer = AddBuffer<PrefabContainer>(entity);
			var prefabs = new NativeArray<PrefabContainer>(1024, Allocator.Temp);
			foreach (var gameObject in Resources.LoadAll<GameObject>("")) {
				if (Enum.TryParse(gameObject.name, out Prefab prefab)) {
					prefabs[(int)prefab] = new PrefabContainer {

						Prefab = GetEntity(gameObject, TransformUsageFlags.None),

					};
				}
			}
			buffer.AddRange(prefabs);
			prefabs.Dispose();
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Prefab Container
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[InternalBufferCapacity(1024)]
public struct PrefabContainer : IBufferElementData {

	public Entity Prefab;
}

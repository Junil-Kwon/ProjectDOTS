using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// NavMesh Manager Bridge Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/NavMesh Manager Bridge")]
public class NavMeshManagerBridgeAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(NavMeshManagerBridgeAuthoring))]
		class AIManagerBridgeAuthoringEditor : EditorExtensions {
			NavMeshManagerBridgeAuthoring I => target as NavMeshManagerBridgeAuthoring;
			public override void OnInspectorGUI() {
				Begin("NavMesh Manager Bridge Authoring");

				End();
			}
		}
	#endif



	// Baker

	public class Baker : Baker<NavMeshManagerBridgeAuthoring> {
		public override void Bake(NavMeshManagerBridgeAuthoring authoring) {
			Entity entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new NavMeshManagerBridge());
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// NavMesh Manager Bridge
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct NavMeshManagerBridge : IComponentData {

	// Fields

	uint m_Flag;



	// Properties

	public uint Flag {
		get => m_Flag;
		set => m_Flag = value;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// NavMesh Manager Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class NavMeshManagerBridgeSystem : SystemBase {

	[BurstCompile]
	protected override void OnCreate() {
		RequireForUpdate<NavMeshManagerBridge>();
	}

	[BurstDiscard]
	protected override void OnUpdate() {
		var bridge = SystemAPI.GetSingletonRW<NavMeshManagerBridge>();
		var flag   = bridge.ValueRO.Flag;

		if (flag != 0u) bridge.ValueRW.Flag = 0u;
	}
}

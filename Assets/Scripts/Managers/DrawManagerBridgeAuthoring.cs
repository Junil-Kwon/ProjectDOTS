using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Draw Manager Bridge Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/Draw Manager Bridge")]
public class DrawManagerBridgeAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(DrawManagerBridgeAuthoring))]
		class DrawManagerBridgeAuthoringEditor : EditorExtensions {
			DrawManagerBridgeAuthoring I => target as DrawManagerBridgeAuthoring;
			public override void OnInspectorGUI() {
				Begin("Draw Manager Bridge Authoring");

				End();
			}
		}
	#endif



	// Baker

	public class Baker : Baker<DrawManagerBridgeAuthoring> {
		public override void Bake(DrawManagerBridgeAuthoring authoring) {
			Entity entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new DrawManagerBridge());
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Draw Manager Bridge
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct DrawManagerBridge : IComponentData {

	// Fields

	uint m_Flag;



	// Properties

	public uint Flag {
		get => m_Flag;
		set => m_Flag = value;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Draw Manager Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class DrawManagerBridgeSystem : SystemBase {

	[BurstCompile]
	protected override void OnCreate() {
		RequireForUpdate<DrawManagerBridge>();
	}

	[BurstDiscard]
	protected override void OnUpdate() {
		var bridge = SystemAPI.GetSingletonRW<DrawManagerBridge>();
		var flag   = bridge.ValueRO.Flag;

		if (flag != 0u) bridge.ValueRW.Flag = 0u;
	}
}

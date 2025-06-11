using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// AI Manager Bridge Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/AI Manager Bridge")]
public class AIManagerBridgeAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(AIManagerBridgeAuthoring))]
	class AIManagerBridgeAuthoringEditor : EditorExtensions {
		AIManagerBridgeAuthoring I => target as AIManagerBridgeAuthoring;
		public override void OnInspectorGUI() {
			Begin("AI Manager Bridge Authoring");

			End();
		}
	}
	#endif



	// Baker

	public class Baker : Baker<AIManagerBridgeAuthoring> {
		public override void Bake(AIManagerBridgeAuthoring authoring) {
			Entity entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new AIManagerBridge {

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// AI Manager Bridge
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct AIManagerBridge : IComponentData {

	// Fields

	public uint Flag;



	// Properties

}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// AI Manager Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class AIManagerBridgeSystem : SystemBase {

	protected override void OnCreate() {
		RequireForUpdate<AIManagerBridge>();
	}

	protected override void OnUpdate() {
		var bridge = SystemAPI.GetSingletonRW<AIManagerBridge>();

		var i = bridge.ValueRO;

		ref var o = ref bridge.ValueRW;

		o.Flag = 0u;
	}
}

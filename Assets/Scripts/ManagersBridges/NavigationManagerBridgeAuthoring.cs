using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Navigation Manager Bridge Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/Navigation Manager Bridge")]
public class NavigationManagerBridgeAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(NavigationManagerBridgeAuthoring))]
	class NavigationManagerBridgeAuthoringEditor : EditorExtensions {
		NavigationManagerBridgeAuthoring I => target as NavigationManagerBridgeAuthoring;
		public override void OnInspectorGUI() {
			Begin("Navigation Manager Bridge Authoring");

			End();
		}
	}
	#endif



	// Baker

	public class Baker : Baker<NavigationManagerBridgeAuthoring> {
		public override void Bake(NavigationManagerBridgeAuthoring authoring) {
			Entity entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new NavigationManagerBridge {

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Navigation Manager Bridge
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct NavigationManagerBridge : IComponentData {

	// Fields

	public uint Flag;



	// Properties

}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Navigation Manager Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class NavigationManagerBridgeSystem : SystemBase {

	protected override void OnCreate() {
		RequireForUpdate<NavigationManagerBridge>();
	}

	protected override void OnUpdate() {
		var bridge = SystemAPI.GetSingletonRW<NavigationManagerBridge>();

		var i = bridge.ValueRO;

		ref var o = ref bridge.ValueRW;

		o.Flag = 0u;
	}
}

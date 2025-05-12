using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager Bridge Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/Network Manager Bridge")]
public class NetworkManagerBridgeAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(NetworkManagerBridgeAuthoring))]
		class NetworkManagerBridgeAuthoringEditor : EditorExtensions {
			NetworkManagerBridgeAuthoring I => target as NetworkManagerBridgeAuthoring;
			public override void OnInspectorGUI() {
				Begin("Network Manager Bridge Authoring");

				End();
			}
		}
	#endif



	// Baker

	public class Baker : Baker<NetworkManagerBridgeAuthoring> {
		public override void Bake(NetworkManagerBridgeAuthoring authoring) {
			Entity entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new NetworkManagerBridge());
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager Bridge
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct NetworkManagerBridge : IComponentData {

	// Fields

	uint m_Flag;



	// Properties

	public uint Flag {
		get => m_Flag;
		set => m_Flag = value;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class NetworkManagerBridgeSystem : SystemBase {

	[BurstCompile]
	protected override void OnCreate() {
		RequireForUpdate<NetworkManagerBridge>();
	}

	[BurstDiscard]
	protected override void OnUpdate() {
		var bridge = SystemAPI.GetSingletonRW<NetworkManagerBridge>();
		var flag   = bridge.ValueRO.Flag;

		if (flag != 0u) bridge.ValueRW.Flag = 0u;
	}
}

using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Environment Manager Bridge Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/Environment Manager Bridge")]
public class EnvironmentManagerBridgeAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(EnvironmentManagerBridgeAuthoring))]
		class EnvironmentManagerBridgeAuthoringEditor : EditorExtensions {
			EnvironmentManagerBridgeAuthoring I => target as EnvironmentManagerBridgeAuthoring;
			public override void OnInspectorGUI() {
				Begin("Environment Manager Bridge Authoring");
				End();
			}
		}
	#endif



	// Baker

	public class Baker : Baker<EnvironmentManagerBridgeAuthoring> {
		public override void Bake(EnvironmentManagerBridgeAuthoring authoring) {
			Entity entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new EnvironmentManagerBridge());
		}
	}

}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Environment Manager Bridge
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct EnvironmentManagerBridge : IComponentData {

	// Fields

	float m_TimeOfDay;

	uint m_Flag;



	// Properties

	public float TimeOfDay {
		get => m_TimeOfDay;
		set {
			m_TimeOfDay = value;
			m_Flag |= 0x0001u;
		}
	}



	public uint Flag {
		get => m_Flag;
		set => m_Flag = value;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Environment Manager Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class EnvironmentManagerBridgeSystem : SystemBase {

	[BurstCompile]
	protected override void OnCreate() {
		RequireForUpdate<EnvironmentManagerBridge>();
	}

	[BurstDiscard]
	protected override void OnUpdate() {
		var bridge = SystemAPI.GetSingletonRW<EnvironmentManagerBridge>();
		var flag   = bridge.ValueRO.Flag;

		if ((flag & 0x0001u) != 0u) EnvironmentManager.TimeOfDay = bridge.ValueRO.TimeOfDay;

		bridge.ValueRW.TimeOfDay = EnvironmentManager.TimeOfDay;

		if (flag != 0u) bridge.ValueRW.Flag = 0u;
	}
}

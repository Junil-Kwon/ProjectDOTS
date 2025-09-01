using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.NetCode;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// World Regulator Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/World Regulator")]
public sealed class WorldRegulatorAuthoring : MonoComponent<WorldRegulatorAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(WorldRegulatorAuthoring))]
	class WorldRegulatorAuthoringEditor : EditorExtensions {
		WorldRegulatorAuthoring I => target as WorldRegulatorAuthoring;
		public override void OnInspectorGUI() {
			Begin();



			End();
		}
	}
	#endif



	// Fields



	// Baker

	class Baker : Baker<WorldRegulatorAuthoring> {
		public override void Bake(WorldRegulatorAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new WorldRegulator {

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// World Regulator
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[GhostComponent]
public struct WorldRegulator : IComponentData {

	// Fields

	[GhostField] public float TimeOfDay;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// World Regulator System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(DOTSServerSimulationSystemGroup))]
public partial class WorldRegulatorServerSystem : SystemBase {

	protected override void OnCreate() {
		RequireForUpdate<WorldRegulator>();
	}

	protected override void OnUpdate() {
		var worldRegulator = SystemAPI.GetSingletonRW<WorldRegulator>();
		float delta = SystemAPI.Time.DeltaTime / EnvironmentManager.DayLength;
		worldRegulator.ValueRW.TimeOfDay = EnvironmentManager.TimeOfDay += delta;
	}
}

[UpdateInGroup(typeof(DOTSClientSimulationSystemGroup))]
public partial class WorldRegulatorClientSystem : SystemBase {

	protected override void OnCreate() {
		RequireForUpdate<WorldRegulator>();
	}

	protected override void OnUpdate() {
		var worldRegulator = SystemAPI.GetSingletonRW<WorldRegulator>();
		EnvironmentManager.TimeOfDay = worldRegulator.ValueRO.TimeOfDay;
	}
}

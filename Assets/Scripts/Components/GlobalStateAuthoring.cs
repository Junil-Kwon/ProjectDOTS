using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.NetCode;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Global State Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Global State")]
[RequireComponent(typeof(GhostAuthoringComponent))]
public sealed class GlobalStateAuthoring : MonoComponent<GlobalStateAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(GlobalStateAuthoring))]
	class GlobalStateAuthoringEditor : EditorExtensions {
		GlobalStateAuthoring I => target as GlobalStateAuthoring;
		public override void OnInspectorGUI() {
			Begin();

			End();
		}
	}
	#endif



	// Baker

	class Baker : Baker<GlobalStateAuthoring> {
		public override void Bake(GlobalStateAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new GlobalState {

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Global State
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[GhostComponent]
public struct GlobalState : IComponentData {

	// Fields

	[GhostField] public float TimeOfDay;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Global State System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(DOTSServerSimulationSystemGroup))]
public partial class GlobalStateServerSystem : SystemBase {

	protected override void OnCreate() {
		RequireForUpdate<GlobalState>();
	}

	protected override void OnUpdate() {
		var globalState = SystemAPI.GetSingletonRW<GlobalState>();
		float delta = SystemAPI.Time.DeltaTime / EnvironmentManager.DayLength;
		globalState.ValueRW.TimeOfDay = EnvironmentManager.TimeOfDay += delta;
	}
}

[UpdateInGroup(typeof(DOTSClientSimulationSystemGroup))]
public partial class GlobalStateClientSystem : SystemBase {

	protected override void OnCreate() {
		RequireForUpdate<GlobalState>();
	}

	protected override void OnUpdate() {
		var globalState = SystemAPI.GetSingletonRW<GlobalState>();
		EnvironmentManager.TimeOfDay = globalState.ValueRO.TimeOfDay;
	}
}

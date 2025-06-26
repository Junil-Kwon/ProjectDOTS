using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Physics;
using Unity.NetCode;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Ghost Color Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Ghost Color")]
public class GhostColorAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(GhostColorAuthoring))]
	class GhostColorAuthoringEditor : EditorExtensions {
		GhostColorAuthoring I => target as GhostColorAuthoring;
		public override void OnInspectorGUI() {
			Begin("Ghost Color Authoring");

			I.Value = ColorField("Value", I.Value);

			End();
		}
	}
	#endif



	// Fields

	public Color32 m_Value;



	// Properties

	public Color32 Value {
		get => m_Value;
		set => m_Value = value;
	}



	// Baker

	public class Baker : Baker<GhostColorAuthoring> {
		public override void Bake(GhostColorAuthoring authoring) {
			Entity entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new GhostColor {

				value = authoring.Value,

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Ghost Color
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct GhostColor : IComponentData {

	[GhostField] public color value;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Ghost Color System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSClientSimulationSystemGroup))]
partial struct GhostColorSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<GhostColor>();
	}

	public void OnUpdate(ref SystemState state) {
		state.Dependency = new ChangeTileMaskColorJob {
		}.ScheduleParallel(state.Dependency);
		state.Dependency = new ChangeSpriteMaskColorJob {
		}.ScheduleParallel(state.Dependency);
	}

	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct ChangeTileMaskColorJob : IJobEntity {
		public void Execute(in GhostColor color, DynamicBuffer<TileDrawer> drawer) {
			for (int i = 0; i < drawer.Length; i++) drawer.ElementAt(i).MaskColor = color.value;
		}
	}
	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct ChangeSpriteMaskColorJob : IJobEntity {
		public void Execute(in GhostColor color, DynamicBuffer<SpriteDrawer> drawer) {
			for (int i = 0; i < drawer.Length; i++) drawer.ElementAt(i).MaskColor = color.value;
		}
	}
}

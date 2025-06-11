using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Transforms;
using Unity.Physics;
using Unity.NetCode;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Dummy Head
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct DummyHead : IComponentData {

	public byte Data;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Dummy Head System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(GhostInputSystemGroup))]
partial struct DummyHeadSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<InputManagerBridge>();
		state.RequireForUpdate<CameraManagerBridge>();
		state.RequireForUpdate<DummyHead>();
	}

	public void OnUpdate(ref SystemState state) {
		state.Dependency = new DummyHeadSimulationJob() {
		}.ScheduleParallel(state.Dependency);
	}

	[BurstCompile, WithAll(typeof(GhostOwnerIsLocal), typeof(Simulate))]
	partial struct DummyHeadSimulationJob : IJobEntity {
		public void Execute(
			in CreatureCore core,
			ref CreatureInput input,
			ref DummyHead head) {

		}
	}
}

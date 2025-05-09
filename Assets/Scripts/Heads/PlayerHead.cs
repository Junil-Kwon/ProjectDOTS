using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Transforms;
using Unity.Physics;
using Unity.NetCode;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Player Head
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct PlayerHead : IComponentData {

	// Fields

	byte data;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Player Head System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(GhostInputSystemGroup))]
partial struct PlayerHeadSystem : ISystem {

	[BurstCompile]
	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<InputManagerBridge >();
		state.RequireForUpdate<CameraManagerBridge>();
		state.RequireForUpdate<PlayerHead>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state) {
		var simulationJob = new PlayerHeadSimulationJob() {
			inputManager  = SystemAPI.GetSingleton<InputManagerBridge >(),
			cameraManager = SystemAPI.GetSingleton<CameraManagerBridge>(),
		};
		state.Dependency = simulationJob.ScheduleParallel(state.Dependency);
	}

	[BurstCompile, WithAll(typeof(GhostOwnerIsLocal))]
	partial struct PlayerHeadSimulationJob : IJobEntity {
		public InputManagerBridge  inputManager;
		public CameraManagerBridge cameraManager;
		public void Execute(
			ref CreatureInput input,
			in  CreatureCore  core,
			ref PlayerHead    head) {

			input.Key = inputManager.KeyNext;
			float3 moveDirection = new(inputManager.MoveDirection.x, 0f, inputManager.MoveDirection.y);
			float3 eulerRotation = new(0f, cameraManager.Yaw * math.TORADIANS, 0f);
			input.MoveVector = math.mul(quaternion.Euler(eulerRotation), moveDirection);
		}
	}
}

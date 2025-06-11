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

	public byte Data;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Player Head System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(GhostInputSystemGroup))]
partial struct PlayerHeadSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<InputManagerBridge>();
		state.RequireForUpdate<CameraManagerBridge>();
		state.RequireForUpdate<PlayerHead>();
	}

	public void OnUpdate(ref SystemState state) {
		state.Dependency = new PlayerHeadSimulationJob() {
			InputManager  = SystemAPI.GetSingleton<InputManagerBridge>(),
			CameraManager = SystemAPI.GetSingleton<CameraManagerBridge>(),
		}.ScheduleParallel(state.Dependency);
	}

	[BurstCompile, WithAll(typeof(GhostOwnerIsLocal), typeof(Simulate))]
	partial struct PlayerHeadSimulationJob : IJobEntity {
		[ReadOnly] public InputManagerBridge  InputManager;
		[ReadOnly] public CameraManagerBridge CameraManager;
		public void Execute(
			in CreatureCore core,
			ref CreatureInput input,
			ref PlayerHead head) {

			input.Key = InputManager.KeyNext;
			float3 moveDirection = new(InputManager.MoveDirection.x, 0f, InputManager.MoveDirection.y);
			float3 eulerRotation = new(0f, CameraManager.Yaw * math.TORADIANS, 0f);
			input.MoveVector = math.mul(quaternion.Euler(eulerRotation), moveDirection);
		}
	}
}

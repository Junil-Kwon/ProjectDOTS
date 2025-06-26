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

public struct PlayerHead : IInputComponentData {

	public uint Key;
	public float2 MoveDirection;

	public bool GetKey(KeyAction key) => (Key & (1 << (int)key)) != 0;
	public void SetKey(KeyAction key, bool value) => Key = value switch {
		true  => Key |  (1u << (int)key),
		false => Key & ~(1u << (int)key),
	};
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Player Head System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(GhostInputSystemGroup))]
partial struct PlayerHeadGhostSystem : ISystem {

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
		[ReadOnly] public InputManagerBridge InputManager;
		[ReadOnly] public CameraManagerBridge CameraManager;
		public void Execute(
			ref PlayerHead head) {

			head.Key = InputManager.KeyNext;
			float3 moveDirection = new(InputManager.MoveDirection.x, 0f, InputManager.MoveDirection.y);
			float3 eulerRotation = new(0f, CameraManager.Yaw * math.TORADIANS, 0f);
			float3 rotatedMoveDirection = math.mul(quaternion.Euler(eulerRotation), moveDirection);
			head.MoveDirection = new(rotatedMoveDirection.x, rotatedMoveDirection.z);
		}
	}
}



[BurstCompile]
[UpdateInGroup(typeof(DOTSPredictedSimulationSystemGroup))]
partial struct PlayerHeadSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<PlayerHead>();
		state.RequireForUpdate<CreatureInput>();
	}

	public void OnUpdate(ref SystemState state) {
		state.Dependency = new PlayerHeadInputJob() {
		}.ScheduleParallel(state.Dependency);
	}



	[BurstCompile, WithAll(typeof(GhostOwnerIsLocal), typeof(Simulate))]
	partial struct PlayerHeadInputJob : IJobEntity {
		public void Execute(
			ref PlayerHead head,
			ref CreatureInput input) {

			input.Key = head.Key;
			input.MoveDirection = head.MoveDirection;
		}
	}
}

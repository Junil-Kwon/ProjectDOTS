using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Transforms;
using Unity.Physics;
using Unity.NetCode;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Dummy Body
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct DummyBody : IComponentData {

	public byte Data;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Dummy Body Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSPredictedSimulationSystemGroup))]
partial struct DummyBodySimulationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<NetworkTime>();
		state.RequireForUpdate<DummyBody>();
	}

	public void OnUpdate(ref SystemState state) {
		var networkTime = SystemAPI.GetSingleton<NetworkTime>();
		state.Dependency = new DummyBodySimulationJob {
			IsFullTick = networkTime.IsFirstTimeFullyPredictingTick,
		}.ScheduleParallel(state.Dependency);
	}



	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct DummyBodySimulationJob : IJobEntity {
		public bool IsFullTick;
		public void Execute(
			in DummyBody body,
			in CreatureInput input,
			ref CreatureCore core,
			ref LocalTransform transform,
			ref PhysicsVelocity velocity) {

			switch (core.MotionX) {
				case Motion.None:
					core.MotionX = Motion.Idle;
					break;

				case Motion.Idle:
					velocity.Linear.x = 0f;
					velocity.Linear.z = 0f;
					core.MotionXTick++;
					if (math.any(input.MoveDirection != default)) core.MotionX = Motion.Move;
					if (input.GetKey(KeyAction.Jump) && core.IsGrounded) core.MotionX = Motion.Jump;
					break;

				case Motion.Move:
					if (math.any(input.MoveDirection != default)) {
						float3 vector = new(-input.MoveDirection.x, 0f, -input.MoveDirection.y);
						transform.Rotation = quaternion.LookRotationSafe(vector, math.up());
						velocity.Linear.x = 5f * input.MoveDirection.x;
						velocity.Linear.z = 5f * input.MoveDirection.y;
					}
					core.MotionXTick++;
					if (math.all(input.MoveDirection == default)) core.MotionX = Motion.Idle;
					if (input.GetKey(KeyAction.Jump) && core.IsGrounded) core.MotionX = Motion.Jump;
					break;

				case Motion.Jump:
					velocity.Linear.x = 5f * input.MoveDirection.x;
					velocity.Linear.z = 5f * input.MoveDirection.y;
					if (core.MotionXTick != 10) core.MotionXTick++;
					if (core.MotionXTick ==  5) core.KnockVector += new float3(0f, 2.4f, 0f);
					if (core.MotionXTick == 10 && core.IsGrounded) core.MotionXTick++;
					if (15 < core.MotionXTick) core.MotionX = Motion.Idle;
					break;
			}
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Dummy Body Presentation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSPresentationSystemGroup))]
partial struct DummyBodyPresentationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<DummyBody>();
	}

	public void OnUpdate(ref SystemState state) {
		state.Dependency = new DummyBodyPresentationJob {
		}.ScheduleParallel(state.Dependency);
	}



	[BurstCompile, WithAll(typeof(DummyBody))]
	partial struct DummyBodyPresentationJob : IJobEntity {
		public void Execute(
			in CreatureCore core,
			DynamicBuffer<SpriteDrawer> sprite,
			DynamicBuffer<ShadowDrawer> shadow) {

			sprite.ElementAt(0).Motion = core.MotionX;
			shadow.ElementAt(0).Motion = core.MotionX;
			sprite.ElementAt(0).Offset = core.MotionXOffset;
			shadow.ElementAt(0).Offset = core.MotionXOffset;
		}
	}
}

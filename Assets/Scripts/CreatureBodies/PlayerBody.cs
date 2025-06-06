using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Transforms;
using Unity.Physics;
using Unity.NetCode;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Player Body
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct PlayerBody : IComponentData {

	// Fields

	byte data;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Player Body Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSPredictedSimulationSystemGroup))]
partial struct PlayerBodySimulationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<PlayerBody>();
	}

	public void OnUpdate(ref SystemState state) {
		state.Dependency = new PlayerBodySimulationJob {
		}.ScheduleParallel(state.Dependency);
	}

	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct PlayerBodySimulationJob : IJobEntity {
		public void Execute(
			ref LocalTransform  transform,
			in  CreatureInput   input,
			ref CreatureCore    core,
			ref PlayerBody      body,
			ref PhysicsVelocity velocity) {

			if (!core.HasFlag(Flag.Piercing) && input.GetKey(KeyAction.Ability1)) {
				core.SetFlag(Flag.Piercing, true);
			}
			if (core.HasFlag(Flag.Piercing) && !input.GetKey(KeyAction.Ability1)) {
				core.SetFlag(Flag.Piercing, false);
			}
			switch (core.MotionX) {
				case Motion.None:
					core.MotionX = Motion.Idle;
					break;

				case Motion.Idle:
					velocity.Linear.x = 0f;
					velocity.Linear.z = 0f;
					core.MotionXTick++;
					if (0f < input.MoveFactor) core.MotionX = Motion.Move;
					if (input.GetKey(KeyAction.Jump) && core.IsGrounded()) core.MotionX = Motion.Jump;
					break;

				case Motion.Move:
					if (0 < input.MoveFactor) {
						transform.Rotation = quaternion.LookRotationSafe(-input.MoveVector, math.up());
						velocity.Linear.x = 5f * input.MoveVector.x;
						velocity.Linear.z = 5f * input.MoveVector.z;

					}
					core.MotionXTick++;
					if (input.MoveFactor == 0f) core.MotionX = Motion.Idle;
					if (input.GetKey(KeyAction.Jump) && core.IsGrounded()) core.MotionX = Motion.Jump;
					break;

				case Motion.Jump:
					velocity.Linear.x = 5f * input.MoveVector.x;
					velocity.Linear.z = 5f * input.MoveVector.z;
					if (core.MotionXTick != 10) core.MotionXTick++;
					if (core.MotionXTick ==  5) core.KnockVector += new float3(0f, 2.4f, 0f);
					if (core.MotionXTick == 10 && core.IsGrounded()) core.MotionXTick++;
					if (15 < core.MotionXTick) core.MotionX = Motion.Idle;
					break;
			}
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Player Body Presentation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSPresentationSystemGroup))]
partial struct PlayerBodyPresentationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<PlayerBody>();
	}

	public void OnUpdate(ref SystemState state) {
		state.Dependency = new PlayerBodyPresentationJob {
		}.ScheduleParallel(state.Dependency);
	}

	[BurstCompile, WithAll(typeof(PlayerBody))]
	partial struct PlayerBodyPresentationJob : IJobEntity {
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
using UnityEngine;
using System.Runtime.InteropServices;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Physics;
using Unity.NetCode;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Dummy Body Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Character Body/Dummy Body")]
[RequireComponent(typeof(CharacterCoreAuthoring))]
public sealed class DummyBodyAuthoring : MonoComponent<DummyBodyAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(DummyBodyAuthoring))]
	class DummyBodyAuthoringEditor : EditorExtensions {
		DummyBodyAuthoring I => target as DummyBodyAuthoring;
		public override void OnInspectorGUI() {
			Begin();

			if (I.CharacterCore.Body == default) I.CharacterCore.Body = Body.Dummy;
			else if (I.CharacterCore.Body != Body.Dummy) DestroyImmediate(I, true);
			BeginDisabledGroup(I.IsPrefabConnected);
			var data = new DummyBodyBlob { Data = I.Data };
			data.MoveSpeed = FloatField("Move Speed", data.MoveSpeed);
			data.JumpScale = FloatField("Jump Scale", data.JumpScale);
			I.Data = data.Data;
			EndDisabledGroup();

			End();
		}
	}
	#endif



	// Fields

	CharacterCoreAuthoring m_CharacterCore;



	// Properties

	CharacterCoreAuthoring CharacterCore => !m_CharacterCore ?
		m_CharacterCore = GetOwnComponent<CharacterCoreAuthoring>() :
		m_CharacterCore;

	ref FixedBytes30 Data {
		get => ref CharacterCore.BodyData;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Dummy Body Blob
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[StructLayout(LayoutKind.Explicit)]
public struct DummyBodyBlob {

	// fields

	[FieldOffset(00)] public FixedBytes30 Data;
	[FieldOffset(00)] public float MoveSpeed;
	[FieldOffset(04)] public float JumpScale;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Dummy Body Data
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct DummyBodyData : IComponentData {

	// Fields

	public byte Data;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Dummy Body Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSPredictedSimulationSystemGroup))]
partial struct DummyBodySimulationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<DummyBodyData>();
	}

	public void OnUpdate(ref SystemState state) {
		state.Dependency = new DummyBodySimulationJob {
			DeltaTime = SystemAPI.Time.DeltaTime,
		}.ScheduleParallel(state.Dependency);
	}
}

[BurstCompile, WithAll(typeof(Simulate))]
partial struct DummyBodySimulationJob : IJobEntity {
	[ReadOnly] public float DeltaTime;

	public void Execute(
		in CharacterInput input,
		ref CharacterCoreData coreData,
		in CharacterBodyBlob bodyBlob,
		ref DummyBodyData bodyData,
		ref LocalTransform transform,
		ref PhysicsVelocity velocity) {

		var blobData = new DummyBodyBlob { Data = bodyBlob.Value.Value.Data	};
		switch (coreData.Motion) {
	
			case Motion.Idle: {
				if (math.any(input.MoveDirection != default)) {
					coreData.Motion = Motion.Move;
					coreData.Time = 0f;
					coreData.Data = 0u;
				}
				if (input.GetKey(KeyAction.Jump) && coreData.IsGrounded) {
					coreData.Motion = Motion.Jump;
					coreData.Time = 0f;
					coreData.Data = 0u;
				}
			} break;
	
			case Motion.Move: {
				velocity.Linear.x = blobData.MoveSpeed * input.MoveDirection.x;
				velocity.Linear.z = blobData.MoveSpeed * input.MoveDirection.y;
				if (math.any(input.MoveDirection != default)) {
					var rotation = transform.Rotation.value;
					float x = 0.0f + 2.0f * (rotation.y * rotation.w + rotation.x * rotation.z);
					float z = 1.0f - 2.0f * (rotation.y * rotation.y + rotation.z * rotation.z);
					float aYaw = math.atan2(x, z) * math.TODEGREES;
					x = -input.MoveDirection.x;
					z = -input.MoveDirection.y;
					float bYaw = math.atan2(x, z) * math.TODEGREES;
					float delta = bYaw - aYaw;
					if (+180f < delta) delta -= 360f;
					if (delta < -180f) delta += 360f;
					if (0f < delta) delta = math.min(delta - 1f, +540f * DeltaTime);
					if (delta < 0f) delta = math.max(delta + 1f, -540f * DeltaTime);
					var euler = new float3(0f, (aYaw + delta) * math.TORADIANS, 0f);
					transform.Rotation = quaternion.Euler(euler);
				}
				if (math.all(input.MoveDirection == default)) {
					coreData.Motion = Motion.Idle;
					coreData.Time = 0f;
					coreData.Data = 0u;
				}
				if (input.GetKey(KeyAction.Jump) && coreData.IsGrounded) {
					coreData.Motion = Motion.Jump;
					coreData.Time = 0f;
					coreData.Data = 0u;
				}
			} break;
	
			case Motion.Jump: {
				velocity.Linear.x = blobData.MoveSpeed * input.MoveDirection.x;
				velocity.Linear.z = blobData.MoveSpeed * input.MoveDirection.y;
				if (0.1f < coreData.Time && coreData.Data == 0) {
					coreData.KnockVector += new float3(0f, blobData.JumpScale, 0f);
					coreData.Data = 1u;
				}
				if (0.2f < coreData.Time && coreData.IsGrounded) {
					coreData.Motion = Motion.Idle;
					coreData.Time = 0f;
					coreData.Data = 0u;
				}
			} break;
		}
	}
}

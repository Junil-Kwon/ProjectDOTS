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
// Player Head Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Character Head/Player Head")]
[RequireComponent(typeof(CharacterCoreAuthoring))]
public sealed class PlayerHeadAuthoring : MonoComponent<PlayerHeadAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(PlayerHeadAuthoring))]
	class PlayerHeadAuthoringEditor : EditorExtensions {
		PlayerHeadAuthoring I => target as PlayerHeadAuthoring;
		public override void OnInspectorGUI() {
			Begin();

			if (I.CharacterCore.Head == default) I.CharacterCore.Head = Head.Player;
			else if (I.CharacterCore.Head != Head.Player) DestroyImmediate(I, true);
			BeginDisabledGroup(I.IsPrefabConnected);
			var data = new PlayerHeadBlob { Data = I.Data };
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
		get => ref CharacterCore.HeadData;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Player Head Blob
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[StructLayout(LayoutKind.Explicit)]
public struct PlayerHeadBlob {

	// Fields

	[FieldOffset(00)] public FixedBytes30 Data;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Player Head Data
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct PlayerHeadData : IInputComponentData {

	// Constants

	const uint KeyMask   = 0xFFFFFF00u;
	const uint MoveFMask = 0x000000C0u;
	const uint MoveDMask = 0x0000003Fu;

	const int KeyShift   = 08;
	const int MoveFShift = 06;
	const int MoveDShift = 00;



	// Fields

	public uint Data;



	// Methods

	public uint GetKey() => (Data & KeyMask) >> KeyShift;
	public void SetKey(uint value) => Data = (Data & ~KeyMask) | (value << KeyShift);

	public bool GetKey(KeyAction key) => (GetKey() & (1 << (int)key)) != 0;
	public void SetKey(KeyAction key, bool value) => SetKey(value switch {
		true  => GetKey() |  (1u << (int)key),
		false => GetKey() & ~(1u << (int)key),
	});

	public float GetMoveFactor() {
		return ((Data & MoveFMask) >> MoveFShift) * 0.333333f;
	}

	public void SetMoveFactor(float value) {
		uint moveFactor = (uint)math.round(math.saturate(value) * 3f);
		Data = (Data & ~MoveFMask) | (moveFactor << MoveFShift);
	}

	public float2 GetMoveDirection() {
		if (GetMoveFactor() == 0f) return float2.zero;
		else {
			float yawRadians = ((Data & MoveDMask) >> MoveDShift) * 5.625f * math.TORADIANS;
			return GetMoveFactor() * new float2(math.sin(yawRadians), math.cos(yawRadians));
		}
	}

	public void SetMoveDirection(float2 value) {
		SetMoveFactor(math.length(value));
		if (0f < GetMoveFactor()) {
			float yaw = (math.atan2(value.x, value.y) * math.TODEGREES + 362.8125f) % 360f;
			Data = (Data & ~MoveDMask) | ((uint)(yaw * 0.177777f) << MoveDShift);
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Player Head Ghost Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(GhostInputSystemGroup))]
partial struct PlayerHeadGhostSimulationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<CameraBridgeSystem.Singleton>();
		state.RequireForUpdate<InputBridgeSystem.Singleton>();
		state.RequireForUpdate<PlayerHeadData>();
	}

	public void OnUpdate(ref SystemState state) {
		var cameraBridge = SystemAPI.GetSingleton<CameraBridgeSystem.Singleton>();
		var inputBridge = SystemAPI.GetSingleton<InputBridgeSystem.Singleton>();

		state.Dependency = new PlayerHeadGhostSimulationJob() {
			CameraBridgeProperty = cameraBridge.Property[0],
			InputBridgeProperty  = inputBridge.Property[0],
		}.ScheduleParallel(state.Dependency);
	}
}

[BurstCompile, WithAll(typeof(Simulate))]
[WithAll(typeof(GhostOwnerIsLocal))]
partial struct PlayerHeadGhostSimulationJob : IJobEntity {
	[ReadOnly] public CameraBridge.Property CameraBridgeProperty;
	[ReadOnly] public InputBridge.Property InputBridgeProperty;

	public void Execute(
		in CharacterHeadBlob headBlob,
		ref PlayerHeadData headData) {

		headData.SetKey(InputBridgeProperty.KeyNext);
		float x = InputBridgeProperty.MoveDirection.x;
		float y = InputBridgeProperty.MoveDirection.y;
		var moveDirection = new float3(x, 0f, y);
		var eulerRotation = new float3(0f, CameraBridgeProperty.Yaw * math.TORADIANS, 0f);
		var rotatedMoveDirection = math.mul(quaternion.Euler(eulerRotation), moveDirection);
		headData.SetMoveDirection(new float2(rotatedMoveDirection.x, rotatedMoveDirection.z));
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Player Head Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSPredictedSimulationSystemGroup))]
partial struct PlayerHeadSimulationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<CharacterInput>();
		state.RequireForUpdate<PlayerHeadData>();
	}

	public void OnUpdate(ref SystemState state) {
		state.Dependency = new PlayerHeadSimulationJob() {
		}.ScheduleParallel(state.Dependency);
	}
}

[BurstCompile, WithAll(typeof(Simulate))]
[WithAll(typeof(GhostOwnerIsLocal))]
partial struct PlayerHeadSimulationJob : IJobEntity {

	public void Execute(
		ref CharacterInput input,
		in CharacterHeadBlob headBlob,
		ref PlayerHeadData headData) {

		input.Key = headData.GetKey();
		input.MoveDirection = headData.GetMoveDirection();
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Player Head Client Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(DOTSClientSimulationSystemGroup))]
public partial class PlayerHeadClientSimulationSystem : SystemBase {

	protected override void OnCreate() {
		RequireForUpdate<PhysicsWorldSingleton>();
		RequireForUpdate<GhostOwnerIsLocal>();
	}

	protected override void OnUpdate() {
		if (GameManager.GameState != GameState.Gameplay) return;
		var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
		foreach (var (coreBlob, transform) in
			SystemAPI.Query<
				RefRO<CharacterCoreBlob>,
				RefRO<LocalTransform>
			>().WithAll<GhostOwnerIsLocal>()) {

			float height = coreBlob.ValueRO.Value.Value.RoughHeight;
			float radius = coreBlob.ValueRO.Value.Value.RoughRadius;
			var position = transform.ValueRO.Position;
			if (physicsWorld.CastRay(new RaycastInput {
				Start        = position + new float3(0f, height, 0f),
				End          = position + new float3(0f, -5.00f, 0f),
				Filter       = new CollisionFilter {
				BelongsTo    = uint.MaxValue,
				CollidesWith = 1u << (int)PhysicsCategory.Terrain,
				}, }, out var hit)) {
				position = hit.Position + new float3(0f, height, 0f);
			}
			var delta = (position - (float3)CameraManager.Position) * new float3(10f, 2f, 10f);
			CameraManager.Position += (Vector3)delta * SystemAPI.Time.DeltaTime;
			CameraManager.Yaw += InputManager.LookDirection.x * InputManager.Sensitivity * 0.1f;
			break;
		}
	}
}

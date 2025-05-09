using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager Bridge Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/Camera Manager Bridge")]
public class CameraManagerBridgeAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(CameraManagerBridgeAuthoring))]
		class CameraManagerBridgeAuthoringEditor : EditorExtensions {
			CameraManagerBridgeAuthoring I => target as CameraManagerBridgeAuthoring;
			public override void OnInspectorGUI() {
				Begin("Camera Manager Bridge Authoring");
				End();
			}
		}
	#endif



	// Baker

	public class Baker : Baker<CameraManagerBridgeAuthoring> {
		public override void Bake(CameraManagerBridgeAuthoring authoring) {
			Entity entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new CameraManagerBridge());
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager Bridge
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CameraManagerBridge : IComponentData {

	// Fields

	float3      m_Position;
	quaternion  m_Rotation;
	constraints m_Constraints;
	float       m_FocusDistance;
	float       m_FieldOfView;
	float       m_OrthographicSize;
	float       m_Projection;

	uint m_Flag;



	// Properties

	public float3 Position {
		get => m_Position;
		set {
			m_Position = value;
			Flag |= 0x01u;
		}
	}
	public quaternion Rotation {
		get => m_Rotation;
		set {
			m_Rotation = value;
			Flag |= 0x02u;
		}
	}
	public float3 EulerRotation {
		get => math.Euler(Rotation) * math.TODEGREES;
		set => Rotation = quaternion.Euler(value * math.TORADIANS);
	}
	public float Yaw {
		get => EulerRotation.y;
		set => EulerRotation = new float3(EulerRotation.x, value, EulerRotation.z);
	}
	public float3 Right   => math.mul(Rotation, new float3(1f, 0f, 0f));
	public float3 Up      => math.mul(Rotation, new float3(0f, 1f, 0f));
	public float3 Forward => math.mul(Rotation, new float3(0f, 0f, 1f));

	public constraints Constraints {
		get => m_Constraints;
		set {
			m_Constraints = value;
			Flag |= 0x04u;
		}
	}
	public bool3 FreezePosition => Constraints.position;
	public bool3 FreezeRotation => Constraints.rotation;



	public float FocusDistance {
		get => m_FocusDistance;
		set  {
			m_FocusDistance = value;
			Flag |= 0x08u;
		}
	}
	public float FieldOfView {
		get => m_FieldOfView;
		set {
			m_FieldOfView = value;
			Flag |= 0x10u;
		}
	}
	public float OrthographicSize {
		get => m_OrthographicSize;
		set {
			m_OrthographicSize = value;
			Flag |= 0x20u;
		}
	}
	public float Projection {
		get => m_Projection;
		set {
			m_Projection = value;
			Flag |= 0x40u;
		}
	}



	public uint Flag {
		get => m_Flag;
		set => m_Flag = value;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class CameraBridgeSystem : SystemBase {

	[BurstCompile]
	protected override void OnCreate() {
		RequireForUpdate<CameraManagerBridge>();
	}

	[BurstDiscard]
	protected override void OnUpdate() {
		var bridge = SystemAPI.GetSingletonRW<CameraManagerBridge>();
		var flag   = bridge.ValueRO.Flag;

		if ((flag & 0x01u) != 0u) CameraManager.Position         = bridge.ValueRO.Position;
		if ((flag & 0x02u) != 0u) CameraManager.Rotation         = bridge.ValueRO.Rotation;
		if ((flag & 0x04u) != 0u) CameraManager.Constraints      = bridge.ValueRO.Constraints;
		if ((flag & 0x08u) != 0u) CameraManager.FocusDistance    = bridge.ValueRO.FocusDistance;
		if ((flag & 0x10u) != 0u) CameraManager.FieldOfView      = bridge.ValueRO.FieldOfView;
		if ((flag & 0x20u) != 0u) CameraManager.OrthographicSize = bridge.ValueRO.OrthographicSize;
		if ((flag & 0x40u) != 0u) CameraManager.Projection       = bridge.ValueRO.Projection;

		bridge.ValueRW.Position			= CameraManager.Position;
		bridge.ValueRW.Rotation			= CameraManager.Rotation;
		bridge.ValueRW.Constraints		= CameraManager.Constraints;
		bridge.ValueRW.FocusDistance	= CameraManager.FocusDistance;
		bridge.ValueRW.FieldOfView		= CameraManager.FieldOfView;
		bridge.ValueRW.OrthographicSize	= CameraManager.OrthographicSize;
		bridge.ValueRW.Projection		= CameraManager.Projection;

		if (flag != 0u) bridge.ValueRW.Flag = 0u;
	}
}

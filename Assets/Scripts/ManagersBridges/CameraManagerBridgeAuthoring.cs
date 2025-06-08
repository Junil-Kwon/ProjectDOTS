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
			AddComponent(entity, new CameraManagerBridge {

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager Bridge
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CameraManagerBridge : IComponentData {

	// Fields

	public float3      m_Position;
	public quaternion  m_Rotation;
	public constraints m_Constraints;
	public float       m_FocusDistance;
	public float       m_FieldOfView;
	public float       m_OrthographicSize;
	public float       m_Projection;

	public uint Flag;



	// Properties

	public float3 Position {
		get => m_Position;
		set {
			m_Position = value;
			Flag |= 0x0001u;
		}
	}
	public quaternion Rotation {
		get => m_Rotation;
		set {
			m_Rotation = value;
			Flag |= 0x0002u;
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
			Flag |= 0x0004u;
		}
	}
	public bool3 FreezePosition {
		get => Constraints.position;
		set => Constraints = new constraints(value, Constraints.rotation);
	}
	public bool3 FreezeRotation {
		get => Constraints.rotation;
		set => Constraints = new constraints(Constraints.position, value);
	}



	public float FocusDistance {
		get => m_FocusDistance;
		set {
			m_FocusDistance = math.clamp(value, 0f, 255f);
			Flag |= 0x0008u;
		}
	}
	public float FieldOfView {
		get => m_FieldOfView;
		set {
			m_FieldOfView = math.clamp(value, 1f, 179f);
			Flag |= 0x0010u;
		}
	}
	public float OrthographicSize {
		get => m_OrthographicSize;
		set {
			m_OrthographicSize = value;
			Flag |= 0x0020u;
		}
	}
	public float Projection {
		get => m_Projection;
		set {
			m_Projection = value;
			Flag |= 0x0040u;
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class CameraBridgeSystem : SystemBase {

	protected override void OnCreate() {
		RequireForUpdate<CameraManagerBridge>();
	}

	protected override void OnUpdate() {
		var bridge = SystemAPI.GetSingletonRW<CameraManagerBridge>();

		var i = bridge.ValueRO;
		if ((i.Flag & 0x0001u) != 0u) CameraManager.Position         = i.Position;
		if ((i.Flag & 0x0002u) != 0u) CameraManager.Rotation         = i.Rotation;
		if ((i.Flag & 0x0004u) != 0u) CameraManager.Constraints      = i.Constraints;
		if ((i.Flag & 0x0008u) != 0u) CameraManager.FocusDistance    = i.FocusDistance;
		if ((i.Flag & 0x0010u) != 0u) CameraManager.FieldOfView      = i.FieldOfView;
		if ((i.Flag & 0x0020u) != 0u) CameraManager.OrthographicSize = i.OrthographicSize;
		if ((i.Flag & 0x0040u) != 0u) CameraManager.Projection       = i.Projection;

		ref var o = ref bridge.ValueRW;
		o.Position         = CameraManager.Position;
		o.Rotation         = CameraManager.Rotation;
		o.Constraints      = CameraManager.Constraints;
		o.FocusDistance    = CameraManager.FocusDistance;
		o.FieldOfView      = CameraManager.FieldOfView;
		o.OrthographicSize = CameraManager.OrthographicSize;
		o.Projection       = CameraManager.Projection;

		o.Flag = 0u;
	}
}

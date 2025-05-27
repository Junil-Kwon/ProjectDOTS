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

	public float3      Position;
	public quaternion  Rotation;
	public constraints Constraints;
	public float       FocusDistance;
	public float       FieldOfView;
	public float       OrthographicSize;
	public float       Projection;
}



public static class CameraManagerBridgeExtensions {

	public static float3 GetEulerRotation(this in CameraManagerBridge bridge) {
		return math.Euler(bridge.Rotation) * math.TODEGREES;
	}
	public static void SetEulerRotation(this ref CameraManagerBridge bridge, float3 value) {
		bridge.Rotation = quaternion.Euler(value * math.TORADIANS);
	}

	public static float GetYaw(this in CameraManagerBridge bridge) {
		return bridge.GetEulerRotation().y;
	}
	public static void SetYaw(this ref CameraManagerBridge bridge, float value) {
		var eulerRotation = bridge.GetEulerRotation();
		eulerRotation.y = value;
		bridge.SetEulerRotation(eulerRotation);
	}

	public static float3 Right(this in CameraManagerBridge bridge) {
		return math.mul(bridge.Rotation, new float3(1f, 0f, 0f));
	}
	public static float3 Up(this in CameraManagerBridge bridge) {
		return math.mul(bridge.Rotation, new float3(0f, 1f, 0f));
	}
	public static float3 Forward(this in CameraManagerBridge bridge) {
		return math.mul(bridge.Rotation, new float3(0f, 0f, 1f));
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class CameraBridgeSystem : SystemBase {

	bool initialized = false;
	CameraManagerBridge prev;

	protected override void OnCreate() {
		RequireForUpdate<CameraManagerBridge>();
	}

	protected override void OnUpdate() {
		var bridge = SystemAPI.GetSingletonRW<CameraManagerBridge>();
		if (initialized == false) {
			initialized = true;
			prev.Position         = CameraManager.Position;
			prev.Rotation         = CameraManager.Rotation;
			prev.Constraints      = CameraManager.Constraints;
			prev.FocusDistance    = CameraManager.FocusDistance;
			prev.FieldOfView      = CameraManager.FieldOfView;
			prev.OrthographicSize = CameraManager.OrthographicSize;
			prev.Projection       = CameraManager.Projection;
		}
		var next = bridge.ValueRO;

		var position = false;
		position |= math.EPSILON < math.abs(prev.Position.x - next.Position.x);
		position |= math.EPSILON < math.abs(prev.Position.y - next.Position.y);
		position |= math.EPSILON < math.abs(prev.Position.z - next.Position.z);
		if (position) CameraManager.Position = next.Position;
		else          next.Position = CameraManager.Position;

		var rotation = false;
		rotation |= math.EPSILON < math.abs(prev.Rotation.value.x - next.Rotation.value.x);
		rotation |= math.EPSILON < math.abs(prev.Rotation.value.y - next.Rotation.value.y);
		rotation |= math.EPSILON < math.abs(prev.Rotation.value.z - next.Rotation.value.z);
		rotation |= math.EPSILON < math.abs(prev.Rotation.value.w - next.Rotation.value.w);
		if (rotation) CameraManager.Rotation = next.Rotation;
		else          next.Rotation = CameraManager.Rotation;

		var constraints = false;
		constraints |= prev.Constraints.positionX != next.Constraints.positionX;
		constraints |= prev.Constraints.positionY != next.Constraints.positionY;
		constraints |= prev.Constraints.positionZ != next.Constraints.positionZ;
		constraints |= prev.Constraints.rotationX != next.Constraints.rotationX;
		constraints |= prev.Constraints.rotationY != next.Constraints.rotationY;
		constraints |= prev.Constraints.rotationZ != next.Constraints.rotationZ;
		if (constraints) CameraManager.Constraints = next.Constraints;
		else             next.Constraints = CameraManager.Constraints;

		var focusDistance = false;
		focusDistance |= math.EPSILON < math.abs(prev.FocusDistance - next.FocusDistance);
		if (focusDistance) CameraManager.FocusDistance = next.FocusDistance;
		else               next.FocusDistance = CameraManager.FocusDistance;

		var fieldOfView = false;
		fieldOfView |= math.EPSILON < math.abs(prev.FieldOfView - next.FieldOfView);
		if (fieldOfView) CameraManager.FieldOfView = next.FieldOfView;
		else             next.FieldOfView = CameraManager.FieldOfView;

		var orthographicSize = false;
		orthographicSize |= math.EPSILON < math.abs(prev.OrthographicSize - next.OrthographicSize);
		if (orthographicSize) CameraManager.OrthographicSize = next.OrthographicSize;
		else                  next.OrthographicSize = CameraManager.OrthographicSize;

		var projection = false;
		projection |= math.EPSILON < math.abs(prev.Projection - next.Projection);
		if (projection) CameraManager.Projection = next.Projection;
		else            next.Projection = CameraManager.Projection;

		bridge.ValueRW = prev = next;
	}
}

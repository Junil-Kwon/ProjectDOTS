using System.Runtime.InteropServices;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Physics;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Bridge
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public static class CameraBridge {

	// Constantas

	public struct Property {
		public float3 Position;
		public quaternion Rotation;

		public float DollyDistance;
		public float FieldOfView;
		public float OrthographicSize;
		public float Projection;
		public int CullingMask;
		public constraints Constraints;

		public float3 EulerRotation => math.Euler(Rotation) * math.TODEGREES;
		public float Yaw => EulerRotation.y;
		public float3 Right   => math.mul(Rotation, new float3(1f, 0f, 0f));
		public float3 Up      => math.mul(Rotation, new float3(0f, 1f, 0f));
		public float3 Forward => math.mul(Rotation, new float3(0f, 0f, 1f));

		public bool3 FreezePosition => Constraints.position;
		public bool3 FreezeRotation => Constraints.rotation;
	}

	public enum Method : byte {
		ShakeCamera,
		StopShaking,
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct ShakeCamera {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public float Strength;
		[FieldOffset(13)] public float Duration;
		[FieldOffset(17)] public bool2 Direction;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct StopShaking {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
	}
}



public static class CameraBridgeExtensions {

	// Methods

	public static void ShakeCamera(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, float strength, float duration, bool direction = default) {
		var i = new CameraBridge.ShakeCamera {
			Method = CameraBridge.Method.ShakeCamera,
			Entity = entity, Strength = strength, Duration = duration, Direction = direction,
		};
		method.Enqueue(i.Data);
	}

	public static void StopShaking(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity) {
		var i = new CameraBridge.StopShaking {
			Method = CameraBridge.Method.StopShaking,
			Entity = entity,
		};
		method.Enqueue(i.Data);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class CameraBridgeSystem : SystemBase {
	bool Initialized;
	NativeArray<CameraBridge.Property> Property;
	NativeQueue<FixedBytes62> Method;
	NativeHashMap<Entity, FixedBytes16> Result;

	public struct Singleton : IComponentData {
		public NativeArray<CameraBridge.Property>.ReadOnly Property;
		public NativeQueue<FixedBytes62>.ParallelWriter Method;
		public NativeHashMap<Entity, FixedBytes16>.ReadOnly Result;
	}

	protected override void OnCreate() {
		RequireForUpdate<PhysicsStep>();
	}

	protected override void OnUpdate() {
		if (Initialized != true) {
			if (SystemAPI.TryGetSingletonEntity<PhysicsStep>(out var entity)) {
				EntityManager.AddComponentData(entity, new Singleton {
					Property = (Property = new(1, Allocator.Persistent)).AsReadOnly(),
					Method = (Method = new(Allocator.Persistent)).AsParallelWriter(),
					Result = (Result = new(64, Allocator.Persistent)).AsReadOnly(),
				});
				Initialized = true;
			} else return;
		}
		EntityManager.CompleteAllTrackedJobs();
		Property[0] = new CameraBridge.Property {
			Position         = CameraManager.Position,
			Rotation         = CameraManager.Rotation,
			DollyDistance    = CameraManager.DollyDistance,
			FieldOfView      = CameraManager.FieldOfView,
			OrthographicSize = CameraManager.OrthographicSize,
			Projection       = CameraManager.Projection,
			CullingMask      = CameraManager.CullingMask,
			Constraints      = CameraManager.Constraints,
		};
		Result.Clear();
		while (Method.TryDequeue(out var data)) {
			switch ((CameraBridge.Method)data.offset0000.byte0000) {
				case CameraBridge.Method.ShakeCamera: {
					var i = new CameraBridge.ShakeCamera { Data = data };
					CameraManager.ShakeCamera(i.Strength, i.Duration, i.Direction);
				} break;
				case CameraBridge.Method.StopShaking: {
					var i = new CameraBridge.StopShaking { Data = data };
					CameraManager.StopShaking();
				} break;
			}
		}
	}

	protected override void OnDestroy() {
		Property.Dispose();
		Method.Dispose();
		Result.Dispose();
	}
}

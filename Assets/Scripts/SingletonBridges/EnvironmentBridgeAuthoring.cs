using System.Runtime.InteropServices;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Environment Bridge
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public static class EnvironmentBridge {

	// Constants

	public struct Property {
		public quaternion Rotation;
		public LightMode LightMode;
		public float TimeOfDay;
		public float DayLength;
	}

	public enum Method : byte {
		SetTimeOfDay,
		AddLight,
		SetLightColor,
		SetLightIntensity,
		SetLightPosition,
		SetLightDuration,
		SetLightRange,
		RemoveLight,
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct SetTimeOfDay {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public float TimeOfDay;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct SetSimulateTime {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public bool SimulateTime;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct AddLight {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public color Color;
		[FieldOffset(13)] public float Intensity;
		[FieldOffset(17)] public float3 Position;
		[FieldOffset(29)] public float Duration;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct AddLightResult {
		[FieldOffset(00)] public FixedBytes16 Data;
		[FieldOffset(00)] public uint LightID;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct SetLightColor {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public uint LightID;
		[FieldOffset(13)] public color Color;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct SetLightIntensity {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public uint LightID;
		[FieldOffset(13)] public float Intensity;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct SetLightPosition {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public uint LightID;
		[FieldOffset(13)] public float3 Position;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct SetLightDuration {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public uint LightID;
		[FieldOffset(13)] public float Duration;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct SetLightRange {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public uint LightID;
		[FieldOffset(13)] public float Range;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct RemoveLight {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public uint LightID;
	}
}



public static class EnvironmentBridgeExtensions {

	// Methods

	public static void SetTimeOfDay(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, float timeOfDay) {
		var i = new EnvironmentBridge.SetTimeOfDay {
			Method = EnvironmentBridge.Method.SetTimeOfDay,
			Entity = entity, TimeOfDay = timeOfDay,
		};
		method.Enqueue(i.Data);
	}

	public static void AddLight(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, color color, float intensity, float3 position,
		float duration = 1f) {
		var i = new EnvironmentBridge.AddLight {
			Method = EnvironmentBridge.Method.AddLight,
			Entity = entity, Color = color, Intensity = intensity, Position = position,
			Duration = duration,
		};
		method.Enqueue(i.Data);
	}

	public static bool TryGetAddLightResult(
		this NativeHashMap<Entity, FixedBytes16>.ReadOnly result,
		Entity entity, out uint lightID) {
		if (result.TryGetValue(entity, out var data)) {
			var o = new EnvironmentBridge.AddLightResult { Data = data };
			lightID = o.LightID;
			return true;
		}
		lightID = default;
		return false;
	}

	public static void SetLightPosition(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, float3 position) {
		var i = new EnvironmentBridge.SetLightPosition {
			Method = EnvironmentBridge.Method.SetLightPosition,
			Entity = entity, Position = position,
		};
		method.Enqueue(i.Data);
	}

	public static void SetLightIntensity(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, float intensity) {
		var i = new EnvironmentBridge.SetLightIntensity {
			Method = EnvironmentBridge.Method.SetLightIntensity,
			Entity = entity, Intensity = intensity,
		};
		method.Enqueue(i.Data);
	}

	public static void SetLightColor(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, color color) {
		var i = new EnvironmentBridge.SetLightColor {
			Method = EnvironmentBridge.Method.SetLightColor,
			Entity = entity, Color = color,
		};
		method.Enqueue(i.Data);
	}

	public static void SetLightDuration(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, float duration) {
		var i = new EnvironmentBridge.SetLightDuration {
			Method = EnvironmentBridge.Method.SetLightDuration,
			Entity = entity, Duration = duration,
		};
		method.Enqueue(i.Data);
	}

	public static void SetLightRange(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, float range) {
		var i = new EnvironmentBridge.SetLightRange {
			Method = EnvironmentBridge.Method.SetLightRange,
			Entity = entity, Range = range,
		};
		method.Enqueue(i.Data);
	}

	public static void RemoveLight(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, uint lightID) {
		var i = new EnvironmentBridge.RemoveLight {
			Method = EnvironmentBridge.Method.RemoveLight,
			Entity = entity, LightID = lightID,
		};
		method.Enqueue(i.Data);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Environment Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class EnvironmentBridgeSystem : SystemBase {
	NativeArray<EnvironmentBridge.Property> Property;
	NativeQueue<FixedBytes62> Method;
	NativeHashMap<Entity, FixedBytes16> Result;

	public struct Singleton : IComponentData {
		public NativeArray<EnvironmentBridge.Property>.ReadOnly Property;
		public NativeQueue<FixedBytes62>.ParallelWriter Method;
		public NativeHashMap<Entity, FixedBytes16>.ReadOnly Result;
	}

	protected override void OnCreate() {
		EntityManager.CreateEntity(ComponentType.ReadOnly<Singleton>());
		SystemAPI.SetSingleton(new Singleton {
			Property = (Property = new(1, Allocator.Persistent)).AsReadOnly(),
			Method = (Method = new(Allocator.Persistent)).AsParallelWriter(),
			Result = (Result = new(64, Allocator.Persistent)).AsReadOnly(),
		});
	}

	protected override void OnUpdate() {
		Dependency.Complete();
		Property[0] = new EnvironmentBridge.Property {
			Rotation  = EnvironmentManager.Rotation,
			LightMode = EnvironmentManager.LightMode,
			TimeOfDay = EnvironmentManager.TimeOfDay,
			DayLength = EnvironmentManager.DayLength,
		};
		Result.Clear();
		while (Method.TryDequeue(out var data)) {
			switch ((EnvironmentBridge.Method)data.offset0000.byte0000) {
				case EnvironmentBridge.Method.SetTimeOfDay: {
					var i = new EnvironmentBridge.SetTimeOfDay { Data = data };
					EnvironmentManager.TimeOfDay = i.TimeOfDay;
				} break;
				case EnvironmentBridge.Method.AddLight: {
					var i = new EnvironmentBridge.AddLight { Data = data };
					uint lightID = EnvironmentManager.AddLight(
						i.Color, i.Intensity, i.Position, i.Duration);
					var o = new EnvironmentBridge.AddLightResult { LightID = lightID };
					Result.TryAdd(i.Entity, o.Data);
				} break;
				case EnvironmentBridge.Method.SetLightColor: {
					var i = new EnvironmentBridge.SetLightColor { Data = data };
					EnvironmentManager.SetLightColor(i.LightID, i.Color);
				} break;
				case EnvironmentBridge.Method.SetLightIntensity: {
					var i = new EnvironmentBridge.SetLightIntensity { Data = data };
					EnvironmentManager.SetLightIntensity(i.LightID, i.Intensity);
				} break;
				case EnvironmentBridge.Method.SetLightPosition: {
					var i = new EnvironmentBridge.SetLightPosition { Data = data };
					EnvironmentManager.SetLightPosition(i.LightID, i.Position);
				} break;
				case EnvironmentBridge.Method.SetLightDuration: {
					var i = new EnvironmentBridge.SetLightDuration { Data = data };
					EnvironmentManager.SetLightDuration(i.LightID, i.Duration);
				} break;
				case EnvironmentBridge.Method.SetLightRange: {
					var i = new EnvironmentBridge.SetLightRange { Data = data };
					EnvironmentManager.SetLightRange(i.LightID, i.Range);
				} break;
				case EnvironmentBridge.Method.RemoveLight: {
					var i = new EnvironmentBridge.RemoveLight { Data = data };
					EnvironmentManager.RemoveLight(i.LightID);
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

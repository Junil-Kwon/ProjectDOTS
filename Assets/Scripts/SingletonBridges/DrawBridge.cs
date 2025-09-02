using System.Runtime.InteropServices;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Physics;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Draw Bridge
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public static class DrawBridge {

	// Constants

	public struct Property {
		public byte Data;
	}

	public enum Method : byte {
		Temp,
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct Temp {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
	}
}

public static class DrawBridgeExtensions {

	// Methods

	public static void Temp(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity) {
		var i = new DrawBridge.Temp {
			Method = DrawBridge.Method.Temp,
			Entity = entity,
		};
		method.Enqueue(i.Data);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Draw Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class DrawBridgeSystem : SystemBase {
	bool Initialized;
	NativeArray<DrawBridge.Property> Property;
	NativeQueue<FixedBytes62> Method;
	NativeHashMap<Entity, FixedBytes16> Result;

	public struct Singleton : IComponentData {
		public NativeArray<DrawBridge.Property>.ReadOnly Property;
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
		Property[0] = new DrawBridge.Property {
		};
		Result.Clear();
		while (Method.TryDequeue(out var data)) {
			switch ((DrawBridge.Method)data.offset0000.byte0000) {
				case DrawBridge.Method.Temp: {
					var i = new DrawBridge.Temp { Data = data };
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

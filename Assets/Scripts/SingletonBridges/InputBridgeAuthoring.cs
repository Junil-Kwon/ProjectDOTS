using System.Runtime.InteropServices;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Input Bridge
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public static class InputBridge {

	// Constants

	public struct Property {
		public uint KeyPrev;
		public uint KeyNext;
		public float2 LookDirection;
		public float2 MoveDirection;
		public float2 PointPosition;
		public float2 ScrollWheel;
		public float2 Navigate;

		bool GetKeyNext(KeyAction key) => (KeyNext & (1u << (int)key)) != 0u;
		bool GetKeyPrev(KeyAction key) => (KeyPrev & (1u << (int)key)) != 0u;

		public bool GetKey(KeyAction key) => GetKeyNext(key);
		public bool GetKeyDown(KeyAction key) => GetKeyNext(key) && !GetKeyPrev(key);
		public bool GetKeyUp(KeyAction key) => !GetKeyNext(key) && GetKeyPrev(key);
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



public static class InputBridgeExtensions {

	// Methods

	public static void Temp(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity) {
		var i = new InputBridge.Temp {
			Method = InputBridge.Method.Temp,
			Entity = entity,
		};
		method.Enqueue(i.Data);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Input Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class InputBridgeSystem : SystemBase {
	NativeArray<InputBridge.Property> Property;
	NativeQueue<FixedBytes62> Method;
	NativeHashMap<Entity, FixedBytes16> Result;

	public struct Singleton : IComponentData {
		public NativeArray<InputBridge.Property>.ReadOnly Property;
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
		Property[0] = new InputBridge.Property {
			KeyPrev       = InputManager.KeyPrev,
			KeyNext       = InputManager.KeyNext,
			LookDirection = InputManager.LookDirection,
			MoveDirection = InputManager.MoveDirection,
			PointPosition = InputManager.PointPosition,
			ScrollWheel   = InputManager.ScrollWheel,
			Navigate      = InputManager.Navigate,
		};
		Result.Clear();
		while (Method.TryDequeue(out var data)) {
			switch ((InputBridge.Method)data.offset0000.byte0000) {
				case InputBridge.Method.Temp: {
					var i = new InputBridge.Temp { Data = data };
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

using System.Runtime.InteropServices;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Physics;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game Bridge
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public static class GameBridge {

	// Constants

	public struct Property {
		public GameState GameState;
		public float TimeScale;
	}

	public enum Method : byte {
		SetGameState,
		SetTimeScale,
		PlayEvent,
		IsEventPlaying,
		IsEventPlayingResult,
		StopEvent,
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct SetGameState {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public GameState GameState;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct SetTimeScale {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public float TimeScale;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct PlayEvent {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public UnityObjectRef<EventGraph> EventGraph;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct PlayEventResult {
		[FieldOffset(00)] public FixedBytes16 Data;
		[FieldOffset(00)] public uint EventID;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct IsEventPlaying {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public uint EventID;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct IsEventPlayingResult {
		[FieldOffset(00)] public FixedBytes16 Data;
		[FieldOffset(00)] public bool IsPlaying;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct StopEvent {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public uint EventID;
	}
}



public static class GameBridgeExtensions {

	// Methods

	public static void SetGameState(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, GameState gameState) {
		var i = new GameBridge.SetGameState {
			Method = GameBridge.Method.SetGameState,
			Entity = entity, GameState = gameState,
		};
		method.Enqueue(i.Data);
	}

	public static void SetTimeScale(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, float timeScale) {
		var i = new GameBridge.SetTimeScale {
			Method = GameBridge.Method.SetTimeScale,
			Entity = entity, TimeScale = timeScale,
		};
		method.Enqueue(i.Data);
	}

	public static void PlayEvent(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, UnityObjectRef<EventGraph> eventGraph) {
		var i = new GameBridge.PlayEvent {
			Method = GameBridge.Method.PlayEvent,
			Entity = entity, EventGraph = eventGraph,
		};
		method.Enqueue(i.Data);
	}

	public static bool TryGetPlayEventResult(
		this NativeHashMap<Entity, FixedBytes16>.ReadOnly result,
		Entity entity, out uint eventID) {
		if (result.TryGetValue(entity, out var data)) {
			var o = new GameBridge.PlayEventResult { Data = data };
			eventID = o.EventID;
			return true;
		}
		eventID = default;
		return false;
	}

	public static void IsEventPlaying(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, uint eventID = default) {
		var i = new GameBridge.IsEventPlaying {
			Method = GameBridge.Method.IsEventPlaying,
			Entity = entity, EventID = eventID,
		};
		method.Enqueue(i.Data);
	}

	public static bool TryGetIsEventPlayingResult(
		this NativeHashMap<Entity, FixedBytes16>.ReadOnly result,
		Entity entity, out bool isPlaying) {
		if (result.TryGetValue(entity, out var data)) {
			var o = new GameBridge.IsEventPlayingResult { Data = data };
			isPlaying = o.IsPlaying;
			return true;
		}
		isPlaying = default;
		return false;
	}

	public static void StopEvent(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, uint eventID) {
		var i = new GameBridge.StopEvent {
			Method = GameBridge.Method.StopEvent,
			Entity = entity,
			EventID = eventID,
		};
		method.Enqueue(i.Data);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class GameBridgeSystem : SystemBase {
	bool Initialized;
	NativeArray<GameBridge.Property> Property;
	NativeQueue<FixedBytes62> Method;
	NativeHashMap<Entity, FixedBytes16> Result;

	public struct Singleton : IComponentData {
		public NativeArray<GameBridge.Property>.ReadOnly Property;
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
		Property[0] = new GameBridge.Property {
			GameState = GameManager.GameState,
			TimeScale = GameManager.TimeScale,
		};
		Result.Clear();
		while (Method.TryDequeue(out var data)) {
			switch ((GameBridge.Method)data.offset0000.byte0000) {
				case GameBridge.Method.SetGameState: {
					var i = new GameBridge.SetGameState { Data = data };
					GameManager.GameState = i.GameState;
				} break;
				case GameBridge.Method.SetTimeScale: {
					var i = new GameBridge.SetTimeScale { Data = data };
					GameManager.TimeScale = i.TimeScale;
				} break;
				case GameBridge.Method.PlayEvent: {
					var i = new GameBridge.PlayEvent { Data = data };
					uint eventID = GameManager.PlayEvent(i.EventGraph);
					var o = new GameBridge.PlayEventResult { EventID = eventID };
					Result.TryAdd(i.Entity, o.Data);
				} break;
				case GameBridge.Method.IsEventPlaying: {
					var i = new GameBridge.IsEventPlaying { Data = data };
					bool isPlaying = GameManager.IsEventPlaying(i.EventID);
					var o = new GameBridge.IsEventPlayingResult { IsPlaying = isPlaying };
					Result.TryAdd(i.Entity, o.Data);
				} break;
				case GameBridge.Method.StopEvent: {
					var i = new GameBridge.StopEvent { Data = data };
					GameManager.StopEvent(i.EventID);
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

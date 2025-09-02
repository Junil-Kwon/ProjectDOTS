using System.Runtime.InteropServices;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Physics;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Audio Bridge
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public static class AudioBridge {

	// Constants

	public struct Property {
		public byte Data;
	}

	public enum Method : byte {
		PlayMusic,
		PlaySoundFX,
		PlayPointSoundFX,
		PlayBlendSoundFX,
		SetAudioPosition,
		SetAudioVolume,
		StopAudio,
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct PlayMusic {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public Audio Audio;
		[FieldOffset(13)] public float Volume;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct PlayMusicResult {
		[FieldOffset(00)] public FixedBytes16 Data;
		[FieldOffset(00)] public uint AudioID;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct PlaySoundFX {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public Audio Audio;
		[FieldOffset(13)] public float Volume;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct PlaySoundFXResult {
		[FieldOffset(00)] public FixedBytes16 Data;
		[FieldOffset(00)] public uint AudioID;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct PlayPointSoundFX {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public Audio Audio;
		[FieldOffset(13)] public float3 Position;
		[FieldOffset(25)] public float Volume;
		[FieldOffset(29)] public float Spread;
		[FieldOffset(33)] public float MinDistance;
		[FieldOffset(37)] public float MaxDistance;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct PlayPointSoundFXResult {
		[FieldOffset(00)] public FixedBytes16 Data;
		[FieldOffset(00)] public uint AudioID;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct PlayBlendSoundFX {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public Audio Audio;
		[FieldOffset(13)] public float3 Position;
		[FieldOffset(25)] public float Volume;
		[FieldOffset(29)] public float SpatialBlend;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct PlayBlendSoundFXResult {
		[FieldOffset(00)] public FixedBytes16 Data;
		[FieldOffset(00)] public uint AudioID;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct SetAudioPosition {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public uint AudioID;
		[FieldOffset(13)] public float3 Position;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct SetAudioVolume {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public uint AudioID;
		[FieldOffset(13)] public float Volume;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct StopAudio {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public uint AudioID;
	}
}



public static class AudioBridgeExtensions {

	// Methods

	public static void PlayMusic(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, Audio audio, float volume = 1f) {
		var i = new AudioBridge.PlayMusic {
			Method = AudioBridge.Method.PlayMusic,
			Entity = entity, Audio = audio, Volume = volume,
		};
		method.Enqueue(i.Data);
	}

	public static bool TryGetPlayMusicResult(
		this NativeHashMap<Entity, FixedBytes16> result,
		Entity entity, out uint audioID) {
		if (result.TryGetValue(entity, out var data)) {
			var o = new AudioBridge.PlayMusicResult { Data = data };
			audioID = o.AudioID;
			return true;
		}
		audioID = default;
		return false;
	}

	public static void PlaySoundFX(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, Audio audio, float volume = 1f) {
		var i = new AudioBridge.PlaySoundFX {
			Method = AudioBridge.Method.PlaySoundFX,
			Entity = entity, Audio = audio, Volume = volume,
		};
		method.Enqueue(i.Data);
	}

	public static bool TryGetPlaySoundFXResult(
		this NativeHashMap<Entity, FixedBytes16> result,
		Entity entity, out uint audioID) {
		if (result.TryGetValue(entity, out var data)) {
			var o = new AudioBridge.PlaySoundFXResult { Data = data };
			audioID = o.AudioID;
			return true;
		}
		audioID = default;
		return false;
	}

	public static void PlayPointSoundFX(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, Audio audio, float3 position, float volume = 1f,
		float spread = 0f, float minDistance = default, float maxDistance = default) {
		var i = new AudioBridge.PlayPointSoundFX {
			Method = AudioBridge.Method.PlayPointSoundFX,
			Entity = entity, Audio = audio, Position = position, Volume = volume,
			Spread = spread, MinDistance = minDistance, MaxDistance = maxDistance,
		};
		method.Enqueue(i.Data);
	}

	public static bool TryGetPlayPointSoundFXResult(
		this NativeHashMap<Entity, FixedBytes16> result,
		Entity entity, out uint audioID) {
		if (result.TryGetValue(entity, out var data)) {
			var o = new AudioBridge.PlayPointSoundFXResult { Data = data };
			audioID = o.AudioID;
			return true;
		}
		audioID = default;
		return false;
	}

	public static void PlayBlendSoundFX(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, Audio audio, float3 position, float volume = 1f,
		float spatialBlend = 0.5f) {
		var i = new AudioBridge.PlayBlendSoundFX {
			Method = AudioBridge.Method.PlayBlendSoundFX,
			Entity = entity, Audio = audio, Position = position, Volume = volume,
			SpatialBlend = spatialBlend,
		};
		method.Enqueue(i.Data);
	}

	public static bool TryGetPlayBlendSoundFXResult(
		this NativeHashMap<Entity, FixedBytes16> result,
		Entity entity, out uint audioID) {
		if (result.TryGetValue(entity, out var data)) {
			var o = new AudioBridge.PlayBlendSoundFXResult { Data = data };
			audioID = o.AudioID;
			return true;
		}
		audioID = default;
		return false;
	}

	public static void SetAudioPosition(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, uint audioID, float3 position) {
		var i = new AudioBridge.SetAudioPosition {
			Method = AudioBridge.Method.SetAudioPosition,
			Entity = entity, AudioID = audioID, Position = position,
		};
		method.Enqueue(i.Data);
	}

	public static void SetAudioVolume(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, uint audioID, float volume) {
		var i = new AudioBridge.SetAudioVolume {
			Method = AudioBridge.Method.SetAudioVolume,
			Entity = entity, AudioID = audioID, Volume = volume,
		};
		method.Enqueue(i.Data);
	}

	public static void StopAudio(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, uint audioID) {
		var i = new AudioBridge.StopAudio {
			Method = AudioBridge.Method.StopAudio,
			Entity = entity, AudioID = audioID,
		};
		method.Enqueue(i.Data);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Audio Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class AudioBridgeSystem : SystemBase {
	bool Initialized;
	NativeArray<AudioBridge.Property> Property;
	NativeQueue<FixedBytes62> Method;
	NativeHashMap<Entity, FixedBytes16> Result;

	public struct Singleton : IComponentData {
		public NativeArray<AudioBridge.Property>.ReadOnly Property;
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
		Property[0] = new AudioBridge.Property {
		};
		Result.Clear();
		while (Method.TryDequeue(out var data)) {
			switch ((AudioBridge.Method)data.offset0000.byte0000) {
				case AudioBridge.Method.PlayMusic: {
					var i = new AudioBridge.PlayMusic { Data = data };
					uint audioID = AudioManager.PlayMusic(i.Audio, i.Volume);
					var o = new AudioBridge.PlayMusicResult { AudioID = audioID };
					Result.TryAdd(i.Entity, o.Data);
				} break;
				case AudioBridge.Method.PlaySoundFX: {
					var i = new AudioBridge.PlaySoundFX { Data = data };
					uint audioID = AudioManager.PlaySoundFX(i.Audio, i.Volume);
					var o = new AudioBridge.PlaySoundFXResult { AudioID = audioID };
					Result.TryAdd(i.Entity, o.Data);
				} break;
				case AudioBridge.Method.PlayPointSoundFX: {
					var i = new AudioBridge.PlayPointSoundFX { Data = data };
					uint audioID = AudioManager.PlayPointSoundFX(
						i.Audio, i.Position, i.Volume, i.Spread, i.MinDistance, i.MaxDistance);
					var o = new AudioBridge.PlayPointSoundFXResult { AudioID = audioID };
					Result.TryAdd(i.Entity, o.Data);
				} break;
				case AudioBridge.Method.PlayBlendSoundFX: {
					var i = new AudioBridge.PlayBlendSoundFX { Data = data };
					uint audioID = AudioManager.PlayBlendSoundFX(
						i.Audio, i.Position, i.Volume, i.SpatialBlend);
					var o = new AudioBridge.PlayBlendSoundFXResult { AudioID = audioID };
					Result.TryAdd(i.Entity, o.Data);
				} break;
				case AudioBridge.Method.SetAudioPosition: {
					var i = new AudioBridge.SetAudioPosition { Data = data };
					AudioManager.SetAudioPosition(i.AudioID, i.Position);
				} break;
				case AudioBridge.Method.SetAudioVolume: {
					var i = new AudioBridge.SetAudioVolume { Data = data };
					AudioManager.SetAudioVolume(i.AudioID, i.Volume);
				} break;
				case AudioBridge.Method.StopAudio: {
					var i = new AudioBridge.StopAudio { Data = data };
					AudioManager.StopAudio(i.AudioID);
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

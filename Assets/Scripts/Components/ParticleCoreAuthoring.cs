using UnityEngine;
using System;
using System.Text.RegularExpressions;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Physics;
using Unity.Burst;
using Random = Unity.Mathematics.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif



public enum Pattern {
	RelativeYaw,
	FlipRandom,
	HasGravity,
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Particle Core Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Particle Core")]
public sealed class ParticleCoreAuthoring : MonoComponent<ParticleCoreAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(ParticleCoreAuthoring))]
	class ParticleCoreAuthoringEditor : EditorExtensions {
		ParticleCoreAuthoring I => target as ParticleCoreAuthoring;
		public override void OnInspectorGUI() {
			Begin();

			if (Enum.TryParse(I.name, out Particle particle)) {
				BeginDisabledGroup();
				EnumField("Particle", particle);
				EndDisabledGroup();
				Space();
			}

			BeginDisabledGroup(I.IsPrefabConnected);
			I.Duration = FloatField("Duration", I.Duration);
			I.Velocity = Vector3Field("Velocity", I.Velocity);
			for (int i = 0; i < PatternCount; i++) {
				BeginHorizontal();
				var pattern = (Pattern)i;
				var text = pattern.ToString();
				var name = Regex.Replace(text, @"(?<!^)(?=[A-Z])", " ");
				PrefixLabel(name);
				bool match = I.GetPattern(pattern);
				bool value = EditorGUILayout.Toggle(match, GUILayout.Width(14));
				I.SetPattern(pattern, value);
				BeginDisabledGroup(!value);
				switch (pattern) {
					case global::Pattern.HasGravity: {
						I.Gravity = Vector3Field(I.Gravity);
					} break;
				}
				EndDisabledGroup();
				EndHorizontal();
			}
			EndDisabledGroup();

			End();
		}
	}
	#endif



	// Constants

	static readonly int PatternCount = Enum.GetValues(typeof(Pattern)).Length;



	// Fields

	[SerializeField] float m_Duration = 1f;
	[SerializeField] Vector3 m_Velocity;
	[SerializeField] uint m_Pattern;
	[SerializeField] Vector3 m_Gravity = new(0f, -9.81f, 0f);



	// Properties

	float Duration {
		get => m_Duration;
		set => m_Duration = value;
	}
	Vector3 Velocity {
		get => m_Velocity;
		set => m_Velocity = value;
	}
	uint Pattern {
		get => m_Pattern;
		set => m_Pattern = value;
	}
	Vector3 Gravity {
		get => m_Gravity;
		set => m_Gravity = value;
	}



	// Methods

	bool GetPattern(Pattern pattern) => (Pattern & (1u << (int)pattern)) != 0u;
	void SetPattern(Pattern pattern, bool value) => Pattern = value switch {
		true  => Pattern |  (1u << (int)pattern),
		false => Pattern & ~(1u << (int)pattern),
	};



	// Baker

	class Baker : Baker<ParticleCoreAuthoring> {
		public override void Bake(ParticleCoreAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.Dynamic);
			AddComponent(entity, new ParticleInitialize());
			AddComponent(entity, new ParticleCoreBlob {
				Value = this.AddBlobAsset(new ParticleBlobData {

					Duration = authoring.Duration,
					Pattern  = authoring.Pattern,
					Gravity  = authoring.Gravity,

				})
			});
			AddComponent(entity, new ParticleCoreData {

				Velocity = authoring.Velocity,

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Particle Initialize
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct ParticleInitialize : IComponentData, IEnableableComponent { }




// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Particle Core Blob
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct ParticleCoreBlob : IComponentData {

	// Fields

	public BlobAssetReference<ParticleBlobData> Value;
}



public struct ParticleBlobData {

	// Fields

	public float Duration;
	public uint Pattern;
	public float3 Gravity;



	// Methods

	public bool GetPattern(Pattern pattern) => (Pattern & (1u << (int)pattern)) != 0u;
	public void SetPattern(Pattern pattern, bool value) => Pattern = value switch {
		true  => Pattern |  (1u << (int)pattern),
		false => Pattern & ~(1u << (int)pattern),
	};
}



public static class ParticleBlobExtensions {

	// Methods

	public static bool GetPattern(
		this ParticleCoreBlob particleBlob, Pattern pattern) {
		return particleBlob.Value.Value.GetPattern(pattern);
	}
	public static void SetPattern(
		this ParticleCoreBlob particleBlob, Pattern pattern, bool value) {
		particleBlob.Value.Value.SetPattern(pattern, value);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Particle Core Data
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct ParticleCoreData : IComponentData {

	// Fields

	public Random Random;
	public float Time;
	public float3 Velocity;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Particle Server Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSServerSimulationSystemGroup))]
partial struct ParticleServerSimulationSystem : ISystem {

	[BurstCompile]
	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
		state.RequireForUpdate<ParticleCoreData>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state) {
		var bufferSystem =
			SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
		var buffer = bufferSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

		state.Dependency = new ParticleServerSimulationJob {
			Buffer = buffer,
		}.ScheduleParallel(state.Dependency);
	}
}

[BurstCompile, WithAll(typeof(Simulate))]
partial struct ParticleServerSimulationJob : IJobEntity {
	public EntityCommandBuffer.ParallelWriter Buffer;

	public void Execute(
		in ParticleCoreData coreData,
		Entity entity,
		[ChunkIndexInQuery] int chunkIndex) {

		Buffer.DestroyEntity(chunkIndex, entity);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Particle Client Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSClientSimulationSystemGroup))]
partial struct ParticleClientSimulationSystem : ISystem {

	[BurstCompile]
	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
		state.RequireForUpdate<PhysicsWorldSingleton>();
		state.RequireForUpdate<CameraBridgeSystem.Singleton>();
		state.RequireForUpdate<ParticleCoreData>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state) {
		var bufferSystem =
			SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
		var buffer = bufferSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
		var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
		var cameraBridge = SystemAPI.GetSingleton<CameraBridgeSystem.Singleton>();

		state.Dependency = new ParticleClientInitializationJob {
		}.ScheduleParallel(state.Dependency);
		state.Dependency = new ParticleClientSimulationJob {
			Buffer               = buffer,
			PhysicsWorld         = physicsWorld,
			RenderAreaLookup     = SystemAPI.GetComponentLookup<RenderArea>(true),
			CameraBridgeProperty = cameraBridge.Property[0],
			DeltaTime            = SystemAPI.Time.DeltaTime,
		}.ScheduleParallel(state.Dependency);
	}
}

[BurstCompile, WithAll(typeof(Simulate))]
partial struct ParticleClientInitializationJob : IJobEntity {

	public void Execute(
		EnabledRefRW<ParticleInitialize> initialize,
		in ParticleCoreBlob coreBlob,
		ref ParticleCoreData coreData,
		ref SpriteHash hash,
		Entity entity) {

		initialize.ValueRW = false;
		var seed = new int2(entity.Index, entity.Version);
		coreData.Random = new Random(math.hash(seed));

		if (coreBlob.GetPattern(Pattern.FlipRandom)) {
			hash.Flip = coreData.Random.NextBool2();
		}
	}
}

[BurstCompile, WithAll(typeof(Simulate))]
partial struct ParticleClientSimulationJob : IJobEntity {
	public EntityCommandBuffer.ParallelWriter Buffer;
	[ReadOnly] public PhysicsWorld PhysicsWorld;
	[ReadOnly] public ComponentLookup<RenderArea> RenderAreaLookup;
	[ReadOnly] public CameraBridge.Property CameraBridgeProperty;
	[ReadOnly] public float DeltaTime;

	public void Execute(
		in ParticleCoreBlob coreBlob,
		ref ParticleCoreData coreData,
		ref LocalTransform transform,
		ref SpriteHash hash,
		Entity entity,
		[ChunkIndexInQuery] int chunkIndex) {

		if (coreBlob.GetPattern(Pattern.RelativeYaw)) {
			var rotation = transform.Rotation.value;
			float x = 0.0f + 2.0f * (rotation.y * rotation.w + rotation.x * rotation.z);
			float z = 1.0f - 2.0f * (rotation.y * rotation.y + rotation.z * rotation.z);
			float yaw = math.atan2(x, z) * math.TODEGREES;
			hash.ObjectYaw = yaw - CameraBridgeProperty.Yaw;
		}
		if (coreBlob.GetPattern(Pattern.HasGravity)) {
			coreData.Velocity += coreBlob.Value.Value.Gravity * DeltaTime;
		}

		hash.Time = coreData.Time += DeltaTime;
		if (coreBlob.Value.Value.Duration <= coreData.Time) {
			Buffer.DestroyEntity(chunkIndex, entity);
		}
		if (math.any(coreData.Velocity != default)) {
			transform.Position += coreData.Velocity * DeltaTime;
			transform.Rotation = quaternion.LookRotation(-coreData.Velocity, math.up());
		}
	}
}

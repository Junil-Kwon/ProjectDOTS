using UnityEngine;
using System;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Physics;
using Random = Unity.Mathematics.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif



// Particle Patterns

public enum Pattern : byte {
	None,
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Particle Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Particle")]
public class ParticleAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(ParticleAuthoring))]
	class ParticleAuthoringEditor : EditorExtensions {
		ParticleAuthoring I => target as ParticleAuthoring;
		public override void OnInspectorGUI() {
			Begin("Particle Authoring");

			LabelField("Particle", EditorStyles.boldLabel);
			I.Pattern = FlagField<Pattern>("Pattern", I.Pattern);
			Space();

			I.Lifetime   = FloatField("Lifetime",    I.Lifetime);
			I.FlipRandom = Toggle2   ("Flip Random", I.FlipRandom);
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] uint m_Pattern;

	[SerializeField] float m_Lifetime = 1f;
	[SerializeField] bool2 m_FlipRandom = new(false, false);



	// Properties

	public uint Pattern {
		get => m_Pattern;
		set => m_Pattern = value;
	}
	public bool GetPattern(Pattern pattern) => (Pattern & (1u << (int)pattern)) != 0;
	public void SetPattern(Pattern pattern, bool value) {
		Pattern = value ? (Pattern | (1u << (int)pattern)) : (Pattern & ~(1u << (int)pattern));
	}
	public bool HasPattern   (Pattern pattern) => GetPattern(pattern);
	public void AddPattern   (Pattern pattern) => SetPattern(pattern, true);
	public void RemovePattern(Pattern pattern) => SetPattern(pattern, false);

	public float Lifetime {
		get => m_Lifetime;
		set => m_Lifetime = value;
	}
	public bool2 FlipRandom {
		get => m_FlipRandom;
		set => m_FlipRandom = value;
	}



	// Baker

	public class Baker : Baker<ParticleAuthoring> {
		public override void Bake(ParticleAuthoring authoring) {
			Entity entity = GetEntity(TransformUsageFlags.Dynamic);
			AddComponent(entity, new ParticleInitialize());
			AddComponent(entity, new Particle {

				Lifetime   = authoring.Lifetime,
				FlipRandom = authoring.FlipRandom,

			});
			//if (authoring.HasPattern(Pattern.None)) AddComponent(entity, new NonePattern {});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Particle Initialize
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct ParticleInitialize : IComponentData, IEnableableComponent { }



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Particle
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct Particle : IComponentData {

	public float Lifetime;
	public bool2 FlipRandom;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Particle Pattern
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━





// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Particle Initialization System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSInitializationSystemGroup))]
partial struct ParticleInitializationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<ParticleInitialize>();
	}

	public void OnUpdate(ref SystemState state) {
		state.Dependency = new ParticleInitializationJob() {
			Random = new Random(1u + (uint)(4801 * SystemAPI.Time.ElapsedTime) % 1000),
		}.ScheduleParallel(state.Dependency);
	}

	[BurstCompile, WithAll(typeof(ParticleInitialize))]
	partial struct ParticleInitializationJob : IJobEntity {
		public Random Random;
		public void Execute(
			in Particle particle,
			DynamicBuffer<SpriteDrawer> sprite,
			EnabledRefRW<ParticleInitialize> initialize) {

			initialize.ValueRW = false;
			if (particle.FlipRandom.x || particle.FlipRandom.y) {
				for (int i = 0; i < sprite.Length; i++) {
					var flip = sprite.ElementAt(i).Flip;
					if (particle.FlipRandom.x) flip.x = Random.NextBool();
					if (particle.FlipRandom.y) flip.y = Random.NextBool();
					sprite.ElementAt(i).Flip = flip;
				}
			}
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Particle Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSSimulationSystemGroup))]
partial struct ParticleSimulationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
		state.RequireForUpdate<Particle>();
	}

	public void OnUpdate(ref SystemState state) {
		var singleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
		state.Dependency = new ParticleLifetimeSimulationJob() {
			Buffer    = singleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
			DeltaTime = SystemAPI.Time.DeltaTime,
		}.ScheduleParallel(state.Dependency);
	}

	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct ParticleLifetimeSimulationJob : IJobEntity {
		public EntityCommandBuffer.ParallelWriter Buffer;
		public float DeltaTime;
		public void Execute(
			[ChunkIndexInQuery] int sortKey,
			Entity entity,
			ref Particle particle) {

			if ((particle.Lifetime -= DeltaTime) <= 0f) Buffer.DestroyEntity(sortKey, entity);
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Particle Presentation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSPresentationSystemGroup))]
partial struct ParticlePresentationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<Particle>();
	}

	public void OnUpdate(ref SystemState state) {
		var deltaTime = SystemAPI.Time.DeltaTime;
		var tilePresentationJob   = new ParticleTilePresentationJob  () { DeltaTime = deltaTime, };
		var spritePresentationJob = new ParticleSpritePresentationJob() { DeltaTime = deltaTime, };
		var shadowPresentationJob = new ParticleShadowPresentationJob() { DeltaTime = deltaTime, };
		var uiPresentationJob     = new ParticleUIPresentationJob    () { DeltaTime = deltaTime, };
		var tile   = tilePresentationJob  .ScheduleParallel(state.Dependency);
		var sprite = spritePresentationJob.ScheduleParallel(state.Dependency);
		var shadow = shadowPresentationJob.ScheduleParallel(state.Dependency);
		var ui     = uiPresentationJob    .ScheduleParallel(state.Dependency);
		var combined     = JobHandle.CombineDependencies(tile, sprite, shadow);
		state.Dependency = JobHandle.CombineDependencies(combined, ui);
	}

	[BurstCompile, WithAll(typeof(Particle))]
	partial struct ParticleTilePresentationJob : IJobEntity {
		public float DeltaTime;
		public void Execute(DynamicBuffer<TileDrawer> tile) {
			for (int i = 0; i < tile.Length; i++) tile.ElementAt(i).Offset += DeltaTime;
		}
	}

	[BurstCompile, WithAll(typeof(Particle))]
	partial struct ParticleSpritePresentationJob : IJobEntity {
		public float DeltaTime;
		public void Execute(DynamicBuffer<SpriteDrawer> sprite) {
			for (int i = 0; i < sprite.Length; i++) sprite.ElementAt(i).Offset += DeltaTime;
		}
	}

	[BurstCompile, WithAll(typeof(Particle))]
	partial struct ParticleShadowPresentationJob : IJobEntity {
		public float DeltaTime;
		public void Execute(DynamicBuffer<ShadowDrawer> shadow) {
			for (int i = 0; i < shadow.Length; i++) shadow.ElementAt(i).Offset += DeltaTime;
		}
	}

	[BurstCompile, WithAll(typeof(Particle))]
	partial struct ParticleUIPresentationJob : IJobEntity {
		public float DeltaTime;
		public void Execute(DynamicBuffer<UIDrawer> ui) {
			for (int i = 0; i < ui.Length; i++) ui.ElementAt(i).Offset += DeltaTime;
		}
	}
}

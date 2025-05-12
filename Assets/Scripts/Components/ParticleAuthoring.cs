using UnityEngine;
using System;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Physics;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━

public enum Pattern : byte {
	None,
}
public static class ParticleExtensions {
	public static Pattern ToEnum(this ComponentType type) => type switch {
		_ when type == ComponentType.ReadWrite<NonePattern>() => Pattern.None,
		_ => default,
	};
	public static ComponentType ToComponentType(this Pattern pattern) => pattern switch {
		Pattern.None => ComponentType.ReadWrite<NonePattern>(),
		_ => default,
	};
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
				BeginHorizontal();
				PrefixLabel("Pattern Component");
				I.PatternString = TextField(I.PatternString);
				I.Pattern       = EnumField(I.Pattern);
				EndHorizontal();
				I.Lifetime = FloatField("Lifetime", I.Lifetime);
				Space();

				End();
			}
		}
	#endif



	// Fields

	[SerializeField] string m_PatternString;
	[SerializeField] float  m_Lifetime = 1f;



	// Properties

	public string PatternString {
		get => m_PatternString;
		set => m_PatternString = value;
	}
	public Pattern Pattern {
		get => Enum.TryParse(PatternString, out Pattern pattern) ? pattern : default;
		set => m_PatternString = value.ToString();
	}
	public float Lifetime {
		get => m_Lifetime;
		set => m_Lifetime = value;
	}



	// Baker

	public class Baker : Baker<ParticleAuthoring> {
		public override void Bake(ParticleAuthoring authoring) {
			Entity entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new Particle {

				Lifetime = authoring.Lifetime,

			});
			AddComponent(entity, authoring.Pattern.ToComponentType());
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Particle
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct Particle : IComponentData {

	// Fields

	public float Lifetime;
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Particle Pattern
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct NonePattern : IComponentData { }



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Particle Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
partial struct ParticleSimulationSystem : ISystem {

	[BurstCompile]
	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
		state.RequireForUpdate<Particle>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state) {
		var singleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
		state.Dependency = new ParticleLifetimeSimulationJob() {
			buffer    = singleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
			deltaTime = SystemAPI.Time.DeltaTime,
		}.ScheduleParallel(state.Dependency);
	}

	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct ParticleLifetimeSimulationJob : IJobEntity {
		public EntityCommandBuffer.ParallelWriter buffer;
		public float deltaTime;
		public void Execute(Entity entity, [ChunkIndexInQuery] int sortKey, ref Particle particle) {
			if ((particle.Lifetime -= deltaTime) <= 0f) buffer.DestroyEntity(sortKey, entity);
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Particle Presentation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(PresentationSystemGroup))]
partial struct ParticlePresentationSystem : ISystem {

	[BurstCompile]
	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<Particle>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state) {
		var deltaTime = SystemAPI.Time.DeltaTime;
		var tilePresentationJob   = new ParticleTilePresentationJob  () { deltaTime = deltaTime, };
		var spritePresentationJob = new ParticleSpritePresentationJob() { deltaTime = deltaTime, };
		var shadowPresentationJob = new ParticleShadowPresentationJob() { deltaTime = deltaTime, };
		var uiPresentationJob     = new ParticleUIPresentationJob    () { deltaTime = deltaTime, };
		var tile   = tilePresentationJob  .ScheduleParallel(state.Dependency);
		var sprite = spritePresentationJob.ScheduleParallel(state.Dependency);
		var shadow = shadowPresentationJob.ScheduleParallel(state.Dependency);
		var ui     = uiPresentationJob    .ScheduleParallel(state.Dependency);
		var combined     = JobHandle.CombineDependencies(tile, sprite, shadow);
		state.Dependency = JobHandle.CombineDependencies(combined, ui);
	}

	[BurstCompile, WithAll(typeof(Particle))]
	partial struct ParticleTilePresentationJob : IJobEntity {
		public float deltaTime;
		public void Execute(DynamicBuffer<TileDrawer> tile) {
			for (int i = 0; i < tile.Length; i++) tile.ElementAt(i).Offset += deltaTime;
		}
	}

	[BurstCompile, WithAll(typeof(Particle))]
	partial struct ParticleSpritePresentationJob : IJobEntity {
		public float deltaTime;
		public void Execute(DynamicBuffer<SpriteDrawer> sprite) {
			for (int i = 0; i < sprite.Length; i++) sprite.ElementAt(i).Offset += deltaTime;
		}
	}

	[BurstCompile, WithAll(typeof(Particle))]
	partial struct ParticleShadowPresentationJob : IJobEntity {
		public float deltaTime;
		public void Execute(DynamicBuffer<ShadowDrawer> shadow) {
			for (int i = 0; i < shadow.Length; i++) shadow.ElementAt(i).Offset += deltaTime;
		}
	}

	[BurstCompile, WithAll(typeof(Particle))]
	partial struct ParticleUIPresentationJob : IJobEntity {
		public float deltaTime;
		public void Execute(DynamicBuffer<UIDrawer> ui) {
			for (int i = 0; i < ui.Length; i++) ui.ElementAt(i).Offset += deltaTime;
		}
	}
}

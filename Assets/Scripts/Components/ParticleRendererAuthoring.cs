using UnityEngine;
using UnityEngine.Rendering;
using System;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Physics;
using Unity.Jobs;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Particle Renderer Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Particle Renderer")]
[RequireComponent(typeof(ParticleCoreAuthoring))]
public sealed class ParticleRendererAuthoring : MonoComponent<ParticleRendererAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(ParticleRendererAuthoring))]
	class ParticleRendererAuthoringEditor : EditorExtensions {
		ParticleRendererAuthoring I => target as ParticleRendererAuthoring;
		public override void OnInspectorGUI() {
			Begin();

			BeginDisabledGroup(I.IsPrefabConnected);
			I.Sprite = TextEnumField("Sprite", I.Sprite);
			I.Motion = TextEnumField("Motion", I.Motion);
			I.Center = Vector3Field("Center", I.Center);
			I.BaseColor = ColorField("Base Color", I.BaseColor);
			I.MaskColor = ColorField("Mask Color", I.MaskColor);
			I.Emission  = ColorField("Emission",   I.Emission);
			EndDisabledGroup();

			End();
		}
	}
	#endif



	// Fields

	#if UNITY_EDITOR
	[SerializeField] string m_SpriteName;
	[SerializeField] string m_MotionName;
	#endif

	[SerializeField] Sprite m_Sprite;
	[SerializeField] Motion m_Motion;

	[SerializeField] Vector3 m_Center;
	[SerializeField] Vector4 m_BaseColor = new(1f, 1f, 1f, 1f);
	[SerializeField] Vector4 m_MaskColor = new(1f, 1f, 1f, 0f);
	[SerializeField] Vector4 m_Emission  = new(0f, 0f, 0f, 0f);



	// Properties

	#if UNITY_EDITOR
	Sprite Sprite {
		get => !Enum.TryParse(m_SpriteName, out Sprite sprite) ?
			Enum.Parse<Sprite>(m_SpriteName = m_Sprite.ToString()) :
			m_Sprite = sprite;
		set => m_SpriteName = (m_Sprite = value).ToString();
	}
	Motion Motion {
		get => !Enum.TryParse(m_MotionName, out Motion motion) ?
			Enum.Parse<Motion>(m_MotionName = m_Motion.ToString()) :
			m_Motion = motion;
		set => m_MotionName = (m_Motion = value).ToString();
	}
	#else
	Sprite Sprite {
		get => m_Sprite;
		set => m_Sprite = value;
	}
	Motion Motion {
		get => m_Motion;
		set => m_Motion = value;
	}
	#endif

	Vector3 Center {
		get => m_Center;
		set => m_Center = value;
	}
	Vector4 BaseColor {
		get => m_BaseColor;
		set => m_BaseColor = value;
	}
	Vector4 MaskColor {
		get => m_MaskColor;
		set => m_MaskColor = value;
	}
	Vector4 Emission {
		get => m_Emission;
		set => m_Emission = value;
	}



	// Baker

	class Baker : Baker<ParticleRendererAuthoring> {
		public override void Bake(ParticleRendererAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.Dynamic);
			AddComponent(entity, new ParticleRenderer {

				Sprite    = authoring.Sprite,
				Motion    = authoring.Motion,
				Center    = authoring.Center,
				BaseColor = authoring.BaseColor,
				MaskColor = authoring.MaskColor,
				Emission  = authoring.Emission,

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Particle Renderer
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct ParticleRenderer : IComponentData {

	// Fields

	public bool Initialized;
	public int MainCullingMask;
	public int TempCullingMask;
	public float Transition;
	public float Alpha0;
	public float Alpha1;

	public Sprite Sprite;
	public Motion Motion;
	public float ObjectYaw;
	public float Time;
	public bool2 Flip;

	public float3 Center;
	public float4 BaseColor;
	public float4 MaskColor;
	public float4 Emission;



	// Properties

	public float this[int index] {
		get => (index == 0) ? Alpha0 : Alpha1;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Particle Renderer Simultaion System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSClientSimulationSystemGroup))]
[UpdateAfter(typeof(ParticleClientSimulationSystem))]
partial struct ParticleRendererSimulationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<PhysicsWorldSingleton>();
		state.RequireForUpdate<CameraBridgeSystem.Singleton>();
		state.RequireForUpdate<RenderZonePresentationSystem.Singleton>();
		state.RequireForUpdate<ParticleRenderer>();
	}

	public void OnUpdate(ref SystemState state) {
		var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
		var cameraBridge = SystemAPI.GetSingleton<CameraBridgeSystem.Singleton>();
		var singleton = SystemAPI.GetSingleton<RenderZonePresentationSystem.Singleton>();

		state.Dependency = new ParticleRendererSimulationJob {
			PhysicsWorld         = physicsWorld,
			CameraBridgeProperty = cameraBridge.Property[0],
			RenderZoneLookup     = SystemAPI.GetComponentLookup<RenderZone>(true),
			MainRenderZone       = singleton.MainRenderZone[0],
			TempRenderZone       = singleton.TempRenderZone[0],
			DeltaTime            = SystemAPI.Time.DeltaTime,
		}.ScheduleParallel(state.Dependency);
	}
}

[BurstCompile, WithAll(typeof(Simulate))]
partial struct ParticleRendererSimulationJob : IJobEntity {
	[ReadOnly] public PhysicsWorld PhysicsWorld;
	[ReadOnly] public CameraBridge.Property CameraBridgeProperty;
	[ReadOnly] public ComponentLookup<RenderZone> RenderZoneLookup;
	[ReadOnly] public RenderZone MainRenderZone;
	[ReadOnly] public RenderZone TempRenderZone;
	[ReadOnly] public float DeltaTime;

	public void Execute(
		ref ParticleRenderer renderer,
		in LocalTransform transform) {

		var renderZone = new RenderZone {
			CullingMask = 1 << 0,
		};
		if (PhysicsWorld.CalculateDistance(new PointDistanceInput {
			Position     = transform.Position + renderer.Center,
			MaxDistance  = 0f,
			Filter       = new CollisionFilter {
			BelongsTo    = uint.MaxValue,
			CollidesWith = 1u << (int)PhysicsCategory.RenderZone,
			}, }, out var hit)) {
			renderZone = RenderZoneLookup[hit.Entity];
		}
		float transition = renderer.Transition;
		if (renderer.MainCullingMask != renderZone.CullingMask) {
			renderer.TempCullingMask = renderer.MainCullingMask;
			renderer.MainCullingMask = renderZone.CullingMask;
			transition = (0f < transition) ? (1f - transition) : float.Epsilon;
		}
		if (0f < transition) {
			float delta = DeltaTime / RenderZone.TransitionTime;
			transition = math.min(transition + delta, 1f);
			if (transition == 1f) {
				transition = 0f;
				renderer.TempCullingMask = renderer.MainCullingMask;
			}
		}
		if (renderer.Transition != transition) {
			if (renderer.Initialized != true) {
				renderer.Initialized = true;
				transition = 1f;
			}
			renderer.Transition = transition;
		}
		bool main = (MainRenderZone.CullingMask & renderer.MainCullingMask) != 0;
		bool temp = (TempRenderZone.CullingMask & renderer.TempCullingMask) != 0;
		renderer.Alpha0 = main ? 1f : (0f < transition) ? (1f - transition) : 0f;
		renderer.Alpha1 = temp ? 1f : (0f < transition) ? (0f + transition) : 0f;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Particle Renderer Presentation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(DOTSPresentationSystemGroup))]
public partial class ParticleRendererPresentationSystem : SystemBase {
	IndirectRenderer<SpriteDrawData>[] SpriteRenderer;
	EntityQuery ParticleQuery;

	const int Length = 2;

	protected override void OnCreate() {
		SpriteRenderer = new IndirectRenderer<SpriteDrawData>[Length];
		for (int i = 0; i < Length; i++) {
			SpriteRenderer[i] = new(DrawManager.QuadMesh, DrawManager.SpriteMaterial);
			SpriteRenderer[i].Param.shadowCastingMode = ShadowCastingMode.Off;
		}
		RequireForUpdate<RenderZonePresentationSystem.Singleton>();
		ParticleQuery = GetEntityQuery(
			ComponentType.ReadOnly<ParticleRenderer>(),
			ComponentType.ReadOnly<LocalToWorld>());
		RequireForUpdate(ParticleQuery);
	}

	protected override void OnUpdate() {
		var singleton = SystemAPI.GetSingleton<RenderZonePresentationSystem.Singleton>();
		var entityArray = ParticleQuery.ToEntityArray(Allocator.TempJob);
		int count = entityArray.Length;
		for (int i = 0; i < Length; i++) {
			if (RenderZonePresentationSystem.TryGetData(
				singleton, i, out RenderZone renderZone, out int layer)) {
				SpriteRenderer[i].Param.layer = layer;
			} else continue;
			EnvironmentManager.LightMode = renderZone.LightMode;

			var spriteBuffer = SpriteRenderer[i].LockBuffer(count);
			new ParticleSpritePresentationJob {
				SpriteHashMap   = DrawManager.SpriteHashMapReadOnly,
				EntityArray     = entityArray,
				RendererLookup  = GetComponentLookup<ParticleRenderer>(true),
				TransformLookup = GetComponentLookup<LocalToWorld>(true),
				SpriteBuffer    = spriteBuffer,
				GlobalYaw       = CameraManager.Yaw,
				Index           = i,
			}.Schedule(entityArray.Length, 64, Dependency).Complete();
			SpriteRenderer[i].UnlockBuffer(count);
			SpriteRenderer[i].Draw();
			SpriteRenderer[i].Clear();
		}
		entityArray.Dispose();
	}

	protected override void OnDestroy() {
		for (int i = 0; i < Length; i++) {
			SpriteRenderer[i].Dispose();
		}
	}
}

[BurstCompile]
partial struct ParticleSpritePresentationJob : IJobParallelFor {
	[ReadOnly] public NativeHashMap<uint, AtlasData>.ReadOnly SpriteHashMap;
	[ReadOnly] public NativeArray<Entity> EntityArray;
	[ReadOnly] public ComponentLookup<ParticleRenderer> RendererLookup;
	[ReadOnly] public ComponentLookup<LocalToWorld> TransformLookup;
	[NativeDisableParallelForRestriction] public NativeArray<SpriteDrawData> SpriteBuffer;
	[ReadOnly] public float GlobalYaw;
	[ReadOnly] public int Index;

	public void Execute(int index) {
		var entity = EntityArray[index];
		var renderer = RendererLookup[entity];
		var data = DrawSpriteJob.GetSpriteData(SpriteHashMap, new SpriteHash() {
			Sprite    = renderer.Sprite,
			Motion    = renderer.Motion,
			Direction = default,
			ObjectYaw = renderer.ObjectYaw,
			Time      = renderer.Time,
		}, true);
		renderer.BaseColor.w *= renderer[Index];
		SpriteBuffer[index] = new SpriteDrawData() {
			Position  = TransformLookup[entity].Position,
			Rotation  = quaternion.Euler(new float3(0f, GlobalYaw, 0f) * math.TORADIANS).value,
			Scale     = data.scale,
			Pivot     = data.pivot,
			Tiling    = data.tiling,
			Offset    = data.offset,
			Center    = renderer.Center,
			BaseColor = renderer.BaseColor,
			MaskColor = renderer.MaskColor,
			Emission  = renderer.Emission,
			Billboard = 1f,
		};
	}
}

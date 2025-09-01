using UnityEngine;
using UnityEngine.Rendering;
using System;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Physics;
using Unity.NetCode;
using Unity.Jobs;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Renderer Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Character Renderer")]
[RequireComponent(typeof(CharacterCoreAuthoring))]
public sealed class CharacterRendererAuthoring : MonoComponent<CharacterRendererAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(CharacterRendererAuthoring))]
	class CharacterRendererAuthoringEditor : EditorExtensions {
		CharacterRendererAuthoring I => target as CharacterRendererAuthoring;
		public override void OnInspectorGUI() {
			Begin();

			BeginDisabledGroup(I.IsPrefabConnected);
			I.Center = Vector3Field("Center", I.Center);
			I.Sprite = TextEnumField("Sprite", I.Sprite);
			I.Motion = TextEnumField("Motion", I.Motion);
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

	class Baker : Baker<CharacterRendererAuthoring> {
		public override void Bake(CharacterRendererAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.Dynamic);
			AddComponent(entity, new CharacterRenderer {

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
// Character Renderer
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CharacterRenderer : IComponentData {

	// Fields

	public bool Initialized;
	public int MainCullingMask;
	public int TempCullingMask;
	public float Transition;
	public float MainAlpha;
	public float TempAlpha;

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
		get => (index == 0) ? MainAlpha : TempAlpha;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Renderer Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSClientSimulationSystemGroup), OrderLast = true)]
partial struct CharacterRendererSimulationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<PhysicsWorldSingleton>();
		state.RequireForUpdate<RenderZonePresentationSystem.Singleton>();
		state.RequireForUpdate<CharacterRenderer>();
	}

	public void OnUpdate(ref SystemState state) {
		var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
		var singleton = SystemAPI.GetSingleton<RenderZonePresentationSystem.Singleton>();

		state.Dependency = new CharacterRendererSimulationJob {
			PhysicsWorld     = physicsWorld,
			RenderZoneLookup = SystemAPI.GetComponentLookup<RenderZone>(true),
			MainRenderZone   = singleton.MainRenderZone[0],
			TempRenderZone   = singleton.TempRenderZone[0],
			Transition       = singleton.Transition[0],
			DeltaTime        = SystemAPI.Time.DeltaTime,
		}.ScheduleParallel(state.Dependency);
	}
}

[BurstCompile, WithAll(typeof(Simulate))]
partial struct CharacterRendererSimulationJob : IJobEntity {
	[ReadOnly] public PhysicsWorld PhysicsWorld;
	[ReadOnly] public ComponentLookup<RenderZone> RenderZoneLookup;
	[ReadOnly] public RenderZone MainRenderZone;
	[ReadOnly] public RenderZone TempRenderZone;
	[ReadOnly] public float Transition;
	[ReadOnly] public float DeltaTime;

	public void Execute(
		ref CharacterRenderer renderer,
		in LocalTransform transform) {

		var renderZone = new RenderZone {
			CullingMask = 1 << 0,
		};
		if (PhysicsWorld.CalculateDistance(new PointDistanceInput {
			Position     = transform.Position + new float3(0f, 0.1f, 0f),
			MaxDistance  = 0f,
			Filter       = new CollisionFilter {
			BelongsTo    = uint.MaxValue,
			CollidesWith = 1u << (int)PhysicsCategory.RenderZone,
			}, }, out var hit)) {
			renderZone = RenderZoneLookup[hit.Entity];
		}
		if (renderer.MainCullingMask != renderZone.CullingMask) {
			renderer.TempCullingMask = renderer.MainCullingMask;
			renderer.MainCullingMask = renderZone.CullingMask;
			bool match = (0f < renderer.Transition) && (renderer.Transition < 1f);
			renderer.Transition = match ? (1f - renderer.Transition) : float.Epsilon;
		}
		if (0f < renderer.Transition && renderer.Transition < 1f) {
			float delta = DeltaTime / RenderZone.TransitionTime;
			renderer.Transition = math.min(renderer.Transition + delta, 1f);
		} else if (renderer.Transition == 1f) {
			renderer.Transition = 0f;
			renderer.TempCullingMask = renderer.MainCullingMask;
		}

		if (renderer.Initialized != true) {
			renderer.Initialized = true;
			renderer.MainCullingMask = renderZone.CullingMask;
			renderer.TempCullingMask = renderZone.CullingMask;
			renderer.Transition = 0f;
		}
		bool mainmain = (renderer.MainCullingMask & MainRenderZone.CullingMask) != 0;
		bool maintemp = (renderer.MainCullingMask & TempRenderZone.CullingMask) != 0;
		bool tempmain = (renderer.TempCullingMask & MainRenderZone.CullingMask) != 0;
		bool temptemp = (renderer.TempCullingMask & TempRenderZone.CullingMask) != 0;
		float transition = renderer.Transition;
		if (transition == 0f) {
			renderer.MainAlpha = mainmain ? 1f : 0f;
			renderer.TempAlpha = maintemp ? 1f : 0f;
		} else if (0f < transition && transition < 1f) {
			float main = 1f - transition;
			float temp = 0f + transition;
			renderer.MainAlpha = 1f - ((mainmain ? main : 0f) + (tempmain ? temp : 0f));
			renderer.TempAlpha = 1f - ((maintemp ? main : 0f) + (temptemp ? temp : 0f));
		} else if (transition == 1f) {
			renderer.MainAlpha = tempmain ? 0f : 1f;
			renderer.TempAlpha = temptemp ? 0f : 1f;
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Renderer Presentation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(DOTSPresentationSystemGroup))]
public partial class CharacterRendererPresentationSystem : SystemBase {
	IndirectRenderer<SpriteDrawData>[] SpriteRenderer;
	IndirectRenderer<SpriteDrawData>[] RiseShadowRenderer;
	IndirectRenderer<ShadowDrawData>[] FlatShadowRenderer;
	EntityQuery CharacterQuery;

	const int Length = 2;

	protected override void OnCreate() {
		SpriteRenderer = new IndirectRenderer<SpriteDrawData>[Length];
		RiseShadowRenderer = new IndirectRenderer<SpriteDrawData>[Length];
		FlatShadowRenderer = new IndirectRenderer<ShadowDrawData>[Length];
		for (int i = 0; i < Length; i++) {
			SpriteRenderer[i] = new(DrawManager.QuadMesh, DrawManager.SpriteMaterial);
			RiseShadowRenderer[i] = new(DrawManager.QuadMesh, DrawManager.SpriteMaterial);
			FlatShadowRenderer[i] = new(DrawManager.QuadMesh, DrawManager.ShadowMaterial);
			SpriteRenderer[i].Param.shadowCastingMode = ShadowCastingMode.Off;
			RiseShadowRenderer[i].Param.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
			FlatShadowRenderer[i].Param.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
			SpriteRenderer[i].Param.layer = 1 << i;
			RiseShadowRenderer[i].Param.layer = 1 << i;
			FlatShadowRenderer[i].Param.layer = 1 << i;
		}
		RequireForUpdate<RenderZonePresentationSystem.Singleton>();
		CharacterQuery = GetEntityQuery(
			ComponentType.ReadOnly<CharacterRenderer>(),
			ComponentType.ReadOnly<LocalToWorld>());
		RequireForUpdate(CharacterQuery);
	}

	protected override void OnUpdate() {
		var singleton = SystemAPI.GetSingleton<RenderZonePresentationSystem.Singleton>();
		var entityArray = CharacterQuery.ToEntityArray(Allocator.TempJob);
		int count = entityArray.Length;
		for (int i = 0; i < Length; i++) {
			if (0 < i && singleton.Transition[0] == 0f) continue;
			var renderZone = i switch {
				0 => singleton.MainRenderZone[0],
				_ => singleton.TempRenderZone[0],
			};
			EnvironmentManager.LightMode = renderZone.LightMode;

			var spriteBuffer = SpriteRenderer[i].LockBuffer(count);
			new CharacterSpritePresentationJob {
				SpriteHashMap   = DrawManager.SpriteHashMapReadOnly,
				EntityArray     = entityArray,
				RendererLookup  = GetComponentLookup<CharacterRenderer>(true),
				TransformLookup = GetComponentLookup<LocalToWorld>(true),
				SpriteBuffer    = spriteBuffer,
				GlobalYaw       = CameraManager.Yaw,
				Index           = i,
			}.Schedule(entityArray.Length, 64, Dependency).Complete();
			SpriteRenderer[i].UnlockBuffer(count);
			SpriteRenderer[i].Draw();
			SpriteRenderer[i].Clear();

			var riseShadowBuffer = RiseShadowRenderer[i].LockBuffer(count);
			new CharacterRiseShadowPresentationJob {
				SpriteHashMap   = DrawManager.SpriteHashMapReadOnly,
				EntityArray     = entityArray,
				RendererLookup  = GetComponentLookup<CharacterRenderer>(true),
				TransformLookup = GetComponentLookup<LocalToWorld>(true),
				SpriteBuffer    = riseShadowBuffer,
				GlobalYaw       = EnvironmentManager.Rotation.eulerAngles.y,
				Mask            = renderZone.CullingMask,
			}.Schedule(entityArray.Length, 64, Dependency).Complete();
			RiseShadowRenderer[i].UnlockBuffer(count);
			RiseShadowRenderer[i].Draw();
			RiseShadowRenderer[i].Clear();

			var flatShadowBuffer = FlatShadowRenderer[i].LockBuffer(count);
			new CharacterFlatShadowPresentationJob {
				ShadowHashMap   = DrawManager.ShadowHashMapReadOnly,
				EntityArray     = entityArray,
				RendererLookup  = GetComponentLookup<CharacterRenderer>(true),
				TransformLookup = GetComponentLookup<LocalToWorld>(true),
				ShadowBuffer    = flatShadowBuffer,
				GlobalYaw       = CameraManager.Yaw,
				Mask            = renderZone.CullingMask,
			}.Schedule(entityArray.Length, 64, Dependency).Complete();
			FlatShadowRenderer[i].UnlockBuffer(count);
			FlatShadowRenderer[i].Draw();
			FlatShadowRenderer[i].Clear();
		}
		entityArray.Dispose();
	}

	protected override void OnDestroy() {
		for (int i = 0; i < Length; i++) {
			SpriteRenderer[i].Dispose();
			RiseShadowRenderer[i].Dispose();
			FlatShadowRenderer[i].Dispose();
		}
	}
}

[BurstCompile]
partial struct CharacterSpritePresentationJob : IJobParallelFor {
	[ReadOnly] public NativeHashMap<uint, AtlasData>.ReadOnly SpriteHashMap;
	[ReadOnly] public NativeArray<Entity> EntityArray;
	[ReadOnly] public ComponentLookup<CharacterRenderer> RendererLookup;
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

[BurstCompile]
partial struct CharacterRiseShadowPresentationJob : IJobParallelFor {
	[ReadOnly] public NativeHashMap<uint, AtlasData>.ReadOnly SpriteHashMap;
	[ReadOnly] public NativeArray<Entity> EntityArray;
	[ReadOnly] public ComponentLookup<CharacterRenderer> RendererLookup;
	[ReadOnly] public ComponentLookup<LocalToWorld> TransformLookup;
	[NativeDisableParallelForRestriction] public NativeArray<SpriteDrawData> SpriteBuffer;
	[ReadOnly] public float GlobalYaw;
	[ReadOnly] public int Mask;

	public void Execute(int index) {
		var entity = EntityArray[index];
		var renderer = RendererLookup[entity];
		var data = DrawSpriteJob.GetSpriteData(SpriteHashMap, new SpriteHash() {
			Sprite    = renderer.Sprite,
			Motion    = renderer.Motion,
			Direction = default,
			ObjectYaw = renderer.ObjectYaw - GlobalYaw,
			Time      = renderer.Time,
		}, false);
		if ((renderer.MainCullingMask & Mask) == 0) data.scale = default;
		SpriteBuffer[index] = new SpriteDrawData() {
			Position  = TransformLookup[entity].Position,
			Rotation  = quaternion.Euler(new float3(0f, GlobalYaw, 0f) * math.TORADIANS).value,
			Scale     = data.scale,
			Pivot     = data.pivot,
			Tiling    = data.tiling,
			Offset    = data.offset,
			Center    = renderer.Center,
			BaseColor = new float4(1f, 1f, 1f, 1f),
			MaskColor = new float4(1f, 1f, 1f, 0f),
			Emission  = new float4(0f, 0f, 0f, 0f),
			Billboard = 0f,
		};
	}
}

[BurstCompile]
partial struct CharacterFlatShadowPresentationJob : IJobParallelFor {
	[ReadOnly] public NativeHashMap<uint, AtlasData>.ReadOnly ShadowHashMap;
	[ReadOnly] public NativeArray<Entity> EntityArray;
	[ReadOnly] public ComponentLookup<CharacterRenderer> RendererLookup;
	[ReadOnly] public ComponentLookup<LocalToWorld> TransformLookup;
	[NativeDisableParallelForRestriction] public NativeArray<ShadowDrawData> ShadowBuffer;
	[ReadOnly] public float GlobalYaw;
	[ReadOnly] public int Mask;

	public void Execute(int index) {
		var entity = EntityArray[index];
		var renderer = RendererLookup[entity];
		var data = DrawShadowJob.GetShadowData(ShadowHashMap, new ShadowHash() {
			Sprite    = renderer.Sprite,
			Motion    = renderer.Motion,
			Direction = default,
			ObjectYaw = renderer.ObjectYaw,
			Time      = renderer.Time,
		}, false);
		if ((renderer.MainCullingMask & Mask) == 0) data.scale = default;
		ShadowBuffer[index] = new ShadowDrawData() {
			Position  = TransformLookup[entity].Position,
			Rotation  = quaternion.Euler(new float3(90f, GlobalYaw, 0f) * math.TORADIANS).value,
			Scale     = data.scale,
			Pivot     = data.pivot,
			Tiling    = data.tiling,
			Offset    = data.offset,
			Center    = new float3(0f, 0.1f, 0f),
		};
	}
}

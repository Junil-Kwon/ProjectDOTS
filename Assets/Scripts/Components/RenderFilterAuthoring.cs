using UnityEngine;
using UnityEngine.Rendering;

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
// Render Filter Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Render Filter")]
[RequireComponent(typeof(SpritePropertyAuthoring))]
public sealed class RenderFilterAuthoring : MonoComponent<RenderFilterAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(RenderFilterAuthoring))]
	class RenderFilterAuthoringEditor : EditorExtensions {
		RenderFilterAuthoring I => target as RenderFilterAuthoring;
		public override void OnInspectorGUI() {
			Begin();

			if (I.gameObject.layer != RenderArea.MainLayer) {
				I.gameObject.layer = RenderArea.MainLayer;
			}

			End();
		}
	}
	#endif



	// Baker

	class Baker : Baker<RenderFilterAuthoring> {
		public override void Bake(RenderFilterAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.Dynamic);
			AddComponent(entity, new RenderFilter {

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Render Filter
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct RenderFilter : IComponentData {

	// Fields

	public bool Initialized;
	public int MainCullingMask;
	public int TempCullingMask;
	public float Transition;

	public float MainAlpha;
	public float TempAlpha;



	// Properties

	public float this[int layer] => layer switch {
		RenderArea.MainLayer => MainAlpha,
		RenderArea.TempLayer => TempAlpha,
		_ => default,
	};
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Render Filter Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSClientSimulationSystemGroup), OrderLast = true)]
partial struct RenderFilterSimulationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<PhysicsWorldSingleton>();
		state.RequireForUpdate<RenderAreaPresentationSystem.Singleton>();
		state.RequireForUpdate<RenderFilter>();
	}

	public void OnUpdate(ref SystemState state) {
		var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
		var renderzoneSystem = SystemAPI.GetSingleton<RenderAreaPresentationSystem.Singleton>();
		state.Dependency = new RenderFilterSimulationJob {
			PhysicsWorld     = physicsWorld,
			RenderAreaLookup = SystemAPI.GetComponentLookup<RenderArea>(true),
			MainRenderArea   = renderzoneSystem.MainRenderArea[0],
			TempRenderArea   = renderzoneSystem.TempRenderArea[0],
			Transition       = renderzoneSystem.Transition[0],
			DeltaTime        = SystemAPI.Time.DeltaTime,
		}.ScheduleParallel(state.Dependency);
	}
}

[BurstCompile, WithAll(typeof(Simulate))]
partial struct RenderFilterSimulationJob : IJobEntity {
	[ReadOnly] public PhysicsWorld PhysicsWorld;
	[ReadOnly] public ComponentLookup<RenderArea> RenderAreaLookup;
	[ReadOnly] public RenderArea MainRenderArea;
	[ReadOnly] public RenderArea TempRenderArea;
	[ReadOnly] public float Transition;
	[ReadOnly] public float DeltaTime;

	public void Execute(
		in LocalTransform transform,
		ref RenderFilter filter,
		ref SpritePropertyBaseColor baseColor) {

		var renderArea = new RenderArea {
			CullingMask = 1 << 0,
		};
		if (PhysicsWorld.CalculateDistance(new PointDistanceInput {
			Position     = transform.Position + new float3(0f, 0.1f, 0f),
			MaxDistance  = 0f,
			Filter       = new CollisionFilter {
			BelongsTo    = uint.MaxValue,
			CollidesWith = 1u << (int)PhysicsCategory.RenderArea,
			}, }, out var hit)) {
			renderArea = RenderAreaLookup[hit.Entity];
		}
		if (filter.Transition < 1f) {
			if (filter.MainCullingMask != renderArea.CullingMask) {
				bool match = false;
				match = match || filter.Transition == 0f;
				match = match || filter.TempCullingMask != renderArea.CullingMask;
				if (match) filter.Transition = 0.001f;
				else filter.Transition = 1f - filter.Transition;
				filter.TempCullingMask = filter.MainCullingMask;
				filter.MainCullingMask = renderArea.CullingMask;
			}
		}
		if (0f < filter.Transition && filter.Transition < 1f) {
			float delta = DeltaTime / RenderArea.TransitionTime;
			filter.Transition = math.min(filter.Transition + delta, 1f);
		} else if (filter.Transition == 1f) {
			filter.Transition = 0f;
			filter.TempCullingMask = filter.MainCullingMask;
		}

		if (filter.Initialized != true) {
			filter.Initialized = true;
			filter.MainCullingMask = renderArea.CullingMask;
			filter.TempCullingMask = renderArea.CullingMask;
			filter.Transition = 0f;
		}
		bool mainmain = (filter.MainCullingMask & MainRenderArea.CullingMask) != 0;
		bool maintemp = (filter.MainCullingMask & TempRenderArea.CullingMask) != 0;
		bool tempmain = (filter.TempCullingMask & MainRenderArea.CullingMask) != 0;
		bool temptemp = (filter.TempCullingMask & TempRenderArea.CullingMask) != 0;
		if (filter.Transition == 0f) {
			filter.MainAlpha = mainmain ? 1f : 0f;
			filter.TempAlpha = maintemp ? 1f : 0f;
		} else if (filter.Transition < 1f) {
			float main = 1f - filter.Transition;
			float temp = 0f + filter.Transition;
			filter.MainAlpha = 1f - ((mainmain ? main : 0f) + (tempmain ? temp : 0f));
			filter.TempAlpha = 1f - ((maintemp ? main : 0f) + (temptemp ? temp : 0f));
		} else {
			filter.MainAlpha = tempmain ? 0f : 1f;
			filter.TempAlpha = temptemp ? 0f : 1f;
		}
		if (baseColor.Value.w != filter.MainAlpha) {
			baseColor.Value.w = filter.MainAlpha;
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Render Filter Presentation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(DOTSPresentationSystemGroup))]
[UpdateBefore(typeof(RenderAreaPresentationSystem))]
public partial class RenderFilterPresentationSystem : SystemBase {
	IndirectRenderer<SpriteDrawData> SpriteRenderer;
	EntityQuery ParticleQuery;

	protected override void OnCreate() {
		RequireForUpdate<RenderAreaPresentationSystem.Singleton>();
		SpriteRenderer = new(DrawManager.QuadMesh, DrawManager.SpriteMaterial);
		SpriteRenderer.Param.shadowCastingMode = ShadowCastingMode.Off;
		SpriteRenderer.Param.layer = RenderArea.TempLayer;
		ParticleQuery = GetEntityQuery(
			ComponentType.ReadOnly<LocalToWorld>(),
			ComponentType.ReadOnly<RenderFilter>(),
			ComponentType.ReadOnly<SpritePropertyCenter>(),
			ComponentType.ReadOnly<SpritePropertyBaseColor>(),
			ComponentType.ReadOnly<SpritePropertyMaskColor>(),
			ComponentType.ReadOnly<SpritePropertyEmission>(),
			ComponentType.ReadOnly<SpriteHash>());
		RequireForUpdate(ParticleQuery);
	}

	protected override void OnUpdate() {
		var renderAreaSystem = SystemAPI.GetSingleton<RenderAreaPresentationSystem.Singleton>();
		if (renderAreaSystem.Transition[0] == 0f) return;
		var renderArea = renderAreaSystem.TempRenderArea[0];
		EnvironmentManager.LightMode = renderArea.LightMode;
		var entityArray = ParticleQuery.ToEntityArray(Allocator.TempJob);
		int count = entityArray.Length;

		var spriteBuffer = SpriteRenderer.LockBuffer(count);
		new ParticleSpritePresentationJob {
			SpriteBuffer    = spriteBuffer,
			EntityArray     = entityArray,
			SpriteHashMap   = DrawManager.SpriteHashMapReadOnly,
			TransformLookup = GetComponentLookup<LocalToWorld>(true),
			FilterLookup    = GetComponentLookup<RenderFilter>(true),
			CenterLookup    = GetComponentLookup<SpritePropertyCenter>(true),
			BaseColorLookup = GetComponentLookup<SpritePropertyBaseColor>(true),
			MaskColorLookup = GetComponentLookup<SpritePropertyMaskColor>(true),
			EmissionLookup  = GetComponentLookup<SpritePropertyEmission>(true),
			HashLookup      = GetComponentLookup<SpriteHash>(true),
			GlobalYaw       = CameraManager.Yaw,
			Layer           = SpriteRenderer.Param.layer,
		}.Schedule(entityArray.Length, 64, Dependency).Complete();
		SpriteRenderer.UnlockBuffer(count);
		SpriteRenderer.Draw();
		SpriteRenderer.Clear();

		entityArray.Dispose();
	}

	protected override void OnDestroy() {
		SpriteRenderer.Dispose();
	}
}

[BurstCompile]
partial struct ParticleSpritePresentationJob : IJobParallelFor {
	[NativeDisableParallelForRestriction] public NativeArray<SpriteDrawData> SpriteBuffer;
	[ReadOnly] public NativeArray<Entity> EntityArray;
	[ReadOnly] public NativeHashMap<uint, AtlasData>.ReadOnly SpriteHashMap;
	[ReadOnly] public ComponentLookup<LocalToWorld> TransformLookup;
	[ReadOnly] public ComponentLookup<RenderFilter> FilterLookup;
	[ReadOnly] public ComponentLookup<SpritePropertyCenter> CenterLookup;
	[ReadOnly] public ComponentLookup<SpritePropertyBaseColor> BaseColorLookup;
	[ReadOnly] public ComponentLookup<SpritePropertyMaskColor> MaskColorLookup;
	[ReadOnly] public ComponentLookup<SpritePropertyEmission> EmissionLookup;
	[ReadOnly] public ComponentLookup<SpriteHash> HashLookup;
	[ReadOnly] public float GlobalYaw;
	[ReadOnly] public int Layer;

	public void Execute(int index) {
		var entity = EntityArray[index];
		var renderer = FilterLookup[entity];
		var hash = HashLookup[entity];
		var data = DrawSpriteJob.GetSpriteData(SpriteHashMap, new SpriteHash() {
			Sprite    = hash.Sprite,
			Motion    = hash.Motion,
			Direction = default,
			ObjectYaw = hash.ObjectYaw,
			Time      = hash.Time,
		}, true);
		var color = BaseColorLookup[entity].Value;
		SpriteBuffer[index] = new SpriteDrawData() {
			Position  = TransformLookup[entity].Position,
			Rotation  = quaternion.Euler(new float3(0f, GlobalYaw, 0f) * math.TORADIANS).value,
			Scale     = data.scale,
			Pivot     = data.pivot,
			Tiling    = data.tiling,
			Offset    = data.offset,
			Center    = CenterLookup[entity].Value,
			BaseColor = new(color.x, color.y, color.z, renderer[Layer]),
			MaskColor = MaskColorLookup[entity].Value,
			Emission  = EmissionLookup[entity].Value,
			Billboard = 1f,
		};
	}
}

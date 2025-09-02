using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Physics;
using Unity.Physics.Authoring;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Render Area Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Render Area")]
[RequireComponent(typeof(PhysicsShapeAuthoring))]
public class RenderAreaAuthoring : MonoComponent<RenderAreaAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(RenderAreaAuthoring))]
	class RenderAreaAuthoringEditor : EditorExtensions {
		RenderAreaAuthoring I => target as RenderAreaAuthoring;
		public override void OnInspectorGUI() {
			Begin();

			I.CullingMask = LayerField("Culling Mask", I.CullingMask);
			I.LightMode = EnumField("Light Mode", I.LightMode);

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] int m_CullingMask;
	[SerializeField] LightMode m_LightMode;



	// Properties

	int CullingMask {
		get => m_CullingMask;
		set => m_CullingMask = value;
	}
	LightMode LightMode {
		get => m_LightMode;
		set => m_LightMode = value;
	}



	// Baker

	class Baker : Baker<RenderAreaAuthoring> {
		public override void Bake(RenderAreaAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.Dynamic);
			AddComponent(entity, new RenderArea {

				CullingMask = authoring.CullingMask,
				LightMode   = authoring.LightMode,

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Render Area
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct RenderArea : IComponentData {

	// Constants

	public const int MainLayer = 3;
	public const int TempLayer = 1;
	public const float TransitionTime = 0.5f;



	// Fields

	public int CullingMask;
	public LightMode LightMode;



	// Operators

	public static bool operator ==(RenderArea left, RenderArea right) {
		return left.CullingMask == right.CullingMask && left.LightMode == right.LightMode;
	}

	public static bool operator !=(RenderArea left, RenderArea right) {
		return !(left == right);
	}

	public override bool Equals(object obj) {
		return (obj is RenderArea other) && this == other;
	}

	public override int GetHashCode() {
		return CullingMask.GetHashCode() ^ LightMode.GetHashCode();
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Render Area Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(DOTSClientSimulationSystemGroup), OrderFirst = true)]
public partial class RenderAreaSimulationSystem : SystemBase {

	protected override void OnCreate() {
		RequireForUpdate<PhysicsWorldSingleton>();
		RequireForUpdate<RenderAreaPresentationSystem.Singleton>();
	}

	protected override void OnUpdate() {
		var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
		var renderAreaSystem = SystemAPI.GetSingleton<RenderAreaPresentationSystem.Singleton>();

		var renderArea = new RenderArea {
			CullingMask = 1 << 0,
			LightMode   = LightMode.DayNightCycle,
		};
		if (physicsWorld.CalculateDistance(new PointDistanceInput {
			Position     = CameraManager.Position + new Vector3(0f, 0.1f, 0f),
			MaxDistance  = 0f,
			Filter       = new CollisionFilter {
			BelongsTo    = uint.MaxValue,
			CollidesWith = 1u << (int)PhysicsCategory.RenderArea,
			}, }, out var hit)) {
			renderArea = SystemAPI.GetComponent<RenderArea>(hit.Entity);
		}
		if (renderAreaSystem.Transition[0] < 1f) {
			if (renderAreaSystem.MainRenderArea[0] != renderArea) {
				bool match = false;
				match = match || renderAreaSystem.Transition[0] == 0f;
				match = match || renderAreaSystem.TempRenderArea[0] != renderArea;
				if (match) renderAreaSystem.Transition[0] = 0.001f;
				else renderAreaSystem.Transition[0] = 1f - renderAreaSystem.Transition[0];
				renderAreaSystem.TempRenderArea[0] = renderAreaSystem.MainRenderArea[0];
				renderAreaSystem.MainRenderArea[0] = renderArea;
			}
		}
		if (0f < renderAreaSystem.Transition[0] && renderAreaSystem.Transition[0] < 1f) {
			float delta = SystemAPI.Time.DeltaTime / RenderArea.TransitionTime;
			renderAreaSystem.Transition[0] = math.min(renderAreaSystem.Transition[0] + delta, 1f);
		} else if (renderAreaSystem.Transition[0] == 1f) {
			renderAreaSystem.Transition[0] = 0f;
			renderAreaSystem.TempRenderArea[0] = renderAreaSystem.MainRenderArea[0];
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Render Area Presentation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(DOTSPresentationSystemGroup))]
public partial class RenderAreaPresentationSystem : SystemBase {
	NativeArray<RenderArea> MainRenderArea;
	NativeArray<RenderArea> TempRenderArea;
	NativeArray<float> Transition;

	public struct Singleton : IComponentData {
		public NativeArray<RenderArea> MainRenderArea;
		public NativeArray<RenderArea> TempRenderArea;
		public NativeArray<float> Transition;
	}

	protected override void OnCreate() {
		EntityManager.CreateEntity(ComponentType.ReadOnly<Singleton>());
		SystemAPI.SetSingleton(new Singleton {
			MainRenderArea = MainRenderArea = new(1, Allocator.Persistent),
			TempRenderArea = TempRenderArea = new(1, Allocator.Persistent),
			Transition = Transition = new(1, Allocator.Persistent),
		});
	}

	protected override void OnUpdate() {
		if (0f < Transition[0]) {
			EnvironmentManager.LightMode = TempRenderArea[0].LightMode;
			foreach (var world in World.All) {
				if (world.IsCreated) world.EntityManager.CompleteAllTrackedJobs();
			}
			int mainCullingMask = MainRenderArea[0].CullingMask | (1 << RenderArea.MainLayer);
			int tempCullingMask = TempRenderArea[0].CullingMask | (1 << RenderArea.TempLayer);
			CameraManager.SetTransition(tempCullingMask, 1f - Transition[0]);
			CameraManager.CullingMask = mainCullingMask;
		}
		EnvironmentManager.LightMode = MainRenderArea[0].LightMode;
	}

	protected override void OnDestroy() {
		MainRenderArea.Dispose();
		TempRenderArea.Dispose();
		Transition.Dispose();
	}
}

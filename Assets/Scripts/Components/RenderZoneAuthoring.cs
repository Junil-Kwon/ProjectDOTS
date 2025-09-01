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
// Render Zone Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Render Zone")]
[RequireComponent(typeof(PhysicsShapeAuthoring))]
public class RenderZoneAuthoring : MonoComponent<RenderZoneAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(RenderZoneAuthoring))]
	class RenderZoneAuthoringEditor : EditorExtensions {
		RenderZoneAuthoring I => target as RenderZoneAuthoring;
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

	class Baker : Baker<RenderZoneAuthoring> {
		public override void Bake(RenderZoneAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.Dynamic);
			AddComponent(entity, new RenderZone {

				CullingMask = authoring.CullingMask,
				LightMode   = authoring.LightMode,

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Render Zone
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct RenderZone : IComponentData {

	// Constants

	public const float TransitionTime = 0.5f;



	// Fields

	public int CullingMask;
	public LightMode LightMode;



	// Operators

	public static bool operator ==(RenderZone left, RenderZone right) {
		return left.CullingMask == right.CullingMask && left.LightMode == right.LightMode;
	}

	public static bool operator !=(RenderZone left, RenderZone right) {
		return !(left == right);
	}

	public override bool Equals(object obj) {
		return (obj is RenderZone other) && this == other;
	}

	public override int GetHashCode() {
		return CullingMask.GetHashCode() ^ LightMode.GetHashCode();
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Render Zone Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(DOTSClientSimulationSystemGroup), OrderFirst = true)]
public partial class RenderZoneSimulationSystem : SystemBase {

	protected override void OnCreate() {
		RequireForUpdate<PhysicsWorldSingleton>();
		RequireForUpdate<RenderZonePresentationSystem.Singleton>();
	}

	protected override void OnUpdate() {
		var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
		var singleton = SystemAPI.GetSingleton<RenderZonePresentationSystem.Singleton>();

		var renderZone = new RenderZone {
			CullingMask = 1 << 0,
			LightMode   = LightMode.DayNightCycle,
		};
		if (physicsWorld.CalculateDistance(new PointDistanceInput {
			Position     = CameraManager.Position + new Vector3(0f, 0.1f, 0f),
			MaxDistance  = 0f,
			Filter       = new CollisionFilter {
			BelongsTo    = uint.MaxValue,
			CollidesWith = 1u << (int)PhysicsCategory.RenderZone,
			}, }, out var hit)) {
			renderZone = SystemAPI.GetComponent<RenderZone>(hit.Entity);
		}
		if (singleton.MainRenderZone[0] != renderZone) {
			singleton.TempRenderZone[0] = singleton.MainRenderZone[0];
			singleton.MainRenderZone[0] = renderZone;
			bool match = (0f < singleton.Transition[0]) && (singleton.Transition[0] < 1f);
			singleton.Transition[0] = match ? (1f - singleton.Transition[0]) : float.Epsilon;
		}
		if (0f < singleton.Transition[0] && singleton.Transition[0] < 1f) {
			float delta = SystemAPI.Time.DeltaTime / RenderZone.TransitionTime;
			singleton.Transition[0] = math.min(singleton.Transition[0] + delta, 1f);
		} else if (singleton.Transition[0] == 1f) {
			singleton.Transition[0] = 0f;
			singleton.TempRenderZone[0] = singleton.MainRenderZone[0];
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Render Zone Presentation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(DOTSPresentationSystemGroup), OrderLast = true)]
public partial class RenderZonePresentationSystem : SystemBase {
	NativeArray<RenderZone> MainRenderZone;
	NativeArray<RenderZone> TempRenderZone;
	NativeArray<float> Transition;

	public struct Singleton : IComponentData {
		public NativeArray<RenderZone> MainRenderZone;
		public NativeArray<RenderZone> TempRenderZone;
		public NativeArray<float> Transition;
	}

	protected override void OnCreate() {
		EntityManager.CreateEntity(ComponentType.ReadOnly<Singleton>());
		SystemAPI.SetSingleton(new Singleton {
			MainRenderZone = MainRenderZone = new(1, Allocator.Persistent),
			TempRenderZone = TempRenderZone = new(1, Allocator.Persistent),
			Transition = Transition = new(1, Allocator.Persistent),
		});
	}

	protected override void OnUpdate() {
		if (0f < Transition[0]) {
			EnvironmentManager.LightMode = TempRenderZone[0].LightMode;
			foreach (var world in World.All) {
				if (world.IsCreated) world.EntityManager.CompleteAllTrackedJobs();
			}
			int mainCullingMask = MainRenderZone[0].CullingMask | (1 << 1);
			int tempCullingMask = TempRenderZone[0].CullingMask | (1 << 2);
			CameraManager.SetTransition(tempCullingMask, 1f - Transition[0]);
			CameraManager.CullingMask = mainCullingMask;
		}
		EnvironmentManager.LightMode = MainRenderZone[0].LightMode;
	}

	protected override void OnDestroy() {
		MainRenderZone.Dispose();
		TempRenderZone.Dispose();
		Transition.Dispose();
	}
}

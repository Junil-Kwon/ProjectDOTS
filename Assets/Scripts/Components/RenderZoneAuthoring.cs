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

	public const float TransitionTime = 0.4f;



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
		var mainRenderZone = singleton.MainRenderZone[0];
		var tempRenderZone = singleton.TempRenderZone[0];
		var transition = singleton.Transition[0];

		var renderZone = new RenderZone {
			CullingMask = 1 << 0,
			LightMode   = LightMode.TimeOfDay,
		};
		if (physicsWorld.CalculateDistance(new PointDistanceInput {
			Position     = CameraManager.Position,
			MaxDistance  = 0f,
			Filter       = new CollisionFilter {
			BelongsTo    = uint.MaxValue,
			CollidesWith = 1u << (int)PhysicsCategory.RenderZone,
			}, }, out var hit)) {
			renderZone = SystemAPI.GetComponent<RenderZone>(hit.Entity);
		}
		if (mainRenderZone != renderZone) {
			tempRenderZone = mainRenderZone;
			mainRenderZone = renderZone;
			CameraManager.CullingMask = renderZone.CullingMask;
			transition = (0f < transition) ? (1f - transition) : float.Epsilon;
		}
		if (0f < transition) {
			float delta = SystemAPI.Time.DeltaTime / RenderZone.TransitionTime;
			transition = math.min(transition + delta, 1f);
			if (transition == 1f) {
				transition = 0f;
				tempRenderZone = mainRenderZone;
			}
		}
		singleton.MainRenderZone[0] = mainRenderZone;
		singleton.TempRenderZone[0] = tempRenderZone;
		singleton.Transition[0] = transition;
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
		var mainRenderZone = MainRenderZone[0];
		var tempRenderZone = TempRenderZone[0];
		var transition = Transition[0];

		EnvironmentManager.LightMode = tempRenderZone.LightMode;
		foreach (var world in World.All) {
			if (world.IsCreated) world.EntityManager.CompleteAllTrackedJobs();
		}
		CameraManager.SetTransition(tempRenderZone.CullingMask, 1f - transition);
		CameraManager.CullingMask = mainRenderZone.CullingMask;
		EnvironmentManager.LightMode = mainRenderZone.LightMode;
	}

	public static bool TryGetData(
		in Singleton singleton, int index, out RenderZone renderZone, out int layer) {
		var mainRenderZone = singleton.MainRenderZone[0];
		var tempRenderZone = singleton.TempRenderZone[0];
		switch (index) {
			case 0: for (int i = 0; i < 32; i++) {
				bool match = true;
				match = match && ((mainRenderZone.CullingMask & (1 << i)) != 0);
				if (match) {
					renderZone = mainRenderZone;
					layer = i;
					return true;
				}
			} break;
			case 1: for (int i = 0; i < 32; i++) {
				bool match = true;
				match = match && ((mainRenderZone.CullingMask & (1 << i)) == 0);
				match = match && ((tempRenderZone.CullingMask & (1 << i)) != 0);
				if (match) {
					renderZone = tempRenderZone;
					layer = i;
					return true;
				}
			} break;
		}
		renderZone = default;
		layer = -1;
		return false;
	}

	protected override void OnDestroy() {
		MainRenderZone.Dispose();
		TempRenderZone.Dispose();
		Transition.Dispose();
	}
}

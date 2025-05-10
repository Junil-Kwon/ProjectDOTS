using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Physics;
using Unity.NetCode;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Local Event Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Local Event")]
public class LocalEventAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(LocalEventAuthoring))]
		class LocalEventAuthoringEditor : EditorExtensions {
			LocalEventAuthoring I => target as LocalEventAuthoring;
			public override void OnInspectorGUI() {
				Begin("Local Event Authoring");

				LabelField("Event", EditorStyles.boldLabel);
				I.Event = ObjectField("Event Graph", I.Event);
				if (Button("Open Event Graph")) {
					if (!I.Event) I.Event = CreateInstance<EventGraphSO>();
					I.Event.name = I.gameObject.name;
					I.Event.Open();
				}
				Space();

				if (I.Event != null) {
					LabelField("Trigger", EditorStyles.boldLabel);
					if (!Toggle("Use Iteration Limit", I.Count != -1)) I.Count = -1;
					else {
						IntentLevel++;
						I.Count = Mathf.Max(0, IntField("Count", I.Count));
						IntentLevel--;
					}
					if (!Toggle("Use Time Limit", I.Timer != -1f)) I.Timer = -1f;
					else {
						IntentLevel++;
						I.Timer = Mathf.Max(0f, FloatField("Timer", I.Timer));
						IntentLevel--;
					}
					Space();
				}

				End();
			}

			void OnSceneGUI() {
				if (I.Event == null) return;
				if (I.Event.Clone != null) {
					Tools.current = Tool.None;
					foreach (var data in I.Event.Clone.GetEvents()) data.DrawHandles();
				}
			}
		}

		void OnDrawGizmosSelected() {
			if (Event == null) return;
			Gizmos.color = color.green;
			foreach (var data in Event.Entry.GetEvents()) data.DrawGizmos();
			
			if (Event.Clone == null) return;
			Gizmos.color = color.white;
			foreach (var data in Event.Clone.GetEvents()) data.DrawGizmos();
		}
	#endif



	// Fields

	[SerializeField] EventGraphSO m_Event;
	[SerializeField] int   m_Count = -1;
	[SerializeField] float m_Timer = -1f;



	// Properties

	public EventGraphSO Event {
		get => m_Event;
		set => m_Event = value;
	}
	public int Count {
		get => m_Count;
		set => m_Count = value;
	}
	public float Timer {
		get => m_Timer;
		set => m_Timer = value;
	}



	// Baker
	
	class Baker : Baker<LocalEventAuthoring> {
		public override void Bake(LocalEventAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new LocalEvent {

				Event = authoring.Event,
				Count = authoring.Count,
				Timer = authoring.Timer,

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Local Event
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct LocalEvent : IComponentData {

	public UnityObjectRef<EventGraphSO> Event;
	public int   Count;
	public float Timer;

	public float Cooldown;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Local Event System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct LocalEventSystem : ISystem {

	[BurstCompile]
	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<LocalEvent>();
		state.RequireForUpdate<SimulationSingleton>();
		state.RequireForUpdate<GameManagerBridge>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state) {
		float deltaTime = SystemAPI.Time.DeltaTime;
		foreach (var trigger in SystemAPI.Query<RefRW<LocalEvent>>()) {
			trigger.ValueRW.Cooldown = math.max(0f, trigger.ValueRO.Cooldown - deltaTime);
		}
		var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
		var triggerEventJob = new LocalEventJob {
			gameManager = SystemAPI.GetSingletonRW<GameManagerBridge>(),
			ghostOwner  = SystemAPI.GetComponentLookup<GhostOwnerIsLocal>(true),
			localEvent  = SystemAPI.GetComponentLookup<LocalEvent>(),
		};
		state.Dependency = triggerEventJob.Schedule(simulationSingleton, state.Dependency);
	}

	[BurstCompile]
	partial struct LocalEventJob : ITriggerEventsJob {
		[NativeDisableUnsafePtrRestriction] public RefRW<GameManagerBridge> gameManager;
		[ReadOnly] public ComponentLookup<GhostOwnerIsLocal> ghostOwner;
		public ComponentLookup<LocalEvent> localEvent;

		public void Execute(TriggerEvent triggerEvent) {
			Execute(triggerEvent.EntityA, triggerEvent.EntityB);
			Execute(triggerEvent.EntityB, triggerEvent.EntityA);
		}

		public void Execute(Entity entity, Entity target) {
			if (ghostOwner.HasComponent(entity) && localEvent.HasComponent(target)) {
				if (localEvent[target].Count != 0 && localEvent[target].Cooldown == 0f) {
					gameManager.ValueRW.PlayEvent(localEvent[target].Event);

					var temp = localEvent[target];
					if (0 < localEvent[target].Count) temp.Count--;
					temp.Cooldown = math.max(float.Epsilon, temp.Timer);
					localEvent[target] = temp;
				}
			}
		}
	}
}

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
// Event Trigger Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Event Trigger")]
public class EventTriggerAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(EventTriggerAuthoring))]
		class EventTriggerAuthoringEditor : EditorExtensions {
			EventTriggerAuthoring I => target as EventTriggerAuthoring;
			public override void OnInspectorGUI() {
				Begin("Event Trigger Authoring");

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
					I.IsGlobal = Toggle("Is Global", I.IsGlobal);
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
	[SerializeField] bool  m_IsGlobal = false;
	[SerializeField] int   m_Count = -1;
	[SerializeField] float m_Timer = -1f;



	// Properties

	public EventGraphSO Event {
		get => m_Event;
		set => m_Event = value;
	}
	public bool IsGlobal {
		get => m_IsGlobal;
		set => m_IsGlobal = value;
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
	
	class Baker : Baker<EventTriggerAuthoring> {
		public override void Bake(EventTriggerAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new EventTrigger {

				Event = authoring.Event,
				Count = authoring.Count,
				Timer = authoring.Timer,

			});
			if (authoring.IsGlobal) AddComponent(entity, new ServerEvent());
			else                    AddComponent(entity, new ClientEvent());
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Event Trigger
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct EventTrigger : IComponentData {

	public UnityObjectRef<EventGraphSO> Event;
	public int   Count;
	public float Timer;
	public float Cooldown;
}

public struct ServerEvent : IComponentData { }
public struct ClientEvent : IComponentData { }



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Event Trigger Server System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(DOTSSimulationSystemGroup))]
partial struct GlobalEventSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<SimulationSingleton>();
		state.RequireForUpdate<GameManagerBridge>();
	}

	public void OnUpdate(ref SystemState state) {
		state.Dependency = new ServerEventSimulationJob {
			DeltaTime = SystemAPI.Time.DeltaTime,
		}.ScheduleParallel(state.Dependency);
		state.Dependency = new TriggerServerEventJob {
			GameManager       = SystemAPI.GetSingletonRW<GameManagerBridge>(),
			TriggerGroup      = SystemAPI.GetComponentLookup<EventTrigger>(),
			ServerGroup       = SystemAPI.GetComponentLookup<ServerEvent>(),
			InteractableGroup = SystemAPI.GetComponentLookup<Interactable>(true),
			LocalGhostGroup   = SystemAPI.GetComponentLookup<GhostOwnerIsLocal>(true),
		}.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
	}

	[BurstCompile, WithAll(typeof(ServerEvent), typeof(Simulate))]
	partial struct ServerEventSimulationJob : IJobEntity {
		public float DeltaTime;
		public void Execute(ref EventTrigger trigger) {
			trigger.Cooldown = math.max(0f, trigger.Cooldown - DeltaTime);
		}
	}

	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct TriggerServerEventJob : ITriggerEventsJob {
		[NativeDisableUnsafePtrRestriction] public RefRW<GameManagerBridge> GameManager;
		public ComponentLookup<EventTrigger> TriggerGroup;
		public ComponentLookup<ServerEvent> ServerGroup;
		[ReadOnly] public ComponentLookup<Interactable> InteractableGroup;
		[ReadOnly] public ComponentLookup<GhostOwnerIsLocal> LocalGhostGroup;

		public void Execute(TriggerEvent triggerEvent) {
			Execute(triggerEvent.EntityA, triggerEvent.EntityB);
			Execute(triggerEvent.EntityB, triggerEvent.EntityA);
		}

		public void Execute(Entity entity, Entity target) {
			if (ServerGroup.HasComponent(entity) && !InteractableGroup.HasComponent(entity)) {
					if (LocalGhostGroup.HasComponent(target)) {
					if (TriggerGroup[entity].Count != 0 && TriggerGroup[entity].Cooldown == 0f) {
						GameManager.ValueRW.PlayEvent(TriggerGroup[entity].Event);

						var temp = TriggerGroup[entity];
						if (0 < temp.Count) temp.Count--;
						temp.Cooldown = math.max(float.Epsilon, temp.Timer);
						TriggerGroup[entity] = temp;
					}
				}
			}
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Event Trigger Client System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(DOTSSimulationSystemGroup))]
partial struct LocalEventSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<SimulationSingleton>();
		state.RequireForUpdate<GameManagerBridge>();
	}

	public void OnUpdate(ref SystemState state) {
		state.Dependency = new ClientEventSimulationJob {
			DeltaTime = SystemAPI.Time.DeltaTime,
		}.ScheduleParallel(state.Dependency);
		state.Dependency = new TriggerClientEventJob {
			GameManager       = SystemAPI.GetSingletonRW<GameManagerBridge>(),
			TriggerGroup      = SystemAPI.GetComponentLookup<EventTrigger>(),
			ClientGroup       = SystemAPI.GetComponentLookup<ClientEvent>(),
			InteractableGroup = SystemAPI.GetComponentLookup<Interactable>(true),
			LocalGhostGroup   = SystemAPI.GetComponentLookup<GhostOwnerIsLocal>(true),
		}.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
	}

	[BurstCompile, WithAll(typeof(ClientEvent), typeof(Simulate))]
	partial struct ClientEventSimulationJob : IJobEntity {
		public float DeltaTime;
		public void Execute(ref EventTrigger trigger) {
			trigger.Cooldown = math.max(0f, trigger.Cooldown - DeltaTime);
		}
	}

	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct TriggerClientEventJob : ITriggerEventsJob {
		[NativeDisableUnsafePtrRestriction] public RefRW<GameManagerBridge> GameManager;
		public ComponentLookup<EventTrigger> TriggerGroup;
		public ComponentLookup<ClientEvent> ClientGroup;
		[ReadOnly] public ComponentLookup<Interactable> InteractableGroup;
		[ReadOnly] public ComponentLookup<GhostOwnerIsLocal> LocalGhostGroup;

		public void Execute(TriggerEvent triggerEvent) {
			Execute(triggerEvent.EntityA, triggerEvent.EntityB);
			Execute(triggerEvent.EntityB, triggerEvent.EntityA);
		}

		public void Execute(Entity entity, Entity target) {
			if (ClientGroup.HasComponent(entity) && !InteractableGroup.HasComponent(entity)) {
					if (LocalGhostGroup.HasComponent(target)) {
					if (TriggerGroup[entity].Count != 0 && TriggerGroup[entity].Cooldown == 0f) {
						GameManager.ValueRW.PlayEvent(TriggerGroup[entity].Event);

						var temp = TriggerGroup[entity];
						if (0 < temp.Count) temp.Count--;
						temp.Cooldown = math.max(float.Epsilon, temp.Timer);
						TriggerGroup[entity] = temp;
					}
				}
			}
		}
	}
}

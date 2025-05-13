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

	// Fields

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
partial struct GlobalEventSystem : ISystem {

	[BurstCompile]
	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<SimulationSingleton>();
		state.RequireForUpdate<GameManagerBridge>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state) {
		state.Dependency = new ServerEventSimulationJob {
			deltaTime = SystemAPI.Time.DeltaTime,
		}.ScheduleParallel(state.Dependency);
		state.Dependency = new TriggerServerEventJob {
			gameManager  = SystemAPI.GetSingletonRW<GameManagerBridge>(),
			trigger      = SystemAPI.GetComponentLookup<EventTrigger>(),
			server       = SystemAPI.GetComponentLookup<ServerEvent>(),
			interactable = SystemAPI.GetComponentLookup<Interactable>(true),
			ghostOwner   = SystemAPI.GetComponentLookup<GhostOwnerIsLocal>(true),
		}.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
	}

	[BurstCompile, WithAll(typeof(ServerEvent), typeof(Simulate))]
	partial struct ServerEventSimulationJob : IJobEntity {
		public float deltaTime;
		public void Execute(ref EventTrigger trigger) {
			trigger.Cooldown = math.max(0f, trigger.Cooldown - deltaTime);
		}
	}

	[BurstCompile]
	partial struct TriggerServerEventJob : ITriggerEventsJob {
		[NativeDisableUnsafePtrRestriction] public RefRW<GameManagerBridge> gameManager;
		public ComponentLookup<EventTrigger> trigger;
		public ComponentLookup<ServerEvent> server;
		[ReadOnly] public ComponentLookup<Interactable> interactable;
		[ReadOnly] public ComponentLookup<GhostOwnerIsLocal> ghostOwner;

		public void Execute(TriggerEvent triggerEvent) {
			Execute(triggerEvent.EntityA, triggerEvent.EntityB);
			Execute(triggerEvent.EntityB, triggerEvent.EntityA);
		}
		public void Execute(Entity entity, Entity target) {
			if (server.HasComponent(entity) && !interactable.HasComponent(entity)) {
					if (ghostOwner.HasComponent(target)) {
					if (trigger[entity].Count != 0 && trigger[entity].Cooldown == 0f) {
						gameManager.ValueRW.PlayEvent(trigger[entity].Event);

						var temp = trigger[entity];
						if (0 < temp.Count) temp.Count--;
						temp.Cooldown = math.max(float.Epsilon, temp.Timer);
						trigger[entity] = temp;
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
partial struct LocalEventSystem : ISystem {

	[BurstCompile]
	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<SimulationSingleton>();
		state.RequireForUpdate<GameManagerBridge>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state) {
		state.Dependency = new ClientEventSimulationJob {
			deltaTime = SystemAPI.Time.DeltaTime,
		}.ScheduleParallel(state.Dependency);
		state.Dependency = new TriggerClientEventJob {
			gameManager  = SystemAPI.GetSingletonRW<GameManagerBridge>(),
			trigger      = SystemAPI.GetComponentLookup<EventTrigger>(),
			client       = SystemAPI.GetComponentLookup<ClientEvent>(),
			interactable = SystemAPI.GetComponentLookup<Interactable>(true),
			ghostOwner   = SystemAPI.GetComponentLookup<GhostOwnerIsLocal>(true),
		}.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
	}

	[BurstCompile, WithAll(typeof(ClientEvent), typeof(Simulate))]
	partial struct ClientEventSimulationJob : IJobEntity {
		public float deltaTime;
		public void Execute(ref EventTrigger trigger) {
			trigger.Cooldown = math.max(0f, trigger.Cooldown - deltaTime);
		}
	}

	[BurstCompile]
	partial struct TriggerClientEventJob : ITriggerEventsJob {
		[NativeDisableUnsafePtrRestriction] public RefRW<GameManagerBridge> gameManager;
		public ComponentLookup<EventTrigger> trigger;
		public ComponentLookup<ClientEvent> client;
		[ReadOnly] public ComponentLookup<Interactable> interactable;
		[ReadOnly] public ComponentLookup<GhostOwnerIsLocal> ghostOwner;

		public void Execute(TriggerEvent triggerEvent) {
			Execute(triggerEvent.EntityA, triggerEvent.EntityB);
			Execute(triggerEvent.EntityB, triggerEvent.EntityA);
		}
		public void Execute(Entity entity, Entity target) {
			if (client.HasComponent(entity) && !interactable.HasComponent(entity)) {
					if (ghostOwner.HasComponent(target)) {
					if (trigger[entity].Count != 0 && trigger[entity].Cooldown == 0f) {
						gameManager.ValueRW.PlayEvent(trigger[entity].Event);

						var temp = trigger[entity];
						if (0 < temp.Count) temp.Count--;
						temp.Cooldown = math.max(float.Epsilon, temp.Timer);
						trigger[entity] = temp;
					}
				}
			}
		}
	}
}

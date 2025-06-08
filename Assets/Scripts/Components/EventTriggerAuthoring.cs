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



// ━

public enum TriggerType : byte {
	InRange,
	OnInteract,
}



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
				if (I.Event == null && Button("Create Event Graph")) {
					I.Event = CreateInstance<EventGraphSO>();
					I.Event.name = I.gameObject.name;
				}
				if (I.Event != null && Button("Open Event Graph")) {
					I.Event.Open();
				}
				Space();
				LabelField("Trigger", EditorStyles.boldLabel);
				I.TriggerType   = EnumField("Trigger Type",    I.TriggerType);
				I.IsGlobal      = Toggle   ("Is Global",       I.IsGlobal);
				I.UseCountLimit = Toggle   ("Use Count Limit", I.UseCountLimit);
				I.UseCooldown   = Toggle   ("Use Cooldown",    I.UseCooldown);
				if (I.UseCountLimit) I.Count    = IntField  ("Count",    I.Count);
				if (I.UseCooldown  ) I.Cooldown = FloatField("Cooldown", I.Cooldown);
				if (I.TryGetComponent(out InteractableAuthoring _)) {
					I.TriggerType = TriggerType.OnInteract;
				}
				Space();

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
	[SerializeField] TriggerType  m_TriggerType;
	[SerializeField] bool  m_IsGlobal = false;
	[SerializeField] int   m_Count = -1;
	[SerializeField] float m_Cooldown = -1f;



	// Properties

	public EventGraphSO Event {
		get => m_Event;
		set => m_Event = value;
	}
	public TriggerType TriggerType {
		get => m_TriggerType;
		set {
			if (m_TriggerType != value) {
				m_TriggerType = value;
				bool hasComponent = gameObject.TryGetComponent(out InteractableAuthoring interactable);
				if (m_TriggerType == TriggerType.OnInteract) {
					if (!hasComponent) gameObject.AddComponent<InteractableAuthoring>();
				} else {
					if (hasComponent) DestroyImmediate(interactable);
				}
			}
		}
	}
	public bool IsGlobal {
		get => m_IsGlobal;
		set => m_IsGlobal = value;
	}
	public bool UseCountLimit {
		get => 0 <= m_Count;
		set => m_Count = value ? Mathf.Max(0, m_Count) : -1;
	}
	public bool UseCooldown {
		get => 0f <= m_Cooldown;
		set => m_Cooldown = value ? Mathf.Max(0f, m_Cooldown) : -1f;
	}
	public int Count {
		get => m_Count;
		set => m_Count = value;
	}
	public float Cooldown {
		get => m_Cooldown;
		set => m_Cooldown = value;
	}



	// Baker
	
	class Baker : Baker<EventTriggerAuthoring> {
		public override void Bake(EventTriggerAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new EventTrigger {

				Event    = authoring.Event,
				IsGlobal = authoring.IsGlobal,
				Count    = authoring.Count,
				Cooldown = authoring.Cooldown,

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Event Trigger
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct EventTrigger : IComponentData {

	public UnityObjectRef<EventGraphSO> Event;
	public bool  IsGlobal;
	public int   Count;
	public float Cooldown;
	public float Timer;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Event Trigger System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

/*
if is global enabled, then the event will be triggered on all clients
rpc this entity
*/

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
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
			GameManager        = SystemAPI.GetSingletonRW<GameManagerBridge>(),
			LocalGhostLookup   = SystemAPI.GetComponentLookup<GhostOwnerIsLocal>(true),
			InteractableLookup = SystemAPI.GetComponentLookup<Interactable>(true),
			TriggerLookup      = SystemAPI.GetComponentLookup<EventTrigger>(),
		}.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
	}

	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct ServerEventSimulationJob : IJobEntity {
		public float DeltaTime;
		public void Execute(ref EventTrigger trigger) {
			if (0f <= trigger.Cooldown) trigger.Timer = math.max(0f, trigger.Timer - DeltaTime);
		}
	}

	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct TriggerServerEventJob : ITriggerEventsJob {
		[NativeDisableUnsafePtrRestriction] public RefRW<GameManagerBridge> GameManager;
		[ReadOnly] public ComponentLookup<GhostOwnerIsLocal> LocalGhostLookup;
		[ReadOnly] public ComponentLookup<Interactable> InteractableLookup;
		public ComponentLookup<EventTrigger> TriggerLookup;

		public void Execute(TriggerEvent triggerEvent) {
			Execute(triggerEvent.EntityA, triggerEvent.EntityB);
			Execute(triggerEvent.EntityB, triggerEvent.EntityA);
		}

		public void Execute(Entity entity, Entity target) {
			if (LocalGhostLookup.HasComponent(entity) && LocalGhostLookup.IsComponentEnabled(entity)) {
				if (!InteractableLookup.HasComponent(target) && TriggerLookup.HasComponent(target)) {
					if (TriggerLookup[target].Count != 0 && TriggerLookup[target].Timer == 0f) {
						GameManager.ValueRW.PlayEvent(TriggerLookup[target].Event);

						var temp = TriggerLookup[target];
						if (0 < temp.Count) temp.Count--;
						if (0f <= temp.Cooldown) temp.Timer = math.max(float.Epsilon, temp.Cooldown);
						TriggerLookup[target] = temp;
					}
				}
			}
		}
	}
}

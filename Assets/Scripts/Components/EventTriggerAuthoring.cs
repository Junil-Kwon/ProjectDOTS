using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.NetCode;

#if UNITY_EDITOR
using UnityEditor;
#endif



// Event Trigger Types

public enum TriggerType : byte {
	InRange,
	OnInteract,
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Event Trigger Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Event Trigger")]
[RequireComponent(typeof(PhysicsShapeAuthoring))]
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
			I.TriggerType = EnumField("Trigger Type", I.TriggerType);
			var width = GUILayout.Width(18f);
			BeginHorizontal();
			PrefixLabel("Use Count Limit");
			I.UseCountLimit = EditorGUILayout.Toggle(I.UseCountLimit, width);
			if (I.UseCountLimit) I.Count = IntField(I.Count);
			EndHorizontal();
			BeginHorizontal();
			PrefixLabel("Use Cooldown");
			I.UseCooldown = EditorGUILayout.Toggle(I.UseCooldown, width);
			if (I.UseCooldown) I.Cooldown = FloatField(I.Cooldown);
			EndHorizontal();
			BeginHorizontal();
			PrefixLabel("Is Global");
			I.IsGlobal = EditorGUILayout.Toggle(I.IsGlobal, width);
			EndHorizontal();
			if (I.TryGetComponent(out InteractableAuthoring _)) {
				I.TriggerType = TriggerType.OnInteract;
			}
			Space();

			End();
		}

		void OnSceneGUI() {
			if (I.Event?.Clone != null) {
				Tools.current = Tool.None;
				foreach (var data in I.Event.Clone.GetEvents()) data.DrawHandles();
			}
		}
	}

	void OnDrawGizmosSelected() {
		if (Event != null) {
			Gizmos.color = color.green;
			foreach (var data in Event.Entry.GetEvents()) data.DrawGizmos();
		}
		if (Event?.Clone != null) {
			Gizmos.color = color.white;
			foreach (var data in Event.Clone.GetEvents()) data.DrawGizmos();
		}
	}
	#endif



	// Fields

	[SerializeField] EventGraphSO m_Event;
	[SerializeField] TriggerType  m_TriggerType;
	[SerializeField] int   m_Count    = -1;
	[SerializeField] float m_Cooldown = -1;
	[SerializeField] bool  m_IsGlobal = false;



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
				bool interactType = value == TriggerType.OnInteract;
				bool hasComponent = gameObject.TryGetComponent(out InteractableAuthoring interactable);
				if (interactType ^ hasComponent) {
					if (interactType) gameObject.AddComponent<InteractableAuthoring>();
					else DestroyImmediate(interactable);
				}
			}
		}
	}
	public bool UseCountLimit {
		get => 0 <= m_Count;
		set => m_Count = value ? Mathf.Max(0, m_Count) : -1;
	}
	public int Count {
		get => m_Count;
		set => m_Count = value;
	}
	public bool UseCooldown {
		get => 0f <= m_Cooldown;
		set => m_Cooldown = value ? Mathf.Max(0f, m_Cooldown) : -1f;
	}
	public float Cooldown {
		get => m_Cooldown;
		set => m_Cooldown = value;
	}
	public bool IsGlobal {
		get => m_IsGlobal;
		set => m_IsGlobal = value;
	}



	// Baker
	
	class Baker : Baker<EventTriggerAuthoring> {
		public override void Bake(EventTriggerAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new EventTrigger {

				Event    = authoring.Event,
				Count    = authoring.Count,
				Cooldown = authoring.Cooldown,
				IsGlobal = authoring.IsGlobal,

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Event Trigger
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct EventTrigger : IComponentData {

	public UnityObjectRef<EventGraphSO> Event;
	public int   Count;
	public float Cooldown;
	public bool  IsGlobal;
	public float Timer;

	public readonly bool UseCountLimit => 0 <= Count;
	public readonly bool UseCooldown => 0f <= Cooldown;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Event Trigger System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

/*
if is global enabled, then the event will be triggered on all clients
rpc this entity
*/

[BurstCompile]
[UpdateInGroup(typeof(DOTSClientSimulationSystemGroup))]
partial struct EventTriggerSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<SimulationSingleton>();
		state.RequireForUpdate<GameManagerBridge>();
	}

	public void OnUpdate(ref SystemState state) {
		state.Dependency = new EventSimulationJob {
			DeltaTime = SystemAPI.Time.DeltaTime,
		}.ScheduleParallel(state.Dependency);
		state.Dependency = new TriggerEventJob {
			GameManager        = SystemAPI.GetSingletonRW<GameManagerBridge>(),
			GhostOwnerLookup   = SystemAPI.GetComponentLookup<GhostOwnerIsLocal>(true),
			InteractableLookup = SystemAPI.GetComponentLookup<Interactable>(true),
			TriggerLookup      = SystemAPI.GetComponentLookup<EventTrigger>(),
		}.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
	}

	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct EventSimulationJob : IJobEntity {
		public float DeltaTime;
		public void Execute(ref EventTrigger trigger) {
			if (trigger.UseCooldown) trigger.Timer -= DeltaTime;
		}
	}

	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct TriggerEventJob : ITriggerEventsJob {
		[NativeDisableUnsafePtrRestriction] public RefRW<GameManagerBridge> GameManager;
		[ReadOnly] public ComponentLookup<GhostOwnerIsLocal> GhostOwnerLookup;
		[ReadOnly] public ComponentLookup<Interactable> InteractableLookup;
		public ComponentLookup<EventTrigger> TriggerLookup;

		public void Execute(TriggerEvent triggerEvent) {
			Execute(triggerEvent.EntityA, triggerEvent.EntityB);
			Execute(triggerEvent.EntityB, triggerEvent.EntityA);
		}
		public void Execute(Entity entity, Entity target) {
			if (GhostOwnerLookup.HasComponent(entity) && GhostOwnerLookup.IsComponentEnabled(entity)) {
				if (TriggerLookup.HasComponent(target) && !InteractableLookup.HasComponent(target)) {
					var trigger = TriggerLookup[target];
					bool match = true;
					match &= !trigger.UseCountLimit || 0 < trigger.Count;
					match &= !trigger.UseCooldown || trigger.Timer <= 0f;
					if (match) {
						GameManager.ValueRW.PlayEvent(trigger.Event);
						if (trigger.UseCountLimit) trigger.Count--;
						if (trigger.UseCooldown) trigger.Timer = trigger.Cooldown;
						TriggerLookup[target] = trigger;
					}
				}
			}
		}
	}
}

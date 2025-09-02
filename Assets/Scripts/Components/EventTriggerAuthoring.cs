using UnityEngine;
using System;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEditor;
#endif



public enum Trigger : byte {
	Instant,
	InRange,
	OnInteract,
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Event Trigger Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Event Trigger")]
[RequireComponent(typeof(InteractorAuthoring), typeof(PhysicsShapeAuthoring))]
public sealed class EventTriggerAuthoring : MonoComponent<EventTriggerAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(EventTriggerAuthoring))]
	class EventTriggerAuthoringEditor : EditorExtensions {
		EventTriggerAuthoring I => target as EventTriggerAuthoring;
		public override void OnInspectorGUI() {
			Begin();

			if (Enum.TryParse(I.name, out Event value)) {
				BeginDisabledGroup();
				EnumField("Event", value);
				EndDisabledGroup();
				Space();
			}

			LabelField("Event Data", EditorStyles.boldLabel);
			BeginHorizontal();
			I.EventGraph = ObjectField("Event Graph", I.EventGraph);
			if (I.EventGraph == null && Button("Create", GUILayout.Width(64f))) {
				I.EventGraph = CreateInstance<EventGraph>();
				I.EventGraph.name = I.gameObject.name;
			}
			if (I.EventGraph != null && Button("Open", GUILayout.Width(64f))) {
				I.EventGraph.Open();
				I.EventGraph.name = I.gameObject.name;
			}
			EndHorizontal();
			I.Trigger = EnumField("Trigger", I.Trigger);
			BeginHorizontal();
			BeginDisabledGroup(I.Trigger == Trigger.Instant);
			PrefixLabel("Use Count Limit");
			I.UseCountLimit = EditorGUILayout.Toggle(I.UseCountLimit, GUILayout.Width(14f));
			if (I.UseCountLimit) I.Count = IntField(I.Count);
			EndHorizontal();
			BeginHorizontal();
			PrefixLabel("Use Cooldown");
			I.UseCooldown = EditorGUILayout.Toggle(I.UseCooldown, GUILayout.Width(14f));
			if (I.UseCooldown) I.Cooldown = FloatField(I.Cooldown);
			EndHorizontal();
			EndDisabledGroup();
			Space();

			End();
		}

		void OnSceneGUI() {
			if (I.EventGraph?.Clone != null) {
				Tools.current = Tool.None;
				var list = I.EventGraph.Clone.GetEvents();
				foreach (var eventBase in list) {
					eventBase.DrawHandles();
				}
			}
		}
	}

	void OnDrawGizmosSelected() {
		if (EventGraph?.Entry != null) {
			Gizmos.color = Color.white;
			var list = EventGraph.Entry.GetEvents();
			foreach (var eventBase in list) {
				eventBase.DrawGizmos();
			}
		}
		if (EventGraph?.Clone != null) {
			Gizmos.color = Color.green;
			var list = EventGraph.Clone.GetEvents();
			foreach (var eventBase in list) {
				eventBase.DrawGizmos();
			}
		}
	}
	#endif



	// Fields

	#if UNITY_EDITOR
	[SerializeField] string m_TriggerName;
	#endif

	[SerializeField] EventGraph m_EventGraph;
	[SerializeField] Trigger m_Trigger;
	[SerializeField] int m_Count = int.MaxValue;
	[SerializeField] float m_Cooldown = float.MaxValue;



	// Properties

	EventGraph EventGraph {
		get => m_EventGraph;
		set => m_EventGraph = value;
	}

	#if UNITY_EDITOR
	Trigger Trigger {
		get => !Enum.TryParse(m_TriggerName, out Trigger trigger) ?
			Enum.Parse<Trigger>(m_TriggerName = m_Trigger.ToString()) :
			m_Trigger = trigger;
		set => m_TriggerName = (m_Trigger = value).ToString();
	}
	#else
	Trigger Trigger {
		get => m_Trigger;
		set => m_Trigger = value;
	}
	#endif

	bool UseCountLimit {
		get => m_Count != int.MaxValue;
		set {
			if (m_Count != int.MaxValue != value) {
				m_Count = value ? 1 : int.MaxValue;
			}
		}
	}
	int Count {
		get => m_Count;
		set {
			if (m_Count != int.MaxValue) {
				m_Count = Mathf.Max(1, value);
			}
		}
	}

	bool UseCooldown {
		get => m_Cooldown != float.MaxValue;
		set {
			if (m_Cooldown != float.MaxValue != value) {
				m_Cooldown = value ? 0.01f : float.MaxValue;
			}
		}
	}
	float Cooldown {
		get => m_Cooldown;
		set {
			if (m_Cooldown != float.MaxValue) {
				m_Cooldown = Mathf.Max(0.01f, value);
			}
		}
	}



	// Baker

	class Baker : Baker<EventTriggerAuthoring> {
		public override void Bake(EventTriggerAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.Dynamic);
			AddComponent(entity, new EventTriggerBlob {
				Value = this.AddBlobAsset(new EventTriggerBlobData {

					EventGraph = authoring.EventGraph,

				})
			});
			AddComponent(entity, new EventTriggerData {

				Trigger  = authoring.Trigger,
				Count    = authoring.Count,
				Cooldown = authoring.Cooldown,

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Event Trigger Blob
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct EventTriggerBlob : IComponentData {

	// Fields

	public BlobAssetReference<EventTriggerBlobData> Value;
}



public struct EventTriggerBlobData {

	// Fields

	public UnityObjectRef<EventGraph> EventGraph;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Event Trigger Data
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct EventTriggerData : IComponentData {

	// Fields

	public Trigger Trigger;
	public int Count;
	public float Cooldown;
	public float Timer;
	public byte State;
	public uint EventID;



	// Properties

	public bool UseCountLimit {
		get => Count != int.MaxValue;
	}
	public bool UseCooldown {
		get => Cooldown != float.MaxValue;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Event Trigger Server Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSServerSimulationSystemGroup))]
partial struct EventTriggerServerSimulationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
		state.RequireForUpdate<EventTriggerData>();
	}

	public void OnUpdate(ref SystemState state) {
		var bufferSystem =
			SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
		var buffer = bufferSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

		state.Dependency = new EventTriggerServerSimulationJob {
			Buffer = buffer,
		}.ScheduleParallel(state.Dependency);
	}
}

[BurstCompile, WithAll(typeof(Simulate))]
partial struct EventTriggerServerSimulationJob : IJobEntity {
	public EntityCommandBuffer.ParallelWriter Buffer;

	public void Execute(
		in EventTriggerData triggerData,
		Entity entity,
		[ChunkIndexInQuery] int chunkIndex) {

		Buffer.DestroyEntity(chunkIndex, entity);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Event Trigger Client Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSClientSimulationSystemGroup))]
[UpdateAfter(typeof(InteractorClientSimulationSystem))]
partial struct EventTriggerClientSimulationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
		state.RequireForUpdate<GameBridgeSystem.Singleton>();
		state.RequireForUpdate<EventTriggerData>();
	}

	public void OnUpdate(ref SystemState state) {
		var bufferSystem =
			SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
		var buffer = bufferSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
		var gameBridge = SystemAPI.GetSingleton<GameBridgeSystem.Singleton>();

		state.Dependency = new EventTriggerClientSimulationJob {
			Buffer           = buffer,
			GameBridgeMethod = gameBridge.Method,
			GameBridgeResult = gameBridge.Result,
			DeltaTime        = SystemAPI.Time.DeltaTime,
		}.ScheduleParallel(state.Dependency);
	}
}

[BurstCompile, WithAll(typeof(Simulate))]
[WithAll(typeof(Interactor)), WithPresent(typeof(Interactable), typeof(Interact))]
partial struct EventTriggerClientSimulationJob : IJobEntity {
	public EntityCommandBuffer.ParallelWriter Buffer;
	public NativeQueue<FixedBytes62>.ParallelWriter GameBridgeMethod;
	public NativeHashMap<Entity, FixedBytes16>.ReadOnly GameBridgeResult;
	[ReadOnly] public float DeltaTime;

	public void Execute(
		EnabledRefRO<Interactable> interactable,
		EnabledRefRW<Interact> interact,
		in EventTriggerBlob triggerBlob,
		ref EventTriggerData triggerData,
		Entity entity,
		[ChunkIndexInQuery] int chunkIndex) {

		switch (triggerData.State) {
			case 0: {
				if (triggerData.Trigger switch {
					Trigger.Instant => true,
					Trigger.InRange => interactable.ValueRO || interact.ValueRO,
					Trigger.OnInteract => interact.ValueRO,
					_ => false,
					}) {
					interact.ValueRW = false;
					triggerData.State = 1;
				}
			} break;
			case 1: {
				if (!GameBridgeResult.TryGetPlayEventResult(entity, out uint eventID)) {
					GameBridgeMethod.PlayEvent(entity, triggerBlob.Value.Value.EventGraph);
				} else {
					if (triggerData.UseCountLimit) triggerData.Count--;
					if (triggerData.Count <= 0) Buffer.DestroyEntity(chunkIndex, entity);
					if (triggerData.UseCooldown) triggerData.Timer = triggerData.Cooldown;
					triggerData.EventID = eventID;
					triggerData.State = 2;
				}
			} break;
			case 2: {
				if (!GameBridgeResult.TryGetIsEventPlayingResult(entity, out bool isPlaying)) {
					GameBridgeMethod.IsEventPlaying(entity, triggerData.EventID);
				} else if (!isPlaying) {
					triggerData.EventID = default;
					triggerData.State = 3;
				}
			} break;
			case 3: {
				if (0f < triggerData.Timer) triggerData.Timer -= DeltaTime;
				if (!triggerData.UseCooldown || triggerData.Timer <= 0f) {
					interact.ValueRW = false;
					triggerData.State = 0;
				}
			} break;
		}
	}
}

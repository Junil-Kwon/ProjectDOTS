using UnityEngine;
using System;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.NetCode;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEditor;
#endif



public enum Interaction : byte {
	Interact,
	Use,
	Equip,
	Talk,
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Interactor Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Interactor")]
[RequireComponent(typeof(PhysicsShapeAuthoring))]
public class InteractorAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(InteractorAuthoring))]
	class InteractorAuthoringEditor : EditorExtensions {
		InteractorAuthoring I => target as InteractorAuthoring;
		public override void OnInspectorGUI() {
			Begin();

			I.Interaction = TextEnumField("Interaction", I.Interaction);
			I.Label = TextField("Label", I.Label);
			I.Center = Vector3Field("Center", I.Center);

			End();
		}
	}
	#endif



	// Fields

	#if UNITY_EDITOR
	[SerializeField] string m_InteractionName;
	#endif

	[SerializeField] Interaction m_Interaction;
	[SerializeField] string m_Label = "Event";
	[SerializeField] Vector3 m_Center;



	// Properties

	#if UNITY_EDITOR
	public Interaction Interaction {
		get => !Enum.TryParse(m_InteractionName, out Interaction interactType) ?
			Enum.Parse<Interaction>(m_InteractionName = m_Interaction.ToString()) :
			m_Interaction = interactType;
		set => m_InteractionName = (m_Interaction = value).ToString();
	}
	#else
	public Interaction Interaction {
		get => m_Interaction;
		set => m_Interaction = value;
	}
	#endif

	public string Label {
		get => m_Label;
		set => m_Label = value;
	}
	public Vector3 Center {
		get => m_Center;
		set => m_Center = value;
	}



	// Baker

	public class Baker : Baker<InteractorAuthoring> {
		public override void Bake(InteractorAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new Interactor {

				Interaction = authoring.Interaction,
				Label       = authoring.Label,
				Center      = authoring.Center,

			});
			AddComponent(entity, new Interactable());
			SetComponentEnabled<Interactable>(entity, false);
			AddComponent(entity, new Interact());
			SetComponentEnabled<Interact>(entity, false);
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Interactor
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct Interactor : IComponentData, IEnableableComponent {

	// Fields

	public Interaction Interaction;
	public FixedString32Bytes Label;
	public float3 Center;
	public uint TextID;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Interactable
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct Interactable : IComponentData, IEnableableComponent { }



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Interact
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct Interact : IComponentData, IEnableableComponent { }



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Interactor Client Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSClientSimulationSystemGroup))]
partial struct InteractorClientSimulationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<InputBridgeSystem.Singleton>();
		state.RequireForUpdate<UIBridgeSystem.Singleton>();
		state.RequireForUpdate<SimulationSingleton>();
		state.RequireForUpdate<Interactor>();
	}

	public void OnUpdate(ref SystemState state) {
		var inputBridge = SystemAPI.GetSingleton<InputBridgeSystem.Singleton>();
		var uiBridge = SystemAPI.GetSingleton<UIBridgeSystem.Singleton>();
		var simulation = SystemAPI.GetSingleton<SimulationSingleton>();

		state.Dependency = new InteractorClientSimulationJob {
			UIBridgeMethod = uiBridge.Method,
			UIBridgeResult = uiBridge.Result,
		}.ScheduleParallel(state.Dependency);
		state.Dependency = new InteractorClientTriggerEventsJob {
			InputBridgeProperty = inputBridge.Property[0],
			GhostOwnerLookup    = SystemAPI.GetComponentLookup<GhostOwnerIsLocal>(true),
			InteractorLookup    = SystemAPI.GetComponentLookup<Interactor>(true),
			InteractableLookup  = SystemAPI.GetComponentLookup<Interactable>(),
			InteractLookup      = SystemAPI.GetComponentLookup<Interact>(),
		}.Schedule(simulation, state.Dependency);
	}
}

[BurstCompile, WithAll(typeof(Simulate))]
[WithPresent(typeof(Interactor), typeof(Interactable))]
partial struct InteractorClientSimulationJob : IJobEntity {
	public NativeQueue<FixedBytes62>.ParallelWriter UIBridgeMethod;
	public NativeHashMap<Entity, FixedBytes16>.ReadOnly UIBridgeResult;

	public void Execute(
		ref Interactor interactor,
		EnabledRefRW<Interactable> interactable,
		in LocalTransform transform,
		Entity entity) {

		if (interactable.ValueRO) {
			interactable.ValueRW = false;
			if (interactor.TextID == default && 0 < interactor.Label.Length) {
				if (!UIBridgeResult.TryGetAddTextResult(entity, out uint textID)) {
					var position = transform.Position;
					if (math.any(interactor.Center != default)) {
						position += math.mul(transform.Rotation, interactor.Center);
					}
					UIBridgeMethod.AddText(entity, interactor.Label, position, float.MaxValue);
				} else interactor.TextID = textID;
			}
			if (interactor.TextID != default) {
				var position = transform.Position;
				if (math.any(interactor.Center != default)) {
					position += math.mul(transform.Rotation, interactor.Center);
				}
				UIBridgeMethod.SetTextPosition(entity, interactor.TextID, position);
			}
		} else {
			uint textID = interactor.TextID;
			if (interactor.TextID != default) {
				interactor.TextID = default;
				UIBridgeMethod.RemoveText(entity, textID);
			}
		}
	}
}

[BurstCompile, WithAll(typeof(Simulate))]
partial struct InteractorClientTriggerEventsJob : ITriggerEventsJob {
	[ReadOnly] public InputBridge.Property InputBridgeProperty;
	[ReadOnly] public ComponentLookup<GhostOwnerIsLocal> GhostOwnerLookup;
	[ReadOnly] public ComponentLookup<Interactor> InteractorLookup;
	public ComponentLookup<Interactable> InteractableLookup;
	public ComponentLookup<Interact> InteractLookup;

	public void Execute(TriggerEvent triggerEvent) {
		Execute(triggerEvent.EntityA, triggerEvent.EntityB);
		Execute(triggerEvent.EntityB, triggerEvent.EntityA);
	}

	public void Execute(Entity entity, Entity target) {
		bool match = true;
		match = match && GhostOwnerLookup.HasComponent(target);
		match = match && GhostOwnerLookup.IsComponentEnabled(target);
		match = match && InteractorLookup.HasComponent(entity);
		if (match) {
			var interactor   = InteractorLookup.GetEnabledRefRO<Interactor>(entity);
			var interactable = InteractableLookup.GetEnabledRefRW<Interactable>(entity);
			var interact     = InteractLookup.GetEnabledRefRW<Interact>(entity);

			if (interactor.ValueRO && !interact.ValueRO) {
				interactable.ValueRW = true;
			}
			if (interactable.ValueRO && InputBridgeProperty.GetKey(KeyAction.Interact)) {
				interactable.ValueRW = false;
				interact.ValueRW = true;
			}
		}
	}
}

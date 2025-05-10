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
// Global Event Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Global Event")]
public class GlobalEventAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(GlobalEventAuthoring))]
		class GlobalEventAuthoringEditor : EditorExtensions {
			GlobalEventAuthoring I => target as GlobalEventAuthoring;
			public override void OnInspectorGUI() {
				Begin("Global Event Authoring");

				LabelField("Event", EditorStyles.boldLabel);
				I.Event = ObjectField("Event Graph", I.Event);
				if (Button("Open Event Graph")) {
					if (!I.Event) I.Event = CreateInstance<EventGraphSO>();
					I.Event.name = I.gameObject.name;
					I.Event.Open();
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



	// Properties

	public EventGraphSO Event {
		get => m_Event;
		set => m_Event = value;
	}



	// Baker

	class Baker : Baker<GlobalEventAuthoring> {
		public override void Bake(GlobalEventAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new LocalEvent {

				Event = authoring.Event,

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Global Event
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct GlobalEvent : IComponentData {

	public UnityObjectRef<EventGraphSO> Event;



	[GhostField] public Entity Target;

}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Global Event System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
partial struct GlobalEventSystem : ISystem {

	[BurstCompile]
	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<GlobalEvent>();
		state.RequireForUpdate<SimulationSingleton>();
		state.RequireForUpdate<GameManagerBridge>();
	}



}

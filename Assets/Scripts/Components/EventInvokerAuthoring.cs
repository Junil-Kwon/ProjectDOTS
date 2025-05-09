using UnityEngine;

using Unity.Entities;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Event Invoker Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Event Invoker")]
public class EventAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(EventAuthoring))]
		class EventAuthoringEditor : EditorExtensions {
			EventAuthoring I => target as EventAuthoring;
			public override void OnInspectorGUI() {
				Begin("Event Authoring");

				LabelField("Event", EditorStyles.boldLabel);
				I.EventGraph = ObjectField("Event Graph", I.EventGraph);
				if (Button("Open Event Graph")) {
					if (!I.EventGraph) {
						I.EventGraph = CreateInstance<EventGraphSO>();
						I.EventGraph.name = I.gameObject.name;
					}
					I.EventGraph.Open();
				}
				Space();

				End();
			}

			void OnSceneGUI() {
				if (I.EventGraph == null) return;
				if (I.EventGraph.Clone != null) {
					Tools.current = Tool.None;
					foreach (var data in I.EventGraph.Clone.GetEvents()) data.DrawHandles();
				}
			}
		}
	#endif



	// Fields

	[SerializeField] EventGraphSO m_EventGraph;



	// Properties

	public EventGraphSO EventGraph {
		get => m_EventGraph;
		set => m_EventGraph = value;
	}



	// Methods

	#if UNITY_EDITOR
		void OnDrawGizmosSelected() {
			if (EventGraph == null) return;
			Gizmos.color = color.green;
			foreach (var data in EventGraph.Entry.GetEvents()) data.DrawGizmos();
			
			if (EventGraph.Clone == null) return;
			Gizmos.color = color.white;
			foreach (var data in EventGraph.Clone.GetEvents()) data.DrawGizmos();
		}
	#endif



	// Baker

	class Baker : Baker<EventAuthoring> {
		public override void Bake(EventAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new EventInvoker {

				EventGraph = authoring.EventGraph,

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Event Invoker
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct EventInvoker : IComponentData, IEnableableComponent {

	public UnityObjectRef<EventGraphSO> EventGraph;
}

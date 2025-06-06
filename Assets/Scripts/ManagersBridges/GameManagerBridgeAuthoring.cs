using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game Manager Bridge Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/Game Manager Bridge")]
public class GameManagerBridgeAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(GameManagerBridgeAuthoring))]
		class GameManagerBridgeAuthoringEditor : EditorExtensions {
			GameManagerBridgeAuthoring I => target as GameManagerBridgeAuthoring;
			public override void OnInspectorGUI() {
				Begin("Game Manager Bridge Authoring");

				End();
			}
		}
	#endif



	// Baker

	public class Baker : Baker<GameManagerBridgeAuthoring> {
		public override void Bake(GameManagerBridgeAuthoring authoring) {
			Entity entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new GameManagerBridge {

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game Manager Bridge
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct GameManagerBridge : IComponentData {

	public UnityObjectRef<EventGraphSO> PlayEvent_graph;
}



public static class GameManagerBridgeExtensions {

	public static void PlayEvent
		(this ref GameManagerBridge bridge, UnityObjectRef<EventGraphSO> graph) {
		bridge.PlayEvent_graph = graph;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game Manager Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class GameManagerBridgeSystem : SystemBase {

	bool initialized = false;
	GameManagerBridge prev;

	protected override void OnCreate() {
		RequireForUpdate<GameManagerBridge>();
	}

	protected override void OnUpdate() {
		var bridge = SystemAPI.GetSingletonRW<GameManagerBridge>();
		if (initialized == false) {
			initialized = true;
			
			prev = bridge.ValueRO;
		}
		var next = bridge.ValueRO;

		var playEvent = false;
		playEvent |= prev.PlayEvent_graph != next.PlayEvent_graph;
		if (playEvent) GameManager.PlayEvent(next.PlayEvent_graph.Value);
		next.PlayEvent_graph = default;

		bridge.ValueRW = prev = next;
	}
}

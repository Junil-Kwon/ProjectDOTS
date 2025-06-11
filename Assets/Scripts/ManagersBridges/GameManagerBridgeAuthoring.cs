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

	// Fields

	public GameState m_GameState;

	public UnityObjectRef<EventGraphSO> PlayEvent_graph;

	public uint Flag;



	// Properties

	public GameState GameState {
		get => m_GameState;
		set => m_GameState = value;
	}



	// Methods

	public void PlayEvent(UnityObjectRef<EventGraphSO> graph) {
		PlayEvent_graph = graph;
		Flag |= 0x0001u;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game Manager Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class GameManagerBridgeSystem : SystemBase {

	protected override void OnCreate() {
		RequireForUpdate<GameManagerBridge>();
	}

	protected override void OnUpdate() {
		var bridge = SystemAPI.GetSingletonRW<GameManagerBridge>();

		var i = bridge.ValueRO;
		if ((i.Flag & 0x0001u) != 0u) GameManager.PlayEvent(i.PlayEvent_graph.Value);

		ref var o = ref bridge.ValueRW;
		o.GameState = GameManager.GameState;
		o.PlayEvent_graph = default;

		o.Flag = 0u;
	}
}

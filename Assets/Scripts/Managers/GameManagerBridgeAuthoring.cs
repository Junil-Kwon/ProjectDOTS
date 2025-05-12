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
			AddComponent(entity, new GameManagerBridge());
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game Manager Bridge
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct GameManagerBridge : IComponentData {

	// Fields

	GameState m_GameState;

	uint m_Flag;



	// Properties

	public GameState GameState {
		get => m_GameState;
		set {
			m_GameState = value;
			Flag |= 0x0001u;
		}
	}



	public uint Flag {
		get => m_Flag;
		set => m_Flag = value;
	}



	// Methods

	public UnityObjectRef<EventGraphSO> PlayEvent_graph;

	public void PlayEvent(UnityObjectRef<EventGraphSO> graph) {
		PlayEvent_graph = graph;
		Flag |= 0x0100u;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game Manager Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class GameManagerBridgeSystem : SystemBase {

	[BurstCompile]
	protected override void OnCreate() {
		RequireForUpdate<GameManagerBridge>();
	}

	[BurstDiscard]
	protected override void OnUpdate() {
		var bridge = SystemAPI.GetSingletonRW<GameManagerBridge>();
		var flag   = bridge.ValueRO.Flag;

		if ((flag & 0x0001u) != 0u) GameManager.GameState = bridge.ValueRO.GameState;

		bridge.ValueRW.GameState = GameManager.GameState;

		if ((flag & 0x0100u) != 0u) {
			var graph = bridge.ValueRO.PlayEvent_graph.Value;
			GameManager.PlayEvent(graph);
		}

		if (flag != 0u) bridge.ValueRW.Flag = 0u;
	}
}

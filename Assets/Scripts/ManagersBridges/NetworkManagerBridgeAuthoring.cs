using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager Bridge Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/Network Manager Bridge")]
public class NetworkManagerBridgeAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(NetworkManagerBridgeAuthoring))]
		class NetworkManagerBridgeAuthoringEditor : EditorExtensions {
			NetworkManagerBridgeAuthoring I => target as NetworkManagerBridgeAuthoring;
			public override void OnInspectorGUI() {
				Begin("Network Manager Bridge Authoring");

				End();
			}
		}
	#endif



	// Baker

	public class Baker : Baker<NetworkManagerBridgeAuthoring> {
		public override void Bake(NetworkManagerBridgeAuthoring authoring) {
			Entity entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new NetworkManagerBridge {

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager Bridge
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct NetworkManagerBridge : IComponentData {

	// Fields

	public ServiceState m_ServiceState;
	public NetworkState m_NetworkState;

	public uint Flag;



	// Properties

	public ServiceState ServiceState {
		get => m_ServiceState;
		set => m_ServiceState = value;
	}
	public NetworkState NetworkState {
		get => m_NetworkState;
		set => m_NetworkState = value;
	}
	public bool IsHost   => m_NetworkState == NetworkState.ConnectedAsHost;
	public bool IsClient => m_NetworkState == NetworkState.ConnectedAsClient;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class NetworkManagerBridgeSystem : SystemBase {

	protected override void OnCreate() {
		RequireForUpdate<NetworkManagerBridge>();
	}

	protected override void OnUpdate() {
		var bridge = SystemAPI.GetSingletonRW<NetworkManagerBridge>();

		var i = bridge.ValueRO;

		ref var o = ref bridge.ValueRW;
		o.ServiceState = i.ServiceState;
		o.NetworkState = i.NetworkState;

		o.Flag = 0u;
	}
}

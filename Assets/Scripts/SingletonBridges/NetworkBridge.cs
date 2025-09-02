using System.Runtime.InteropServices;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Physics;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Bridge
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public static class NetworkBridge {

	// Constants

	public struct Property {
		public ServiceState ServiceState;
		public NetworkState NetworkState;
		public NetworkError NetworkError;
		public int MaxPlayers;

		public bool IsSinglePlayer => MaxPlayers == 1;
		public bool IsMultiPlayer => !IsSinglePlayer;
	}

	public enum Method : byte {
		SendChatMessage,
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct SendChatMessage {
		[FieldOffset(00)] public FixedBytes62 Data;
		[FieldOffset(00)] public Method Method;
		[FieldOffset(01)] public Entity Entity;
		[FieldOffset(09)] public FixedString32Bytes Message;
	}
}



public static class NetworkBridgeExtensions {

	// Methods

	public static void SendChatMessage(
		this NativeQueue<FixedBytes62>.ParallelWriter method,
		Entity entity, FixedString32Bytes message) {
		var i = new NetworkBridge.SendChatMessage {
			Method = NetworkBridge.Method.SendChatMessage,
			Entity = entity, Message = message,
		};
		method.Enqueue(i.Data);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class NetworkBridgeSystem : SystemBase {
	bool Initialized;
	NativeArray<NetworkBridge.Property> Property;
	NativeQueue<FixedBytes62> Method;
	NativeHashMap<Entity, FixedBytes16> Result;

	public struct Singleton : IComponentData {
		public NativeArray<NetworkBridge.Property>.ReadOnly Property;
		public NativeQueue<FixedBytes62>.ParallelWriter Method;
		public NativeHashMap<Entity, FixedBytes16>.ReadOnly Result;
	}

	protected override void OnCreate() {
		RequireForUpdate<PhysicsStep>();
	}

	protected override void OnUpdate() {
		if (Initialized != true) {
			if (SystemAPI.TryGetSingletonEntity<PhysicsStep>(out var entity)) {
				EntityManager.AddComponentData(entity, new Singleton {
					Property = (Property = new(1, Allocator.Persistent)).AsReadOnly(),
					Method = (Method = new(Allocator.Persistent)).AsParallelWriter(),
					Result = (Result = new(64, Allocator.Persistent)).AsReadOnly(),
				});
				Initialized = true;
			} else return;
		}
		EntityManager.CompleteAllTrackedJobs();
		Property[0] = new NetworkBridge.Property {
			ServiceState = NetworkManager.ServiceState,
			NetworkState = NetworkManager.NetworkState,
			NetworkError = NetworkManager.NetworkError,
			MaxPlayers   = NetworkManager.MaxPlayers,
		};
		Result.Clear();
		while (Method.TryDequeue(out var data)) {
			switch ((NetworkBridge.Method)data.offset0000.byte0000) {
				case NetworkBridge.Method.SendChatMessage: {
					var i = new NetworkBridge.SendChatMessage { Data = data };
					NetworkManager.SendChatMessage(i.Message.ToString());
				} break;
			}
		}
	}

	protected override void OnDestroy() {
		Property.Dispose();
		Method.Dispose();
		Result.Dispose();
	}
}

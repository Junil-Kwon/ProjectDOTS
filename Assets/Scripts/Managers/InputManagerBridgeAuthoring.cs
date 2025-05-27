using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Input Manager Bridge Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/Input Manager Bridge")]
public class InputManagerBridgeAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(InputManagerBridgeAuthoring))]
		class InputManagerBridgeAuthoringEditor : EditorExtensions {
			InputManagerBridgeAuthoring I => target as InputManagerBridgeAuthoring;
			public override void OnInspectorGUI() {
				Begin("Input Manager Bridge Authoring");

				End();
			}
		}
	#endif



	// Baker

	public class Baker : Baker<InputManagerBridgeAuthoring> {
		public override void Bake(InputManagerBridgeAuthoring authoring) {
			Entity entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new InputManagerBridge {

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Input Manager Bridge
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct InputManagerBridge : IComponentData {

	public uint KeyPrev;
	public uint KeyNext;
	public float2 MousePosition;
	public float2 ScrollWheel;
	public float2 LookDirection;
	public float2 MoveDirection;
}



public static class InputManagerBridgeExtensions {

	public static bool GetKeyNext(this in InputManagerBridge bridge, KeyAction key) {
		return (bridge.KeyNext & (1u << (int)key)) != 0u;
	}
	public static bool GetKeyPrev(this in InputManagerBridge bridge, KeyAction key) {
		return (bridge.KeyPrev & (1u << (int)key)) != 0u;
	}

	public static bool GetKey(this in InputManagerBridge bridge, KeyAction key) {
		return bridge.GetKeyNext(key);
	}
	public static bool GetKeyDown(this in InputManagerBridge bridge, KeyAction key) {
		return bridge.GetKeyNext(key) && !bridge.GetKeyPrev(key);
	}
	public static bool GetKeyUp(this in InputManagerBridge bridge, KeyAction key) {
		return !bridge.GetKeyNext(key) && bridge.GetKeyPrev(key);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Input Manager Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class InputManagerBridgeSystem : SystemBase {

	bool initialized = false;
	InputManagerBridge prev;

	protected override void OnCreate() {
		RequireForUpdate<InputManagerBridge>();
	}

	protected override void OnUpdate() {
		var bridge = SystemAPI.GetSingletonRW<InputManagerBridge>();
		if (initialized == false) {
			initialized = true;
			// prev.
		}
		var next = bridge.ValueRO;

		next.KeyPrev       = InputManager.KeyPrev;
		next.KeyNext       = InputManager.KeyNext;
		next.MousePosition = InputManager.PointPosition;
		next.ScrollWheel   = InputManager.ScrollWheel;
		next.LookDirection = InputManager.LookDirection;
		next.MoveDirection = InputManager.MoveDirection;

		bridge.ValueRW = prev = next;
	}
}

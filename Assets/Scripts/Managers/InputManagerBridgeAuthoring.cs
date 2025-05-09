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
			AddComponent(entity, new InputManagerBridge());
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Input Manager Bridge
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct InputManagerBridge : IComponentData {

	// Fields

	uint   m_KeyPrev;
	uint   m_KeyNext;
	float2 m_MousePosition;
	float2 m_ScrollWheel;
	float2 m_LookDirection;
	float2 m_MoveDirection;

	uint m_Flag;


	// Properties

	public uint KeyPrev {
		get => m_KeyPrev;
		set => m_KeyPrev = value;
	}
	public uint KeyNext {
		get => m_KeyNext;
		set => m_KeyNext = value;
	}
	public float2 MousePosition {
		get => m_MousePosition;
		set => m_MousePosition = value;
	}
	public float2 ScrollWheel {
		get => m_ScrollWheel;
		set => m_ScrollWheel = value;
	}
	public float2 LookDirection {
		get => m_LookDirection;
		set => m_LookDirection = value;
	}
	public float2 MoveDirection {
		get => m_MoveDirection;
		set => m_MoveDirection = value;
	}



	public uint Flag {
		get => m_Flag;
		set => m_Flag = value;
	}



	// Methods

	bool GetKeyNext(KeyAction key) => (KeyNext & (1u << (int)key)) != 0u;
	bool GetKeyPrev(KeyAction key) => (KeyPrev & (1u << (int)key)) != 0u;

	public bool GetKey    (KeyAction key) =>  GetKeyNext(key);
	public bool GetKeyDown(KeyAction key) =>  GetKeyNext(key) && !GetKeyPrev(key);
	public bool GetKeyUp  (KeyAction key) => !GetKeyNext(key) &&  GetKeyPrev(key);
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Input Manager Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class InputManagerBridgeSystem : SystemBase {

	[BurstCompile]
	protected override void OnCreate() {
		RequireForUpdate<InputManagerBridge>();
	}

	[BurstDiscard]
	protected override void OnUpdate() {
		var bridge = SystemAPI.GetSingletonRW<InputManagerBridge>();
		var flag   = bridge.ValueRO.Flag;

		bridge.ValueRW.KeyPrev       = InputManager.KeyPrev;
		bridge.ValueRW.KeyNext       = InputManager.KeyNext;
		bridge.ValueRW.MousePosition = InputManager.PointPosition;
		bridge.ValueRW.ScrollWheel   = InputManager.ScrollWheel;
		bridge.ValueRW.LookDirection = InputManager.LookDirection;
		bridge.ValueRW.MoveDirection = InputManager.MoveDirection;

		if (flag != 0u) bridge.ValueRW.Flag = 0u;
	}
}

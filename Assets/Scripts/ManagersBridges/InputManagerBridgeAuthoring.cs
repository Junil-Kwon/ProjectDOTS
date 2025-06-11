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

	// Fields

	public float  m_MouseSensitivity;
	public uint   m_KeyPrev;
	public uint   m_KeyNext;
	public float2 m_LookDirection;
	public float2 m_MoveDirection;
	public float2 m_PointPosition;
	public float2 m_ScrollWheel;
	public float2 m_Navigate;

	public uint Flag;



	// Properties

	public float MouseSensitivity {
		get => m_MouseSensitivity;
		set => m_MouseSensitivity = value;
	}

	public uint KeyPrev {
		get => m_KeyPrev;
		set => m_KeyPrev = value;
	}
	public uint KeyNext {
		get => m_KeyNext;
		set => m_KeyNext = value;
	}
	public float2 LookDirection {
		get => m_LookDirection;
		set => m_LookDirection = value;
	}
	public float2 MoveDirection {
		get => m_MoveDirection;
		set => m_MoveDirection = value;
	}
	public float2 PointPosition {
		get => m_PointPosition;
		set => m_PointPosition = value;
	}
	public float2 ScrollWheel {
		get => m_ScrollWheel;
		set => m_ScrollWheel = value;
	}
	public float2 Navigate {
		get => m_Navigate;
		set => m_Navigate = value;
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

[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class InputManagerBridgeSystem : SystemBase {

	protected override void OnCreate() {
		RequireForUpdate<InputManagerBridge>();
	}

	protected override void OnUpdate() {
		var bridge = SystemAPI.GetSingletonRW<InputManagerBridge>();
		
		var i = bridge.ValueRO;

		ref var o = ref bridge.ValueRW;
		o.MouseSensitivity = InputManager.MouseSensitivity;
		o.KeyPrev          = InputManager.KeyPrev;
		o.KeyNext          = InputManager.KeyNext;
		o.LookDirection    = InputManager.LookDirection;
		o.MoveDirection    = InputManager.MoveDirection;
		o.PointPosition    = InputManager.PointPosition;
		o.ScrollWheel      = InputManager.ScrollWheel;
		o.Navigate         = InputManager.Navigate;

		o.Flag = 0u;
	}
}

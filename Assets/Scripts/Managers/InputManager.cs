using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// Input Actions

public enum ActionMap : byte {
	None,
	Player,
	UI,
}

public enum KeyAction : byte {
	Look,
	Move,
	Jump,
	Interact,
	Primary,
	Sidearm,
	Reload,
	Ability1,
	Ability2,
	Ability3,
	Ping,
	Emotion,

	Point,
	Click,
	MiddleClick,
	RightClick,
	Navigate,
	ScrollWheel,
	Submit,
	Cancel,
	TrackedDevicePosition,
	TrackedDeviceOrientation,
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Input Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/Input Manager")]
[RequireComponent(typeof(PlayerInput))]
public class InputManager : MonoSingleton<InputManager> {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(InputManager))]
		class InputManagerEditor : EditorExtensions {
			InputManager I => target as InputManager;
			public override void OnInspectorGUI() {
				Begin("Input Manager");

				LabelField("Actions", EditorStyles.boldLabel);
				InputActionAsset = ObjectField("Input Action Asset", InputActionAsset);
				PlayerInput.notificationBehavior = PlayerNotifications.InvokeUnityEvents;
				PlayerInput.actions = InputActionAsset;
				if (InputActionAsset) {
					DefaultActionMap = EnumField("Default Action Map", DefaultActionMap);
					PlayerInput.defaultActionMap = DefaultActionMap.ToString();
				}
				Space();

				if (Application.isPlaying) {
					LabelField("Debug", EditorStyles.boldLabel);
					BeginHorizontal();
					PrefixLabel("Switch Action Map");
					for (int i = 0; i < ActionMapLength; i++) {
						if (Button(((ActionMap)i).ToString())) SwitchActionMap((ActionMap)i);
					}
					EndHorizontal();
					IntField("Key", (int)KeyNext);
					Vector2Field("Point Position", PointPosition);
					Vector2Field("Scroll Wheel",   ScrollWheel);
					Vector2Field("Move Direction", MoveDirection);
					Vector2Field("Look Direction", LookDirection);
					Space();
				}

				End();
			}
		}
	#endif



	// Definitions

	static readonly int ActionMapLength = Enum.GetValues(typeof(ActionMap)).Length;
	static readonly int KeyActionLength = Enum.GetValues(typeof(KeyAction)).Length;



	// Fields

	[SerializeField] InputActionAsset m_InputActionAsset;
	[SerializeField] ActionMap        m_DefaultActionMap;

	PlayerInput m_PlayerInput;

	static uint m_KeyNext = 0u;
	static uint m_KeyPrev = 0u;
	static Vector2 m_PointPosition = Vector2.zero;
	static Vector2 m_ScrollWheel   = Vector2.zero;
	static Vector2 m_LookDirection = Vector2.zero;
	static Vector2 m_MoveDirection = Vector2.zero;

	static string m_KeyPressed;



	// Properties

	static InputActionAsset InputActionAsset {
		get => Instance.m_InputActionAsset;
		set => Instance.m_InputActionAsset = value;
	}
	static ActionMap        DefaultActionMap {
		get => Instance.m_DefaultActionMap;
		set => Instance.m_DefaultActionMap = value;
	}

	static PlayerInput PlayerInput =>
		Instance.m_PlayerInput || Instance.TryGetComponent(out Instance.m_PlayerInput) ?
		Instance.m_PlayerInput : null;

	public static uint KeyNext {
		get         => m_KeyNext;
		private set => m_KeyNext = value;
	}
	public static uint KeyPrev {
		get         => m_KeyPrev;
		private set => m_KeyPrev = value;
	}
	public static Vector2 PointPosition {
		get         => m_PointPosition;
		private set => m_PointPosition = value;
	}
	public static Vector2 ScrollWheel {
		get         => m_ScrollWheel;
		private set => m_ScrollWheel = value;
	}
	public static Vector2 LookDirection {
		get         => m_LookDirection;
		private set => m_LookDirection = value;
	}
	public static Vector2 MoveDirection {
		get         => m_MoveDirection;
		private set => m_MoveDirection = value;
	}

	public static string KeyPressed {
		get         => m_KeyPressed;
		private set => m_KeyPressed = value;
	}



	// Key State Methods

	static void RegisterActionMap(ActionMap map, bool register = true) {
		KeyNext = 0u;
		KeyPrev = 0u;
		PointPosition = Vector2.zero;
		ScrollWheel   = Vector2.zero;
		LookDirection = Vector2.zero;
		MoveDirection = Vector2.zero;

		var actionMap = InputActionAsset.FindActionMap(map.ToString());
		if (actionMap == null) return;
		for (int i = 0; i < KeyActionLength; i++) {
			var inputAction = actionMap.FindAction(((KeyAction)i).ToString());
			if (inputAction == null) continue;
			int index = i;
			Action<InputAction.CallbackContext> action = (KeyAction)index switch {
				KeyAction.Point       => callback => PointPosition = callback.ReadValue<Vector2>(),
				KeyAction.ScrollWheel => callback => ScrollWheel   = callback.ReadValue<Vector2>(),
				KeyAction.Look        => callback => LookDirection = callback.ReadValue<Vector2>(),
				KeyAction.Move        => callback => MoveDirection = callback.ReadValue<Vector2>(),
				_ => callback => {
					bool flag = callback.action.IsPressed();
					KeyNext = flag ? KeyNext | (1u << index) : KeyNext & ~(1u << index);
				},
			};
			if (register) inputAction.performed += action;
			else          inputAction.performed -= action;
		}
		if (register) {
			//Cursor.visible = map != ActionMap.Player;
			//Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
		}
	}

	static void UpdateKey() => KeyPrev = KeyNext;

	static bool GetKeyNext(KeyAction key) => (KeyNext & (1u << (int)key)) != 0u;
	static bool GetKeyPrev(KeyAction key) => (KeyPrev & (1u << (int)key)) != 0u;

	public static bool GetKey    (KeyAction key) =>  GetKeyNext(key);
	public static bool GetKeyDown(KeyAction key) =>  GetKeyNext(key) && !GetKeyPrev(key);
	public static bool GetKeyUp  (KeyAction key) => !GetKeyNext(key) &&  GetKeyPrev(key);

	public static void SwitchActionMap(ActionMap map) {
		RegisterActionMap(DefaultActionMap, false);
		RegisterActionMap(DefaultActionMap = map);
	}



	// Key Binding Methods

	/*
	public static List<string> GetKeysBinding(KeyAction keyAction) {
		var keys = new List<string>();
		var inputAction = InputActionAsset.FindAction(keyAction.ToString());
		if (inputAction != null) {
			for (int i = 0; i < inputAction.bindings.Count; i++) {
				var split = inputAction.bindings[i].path.Split('/');
				if (split[0].Equals("<Keyboard>")) keys.Add(split[1]);
				// path: "<Device>/Key"
			}
		}
		return keys;
	}

	public static void SetKeysBinding(KeyAction keyAction, List<string> keys) {
		var inputAction = InputActionAsset.FindAction(keyAction.ToString());
		if (inputAction != null) {
			for (int i = inputAction.bindings.Count - 1; -1 < i; i--) {
				var split = inputAction.bindings[i].path.Split('/');
				if (split[0].Equals("<Keyboard>")) inputAction.ChangeBinding(i).Erase();
				// path: "<Device>/Key"
			}
			foreach (string key in keys) inputAction.AddBinding("<Keyboard>/" + key);
		}
		//bool isMove = KeyAction.MoveUp <= keyAction && keyAction <= KeyAction.MoveRight;
		//if (isMove) SyncMoveBinding();
	}

	static void SyncMoveBinding() {
		var inputAction = InputActionAsset.FindAction(KeyAction.Move.ToString());
		if (inputAction != null) {
			for (int i = inputAction.bindings.Count - 1; -1 < i; i--) {
				bool isComposite = inputAction.bindings[i].isComposite;
				bool is2DVector  = inputAction.bindings[i].name.Equals("2DVector");
				if (isComposite && is2DVector) inputAction.ChangeBinding(i).Erase();
			}
			var composite = inputAction.AddCompositeBinding("2DVector");
			var keysUp    = GetKeysBinding(KeyAction.MoveUp   );
			var keysLeft  = GetKeysBinding(KeyAction.MoveLeft );
			var keysDown  = GetKeysBinding(KeyAction.MoveDown );
			var keysRight = GetKeysBinding(KeyAction.MoveRight);
			foreach (var key in keysUp   ) composite.With("Up",    "<Keyboard>/" + key);
			foreach (var key in keysLeft ) composite.With("Left",  "<Keyboard>/" + key);
			foreach (var key in keysDown ) composite.With("Down",  "<Keyboard>/" + key);
			foreach (var key in keysRight) composite.With("Right", "<Keyboard>/" + key);
		}
	}
	*/



	// Key Record Methods

	public static void BeginRecordKey() => KeyPressed = "";
	public static void EndRecordKey  () => KeyPressed = null;

	static void RegisterKeyRecord() {
		KeyPressed = null;

		InputSystem.onAnyButtonPress.Call(inputControl => {
			if (KeyPressed != null) {
				var parts = inputControl.path.Split('/');
				if (parts[1].Equals("Keyboard")) KeyPressed = parts[2];
				// path: "/Device/Key"
			}
		});
	}



	// Lifecycle

	void Start() {
		if (InputActionAsset) {
			RegisterActionMap(DefaultActionMap);
			RegisterKeyRecord();
		}
	}

	void LateUpdate() {
		UpdateKey();
	}
}

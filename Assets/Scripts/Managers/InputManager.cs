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
	Menu,

	Point,
	Click,
	MiddleClick,
	RightClick,
	Navigate,
	ScrollWheel,
	Submit,
	Cancel,
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Input Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/Input Manager")]
[RequireComponent(typeof(PlayerInput))]
public sealed class InputManager : MonoSingleton<InputManager> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(InputManager))]
	class InputManagerEditor : EditorExtensions {
		InputManager I => target as InputManager;
		public override void OnInspectorGUI() {
			Begin("Input Manager");

			if (InputActionAsset == null) {
				HelpBox("Input Action Asset is missing.\nPlease assign it in the Player Input.");
				Space();
			} else {
				LabelField("Debug", EditorStyles.boldLabel);
				BeginDisabledGroup();
				var actionMap = Application.isPlaying ? PlayerInput.currentActionMap.name : "None";
				TextField("Action Map", actionMap);
				EndDisabledGroup();
				Space();
			}
			End();
		}
	}
	#endif



	// Constants

	const string SensitivityKey = "MouseSensitivity";
	const float SensitivityValue = 0.2f;



	// Fields

	PlayerInput m_PlayerInput;
	bool m_IsPointing;

	float m_MouseSensitivity;
	uint m_KeyNext;
	uint m_KeyPrev;
	Vector2 m_LookDirection;
	Vector2 m_MoveDirection;
	Vector2 m_PointPosition;
	Vector2 m_ScrollWheel;
	Vector2 m_Navigate;

	string m_KeyPressed;



	// Properties

	static PlayerInput PlayerInput =>
		Instance.m_PlayerInput || Instance.TryGetComponent(out Instance.m_PlayerInput) ?
		Instance.m_PlayerInput : null;

	static InputActionAsset InputActionAsset => PlayerInput.actions;

	public static bool IsPointing {
		get         => Instance.m_IsPointing;
		private set => Instance.m_IsPointing = value;
	}



	public static float MouseSensitivity {
		get => Instance.m_MouseSensitivity == default ?
			Instance.m_MouseSensitivity = PlayerPrefs.GetFloat(SensitivityKey, SensitivityValue) :
			Instance.m_MouseSensitivity;
		set => PlayerPrefs.SetFloat(SensitivityKey, Instance.m_MouseSensitivity = value);
	}

	public static uint KeyNext {
		get         => Instance.m_KeyNext;
		private set => Instance.m_KeyNext = value;
	}
	public static uint KeyPrev {
		get         => Instance.m_KeyPrev;
		private set => Instance.m_KeyPrev = value;
	}
	public static Vector2 LookDirection {
		get         => Instance.m_LookDirection;
		private set => Instance.m_LookDirection = value;
	}
	public static Vector2 MoveDirection {
		get         => Instance.m_MoveDirection;
		private set => Instance.m_MoveDirection = value;
	}
	public static Vector2 PointPosition {
		get         => Instance.m_PointPosition;
		private set => Instance.m_PointPosition = value;
	}
	public static Vector2 ScrollWheel {
		get         => Instance.m_ScrollWheel;
		private set => Instance.m_ScrollWheel = value;
	}
	public static Vector2 Navigate {
		get         => Instance.m_Navigate;
		private set => Instance.m_Navigate = value;
	}

	public static string KeyPressed {
		get         => Instance.m_KeyPressed;
		private set => Instance.m_KeyPressed = value;
	}



	// Key State Methods

	static void RegisterActionMap() {
		if (InputActionAsset == null) return;
		foreach (var inputActionMap in InputActionAsset.actionMaps) {
			if (!Enum.TryParse(inputActionMap.name, out ActionMap actionMap)) continue;
			foreach (var inputAction in inputActionMap.actions) {
				if (!Enum.TryParse(inputAction.name, out KeyAction keyAction)) continue;

				int index = (int)keyAction;
				inputAction.started += callback => KeyNext |= 1u << index;
				inputAction.performed += keyAction switch {
					KeyAction.Look        => callback => LookDirection = callback.ReadValue<Vector2>(),
					KeyAction.Move        => callback => MoveDirection = callback.ReadValue<Vector2>(),
					KeyAction.Point       => callback => PointPosition = callback.ReadValue<Vector2>(),
					KeyAction.ScrollWheel => callback => ScrollWheel   = callback.ReadValue<Vector2>(),
					KeyAction.Navigate    => callback => Navigate      = callback.ReadValue<Vector2>(),
					_ => callback => _ = callback.action.IsPressed() switch {
						true  => KeyNext |=  (1u << index),
						false => KeyNext &= ~(1u << index),
					},
				};
				inputAction.canceled += keyAction switch {
					KeyAction.Look        => callback => LookDirection = Vector2.zero,
					KeyAction.Move        => callback => MoveDirection = Vector2.zero,
					KeyAction.Point       => callback => PointPosition = Vector2.zero,
					KeyAction.ScrollWheel => callback => ScrollWheel   = Vector2.zero,
					KeyAction.Navigate    => callback => Navigate      = Vector2.zero,
					_ => callback => KeyNext &= ~(1u << index),
				};
			}
		}
		InputSystem.onBeforeUpdate += () => KeyPrev = KeyNext;
		InputSystem.onActionChange += (obj, change) => {
			if (change != InputActionChange.ActionPerformed) return;
			var inputAction = obj as InputAction;
			if (inputAction?.activeControl == null) return;
			var device = inputAction.activeControl.device;
			IsPointing = device is Pointer;
		};
	}

	static bool GetKeyNext(KeyAction key) => (KeyNext & (1u << (int)key)) != 0u;
	static bool GetKeyPrev(KeyAction key) => (KeyPrev & (1u << (int)key)) != 0u;

	public static bool GetKey(KeyAction key) => GetKeyNext(key);
	public static bool GetKeyDown(KeyAction key) => GetKeyNext(key) && !GetKeyPrev(key);
	public static bool GetKeyUp(KeyAction key) => !GetKeyNext(key) && GetKeyPrev(key);

	public static void SwitchActionMap(ActionMap actionMap, bool hideCursor = false) {
		if (InputActionAsset == null) return;
		PlayerInput.currentActionMap = InputActionAsset.FindActionMap(actionMap.ToString());
		KeyPrev = KeyNext = default;
		LookDirection = MoveDirection = PointPosition = ScrollWheel = Navigate = default;
		Cursor.lockState = hideCursor ? CursorLockMode.Locked : CursorLockMode.None;
		Cursor.visible = !hideCursor;
	}



	// Key Binding Methods

	public static List<string> GetKeysBinding(KeyAction keyAction) {
		var keys = new List<string>();
		if (InputActionAsset == null) return keys;
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
		if (InputActionAsset == null) return;
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

	/*static void SyncMoveBinding() {
		var inputAction = InputActionAsset.FindAction(KeyAction.Move.ToString());
		if (inputAction != null) {
			for (int i = inputAction.bindings.Count - 1; -1 < i; i--) {
				bool isComposite = inputAction.bindings[i].isComposite;
				bool is2DVector = inputAction.bindings[i].name.Equals("2DVector");
				if (isComposite && is2DVector) inputAction.ChangeBinding(i).Erase();
			}
			var composite = inputAction.AddCompositeBinding("2DVector");
			var keysUp = GetKeysBinding(KeyAction.MoveUp);
			var keysLeft = GetKeysBinding(KeyAction.MoveLeft);
			var keysDown = GetKeysBinding(KeyAction.MoveDown);
			var keysRight = GetKeysBinding(KeyAction.MoveRight);
			foreach (var key in keysUp) composite.With("Up", "<Keyboard>/" + key);
			foreach (var key in keysLeft) composite.With("Left", "<Keyboard>/" + key);
			foreach (var key in keysDown) composite.With("Down", "<Keyboard>/" + key);
			foreach (var key in keysRight) composite.With("Right", "<Keyboard>/" + key);
		}
	}*/



	// Key Record Methods

	public static void BeginRecordKey() => KeyPressed = "";
	public static void EndRecordKey() => KeyPressed = null;

	static void RegisterKeyRecord() {
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
		MouseSensitivity = MouseSensitivity;
		RegisterActionMap();
		RegisterKeyRecord();
	}
}

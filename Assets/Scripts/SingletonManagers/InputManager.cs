using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif



public enum ActionMap {
	Player,
	UI,
}

public enum KeyAction {
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
	Map,
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



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Input Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Singleton Manager/Input Manager")]
[RequireComponent(typeof(PlayerInput))]
public sealed class InputManager : MonoSingleton<InputManager> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(InputManager))]
	class InputManagerEditor : EditorExtensions {
		InputManager I => target as InputManager;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Player Input", EditorStyles.boldLabel);
			ObjectField("Player Input", PlayerInput);
			InputActionAsset = ObjectField("Input Action Asset", InputActionAsset);
			if (InputActionAsset == null) {
				var message = string.Empty;
				message += $"Input Action Asset is missing.\n";
				message += $"Please assign a Input Action Asset to here.";
				HelpBox(message, MessageType.Info);
				Space();
			}

			End();
		}
	}
	#endif



	// Constants

	const string SensitivityK = "Sensitivity";
	const float SensitivityD = 1f;



	// Fields

	PlayerInput m_PlayerInput;

	float? m_Sensitivity;
	bool m_IsPointerMode;
	uint m_KeyNext;
	uint m_KeyPrev;
	Vector2 m_LookDirection;
	Vector2 m_MoveDirection;
	Vector2 m_PointPosition;
	Vector2 m_ScrollWheel;
	Vector2 m_Navigate;
	string m_KeyPressed;



	// Properties

	static PlayerInput PlayerInput => !Instance.m_PlayerInput ?
		Instance.m_PlayerInput = Instance.GetOwnComponent<PlayerInput>() :
		Instance.m_PlayerInput;

	static InputActionAsset InputActionAsset {
		get => PlayerInput.actions;
		set => PlayerInput.actions = value;
	}



	public static float Sensitivity {
		get => Instance.m_Sensitivity ??= PlayerPrefs.GetFloat(SensitivityK, SensitivityD);
		set => PlayerPrefs.SetFloat(SensitivityK, (Instance.m_Sensitivity = value).Value);
	}
	public static bool IsPointerMode {
		get         => Instance.m_IsPointerMode;
		private set => Instance.m_IsPointerMode = value;
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

	public static void LockCursor(bool lockCursor = true) {
		Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
		Cursor.visible = !lockCursor;
	}

	static void RegisterActionMap() {
		if (InputActionAsset == null) return;
		foreach (var inputActionMap in InputActionAsset.actionMaps) {
			if (!Enum.TryParse(inputActionMap.name, out ActionMap actionMap)) continue;
			foreach (var inputAction in inputActionMap.actions) {
				if (!Enum.TryParse(inputAction.name, out KeyAction keyAction)) continue;

				int index = (int)keyAction;
				inputAction.started += action => KeyNext |= 1u << index;
				inputAction.performed += keyAction switch {
					KeyAction.Look        => action => LookDirection = action.ReadValue<Vector2>(),
					KeyAction.Move        => action => MoveDirection = action.ReadValue<Vector2>(),
					KeyAction.Point       => action => PointPosition = action.ReadValue<Vector2>(),
					KeyAction.ScrollWheel => action => ScrollWheel   = action.ReadValue<Vector2>(),
					KeyAction.Navigate    => action => Navigate      = action.ReadValue<Vector2>(),
					_ => action => _ = action.action.IsPressed() switch {
						true  => KeyNext |=  (1u << index),
						false => KeyNext &= ~(1u << index),
					},
				};
				inputAction.canceled += keyAction switch {
					KeyAction.Look        => action => LookDirection = Vector2.zero,
					KeyAction.Move        => action => MoveDirection = Vector2.zero,
					KeyAction.Point       => action => PointPosition = Vector2.zero,
					KeyAction.ScrollWheel => action => ScrollWheel   = Vector2.zero,
					KeyAction.Navigate    => action => Navigate      = Vector2.zero,
					_ => action => KeyNext &= ~(1u << index),
				};
			}
		}
		InputSystem.onBeforeUpdate += () => KeyPrev = KeyNext;
		InputSystem.onActionChange += (obj, change) => {
			if (change != InputActionChange.ActionPerformed) return;
			var inputAction = obj as InputAction;
			if (inputAction?.activeControl == null) return;
			var device = inputAction.activeControl.device;
			IsPointerMode = device is Pointer;
		};
	}

	public static void SwitchActionMap(ActionMap actionMap) {
		if (InputActionAsset == null) return;
		PlayerInput.currentActionMap = InputActionAsset.FindActionMap(actionMap.ToString());
		KeyPrev = KeyNext = default;
		LookDirection = MoveDirection = PointPosition = ScrollWheel = Navigate = default;
	}

	static bool GetKeyNext(KeyAction key) => (KeyNext & (1u << (int)key)) != 0u;
	static bool GetKeyPrev(KeyAction key) => (KeyPrev & (1u << (int)key)) != 0u;

	public static bool GetKey(KeyAction key) => GetKeyNext(key);
	public static bool GetKeyDown(KeyAction key) => GetKeyNext(key) && !GetKeyPrev(key);
	public static bool GetKeyUp(KeyAction key) => !GetKeyNext(key) && GetKeyPrev(key);



	// Key Binding Methods

	public static void GetKeysBinding(KeyAction keyAction, List<string> keys) {
		if (InputActionAsset == null) return;
		var inputAction = InputActionAsset.FindAction(keyAction.ToString());
		if (inputAction != null) {
			keys.Clear();
			for (int i = 0; i < inputAction.bindings.Count; i++) {
				var split = inputAction.bindings[i].path.Split('/');
				if (split[0].Equals("<Keyboard>")) keys.Add(split[1]);
				// path: "<Device>/Key"
			}
		}
	}

	public static void SetKeysBinding(KeyAction keyAction, List<string> keys) {
		if (InputActionAsset == null) return;
		var inputAction = InputActionAsset.FindAction(keyAction.ToString());
		if (inputAction != null) {
			for (int i = inputAction.bindings.Count; 0 < i--;) {
				var split = inputAction.bindings[i].path.Split('/');
				if (split[0].Equals("<Keyboard>")) inputAction.ChangeBinding(i).Erase();
				// path: "<Device>/Key"
			}
			if (keys != null) foreach (string key in keys) {
				inputAction.AddBinding("<Keyboard>/" + key);
			}
		}
	}

	/*static void SyncMoveBinding() {
		var inputAction = InputActionAsset.FindAction(KeyAction.Move.ToString());
		if (inputAction != null) {
			for (int i = inputAction.bindings.Count; 0 < i--;) {
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
			if (KeyPressed == null) return;
			var split = inputControl.path.Split('/');
			if (split[1].Equals("Keyboard")) KeyPressed = split[2];
			// path: "/Device/Key"
		});
	}



	// Lifecycle

	void Start() {
		Sensitivity = Sensitivity;
		RegisterActionMap();
		RegisterKeyRecord();
	}
}

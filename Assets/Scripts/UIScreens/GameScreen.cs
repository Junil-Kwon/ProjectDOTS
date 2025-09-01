using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game Screen
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI Screen/Game Screen")]
public sealed class GameScreen : ScreenBase {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(GameScreen))]
	class GameScreenEditor : EditorExtensions {
		GameScreen I => target as GameScreen;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Selected", EditorStyles.boldLabel);
			I.DefaultSelected = ObjectField("Default Selected", I.DefaultSelected);
			Space();

			LabelField("Game", EditorStyles.boldLabel);
			I.AutoPause = Toggle("Auto Pause", I.AutoPause);
			I.LocalPlayer = ObjectField("Local Player", I.LocalPlayer);
			I.RemotePlayerTemplate = ObjectField("Remote Player Template", I.RemotePlayerTemplate);
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] bool m_AutoPause = true;

	[SerializeField] PlayerStatus m_LocalPlayer;
	[SerializeField] PlayerStatus m_RemotePlayerTemplate;
	List<PlayerStatus> m_RemotePlayerList = new();
	Stack<PlayerStatus> m_RemotePlayerPool = new();



	// Properties

	public override bool IsPrimary => true;
	public override bool IsOverlay => false;
	public override bool UseScreenBlur => false;



	bool AutoPause {
		get => m_AutoPause;
		set => m_AutoPause = value;
	}



	PlayerStatus LocalPlayer {
		get => m_LocalPlayer;
		set => m_LocalPlayer = value;
	}
	PlayerStatus RemotePlayerTemplate {
		get => m_RemotePlayerTemplate;
		set => m_RemotePlayerTemplate = value;
	}
	List<PlayerStatus> RemotePlayerList {
		get => m_RemotePlayerList;
	}
	Stack<PlayerStatus> RemotePlayerPool {
		get => m_RemotePlayerPool;
	}



	// Methods

	public override void Show() {
		base.Show();
		if (GameManager.GameState == GameState.Paused) {
			GameManager.GameState = GameState.Gameplay;
		}
	}

	public override void Hide() {
		base.Hide();
		if (GameManager.GameState == GameState.Gameplay) {
			GameManager.GameState = GameState.Paused;
		}
	}

	public override void Back() {
		UIManager.OpenScreen(Screen.Menu);
	}



	// Instance Methods

	PlayerStatus GetOrCreateInstance(int index) {
		PlayerStatus instance;
		while (RemotePlayerPool.TryPop(out instance) && instance == null);
		if (instance == null) instance = Instantiate(RemotePlayerTemplate, transform);
		var instanceTransform = (RectTransform)instance.transform;
		var templateTransform = (RectTransform)RemotePlayerTemplate.transform;
		var position = templateTransform.anchoredPosition;
		position.y += index * templateTransform.rect.height;
		instanceTransform.anchoredPosition = position;
		instance.gameObject.SetActive(true);
		return instance;
	}

	void RemoveInstance(PlayerStatus instance) {
		instance.gameObject.SetActive(false);
		RemotePlayerPool.Push(instance);
	}



	// Status Methods

	void UpdatePlayerStatus() {
		if (GameManager.LocalPlayer == null) {
			if (LocalPlayer.gameObject.activeSelf) LocalPlayer.gameObject.SetActive(false);
		} else {
			if (!LocalPlayer.gameObject.activeSelf) LocalPlayer.gameObject.SetActive(true);
			LocalPlayer.StatusBlob = GameManager.LocalPlayer.Value.StatusBlob;
			LocalPlayer.StatusData = GameManager.LocalPlayer.Value.StatusData;
		}
		for (int i = 0; i < GameManager.RemotePlayers.Count; i++) {
			if (RemotePlayerList.Count == i) RemotePlayerList.Add(GetOrCreateInstance(i));
			RemotePlayerList[i].StatusBlob = GameManager.RemotePlayers[i].StatusBlob;
			RemotePlayerList[i].StatusData = GameManager.RemotePlayers[i].StatusData;
		}
		while (GameManager.RemotePlayers.Count < RemotePlayerList.Count) {
			int l = RemotePlayerList.Count - 1;
			RemoveInstance(RemotePlayerList[l]);
			RemotePlayerList.RemoveAt(l);
		}
	}



	// Lifecycle

	protected override void Update() {
		switch (UIManager.CurrentScreen) {
			case Screen.Debug:
			case Screen.Game:
			case Screen.Map: {
				bool match = false;
				match = match || InputManager.GetKeyUp(KeyAction.Menu);
				match = match || (AutoPause && !Application.isFocused);
				if (match) Back();
				match = true;
				match = match && InputManager.GetKeyDown(KeyAction.Map);
				match = match && UIManager.CurrentScreen != Screen.Map;
				if (match) UIManager.OpenScreen(Screen.Map);

				if (GameManager.GameState == GameState.Paused) {
					GameManager.GameState = GameState.Gameplay;
				}
			} break;
			default: {
				if (GameManager.GameState == GameState.Gameplay) {
					GameManager.GameState = GameState.Paused;
				}
			} break;
		}
		UpdatePlayerStatus();
	}
}

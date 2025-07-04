using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Multiplayer Canvas
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI/Multiplayer Canvas")]
public class MultiplayerCanvas : BaseCanvas {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(MultiplayerCanvas))]
	class MultiplayerCanvasEditor : EditorExtensions {
		MultiplayerCanvas I => target as MultiplayerCanvas;
		public override void OnInspectorGUI() {
			Begin("Multiplayer Canvas");

			if (I.Raycaster && I.Raycaster.enabled) {
				LabelField("Selected", EditorStyles.boldLabel);
				I.FirstSelected = ObjectField("First Selected", I.FirstSelected);
				Space();
			}
			LabelField("Layout", EditorStyles.boldLabel);
			PropertyField("m_JoinRelayServerLayout");
			PropertyField("m_CreateRelayHostLayout");
			PropertyField("m_JoinLocalServerLayout");
			PropertyField("m_CreateLocalHostLayout");
			Space();
			LabelField("Switch Navigation", EditorStyles.boldLabel);
			PropertyField("m_TopSelectables");
			for (int i = 0; i < I.SelectOnDown.Length; i++) {
				I.SelectOnDown[i] = ObjectField($"Select On Down [{i}]", I.SelectOnDown[i]);
			}
			PropertyField("m_BottomSelectables");
			for (int i = 0; i < I.SelectOnUp.Length; i++) {
				I.SelectOnUp[i] = ObjectField($"Select On Up [{i}]", I.SelectOnUp[i]);
			}
			Space();
			LabelField("Connection", EditorStyles.boldLabel);
			PropertyField("m_JoinRelayServerJoinCodeInputfield");
			PropertyField("m_CreateRelayHostMaxPlayersSlider");
			PropertyField("m_JoinLocalServerIPAddressInputfield");
			PropertyField("m_JoinLocalServerPortInputfield");
			PropertyField("m_CreateLocalHostMaxPlayersSlider");
			PropertyField("m_CreateLocalHostPortInputfield");
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] GameObject m_JoinRelayServerLayout;
	[SerializeField] GameObject m_CreateRelayHostLayout;
	[SerializeField] GameObject m_JoinLocalServerLayout;
	[SerializeField] GameObject m_CreateLocalHostLayout;

	[SerializeField] Selectable[] m_TopSelectables;
	[SerializeField] Selectable[] m_SelectOnDown = new Selectable[4];
	[SerializeField] Selectable[] m_BottomSelectables;
	[SerializeField] Selectable[] m_SelectOnUp = new Selectable[4];

	[SerializeField] CustomSlider m_CreateRelayHostMaxPlayersSlider;
	[SerializeField] CustomSlider m_CreateLocalHostMaxPlayersSlider;
	[SerializeField] CustomInputfield m_JoinRelayServerJoinCodeInputfield;
	[SerializeField] CustomInputfield m_JoinLocalServerIPAddressInputfield;
	[SerializeField] CustomInputfield m_JoinLocalServerPortInputfield;
	[SerializeField] CustomInputfield m_CreateLocalHostPortInputfield;



	// Properties

	Selectable[] TopSelectables    => m_TopSelectables;
	Selectable[] SelectOnDown      => m_SelectOnDown;
	Selectable[] BottomSelectables => m_BottomSelectables;
	Selectable[] SelectOnUp        => m_SelectOnUp;

	int LayoutIndex {
		get {
			if (m_JoinRelayServerLayout.activeSelf) return 0;
			if (m_CreateRelayHostLayout.activeSelf) return 1;
			if (m_JoinLocalServerLayout.activeSelf) return 2;
			if (m_CreateLocalHostLayout.activeSelf) return 3;
			return -1;
		}
		set {
			foreach (var selectable in TopSelectables) {
				var navigation = selectable.navigation;
				navigation.selectOnDown = SelectOnDown[value];
				selectable.navigation = navigation;
			}
			foreach (var selectable in BottomSelectables) {
				var navigation = selectable.navigation;
				navigation.selectOnUp = SelectOnUp[value];
				selectable.navigation = navigation;
			}
			m_JoinRelayServerLayout.SetActive(value == 0);
			m_CreateRelayHostLayout.SetActive(value == 1);
			m_JoinLocalServerLayout.SetActive(value == 2);
			m_CreateLocalHostLayout.SetActive(value == 3);
		}
	}



	int CreateRelayHostMaxPlayers => (int)m_CreateRelayHostMaxPlayersSlider.CurrentValue;
	int CreateLocalHostMaxPlayers => (int)m_CreateLocalHostMaxPlayersSlider.CurrentValue;

	string JoinRelayServerJoinCode {
		get {
			string text = m_JoinRelayServerJoinCodeInputfield.CurrentValue;
			if (string.IsNullOrEmpty(text)) text = m_JoinRelayServerJoinCodeInputfield.PlaceHolder;
			return text;
		}
	}
	ushort CreateLocalHostPort {
		get {
			string text = m_CreateLocalHostPortInputfield.CurrentValue;
			if (string.IsNullOrEmpty(text)) text = m_CreateLocalHostPortInputfield.PlaceHolder;
			return ushort.TryParse(text, out ushort port) ? port : default;
		}
	}
	string JoinLocalServerIPAddress {
		get {
			string text = m_JoinLocalServerIPAddressInputfield.CurrentValue;
			if (string.IsNullOrEmpty(text)) text = m_JoinLocalServerIPAddressInputfield.PlaceHolder;
			return text;
		}
	}
	ushort JoinLocalServerPort {
		get {
			string text = m_JoinLocalServerPortInputfield.CurrentValue;
			if (string.IsNullOrEmpty(text)) text = m_JoinLocalServerPortInputfield.PlaceHolder;
			return ushort.TryParse(text, out ushort port) ? port : default;
		}
	}



	// Methods

	public void SwitchSection(int value) => LayoutIndex = value switch {
		0 => (LayoutIndex / 2 == 0) ? 0 : 2,
		1 => (LayoutIndex / 2 == 0) ? 1 : 3,
		_ => LayoutIndex,
	};

	public void SwitchConnection(bool value) => LayoutIndex = value switch {
		true  => (LayoutIndex % 2 == 0) ? 2 : 3,
		false => (LayoutIndex % 2 == 0) ? 0 : 1,
	};

	public void CreateRelayHost() {
		NetworkManager.CreateRelayHost(CreateRelayHostMaxPlayers);
		UIManager.Back();
	}
	public void JoinRelayServer() {
		NetworkManager.JoinRelayServer(JoinRelayServerJoinCode);
		UIManager.Back();
	}
	public void CreateLocalHost() {
		NetworkManager.CreateLocalHost(CreateLocalHostPort, CreateLocalHostMaxPlayers);
		UIManager.Back();
	}
	public void JoinLocalServer() {
		NetworkManager.JoinLocalServer(JoinLocalServerIPAddress, JoinLocalServerPort);
		UIManager.Back();
	}

	public override void Back() {
		UIManager.PopOverlay();
	}



	// Lifecycle

	void Start() => LayoutIndex = 0;
}

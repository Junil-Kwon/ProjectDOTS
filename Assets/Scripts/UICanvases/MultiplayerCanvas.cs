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

				LabelField("Selected", EditorStyles.boldLabel);
				I.FirstSelected = ObjectField("First Selected", I.FirstSelected);
				Space();
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



	// Constants

	const string DefaultIPAddress = "127.0.0.1";
	const ushort DefaultPort      = 7979;



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



	int CreateRelayHostMaxPlayers => (int)m_CreateRelayHostMaxPlayersSlider.Value;
	int CreateLocalHostMaxPlayers => (int)m_CreateLocalHostMaxPlayersSlider.Value;

	string JoinRelayServerJoinCode {
		get {
			string text = m_JoinRelayServerJoinCodeInputfield.Value;
			if (!string.IsNullOrEmpty(text)) return text;
			return default;
		}
	}
	ushort CreateLocalHostPort {
		get {
			string text = m_CreateLocalHostPortInputfield.Value;
			if (ushort.TryParse(text, out ushort port)) return port;
			return DefaultPort;
		}
	}
	string JoinLocalServerIPAddress {
		get {
			string text = m_JoinLocalServerIPAddressInputfield.Value;
			if (!string.IsNullOrEmpty(text)) return text;
			return DefaultIPAddress;
		}
	}
	ushort JoinLocalServerPort {
		get {
			string text = m_JoinLocalServerPortInputfield.Value;
			if (ushort.TryParse(text, out ushort port)) return port;
			return DefaultPort;
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
	}
	public void JoinRelayServer() {
		NetworkManager.JoinRelayServer(JoinRelayServerJoinCode);
	}
	public void CreateLocalHost() {
		NetworkManager.CreateLocalHost(CreateLocalHostPort, CreateLocalHostMaxPlayers);
	}
	public void JoinLocalServer() {
		NetworkManager.JoinLocalServer(JoinLocalServerIPAddress, JoinLocalServerPort);
	}



	// Lifecycle

	void Start() => LayoutIndex = 0;
}

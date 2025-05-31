using UnityEngine;

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

				LabelField("Layouts", EditorStyles.boldLabel);
				PropertyField("m_CreateRelayHostLayout");
				PropertyField("m_JoinRelayServerLayout");
				PropertyField("m_CreateLocalHostLayout");
				PropertyField("m_JoinLocalServerLayout");
				Space();
				LabelField("Sliders", EditorStyles.boldLabel);
				PropertyField("m_CreateRelayHostMaxPlayersSlider");
				PropertyField("m_CreateLocalHostMaxPlayersSlider");
				Space();
				LabelField("Inputfields", EditorStyles.boldLabel);
				PropertyField("m_JoinRelayServerJoinCodeInputfield");
				PropertyField("m_CreateLocalHostPortInputfield");
				PropertyField("m_JoinLocalServerIPAddressInputfield");
				PropertyField("m_JoinLocalServerPortInputfield");
				Space();

				End();
			}
		}
	#endif



	// Constants

	const string DefaultIPAddress = "127.0.0.1";
	const ushort DefaultPort      = 7979;



	// Fields

	[SerializeField] GameObject m_CreateRelayHostLayout;
	[SerializeField] GameObject m_JoinRelayServerLayout;
	[SerializeField] GameObject m_CreateLocalHostLayout;
	[SerializeField] GameObject m_JoinLocalServerLayout;

	[SerializeField] CustomSlider m_CreateRelayHostMaxPlayersSlider;
	[SerializeField] CustomSlider m_CreateLocalHostMaxPlayersSlider;

	[SerializeField] CustomInputfield m_JoinRelayServerJoinCodeInputfield;
	[SerializeField] CustomInputfield m_CreateLocalHostPortInputfield;
	[SerializeField] CustomInputfield m_JoinLocalServerIPAddressInputfield;
	[SerializeField] CustomInputfield m_JoinLocalServerPortInputfield;



	// Properties

	int LayoutIndex {
		get {
			if (m_CreateRelayHostLayout.activeSelf) return 0;
			if (m_JoinRelayServerLayout.activeSelf) return 1;
			if (m_CreateLocalHostLayout.activeSelf) return 2;
			if (m_JoinLocalServerLayout.activeSelf) return 3;
			return -1;
		}
		set {
			m_CreateRelayHostLayout.SetActive(value == 0);
			m_JoinRelayServerLayout.SetActive(value == 1);
			m_CreateLocalHostLayout.SetActive(value == 2);
			m_JoinLocalServerLayout.SetActive(value == 3);
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

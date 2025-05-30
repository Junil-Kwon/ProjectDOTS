using UnityEngine;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Sound Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/Sound Manager")]
public class SoundManager : MonoSingleton<SoundManager> {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(SoundManager))]
		class SoundManagerEditor : EditorExtensions {
			SoundManager I => target as SoundManager;
			public override void OnInspectorGUI() {
				Begin("Sound Manager");

				LabelField("Volume", EditorStyles.boldLabel);
				Music   = Slider("Music",    Music,   0.0f, 1.5f);
				SoundFX = Slider("Sound FX", SoundFX, 0.0f, 1.5f);
				Space();

				End();
			}
		}
	#endif



	// Constants

	const string MusicKey   = "Music";
	const string SoundFXKey = "SoundFX";



	// Methods

	[SerializeField] float m_DefaultMusic   = 1f;
	[SerializeField] float m_DefaultSoundFX = 1f;

	float m_Music;
	float m_SoundFX;



	// Properties

	static float DefaultMusic {
		get => Instance.m_DefaultMusic;
		set => Instance.m_DefaultMusic = value;
	}
	static float DefaultSoundFX {
		get => Instance.m_DefaultSoundFX;
		set => Instance.m_DefaultSoundFX = value;
	}

	public static float Music {
		get {
			if (Instance.m_Music == default) {
				Instance.m_Music = PlayerPrefs.GetFloat(MusicKey, DefaultMusic);
			}
			return Instance.m_Music;
		}
		set {
			PlayerPrefs.SetFloat(MusicKey, Instance.m_Music = value);
		}
	}
	public static float SoundFX {
		get {
			if (Instance.m_SoundFX == default) {
				Instance.m_SoundFX = PlayerPrefs.GetFloat(SoundFXKey, DefaultSoundFX);
			}
			return Instance.m_SoundFX;
		}
		set {
			PlayerPrefs.SetFloat(SoundFXKey, Instance.m_SoundFX = value);
		}
	}
}

using UnityEngine;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Sound Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/Sound Manager")]
public sealed class SoundManager : MonoSingleton<SoundManager> {

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

	const string MusicKey = "Music";
	const string SoundFXKey = "SoundFX";
	const float MusicValue = 1f;
	const float SoundFXValue = 1f;



	// Methods

	float m_Music;
	float m_SoundFX;



	// Properties

	public static float Music {
		get => Instance.m_Music == default ?
			Instance.m_Music = PlayerPrefs.GetFloat(MusicKey, MusicValue) :
			Instance.m_Music;
		set => PlayerPrefs.SetFloat(MusicKey, Instance.m_Music = value);
	}
	public static float SoundFX {
		get => Instance.m_SoundFX == default ?
			Instance.m_SoundFX = PlayerPrefs.GetFloat(SoundFXKey, SoundFXValue) :
			Instance.m_SoundFX;
		set => PlayerPrefs.SetFloat(SoundFXKey, Instance.m_SoundFX = value);
	}
}

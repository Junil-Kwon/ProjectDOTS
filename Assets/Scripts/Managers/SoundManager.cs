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

			I.TrySetInstance();

			End();
		}
	}
	#endif



	// Constants

	const string MusicKey = "Music";
	const float MusicValue = 1f;

	const string SoundFXKey = "SoundFX";
	const float SoundFXValue = 1f;



	// Fields

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



	// Lifecycle

	void Start() {
		Music = Music;
		SoundFX = SoundFX;
	}
}

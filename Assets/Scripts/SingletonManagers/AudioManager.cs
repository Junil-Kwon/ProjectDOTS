using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#if UNITY_EDITOR
using UnityEditor;
#endif



public enum Audio {
	None,
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Audio Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Singleton Manager/Audio Manager")]
public sealed class AudioManager : MonoSingleton<AudioManager> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(AudioManager))]
	class AudioManagerEditor : EditorExtensions {
		AudioManager I => target as AudioManager;
		int Page { get; set; } = 0;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Audio Mixer", EditorStyles.boldLabel);
			AudioMixer   = ObjectField("Audio Mixer",   AudioMixer);
			MusicGroup   = ObjectField("Music Group",   MusicGroup);
			SoundFXGroup = ObjectField("SoundFX Group", SoundFXGroup);
			Space();

			LabelField("Audio Instance", EditorStyles.boldLabel);
			AudioTemplate = ObjectField("Audio Template", AudioTemplate);
			if (AudioTemplate == null) {
				var message = string.Empty;
				message += $"Audio Template is missing.\n";
				message += $"Please assign a Audio Template here.";
				HelpBox(message, MessageType.Info);
				Space();
			} else {
				int num = AudioInstance.Count;
				int den = AudioInstance.Count + AudioPool.Count;
				LabelField("Audio Pool", $"{num} / {den}");
				Space();
			}

			LabelField("Audio Clip", EditorStyles.boldLabel);
			SourcePath = TextField("Source Path", SourcePath);
			BeginHorizontal();
			PrefixLabel("Load Audio Clip");
			if (Button("Clear")) ClearAudioClip();
			if (Button("Load")) LoadAudioClip();
			EndHorizontal();
			Space();

			LabelField("Audio Clip Data", EditorStyles.boldLabel);
			LabelField("Audio Clip Count", $"{ClipList.Count} / {AudioCount}");
			Page = BookField(ClipData, 5, Page, (match, index, value) => {
				BeginDisabledGroup();
				if (match) {
					var text = ((Audio)index).ToString();
					var name = Regex.Replace(text, @"(?<!^)(?=[A-Z])", " ");
					ObjectField(name, value.AudioClip);
					EnumField("State", value.State);
				} else {
					LabelField(" ");
					LabelField(" ");
				}
				EndDisabledGroup();
			});
			Space();

			End();
		}
	}
	#endif



	// Constants

	const string MasterVolumeK = "MasterVolume";
	const float MasterVolumeD = 1f;

	const string MusicVolumeK = "MusicVolume";
	const float MusicVolumeD = 1f;

	const string SoundFXVolumeK = "SoundFXVolume";
	const float SoundFXVolumeD = 1f;



	static readonly int AudioCount = Enum.GetValues(typeof(Audio)).Length;

	enum State : byte {
		Unloaded,
		Loaded,
		Preloaded,
	}

	[Serializable]
	struct ClipEntry {
		public AudioClip AudioClip;
		public State State;
		public float EndTime;
	}

	const int TimeSliceCount = 30;
	const float UnloadThreshold = 30f;



	// Fields

	[SerializeField] AudioMixer m_AudioMixer;
	[SerializeField] AudioMixerGroup m_MusicGroup;
	[SerializeField] AudioMixerGroup m_SoundFXGroup;
	float? m_MasterVolume;
	float? m_MusicVolume;
	float? m_SoundFXVolume;

	[SerializeField] AudioSource m_AudioTemplate;
	Dictionary<uint, (Audio, AudioSource)> m_AudioInstance = new();
	Stack<AudioSource> m_AudioPool = new();
	List<uint> m_IDBuffer = new();
	uint m_NextID;
	uint m_MusicID;

	[SerializeField] string m_SourcePath = "Assets/Audio";
	[SerializeField] List<Audio> m_ClipList = new();
	[SerializeField] ClipEntry[] m_ClipData = new ClipEntry[AudioCount];
	int m_SliceIndex;



	// Properties

	static AudioMixer AudioMixer {
		get => Instance.m_AudioMixer;
		set => Instance.m_AudioMixer = value;
	}
	static AudioMixerGroup MusicGroup {
		get => Instance.m_MusicGroup;
		set => Instance.m_MusicGroup = value;
	}
	static AudioMixerGroup SoundFXGroup {
		get => Instance.m_SoundFXGroup;
		set => Instance.m_SoundFXGroup = value;
	}

	public static float MasterVolume {
		get => Instance.m_MasterVolume ??= PlayerPrefs.GetFloat(MasterVolumeK, MasterVolumeD);
		set {
			PlayerPrefs.SetFloat(MasterVolumeK, (Instance.m_MasterVolume = value).Value);
			AudioMixer?.SetFloat(MasterVolumeK, Mathf.Log10(Mathf.Max(0.00001f, value)) * 20f);
		}
	}
	public static float MusicVolume {
		get => Instance.m_MusicVolume ??= PlayerPrefs.GetFloat(MusicVolumeK, MusicVolumeD);
		set {
			PlayerPrefs.SetFloat(MusicVolumeK, (Instance.m_MusicVolume = value).Value);
			AudioMixer?.SetFloat(MusicVolumeK, Mathf.Log10(Mathf.Max(0.00001f, value)) * 20f);
		}
	}
	public static float SoundFXVolume {
		get => Instance.m_SoundFXVolume ??= PlayerPrefs.GetFloat(SoundFXVolumeK, SoundFXVolumeD);
		set {
			PlayerPrefs.SetFloat(SoundFXVolumeK, (Instance.m_SoundFXVolume = value).Value);
			AudioMixer?.SetFloat(SoundFXVolumeK, Mathf.Log10(Mathf.Max(0.00001f, value)) * 20f);
		}
	}



	static AudioSource AudioTemplate {
		get => Instance.m_AudioTemplate;
		set => Instance.m_AudioTemplate = value;
	}
	static Dictionary<uint, (Audio, AudioSource)> AudioInstance {
		get => Instance.m_AudioInstance;
	}
	static Stack<AudioSource> AudioPool {
		get => Instance.m_AudioPool;
	}
	static List<uint> IDBuffer {
		get => Instance.m_IDBuffer;
	}
	static uint NextID {
		get => Instance.m_NextID;
		set => Instance.m_NextID = value;
	}
	static uint MusicID {
		get => Instance.m_MusicID;
		set => Instance.m_MusicID = value;
	}



	static string SourcePath {
		get => Instance.m_SourcePath;
		set => Instance.m_SourcePath = value;
	}
	static List<Audio> ClipList {
		get => Instance.m_ClipList;
	}
	static ClipEntry[] ClipData {
		get => Instance.m_ClipData;
		set => Instance.m_ClipData = value;
	}

	static int SliceIndex {
		get => Instance.m_SliceIndex;
		set => Instance.m_SliceIndex = value;
	}



	// Data Methods

	#if UNITY_EDITOR
	static void ClearAudioClip() {
		ClipList.Clear();
		ClipData = new ClipEntry[AudioCount];
	}

	static void LoadAudioClip() {
		ClearAudioClip();
		foreach (var clip in LoadAssets<AudioClip>(SourcePath)) {
			if (!Enum.TryParse(clip.name, out Audio audio)) continue;
			ClipList.Add(audio);
			ClipData[(int)audio] = new ClipEntry {
				AudioClip = clip,
				State = clip.preloadAudioData ? State.Preloaded : State.Unloaded,
			};
		}
		ClipList.TrimExcess();
	}

	static T[] LoadAssets<T>(string path) where T : UnityEngine.Object {
		var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { path });
		var assets = new T[guids.Length];
		for (int i = 0; i < guids.Length; i++) {
			var assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
			assets[i] = AssetDatabase.LoadAssetAtPath<T>(assetPath);
		}
		return assets;
	}
	#endif



	// Instance Methods

	static (uint, AudioSource) GetOrCreateInstance(Audio audio) {
		AudioSource instance;
		while (AudioPool.TryPop(out instance) && instance == null);
		if (instance == null) instance = Instantiate(AudioTemplate);
		ref var data = ref ClipData[(int)audio];
		if (data.AudioClip) {
			instance.clip = data.AudioClip;
			if (data.State == State.Unloaded) {
				data.State = State.Loaded;
				data.AudioClip.LoadAudioData();
			}
		}
		instance.gameObject.SetActive(true);
		while (++NextID == default || AudioInstance.ContainsKey(NextID));
		AudioInstance.Add(NextID, (audio, instance));
		return (NextID, instance);
	}

	static void UpdateInstances() {
		foreach (var (audioID, (audio, instance)) in AudioInstance) {
			if (instance) {
				ref var data = ref ClipData[(int)audio];
				if (instance.isPlaying) data.EndTime = Time.time;
				else {
					var state = data.AudioClip.loadState;
					if (state != AudioDataLoadState.Loading) IDBuffer.Add(audioID);
				}
			} else IDBuffer.Add(audioID);
		}
		if (0 < IDBuffer.Count) {
			foreach (var audioID in IDBuffer) RemoveInstance(audioID);
			IDBuffer.Clear();
		}
		int lastIndex = Mathf.Min(SliceIndex + TimeSliceCount, ClipList.Count);
		for (; SliceIndex < lastIndex; SliceIndex++) {
			ref var data = ref ClipData[(int)ClipList[SliceIndex]];
			if (data.State == State.Loaded && UnloadThreshold <= Time.time - data.EndTime) {
				data.State = State.Unloaded;
				data.AudioClip.UnloadAudioData();
			}
		}
		if (SliceIndex == ClipList.Count) SliceIndex = 0;
	}

	static void RemoveInstance(uint audioID) {
		var (audio, instance) = AudioInstance[audioID];
		if (instance) {
			var outputAudioMixerGroup = AudioTemplate.outputAudioMixerGroup;
			if (instance.outputAudioMixerGroup != outputAudioMixerGroup) {
				instance.outputAudioMixerGroup = outputAudioMixerGroup;
			}
			var loop = AudioTemplate.loop;
			if (instance.loop != loop) {
				instance.loop = loop;
			}
			var volume = AudioTemplate.volume;
			if (instance.volume != volume) {
				instance.volume = volume;
			}
			var spatialBlend = AudioTemplate.spatialBlend;
			if (instance.spatialBlend != spatialBlend) {
				instance.spatialBlend = spatialBlend;
			}
			var spread = AudioTemplate.spread;
			if (instance.spread != spread) {
				instance.spread = spread;
			}
			var minDistance = AudioTemplate.minDistance;
			if (instance.minDistance != minDistance) {
				instance.minDistance = minDistance;
			}
			var maxDistance = AudioTemplate.maxDistance;
			if (instance.maxDistance != maxDistance) {
				instance.maxDistance = maxDistance;
			}
			instance.gameObject.SetActive(false);
			AudioPool.Push(instance);
		}
		AudioInstance.Remove(audioID);
	}



	// Audio Methods

	public static uint PlayMusic(Audio audio, float volume = 1f) {
		StopAudio(MusicID);
		var (audioID, instance) = GetOrCreateInstance(audio);
		instance.outputAudioMixerGroup = MusicGroup;
		instance.loop = true;
		instance.volume = volume;
		instance.Play();
		return MusicID = audioID;
	}

	public static uint PlaySoundFX(Audio audio, float volume = 1f) {
		var (audioID, instance) = GetOrCreateInstance(audio);
		instance.outputAudioMixerGroup = SoundFXGroup;
		instance.volume = volume;
		instance.Play();
		return audioID;
	}

	public static uint PlayPointSoundFX(
		Audio audio, Vector3 position, float volume = 1f, float spread = 0f,
		float minDistance = default, float maxDistance = default) {
		var (audioID, instance) = GetOrCreateInstance(audio);
		instance.transform.position = position;
		instance.outputAudioMixerGroup = SoundFXGroup;
		instance.volume = volume;
		instance.spatialBlend = 1f;
		instance.spread = spread;
		if (minDistance != default) instance.minDistance = minDistance;
		if (maxDistance != default) instance.maxDistance = maxDistance;
		instance.Play();
		return audioID;
	}

	public static uint PlayBlendSoundFX(
		Audio audio, Vector3 position, float volume = 1f, float spatialBlend = 0.5f) {
		var (audioID, instance) = GetOrCreateInstance(audio);
		instance.transform.position = position;
		instance.outputAudioMixerGroup = SoundFXGroup;
		instance.volume = volume;
		instance.spatialBlend = spatialBlend;
		instance.Play();
		return audioID;
	}

	public static void SetAudioPosition(uint audioID, Vector3 position) {
		if (AudioInstance.TryGetValue(audioID, out var value)) {
			var (audio, instance) = value;
			instance.transform.position = position;
		}
	}

	public static void SetAudioVolume(uint audioID, float volume) {
		if (AudioInstance.TryGetValue(audioID, out var value)) {
			var (audio, instance) = value;
			instance.volume = volume;
		}
	}

	public static void StopAudio(uint audioID) {
		if (AudioInstance.TryGetValue(audioID, out var value)) {
			var (audio, instance) = value;
			instance.Stop();
			RemoveInstance(audioID);
		}
	}



	// Lifecycle

	void Start() {
		MasterVolume = MasterVolume;
		MusicVolume = MusicVolume;
		SoundFXVolume = SoundFXVolume;
	}

	void LateUpdate() {
		UpdateInstances();
	}
}

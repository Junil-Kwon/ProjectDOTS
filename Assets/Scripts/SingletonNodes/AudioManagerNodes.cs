using UnityEngine;
using UnityEngine.UIElements;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using static EditorVisualElement;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Audio Manager | Play Music
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Audio Manager/Play Music")]
public sealed class PlayMusicEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class PlayMusicEventNode : EventNodeBase {
		PlayMusicEvent I => target as PlayMusicEvent;

		public PlayMusicEventNode() : base() {
			mainContainer.style.width = Node2U;
		}

		public override void ConstructData() {
			var audio = TextEnumField("Audio", I.Audio, value => I.Audio = value);
			var volume = FloatField("Volume", I.Volume, value => I.Volume = value);
			mainContainer.Add(audio);
			mainContainer.Add(volume);
		}

		public override void ConstructPort() {
			CreatePort(Direction.Input);
			CreatePort(Direction.Output);
			CreatePort(Direction.Output, PortType.DataID);
			RefreshExpandedState();
			RefreshPorts();
		}
	}
	#endif



	// Fields

	#if UNITY_EDITOR
	[SerializeField] string m_AudioName;
	#endif

	[SerializeField] Audio m_Audio;
	[SerializeField] float m_Volume = 1f;

	uint m_AudioID;



	// Properties

	#if UNITY_EDITOR
	public Audio Audio {
		get => !Enum.TryParse(m_AudioName, out Audio audio) ?
			Enum.Parse<Audio>(m_AudioName = m_Audio.ToString()) :
			m_Audio = audio;
		set => m_AudioName = (m_Audio = value).ToString();
	}
	#else
	public Audio Audio {
		get => m_Audio;
		set => m_Audio = value;
	}
	#endif

	public float Volume {
		get => m_Volume;
		set => m_Volume = value;
	}

	ref uint AudioID {
		get => ref m_AudioID;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is PlayMusicEvent playMusicEvent) {
			Audio  = playMusicEvent.Audio;
			Volume = playMusicEvent.Volume;
		}
	}

	public override void Start() {
		AudioID = default;
	}

	public override void End() {
		if (AudioID == default) {
			AudioID = AudioManager.PlayMusic(Audio, Volume);
		}
	}

	protected override void GetDataID(ref uint audioID) {
		End();
		if (audioID != default) audioID = AudioID;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Audio Manager | Play Sound FX
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Audio Manager/Play Sound FX")]
public sealed class PlaySoundFXEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class PlaySoundFXEventNode : EventNodeBase {
		PlaySoundFXEvent I => target as PlaySoundFXEvent;

		public PlaySoundFXEventNode() : base() {
			mainContainer.style.width = Node2U;
		}

		public override void ConstructData() {
			var audio = TextEnumField("Audio", I.Audio, value => I.Audio = value);
			var volume = FloatField("Volume", I.Volume, value => I.Volume = value);
			mainContainer.Add(audio);
			mainContainer.Add(volume);
		}

		public override void ConstructPort() {
			CreatePort(Direction.Input);
			CreatePort(Direction.Output);
			CreatePort(Direction.Output, PortType.DataID);
			RefreshExpandedState();
			RefreshPorts();
		}
	}
	#endif



	// Fields

	#if UNITY_EDITOR
	[SerializeField] string m_AudioName;
	#endif

	[SerializeField] Audio m_Audio;
	[SerializeField] float m_Volume = 1f;

	uint m_AudioID;



	// Properties

	#if UNITY_EDITOR
	public Audio Audio {
		get => !Enum.TryParse(m_AudioName, out Audio audio) ?
			Enum.Parse<Audio>(m_AudioName = m_Audio.ToString()) :
			m_Audio = audio;
		set => m_AudioName = (m_Audio = value).ToString();
	}
	#else
	public Audio Audio {
		get => m_Audio;
		set => m_Audio = value;
	}
	#endif

	public float Volume {
		get => m_Volume;
		set => m_Volume = value;
	}

	ref uint AudioID {
		get => ref m_AudioID;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is PlaySoundFXEvent playSoundFXEvent) {
			Audio  = playSoundFXEvent.Audio;
			Volume = playSoundFXEvent.Volume;
		}
	}

	public override void Start() {
		AudioID = default;
	}

	public override void End() {
		if (AudioID == default) {
			AudioID = AudioManager.PlaySoundFX(Audio, Volume);
		}
	}

	protected override void GetDataID(ref uint audioID) {
		End();
		if (audioID != default) audioID = AudioID;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Audio Manager | Play Point Sound FX
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Audio Manager/Play Point Sound FX")]
public sealed class PlayPointSoundFXEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class PlayPointSoundFXEventNode : EventNodeBase {
		PlayPointSoundFXEvent I => target as PlayPointSoundFXEvent;

		public PlayPointSoundFXEventNode() : base() {
			mainContainer.style.width = Node2U;
		}

		public override void ConstructData() {
			var audio = TextEnumField("Audio", I.Audio, value => I.Audio = value);
			var anchor = ObjectField("Anchor", I.Anchor, value => I.Anchor = value);
			var offset = Vector3Field("Offset", I.Offset, value => I.Offset = value);
			var volume = FloatField("Volume", I.Volume, value => I.Volume = value);
			var spread = FloatField("Spread", I.Spread, value => I.Spread = value);
			mainContainer.Add(audio);
			mainContainer.Add(anchor);
			mainContainer.Add(offset);
			mainContainer.Add(volume);
			mainContainer.Add(spread);

			var element = new VisualElement();
			element.style.flexDirection = FlexDirection.Row;
			mainContainer.Add(element);
			element.Add(Label("Distance"));
			var child1 = Label("Min");
			child1.style.width = 26f;
			element.Add(child1);
			var child2 = new FloatField() { value = I.MinDistance };
			child2.RegisterValueChangedCallback(evt => I.MinDistance = evt.newValue);
			child2.ElementAt(0).style.minWidth = (Node2U - Node2ULabel - 20f) * 0.5f - 26f;
			child2.ElementAt(0).style.maxWidth = (Node2U - Node2ULabel - 20f) * 0.5f - 26f;
			element.Add(child2);
			var child3 = Label("Max");
			child3.style.width = 26f;
			element.Add(child3);
			var child4 = new FloatField() { value = I.MaxDistance };
			child4.RegisterValueChangedCallback(evt => I.MaxDistance = evt.newValue);
			child4.ElementAt(0).style.minWidth = (Node2U - Node2ULabel - 20f) * 0.5f - 26f;
			child4.ElementAt(0).style.maxWidth = (Node2U - Node2ULabel - 20f) * 0.5f - 26f;
			element.Add(child4);
		}

		public override void ConstructPort() {
			CreatePort(Direction.Input);
			CreatePort(Direction.Output);
			CreatePort(Direction.Output, PortType.DataID);
			RefreshExpandedState();
			RefreshPorts();
		}
	}
	#endif



	// Fields

	#if UNITY_EDITOR
	[SerializeField] string m_AudioName;
	#endif

	[SerializeField] Audio m_Audio;
	[SerializeField] GameObject m_Anchor;
	[SerializeField] Vector3 m_Offset;
	[SerializeField] float m_Volume = 1f;
	[SerializeField] float m_Spread = 0f;
	[SerializeField] float m_MinDistance = default;
	[SerializeField] float m_MaxDistance = default;

	uint m_AudioID;



	// Properties

	#if UNITY_EDITOR
	public Audio Audio {
		get => !Enum.TryParse(m_AudioName, out Audio audio) ?
			Enum.Parse<Audio>(m_AudioName = m_Audio.ToString()) :
			m_Audio = audio;
		set => m_AudioName = (m_Audio = value).ToString();
	}
	#else
	public Audio Audio {
		get => m_Audio;
		set => m_Audio = value;
	}
	#endif

	public GameObject Anchor {
		get => m_Anchor;
		set => m_Anchor = value;
	}
	public Vector3 Offset {
		get => m_Offset;
		set => m_Offset = value;
	}
	public float Volume {
		get => m_Volume;
		set => m_Volume = value;
	}
	public float Spread {
		get => m_Spread;
		set => m_Spread = value;
	}
	public float MinDistance {
		get => m_MinDistance;
		set => m_MinDistance = value;
	}
	public float MaxDistance {
		get => m_MaxDistance;
		set => m_MaxDistance = value;
	}

	ref uint AudioID {
		get => ref m_AudioID;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is PlayPointSoundFXEvent playPointSoundFXEvent) {
			Audio       = playPointSoundFXEvent.Audio;
			Volume      = playPointSoundFXEvent.Volume;
			Anchor      = playPointSoundFXEvent.Anchor;
			Offset      = playPointSoundFXEvent.Offset;
			Spread      = playPointSoundFXEvent.Spread;
			MinDistance = playPointSoundFXEvent.MinDistance;
			MaxDistance = playPointSoundFXEvent.MaxDistance;
		}
	}

	public override void Start() {
		AudioID = default;
	}

	public override void End() {
		if (AudioID == default) {
			var position = Anchor ? Anchor.transform.TransformPoint(Offset) : Offset;
			AudioID = AudioManager.PlayPointSoundFX(
				Audio, position, Volume, Spread, MinDistance, MaxDistance);
		}
	}

	protected override void GetDataID(ref uint audioID) {
		End();
		if (audioID != default) audioID = AudioID;
	}



	#if UNITY_EDITOR
	public override void DrawGizmos() {
		var position = Anchor ? Anchor.transform.TransformPoint(Offset) : Offset;
		Gizmos.DrawIcon(position, "AudioSource Gizmo", true, Gizmos.color);
	}

	public override void DrawHandles() {
		var position = Anchor ? Anchor.transform.TransformPoint(Offset) : Offset;
		var handle = Handles.PositionHandle(position, Quaternion.identity);
		Offset = Anchor ? Anchor.transform.InverseTransformPoint(handle) : handle;
		if (Node != null) Node.Q<Vector3Field>().value = Offset;
	}
	#endif
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Audio Manager | Play Blend Sound FX
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Audio Manager/Play Blend Sound FX")]
public sealed class PlayBlendSoundFXEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class PlayBlendSoundFXEventNode : EventNodeBase {
		PlayBlendSoundFXEvent I => target as PlayBlendSoundFXEvent;

		public PlayBlendSoundFXEventNode() : base() {
			mainContainer.style.width = Node2U;
		}

		public override void ConstructData() {
			var audio = TextEnumField("Audio", I.Audio, value => I.Audio = value);
			var anchor = ObjectField("Anchor", I.Anchor, value => I.Anchor = value);
			var offset = Vector3Field("Offset", I.Offset, value => I.Offset = value);
			var volume = FloatField("Volume", I.Volume, value => I.Volume = value);
			var blend = Slider("Blend", I.Blend, 0f, 1f, value => I.Blend = value);
			mainContainer.Add(audio);
			mainContainer.Add(anchor);
			mainContainer.Add(offset);
			mainContainer.Add(volume);
			mainContainer.Add(blend);
		}

		public override void ConstructPort() {
			CreatePort(Direction.Input);
			CreatePort(Direction.Output);
			CreatePort(Direction.Output, PortType.DataID);
			RefreshExpandedState();
			RefreshPorts();
		}
	}
	#endif



	// Fields

	#if UNITY_EDITOR
	[SerializeField] string m_AudioName;
	#endif

	[SerializeField] Audio m_Audio;
	[SerializeField] GameObject m_Anchor;
	[SerializeField] Vector3 m_Offset;
	[SerializeField] float m_Volume = 1f;
	[SerializeField] float m_Blend = 0.5f;

	uint m_AudioID;



	// Properties

	#if UNITY_EDITOR
	public Audio Audio {
		get => !Enum.TryParse(m_AudioName, out Audio audio) ?
			Enum.Parse<Audio>(m_AudioName = m_Audio.ToString()) :
			m_Audio = audio;
		set => m_AudioName = (m_Audio = value).ToString();
	}
	#else
	public Audio Audio {
		get => m_Audio;
		set => m_Audio = value;
	}
	#endif

	public GameObject Anchor {
		get => m_Anchor;
		set => m_Anchor = value;
	}
	public Vector3 Offset {
		get => m_Offset;
		set => m_Offset = value;
	}
	public float Volume {
		get => m_Volume;
		set => m_Volume = value;
	}
	public float Blend {
		get => m_Blend;
		set => m_Blend = value;
	}

	ref uint AudioID {
		get => ref m_AudioID;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is PlayBlendSoundFXEvent playBlendSoundFXEvent) {
			Audio  = playBlendSoundFXEvent.Audio;
			Volume = playBlendSoundFXEvent.Volume;
			Anchor = playBlendSoundFXEvent.Anchor;
			Offset = playBlendSoundFXEvent.Offset;
			Blend  = playBlendSoundFXEvent.Blend;
		}
	}

	public override void Start() {
		AudioID = default;
	}

	public override void End() {
		if (AudioID == default) {
			var position = Anchor ? Anchor.transform.TransformPoint(Offset) : Offset;
			AudioID = AudioManager.PlayBlendSoundFX(Audio, position, Volume, Blend);
		}
	}

	protected override void GetDataID(ref uint audioID) {
		End();
		if (audioID != default) audioID = AudioID;
	}



	#if UNITY_EDITOR
	public override void DrawGizmos() {
		var position = Anchor ? Anchor.transform.TransformPoint(Offset) : Offset;
		Gizmos.DrawIcon(position, "AudioSource Gizmo", true, Gizmos.color);
	}

	public override void DrawHandles() {
		var position = Anchor ? Anchor.transform.TransformPoint(Offset) : Offset;
		var handle = Handles.PositionHandle(position, Quaternion.identity);
		Offset = Anchor ? Anchor.transform.InverseTransformPoint(handle) : handle;
		if (Node != null) Node.Q<Vector3Field>().value = Offset;
	}
	#endif
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Audio Manager | Set Audio Position
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Audio Manager/Set Audio Position")]
public sealed class SetAudioPositionEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class SetAudioPositionEventNode : EventNodeBase {
		SetAudioPositionEvent I => target as SetAudioPositionEvent;

		public SetAudioPositionEventNode() : base() {
			mainContainer.style.width = Node2U;
		}

		public override void ConstructData() {
			var anchor = ObjectField("Anchor", I.Anchor, value => I.Anchor = value);
			var offset = Vector3Field("Offset", I.Offset, value => I.Offset = value);
			mainContainer.Add(anchor);
			mainContainer.Add(offset);
		}

		public override void ConstructPort() {
			CreatePort(Direction.Input);
			CreatePort(Direction.Input, PortType.DataID);
			CreatePort(Direction.Output);
			CreatePort(Direction.Output, PortType.DataID);
			RefreshExpandedState();
			RefreshPorts();
		}
	}
	#endif



	// Fields

	[SerializeField] GameObject m_Anchor;
	[SerializeField] Vector3 m_Offset;

	uint m_AudioID;



	// Properties

	public GameObject Anchor {
		get => m_Anchor;
		set => m_Anchor = value;
	}
	public Vector3 Offset {
		get => m_Offset;
		set => m_Offset = value;
	}

	ref uint AudioID {
		get => ref m_AudioID;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetAudioPositionEvent setAudioPositionEvent) {
			Anchor = setAudioPositionEvent.Anchor;
			Offset = setAudioPositionEvent.Offset;
		}
	}

	public override void Start() {
		AudioID = default;
	}

	public override void End() {
		if (AudioID == default) base.GetDataID(ref AudioID);
		if (AudioID != default) {
			var position = Anchor ? Anchor.transform.TransformPoint(Offset) : Offset;
			AudioManager.SetAudioPosition(AudioID, position);
		}
	}

	protected override void GetDataID(ref uint audioID) {
		if (audioID == default) base.GetDataID(ref audioID);
		if (audioID != default) audioID = AudioID;
	}



	#if UNITY_EDITOR
	public override void DrawHandles() {
		var position = Anchor ? Anchor.transform.TransformPoint(Offset) : Offset;
		var handle = Handles.PositionHandle(position, Quaternion.identity);
		Offset = Anchor ? Anchor.transform.InverseTransformPoint(handle) : handle;
		if (Node != null) Node.Q<Vector3Field>().value = Offset;
	}
	#endif
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Audio Manager | Set Audio Volume
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Audio Manager/Set Audio Volume")]
public sealed class SetAudioVolumeEvent : EventBase {

	// Node

	#if UNITY_EDITOR
	public sealed class SetAudioVolumeEventNode : EventNodeBase {
		SetAudioVolumeEvent I => target as SetAudioVolumeEvent;

		public SetAudioVolumeEventNode() : base() {
			mainContainer.style.width = Node1U;
		}

		public override void ConstructData() {
			var volume = Slider(I.Volume, 0f, 1f, value => I.Volume = value);
			mainContainer.Add(volume);
		}

		public override void ConstructPort() {
			CreatePort(Direction.Input);
			CreatePort(Direction.Input, PortType.DataID);
			CreatePort(Direction.Output);
			CreatePort(Direction.Output, PortType.DataID);
			RefreshExpandedState();
			RefreshPorts();
		}
	}
	#endif



	// Fields

	[SerializeField] float m_Volume = 1f;

	uint m_AudioID;



	// Properties

	public float Volume {
		get => m_Volume;
		set => m_Volume = value;
	}

	ref uint AudioID {
		get => ref m_AudioID;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetAudioVolumeEvent setAudioVolumeEvent) {
			Volume = setAudioVolumeEvent.Volume;
		}
	}

	public override void Start() {
		AudioID = default;
	}

	public override void End() {
		if (AudioID == default) base.GetDataID(ref AudioID);
		if (AudioID != default) AudioManager.SetAudioVolume(AudioID, Volume);
	}

	protected override void GetDataID(ref uint audioID) {
		if (audioID == default) base.GetDataID(ref audioID);
		if (audioID != default) audioID = AudioID;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Audio Manager | Stop Audio
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Audio Manager/Stop Audio")]
public sealed class StopAudioEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class StopAudioEventNode : EventNodeBase {
		StopAudioEvent I => target as StopAudioEvent;

		public StopAudioEventNode() : base() {
			mainContainer.style.width = Node1U;
		}

		public override void ConstructPort() {
			CreatePort(Direction.Input);
			CreatePort(Direction.Input, PortType.DataID);
			CreatePort(Direction.Output);
			RefreshExpandedState();
			RefreshPorts();
		}
	}
	#endif



	// Methods

	public override void End() {
		uint audioID = default;
		base.GetDataID(ref audioID);
		if (audioID != default) AudioManager.StopAudio(audioID);
	}
}

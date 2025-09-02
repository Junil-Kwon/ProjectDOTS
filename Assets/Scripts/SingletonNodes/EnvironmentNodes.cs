using UnityEngine;
using UnityEngine.UIElements;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using static ElementEditorExtensions;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Environment Manager | Set Light Mode
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Environment Manager/Set Light Mode")]
public sealed class SetLightModeEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class SetLightModeEventNode : EventNodeBase {
		SetLightModeEvent I => target as SetLightModeEvent;

		public SetLightModeEventNode() : base() {
			mainContainer.style.width = Node1U;
			var cyan = new Color(180f, 0.75f, 0.60f).ToRGB();
			titleContainer.style.backgroundColor = cyan;
		}

		public override void ConstructData() {
			var lightMode = EnumField("Light Mode", I.LightMode, value => I.LightMode = value);
			mainContainer.Add(lightMode);
		}
	}
	#endif



	// Fields

	[SerializeField] LightMode m_LightMode;



	// Properties

	public LightMode LightMode {
		get => m_LightMode;
		set => m_LightMode = value;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetLightModeEvent setLightModeEvent) {
			LightMode = setLightModeEvent.LightMode;
		}
	}

	public override void End() {
		EnvironmentManager.LightMode = LightMode;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Environment Manager | Set Time Of Day
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Environment Manager/Set Time Of Day")]
public sealed class SetTimeOfDayEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class SetTimeOfDayEventNode : EventNodeBase {
		SetTimeOfDayEvent I => target as SetTimeOfDayEvent;

		public SetTimeOfDayEventNode() : base() {
			mainContainer.style.width = Node1U;
			var cyan = new Color(180f, 0.75f, 0.60f).ToRGB();
			titleContainer.style.backgroundColor = cyan;
		}

		public override void ConstructData() {
			var timeOfDay = FloatField(I.TimeOfDay, value => I.TimeOfDay = value);
			mainContainer.Add(timeOfDay);
		}
	}
	#endif



	// Fields

	[SerializeField] float m_TimeOfDay = 0.5f;



	// Properties

	public float TimeOfDay {
		get => m_TimeOfDay;
		set => m_TimeOfDay = value;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetTimeOfDayEvent setTimeOfDayEvent) {
			TimeOfDay = setTimeOfDayEvent.TimeOfDay;
		}
	}

	public override void End() {
		EnvironmentManager.TimeOfDay = TimeOfDay;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Environment Manager | Add Light
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Environment Manager/Add Light")]
public sealed class AddLightEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class AddLightEventNode : EventNodeBase {
		AddLightEvent I => target as AddLightEvent;

		public AddLightEventNode() : base() {
			mainContainer.style.width = Node2U;
		}

		public override void ConstructData() {
			var anchor = ObjectField("Anchor", I.Anchor, value => I.Anchor = value);
			var offset = Vector3Field("Offset", I.Offset, value => I.Offset = value);
			var color = ColorField("Color", I.Color, value => I.Color = value);
			var intensity = FloatField("Intensity", I.Intensity, value => I.Intensity = value);
			var duration = FloatField("Duration", I.Duration, value => I.Duration = value);
			mainContainer.Add(anchor);
			mainContainer.Add(offset);
			mainContainer.Add(color);
			mainContainer.Add(intensity);
			mainContainer.Add(duration);
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

	[SerializeField] Color m_Color = Color.white;
	[SerializeField] float m_Intensity = 2f;
	[SerializeField] GameObject m_Anchor;
	[SerializeField] Vector3 m_Offset;
	[SerializeField] float m_Duration = 1f;

	uint m_LightID;



	// Properties

	public Color Color {
		get => m_Color;
		set => m_Color = value;
	}
	public float Intensity {
		get => m_Intensity;
		set => m_Intensity = value;
	}
	public GameObject Anchor {
		get => m_Anchor;
		set => m_Anchor = value;
	}
	public Vector3 Offset {
		get => m_Offset;
		set => m_Offset = value;
	}
	public float Duration {
		get => m_Duration;
		set => m_Duration = value;
	}

	ref uint LightID {
		get => ref m_LightID;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is AddLightEvent addLightEvent) {
			Color     = addLightEvent.Color;
			Intensity = addLightEvent.Intensity;
			Anchor    = addLightEvent.Anchor;
			Offset    = addLightEvent.Offset;
			Duration  = addLightEvent.Duration;
		}
	}

	public override void Start() {
		LightID = default;
	}

	public override void End() {
		if (LightID == default) {
			var position = Anchor != null ? Anchor.transform.position + Offset : Vector3.zero;
			LightID = EnvironmentManager.AddLight(Color, Intensity, position, Duration);
		}
	}

	protected override void GetDataID(ref uint lightID) {
		End();
		if (lightID != default) lightID = LightID;
	}



	#if UNITY_EDITOR
	public override void DrawGizmos() {
		var position = Anchor ? Anchor.transform.TransformPoint(Offset) : Offset;
		Gizmos.DrawIcon(position, "PointLight Gizmo", true, Gizmos.color);
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
// Environment Manager | Set Light Position
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Environment Manager/Set Light Position")]
public sealed class SetLightPositionEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class SetLightPositionEventNode : EventNodeBase {
		SetLightPositionEvent I => target as SetLightPositionEvent;

		public SetLightPositionEventNode() : base() {
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

	uint m_LightID;



	// Properties

	public GameObject Anchor {
		get => m_Anchor;
		set => m_Anchor = value;
	}
	public Vector3 Offset {
		get => m_Offset;
		set => m_Offset = value;
	}

	ref uint LightID {
		get => ref m_LightID;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetLightPositionEvent setLightPositionEvent) {
			Anchor = setLightPositionEvent.Anchor;
			Offset = setLightPositionEvent.Offset;
		}
	}

	public override void Start() {
		LightID = default;
	}

	public override void End() {
		if (LightID == default) base.GetDataID(ref LightID);
		if (LightID != default) {
			var position = Anchor ? Anchor.transform.TransformPoint(Offset) : Offset;
			EnvironmentManager.SetLightPosition(LightID, position);
		}
	}

	protected override void GetDataID(ref uint lightID) {
		if (lightID == default) base.GetDataID(ref lightID);
		if (lightID != default) lightID = LightID;
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
// Environment Manager | Set Light Color
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Environment Manager/Set Light Color")]
public sealed class SetLightColorEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class SetLightColorEventNode : EventNodeBase {
		SetLightColorEvent I => target as SetLightColorEvent;

		public SetLightColorEventNode() : base() {
			mainContainer.style.width = Node1U;
		}

		public override void ConstructData() {
			var color = ColorField(I.Color, value => I.Color = value);
			mainContainer.Add(color);
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

	[SerializeField] Color m_Color = Color.white;

	uint m_LightID;



	// Properties

	public Color Color {
		get => m_Color;
		set => m_Color = value;
	}

	ref uint LightID {
		get => ref m_LightID;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetLightColorEvent setLightColorEvent) {
			Color = setLightColorEvent.Color;
		}
	}

	public override void Start() {
		LightID = default;
	}

	public override void End() {
		if (LightID == default) base.GetDataID(ref LightID);
		if (LightID != default) EnvironmentManager.SetLightColor(LightID, Color);
	}

	protected override void GetDataID(ref uint lightID) {
		if (lightID == default) base.GetDataID(ref lightID);
		if (lightID != default) lightID = LightID;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Environment Manager | Set Light Intensity
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Environment Manager/Set Light Intensity")]
public sealed class SetLightIntensityEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class SetLightIntensityEventNode : EventNodeBase {
		SetLightIntensityEvent I => target as SetLightIntensityEvent;

		public SetLightIntensityEventNode() : base() {
			mainContainer.style.width = Node1U;
		}

		public override void ConstructData() {
			var intensity = FloatField(I.Intensity, value => I.Intensity = value);
			mainContainer.Add(intensity);
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

	[SerializeField] float m_Intensity = 1f;

	uint m_LightID;



	// Properties

	public float Intensity {
		get => m_Intensity;
		set => m_Intensity = value;
	}

	ref uint LightID {
		get => ref m_LightID;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetLightIntensityEvent setLightIntensityEvent) {
			Intensity = setLightIntensityEvent.Intensity;
		}
	}

	public override void Start() {
		LightID = default;
	}

	public override void End() {
		if (LightID == default) base.GetDataID(ref LightID);
		if (LightID != default) EnvironmentManager.SetLightIntensity(LightID, Intensity);
	}

	protected override void GetDataID(ref uint lightID) {
		if (lightID == default) base.GetDataID(ref lightID);
		if (lightID != default) lightID = LightID;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Environment Manager | Set Light Duration
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Environment Manager/Set Light Duration")]
public sealed class SetLightDurationEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class SetLightDurationEventNode : EventNodeBase {
		SetLightDurationEvent I => target as SetLightDurationEvent;

		public SetLightDurationEventNode() : base() {
			mainContainer.style.width = Node1U;
		}

		public override void ConstructData() {
			var duration = FloatField(I.Duration, value => I.Duration = value);
			mainContainer.Add(duration);
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

	[SerializeField] float m_Duration = 1f;

	uint m_LightID;



	// Properties

	public float Duration {
		get => m_Duration;
		set => m_Duration = value;
	}

	ref uint LightID {
		get => ref m_LightID;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetLightDurationEvent setLightDurationEvent) {
			Duration = setLightDurationEvent.Duration;
		}
	}

	public override void Start() {
		LightID = default;
	}

	public override void End() {
		if (LightID == default) base.GetDataID(ref LightID);
		if (LightID != default) EnvironmentManager.SetLightDuration(LightID, Duration);
	}

	protected override void GetDataID(ref uint lightID) {
		if (lightID == default) base.GetDataID(ref lightID);
		if (lightID != default) lightID = LightID;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Environment Manager | Set Light Range
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Environment Manager/Set Light Range")]
public sealed class SetLightRangeEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class SetLightRangeEventNode : EventNodeBase {
		SetLightRangeEvent I => target as SetLightRangeEvent;

		public SetLightRangeEventNode() : base() {
			mainContainer.style.width = Node1U;
		}

		public override void ConstructData() {
			var range = FloatField(I.Range, value => I.Range = value);
			mainContainer.Add(range);
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

	[SerializeField] float m_Range = 1f;

	uint m_LightID;



	// Properties

	public float Range {
		get => m_Range;
		set => m_Range = value;
	}

	ref uint LightID {
		get => ref m_LightID;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetLightRangeEvent setLightRangeEvent) {
			Range = setLightRangeEvent.Range;
		}
	}

	public override void Start() {
		LightID = default;
	}

	public override void End() {
		if (LightID == default) base.GetDataID(ref LightID);
		if (LightID != default) EnvironmentManager.SetLightRange(LightID, Range);
	}

	protected override void GetDataID(ref uint lightID) {
		if (lightID == default) base.GetDataID(ref lightID);
		if (lightID != default) lightID = LightID;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Environment Manager | Remove Light
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Environment Manager/Remove Light")]
public sealed class RemoveLightEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class RemoveLightEventNode : EventNodeBase {
		RemoveLightEvent I => target as RemoveLightEvent;

		public RemoveLightEventNode() : base() {
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
		uint lightID = default;
		base.GetDataID(ref lightID);
		if (lightID != default) EnvironmentManager.RemoveLight(lightID);
	}
}

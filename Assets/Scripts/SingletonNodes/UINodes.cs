using UnityEngine;
using UnityEngine.UIElements;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using static ElementEditorExtensions;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// UI Manager | Open Screen
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("UI Manager/Open Screen")]
public sealed class OpenScreenEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class OpenScreenEventNode : EventNodeBase {
		OpenScreenEvent I => target as OpenScreenEvent;

		public OpenScreenEventNode() : base() {
			mainContainer.style.width = Node1U;
			var cyan = new Color(180f, 0.75f, 0.60f).ToRGB();
			titleContainer.style.backgroundColor = cyan;
		}

		public override void ConstructData() {
			var screen = EnumField(I.Screen, value => I.Screen = value);
			mainContainer.Add(screen);
		}
	}
	#endif



	// Fields

	#if UNITY_EDITOR
	[SerializeField] string m_ScreenName;
	#endif

	[SerializeField] Screen m_Screen;



	// Properties

	#if UNITY_EDITOR
	public Screen Screen {
		get => !Enum.TryParse(m_ScreenName, out Screen screen) ?
			Enum.Parse<Screen>(m_ScreenName = m_Screen.ToString()) :
			m_Screen = screen;
		set => m_ScreenName = (m_Screen = value).ToString();
	}
	#else
	public Screen Screen {
		get => m_Screen;
		set => m_Screen = value;
	}
	#endif



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is OpenScreenEvent openScreenEvent) {
			Screen = openScreenEvent.Screen;
		}
	}

	public override void End() {
		UIManager.OpenScreen(Screen);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// UI Manager | Back
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("UI Manager/Back")]
public sealed class BackEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class BackEventNode : EventNodeBase {
		BackEvent I => target as BackEvent;

		public BackEventNode() : base() {
			mainContainer.style.width = Node1U;
			var cyan = new Color(180f, 0.75f, 0.60f).ToRGB();
			titleContainer.style.backgroundColor = cyan;
		}
	}
	#endif



	// Methods

	public override void End() {
		UIManager.Back();
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// UI Manager | Add Text
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("UI Manager/Add Text")]
public sealed class AddTextEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class AddTextEventNode : EventNodeBase {
		AddTextEvent I => target as AddTextEvent;

		public AddTextEventNode() : base() {
			mainContainer.style.width = Node2U;
		}

		public override void ConstructData() {
			var text = TextField("Text", I.Text, value => I.Text = value);
			var anchor = ObjectField("Anchor", I.Anchor, value => I.Anchor = value);
			var offset = Vector3Field("Offset", I.Offset, value => I.Offset = value);
			var duration = FloatField("Duration", I.Duration, value => I.Duration = value);
			var layer = IntField("Layer", I.Layer, value => I.Layer = value);
			mainContainer.Add(text);
			mainContainer.Add(anchor);
			mainContainer.Add(offset);
			mainContainer.Add(duration);
			mainContainer.Add(layer);
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

	[SerializeField] string m_Text;
	[SerializeField] GameObject m_Anchor;
	[SerializeField] Vector3 m_Offset;
	[SerializeField] float m_Duration = 1f;
	[SerializeField] int m_Layer = default;

	uint m_TextID;



	// Properties

	public string Text {
		get => m_Text;
		set => m_Text = value;
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
	public int Layer {
		get => m_Layer;
		set => m_Layer = value;
	}

	ref uint TextID {
		get => ref m_TextID;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is AddTextEvent addTextEvent) {
			Text     = addTextEvent.Text;
			Anchor   = addTextEvent.Anchor;
			Offset   = addTextEvent.Offset;
			Duration = addTextEvent.Duration;
			Layer    = addTextEvent.Layer;
		}
	}

	public override void Start() {
		TextID = default;
	}

	public override void End() {
		if (TextID == default) {
			var position = Anchor != null ? Anchor.transform.position + Offset : Vector3.zero;
			TextID = UIManager.AddText(Text, position, Duration);
		}
	}

	protected override void GetDataID(ref uint textID) {
		End();
		if (textID != default) textID = TextID;
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
// UI Manager | Set Text Value
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("UI Manager/Set Text Value")]
public sealed class SetTextValueEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class SetTextValueEventNode : EventNodeBase {
		SetTextValueEvent I => target as SetTextValueEvent;

		public SetTextValueEventNode() : base() {
			mainContainer.style.width = Node1U;
		}

		public override void ConstructData() {
			var text = TextField(I.Text, value => I.Text = value);
			mainContainer.Add(text);
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

	[SerializeField] string m_Text;

	uint m_TextID;



	// Properties

	public string Text {
		get => m_Text;
		set => m_Text = value;
	}

	ref uint TextID {
		get => ref m_TextID;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetTextValueEvent setTextValueEvent) {
			Text = setTextValueEvent.Text;
		}
	}

	public override void Start() {
		TextID = default;
	}

	public override void End() {
		if (TextID == default) base.GetDataID(ref TextID);
		if (TextID != default) UIManager.SetTextValue(TextID, Text);
	}

	protected override void GetDataID(ref uint textID) {
		if (textID == default) base.GetDataID(ref textID);
		if (textID != default) textID = TextID;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// UI Manager | Set Text Position
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("UI Manager/Set Text Position")]
public sealed class SetTextPositionEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class SetTextPositionEventNode : EventNodeBase {
		SetTextPositionEvent I => target as SetTextPositionEvent;

		public SetTextPositionEventNode() : base() {
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

	uint m_TextID;



	// Properties

	public GameObject Anchor {
		get => m_Anchor;
		set => m_Anchor = value;
	}
	public Vector3 Offset {
		get => m_Offset;
		set => m_Offset = value;
	}

	ref uint TextID {
		get => ref m_TextID;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetTextPositionEvent setTextPositionEvent) {
			Anchor = setTextPositionEvent.Anchor;
			Offset = setTextPositionEvent.Offset;
		}
	}

	public override void Start() {
		TextID = default;
	}

	public override void End() {
		if (TextID == default) base.GetDataID(ref TextID);
		if (TextID != default) {
			var position = Anchor ? Anchor.transform.TransformPoint(Offset) : Offset;
			UIManager.SetTextPosition(TextID, position);
		}
	}

	protected override void GetDataID(ref uint textID) {
		if (textID == default) base.GetDataID(ref textID);
		if (textID != default) textID = TextID;
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
// UI Manager | Set Text Duration
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("UI Manager/Set Text Duration")]
public sealed class SetTextDurationEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class SetTextDurationEventNode : EventNodeBase {
		SetTextDurationEvent I => target as SetTextDurationEvent;

		public SetTextDurationEventNode() : base() {
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

	uint m_TextID;



	// Properties

	public float Duration {
		get => m_Duration;
		set => m_Duration = value;
	}

	ref uint TextID {
		get => ref m_TextID;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetTextDurationEvent setTextDurationEvent) {
			Duration = setTextDurationEvent.Duration;
		}
	}

	public override void Start() {
		TextID = default;
	}

	public override void End() {
		if (TextID == default) base.GetDataID(ref TextID);
		if (TextID != default) UIManager.SetTextDuration(TextID, Duration);
	}

	protected override void GetDataID(ref uint textID) {
		if (textID == default) base.GetDataID(ref textID);
		if (textID != default) textID = TextID;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// UI Manager | Set Text Layer
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("UI Manager/Set Text Layer")]
public sealed class SetTextLayerEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class SetTextLayerEventNode : EventNodeBase {
		SetTextLayerEvent I => target as SetTextLayerEvent;

		public SetTextLayerEventNode() : base() {
			mainContainer.style.width = Node1U;
		}

		public override void ConstructData() {
			var layer = IntField(I.Layer, value => I.Layer = value);
			mainContainer.Add(layer);
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

	[SerializeField] int m_Layer = default;

	uint m_TextID;



	// Properties

	public int Layer {
		get => m_Layer;
		set => m_Layer = value;
	}

	ref uint TextID {
		get => ref m_TextID;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetTextLayerEvent setTextLayerEvent) {
			Layer = setTextLayerEvent.Layer;
		}
	}

	public override void Start() {
		TextID = default;
	}

	public override void End() {
		if (TextID == default) base.GetDataID(ref TextID);
		if (TextID != default) UIManager.SetTextLayer(TextID, Layer);
	}

	protected override void GetDataID(ref uint textID) {
		if (textID == default) base.GetDataID(ref textID);
		if (textID != default) textID = TextID;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// UI Manager | Remove Text
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("UI Manager/Remove Text")]
public sealed class RemoveTextEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class RemoveTextEventNode : EventNodeBase {
		RemoveTextEvent I => target as RemoveTextEvent;

		public RemoveTextEventNode() : base() {
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
		uint textID = default;
		base.GetDataID(ref textID);
		if (textID != default) UIManager.RemoveText(textID);
	}
}

using UnityEngine;
using Unity.Mathematics;

#if UNITY_EDITOR
using static EditorVisualElement;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager | Set Position
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera Manager/Set Position")]
public class SetPositionEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public class SetPositionEventNode : EventNodeBase {
		SetPositionEvent I => target as SetPositionEvent;

		public SetPositionEventNode() : base() {
			mainContainer.style.width = Node1U;
		}

		public override void ConstructData() {
			var anchor = ObjectField("Anchor", I.Anchor, value => I.Anchor = value);
			var offset = Vector3Field("Offset", I.Offset, value => I.Offset = value);
			mainContainer.Add(anchor);
			mainContainer.Add(offset);
		}
	}
	#endif



	// Fields

	[SerializeField] GameObject m_Anchor;
	[SerializeField] Vector3 m_Offset;



	// Properties

	public GameObject Anchor {
		get => m_Anchor;
		set => m_Anchor = value;
	}
	public Vector3 Offset {
		get => m_Offset;
		set => m_Offset = value;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetPositionEvent setPositionEvent) {
			Anchor = setPositionEvent.Anchor;
			Offset = setPositionEvent.Offset;
		}
	}

	public override void End() {
		var position = Anchor != null ? Anchor.transform.position + Offset : Vector3.zero;
		CameraManager.Position = position;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager | Set Rotation
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera Manager/Set Rotation")]
public class SetRotationEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public class SetRotationEventNode : EventNodeBase {
		SetRotationEvent I => target as SetRotationEvent;

		public SetRotationEventNode() : base() {
			mainContainer.style.width = Node1U;
		}

		public override void ConstructData() {
			var offset = Vector3Field("Offset", I.Rotation, value => I.Rotation = value);
			mainContainer.Add(offset);
		}
	}
	#endif



	// Fields

	[SerializeField] Vector3 m_Rotation;



	// Properties

	public Vector3 Rotation {
		get => m_Rotation;
		set => m_Rotation = value;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetRotationEvent setRotationEvent) {
			Rotation = setRotationEvent.Rotation;
		}
	}

	public override void End() {
		CameraManager.EulerRotation = Rotation;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager | Set Dolly Distance
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera Manager/Set Dolly Distance")]
public class SetDollyDistanceEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public class SetDollyDistanceEventNode : EventNodeBase {
		SetDollyDistanceEvent I => target as SetDollyDistanceEvent;

		public SetDollyDistanceEventNode() : base() {
			mainContainer.style.width = Node1U;
		}

		public override void ConstructData() {
			var distance = Slider(I.Distance, -128f, 128f, value => I.Distance = value);
			mainContainer.Add(distance);
		}
	}
	#endif



	// Fields

	[SerializeField] float m_Distance = -48f;



	// Properties

	public float Distance {
		get => m_Distance;
		set => m_Distance = value;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetDollyDistanceEvent setDollyDistanceEvent) {
			Distance = setDollyDistanceEvent.Distance;
		}
	}

	public override void End() {
		CameraManager.DollyDistance = Distance;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager | Set Field Of View
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera Manager/Set Field Of View")]
public class SetFieldOfViewEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public class SetFieldOfViewEventNode : EventNodeBase {
		SetFieldOfViewEvent I => target as SetFieldOfViewEvent;

		public SetFieldOfViewEventNode() : base() {
			mainContainer.style.width = Node1U;
		}

		public override void ConstructData() {
			var fieldOfView = FloatField(I.FieldOfView, value => I.FieldOfView = value);
			mainContainer.Add(fieldOfView);
		}
	}
	#endif



	// Fields

	[SerializeField] float m_FieldOfView = 30f;



	// Properties

	public float FieldOfView {
		get => m_FieldOfView;
		set => m_FieldOfView = value;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetFieldOfViewEvent setFieldOfViewEvent) {
			FieldOfView = setFieldOfViewEvent.FieldOfView;
		}
	}

	public override void End() {
		CameraManager.FieldOfView = FieldOfView;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager | Set Orthographic Size
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera Manager/Set Orthographic Size")]
public class SetOrthographicSizeEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public class SetOrthographicSizeEventNode : EventNodeBase {
		SetOrthographicSizeEvent I => target as SetOrthographicSizeEvent;

		public SetOrthographicSizeEventNode() : base() {
			mainContainer.style.width = Node1U;
		}

		public override void ConstructData() {
			var size = FloatField(I.OrthographicSize, value => I.OrthographicSize = value);
			mainContainer.Add(size);
		}
	}
	#endif



	// Fields

	[SerializeField] float m_OrthographicSize = 11.25f;



	// Properties

	public float OrthographicSize {
		get => m_OrthographicSize;
		set => m_OrthographicSize = value;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetOrthographicSizeEvent setOrthographicSizeEvent) {
			OrthographicSize = setOrthographicSizeEvent.OrthographicSize;
		}
	}

	public override void End() {
		CameraManager.OrthographicSize = OrthographicSize;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager | Set Projection
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera Manager/Set Projection")]
public class SetProjectionEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public class SetProjectionEventNode : EventNodeBase {
		SetProjectionEvent I => target as SetProjectionEvent;

		public SetProjectionEventNode() : base() {
			mainContainer.style.width = Node1U;
		}

		public override void ConstructData() {
			var projection = Slider(I.Projection, 0f, 1f, value => I.Projection = value);
			mainContainer.Add(projection);
		}
	}
	#endif



	// Fields

	[SerializeField] float m_Projection = 1f;



	// Properties

	public float Projection {
		get => m_Projection;
		set => m_Projection = value;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetProjectionEvent setProjectionEvent) {
			Projection = setProjectionEvent.Projection;
		}
	}

	public override void End() {
		CameraManager.Projection = Projection;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager | Set Background Color
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera Manager/Set Background Color")]
public class SetBackgroundColorEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public class SetBackgroundColorEventNode : EventNodeBase {
		SetBackgroundColorEvent I => target as SetBackgroundColorEvent;

		public SetBackgroundColorEventNode() : base() {
			mainContainer.style.width = Node1U;
		}

		public override void ConstructData() {
			var backgroundColor = ColorField(I.BackgroundColor, value => I.BackgroundColor = value);
			mainContainer.Add(backgroundColor);
		}
	}
	#endif



	// Fields

	[SerializeField] Color m_BackgroundColor = Color.black;



	// Properties

	public Color BackgroundColor {
		get => m_BackgroundColor;
		set => m_BackgroundColor = value;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetBackgroundColorEvent setBackgroundColorEvent) {
			BackgroundColor = setBackgroundColorEvent.BackgroundColor;
		}
	}

	public override void End() {
		CameraManager.BackgroundColor = BackgroundColor;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager | Set Constraints
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera Manager/Set Constraints")]
public class SetConstraintsEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public class SetConstraintsEventNode : EventNodeBase {
		SetConstraintsEvent I => target as SetConstraintsEvent;

		public SetConstraintsEventNode() : base() {
			mainContainer.style.width = Node1U;
		}

		public override void ConstructData() {
			var freezePosition = Toggle3(I.FreezePosition, value => I.FreezePosition = value);
			var freezeRotation = Toggle3(I.FreezeRotation, value => I.FreezeRotation = value);
			mainContainer.Add(freezePosition);
			mainContainer.Add(freezeRotation);
		}
	}
	#endif



	// Fields

	[SerializeField] constraints m_Constraints;



	// Properties

	constraints Constraints {
		get => m_Constraints;
		set => m_Constraints = value;
	}
	public bool3 FreezePosition {
		get => m_Constraints.position;
		set => m_Constraints.position = value;
	}
	public bool3 FreezeRotation {
		get => m_Constraints.rotation;
		set => m_Constraints.rotation = value;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetConstraintsEvent setConstraintsEvent) {
			Constraints = setConstraintsEvent.Constraints;
		}
	}

	public override void End() {
		CameraManager.Constraints = Constraints;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager | Set Freeze Position
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera Manager/Set Freeze Position")]
public class SetFreezePositionEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public class SetFreezePositionEventNode : EventNodeBase {
		SetFreezePositionEvent I => target as SetFreezePositionEvent;

		public SetFreezePositionEventNode() : base() {
			mainContainer.style.width = Node1U;
		}

		public override void ConstructData() {
			var freezePosition = Toggle3(I.FreezePosition, value => I.FreezePosition = value);
			mainContainer.Add(freezePosition);
		}
	}
	#endif



	// Fields

	[SerializeField] bool3 m_FreezePosition;



	// Properties

	public bool3 FreezePosition {
		get => m_FreezePosition;
		set => m_FreezePosition = value;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetFreezePositionEvent setFreezePositionEvent) {
			FreezePosition = setFreezePositionEvent.FreezePosition;
		}
	}

	public override void End() {
		CameraManager.FreezePosition = FreezePosition;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager | Set Freeze Rotation
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera Manager/Set Freeze Rotation")]
public class SetFreezeRotationEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public class SetFreezeRotationEventNode : EventNodeBase {
		SetFreezeRotationEvent I => target as SetFreezeRotationEvent;

		public SetFreezeRotationEventNode() : base() {
			mainContainer.style.width = Node1U;
		}

		public override void ConstructData() {
			var freezeRotation = Toggle3(I.FreezeRotation, value => I.FreezeRotation = value);
			mainContainer.Add(freezeRotation);
		}
	}
	#endif



	// Fields

	[SerializeField] bool3 m_FreezeRotation;



	// Properties

	public bool3 FreezeRotation {
		get => m_FreezeRotation;
		set => m_FreezeRotation = value;
	}



	// Methods

	public override void CopyFrom(EventBase eventBase) {
		base.CopyFrom(eventBase);
		if (eventBase is SetFreezeRotationEvent setFreezeRotationEvent) {
			FreezeRotation = setFreezeRotationEvent.FreezeRotation;
		}
	}

	public override void End() {
		CameraManager.FreezeRotation = FreezeRotation;
	}
}

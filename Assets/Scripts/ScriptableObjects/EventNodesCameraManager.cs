using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

using Unity.Mathematics;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager | Set Focus Distance
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera Manager/Set Focus Distance")]
public class SetFocusDistanceEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
	public class SetFocusDistanceEventNode : BaseEventNode {
		SetFocusDistanceEvent I => target as SetFocusDistanceEvent;

		public SetFocusDistanceEventNode() : base() {
			mainContainer.style.width = ExtendedNodeWidth;
		}

		public override void ConstructData() {
			var distance = new VisualElement();
			distance.style.flexDirection = FlexDirection.Row;
			var label  = new Label("Distance");
			var slider = new Slider(0f, 255f) { value = I.distance };
			var value  = new FloatField()     { value = I.distance };
			label .style.marginTop  = 2f;
			label .style.marginLeft = 3f;
			label .style.minWidth = label .style.maxWidth = 56f;
			slider.style.minWidth = slider.style.maxWidth = ExtendedNodeWidth - 17f - 56f - 40f;
			value .style.minWidth = value .style.maxWidth = 40f;
			slider.RegisterValueChangedCallback(evt => I.distance = value .value = evt.newValue);
			value .RegisterValueChangedCallback(evt => I.distance = slider.value = evt.newValue);
			distance.Add(label);
			distance.Add(slider);
			distance.Add(value);

			var duration = new FloatField("Duration") { value = I.duration };
			var curve    = new CurveField("Curve"   ) { value = I.curve    };
			duration.labelElement.style.minWidth = duration.labelElement.style.maxWidth = 56f;
			curve   .labelElement.style.minWidth = curve   .labelElement.style.maxWidth = 56f;
			duration.RegisterValueChangedCallback(evt => I.duration = evt.newValue);
			curve   .RegisterValueChangedCallback(evt => I.curve    = evt.newValue);
			mainContainer.Add(distance);
			mainContainer.Add(duration);
			mainContainer.Add(curve);
		}
	}
	#endif



	// Fields

	public float distance = 48f;
	public float duration =  0f;
	public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);



	// Methods

	public override void CopyFrom(BaseEvent data) {
		base.CopyFrom(data);
		if (data is SetFocusDistanceEvent setFocusDistance) {
			distance = setFocusDistance.distance;
			duration = setFocusDistance.duration;
			curve.CopyFrom(setFocusDistance.curve);
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager | Set Camera Projection
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera Manager/Set Camera Projection")]
public class SetCameraProjectionEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
	public class SetCameraProjectionEventNode : BaseEventNode {
		SetCameraProjectionEvent I => target as SetCameraProjectionEvent;

		public SetCameraProjectionEventNode() : base() {
			mainContainer.style.width = ExtendedNodeWidth;
		}

		public override void ConstructData() {
			var projection = new VisualElement();
			projection.style.flexDirection = FlexDirection.Row;
			var label  = new Label("Projection");
			var slider = new Slider(0f, 1f) { value = I.value };
			var value  = new FloatField()   { value = I.value };
			label .style.marginTop  = 2f;
			label .style.marginLeft = 3f;
			label .style.minWidth = label .style.maxWidth = 56f;
			slider.style.minWidth = slider.style.maxWidth = ExtendedNodeWidth - 17f - 56f - 40f;
			value .style.minWidth = value .style.maxWidth = 40f;
			slider.RegisterValueChangedCallback(evt => I.value = value .value = evt.newValue);
			value .RegisterValueChangedCallback(evt => I.value = slider.value = evt.newValue);
			projection.Add(label);
			projection.Add(slider);
			projection.Add(value);

			var description = new VisualElement();
			description.style.flexDirection = FlexDirection.Row;
			var persp = new Label(" < Perspective" );
			var ortho = new Label("Orthographic > ");
			persp.style.marginLeft  = 3f;
			ortho.style.marginRight = 3f;
			persp.style.minWidth = persp.style.maxWidth = ExtendedNodeWidth * 0.5f - 6f;
			ortho.style.minWidth = ortho.style.maxWidth = ExtendedNodeWidth * 0.5f - 6f;
			persp.style.unityTextAlign = TextAnchor.MiddleLeft;
			ortho.style.unityTextAlign = TextAnchor.MiddleRight;
			persp.style.color = new StyleColor(new color(0.4f));
			ortho.style.color = new StyleColor(new color(0.4f));
			description.Add(persp);
			description.Add(ortho);

			var duration = new FloatField("Duration") { value = I.duration };
			var curve    = new CurveField("Curve"   ) { value = I.curve    };
			duration.labelElement.style.minWidth = duration.labelElement.style.maxWidth = 56f;
			curve   .labelElement.style.minWidth = curve   .labelElement.style.maxWidth = 56f;
			duration.RegisterValueChangedCallback(evt => I.duration = evt.newValue);
			curve   .RegisterValueChangedCallback(evt => I.curve    = evt.newValue);
			mainContainer.Add(projection);
			mainContainer.Add(description);
			mainContainer.Add(duration);
			mainContainer.Add(curve);
		}
	}
	#endif



	// Fields

	public float value    = 1f;
	public float duration = 0f;
	public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);



	// Methods

	public override void CopyFrom(BaseEvent data) {
		base.CopyFrom(data);
		if (data is SetCameraProjectionEvent setCameraProjection) {
			value = setCameraProjection.value;
			duration = setCameraProjection.duration;
			curve.CopyFrom(setCameraProjection.curve);
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager | Lock Camera Position
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera Manager/Lock Camera Position")]
public class LockCameraPositionEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
	public class LockCameraPositionEventNode : BaseEventNode {
		LockCameraPositionEvent I => target as LockCameraPositionEvent;

		public LockCameraPositionEventNode() : base() {
			mainContainer.style.width = DefaultNodeWidth + 24f;
		}

		public override void ConstructData() {
			var axis = new VisualElement();
			axis.style.flexDirection = FlexDirection.Row;
			mainContainer.Add(axis);
			var label = new Label("Axis");
			label.style.marginLeft = 3f;
			label.style.marginTop  = 2f;
			label.style.width = 60f;
			axis.Add(label);
			var xlabel = new Label("X");
			var ylabel = new Label("Y");
			var zlabel = new Label("Z");
			xlabel.style.marginTop = 2f;
			ylabel.style.marginTop = 2f;
			zlabel.style.marginTop = 2f;
			xlabel.style.width = 9f;
			ylabel.style.width = 9f;
			zlabel.style.width = 9f;
			var x = new Toggle() { value = I.axis.x };
			var y = new Toggle() { value = I.axis.y };
			var z = new Toggle() { value = I.axis.z };
			x.ElementAt(0).style.paddingTop = 1f;
			y.ElementAt(0).style.paddingTop = 1f;
			z.ElementAt(0).style.paddingTop = 1f;
			x.ElementAt(0).style.minWidth = x.ElementAt(0).style.maxWidth = 14f;
			y.ElementAt(0).style.minWidth = y.ElementAt(0).style.maxWidth = 14f;
			z.ElementAt(0).style.minWidth = z.ElementAt(0).style.maxWidth = 14f;
			x.RegisterValueChangedCallback(evt => I.axis.x = evt.newValue);
			y.RegisterValueChangedCallback(evt => I.axis.y = evt.newValue);
			z.RegisterValueChangedCallback(evt => I.axis.z = evt.newValue);
			axis.Add(xlabel);
			axis.Add(x);
			axis.Add(ylabel);
			axis.Add(y);
			axis.Add(zlabel);
			axis.Add(z);
		}
	}
	#endif



	// Fields

	public bool3 axis;



	// Methods

	public override void CopyFrom(BaseEvent data) {
		base.CopyFrom(data);
		if (data is LockCameraPositionEvent lockCameraPosition) {
			axis = lockCameraPosition.axis;
		}
	}

	public override void End() => CameraManager.FreezePosition = axis;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager | Lock Camera Rotation
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera Manager/Lock Camera Rotation")]
public class LockCameraRotationEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
	public class LockCameraRotationEventNode : BaseEventNode {
		LockCameraRotationEvent I => target as LockCameraRotationEvent;

		public LockCameraRotationEventNode() : base() {
			mainContainer.style.width = DefaultNodeWidth + 24f;
		}

		public override void ConstructData() {
			var axis = new VisualElement();
			axis.style.flexDirection = FlexDirection.Row;
			mainContainer.Add(axis);
			var label = new Label("Axis");
			label.style.marginLeft = 3f;
			label.style.marginTop  = 2f;
			label.style.width = 60f;
			axis.Add(label);
			var xlabel = new Label("X");
			var ylabel = new Label("Y");
			var zlabel = new Label("Z");
			xlabel.style.marginTop = 2f;
			ylabel.style.marginTop = 2f;
			zlabel.style.marginTop = 2f;
			xlabel.style.width = 9f;
			ylabel.style.width = 9f;
			zlabel.style.width = 9f;
			var x = new Toggle() { value = I.axis.x };
			var y = new Toggle() { value = I.axis.y };
			var z = new Toggle() { value = I.axis.z };
			x.ElementAt(0).style.paddingTop = 1f;
			y.ElementAt(0).style.paddingTop = 1f;
			z.ElementAt(0).style.paddingTop = 1f;
			x.ElementAt(0).style.minWidth = x.ElementAt(0).style.maxWidth = 14f;
			y.ElementAt(0).style.minWidth = y.ElementAt(0).style.maxWidth = 14f;
			z.ElementAt(0).style.minWidth = z.ElementAt(0).style.maxWidth = 14f;
			x.RegisterValueChangedCallback(evt => I.axis.x = evt.newValue);
			y.RegisterValueChangedCallback(evt => I.axis.y = evt.newValue);
			z.RegisterValueChangedCallback(evt => I.axis.z = evt.newValue);
			axis.Add(xlabel);
			axis.Add(x);
			axis.Add(ylabel);
			axis.Add(y);
			axis.Add(zlabel);
			axis.Add(z);
		}
	}
	#endif



	// Fields

	public bool3 axis;



	// Methods

	public override void CopyFrom(BaseEvent data) {
		base.CopyFrom(data);
		if (data is LockCameraRotationEvent lockCameraRotation) {
			axis = lockCameraRotation.axis;
		}
	}

	public override void End() => CameraManager.FreezeRotation = axis;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager | Move Camera
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera Manager/Move Camera")]
public class MoveCameraEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
	public class MoveCameraEventNode : BaseEventNode {
		MoveCameraEvent I => target as MoveCameraEvent;

		public MoveCameraEventNode() {
			mainContainer.style.width = ExtendedNodeWidth;
		}

		public override void ConstructData() {
			var root = new VisualElement();
			mainContainer.Add(root);
			var track = new Foldout() {
				text = "Track",
				value = true,
			};
			track.style.marginTop = 2f;
			for (int i = 0; i < I.track.Count; i++) {
				var index = i;

				var element = new VisualElement();
				element.style.flexDirection = FlexDirection.Row;
				track.Add(element);

				var value = I.track[index];
				var item1 = new Vector3Field() { value = value.Item1 };
				var item2 = new Toggle      () { value = value.Item2 };
				var x = item1.ElementAt(0).ElementAt(0);
				var y = item1.ElementAt(0).ElementAt(1);
				var z = item1.ElementAt(0).ElementAt(2);
				if (x != null) x.style.minWidth = x.style.maxWidth = 48f;
				if (y != null) y.style.minWidth = y.style.maxWidth = 48f;
				if (z != null) z.style.minWidth = z.style.maxWidth = 48f;
				item1.style.minWidth = item1.style.maxWidth = 155f;
				item2.style.minWidth = item2.style.maxWidth =  14f;
				item1.RegisterValueChangedCallback(evt => {
					value.Item1 = evt.newValue;
					I.track[index] = value;
				});
				item2.RegisterValueChangedCallback(evt => {
					value.Item2 = evt.newValue;
					I.track[index] = value;
				});
				element.Add(item1);
				element.Add(item2);

				var removeButton = new Button(() => {
					mainContainer.Remove(root);
					I.track.RemoveAt(index);
					ConstructData();
				}) { text = "-" };
				removeButton.style.width = 18f;
				element.Add(removeButton);
			}
			var addButton = new Button(() => {
				mainContainer.Remove(root);
				I.track.Add();
				ConstructData();
			}) { text = "Add" };
			track.Add(addButton);

			var anchor   = new ObjectField("Anchor"  ) { value = I.anchor   };
			var duration = new FloatField ("Duration") { value = I.duration };
			var curve    = new CurveField ("Curve"   ) { value = I.curve    };
			anchor  .labelElement.style.minWidth = anchor  .labelElement.style.maxWidth = 56f;
			duration.labelElement.style.minWidth = duration.labelElement.style.maxWidth = 56f;
			curve   .labelElement.style.minWidth = curve   .labelElement.style.maxWidth = 56f;
			anchor  .RegisterValueChangedCallback(evt => I.anchor   = evt.newValue as GameObject);
			duration.RegisterValueChangedCallback(evt => I.duration = evt.newValue);
			curve   .RegisterValueChangedCallback(evt => I.curve    = evt.newValue);
			root.Add(anchor);
			root.Add(track);
			root.Add(duration);
			root.Add(curve);
		}
	}
	#endif



	// Fields

	public GameObject anchor;
	public Track track = new();
	public float duration = 1f;
	public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);



	// Methods

	public override void CopyFrom(BaseEvent data) {
		base.CopyFrom(data);
		if (data is MoveCameraEvent moveCamera) {
			anchor = moveCamera.anchor;
			track.CopyFrom(moveCamera.track);
			duration = moveCamera.duration;
			curve.CopyFrom(moveCamera.curve);
		}
	}



	#if UNITY_EDITOR
	public override void DrawGizmos() {
		if (track.Count == 0) return;
		const float Height = 0.1f;

		var position = anchor ? anchor.transform.position : default;
		var prev = default(Vector3);
		for (float time = 0f; time <= duration; time += NetworkManager.Ticktime) {
			var s = curve.Evaluate(Mathf.Clamp01(time / duration)) * track.Distance;
			var next = position + track.Evaluate(s);
			if (time != 0f) Gizmos.DrawLine(prev, next);
			Gizmos.DrawLine(prev, prev + new Vector3(0f, -Height, 0f));
			prev = next;
		}
	}

	public override void DrawHandles() {
		if (track.Count == 0 || node == null) return;

		var position = anchor ? anchor.transform.position : default;
		var query = node.Query<Vector3Field>().ToList();
		for (int i = 0; i < query.Count; i++) {
			var handle = Handles.PositionHandle(position + query[i].value, Quaternion.identity);
			track[i] = (handle - position, track[i].Item2);
			query.ElementAt(i).value = handle - position;
		}
	}
	#endif
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager | Shake Camera
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera Manager/Shake Camera")]
public class ShakeCameraEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
	public class ShakeCameraEventNode : BaseEventNode {
		ShakeCameraEvent I => target as ShakeCameraEvent;

		public ShakeCameraEventNode() : base() {
			mainContainer.style.width = ExtendedNodeWidth;
		}

		public override void ConstructData() {
			var strength = new FloatField("Strength") { value = I.strength };
			var duration = new FloatField("Duration") { value = I.duration };
			strength.labelElement.style.minWidth = strength.labelElement.style.maxWidth = 56;
			duration.labelElement.style.minWidth = duration.labelElement.style.maxWidth = 56;
			strength.RegisterValueChangedCallback(evt => I.strength = evt.newValue);
			duration.RegisterValueChangedCallback(evt => I.duration = evt.newValue);
			mainContainer.Add(strength);
			mainContainer.Add(duration);

			var axis = new VisualElement();
			axis.style.flexDirection = FlexDirection.Row;
			mainContainer.Add(axis);
			var label = new Label("Axis");
			label.style.marginLeft = 3f;
			label.style.marginTop  = 2f;
			label.style.width = 60f;
			axis.Add(label);
			var xlabel = new Label("X");
			var ylabel = new Label("Y");
			xlabel.style.marginTop = 1f;
			ylabel.style.marginTop = 1f;
			xlabel.style.width = 9f;
			ylabel.style.width = 9f;
			var x = new Toggle() { value = I.axis.x };
			var y = new Toggle() { value = I.axis.y };
			x.ElementAt(0).style.minWidth = x.ElementAt(0).style.maxWidth = 14f;
			y.ElementAt(0).style.minWidth = y.ElementAt(0).style.maxWidth = 14f;
			x.RegisterValueChangedCallback(evt => I.axis.x = evt.newValue);
			y.RegisterValueChangedCallback(evt => I.axis.y = evt.newValue);
			axis.Add(xlabel);
			axis.Add(x);
			axis.Add(ylabel);
			axis.Add(y);
		}
	}
	#endif



	// Fields

	public float strength = 0f;
	public float duration = 0f;
	public bool2 axis = new(true, true);



	// Methods

	public override void CopyFrom(BaseEvent data) {
		base.CopyFrom(data);
		if (data is ShakeCameraEvent shakeCamera) {
			strength = shakeCamera.strength;
			duration = shakeCamera.duration;
			axis     = shakeCamera.axis;
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager | Aim Camera
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera Manager/Aim Camera")]
public class AimCameraEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
	public class AimCameraEventNode : BaseEventNode {
		AimCameraEvent I => target as AimCameraEvent;

		public AimCameraEventNode() : base() {
			mainContainer.style.width = ExtendedNodeWidth;
		}

		public override void ConstructData() {
			var target   = new ObjectField("Target"  ) { value = I.target   };
			var duration = new FloatField ("Duration") { value = I.duration };
			var curve    = new CurveField ("Curve"   ) { value = I.curve    };
			target  .labelElement.style.minWidth = target  .labelElement.style.maxWidth = 56f;
			duration.labelElement.style.minWidth = duration.labelElement.style.maxWidth = 56f;
			curve   .labelElement.style.minWidth = curve   .labelElement.style.maxWidth = 56f;
			target  .RegisterValueChangedCallback(evt => I.target   = evt.newValue as GameObject);
			duration.RegisterValueChangedCallback(evt => I.duration = evt.newValue);
			curve   .RegisterValueChangedCallback(evt => I.curve    = evt.newValue);
			mainContainer.Add(target);
			mainContainer.Add(duration);
			mainContainer.Add(curve);
		}
	}
	#endif



	// Fields

	public GameObject target;
	public float duration = 0f;
	public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);



	// Methods

	public override void CopyFrom(BaseEvent data) {
		base.CopyFrom(data);
		if (data is AimCameraEvent aimCamera) {
			target = aimCamera.target;
			curve.CopyFrom(aimCamera.curve);
			duration = aimCamera.duration;
		}
	}
}

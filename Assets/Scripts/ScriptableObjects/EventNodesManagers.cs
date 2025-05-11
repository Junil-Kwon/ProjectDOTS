using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

using Unity.Mathematics;

#if UNITY_EDITOR
	using UnityEditor;
	using UnityEditor.UIElements;
	using UnityEditor.Experimental.GraphView;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Game | Game State
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Game/Game State")]
public class GameStateEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
		public class GameStateEventNode : BaseEventNode {
			GameStateEvent I => target as GameStateEvent;

			public GameStateEventNode() : base() {
				mainContainer.style.minWidth = mainContainer.style.maxWidth = DefaultSize.x;
				var cyan = new StyleColor(color.HSVtoRGB(180f, 0.75f, 0.60f));
				titleContainer.style.backgroundColor = cyan;
			}

			public override void ConstructData() {
				var state = new EnumField(GameState.Gameplay) { value = I.state };
				state.RegisterValueChangedCallback(evt => I.state = (GameState)evt.newValue);
				mainContainer.Add(state);
			}
		}
	#endif



	// Fields

	public GameState state = GameState.Gameplay;



	// Methods

	public override void CopyFrom(BaseEvent data) {
		base.CopyFrom(data);
		if (data is GameStateEvent gameState) {
			state = gameState.state;
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// UI | Dialogue
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("UI/Dialogue")]
public class DialogueEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
		public class DialogueEventNode : BaseEventNode {
			DialogueEvent I => target as DialogueEvent;

			public override void ConstructData() {
				var root = new VisualElement();
				mainContainer.Add(root);
				for (int i = 0; i < I.directives.Count; i++) {
					var index = i;

					var directive = new TextField() { value = I.directives[index], multiline = true };
					directive.style.minWidth = directive.style.maxWidth = 204f;
					directive.textEdition.placeholder = "Directive";
					var field = directive.Q<VisualElement>(className: "unity-text-field__input");
					if (field != null) field.style.minHeight = 46f;
					directive.RegisterValueChangedCallback(evt => I.directives[index] = evt.newValue);
					root.Add(directive);

					var element = new VisualElement();
					element.style.flexDirection = FlexDirection.Row;
					root.Add(element);

					var key = new TextField() { value = I.keys[index] };
					key.style.minWidth = key.style.maxWidth = 180f;
					key.textEdition.placeholder = "Key";
					key.RegisterValueChangedCallback(evt => I.keys[index] = evt.newValue);
					element.Add(key);

					var removeButton = new Button(() => {
						mainContainer.Remove(root);
						I.directives.RemoveAt(index);
						I.keys      .RemoveAt(index);
						ConstructData();
					}) { text = "-" };
					removeButton.style.width = 18f;
					element.Add(removeButton);
				}
				var addButton = new Button(() => {
					mainContainer.Remove(root);
					I.directives.Add("");
					I.keys      .Add("");
					ConstructData();
				}) { text = "Add Element" };
				root.Add(addButton);
			}
		}
	#endif



	// Fields

	public List<string> directives = new() { "", };
	public List<string> keys       = new() { "", };



	// Methods

	public override void CopyFrom(BaseEvent data) {
		base.CopyFrom(data);
		if (data is DialogueEvent dialogue) {
			directives.Clear();
			keys      .Clear();
			directives.AddRange(dialogue.directives);
			keys      .AddRange(dialogue.keys      );
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// UI | Branch
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("UI/Branch")]
public class BranchEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
		public class BranchEventNode : BaseEventNode {
			BranchEvent I => target as BranchEvent;

			public override void ConstructData() {
				var root = new VisualElement();
				mainContainer.Add(root);
				for (int i = 0; i < I.keys.Count; i++) {
					var index = i;

					var element = new VisualElement();
					element.style.flexDirection = FlexDirection.Row;
					root.Add(element);

					var key = new TextField() { value = I.keys[index] };
					key.style.minWidth = key.style.maxWidth = 180f;
					key.textEdition.placeholder = "Key";
					key.RegisterValueChangedCallback(evt => {
						I.keys[index] = evt.newValue;
						(outputContainer[index] as Port).portName = evt.newValue;
					});
					element.Add(key);

					var removeButton = new Button(() => {
						mainContainer.Remove(root);
						I.keys.RemoveAt(index);
						ConstructData();
						outputContainer.RemoveAt(index);
					}) { text = "-" };
					removeButton.style.width = 18f;
					element.Add(removeButton);
				}
				var addButton = new Button(() => {
					mainContainer.Remove(root);
					I.keys.Add("");
					ConstructData();
					var port = CreatePort(Direction.Output);
					port.portName = I.keys[^1];
					outputContainer.Add(port);
				}) { text = "Add Element" };
				root.Add(addButton);
			}

			public override void ConstructPort() {
				CreatePort(Direction.Input);
				for (int i = 0; i < I.keys.Count; i++) {
					var port = CreatePort(Direction.Output);
					port.style.maxWidth = 154f;
					port.portName = I.keys[i];
				}
				RefreshExpandedState();
				RefreshPorts();
			}
		}
	#endif



	// Fields

	public List<string> keys = new() { "", "", };



	// Methods

	public override void CopyFrom(BaseEvent data) {
		base.CopyFrom(data);
		if (data is BranchEvent branch) {
			keys.Clear();
			keys.AddRange(branch.keys);
		}
	}

	public override BaseEvent GetNext() {
		// Get Index from UI Manager, User Selection
		var index = 0;
		foreach (var next in next) if (next.oPortType == PortType.Default) {
			if (next.oPort == index) return next.data;
		}
		return null;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// UI | Fade In
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("UI/Fade In")]
public class FadeInEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
		public class FadeInEventNode : BaseEventNode {
			FadeInEvent I => target as FadeInEvent;

			public FadeInEventNode() : base() {
				mainContainer.style.minWidth = mainContainer.style.maxWidth = DefaultSize.x;
			}

			public override void ConstructData() {
				var root = new VisualElement();
				root.style.flexDirection = FlexDirection.Row;
				mainContainer.Add(root);
				int sample = 32;
				for (int i = 0; i < sample; i++) {
					var element = new VisualElement();
					var color = new color(Mathf.Lerp(0.0f, 0.8f, (float)i / (sample - 1)));
					element.style.backgroundColor = new StyleColor(color);
					element.style.width  = DefaultSize.x / sample;
					element.style.height = 2f;
					root.Add(element);
				}
				var duration = new FloatField("Duration") { value = I.duration, };
				var async    = new Toggle    ("Async"   ) { value = I.async,    };
				var width = DefaultSize.x * 0.5f - 5f;
				duration.labelElement.style.minWidth = duration.labelElement.style.maxWidth = width;
				async   .labelElement.style.minWidth = async   .labelElement.style.maxWidth = width;
				duration.ElementAt(1).style.minWidth = duration.ElementAt(1).style.maxWidth = width;
				async   .ElementAt(1).style.minWidth = async   .ElementAt(1).style.maxWidth = width;
				duration.RegisterValueChangedCallback(evt => I.duration = evt.newValue);
				async   .RegisterValueChangedCallback(evt => I.async    = evt.newValue);
				mainContainer.Add(duration);
				mainContainer.Add(async);
			}
		}
	#endif



	// Fields

	public float duration = 0.5f;



	// Methods

	public override void CopyFrom(BaseEvent data) {
		base.CopyFrom(data);
		if (data is FadeInEvent fadein) {
			duration = fadein.duration;
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// UI | Fade Out
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("UI/Fade Out")]
public class FadeOutEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
		public class FadeOutEventNode : BaseEventNode {
			FadeOutEvent I => target as FadeOutEvent;

			public FadeOutEventNode() : base() {
				mainContainer.style.minWidth = mainContainer.style.maxWidth = DefaultSize.x;
			}

			public override void ConstructData() {
				var root = new VisualElement();
				root.style.flexDirection = FlexDirection.Row;
				mainContainer.Add(root);
				int sample = 32;
				for (int i = 0; i < sample; i++) {
					var element = new VisualElement();
					var color = new color(Mathf.Lerp(0.8f, 0.0f, (float)i / (sample - 1)));
					element.style.backgroundColor = new StyleColor(color);
					element.style.width  = DefaultSize.x / sample;
					element.style.height = 2f;
					root.Add(element);
				}
				var duration = new FloatField("Duration") { value = I.duration, };
				var async    = new Toggle    ("Async"   ) { value = I.async,    };
				var width = DefaultSize.x * 0.5f - 5f;
				duration.labelElement.style.minWidth = duration.labelElement.style.maxWidth = width;
				async   .labelElement.style.minWidth = async   .labelElement.style.maxWidth = width;
				duration.ElementAt(1).style.minWidth = duration.ElementAt(1).style.maxWidth = width;
				async   .ElementAt(1).style.minWidth = async   .ElementAt(1).style.maxWidth = width;
				duration.RegisterValueChangedCallback(evt => I.duration = evt.newValue);
				async   .RegisterValueChangedCallback(evt => I.async    = evt.newValue);
				mainContainer.Add(duration);
				mainContainer.Add(async);
			}
		}
	#endif



	// Fields

	public float duration = 0.5f;



	// Methods

	public override void CopyFrom(BaseEvent data) {
		base.CopyFrom(data);
		if (data is FadeOutEvent fadeout) {
			duration = fadeout.duration;
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera | Focus Distance
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera/Focus Distance")]
public class FocusDistanceEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
		public class FocusDistanceEventNode : BaseEventNode {
			FocusDistanceEvent I => target as FocusDistanceEvent;

			public FocusDistanceEventNode() : base() {
				var purple = new StyleColor(color.HSVtoRGB(260f, 0.75f, 0.60f));
				titleContainer.style.backgroundColor = purple;
			}

			public override void ConstructData() {
				var distance = new VisualElement();
				distance.style.flexDirection = FlexDirection.Row;
				var label  = new Label     ("Distance");
				var slider = new Slider    (0f, 255f) { value = I.distance };
				var value  = new FloatField()         { value = I.distance };
				label .style.marginTop  = 2f;
				label .style.marginLeft = 3f;
				label .style.minWidth = label .style.maxWidth =  60f;
				slider.style.minWidth = slider.style.maxWidth =  96f;
				value .style.minWidth = value .style.maxWidth =  41f;
				slider.RegisterValueChangedCallback(evt => I.distance = value .value = evt.newValue);
				value .RegisterValueChangedCallback(evt => I.distance = slider.value = evt.newValue);
				distance.Add(label );
				distance.Add(slider);
				distance.Add(value );

				var curve    = new CurveField("Curve"   ) { value = I.curve    };
				var duration = new FloatField("Duration") { value = I.duration };
				var async    = new Toggle    ("Async"   ) { value = I.async    };
				curve   .labelElement.style.minWidth = curve   .labelElement.style.maxWidth =  60f;
				duration.labelElement.style.minWidth = duration.labelElement.style.maxWidth =  60f;
				async   .labelElement.style.minWidth = async   .labelElement.style.maxWidth =  60f;
				curve   .ElementAt(1).style.minWidth = curve   .ElementAt(1).style.maxWidth = 144f;
				duration.ElementAt(1).style.minWidth = duration.ElementAt(1).style.maxWidth = 144f;
				async   .ElementAt(1).style.minWidth = async   .ElementAt(1).style.maxWidth = 144f;
				curve   .RegisterValueChangedCallback(evt => I.curve    = evt.newValue);
				duration.RegisterValueChangedCallback(evt => I.duration = evt.newValue);
				async   .RegisterValueChangedCallback(evt => I.async    = evt.newValue);
				mainContainer.Add(distance);
				mainContainer.Add(curve   );
				mainContainer.Add(duration);
				mainContainer.Add(async   );
			}
		}
	#endif



	// Fields

	public float distance = CameraManager.DefaultFocusDistance;
	public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
	public float duration =  0f;



	// Methods

	public override void CopyFrom(BaseEvent data) {
		base.CopyFrom(data);
		if (data is FocusDistanceEvent focusDistance) {
			distance = focusDistance.distance;
			curve.CopyFrom(focusDistance.curve);
			duration = focusDistance.duration;
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera | Projection
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera/Projection")]
public class ProjectionEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
		public class ProjectionEventNode : BaseEventNode {
			ProjectionEvent I => target as ProjectionEvent;

			public ProjectionEventNode() : base() {
				var purple = new StyleColor(color.HSVtoRGB(260f, 0.75f, 0.60f));
				titleContainer.style.backgroundColor = purple;
			}

			public override void ConstructData() {
				var projection = new VisualElement();
				projection.style.flexDirection = FlexDirection.Row;
				var label  = new Label     ("Projection");
				var slider = new Slider    (0f, 1f) { value = I.value };
				var value  = new FloatField()       { value = I.value };
				label .style.marginTop  = 2f;
				label .style.marginLeft = 3f;
				label .style.minWidth = label .style.maxWidth =  60f;
				slider.style.minWidth = slider.style.maxWidth =  96f;
				value .style.minWidth = value .style.maxWidth =  41f;
				slider.RegisterValueChangedCallback(evt => I.value = value .value = evt.newValue);
				value .RegisterValueChangedCallback(evt => I.value = slider.value = evt.newValue);
				projection.Add(label );
				projection.Add(slider);
				projection.Add(value );

				var description = new VisualElement();
				description.style.flexDirection = FlexDirection.Row;
				var persp = new Label(" < Perspective" );
				var ortho = new Label("Orthographic > ");
				persp.style.marginLeft  = 3f;
				ortho.style.marginRight = 3f;
				persp.style.minWidth = persp.style.maxWidth = 102f;
				ortho.style.minWidth = ortho.style.maxWidth = 102f;
				persp.style.unityTextAlign = TextAnchor.MiddleLeft;
				ortho.style.unityTextAlign = TextAnchor.MiddleRight;
				persp.style.color = new StyleColor(new color(0.4f));
				ortho.style.color = new StyleColor(new color(0.4f));
				description.Add(persp);
				description.Add(ortho);

				var curve    = new CurveField("Curve"   ) { value = I.curve    };
				var duration = new FloatField("Duration") { value = I.duration };
				var async    = new Toggle    ("Async"   ) { value = I.async    };
				curve   .labelElement.style.minWidth = curve   .labelElement.style.maxWidth =  60f;
				duration.labelElement.style.minWidth = duration.labelElement.style.maxWidth =  60f;
				async   .labelElement.style.minWidth = async   .labelElement.style.maxWidth =  60f;
				curve   .ElementAt(1).style.minWidth = curve   .ElementAt(1).style.maxWidth = 144f;
				duration.ElementAt(1).style.minWidth = duration.ElementAt(1).style.maxWidth = 144f;
				async   .ElementAt(1).style.minWidth = async   .ElementAt(1).style.maxWidth = 144f;
				curve   .RegisterValueChangedCallback(evt => I.curve    = evt.newValue);
				duration.RegisterValueChangedCallback(evt => I.duration = evt.newValue);
				async   .RegisterValueChangedCallback(evt => I.async    = evt.newValue);
				mainContainer.Add(projection );
				mainContainer.Add(description);
				mainContainer.Add(curve      );
				mainContainer.Add(duration   );
				mainContainer.Add(async      );
			}
		}
	#endif



	// Fields

	public float value = CameraManager.DefaultProjection;
	public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
	public float duration = 0f;



	// Methods

	public override void CopyFrom(BaseEvent data) {
		base.CopyFrom(data);
		if (data is ProjectionEvent projection) {
			value    = projection.value;
			curve.CopyFrom(projection.curve);
			duration = projection.duration;
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera | Freeze Position
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera/Freeze Position")]
public class FreezePositionEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
		public class FreezePositionEventNode : BaseEventNode {
			FreezePositionEvent I => target as FreezePositionEvent;

			public FreezePositionEventNode() : base() {
				mainContainer.style.minWidth = mainContainer.style.maxWidth = DefaultSize.x;
				var purple = new StyleColor(color.HSVtoRGB(260f, 0.75f, 0.60f));
				titleContainer.style.backgroundColor = purple;
			}

			public override void ConstructData() {
				var axis = new VisualElement();
				axis.style.flexDirection = FlexDirection.Row;
				mainContainer.Add(axis);
				var label = new Label("Axis");
				label.style.marginLeft = 3f;
				label.style.marginTop  = 2f;
				label.style.width = 36f;
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
		if (data is FreezePositionEvent freezePosition) {
			axis = freezePosition.axis;
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera | Freeze Rotation
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera/Freeze Rotation")]
public class FreezeRotationEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
		public class FreezeRotationEventNode : BaseEventNode {
			FreezeRotationEvent I => target as FreezeRotationEvent;

			public FreezeRotationEventNode() : base() {
				mainContainer.style.minWidth = mainContainer.style.maxWidth = DefaultSize.x;
				var purple = new StyleColor(color.HSVtoRGB(260f, 0.75f, 0.60f));
				titleContainer.style.backgroundColor = purple;
			}

			public override void ConstructData() {
				var axis = new VisualElement();
				axis.style.flexDirection = FlexDirection.Row;
				mainContainer.Add(axis);
				var label = new Label("Axis");
				label.style.marginLeft = 3f;
				label.style.marginTop  = 2f;
				label.style.width = 36f;
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
		if (data is FreezeRotationEvent freezeRotation) {
			axis = freezeRotation.axis;
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera | Move Camera
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera/Move Camera")]
public class MoveCameraEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
		public class MoveCameraEventNode : BaseEventNode {
			MoveCameraEvent I => target as MoveCameraEvent;

			public MoveCameraEventNode() {
				var purple = new StyleColor(color.HSVtoRGB(280f, 0.75f, 0.60f));
				titleContainer.style.backgroundColor = purple;
			}

			public override void ConstructData() {
				var target   = new ObjectField("Target"  ) { value = I.target,   };
				var curve    = new CurveField ("Curve"   ) { value = I.curve,    };
				var duration = new FloatField ("Duration") { value = I.duration, };
				var async    = new Toggle     ("Async"   ) { value = I.async,    };
				target  .labelElement.style.minWidth = target  .labelElement.style.maxWidth =  60f;
				curve   .labelElement.style.minWidth = curve   .labelElement.style.maxWidth =  60f;
				duration.labelElement.style.minWidth = duration.labelElement.style.maxWidth =  60f;
				async   .labelElement.style.minWidth = async   .labelElement.style.maxWidth =  60f;
				target  .ElementAt(1).style.minWidth = target  .ElementAt(1).style.maxWidth = 144f;
				curve   .ElementAt(1).style.minWidth = curve   .ElementAt(1).style.maxWidth = 144f;
				duration.ElementAt(1).style.minWidth = duration.ElementAt(1).style.maxWidth = 144f;
				async   .ElementAt(1).style.minWidth = async   .ElementAt(1).style.maxWidth = 144f;
				target  .RegisterValueChangedCallback(evt => I.target   = evt.newValue as GameObject);
				curve   .RegisterValueChangedCallback(evt => I.curve    = evt.newValue);
				duration.RegisterValueChangedCallback(evt => I.duration = evt.newValue);
				async   .RegisterValueChangedCallback(evt => I.async    = evt.newValue);
				mainContainer.Add(target);
				mainContainer.Add(curve);
				mainContainer.Add(duration);
				mainContainer.Add(async);
			}
		}
	#endif



	// Fields

	public GameObject target;
	public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
	public float duration = 1f;



	// Methods

	public override void CopyFrom(BaseEvent data) {
		base.CopyFrom(data);
		if (data is MoveCameraEvent moveCamera) {
			target   = moveCamera.target;
			curve.CopyFrom(moveCamera.curve);
			duration = moveCamera.duration;
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera | Track Camera
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera/Track Camera")]
public class TrackCameraEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
		public class TrackCameraEventNode : BaseEventNode {
			TrackCameraEvent I => target as TrackCameraEvent;

			public TrackCameraEventNode() {
				var purple = new StyleColor(color.HSVtoRGB(280f, 0.75f, 0.60f));
				titleContainer.style.backgroundColor = purple;
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
					if (x != null) x.style.minWidth = x.style.maxWidth = 45f;
					if (y != null) y.style.minWidth = y.style.maxWidth = 45f;
					if (z != null) z.style.minWidth = z.style.maxWidth = 45f;
					item1.style.minWidth = item1.style.maxWidth = 145f;
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

				var anchor   = new ObjectField("Anchor"  ) { value = I.anchor,   };
				var curve    = new CurveField ("Curve"   ) { value = I.curve,    };
				var duration = new FloatField ("Duration") { value = I.duration, };
				var async    = new Toggle     ("Async"   ) { value = I.async,    };
				anchor  .labelElement.style.minWidth = anchor  .labelElement.style.maxWidth =  60f;
				curve   .labelElement.style.minWidth = curve   .labelElement.style.maxWidth =  60f;
				duration.labelElement.style.minWidth = duration.labelElement.style.maxWidth =  60f;
				async   .labelElement.style.minWidth = async   .labelElement.style.maxWidth =  60f;
				anchor  .ElementAt(1).style.minWidth = anchor  .ElementAt(1).style.maxWidth = 144f;
				curve   .ElementAt(1).style.minWidth = curve   .ElementAt(1).style.maxWidth = 144f;
				duration.ElementAt(1).style.minWidth = duration.ElementAt(1).style.maxWidth = 144f;
				async   .ElementAt(1).style.minWidth = async   .ElementAt(1).style.maxWidth = 144f;
				anchor  .RegisterValueChangedCallback(evt => I.anchor = evt.newValue as GameObject);
				curve   .RegisterValueChangedCallback(evt => I.curve    = evt.newValue);
				duration.RegisterValueChangedCallback(evt => I.duration = evt.newValue);
				async   .RegisterValueChangedCallback(evt => I.async    = evt.newValue);
				root.Add(anchor);
				root.Add(track);
				root.Add(curve);
				root.Add(duration);
				root.Add(async);
			}
		}
	#endif



	// Fields

	public GameObject anchor;
	public Track track = new();
	public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
	public float duration = 1f;



	// Methods

	public override void CopyFrom(BaseEvent data) {
		base.CopyFrom(data);
		if (data is TrackCameraEvent trackCamera) {
			anchor = trackCamera.anchor;
			track.CopyFrom(trackCamera.track);
			curve.CopyFrom(trackCamera.curve);
			duration = trackCamera.duration;
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
// Camera | Shake Camera
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera/Shake Camera")]
public class ShakeCameraEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
		public class ShakeCameraEventNode : BaseEventNode {
			ShakeCameraEvent I => target as ShakeCameraEvent;

			public ShakeCameraEventNode() : base() {
				mainContainer.style.minWidth = mainContainer.style.maxWidth = DefaultSize.x;
				var purple = new StyleColor(color.HSVtoRGB(280f, 0.75f, 0.60f));
				titleContainer.style.backgroundColor = purple;
			}

			public override void ConstructData() {
				var strength = new FloatField("Strength") { value = I.strength };
				var duration = new FloatField("Duration") { value = I.duration };
				var width = DefaultSize.x * 0.5f - 5f;
				strength.labelElement.style.minWidth = strength.labelElement.style.maxWidth = width;
				duration.labelElement.style.minWidth = duration.labelElement.style.maxWidth = width;
				strength.ElementAt(1).style.minWidth = strength.ElementAt(1).style.maxWidth = width;
				duration.ElementAt(1).style.minWidth = duration.ElementAt(1).style.maxWidth = width;
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
				label.style.width = 63f;
				axis.Add(label);
				var xlabel = new Label("X");
				var ylabel = new Label("Y");
				xlabel.style.marginTop = 2f;
				ylabel.style.marginTop = 2f;
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

				var async = new Toggle("Async") { value = I.async };
				async.labelElement.style.minWidth = async.labelElement.style.maxWidth = width;
				async.ElementAt(1).style.minWidth = async.ElementAt(1).style.maxWidth = width;
				async.RegisterValueChangedCallback(evt => I.async = evt.newValue);
				mainContainer.Add(async);
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
// Camera | Look At
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Camera/Look At")]
public class LookAtEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
		public class LookAtEventNode : BaseEventNode {
			LookAtEvent I => target as LookAtEvent;

			public LookAtEventNode() : base() {
				mainContainer.style.minWidth = mainContainer.style.maxWidth = DefaultSize.x;
				var purple = new StyleColor(color.HSVtoRGB(280f, 0.75f, 0.60f));
				titleContainer.style.backgroundColor = purple;
			}

			public override void ConstructData() {
				var target   = new ObjectField("Target"  ) { value = I.target   };
				var curve    = new CurveField ("Curve"   ) { value = I.curve    };
				var duration = new FloatField ("Duration") { value = I.duration };
				var async    = new Toggle     ("Async"   ) { value = I.async    };
				var width = DefaultSize.x * 0.5f - 5f;
				target  .labelElement.style.minWidth = target  .labelElement.style.maxWidth = width;
				curve   .labelElement.style.minWidth = curve   .labelElement.style.maxWidth = width;
				duration.labelElement.style.minWidth = duration.labelElement.style.maxWidth = width;
				async   .labelElement.style.minWidth = async   .labelElement.style.maxWidth = width;
				target  .ElementAt(1).style.minWidth = target  .ElementAt(1).style.maxWidth = width;
				curve   .ElementAt(1).style.minWidth = curve   .ElementAt(1).style.maxWidth = width;
				duration.ElementAt(1).style.minWidth = duration.ElementAt(1).style.maxWidth = width;
				async   .ElementAt(1).style.minWidth = async   .ElementAt(1).style.maxWidth = width;
				target  .RegisterValueChangedCallback(evt => I.target   = evt.newValue as GameObject);
				curve   .RegisterValueChangedCallback(evt => I.curve    = evt.newValue);
				duration.RegisterValueChangedCallback(evt => I.duration = evt.newValue);
				async   .RegisterValueChangedCallback(evt => I.async    = evt.newValue);
				mainContainer.Add(target  );
				mainContainer.Add(curve   );
				mainContainer.Add(duration);
				mainContainer.Add(async   );
			}
		}
	#endif



	// Fields

	public GameObject target;
	public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
	public float duration = 0f;



	// Methods

	public override void CopyFrom(BaseEvent data) {
		base.CopyFrom(data);
		if (data is LookAtEvent lookat) {
			target   = lookat.target;
			curve.CopyFrom(lookat.curve);
			duration = lookat.duration;
		}
	}
}

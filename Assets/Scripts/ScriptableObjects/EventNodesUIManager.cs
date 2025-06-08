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
// UI Manager | Open Multiplayer
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("UI Manager/Open Multiplayer")]
public class OpenMultiplayerEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
		public class OpenMultiplayerEventNode : BaseEventNode {
			OpenMultiplayerEvent I => target as OpenMultiplayerEvent;

			public OpenMultiplayerEventNode() : base() {
				mainContainer.style.width = DefaultNodeWidth;
			}
		}
	#endif



	// Methods

	public override void End() => UIManager.OpenMultiplayer();
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// UI | Dialogue
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("UI Manager/Dialogue")]
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

[NodeMenu("UI Manager/Branch")]
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

	public override void GetNext(List<BaseEvent> list) {
		list ??= new();
		list.Clear();
		// Get Index from UI Manager, User Selection
		var index = 0;
		foreach (var next in next) if (next.oPortType == PortType.Default) {
			if (next.oPort == index) list.Add(next.data);
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// UI | Fade In
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("UI Manager/Fade In")]
public class FadeInEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
		public class FadeInEventNode : BaseEventNode {
			FadeInEvent I => target as FadeInEvent;

			public FadeInEventNode() : base() {
				mainContainer.style.width = DefaultNodeWidth;
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
					element.style.width  = DefaultNodeWidth / sample;
					element.style.height = 2f;
					root.Add(element);
				}
				var duration = new FloatField("Duration") { value = I.duration, };
				var width = DefaultNodeWidth * 0.5f - 5f;
				duration.labelElement.style.minWidth = duration.labelElement.style.maxWidth = width;
				duration.ElementAt(1).style.minWidth = duration.ElementAt(1).style.maxWidth = width;
				duration.RegisterValueChangedCallback(evt => I.duration = evt.newValue);
				mainContainer.Add(duration);
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

[NodeMenu("UI Manager/Fade Out")]
public class FadeOutEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
		public class FadeOutEventNode : BaseEventNode {
			FadeOutEvent I => target as FadeOutEvent;

			public FadeOutEventNode() : base() {
				mainContainer.style.width = DefaultNodeWidth;
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
					element.style.width  = DefaultNodeWidth / sample;
					element.style.height = 2f;
					root.Add(element);
				}
				var duration = new FloatField("Duration") { value = I.duration, };
				var width = DefaultNodeWidth * 0.5f - 5f;
				duration.labelElement.style.minWidth = duration.labelElement.style.maxWidth = width;
				duration.ElementAt(1).style.minWidth = duration.ElementAt(1).style.maxWidth = width;
				duration.RegisterValueChangedCallback(evt => I.duration = evt.newValue);
				mainContainer.Add(duration);
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

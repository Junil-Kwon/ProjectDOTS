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
// Network Manager | Is Host
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Network Manager/Is Host")]
public class IsHostEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
	public class IsHostEventNode : BaseEventNode {
		IsHostEvent I => target as IsHostEvent;

		public IsHostEventNode() : base() {
			mainContainer.style.width = DefaultNodeWidth;
			var purple = new StyleColor(color.HSVtoRGB(270f, 0.75f, 0.60f));
			titleContainer.style.backgroundColor = purple;
		}

		public override void ConstructPort() {
			CreatePort(Direction.Input);
			CreatePort(Direction.Output).portName = "True";
			CreatePort(Direction.Output).portName = "False";
			RefreshExpandedState();
			RefreshPorts();
		}
	}
	#endif



	// Methods

	public override void GetNext(List<BaseEvent> list) {
		list ??= new();
		list.Clear();
		int index = NetworkManager.IsHost ? 0 : 1;
		foreach (var next in next) if (next.oPortType == PortType.Default) {
			if (next.oPort == index) list.Add(next.data);
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager | Is Client
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Network Manager/Is Client")]
public class IsClientEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
	public class IsClientEventNode : BaseEventNode {
		IsClientEvent I => target as IsClientEvent;

		public IsClientEventNode() : base() {
			mainContainer.style.width = DefaultNodeWidth;
			var purple = new StyleColor(color.HSVtoRGB(270f, 0.75f, 0.60f));
			titleContainer.style.backgroundColor = purple;
		}

		public override void ConstructPort() {
			CreatePort(Direction.Input);
			CreatePort(Direction.Output).portName = "True";
			CreatePort(Direction.Output).portName = "False";
			RefreshExpandedState();
			RefreshPorts();
		}
	}
	#endif



	// Methods

	public override void GetNext(List<BaseEvent> list) {
		list ??= new();
		list.Clear();
		int index = NetworkManager.IsClient ? 0 : 1;
		foreach (var next in next) if (next.oPortType == PortType.Default) {
			if (next.oPort == index) list.Add(next.data);
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager | Is Relay
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Network Manager/Is Relay")]
public class IsRelayEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
	public class IsRelayEventNode : BaseEventNode {
		IsRelayEvent I => target as IsRelayEvent;

		public IsRelayEventNode() : base() {
			mainContainer.style.width = DefaultNodeWidth;
			var purple = new StyleColor(color.HSVtoRGB(270f, 0.75f, 0.60f));
			titleContainer.style.backgroundColor = purple;
		}

		public override void ConstructPort() {
			CreatePort(Direction.Input);
			CreatePort(Direction.Output).portName = "True";
			CreatePort(Direction.Output).portName = "False";
			RefreshExpandedState();
			RefreshPorts();
		}
	}
	#endif



	// Methods

	public override void GetNext(List<BaseEvent> list) {
		list ??= new();
		list.Clear();
		int index = NetworkManager.IsRelay ? 0 : 1;
		foreach (var next in next) if (next.oPortType == PortType.Default) {
			if (next.oPort == index) list.Add(next.data);
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager | Is Local
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Network Manager/Is Local")]
public class IsLocalEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
	public class IsLocalEventNode : BaseEventNode {
		IsLocalEvent I => target as IsLocalEvent;

		public IsLocalEventNode() : base() {
			mainContainer.style.width = DefaultNodeWidth;
			var purple = new StyleColor(color.HSVtoRGB(270f, 0.75f, 0.60f));
			titleContainer.style.backgroundColor = purple;
		}

		public override void ConstructPort() {
			CreatePort(Direction.Input);
			CreatePort(Direction.Output).portName = "True";
			CreatePort(Direction.Output).portName = "False";
			RefreshExpandedState();
			RefreshPorts();
		}
	}
	#endif



	// Methods

	public override void GetNext(List<BaseEvent> list) {
		list ??= new();
		list.Clear();
		int index = NetworkManager.IsLocal ? 0 : 1;
		foreach (var next in next) if (next.oPortType == PortType.Default) {
			if (next.oPort == index) list.Add(next.data);
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager | Is Single Player
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Network Manager/Is Single Player")]
public class IsSinglePlayerEvent : BaseEvent {

	// Node

	#if UNITY_EDITOR
	public class IsSinglePlayerEventNode : BaseEventNode {
		IsSinglePlayerEvent I => target as IsSinglePlayerEvent;

		public IsSinglePlayerEventNode() : base() {
			mainContainer.style.width = DefaultNodeWidth;
			var purple = new StyleColor(color.HSVtoRGB(270f, 0.75f, 0.60f));
			titleContainer.style.backgroundColor = purple;
		}

		public override void ConstructPort() {
			CreatePort(Direction.Input);
			CreatePort(Direction.Output).portName = "True";
			CreatePort(Direction.Output).portName = "False";
			RefreshExpandedState();
			RefreshPorts();
		}
	}
	#endif



	// Methods

	public override void GetNext(List<BaseEvent> list) {
		list ??= new();
		list.Clear();
		int index = NetworkManager.IsSinglePlayer ? 0 : 1;
		foreach (var next in next) if (next.oPortType == PortType.Default) {
			if (next.oPort == index) list.Add(next.data);
		}
	}
}

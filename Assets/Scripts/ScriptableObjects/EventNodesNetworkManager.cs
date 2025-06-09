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
				var @true  = CreatePort(Direction.Output);
				var @false = CreatePort(Direction.Output);
				@true .portName = "True";
				@false.portName = "False";
				RefreshExpandedState();
				RefreshPorts();
			}
		}
	#endif



	// Methods

	public override void GetNext(List<BaseEvent> list) {
		list ??= new();
		list.Clear();
		var index = NetworkManager.IsHost ? 0 : 1;
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
				var @true  = CreatePort(Direction.Output);
				var @false = CreatePort(Direction.Output);
				@true .portName = "True";
				@false.portName = "False";
				RefreshExpandedState();
				RefreshPorts();
			}
		}
	#endif



	// Methods

	public override void GetNext(List<BaseEvent> list) {
		list ??= new();
		list.Clear();
		var index = NetworkManager.IsClient ? 0 : 1;
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
				var @true  = CreatePort(Direction.Output);
				var @false = CreatePort(Direction.Output);
				@true .portName = "True";
				@false.portName = "False";
				RefreshExpandedState();
				RefreshPorts();
			}
		}
	#endif



	// Methods

	public override void GetNext(List<BaseEvent> list) {
		list ??= new();
		list.Clear();
		var index = NetworkManager.IsRelay ? 0 : 1;
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
				var @true  = CreatePort(Direction.Output);
				var @false = CreatePort(Direction.Output);
				@true .portName = "True";
				@false.portName = "False";
				RefreshExpandedState();
				RefreshPorts();
			}
		}
	#endif



	// Methods

	public override void GetNext(List<BaseEvent> list) {
		list ??= new();
		list.Clear();
		var index = NetworkManager.IsLocal ? 0 : 1;
		foreach (var next in next) if (next.oPortType == PortType.Default) {
			if (next.oPort == index) list.Add(next.data);
		}
	}
}

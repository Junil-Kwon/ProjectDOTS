using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
using static ElementEditorExtensions;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager | Is Single Player
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Network Manager/Is Single Player")]
public sealed class IsSinglePlayerEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class IsSinglePlayerEventNode : EventNodeBase {
		IsSinglePlayerEvent I => target as IsSinglePlayerEvent;

		public IsSinglePlayerEventNode() : base() {
			mainContainer.style.width = Node1U;
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

	public override void GetNexts(List<EventBase> list) {
		int index = NetworkManager.IsSinglePlayer ? 0 : 1;
		foreach (var next in Nexts) if (next.oPortType == PortType.Default) {
			if (next.oPort == index) list.Add(next.eventBase);
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager | Is Multi Player
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Network Manager/Is Multi Player")]
public sealed class IsMultiPlayerEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class IsMultiPlayerEventNode : EventNodeBase {
		IsMultiPlayerEvent I => target as IsMultiPlayerEvent;

		public IsMultiPlayerEventNode() : base() {
			mainContainer.style.width = Node1U;
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

	public override void GetNexts(List<EventBase> list) {
		int index = NetworkManager.IsMultiPlayer ? 0 : 1;
		foreach (var next in Nexts) if (next.oPortType == PortType.Default) {
			if (next.oPort == index) list.Add(next.eventBase);
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Network Manager | Send Chat Message
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[NodeMenu("Network Manager/Send Chat Message")]
public sealed class SendChatMessageEvent : EventBase {

	// Editor

	#if UNITY_EDITOR
	public sealed class SendChatMessageEventNode : EventNodeBase {
		SendChatMessageEvent I => target as SendChatMessageEvent;

		public SendChatMessageEventNode() : base() {
			mainContainer.style.width = Node2U;
		}

		public override void ConstructData() {
			var message = TextField(I.Message, value => I.Message = value);
			var field = message.Q(className: "unity-text-field__input");
			if (field != null) field.style.minHeight = 56f;
			message.textEdition.placeholder = "Message";
			message.multiline = true;
			message.style.width = Node2U - 8f;
			mainContainer.Add(message);
		}
	}
	#endif



	// Fields

	[SerializeField] string m_Message;



	// Properties

	public string Message {
		get => m_Message;
		set => m_Message = value;
	}



	// Methods

	public override void End() {
		NetworkManager.SendChatMessage(Message);
	}
}

using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Callbacks;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Event Graph
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[CreateAssetMenu(fileName = "EventGraph", menuName = "Scriptable Objects/Event Graph")]
public class EventGraph : ScriptableObject {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(EventGraph))]
	class EventGraphSOEditor : EditorExtensions {
		EventGraph I => target as EventGraph;
		public override void OnInspectorGUI() {
			Begin();

			if (Button("Open Event Graph")) I.Open();
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeReference] EntryEvent m_Entry = new();
	EntryEvent m_Clone;



	// Properties

	public EntryEvent Entry {
		get => m_Entry;
	}
	public EntryEvent Clone {
		get => m_Clone;
		set => m_Clone = value;
	}



	// Methods

	#if UNITY_EDITOR
	public void Open() => EventGraphWindow.Open(name, this);

	[OnOpenAsset]
	public static bool OnOpen(int instanceID) {
		var target = EditorUtility.InstanceIDToObject(instanceID);
		if (target is EventGraph eventGraph) {
			eventGraph.Open();
			return true;
		} 
		return false;
	}
	#endif
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Event Graph Window
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

#if UNITY_EDITOR
public class EventGraphWindow : EditorWindow {

	// Fields

	Toolbar m_Toolbar;
	EventGraphView m_EventGraphView;
	EventGraph m_EventGraph;



	// Properties

	Toolbar Toolbar {
		get => m_Toolbar;
		set => m_Toolbar = value;
	}
	EventGraphView EventGraphView {
		get => m_EventGraphView;
		set => m_EventGraphView = value;
	}
	EventGraph EventGraph {
		get => m_EventGraph;
		set => m_EventGraph = value;
	}



	// Methods

	public static void Open(string name, EventGraph eventGraph) {
		var windows = Resources.FindObjectsOfTypeAll<EventGraphWindow>();
		var existingWindow = windows.FirstOrDefault(window => window.EventGraph == eventGraph);
		if (existingWindow == null) {
			var dock = new[] { typeof(EventGraphWindow), typeof(SceneView) };
			var window = CreateWindow<EventGraphWindow>(name, dock);
			window.EventGraph = eventGraph;
			window.Initialize();
		} else existingWindow.Focus();
	}

	void Initialize() {
		var mainContainer = new VisualElement();
		mainContainer.style.flexDirection = FlexDirection.Column;
		mainContainer.style.flexGrow = 1;
		rootVisualElement.Clear();
		rootVisualElement.Add(mainContainer);

		Toolbar = new Toolbar();
		Toolbar.Add(new ToolbarButton(() => EventGraphView.Save()) { text = "Save" });
		Toolbar.Add(new ToolbarButton(() => EventGraphView.Load()) { text = "Load" });
		mainContainer.Add(Toolbar);

		EventGraphView = new EventGraphView(EventGraph);
		EventGraphView.style.flexGrow = 1;
		mainContainer.Add(EventGraphView);
	}



	// Lifecycle

	void OnGUI() {
		if (EventGraphView == null) Initialize();
	}
}
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Event Graph View
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

#if UNITY_EDITOR
public class EventGraphView : GraphView {

	// Fields

	bool m_IsFramed;
	EventGraph m_EventGraph;



	// Properties

	bool IsFramed {
		get => m_IsFramed;
		set => m_IsFramed = value;
	}
	EventGraph EventGraph {
		get => m_EventGraph;
		set => m_EventGraph = value;
	}



	// Constructor

	public EventGraphView(EventGraph eventGraph) {
		EventGraph = eventGraph;
		var contentZoomer = new ContentZoomer();
		this.AddManipulator(contentZoomer);
		this.AddManipulator(new ContentDragger());
		this.AddManipulator(new SelectionDragger());
		this.AddManipulator(new RectangleSelector());
		contentZoomer.minScale = 00.1f;
		contentZoomer.maxScale = 10.0f;
		var grid = new GridBackground();
		grid.StretchToParentSize();
		Insert(0, grid);

		RegisterCallback<GeometryChangedEvent>(evt => {
			if (IsFramed == false) {
				IsFramed = true;
				FrameAll();
			}
		});
		RegisterCallback<KeyDownEvent>(evt => {
			if ((evt.ctrlKey || evt.commandKey) && evt.keyCode == KeyCode.S) {
				evt.StopImmediatePropagation();
				Save();
			}
		});
		Load();
	}



	// Graph Methods

	public override void BuildContextualMenu(ContextualMenuPopulateEvent populateEvent) {
		if (populateEvent.target is GraphView) {
			var position = contentViewContainer.WorldToLocal(populateEvent.localMousePosition);
			foreach (var dropdown in EventBase.EventNodeBase.Dropdown) {
				populateEvent.menu.AppendAction("Create Node/"+ dropdown, _ => {
					var name = Regex.Replace(dropdown.Split('/')[^1], @"\s+", "");
					var type = Type.GetType(name + "Event");
					var node = CreateNode(type, position);
					node.ConstructData();
					node.ConstructPort();
				});
			}
			populateEvent.menu.AppendSeparator();
		}
		base.BuildContextualMenu(populateEvent);
	}

	EventBase.EventNodeBase CreateNode(Type type, Vector2 position) {
		if (type == null || !typeof(EventBase).IsAssignableFrom(type)) return null;
		var nodeType = Type.GetType(type.Name + "+" + type.Name + "Node");
		if (nodeType == null) return null;
		var node = Activator.CreateInstance(nodeType) as EventBase.EventNodeBase;
		node.SetPosition(new Rect(position, Vector2.zero));
		AddElement(node);
		return node;
	}

	public override List<Port> GetCompatiblePorts(Port startport, NodeAdapter adapter) {
		return ports.Where(port => {
			bool match = true;
			match = match && port.node != startport.node;
			match = match && port.direction != startport.direction;
			match = match && (byte)port.userData == (byte)startport.userData;
			if (match) {
				var connectedPorts = startport.connections.Select(edge => {
					return edge.output == startport ? edge.input : edge.output;
				}).ToList();
				match = match && !connectedPorts.Contains(port);
			}
			return match;
		}).ToList();
	}



	// IO Methods

	public void Save() {
		if (EventGraph == null) return;
		foreach (var node in nodes.OfType<EventBase.EventNodeBase>()) {
			var eventBase = node.target;
			eventBase.Position = node.GetPosition().position;
			eventBase.Prevs.Clear();
			eventBase.Nexts.Clear();

			var node_iPorts = node.inputContainer.Children().OfType<Port>().ToList();
			foreach (var port in node_iPorts) foreach (var edge in port.connections) {
				if (edge.output.node is EventBase.EventNodeBase prev) {
					var prev_oPorts = prev.outputContainer.Children().OfType<Port>().ToList();
					var iPort = (byte)node_iPorts.IndexOf(edge.input);
					var oPort = (byte)prev_oPorts.IndexOf(edge.output);
					eventBase.Prevs.Add(new EventBase.Connection {
						eventBase = prev.target,
						iPort     = iPort,
						oPort     = oPort,
						iPortType = (PortType)node_iPorts[iPort].userData,
						oPortType = (PortType)prev_oPorts[oPort].userData,
					});
				}
			}
			var node_oPorts = node.outputContainer.Children().OfType<Port>().ToList();
			foreach (var port in node_oPorts) foreach (var edge in port.connections) {
				if (edge.input.node is EventBase.EventNodeBase next) {
					var next_iPorts = next.inputContainer.Children().OfType<Port>().ToList();
					var iPort = (byte)next_iPorts.IndexOf(edge.input);
					var oPort = (byte)node_oPorts.IndexOf(edge.output);
					eventBase.Nexts.Add(new EventBase.Connection {
						eventBase = next.target,
						iPort     = iPort,
						oPort     = oPort,
						iPortType = (PortType)next_iPorts[iPort].userData,
						oPortType = (PortType)node_oPorts[oPort].userData,
					});
				}
			}
			eventBase.Prevs.TrimExcess();
			eventBase.Nexts.TrimExcess();
		}
		EventGraph.Entry.CopyFrom(EventGraph.Clone);
		EditorUtility.SetDirty(EventGraph);
		AssetDatabase.SaveAssets();
		Load();
	}

	public void Load() {
		if (EventGraph == null) return;
		DeleteElements(graphElements);
		var stack = new Stack<EventBase>();
		var cache = new Dictionary<string, EventBase.EventNodeBase>();

		stack.Push(EventGraph.Entry);
		while (0 < stack.Count) {
			var eventBase = stack.Pop();
			if (eventBase == null) continue;
			if (cache.ContainsKey(eventBase.Guid)) continue;
			var node = CreateNode(eventBase.GetType(), eventBase.Position);
			node.target.CopyFrom(eventBase);
			node.ConstructData();
			node.ConstructPort();
			cache.Add(eventBase.Guid, node);
			foreach (var prev in eventBase.Prevs) stack.Push(prev.eventBase);
			foreach (var next in eventBase.Nexts) stack.Push(next.eventBase);
		}
		foreach (var (_, node) in cache) {
			var eventBase = node.target;
			var node_oPorts = node.outputContainer.Children().OfType<Port>().ToList();
			if (eventBase.Nexts != null) for (int i = 0; i < eventBase.Nexts.Count; i++) {
				if (eventBase.Nexts[i].eventBase == null) continue;
				var next = cache[eventBase.Nexts[i].eventBase.Guid];
				var next_iPorts = next.inputContainer.Children().OfType<Port>().ToList();
				var nodeOPort = node_oPorts[eventBase.Nexts[i].oPort];
				var nextIPort = next_iPorts[eventBase.Nexts[i].iPort];
				AddElement(nodeOPort.ConnectTo(nextIPort));
			}
		}
		foreach (var (_, node) in cache) {
			var eventBase = node.target;
			var prev = new List<EventBase.Connection>();
			foreach (var connection in eventBase.Prevs) {
				if (connection.eventBase == null) continue;
				prev.Add(new() {
					eventBase = cache[connection.eventBase.Guid].target,
					iPort     = connection.iPort,
					oPort     = connection.oPort,
					iPortType = connection.iPortType,
					oPortType = connection.oPortType,
				});
			}
			eventBase.Prevs = prev;
			var next = new List<EventBase.Connection>();
				foreach (var connection in eventBase.Nexts) {
				if (connection.eventBase == null) continue;
				next.Add(new() {
					eventBase = cache[connection.eventBase.Guid].target,
					iPort     = connection.iPort,
					oPort     = connection.oPort,
					iPortType = connection.iPortType,
					oPortType = connection.oPortType,
				});
			}
			eventBase.Nexts = next;
		}
		EventGraph.Clone = cache[EventGraph.Entry.Guid].target as EntryEvent;
	}
}
#endif

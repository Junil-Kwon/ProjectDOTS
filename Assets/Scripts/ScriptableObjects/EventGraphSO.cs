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



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Event Graph SO
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[CreateAssetMenu(fileName = "EventGraphSO", menuName = "Scriptable Objects/EventGraph")]
public class EventGraphSO : ScriptableObject {

	#if UNITY_EDITOR
	[CustomEditor(typeof(EventGraphSO))]
	class EventGraphSOEditor : EditorExtensions {
		EventGraphSO I => target as EventGraphSO;
		public override void OnInspectorGUI() {
			Begin("Event Graph SO");

			if (Button("Open Event Graph")) I.Open();
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeReference] EntryEvent m_Entry = new();



	// Properties

	public EntryEvent Entry => m_Entry;

	#if UNITY_EDITOR
	public EntryEvent Clone { get; set; }
	#endif



	// Methods

	#if UNITY_EDITOR
	public void Open() {
		EventGraphWindow.Open(name, this);
	}

	[OnOpenAsset(100)]
	public static bool OnOpen(int instanceID) {
		if (EditorUtility.InstanceIDToObject(instanceID) is EventGraphSO eventGraph) {
			eventGraph.Open();
			return true;
		} 
		return false;
	}
	#endif
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Event Graph Window
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

#if UNITY_EDITOR
public class EventGraphWindow : EditorWindow {

	// Fields

	Toolbar toolbar;
	EventGraphView graphview;
	EventGraphSO graph;



	// Methods

	public static void Open(string name, EventGraphSO graph) {
		var windows = Resources.FindObjectsOfTypeAll<EventGraphWindow>();
		var existingWindow = windows.FirstOrDefault(window => window.graph == graph);
		if (existingWindow != null) existingWindow.Focus();
		else {
			var dock = new[] { typeof(EventGraphWindow), typeof(SceneView) };
			var window = CreateWindow<EventGraphWindow>(name, dock);
			window.graph = graph;
		}
	}

	void OnEnable() {
		toolbar = new Toolbar();
		toolbar.Add(new ToolbarButton(() => graphview?.Save()) { text = "Save" });
		toolbar.Add(new ToolbarButton(() => graphview?.Load()) { text = "Load" });
		rootVisualElement.Insert(0, toolbar);
	}

	void OnGUI() {
		if (graphview == null && graph != null) {
			graphview = new EventGraphView(graph);
			graphview.StretchToParentSize();
			rootVisualElement.Insert(0, graphview);
		}
	}

	void OnDisable() {
		if (toolbar != null) rootVisualElement.Remove(toolbar);
		if (graphview != null) rootVisualElement.Remove(graphview);
		graph.Clone = null;
	}
}
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Event Graph View
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

#if UNITY_EDITOR
public class EventGraphView : GraphView {

	// Fields

	bool framed;
	EventGraphSO graph;



	// Constructor

	public EventGraphView(EventGraphSO graph) {
		this.graph = graph;
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
			if (!framed) {
				FrameAll();
				framed = true;
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



	// Methods

	public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
		if (evt.target is GraphView) {
			var position = contentViewContainer.WorldToLocal(evt.localMousePosition);
			foreach (var dropdown in BaseEvent.BaseEventNode.Dropdown) {
				evt.menu.AppendAction("Create Node/"+ dropdown, _ => {
					var name = Regex.Replace(dropdown.Split('/')[^1], @"\s+", "");
					var type = Type.GetType(name + "Event");
					var node = CreateNode(type, position);
					node.ConstructData();
					node.ConstructPort();
				});
			}
			evt.menu.AppendSeparator();
		}
		// Cut
		// Copy
		// Paste
		// -
		// Delete
		// -
		// Duplicate
		base.BuildContextualMenu(evt);
	}

	BaseEvent.BaseEventNode CreateNode(Type type, Vector2 position) {
		if (type == null || !typeof(BaseEvent).IsAssignableFrom(type)) return null;
		var nodeType = Type.GetType(type.Name + "+" + type.Name + "Node");
		if (nodeType == null) return null;
		var node = Activator.CreateInstance(nodeType) as BaseEvent.BaseEventNode;
		node.SetPosition(new Rect(position, Vector2.zero));
		AddElement(node);
		return node;
	}

	public override List<Port> GetCompatiblePorts(Port startport, NodeAdapter adapter) {
		return ports.Where(port => {
			var match = true;
			if (match) match &= port.node != startport.node;
			if (match) match &= port.direction != startport.direction;
			if (match) match &= (byte)port.userData == (byte)startport.userData;
			if (match) {
				var connectedPorts = startport.connections.Select(edge => {
					return edge.output == startport ? edge.input : edge.output;
				}).ToList();
				match &= !connectedPorts.Contains(port);
			}
			return match;
		}).ToList();
	}



	// IO Methods

	public void Save() {
		if (graph == null) return;
		foreach (var node in nodes.OfType<BaseEvent.BaseEventNode>()) {
			var data = node.target;
			data.prev.Clear();
			data.next.Clear();
			data.position = node.GetPosition().position;

			var node_iPorts = node.inputContainer.Children().OfType<Port>().ToList();
			foreach (var port in node_iPorts) foreach (var edge in port.connections) {
				if (edge.output.node is BaseEvent.BaseEventNode prev) {
					var prev_oPorts = prev.outputContainer.Children().OfType<Port>().ToList();
					var iPort = node_iPorts.IndexOf(edge.input);
					var oPort = prev_oPorts.IndexOf(edge.output);
					data.prev.Add(new BaseEvent.Connection {
						data = prev.target,
						iPort = iPort,
						oPort = oPort,
						iPortType = (PortType)node_iPorts[iPort].userData,
						oPortType = (PortType)prev_oPorts[oPort].userData,
					});
				}
			}
			var node_oPorts = node.outputContainer.Children().OfType<Port>().ToList();
			foreach (var port in node_oPorts) foreach (var edge in port.connections) {
				if (edge.input.node is BaseEvent.BaseEventNode next) {
					var next_iPorts = next.inputContainer.Children().OfType<Port>().ToList();
					var iPort = next_iPorts.IndexOf(edge.input);
					var oPort = node_oPorts.IndexOf(edge.output);
					data.next.Add(new BaseEvent.Connection {
						data = next.target,
						iPort = iPort,
						oPort = oPort,
						iPortType = (PortType)next_iPorts[iPort].userData,
						oPortType = (PortType)node_oPorts[oPort].userData,
					});
				}
			}
		}
		graph.Entry.CopyFrom(graph.Clone);
		EditorUtility.SetDirty(graph);
		AssetDatabase.SaveAssets();
		Load();
	}

	public void Load() {
		if (graph == null) return;
		DeleteElements(graphElements);
		var stack = new Stack<BaseEvent>();
		var cache = new Dictionary<string, BaseEvent.BaseEventNode>();

		stack.Push(graph.Entry);
		while (0 < stack.Count) {
			var data = stack.Pop();
			if (data == null) continue;
			if (cache.ContainsKey(data.guid)) continue;
			var node = CreateNode(data.GetType(), data.position);
			node.target.CopyFrom(data);
			node.ConstructData();
			node.ConstructPort();
			cache.Add(data.guid, node);
			if (data.prev != null) foreach (var prev in data.prev) stack.Push(prev.data);
			if (data.next != null) foreach (var next in data.next) stack.Push(next.data);
		}
		foreach (var (_, node) in cache) {
			var data = node.target;
			var node_oPorts = node.outputContainer.Children().OfType<Port>().ToList();
			if (data.next != null) for (int i = 0; i < data.next.Count; i++) {
				if (data.next[i].data == null) continue;
				var next = cache[data.next[i].data.guid];
				var next_iPorts = next.inputContainer.Children().OfType<Port>().ToList();
				var nodeOPort = node_oPorts[data.next[i].oPort];
				var nextIPort = next_iPorts[data.next[i].iPort];
				AddElement(nodeOPort.ConnectTo(nextIPort));
			}
		}
		foreach (var (_, node) in cache) {
			var data = node.target;
			var prev = new List<BaseEvent.Connection>();
			foreach (var connection in data.prev) {
				if (connection.data == null) continue;
				prev.Add(new() {
					data = cache[connection.data.guid].target,
					iPort = connection.iPort,
					oPort = connection.oPort,
					iPortType = connection.iPortType,
					oPortType = connection.oPortType,
				});
			}
			data.prev = prev;
			var next = new List<BaseEvent.Connection>();
				foreach (var connection in data.next) {
				if (connection.data == null) continue;
				next.Add(new() {
					data = cache[connection.data.guid].target,
					iPort = connection.iPort,
					oPort = connection.oPort,
					iPortType = connection.iPortType,
					oPortType = connection.oPortType,
				});
			}
			data.next = next;
		}
		graph.Clone = cache[graph.Entry.guid].target as EntryEvent;
	}
}
#endif

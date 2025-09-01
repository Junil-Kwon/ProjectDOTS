using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEditor;
#endif



public enum Event {
	None,
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Event Container Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Event Container")]
public sealed class EventContainerAuthoring : MonoComponent<EventContainerAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(EventContainerAuthoring))]
	class EventContainerAuthoringEditor : EditorExtensions {
		EventContainerAuthoring I => target as EventContainerAuthoring;
		int Page { get; set; } = 0;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Prefab", EditorStyles.boldLabel);
			I.SourcePath = TextField("Source Path", I.SourcePath);
			BeginHorizontal();
			PrefixLabel("Load Prefab");
			if (Button("Clear")) I.ClearPrefabData();
			if (Button("Load")) I.LoadPrefabData();
			EndHorizontal();
			Space();

			LabelField("Prefab Data", EditorStyles.boldLabel);
			int eventCount = I.EventList.Count;
			LabelField("Prefab Data Count", $"{eventCount} / {EventCount}");
			Page = BookField(I.PrefabData, 5, Page, (match, index, value) => {
				BeginDisabledGroup();
				if (match) {
					var text = ((Event)index).ToString();
					var name = Regex.Replace(text, @"(?<!^)(?=[A-Z])", " ");
					ObjectField(name, value);
				} else LabelField(" ");
				EndDisabledGroup();
			});
			Space();

			End();
		}
	}
	#endif



	// Constants

	static readonly int EventCount = Enum.GetValues(typeof(Event)).Length;



	// Fields

	[SerializeField] string m_SourcePath = "Assets/Prefabs/Events";
	[SerializeField] List<Event> m_EventList = new();
	[SerializeField] GameObject[] m_PrefabData = new GameObject[EventCount];



	// Properties

	string SourcePath {
		get => m_SourcePath;
		set => m_SourcePath = value;
	}
	List<Event> EventList {
		get => m_EventList;
	}
	GameObject[] PrefabData {
		get => m_PrefabData;
		set => m_PrefabData = value;
	}



	// Methods

	#if UNITY_EDITOR
	void ClearPrefabData() {
		EventList.Clear();
		PrefabData = new GameObject[EventCount];
	}

	void LoadPrefabData() {
		ClearPrefabData();
		foreach (var gameObject in LoadAssets<GameObject>(SourcePath)) {
			if (!Enum.TryParse(gameObject.name, out Event value)) continue;
			EventList.Add(value);
			PrefabData[(int)value] = gameObject;
		}
		EventList.TrimExcess();

		var message = string.Empty;
		for (int i = 0; i < EventCount; i++) if (!PrefabData[i])
			message += $"{(string.IsNullOrEmpty(message) ? string.Empty : ", ")}{(Event)i}";
		if (!string.IsNullOrEmpty(message)) Debug.Log($"Missing events:\n{message}");
	}

	static T[] LoadAssets<T>(string path) where T : UnityEngine.Object {
		var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { path });
		var assets = new T[guids.Length];
		for (int i = 0; i < guids.Length; i++) {
			var assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
			assets[i] = AssetDatabase.LoadAssetAtPath<T>(assetPath);
		}
		return assets;
	}
	#endif



	// Baker 

	class Baker : Baker<EventContainerAuthoring> {
		public override void Bake(EventContainerAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.None);
			var buffer = AddBuffer<EventContainer>(entity);
			buffer.Resize(EventCount, NativeArrayOptions.ClearMemory);
			foreach (var Event in authoring.EventList) {
				var prefab = authoring.PrefabData[(int)Event];
				buffer[(int)Event] = new() {

					Entity = GetEntity(prefab, TransformUsageFlags.Dynamic),

				};
			}
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Event Container
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[InternalBufferCapacity(128)]
public struct EventContainer : IBufferElementData {

	// Fields

	public Entity Entity;
}



public static class EventContainerExtensions {

	// Methods

	public static Entity GetPrefab(
		this in DynamicBuffer<EventContainer> buffer, Event value) {
		return buffer[(int)value].Entity;
	}
}

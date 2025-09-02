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



public enum Terrain {
	None,
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Terrain Container Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Terrain Container")]
public sealed class TerrainContainerAuthoring : MonoComponent<TerrainContainerAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(TerrainContainerAuthoring))]
	class TerrainContainerAuthoringEditor : EditorExtensions {
		TerrainContainerAuthoring I => target as TerrainContainerAuthoring;
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
			int terrainCount = I.TerrainList.Count;
			LabelField("Prefab Data Count", $"{terrainCount} / {TerrainCount}");
			Page = BookField(I.PrefabData, 5, Page, (match, index, value) => {
				BeginDisabledGroup();
				if (match) {
					var text = ((Terrain)index).ToString();
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

	static readonly int TerrainCount = Enum.GetValues(typeof(Terrain)).Length;



	// Fields

	[SerializeField] string m_SourcePath = "Assets/Prefabs/Terrains";
	[SerializeField] List<Terrain> m_TerrainList = new();
	[SerializeField] GameObject[] m_PrefabData = new GameObject[TerrainCount];



	// Properties

	string SourcePath {
		get => m_SourcePath;
		set => m_SourcePath = value;
	}
	List<Terrain> TerrainList {
		get => m_TerrainList;
	}
	GameObject[] PrefabData {
		get => m_PrefabData;
		set => m_PrefabData = value;
	}



	// Methods

	#if UNITY_EDITOR
	void ClearPrefabData() {
		TerrainList.Clear();
		PrefabData = new GameObject[TerrainCount];
	}

	void LoadPrefabData() {
		ClearPrefabData();
		foreach (var gameObject in LoadAssets<GameObject>(SourcePath)) {
			if (!Enum.TryParse(gameObject.name, out Terrain terrain)) continue;
			TerrainList.Add(terrain);
			PrefabData[(int)terrain] = gameObject;
		}
		TerrainList.TrimExcess();

		var message = string.Empty;
		for (int i = 0; i < TerrainCount; i++) if (!PrefabData[i])
			message += $"{(string.IsNullOrEmpty(message) ? string.Empty : ", ")}{(Terrain)i}";
		if (!string.IsNullOrEmpty(message)) Debug.Log($"Missing terrains:\n{message}");
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

	class Baker : Baker<TerrainContainerAuthoring> {
		public override void Bake(TerrainContainerAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.None);
			var buffer = AddBuffer<TerrainContainer>(entity);
			buffer.Resize(TerrainCount, NativeArrayOptions.ClearMemory);
			foreach (var terrain in authoring.TerrainList) {
				var prefab = authoring.PrefabData[(int)terrain];
				buffer[(int)terrain] = new() {

					Entity = GetEntity(prefab, TransformUsageFlags.Dynamic),

				};
			}
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Terrain Container
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[InternalBufferCapacity(512)]
public struct TerrainContainer : IBufferElementData {

	// Fields

	public Entity Entity;
}



public static class TerrainContainerExtensions {

	// Methods

	public static Entity GetPrefab(
		this in DynamicBuffer<TerrainContainer> buffer, Terrain terrain) {
		return buffer[(int)terrain].Entity;
	}
}

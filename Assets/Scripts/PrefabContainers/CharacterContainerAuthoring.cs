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



public enum Character {
	Dummy,
	Player,
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Container Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Character Container")]
public sealed class CharacterContainerAuthoring : MonoComponent<CharacterContainerAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(CharacterContainerAuthoring))]
	class CharacterContainerAuthoringEditor : EditorExtensions {
		CharacterContainerAuthoring I => target as CharacterContainerAuthoring;
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
			int characterCount = I.CharacterList.Count;
			LabelField("Prefab Data Count", $"{characterCount} / {CharacterCount}");
			Page = BookField(I.PrefabData, 5, Page, (match, index, value) => {
				BeginDisabledGroup();
				if (match) {
					var text = ((Character)index).ToString();
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

	static readonly int CharacterCount = Enum.GetValues(typeof(Character)).Length;



	// Fields

	[SerializeField] string m_SourcePath = "Assets/Prefabs/Characters";
	[SerializeField] List<Character> m_CharacterList = new();
	[SerializeField] GameObject[] m_PrefabData = new GameObject[CharacterCount];



	// Properties

	string SourcePath {
		get => m_SourcePath;
		set => m_SourcePath = value;
	}
	List<Character> CharacterList {
		get => m_CharacterList;
	}
	GameObject[] PrefabData {
		get => m_PrefabData;
		set => m_PrefabData = value;
	}



	// Methods

	#if UNITY_EDITOR
	void ClearPrefabData() {
		CharacterList.Clear();
		PrefabData = new GameObject[CharacterCount];
	}

	void LoadPrefabData() {
		ClearPrefabData();
		foreach (var gameObject in LoadAssets<GameObject>(SourcePath)) {
			if (!Enum.TryParse(gameObject.name, out Character character)) continue;
			CharacterList.Add(character);
			PrefabData[(int)character] = gameObject;
		}
		CharacterList.TrimExcess();

		var message = string.Empty;
		for (int i = 0; i < CharacterCount; i++) if (!PrefabData[i])
			message += $"{(string.IsNullOrEmpty(message) ? string.Empty : ", ")}{(Character)i}";
		if (!string.IsNullOrEmpty(message)) Debug.Log($"Missing characters:\n{message}");
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

	class Baker : Baker<CharacterContainerAuthoring> {
		public override void Bake(CharacterContainerAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.None);
			var buffer = AddBuffer<CharacterContainer>(entity);
			buffer.Resize(CharacterCount, NativeArrayOptions.ClearMemory);
			foreach (var character in authoring.CharacterList) {
				var prefab = authoring.PrefabData[(int)character];
				buffer[(int)character] = new() {

					Entity = GetEntity(prefab, TransformUsageFlags.Dynamic),

				};
			}
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Container
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[InternalBufferCapacity(1024)]
public struct CharacterContainer : IBufferElementData {

	// Fields

	public Entity Entity;
}



public static class CharacterContainerExtensions {

	// Methods

	public static Entity GetPrefab(
		this in DynamicBuffer<CharacterContainer> buffer, Character character) {
		return buffer[(int)character].Entity;
	}
}

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



public enum Particle {
	SmokeTiny,
	SmokeSmall,
	SmokeMedium,
	SmokeLarge,
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Particle Container Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Particle Container")]
public sealed class ParticleContainerAuthoring : MonoComponent<ParticleContainerAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(ParticleContainerAuthoring))]
	class ParticleContainerAuthoringEditor : EditorExtensions {
		ParticleContainerAuthoring I => target as ParticleContainerAuthoring;
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
			int particleCount = I.ParticleList.Count;
			LabelField("Prefab Data Count", $"{particleCount} / {ParticleCount}");
			Page = BookField(I.PrefabData, 5, Page, (match, index, value) => {
				BeginDisabledGroup();
				if (match) {
					var text = ((Particle)index).ToString();
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

	static readonly int ParticleCount = Enum.GetValues(typeof(Particle)).Length;



	// Fields

	[SerializeField] string m_SourcePath = "Assets/Prefabs/Particles";
	[SerializeField] List<Particle> m_ParticleList = new();
	[SerializeField] GameObject[] m_PrefabData = new GameObject[ParticleCount];



	// Properties

	string SourcePath {
		get => m_SourcePath;
		set => m_SourcePath = value;
	}
	List<Particle> ParticleList {
		get => m_ParticleList;
	}
	GameObject[] PrefabData {
		get => m_PrefabData;
		set => m_PrefabData = value;
	}



	// Methods

	#if UNITY_EDITOR
	void ClearPrefabData() {
		ParticleList.Clear();
		PrefabData = new GameObject[ParticleCount];
	}

	void LoadPrefabData() {
		ClearPrefabData();
		foreach (var gameObject in LoadAssets<GameObject>(SourcePath)) {
			if (!Enum.TryParse(gameObject.name, out Particle particle)) continue;
			ParticleList.Add(particle);
			PrefabData[(int)particle] = gameObject;
		}
		ParticleList.TrimExcess();

		var message = string.Empty;
		for (int i = 0; i < ParticleCount; i++) if (!PrefabData[i])
			message += $"{(string.IsNullOrEmpty(message) ? string.Empty : ", ")}{(Particle)i}";
		if (!string.IsNullOrEmpty(message)) Debug.Log($"Missing particles:\n{message}");
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

	class Baker : Baker<ParticleContainerAuthoring> {
		public override void Bake(ParticleContainerAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.None);
			var buffer = AddBuffer<ParticleContainer>(entity);
			buffer.Resize(ParticleCount, NativeArrayOptions.ClearMemory);
			foreach (var particle in authoring.ParticleList) {
				var prefab = authoring.PrefabData[(int)particle];
				buffer[(int)particle] = new() {

					Entity = GetEntity(prefab, TransformUsageFlags.Dynamic),

				};
			}
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Particle Container
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[InternalBufferCapacity(128)]
public struct ParticleContainer : IBufferElementData {

	// Fields

	public Entity Entity;
}



public static class ParticleContainerExtensions {

	// Methods

	public static Entity GetPrefab(
		this in DynamicBuffer<ParticleContainer> buffer, Particle particle) {
		return buffer[(int)particle].Entity;
	}
}

using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;

using Unity.Mathematics;
using Unity.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif



[Serializable]
public struct AtlasData {
	public float2 scale;
	public float2 pivot;
	public float2 tiling;
	public float2 offset;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Atlas Map
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[CreateAssetMenu(fileName = "AtlasMap", menuName = "Scriptable Objects/Atlas Map")]
public class AtlasMap : ScriptableObject {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(AtlasMap))]
	class AtlasMapSOEditor : EditorExtensions {
		AtlasMap I => target as AtlasMap;
		int Page { get; set; } = 0;
		public override void OnInspectorGUI() {
			Begin();

			var message = string.Empty;
			message += $"Source Path textures will be reimported with the following settings:\n";
			message += "- Texture Type: Default\n";
			message += "- Read/Write: true\n";
			message += "- Generate Mipmap: false\n";
			message += "- Filter Mode: Point (no filter)\n";
			message += "- Format: RGBA 32 bit";
			HelpBox(message, MessageType.None);
			PropertyField("m_LayerData");
			Space();

			LabelField("Atlas Map", EditorStyles.boldLabel);
			I.PixelPerUnit = FloatField("Pixel Per Unit", I.PixelPerUnit);
			I.Padding = IntField("Padding", I.Padding);
			I.MaxSize = IntField("Max Size", I.MaxSize);
			I.TrimTransparent = Toggle("Trim Transparent", I.TrimTransparent);
			I.MergeDuplicates = Toggle("Merge Duplicates", I.MergeDuplicates);
			BeginHorizontal();
			PrefixLabel("Generate Atlas Map");
			if (Button("Clear")) I.ClearAtlasMap();
			if (Button("Generate")) I.GenerateAtlasMap();

			EndHorizontal();
			Space();

			LabelField("Atlas Map Data", EditorStyles.boldLabel);
			LabelField("Atlas Map Count", I.Count.ToString());
			Page = BookField(I.HashMap, 5, Page, (match, index, value) => {
				BeginDisabledGroup();
				if (match) {
					var scale  = value.Value.scale;
					var pivot  = value.Value.pivot;
					var tiling = value.Value.tiling;
					var offset = value.Value.offset;
					Vector4Field(value.Key, new(scale.x,  scale.y,  pivot.x,  pivot.y));
					Vector4Field("       ", new(tiling.x, tiling.y, offset.x, offset.y));
				} else {
					LabelField(" ");
					LabelField(" ");
				}
				EndDisabledGroup();
			});
			Space();

			End();
		}
	}
	#endif



	// Constants

	[Serializable]
	class LayerEntry {
		public string SourcePath = "Assets/Textures";
		public Texture2D TargetTexture = null;
		public Color FallbackColor = new(1f, 1f, 1f, 0f);
	}



	// Fields

	[SerializeField] LayerEntry[] m_LayerData = new LayerEntry[] { new() };

	[SerializeField] float m_PixelPerUnit = 16f;
	[SerializeField] int m_Padding = 2;
	[SerializeField] int m_MaxSize = 16384;
	[SerializeField] bool m_TrimTransparent = true;
	[SerializeField] bool m_MergeDuplicates = false;
	[SerializeField] HashMap<string, AtlasData> m_HashMap = new();



	// Properties

	LayerEntry[] LayerData {
		get => m_LayerData;
	}

	float PixelPerUnit {
		get => m_PixelPerUnit;
		set => m_PixelPerUnit = value;
	}
	int MaxSize {
		get => m_MaxSize;
		set => m_MaxSize = value;
	}
	int Padding {
		get => m_Padding;
		set => m_Padding = value;
	}
	bool TrimTransparent {
		get => m_TrimTransparent;
		set => m_TrimTransparent = value;
	}
	bool MergeDuplicates {
		get => m_MergeDuplicates;
		set => m_MergeDuplicates = value;
	}
	HashMap<string, AtlasData> HashMap {
		get => m_HashMap;
		set => m_HashMap = value;
	}



	// HashMap Methods

	public AtlasData this[string key] {
		get => HashMap[key];
		set => HashMap[key] = value;
	}
	public int Count => HashMap.Count;

	public HashMap<string, AtlasData>.Enumerator GetEnumerator() => HashMap.GetEnumerator();

	public bool ContainsKey(string key) => HashMap.ContainsKey(key);
	public bool ContainsValue(AtlasData value) => HashMap.ContainsValue(value);
	public bool TryGetValue(string key, out AtlasData value) => HashMap.TryGetValue(key, out value);

	public void Add(string key, AtlasData value) => HashMap.Add(key, value);
	public bool TryAdd(string key, AtlasData value) => HashMap.TryAdd(key, value);
	public bool Remove(string key) => HashMap.Remove(key);
	public bool Remove(string key, out AtlasData value) => HashMap.Remove(key, out value);
	public void Clear() => HashMap.Clear();

	public int EnsureCapacity(int capacity) => HashMap.EnsureCapacity(capacity);



	// Utility Methods

	#if UNITY_EDITOR
	static Texture2D CreateTexture(int width = 1, int height = 1) {
		return CreateTexture(width, height, Color.clear);
	}

	static Texture2D CreateTexture(int width, int height, Color color) {
		var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
		var textureData = texture.GetRawTextureData<Color32>();
		for (int i = 0; i < textureData.Length; i++) textureData[i] = color;
		texture.Apply(false);
		return texture;
	}

	static void EnsureAtlasable(Texture2D texture) {
		var path = AssetDatabase.GetAssetPath(texture);
		var importer = AssetImporter.GetAtPath(path) as TextureImporter;
		var settings = importer.GetDefaultPlatformTextureSettings();
		bool match = false;
		match = match || importer.textureType != TextureImporterType.Default;
		match = match || importer.isReadable != true;
		match = match || importer.mipmapEnabled != false;
		match = match || importer.filterMode != FilterMode.Point;
		match = match || settings.format != TextureImporterFormat.RGBA32;
		if (match) {
			importer.textureType = TextureImporterType.Default;
			importer.isReadable = true;
			importer.mipmapEnabled = false;
			importer.filterMode = FilterMode.Point;
			settings.format = TextureImporterFormat.RGBA32;
			settings.textureCompression = TextureImporterCompression.Uncompressed;
			importer.SetPlatformTextureSettings(settings);
			importer.SaveAndReimport();
		}
	}

	static Texture2D ResizeTexture(Texture2D texture, int width, int height) {
		var source = new RectInt() {
			x = (texture.width  - Mathf.Min(texture.width,  width)) / 2,
			y = (texture.height - Mathf.Min(texture.height, height)) / 2,
			width  = Mathf.Min(texture.width,  width),
			height = Mathf.Min(texture.height, height),
		};
		var target = new RectInt() {
			x = (width  - source.width) / 2,
			y = (height - source.height) / 2,
			width  = source.width,
			height = source.height,
		};
		var resized = CreateTexture(width, height);
		var sourceData = texture.GetRawTextureData<Color32>();
		var targetData = resized.GetRawTextureData<Color32>();
		for (int y = 0; y < source.height; y++) {
			int sourceIndex = source.x + (source.y + y) * texture.width;
			int targetIndex = target.x + (target.y + y) * resized.width;
			int length = source.width;
			NativeArray<Color32>.Copy(sourceData, sourceIndex, targetData, targetIndex, length);
		}
		resized.Apply(false);
		return resized;
	}

	public static Texture2D TrimTexture(Texture2D texture, RectInt rect) {
		var trim = new RectInt() {
			x = Mathf.Max(0, rect.x),
			y = Mathf.Max(0, rect.y),
			width  = Mathf.Min(texture.width,  rect.x + rect.width)  - Mathf.Max(0, rect.x),
			height = Mathf.Min(texture.height, rect.y + rect.height) - Mathf.Max(0, rect.y),
		};
		var trimmed = CreateTexture(trim.width, trim.height);
		var sourceData = texture.GetRawTextureData<Color32>();
		var targetData = trimmed.GetRawTextureData<Color32>();
		for (int y = 0; y < trim.height; y++) {
			int sourceIndex = trim.x + (trim.y + y) * texture.width;
			int targetIndex = 0x0000 + (0x0000 + y) * trimmed.width;
			int length = trim.width;
			NativeArray<Color32>.Copy(sourceData, sourceIndex, targetData, targetIndex, length);
		}
		trimmed.Apply(false);
		return trimmed;
	}

	static Texture2D MergeTextures(Texture2D[] textures) {
		int width  = textures[0].width;
		int height = textures[0].height;
		for (int i = 1; i < textures.Length; i++) {
			if (width  < textures[i].width)  width  = textures[i].width;
			if (height < textures[i].height) height = textures[i].height;
		}
		for (int i = 0; i < textures.Length; i++) {
			if (textures[i].width != width || textures[i].height != height) {
				textures[i] = ResizeTexture(textures[i], width, height);
			}
		}
		var merged = CreateTexture(width, height);
		var mergedData = merged.GetRawTextureData<Color32>();
		foreach (var texture in textures) {
			var textureData = texture.GetRawTextureData<Color32>();
			for (int i = 0; i < mergedData.Length; i++) {
				byte r = (byte)Mathf.Min(mergedData[i].r + textureData[i].r, 255);
				byte g = (byte)Mathf.Min(mergedData[i].g + textureData[i].g, 255);
				byte b = (byte)Mathf.Min(mergedData[i].b + textureData[i].b, 255);
				byte a = (byte)Mathf.Min(mergedData[i].a + textureData[i].a, 255);
				mergedData[i] = new Color32(r, g, b, a);
			}
		}
		merged.Apply(false);
		return merged;
	}

	static RectInt GetOpaqueBounds(Texture2D texture) {
		if (texture == null) return default;
		int xmin = texture.width;
		int ymin = texture.height;
		int xmax = 0;
		int ymax = 0;
		var data = texture.GetRawTextureData<Color32>();
		for (int y = 0; y < texture.height; y++) {
			for (int x = 0; x < texture.width; x++) {
				if (data[x + y * texture.width].a != 0) {
					if (x < xmin) xmin = x;
					if (y < ymin) ymin = y;
					if (xmax < x) xmax = x;
					if (ymax < y) ymax = y;
				}
			}
		}
		if (xmax < xmin || ymax < ymin) return new RectInt(0, 0, 1, 1);
		return new RectInt(xmin, ymin, xmax - xmin + 1, ymax - ymin + 1);
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

	static string GetTextureHash(Texture2D texture) {
		var data = texture.GetRawTextureData<byte>().ToArray();
		using var sha256 = SHA256.Create();
		var hashBytes = sha256.ComputeHash(data);
		return Convert.ToBase64String(hashBytes);
	}
	#endif



	// AtlasMap Methods

	#if UNITY_EDITOR
	public void ClearAtlasMap() {
		int N = LayerData.Length;

		for (int n = 0; n < N; n++) {
			var path = AssetDatabase.GetAssetPath(LayerData[n].TargetTexture);
			if (path == null || path.Equals("")) {
				path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this));
				path = Path.Combine(path, $"{name}_{n}.png");
			}
			var atlas = CreateTexture();
			var bytes = atlas.EncodeToPNG();
			File.WriteAllBytes(path, bytes);
			LayerData[n].TargetTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		}
		HashMap.Clear();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	public void GenerateAtlasMap() {
		int N = LayerData.Length;
		var sourceNM = new Dictionary<string, Texture2D>[N];
		var targetNM = new Dictionary<string, Texture2D>[N];
		for (int n = 0; n < N; n++) {
			var textures = LoadAssets<Texture2D>(LayerData[n].SourcePath);
			sourceNM[n] = new Dictionary<string, Texture2D>();
			targetNM[n] = new Dictionary<string, Texture2D>();
			foreach (var texture in textures) {
				EnsureAtlasable(texture);
				sourceNM[n][texture.name] = texture;
			}
		}
		var hashM = new Dictionary<string, string>();
		var nameM = new Dictionary<string, string>();
		float pixel = 1f / PixelPerUnit;
		HashMap.Clear();

		foreach (var (name, texture) in sourceNM[0]) {
			var tempN = new Texture2D[N];
			tempN[0] = texture;
			for (int n = 1; n < N; n++) {
				var color = LayerData[n].FallbackColor;
				if (sourceNM[n].TryGetValue(name, out var value)) tempN[n] = value;
				else tempN[n] = CreateTexture(texture.width, texture.height, color);
			}
			var merged = MergeTextures(tempN);
			if (TrimTransparent) {
				var bounds = GetOpaqueBounds(merged);
				float x = bounds.x + (0.5f * bounds.width)  - (0.5f * merged.width);
				float y = bounds.y + (0.5f * bounds.height) - (0.5f * merged.height);
				for (int n = 0; n < N; n++) targetNM[n][name] = TrimTexture(tempN[n], bounds);
				var atlasMap = default(AtlasData);
				atlasMap.scale = new(bounds.width * pixel, bounds.height * pixel);
				atlasMap.pivot = new(x * pixel, y * pixel);
				HashMap[name] = atlasMap;
			} else {
				var bounds = new RectInt(0, 0, tempN[0].width, tempN[0].height);
				for (int n = 0; n < N; n++) targetNM[n][name] = TrimTexture(tempN[n], bounds);
				var atlasMap = default(AtlasData);
				atlasMap.scale = new(bounds.width * pixel, bounds.height * pixel);
				atlasMap.pivot = default;
				HashMap[name] = atlasMap;
			}
			if (MergeDuplicates) {
				var hash = GetTextureHash(merged);
				if (!hashM.TryAdd(hash, name)) {
					nameM.Add(name, hashM[hash]);
					for (int n = 0; n < N; n++) targetNM[n].Remove(name);
				}
			}
		}

		var rectM = default(Rect[]);
		for (int n = 0; n < N; n++) {
			var textures = new Texture2D[targetNM[n].Count];
			targetNM[n].Values.CopyTo(textures, 0);
			var path = AssetDatabase.GetAssetPath(LayerData[n].TargetTexture);
			if (path == null || path.Equals("")) {
				path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this));
				path = Path.Combine(path, $"{name}_{n}.png");
			}
			var atlas = CreateTexture();
			var rects = atlas.PackTextures(textures, Padding, MaxSize);
			var bytes = atlas.EncodeToPNG();
			File.WriteAllBytes(path, bytes);
			LayerData[n].TargetTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
			if (n == 0) rectM = rects;
		}

		int m = 0;
		foreach (var name in sourceNM[0].Keys) {
			if (nameM.ContainsKey(name)) continue;
			var atlasMap = HashMap[name];
			atlasMap.tiling = new(rectM[m].width, rectM[m].height);
			atlasMap.offset = new(rectM[m].x, rectM[m].y);
			HashMap[name] = atlasMap;
			m++;
		}
		foreach (var name in nameM.Keys) {
			var atlasMap = HashMap[name];
			atlasMap.tiling = HashMap[nameM[name]].tiling;
			atlasMap.offset = HashMap[nameM[name]].offset;
			HashMap[name] = atlasMap;
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}
	#endif
}

using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Unity.Mathematics;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━

[Serializable]
public struct AtlasData {
	public float2 scale;
	public float2 pivot;
	public float2 tiling;
	public float2 offset;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Atlas Map SO
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[CreateAssetMenu(fileName = "AtlasMapSO", menuName = "Scriptable Objects/AtlasMap")]
public class AtlasMapSO : ScriptableObject {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(AtlasMapSO))]
		class AtlasMapSOEditor : EditorExtensions {
			AtlasMapSO I => target as AtlasMapSO;
			public override void OnInspectorGUI() {
				Begin("Atlas Map SO");

				LabelField("Texture", EditorStyles.boldLabel);
				PropertyField("m_Data");
				Space();

				LabelField("Atlas Map", EditorStyles.boldLabel);
				I.MaximumAtlasSize = IntField  ("Maximum Atlas Size", I.MaximumAtlasSize);
				I.Padding          = IntField  ("Padding",            I.Padding);
				I.PixelPerUnit     = FloatField("Pixel Per Unit",     I.PixelPerUnit);
				I.MergeDuplicate   = Toggle    ("Merge Duplicate",    I.MergeDuplicate);
				BeginHorizontal();
				PrefixLabel("Generate Atlas Map");
				if (Button("Clear"   )) I.ClearAtlasMap();
				if (Button("Generate")) I.GenerateAtlasMap();
				EndHorizontal();
				Space();

				LabelField("Atlas Map Data", EditorStyles.boldLabel);
				LabelField("Count", I.Count.ToString());
				Space();
				BeginDisabledGroup(true);
				int max = Mathf.Min(30, I.Count);
				for (int i = 0; i < max; i++) {
					var data = I.ElementAt(i).Value;
					var uv = new Vector4(data.offset.x, data.offset.y, data.tiling.x, data.tiling.y);
					Vector4Field(I.AtlasMap.ElementAt(i).Key, uv);
				}
				if (max < I.Count) LabelField("...");
				EndDisabledGroup();
				Space();

				End();
			}
		}
	#endif



	// Constants

	[Serializable]
	class HashMap : HashMap<string, AtlasData> { }

	[Serializable]
	public class DataSet {
		public string    SourceTexture = "Assets/Textures";
		public Texture2D TargetTexture = null;
		public Color32   FallbackColor = Color.clear;
	}



	// Fields

	[SerializeField] DataSet[] m_Data             = new DataSet[] { new() };
	[SerializeField] int       m_MaximumAtlasSize = 16384;
	[SerializeField] int       m_Padding          = 2;
	[SerializeField] float     m_PixelPerUnit     = 16f;
	[SerializeField] bool      m_MergeDuplicate   = false;
	[SerializeField] HashMap   m_AtlasMap         = new();

	bool m_IsDirty = false;



	// Properties

	public DataSet[] Data => m_Data;

	public int MaximumAtlasSize {
		get => m_MaximumAtlasSize;
		set => m_MaximumAtlasSize = value;
	}
	public int Padding {
		get => m_Padding;
		set => m_Padding = value;
	}
	public float PixelPerUnit {
		get => m_PixelPerUnit;
		set => m_PixelPerUnit = value;
	}
	public bool MergeDuplicate {
		get => m_MergeDuplicate;
		set => m_MergeDuplicate = value;
	}
	HashMap AtlasMap {
		get => m_AtlasMap;
		set => m_AtlasMap = value;
	}

	public bool IsDirty {
		get => m_IsDirty;
		set => m_IsDirty = value;
	}



	// HashMap Methods

	public AtlasData this[string key] {
		get => AtlasMap[key];
		set => AtlasMap[key] = value;
	}
	public int Count => AtlasMap.Count;

	public HashMap.Enumerator GetEnumerator() => AtlasMap.GetEnumerator();
	public KeyValuePair<string, AtlasData> ElementAt(int index) => AtlasMap.ElementAt(index);

	public void Add        (string key,     AtlasData value) => AtlasMap.Add        (key,     value);
	public bool TryAdd     (string key,     AtlasData value) => AtlasMap.TryAdd     (key,     value);
	public bool TryGetValue(string key, out AtlasData value) => AtlasMap.TryGetValue(key, out value);

	public void Clear() => AtlasMap.Clear();
	public bool Remove(string key                     ) => AtlasMap.Remove(key           );
	public bool Remove(string key, out AtlasData value) => AtlasMap.Remove(key, out value);

	public bool ContainsKey  (string    key  ) => AtlasMap.ContainsKey  (key  );
	public bool ContainsValue(AtlasData value) => AtlasMap.ContainsValue(value);

	public int EnsureCapacity(int capacity) => AtlasMap.EnsureCapacity(capacity);



	// Utility Methods

	#if UNITY_EDITOR
		public static T[] LoadAsset<T>(string path) where T : UnityEngine.Object {
			var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { path });
			var assets = new T[guids.Length];
			for (int i = 0; i < guids.Length; i++) {
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
				assets[i] = AssetDatabase.LoadAssetAtPath<T>(assetPath);
			}
			return assets;
		}

		public static Texture2D CreateTexture(int width = 1, int height = 1) {
			return CreateTexture(width, height, Color.clear);
		}
		public static Texture2D CreateTexture(int width, int height, Color color) {
			var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
			texture.SetPixels(Enumerable.Repeat(color, width * height).ToArray());
			texture.Apply();
			return texture;
		}

		public static Texture2D ResizeTexture(Texture2D texture, int width, int height) {
			return ResizeTexture(texture, new RectInt(0, 0, width, height));
		}
		public static Texture2D ResizeTexture(Texture2D texture, RectInt rect) {
			var copy = default(RectInt);
			copy.x = math.max(0, rect.x);
			copy.y = math.max(0, rect.y);
			copy.width  = math.min(texture.width,  rect.width );
			copy.height = math.min(texture.height, rect.height);

			var resized = CreateTexture(rect.width, rect.height);
			resized.SetPixels(texture.GetPixels(copy.x, copy.y, copy.width, copy.height));
			resized.Apply();
			return resized;
		}

		public static int GetTextureHash(Texture2D texture) {
			var pixels = texture.GetPixels();
			int hash = 17;
			int step = 1;
			if (4096 < pixels.Length) step = Mathf.FloorToInt(Mathf.Sqrt(pixels.Length / 4096));
			for (int i = 0; i < pixels.Length; i += step) {
				Color pixel = pixels[i];
				int r = (int)(pixel.r * 255);
				int g = (int)(pixel.g * 255);
				int b = (int)(pixel.b * 255);
				int a = (int)(pixel.a * 255);
				hash = (hash * 31) ^ (r << 24 | g << 16 | b <<  8 | a <<  0);
			}
			return hash;
		}
	#endif



	// Atlas Methods

	#if UNITY_EDITOR
		public void ClearAtlasMap() {
			int N = Data.Length;

			for (int n = 0; n < N; n++) {
				var path = AssetDatabase.GetAssetPath(Data[n].TargetTexture);
				if (path == null || path.Equals("")) {
					path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this));
					path = Path.Combine(path, $"{name}_{n}.png");
				}
				var atlas = CreateTexture();
				var bytes = atlas.EncodeToPNG();
				File.WriteAllBytes(path, bytes);
				AssetDatabase.Refresh();
				Data[n].TargetTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
			}
			AtlasMap.Clear();
			IsDirty = true;
		}

		public void GenerateAtlasMap() {
			int N = Data.Length;
			int M;
			var sourceNM = new Texture2D[N][];
			var targetNM = new Texture2D[N][];
			for (int n = 0; n < N; n++) {
				sourceNM[n] = LoadAsset<Texture2D>(Data[n].SourceTexture);
				targetNM[n] = new Texture2D[sourceNM[0].Length];
			}
			M = sourceNM[0].Length;
			var hash2index = new Dictionary<int,      int >();
			var index2list = new Dictionary<int, List<int>>();
			var scaleM     = new Vector2[M];
			var pivotM     = new Vector2[M];
			for (int m = 0; m < M; m++) {

				var tempN = new Texture2D[N];
				tempN[0] = sourceNM[0][m];
				for (int n = 1; n < N; n++) {
					var temp = sourceNM[n].FirstOrDefault(t => t.name.Equals(tempN[0].name));
					if (temp) tempN[n] = temp;
					else {
						Color color = Data[n].FallbackColor;
						tempN[n] = CreateTexture(tempN[0].width, tempN[0].height, color);
					}
				}

				int width  = tempN.Select(t => t.width ).Max();
				int height = tempN.Select(t => t.height).Max();
				for (int n = 0; n < N; n++) tempN[n] = ResizeTexture(tempN[n], width, height);
				var merged = CreateTexture(width, height);
				for (int n = 0; n < N; n++) {
					var mPixels = merged  .GetPixels(0, 0, width, height);
					var tPixels = tempN[n].GetPixels(0, 0, width, height);
					for (int i = 0; i < mPixels.Length; i++) mPixels[i] += tPixels[i];
					merged.SetPixels(mPixels);
				}
				merged.Apply();

				if (MergeDuplicate) {
					int hash = GetTextureHash(merged);
					if (hash2index.TryGetValue(hash, out int index)) {
						index2list[index].Add(m);
						continue;
					}
					else {
						hash2index.Add(hash,  m);
						index2list.Add(m, new());
					}
				}

				int xmin = merged.width,  xmax = 0;
				int ymin = merged.height, ymax = 0;
				var pixels = merged.GetPixels();
				for (int y = 0; y < merged.height; y++) {
					for (int x = 0; x < merged.width; x++) {
						if (0f < pixels[x + y * merged.width].a) {
							xmin = math.min(xmin, x);
							xmax = math.max(x, xmax);
							ymin = math.min(ymin, y);
							ymax = math.max(y, ymax);
						}
					}
				}
				if (xmax < xmin || ymax < ymin) {
					xmin = 0; xmax = 0;
					ymin = 0; ymax = 0;
				}
				scaleM[m].x = xmax - xmin + 1;
				scaleM[m].y = ymax - ymin + 1;
				pivotM[m].x = xmin + scaleM[m].x * 0.5f - merged.width  * 0.5f;
				pivotM[m].y = ymin + scaleM[m].y * 0.5f - merged.height * 0.5f;
				var rect = new RectInt(xmin, ymin, (int)scaleM[m].x, (int)scaleM[m].y);
				for (int n = 0; n < N; n++) targetNM[n][m] = ResizeTexture(tempN[n], rect);
			}

			var rectM = new Rect[M];
			for (int n = 0; n < N; n++) {
				var path = AssetDatabase.GetAssetPath(Data[n].TargetTexture);
				if (path == null || path.Equals("")) {
					path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this));
					path = Path.Combine(path, $"{name}_{n}.png");
				}
				var atlas = CreateTexture();
				var rects = atlas.PackTextures(targetNM[n], Padding, MaximumAtlasSize);
				var bytes = atlas.EncodeToPNG();
				File.WriteAllBytes(path, bytes);
				AssetDatabase.Refresh();
				Data[n].TargetTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

				if (n == 0) {
					for (int m = 0; m < M; m++) rectM[m] = rects[m];
					foreach (var i in index2list) foreach (var j in i.Value) {
						scaleM[j] = scaleM[i.Key];
						pivotM[j] = pivotM[i.Key];
						rectM [j] = rectM [i.Key];
					}
				}
			}

			AtlasMap.Clear();
			var inv = 1f / PixelPerUnit;
			for (int m = 0; m < M; m++) {
				AtlasMap.Add(sourceNM[0][m].name, new AtlasData {
					scale  = scaleM[m] * inv,
					pivot  = pivotM[m] * inv,
					tiling = new(rectM[m].width, rectM[m].height),
					offset = new(rectM[m].x,     rectM[m].y     ),
				});
				var a = AtlasMap.ElementAt(AtlasMap.Count - 1);
			}
			IsDirty = true;
		}
	#endif
}

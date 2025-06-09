using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// Shader Structures

public struct TileDrawData {
	public float3 position;
	public float4 rotation;
	public float3 scale;

	public float2 tiling;
	public float2 offset;
	public color  basecolor;
	public color  maskcolor;
	public color  emission;
}

public struct SpriteDrawData {
	public float3 position;
	public float2 scale;
	public float2 pivot;

	public float2 tiling;
	public float2 offset;
	public color  basecolor;
	public color  maskcolor;
	public color  emission;
}

public struct ShadowDrawData {
	public float3 position;
	public float2 scale;
	public float2 pivot;

	public float2 tiling;
	public float2 offset;
}

public struct UIDrawData {
	public float3 position;
	public float2 scale;
	public float2 pivot;

	public float2 tiling;
	public float2 offset;
	public color  basecolor;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Draw Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/Draw Manager")]
public sealed class DrawManager : MonoSingleton<DrawManager> {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(DrawManager))]
		class DrawManagerEditor : EditorExtensions {
			DrawManager I => target as DrawManager;
			public override void OnInspectorGUI() {
				Begin("Draw Manager");

				LabelField("Material", EditorStyles.boldLabel);
				TileMaterial   = ObjectField("Tile Material",   TileMaterial);
				SpriteMaterial = ObjectField("Sprite Material", SpriteMaterial);
				ShadowMaterial = ObjectField("Shadow Material", ShadowMaterial);
				UIMaterial     = ObjectField("UI Material",     UIMaterial);
				Space();
				LabelField("Atlas Map", EditorStyles.boldLabel);
				TileAtlasMapPath   = TextField("Tile Atlas Map Path",   TileAtlasMapPath);
				SpriteAtlasMapPath = TextField("Sprite Atlas Map Path", SpriteAtlasMapPath);
				ShadowAtlasMapPath = TextField("Shadow Atlas Map Path", ShadowAtlasMapPath);
				UIAtlasMapPath     = TextField("UI Atlas Map Path",     UIAtlasMapPath);
				BeginHorizontal();
				PrefixLabel("Load Atlas Map");
				if (Button("Clear")) ClearDataAll();
				if (Button("Load" )) LoadDataAll();
				EndHorizontal();
				AutoLoad = Toggle("Auto Load", AutoLoad);
				Space();
				LabelField("Editor Mesh", EditorStyles.boldLabel);
				CachePath = TextField("Cache Path", CachePath);
				BeginHorizontal();
				PrefixLabel("Draw Editor Mesh");
				if (Button("Clear")) ClearEditorMeshAll();
				if (Button("Draw" )) DrawEditorMeshAll();
				EndHorizontal();
				AutoDraw = Toggle("Auto Draw", AutoDraw);
				Space();
				LabelField("Debug", EditorStyles.boldLabel);
				BeginDisabledGroup();
				int lutable = Marshal.SizeOf<uint>() + Marshal.SizeOf<uint>();
				int datamap = Marshal.SizeOf<uint>() + Marshal.SizeOf<AtlasData>();
				int tile   = lutable * TileLUTable.Count   + datamap * TileDataMap.Count;
				int sprite = lutable * SpriteLUTable.Count + datamap * SpriteDataMap.Count;
				int shadow = lutable * ShadowLUTable.Count + datamap * ShadowDataMap.Count;
				int ui     = lutable * UILUTable.Count     + datamap * UIDataMap.Count;
				TextField("Tile Atlas Map Size",   $"{TileDataMap.Count  } ({tile:N0} Bytes)");
				TextField("Sprite Atlas Map Size", $"{SpriteDataMap.Count} ({sprite:N0} Bytes)");
				TextField("Shadow Atlas Map Size", $"{ShadowDataMap.Count} ({shadow:N0} Bytes)");
				TextField("UI Atlas Map Size",     $"{UIDataMap.Count    } ({ui:N0} Bytes)");
				EndDisabledGroup();

				End();
			}
		}
	#endif



	// Constants

	public const float SampleRate = 60f;



	// Fields

	Mesh m_TileMesh;
	Mesh m_SpriteMesh;
	Mesh m_ShadowMesh;
	Mesh m_UIMesh;

	[SerializeField] Material m_TileMaterial;
	[SerializeField] Material m_SpriteMaterial;
	[SerializeField] Material m_ShadowMaterial;
	[SerializeField] Material m_UIMaterial;

	[SerializeField] HashMap<uint, uint> m_TileLUTable;
	[SerializeField] HashMap<uint, uint> m_SpriteLUTable;
	[SerializeField] HashMap<uint, uint> m_ShadowLUTable;
	[SerializeField] HashMap<uint, uint> m_UILUTable;

	[SerializeField] HashMap<uint, AtlasData> m_TileDataMap;
	[SerializeField] HashMap<uint, AtlasData> m_SpriteDataMap;
	[SerializeField] HashMap<uint, AtlasData> m_ShadowDataMap;
	[SerializeField] HashMap<uint, AtlasData> m_UIDataMap;

	[SerializeField] string m_TileAtlasMapPath   = "Assets/Textures/TileAtlasMap.asset";
	[SerializeField] string m_SpriteAtlasMapPath = "Assets/Textures/SpriteAtlasMap.asset";
	[SerializeField] string m_ShadowAtlasMapPath = "Assets/Textures/ShadowAtlasMap.asset";
	[SerializeField] string m_UIAtlasMapPath     = "Assets/Textures/UIAtlasMap.asset";
	[SerializeField] bool m_AutoLoad = true;

	[SerializeField] string m_CachePath = "Assets/Materials/Caches";
	[SerializeField] bool m_AutoDraw = true;



	// Properties

	public static Mesh TileMesh {
		get => Instance.m_TileMesh ?
			   Instance.m_TileMesh : Instance.m_TileMesh = GetTileMesh();
		private set => Instance.m_TileMesh = value;
	}
	public static Mesh SpriteMesh {
		get => Instance.m_SpriteMesh ?
			   Instance.m_SpriteMesh : Instance.m_SpriteMesh = GetSpriteMesh();
		private set => Instance.m_SpriteMesh = value;
	}
	public static Mesh ShadowMesh {
		get => Instance.m_ShadowMesh ?
			   Instance.m_ShadowMesh : Instance.m_ShadowMesh = GetShadowMesh();
		private set => Instance.m_ShadowMesh = value;
	}
	public static Mesh UIMesh {
		get => Instance.m_UIMesh ?
			   Instance.m_UIMesh : Instance.m_UIMesh = GetUIMesh();
		private set => Instance.m_UIMesh = value;
	}

	public static Material TileMaterial {
		get         => Instance.m_TileMaterial;
		private set => Instance.m_TileMaterial = value;
	}
	public static Material SpriteMaterial {
		get         => Instance.m_SpriteMaterial;
		private set => Instance.m_SpriteMaterial = value;
	}
	public static Material ShadowMaterial {
		get         => Instance.m_ShadowMaterial;
		private set => Instance.m_ShadowMaterial = value;
	}
	public static Material UIMaterial {
		get         => Instance.m_UIMaterial;
		private set => Instance.m_UIMaterial = value;
	}

	public static HashMap<uint, uint> TileLUTable {
		get         => Instance.m_TileLUTable;
		private set => Instance.m_TileLUTable = value;
	}
	public static HashMap<uint, uint> SpriteLUTable {
		get         => Instance.m_SpriteLUTable;
		private set => Instance.m_SpriteLUTable = value;
	}
	public static HashMap<uint, uint> ShadowLUTable {
		get         => Instance.m_ShadowLUTable;
		private set => Instance.m_ShadowLUTable = value;
	}
	public static HashMap<uint, uint> UILUTable {
		get         => Instance.m_UILUTable;
		private set => Instance.m_UILUTable = value;
	}

	public static HashMap<uint, AtlasData> TileDataMap {
		get         => Instance.m_TileDataMap;
		private set => Instance.m_TileDataMap = value;
	}
	public static HashMap<uint, AtlasData> SpriteDataMap {
		get         => Instance.m_SpriteDataMap;
		private set => Instance.m_SpriteDataMap = value;
	}
	public static HashMap<uint, AtlasData> ShadowDataMap {
		get         => Instance.m_ShadowDataMap;
		private set => Instance.m_ShadowDataMap = value;
	}
	public static HashMap<uint, AtlasData> UIDataMap {
		get         => Instance.m_UIDataMap;
		private set => Instance.m_UIDataMap = value;
	}

	static string TileAtlasMapPath {
		get => Instance.m_TileAtlasMapPath;
		set => Instance.m_TileAtlasMapPath = value;
	}
	static string SpriteAtlasMapPath {
		get => Instance.m_SpriteAtlasMapPath;
		set => Instance.m_SpriteAtlasMapPath = value;
	}
	static string ShadowAtlasMapPath {
		get => Instance.m_ShadowAtlasMapPath;
		set => Instance.m_ShadowAtlasMapPath = value;
	}
	static string UIAtlasMapPath {
		get => Instance.m_UIAtlasMapPath;
		set => Instance.m_UIAtlasMapPath = value;
	}
	static bool AutoLoad {
		get => Instance.m_AutoLoad;
		set {
			if (Instance.m_AutoLoad != value) {
				Instance.m_AutoLoad = value;
				#if UNITY_EDITOR
					if (value) LoadDataAll();
				#endif
			}
		}
	}

	static string CachePath {
		get => Instance.m_CachePath;
		set => Instance.m_CachePath = value;
	}
	static bool AutoDraw {
		get => Instance.m_AutoDraw;
		set {
			if (Instance.m_AutoDraw != value) {
				Instance.m_AutoDraw = value;
				#if UNITY_EDITOR
					if (value) DrawEditorMeshAll();
					else ClearEditorMeshAll();
				#endif
			}
		}
	}



	// Editor Methods

	#if UNITY_EDITOR
		static T LoadAsset<T>(string path) where T : UnityEngine.Object {
			return AssetDatabase.LoadAssetAtPath<T>(path);
		}

		static double time;

		[InitializeOnLoadMethod]
		static void InitializeOnLoadMethod() => EditorApplication.update += () => {
			if (Instance == false) return;
			if (AutoLoad && time + 5.0 < EditorApplication.timeSinceStartup) {
				time = EditorApplication.timeSinceStartup;
				var flag = false;
				var tileAtlasMap = LoadAsset<AtlasMapSO>(TileAtlasMapPath);
				if (tileAtlasMap && tileAtlasMap.IsDirty) {
					tileAtlasMap.IsDirty = false;
					LoadTileData(tileAtlasMap);
					flag = true;
				}
				var spriteAtlasMap = LoadAsset<AtlasMapSO>(SpriteAtlasMapPath);
				if (spriteAtlasMap && spriteAtlasMap.IsDirty) {
					spriteAtlasMap.IsDirty = false;
					LoadSpriteData(spriteAtlasMap);
					flag = true;
				}
				var shadowAtlasMap = LoadAsset<AtlasMapSO>(ShadowAtlasMapPath);
				if (shadowAtlasMap && shadowAtlasMap.IsDirty) {
					shadowAtlasMap.IsDirty = false;
					LoadShadowData(shadowAtlasMap);
					flag = true;
				}
				var uiAtlasMap = LoadAsset<AtlasMapSO>(UIAtlasMapPath);
				if (uiAtlasMap && uiAtlasMap.IsDirty) {
					uiAtlasMap.IsDirty = false;
					LoadUIData(uiAtlasMap);
					flag = true;
				}
				if (flag && AutoDraw) DrawEditorMeshAll();
			}
			if (AutoDraw && Selection.activeGameObject) {
				var status = PrefabUtility.GetPrefabInstanceStatus(Selection.activeGameObject);
				if (status != PrefabInstanceStatus.Connected && !Application.isPlaying) {
					DrawEditorMesh(Selection.activeGameObject);
				}
			}
		};

		static void ClearDataAll() {
			ClearTileData();
			ClearSpriteData();
			ClearShadowData();
			ClearUIData();
		}

		static void LoadDataAll() {
			LoadTileData  (LoadAsset<AtlasMapSO>(TileAtlasMapPath));
			LoadSpriteData(LoadAsset<AtlasMapSO>(SpriteAtlasMapPath));
			LoadShadowData(LoadAsset<AtlasMapSO>(ShadowAtlasMapPath));
			LoadUIData    (LoadAsset<AtlasMapSO>(UIAtlasMapPath));
		}

		static void ClearEditorMeshAll() {
			foreach (var prefab in Resources.LoadAll<GameObject>("Prefabs")) {
				if (prefab.TryGetComponent(out MeshFilter   filter  )) DestroyImmediate(filter, true);
				if (prefab.TryGetComponent(out MeshRenderer renderer)) DestroyImmediate(renderer, true);

				var prefabPath = AssetDatabase.GetAssetPath(prefab);
				PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
			}
			AssetDatabase.Refresh();
		}

		static void DrawEditorMeshAll() {
			foreach (var prefab in Resources.LoadAll<GameObject>("Prefabs")) {
				if (!prefab.TryGetComponent(out MeshFilter   _)) prefab.AddComponent<MeshFilter>();
				if (!prefab.TryGetComponent(out MeshRenderer _)) prefab.AddComponent<MeshRenderer>();
				DrawEditorMesh(prefab);
				var prefabPath = AssetDatabase.GetAssetPath(prefab);
				PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
			}
			AssetDatabase.Refresh();
		}

		static void DrawEditorMesh(GameObject gameObject) {
			if (!gameObject) return;
			if (!gameObject.TryGetComponent(out MeshFilter filter)) return;
			if (!gameObject.TryGetComponent(out MeshRenderer renderer)) return;
			var tileArray   = gameObject.GetComponents<TileDrawerAuthoring>();
			var spriteArray = gameObject.GetComponents<SpriteDrawerAuthoring>();
			var shadowArray = gameObject.GetComponents<ShadowDrawerAuthoring>();
			var uiArray     = gameObject.GetComponents<UIDrawerAuthoring>();
			var match = false;
			match |= 0 < tileArray.Length;
			match |= 0 < spriteArray.Length;
			match |= 0 < shadowArray.Length;
			match |= 0 < uiArray.Length;
			if (!match) return;

			var path = Path.Combine(CachePath, $"{gameObject.name}.asset");
			var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
			if (mesh == null) {
				mesh = new Mesh();
				AssetDatabase.CreateAsset(mesh, path);
			}
			var vertices  = new List<Vector3>();
			var uvs       = new List<Vector2>();
			var normals   = new List<Vector3>();
			var triangles = new List<int[]>();
			var offset    = 0;
			var materials = new List<Material>();

			foreach (var tile in tileArray) {
				var data     = GetTileData(gameObject.transform, tile);
				var position = tile.Position;
				var rotation = tile.Rotation;
				var scale    = new Vector3(data.scale.x, data.scale.y, 1f);
				var pivot    = (Vector3)data.position - gameObject.transform.position - position;
				Vector3 vertex  (Vector3 i) => position + rotation * Vector3.Scale(i, scale) + pivot;
				Vector2 uv      (Vector2 i) => i;
				Vector3 normal  (Vector3 i) => rotation * i;
				int     triangle(int     i) => offset + i;

				vertices .AddRange(TileMesh.vertices .Select(vertex));
				uvs      .AddRange(TileMesh.uv       .Select(uv));
				normals  .AddRange(TileMesh.normals  .Select(normal));
				triangles.Add     (TileMesh.triangles.Select(triangle).ToArray());
				offset += TileMesh.vertexCount;
				path = Path.Combine(CachePath, $"{gameObject.name}Tile{materials.Count}.mat");
				var material = AssetDatabase.LoadAssetAtPath<Material>(path);
				if (material == null) {
					material = new Material(TileMaterial);
					AssetDatabase.CreateAsset(material, path);
				}
				material.SetVector("_Tiling",    new(data.tiling.x, data.tiling.y));
				material.SetVector("_Offset",    new(data.offset.x, data.offset.y));
				material.SetColor ("_BaseColor", data.basecolor);
				material.SetColor ("_MaskColor", data.maskcolor);
				material.SetColor ("_Emission",  data.emission);
				materials.Add(material);
			}
			foreach (var sprite in spriteArray) {
				var data     = GetSpriteData(gameObject.transform, sprite);
				var position = sprite.Position;
				var rotation = Quaternion.Euler(30f, 0f, 0f);
				var scale    = new Vector3(data.scale.x, data.scale.y, 1f);
				var pivot    = rotation * new Vector3(data.pivot.x, data.pivot.y, 0f);
				Vector3 vertex  (Vector3 i) => position + rotation * Vector3.Scale(i, scale) + pivot;
				Vector2 uv      (Vector2 i) => i;
				Vector3 normal  (Vector3 i) => rotation * i;
				int     triangle(int     i) => offset + i;

				vertices .AddRange(SpriteMesh.vertices .Select(vertex));
				uvs      .AddRange(SpriteMesh.uv       .Select(uv));
				normals  .AddRange(SpriteMesh.normals  .Select(normal));
				triangles.Add     (SpriteMesh.triangles.Select(triangle).ToArray());
				offset += SpriteMesh.vertexCount;
				path = Path.Combine(CachePath, $"{gameObject.name}Sprite{materials.Count}.mat");
				var material = AssetDatabase.LoadAssetAtPath<Material>(path);
				if (material == null) {
					material = new Material(SpriteMaterial);
					AssetDatabase.CreateAsset(material, path);
				}
				material.SetVector("_Tiling",    new(data.tiling.x, data.tiling.y));
				material.SetVector("_Offset",    new(data.offset.x, data.offset.y));
				material.SetColor ("_BaseColor", data.basecolor);
				material.SetColor ("_MaskColor", data.maskcolor);
				material.SetColor ("_Emission",  data.emission);
				materials.Add(material);
			}
			foreach (var shadow in shadowArray) {
				var data     = GetShadowData(gameObject.transform, shadow);
				var position = shadow.Position;
				var rotation = Quaternion.Euler(0f, 0f, 0f);
				var scale    = new Vector3(data.scale.x, 1f, data.scale.y);
				var pivot    = (Vector3)data.position - gameObject.transform.position - position;
				Vector3 vertex  (Vector3 i) => position + rotation * Vector3.Scale(i, scale) + pivot;
				Vector2 uv      (Vector2 i) => i;
				Vector3 normal  (Vector3 i) => rotation * i;
				int     triangle(int     i) => offset + i;

				vertices .AddRange(ShadowMesh.vertices .Select(vertex));
				uvs      .AddRange(ShadowMesh.uv       .Select(uv));
				normals  .AddRange(ShadowMesh.normals  .Select(normal));
				triangles.Add     (ShadowMesh.triangles.Select(triangle).ToArray());
				offset += ShadowMesh.vertexCount;
				path = Path.Combine(CachePath, $"{gameObject.name}Shadow{materials.Count}.mat");
				var material = AssetDatabase.LoadAssetAtPath<Material>(path);
				if (material == null) {
					material = new Material(ShadowMaterial);
					AssetDatabase.CreateAsset(material, path);
				}
				material.SetVector("_Tiling", new(data.tiling.x, data.tiling.y));
				material.SetVector("_Offset", new(data.offset.x, data.offset.y));
				materials.Add(material);
			}
			foreach (var ui in uiArray) {
				var data     = GetUIData(gameObject.transform, ui);
				var position = ui.Position;
				var rotation = Quaternion.Euler(30f, 0f, 0f);
				var scale    = new Vector3(data.scale.x, data.scale.y, 1f);
				var pivot    = rotation * new Vector3(data.pivot.x, data.pivot.y, 0f);
				Vector3 vertex  (Vector3 i) => position + rotation * Vector3.Scale(i, scale) + pivot;
				Vector2 uv      (Vector2 i) => i;
				Vector3 normal  (Vector3 i) => rotation * i;
				int     triangle(int     i) => offset + i;

				vertices .AddRange(UIMesh.vertices .Select(vertex));
				uvs      .AddRange(UIMesh.uv       .Select(uv));
				normals  .AddRange(UIMesh.normals  .Select(normal));
				triangles.Add     (UIMesh.triangles.Select(triangle).ToArray());
				offset += UIMesh.vertexCount;
				path = Path.Combine(CachePath, $"{gameObject.name}UI{materials.Count}.mat");
				var material = AssetDatabase.LoadAssetAtPath<Material>(path);
				if (material == null) {
					material = new Material(UIMaterial);
					AssetDatabase.CreateAsset(material, path);
				}
				material.SetVector("_Tiling",    new(data.tiling.x, data.tiling.y));
				material.SetVector("_Offset",    new(data.offset.x, data.offset.y));
				material.SetColor ("_BaseColor", data.basecolor);
				materials.Add(material);
			}
			mesh.Clear();
			mesh.name = gameObject.name;
			mesh.subMeshCount = triangles.Count;
			mesh.SetVertices(vertices);
			mesh.SetUVs(0, uvs);
			mesh.SetNormals(normals);
			for (int i = 0; i < triangles.Count; i++) mesh.SetTriangles(triangles[i], i);
			mesh.RecalculateTangents();

			filter.sharedMesh = mesh;
			renderer.sharedMaterials = materials.ToArray();
		}
	#endif



	// Tile Methods

	// Variants
	// {TileName}.png
	// {TileName}_{Frame}_{Milliseconds}.png

	// Bitfield Layout
	// Tile:  10 bits
	// Frame: 12 bits

	static Mesh GetTileMesh() {
		var mesh = new Mesh() {
			name = "Tile Mesh",
			vertices = new Vector3[] {
				new(-0.5f, -0.5f,  0.0f), new( 0.5f, -0.5f,  0.0f),
				new(-0.5f,  0.5f,  0.0f), new( 0.5f,  0.5f,  0.0f),
			},
			uv = new Vector2[] {
				new(0f, 0f), new(1f, 0f),
				new(0f, 1f), new(1f, 1f),
			},
			normals = new Vector3[] {
				new(0f, 0f, -1f), new(0f, 0f, -1f),
				new(0f, 0f, -1f), new(0f, 0f, -1f),
			},
			triangles = new int[] {
				0, 3, 1,
				3, 0, 2,
			},
		};
		mesh.RecalculateTangents();
		return mesh;
	}

	static void ClearTileData() {
		TileLUTable?.Clear();
		TileDataMap?.Clear();
	}

	static void LoadTileData(AtlasMapSO atlasMap) {
		TileLUTable ??= new();
		TileDataMap ??= new();
		ClearTileData();
		if (atlasMap) foreach (var pair in atlasMap) {
			var split = pair.Key.Split('_');
			var match = true;

			var tile         = Tile.None;
			var frame        = 0u;
			var milliseconds = 100u;
			if (1 <= split.Length) match &= Enum.TryParse(split[ 0], out tile);
			if (3 <= split.Length) match &= uint.TryParse(split[^2], out frame);
			if (3 <= split.Length) match &= uint.TryParse(split[^1], out milliseconds);
			if (!match) continue;

			var tileBit  = ((uint)tile  + 1u) << 22;
			var frameBit = ((uint)frame + 1u) <<  0;

			var totalMillisecondsKey = tileBit;
			TileLUTable.TryAdd(totalMillisecondsKey, 1u);
			var totalMilliseconds = TileLUTable[totalMillisecondsKey] + milliseconds;
			TileLUTable[totalMillisecondsKey] = totalMilliseconds;

			var dataKey = tileBit | frameBit;
			TileDataMap.TryAdd(dataKey, pair.Value);

			var sampleA = SampleRate * (totalMilliseconds - milliseconds) * 0.001f;
			var sampleB = SampleRate * (totalMilliseconds -           0u) * 0.001f;
			var sampleABit = ((uint)sampleA + 1u) <<  0;
			var sampleBBit = ((uint)sampleB + 1u) <<  0;
			var sampleAKey = tileBit | sampleABit;
			var sampleBKey = tileBit | sampleBBit;
			for (var i = sampleAKey; i <= sampleBKey; i++) TileLUTable.TryAdd(i, dataKey);
		}
		TileLUTable.TrimExcess();
		TileDataMap.TrimExcess();
	}

	static TileDrawData GetTileData(Transform transform, TileDrawerAuthoring drawer) {
		var position = new float3(transform.position + drawer.Position);
		var rotation = math.mul(transform.rotation, drawer.Rotation);
		var scale    = new float3(1f, 1f, 1f);
		var pivot    = new float3(0f, 0f, 0f);
		var tiling   = new float2(1f, 1f);
		var offset   = new float2(0f, 0f);
		var flip     = drawer.Flip;

		var tileBit   = ((uint)drawer.Tile + 1u) << 22;
		var sampleBit = ((uint)0u          + 1u) <<  0;

		var totalMillisecondsKey = tileBit;
		if (TileLUTable.TryGetValue(totalMillisecondsKey, out var totalMilliseconds)) {
			var sample = SampleRate * ((uint)(drawer.Offset * 1000f) % totalMilliseconds) * 0.001f;
			sampleBit = ((uint)sample + 1u) <<  0;
		}
		var sampleKey = tileBit | sampleBit;
		if (TileLUTable.TryGetValue(sampleKey, out var dataKey)) {
			if (TileDataMap.TryGetValue(dataKey, out var atlas)) {
				scale  = new float3(atlas.scale.x, atlas.scale.y, 1f);
				pivot  = new float3(atlas.pivot.x, atlas.pivot.y, 0f);
				tiling = atlas.tiling;
				offset = atlas.offset;
			}
		}
		var data = new TileDrawData() {
			position  = position + math.mul(drawer.Rotation, pivot),
			rotation  = rotation.value,
			scale     = scale,

			tiling    = tiling,
			offset    = offset,
			basecolor = drawer.BaseColor,
			maskcolor = drawer.MaskColor,
			emission  = drawer.Emission,
		};
		if (flip.x) data.offset.x -= data.tiling.x *= -1f;
		if (flip.y) data.offset.y -= data.tiling.y *= -1f;
		return data;
	}



	// Sprite Methods

	// Variants
	// {SpriteName}.png
	// {SpriteName}_{Frame}_{Milliseconds}.png
	// {SpriteName}_{Direction}_{Frame}_{Milliseconds}.png
	// {SpriteName}_{MotionName}_{Direction}_{Frame}_{Milliseconds}.png

	// Direction Count Mapping
	// 1 ~  2:  2-way (right, left)
	// 3 ~  4:  4-way (down, right, up, left)
	// 5 ~  8:  8-way (down, right-down, right, right-up, up, left-up, left, left-down)
	// 9 ~ 16: 16-way (down, ...)

	// Bitfield Layout
	// Sprite:    10 bits
	// Motion:     5 bits
	// Direction:  5 bits
	// Frame:     12 bits

	static Mesh GetSpriteMesh() {
		var mesh = new Mesh() {
			name = "Sprite Mesh",
			vertices = new Vector3[] {
				new(-0.5f, -0.5f,  0.0f), new( 0.5f, -0.5f,  0.0f),
				new(-0.5f,  0.5f,  0.0f), new( 0.5f,  0.5f,  0.0f),
			},
			uv = new Vector2[] {
				new(0f, 0f), new(1f, 0f),
				new(0f, 1f), new(1f, 1f),
			},
			normals = new Vector3[] {
				new(0f, 3f, -1f), new(0f, 3f, -1f),
				new(0f, 3f, -1f), new(0f, 3f, -1f),
			},
			triangles = new int[] {
				0, 3, 1,
				3, 0, 2,
			},
		};
		mesh.RecalculateTangents();
		return mesh;
	}

	static void ClearSpriteData() {
		SpriteLUTable?.Clear();
		SpriteDataMap?.Clear();
	}

	static void LoadSpriteData(AtlasMapSO atlasMap) {
		SpriteLUTable ??= new();
		SpriteDataMap ??= new();
		ClearSpriteData();
		if (atlasMap) foreach (var pair in atlasMap) {
			var split = pair.Key.Split('_');
			var match = true;

			var sprite       = Sprite.None;
			var motion       = Motion.None;
			var direction    = 0u;
			var frame        = 0u;
			var milliseconds = 100u;
			if (1 <= split.Length) match &= Enum.TryParse(split[ 0], out sprite);
			if (5 <= split.Length) match &= Enum.TryParse(split[ 1], out motion);
			if (4 <= split.Length) match &= uint.TryParse(split[^3], out direction);
			if (3 <= split.Length) match &= uint.TryParse(split[^2], out frame);
			if (3 <= split.Length) match &= uint.TryParse(split[^1], out milliseconds);
			if (!match) continue;

			var spriteBit    = ((uint)sprite    + 1u) << 22;
			var motionBit    = ((uint)motion    + 1u) << 17;
			var directionBit = ((uint)direction + 1u) << 12;
			var frameBit     = ((uint)frame     + 1u) <<  0;

			var numDirectionsKey = spriteBit | motionBit;
			SpriteLUTable.TryAdd(numDirectionsKey, 0u);
			var numDirections = math.max(SpriteLUTable[numDirectionsKey], direction + 1u);
			SpriteLUTable[numDirectionsKey] = numDirections;

			var totalMillisecondsKey = spriteBit | motionBit | directionBit;
			SpriteLUTable.TryAdd(totalMillisecondsKey, 1u);
			var totalMilliseconds = SpriteLUTable[totalMillisecondsKey] + milliseconds;
			SpriteLUTable[totalMillisecondsKey] = totalMilliseconds;

			var dataKey = spriteBit | motionBit | directionBit | frameBit;
			SpriteDataMap.TryAdd(dataKey, pair.Value);

			var sampleA = SampleRate * (totalMilliseconds - milliseconds) * 0.001f;
			var sampleB = SampleRate * (totalMilliseconds -           0u) * 0.001f;
			var sampleABit = ((uint)sampleA + 1u) <<  0;
			var sampleBBit = ((uint)sampleB + 1u) <<  0;
			var sampleAKey = spriteBit | motionBit | directionBit | sampleABit;
			var sampleBKey = spriteBit | motionBit | directionBit | sampleBBit;
			for (uint i = sampleAKey; i <= sampleBKey; i++) SpriteLUTable.TryAdd(i, dataKey);
		}
		SpriteLUTable.TrimExcess();
		SpriteDataMap.TrimExcess();
	}

	static SpriteDrawData GetSpriteData(Transform transform, SpriteDrawerAuthoring drawer) {
		var position = new float3(transform.position + drawer.Position);
		var scale    = new float2(1f, 1f);
		var pivot    = new float2(drawer.Pivot.x, drawer.Pivot.y);
		var tiling   = new float2(1f, 1f);
		var offset   = new float2(0f, 0f);
		var flip     = drawer.Flip;

		var spriteBit    = ((uint)drawer.Sprite + 1u) << 22;
		var motionBit    = ((uint)drawer.Motion + 1u) << 17;
		var directionBit = ((uint)0u            + 1u) << 12;
		var sampleBit    = ((uint)0u            + 1u) <<  0;

		var numDirectionsKey = spriteBit | motionBit;
		if (SpriteLUTable.TryGetValue(numDirectionsKey, out var numDirections)) {
			var maxDirections = 1u;
			var yaw = drawer.Yaw;
			switch (numDirections) {
				case >=  1u and <=  2u: maxDirections =  2u; yaw += 00.00f; break;
				case >=  3u and <=  4u: maxDirections =  4u; yaw += 45.00f; break;
				case >=  5u and <=  8u: maxDirections =  8u; yaw += 22.50f; break;
				case >=  9u and <= 16u: maxDirections = 16u; yaw += 11.25f; break;
			}
			var rotation = ((quaternion)transform.rotation).value;
			var y = 0.0f + 2.0f * (rotation.y * rotation.w + rotation.x * rotation.z);
			var x = 1.0f - 2.0f * (rotation.y * rotation.y + rotation.z * rotation.z);
			yaw -= math.atan2(y, x) * math.TODEGREES;
			yaw -= math.floor(yaw * 0.00277778f) * 360f;
			var normalize = (uint)(yaw * maxDirections * 0.00277778f);
			var direction = math.clamp(normalize, 0u, maxDirections - 1u);
			if (numDirections <= direction) {
				flip.x = !flip.x;
				switch (maxDirections) {
					case  2u: direction =  1u - direction; break;
					case  4u: direction =  4u - direction; break;
					case  8u: direction =  8u - direction; break;
					case 16u: direction = 16u - direction; break;
				}
			}
			directionBit = ((uint)direction + 1u) << 12;
		}
		var totalMillisecondsKey = spriteBit | motionBit | directionBit;
		if (SpriteLUTable.TryGetValue(totalMillisecondsKey, out var totalMilliseconds)) {
			var sample = SampleRate * ((uint)(drawer.Offset * 1000f) % totalMilliseconds) * 0.001f;
			sampleBit = ((uint)sample + 1u) <<  0;
		}
		var sampleKey = spriteBit | motionBit | directionBit | sampleBit;
		if (SpriteLUTable.TryGetValue(sampleKey, out var dataKey)) {
			if (SpriteDataMap.TryGetValue(dataKey, out var atlas)) {
				scale  = atlas.scale;
				pivot += atlas.pivot * new float2(flip.x ? -1f : 1f, 1f);
				tiling = atlas.tiling;
				offset = atlas.offset;
			}
		}
		var data = new SpriteDrawData() {
			position  = position,
			scale     = scale,
			pivot     = pivot,

			tiling    = tiling,
			offset    = offset,
			basecolor = drawer.BaseColor,
			maskcolor = drawer.MaskColor,
			emission  = drawer.Emission,
		};
		if (flip.x) data.offset.x -= data.tiling.x *= -1f;
		if (flip.y) data.offset.y -= data.tiling.y *= -1f;
		return data;
	}



	// Shadow Methods

	// Variants
	// {SpriteName}.png
	// {SpriteName}_{Frame}_{Milliseconds}.png
	// {SpriteName}_{MotionName}_{Frame}_{Milliseconds}.png

	// Bitfield Layout
	// Sprite: 10 bits
	// Motion:  5 bits
	// Frame:  12 bits

	static Mesh GetShadowMesh() {
		var mesh = new Mesh() {
			name = "Shadow Mesh",
			vertices = new Vector3[] {
				new(-0.5f,  0.0f, -0.5f), new( 0.5f,  0.0f, -0.5f),
				new(-0.5f,  0.0f,  0.5f), new( 0.5f,  0.0f,  0.5f),
			},
			uv = new Vector2[] {
				new(0f, 0f), new(1f, 0f),
				new(0f, 1f), new(1f, 1f),
			},
			normals = new Vector3[] {
				new(0f, 1f, 0f), new(0f, 1f, 0f),
				new(0f, 1f, 0f), new(0f, 1f, 0f),
			},
			triangles = new int[] {
				0, 3, 1,
				3, 0, 2,
			},
		};
		mesh.RecalculateTangents();
		return mesh;
	}

	static void ClearShadowData() {
		ShadowLUTable?.Clear();
		ShadowDataMap?.Clear();
	}

	static void LoadShadowData(AtlasMapSO atlasMap) {
		ShadowLUTable ??= new();
		ShadowDataMap ??= new();
		ClearShadowData();
		if (atlasMap) foreach (var pair in atlasMap) {
			var split = pair.Key.Split('_');
			var match = true;

			var shadow       = Sprite.None;
			var motion       = Motion.None;
			var frame        = 0u;
			var milliseconds = 100u;
			if (1 <= split.Length) match &= Enum.TryParse(split[ 0], out shadow);
			if (4 <= split.Length) match &= Enum.TryParse(split[ 1], out motion);
			if (3 <= split.Length) match &= uint.TryParse(split[^2], out frame);
			if (3 <= split.Length) match &= uint.TryParse(split[^1], out milliseconds);
			if (!match) continue;

			var shadowBit = ((uint)shadow + 1u) << 22;
			var motionBit = ((uint)motion + 1u) << 17;
			var frameBit  = ((uint)frame  + 1u) <<  0;

			var numDirectionsKey = shadowBit | motionBit;
			var flag = SpriteLUTable.TryGetValue(numDirectionsKey, out uint numDirections);
			if (flag) ShadowLUTable.TryAdd(numDirectionsKey, numDirections);

			var totalMillisecondsKey = shadowBit | motionBit;
			ShadowLUTable.TryAdd(totalMillisecondsKey, 1u);
			var totalMilliseconds = ShadowLUTable[totalMillisecondsKey] + milliseconds;
			ShadowLUTable[totalMillisecondsKey] = totalMilliseconds;

			var dataKey = shadowBit | motionBit | frameBit;
			ShadowDataMap.TryAdd(dataKey, pair.Value);

			var sampleA = SampleRate * (totalMilliseconds - milliseconds) * 0.001f;
			var sampleB = SampleRate * (totalMilliseconds -           0u) * 0.001f;
			var sampleABit = ((uint)sampleA + 1u) <<  0;
			var sampleBBit = ((uint)sampleB + 1u) <<  0;
			var sampleAKey = shadowBit | motionBit | sampleABit;
			var sampleBKey = shadowBit | motionBit | sampleBBit;
			for (var i = sampleAKey; i <= sampleBKey; i++) ShadowLUTable.TryAdd(i, dataKey);
		}
		ShadowLUTable.TrimExcess();
		ShadowDataMap.TrimExcess();
	}

	static ShadowDrawData GetShadowData(Transform transform, ShadowDrawerAuthoring drawer) {
		var position = new float3(transform.position + drawer.Position);
		var scale    = new float2(1f, 1f);
		var pivot    = new float2(0f, 0f);
		var tiling   = new float2(1f, 1f);
		var offset   = new float2(0f, 0f);
		var flip     = drawer.Flip;

		var shadowBit = ((uint)drawer.Shadow + 1u) << 22;
		var motionBit = ((uint)drawer.Motion + 1u) << 17;
		var sampleBit = ((uint)0u            + 1u) <<  0;

		var numDirectionsKey = shadowBit | motionBit;
		if (ShadowLUTable.TryGetValue(numDirectionsKey, out var numDirections)) {
			var maxDirections = 1u;
			var yaw = drawer.Yaw;
			switch (numDirections) {
				case >=  1u and <=  2u: maxDirections =  2u; yaw += 00.00f; break;
				case >=  3u and <=  4u: maxDirections =  4u; yaw += 45.00f; break;
				case >=  5u and <=  8u: maxDirections =  8u; yaw += 22.50f; break;
				case >=  9u and <= 16u: maxDirections = 16u; yaw += 11.25f; break;
			}
			var rotation = ((quaternion)transform.rotation).value;
			var y = 0.0f + 2.0f * (rotation.y * rotation.w + rotation.x * rotation.z);
			var x = 1.0f - 2.0f * (rotation.y * rotation.y + rotation.z * rotation.z);
			yaw -= math.atan2(y, x) * math.TODEGREES;
			yaw -= math.floor(yaw * 0.00277778f) * 360f;
			var normalize = (uint)(yaw * maxDirections * 0.00277778f);
			var direction = math.clamp(normalize, 0u, maxDirections - 1u);
			if (numDirections <= direction) {
				flip.x = !flip.x;
			}
		}
		var totalMillisecondsKey = shadowBit | motionBit;
		if (ShadowLUTable.TryGetValue(totalMillisecondsKey, out var totalMilliseconds)) {
			var sample = SampleRate * ((uint)(drawer.Offset * 1000f) % totalMilliseconds) * 0.001f;
			sampleBit = ((uint)sample + 1u) <<  0;
		}
		var sampleKey = shadowBit | motionBit | sampleBit;
		if (ShadowLUTable.TryGetValue(sampleKey, out var dataKey)) {
			if (ShadowDataMap.TryGetValue(dataKey, out var atlas)) {
				scale  = atlas.scale;
				pivot += atlas.pivot * new float2(flip.x ? -1f : 1f, 1f);
				tiling = atlas.tiling;
				offset = atlas.offset;
			}
		}
		var data = new ShadowDrawData() {
			position = position,
			scale    = scale,
			pivot    = pivot,

			tiling   = tiling,
			offset   = offset,
		};
		if (flip.x) data.offset.x -= data.tiling.x *= -1f;
		if (flip.y) data.offset.y -= data.tiling.y *= -1f;
		return data;
	}



	// UI Methods

	// Variants
	// {UIName}.png
	// {UIName}_{Frame}_{Milliseconds}.png

	// Bitfield Layout
	// UI:    10 bits
	// Frame: 12 bits

	static Mesh GetUIMesh() {
		var mesh = new Mesh() {
			name = "UI Mesh",
			vertices = new Vector3[] {
				new(-0.5f, -0.5f,  0.0f), new( 0.5f, -0.5f,  0.0f),
				new(-0.5f,  0.5f,  0.0f), new( 0.5f,  0.5f,  0.0f),
			},
			uv = new Vector2[] {
				new(0f, 0f), new(1f, 0f),
				new(0f, 1f), new(1f, 1f),
			},
			normals = new Vector3[] {
				new(0f, 3f, -1f), new(0f, 3f, -1f),
				new(0f, 3f, -1f), new(0f, 3f, -1f),
			},
			triangles = new int[] {
				0, 3, 1,
				3, 0, 2,
			},
		};
		mesh.RecalculateTangents();
		return mesh;
	}

	static void ClearUIData() {
		UILUTable?.Clear();
		UIDataMap?.Clear();
	}

	static void LoadUIData(AtlasMapSO atlasMap) {
		UILUTable ??= new();
		UIDataMap ??= new();
		ClearUIData();
		if (atlasMap) foreach (var pair in atlasMap) {
			var split = pair.Key.Split('_');
			var match = true;

			var ui           = UI.None;
			var frame        = 0u;
			var milliseconds = 100u;
			if (1 <= split.Length) match &= Enum.TryParse(split[ 0], out ui);
			if (3 <= split.Length) match &= uint.TryParse(split[^2], out frame);
			if (3 <= split.Length) match &= uint.TryParse(split[^1], out milliseconds);
			if (!match) continue;

			var uiBit    = ((uint)ui    + 1u) << 22;
			var frameBit = ((uint)frame + 1u) <<  0;

			var totalMillisecondsKey = uiBit;
			UILUTable.TryAdd(totalMillisecondsKey, 1u);
			var totalMilliseconds = UILUTable[totalMillisecondsKey] + milliseconds;
			UILUTable[totalMillisecondsKey] = totalMilliseconds;

			var dataKey = uiBit | frameBit;
			UIDataMap.TryAdd(dataKey, pair.Value);

			var sampleA = SampleRate * (totalMilliseconds - milliseconds) * 0.001f;
			var sampleB = SampleRate * (totalMilliseconds -           0u) * 0.001f;
			var sampleABit = ((uint)sampleA + 1u) <<  0;
			var sampleBBit = ((uint)sampleB + 1u) <<  0;
			var sampleAKey = uiBit | sampleABit;
			var sampleBKey = uiBit | sampleBBit;
			for (uint i = sampleAKey; i <= sampleBKey; i++) UILUTable.TryAdd(i, dataKey);
		}
		UILUTable.TrimExcess();
		UIDataMap.TrimExcess();
	}

	static UIDrawData GetUIData(Transform transform, UIDrawerAuthoring drawer) {
		var position = new float3(transform.position + drawer.Position);
		var scale    = new float2(drawer.Scale.x, drawer.Scale.y);
		var pivot    = new float2(drawer.Pivot.x, drawer.Pivot.y);
		var tiling   = new float2(1f, 1f);
		var offset   = new float2(0f, 0f);
		var flip     = drawer.Flip;

		var uiBit     = ((uint)drawer.UI + 1u) << 22;
		var sampleBit = ((uint)0u        + 1u) <<  0;

		var totalMillisecondsKey = uiBit;
		if (UILUTable.TryGetValue(totalMillisecondsKey, out var totalMilliseconds)) {
			var sample = SampleRate * ((uint)(drawer.Offset * 1000f) % totalMilliseconds) * 0.001f;
			sampleBit = ((uint)sample + 1u) <<  0;
		}
		var sampleKey = uiBit | sampleBit;
		if (UILUTable.TryGetValue(sampleKey, out var dataKey)) {
			if (UIDataMap.TryGetValue(dataKey, out var atlas)) {
				scale *= atlas.scale;
				pivot += atlas.pivot;
				tiling = atlas.tiling;
				offset = atlas.offset;
			}
		}
		var data = new UIDrawData() {
			position  = position,
			scale     = scale,
			pivot     = pivot,

			tiling    = tiling,
			offset    = offset,
			basecolor = drawer.BaseColor,
		};
		if (flip.x) data.offset.x -= data.tiling.x *= -1f;
		if (flip.y) data.offset.y -= data.tiling.y *= -1f;
		return data;
	}



	// Lifecycle

	#if UNITY_EDITOR
		void OnEnable () { if (AutoDraw) ClearEditorMeshAll(); }
		void OnDisable() { if (AutoDraw) DrawEditorMeshAll (); }
	#endif
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Draw Manager System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(DOTSPresentationSystemGroup), OrderLast = true)]
public partial class DrawManagerSystem : SystemBase {
	BeginInitializationEntityCommandBufferSystem System;

	NativeHashMap<uint, uint> TileLUTable;
	NativeHashMap<uint, uint> SpriteLUTable;
	NativeHashMap<uint, uint> ShadowLUTable;
	NativeHashMap<uint, uint> UILUTable;

	NativeHashMap<uint, AtlasData> TileDataMap;
	NativeHashMap<uint, AtlasData> SpriteDataMap;
	NativeHashMap<uint, AtlasData> ShadowDataMap;
	NativeHashMap<uint, AtlasData> UIDataMap;

	IndirectRenderer<TileDrawData  > TileRenderer;
	IndirectRenderer<SpriteDrawData> SpriteRenderer;
	IndirectRenderer<ShadowDrawData> ShadowRenderer;
	IndirectRenderer<UIDrawData    > UIRenderer;

	NativeArray<EntityQuery> TileQuery;
	NativeArray<EntityQuery> SpriteQuery;
	NativeArray<EntityQuery> ShadowQuery;
	NativeArray<EntityQuery> UIQuery;

	protected override void OnCreate() {
		System = World.GetOrCreateSystemManaged<BeginInitializationEntityCommandBufferSystem>();

		TileLUTable   = new(DrawManager.TileLUTable  .Count, Allocator.Persistent);
		SpriteLUTable = new(DrawManager.SpriteLUTable.Count, Allocator.Persistent);
		ShadowLUTable = new(DrawManager.ShadowLUTable.Count, Allocator.Persistent);
		UILUTable     = new(DrawManager.UILUTable    .Count, Allocator.Persistent);
		foreach (var pair in DrawManager.TileLUTable  ) TileLUTable  .Add(pair.Key, pair.Value);
		foreach (var pair in DrawManager.SpriteLUTable) SpriteLUTable.Add(pair.Key, pair.Value);
		foreach (var pair in DrawManager.ShadowLUTable) ShadowLUTable.Add(pair.Key, pair.Value);
		foreach (var pair in DrawManager.UILUTable    ) UILUTable    .Add(pair.Key, pair.Value);

		TileDataMap   = new(DrawManager.TileDataMap  .Count, Allocator.Persistent);
		SpriteDataMap = new(DrawManager.SpriteDataMap.Count, Allocator.Persistent);
		ShadowDataMap = new(DrawManager.ShadowDataMap.Count, Allocator.Persistent);
		UIDataMap     = new(DrawManager.UIDataMap    .Count, Allocator.Persistent);
		foreach (var pair in DrawManager.TileDataMap  ) TileDataMap  .Add(pair.Key, pair.Value);
		foreach (var pair in DrawManager.SpriteDataMap) SpriteDataMap.Add(pair.Key, pair.Value);
		foreach (var pair in DrawManager.ShadowDataMap) ShadowDataMap.Add(pair.Key, pair.Value);
		foreach (var pair in DrawManager.UIDataMap    ) UIDataMap    .Add(pair.Key, pair.Value);

		TileRenderer   = new(DrawManager.TileMaterial,   DrawManager.TileMesh);
		SpriteRenderer = new(DrawManager.SpriteMaterial, DrawManager.SpriteMesh);
		ShadowRenderer = new(DrawManager.ShadowMaterial, DrawManager.ShadowMesh);
		UIRenderer     = new(DrawManager.UIMaterial,     DrawManager.UIMesh);
		TileRenderer  .param.shadowCastingMode = ShadowCastingMode.On;
		SpriteRenderer.param.shadowCastingMode = ShadowCastingMode.Off;
		ShadowRenderer.param.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
		UIRenderer    .param.shadowCastingMode = ShadowCastingMode.Off;
		TileRenderer  .param.receiveShadows = true;
		SpriteRenderer.param.receiveShadows = false;
		ShadowRenderer.param.receiveShadows = false;
		UIRenderer    .param.receiveShadows = false;

		TileQuery   = new NativeArray<EntityQuery>(2, Allocator.Persistent);
		SpriteQuery = new NativeArray<EntityQuery>(2, Allocator.Persistent);
		ShadowQuery = new NativeArray<EntityQuery>(2, Allocator.Persistent);
		UIQuery     = new NativeArray<EntityQuery>(2, Allocator.Persistent);
		var localToWorld = ComponentType.ReadOnly<LocalToWorld>();
		var tileDrawer   = ComponentType.ReadOnly<TileDrawer  >();
		var spriteDrawer = ComponentType.ReadOnly<SpriteDrawer>();
		var shadowDrawer = ComponentType.ReadOnly<ShadowDrawer>();
		var uiDrawer     = ComponentType.ReadOnly<UIDrawer    >();
		TileQuery  [0] = GetEntityQuery(localToWorld, tileDrawer,   ComponentType.ReadOnly<Static>());
		SpriteQuery[0] = GetEntityQuery(localToWorld, spriteDrawer, ComponentType.ReadOnly<Static>());
		ShadowQuery[0] = GetEntityQuery(localToWorld, shadowDrawer, ComponentType.ReadOnly<Static>());
		UIQuery    [0] = GetEntityQuery(localToWorld, uiDrawer,     ComponentType.ReadOnly<Static>());
		TileQuery  [1] = GetEntityQuery(localToWorld, tileDrawer,   ComponentType.Exclude <Static>());
		SpriteQuery[1] = GetEntityQuery(localToWorld, spriteDrawer, ComponentType.Exclude <Static>());
		ShadowQuery[1] = GetEntityQuery(localToWorld, shadowDrawer, ComponentType.Exclude <Static>());
		UIQuery    [1] = GetEntityQuery(localToWorld, uiDrawer,     ComponentType.Exclude <Static>());
	}

	protected override void OnDestroy() {
		if (TileLUTable  .IsCreated) TileLUTable  .Dispose();
		if (SpriteLUTable.IsCreated) SpriteLUTable.Dispose();
		if (ShadowLUTable.IsCreated) ShadowLUTable.Dispose();
		if (UILUTable    .IsCreated) UILUTable    .Dispose();
		if (TileDataMap  .IsCreated) TileDataMap  .Dispose();
		if (SpriteDataMap.IsCreated) SpriteDataMap.Dispose();
		if (ShadowDataMap.IsCreated) ShadowDataMap.Dispose();
		if (UIDataMap    .IsCreated) UIDataMap    .Dispose();
		TileRenderer  ?.Dispose();
		SpriteRenderer?.Dispose();
		ShadowRenderer?.Dispose();
		UIRenderer    ?.Dispose();
		if (TileQuery  .IsCreated) TileQuery  .Dispose();
		if (SpriteQuery.IsCreated) SpriteQuery.Dispose();
		if (ShadowQuery.IsCreated) ShadowQuery.Dispose();
		if (UIQuery    .IsCreated) UIQuery    .Dispose();
	}

	protected override void OnUpdate() {
		var buffer = System.CreateCommandBuffer();
		var random = new Random(1u + (uint)(4801 * SystemAPI.Time.ElapsedTime) % 1000);
		var transformLookup = GetComponentLookup<LocalToWorld>(true);

		var tileDrawerLookup = GetBufferLookup<TileDrawer>(true);
		for (int i = 0; i < TileQuery.Length; i++) if (0 < TileQuery[i].CalculateEntityCount()) {
			var entityArray = TileQuery[i].ToEntityArray(Allocator.TempJob);
			var lengthArray = new NativeArray<int>(entityArray.Length + 1, Allocator.TempJob);
			lengthArray[0] = 0;
			Dependency = new GetLengthJob<TileDrawer> {
				LengthArray  = lengthArray,
				EntityArray  = entityArray,
				DrawerLookup = tileDrawerLookup,
			}.Schedule(entityArray.Length, 64, Dependency);
			Dependency = new PrefixSumJob {
				LengthArray = lengthArray,
			}.Schedule(Dependency);
			Dependency.Complete();
			var count = lengthArray[^1];
			if (0 < count) {
				Dependency = new DrawTileJob {
					DrawDataArray   = TileRenderer.AllocateAndLockBuffer(i, count),
					EntityArray     = entityArray,
					LengthArray     = lengthArray,
					TransformLookup = transformLookup,
					DrawerLookup    = tileDrawerLookup,
					LUTable         = TileLUTable,
					DataMap         = TileDataMap,
					Random          = random,
				}.Schedule(entityArray.Length, 64, Dependency);
				Dependency.Complete();
				TileRenderer.UnlockBuffer(count);
			}
			if (i < TileQuery.Length - 1) foreach (var entity in entityArray) {
				buffer.RemoveComponent<TileDrawer>(entity);
			}
			entityArray.Dispose();
			lengthArray.Dispose();
		}
		TileRenderer.Draw();
		TileRenderer.Clear(TileQuery.Length - 1);

		var spriteDrawerLookup = GetBufferLookup<SpriteDrawer>(true);
		for (int i = 0; i < SpriteQuery.Length; i++) if (0 < SpriteQuery[i].CalculateEntityCount()) {
			var entityArray = SpriteQuery[i].ToEntityArray(Allocator.TempJob);
			var lengthArray = new NativeArray<int>(entityArray.Length + 1, Allocator.TempJob);
			lengthArray[0] = 0;
			Dependency = new GetLengthJob<SpriteDrawer> {
				LengthArray  = lengthArray,
				EntityArray  = entityArray,
				DrawerLookup = spriteDrawerLookup,
			}.Schedule(entityArray.Length, 64, Dependency);
			Dependency = new PrefixSumJob {
				LengthArray = lengthArray,
			}.Schedule(Dependency);
			Dependency.Complete();
			var count = lengthArray[^1];
			if (0 < count) {
				Dependency = new DrawSpriteJob {
					DrawDataArray   = SpriteRenderer.AllocateAndLockBuffer(i, count),
					EntityArray     = entityArray,
					LengthArray     = lengthArray,
					TransformLookup = transformLookup,
					DrawerLookup    = spriteDrawerLookup,
					LUTable         = SpriteLUTable,
					DataMap         = SpriteDataMap,
					YawGlobal       = CameraManager.EulerRotation.y,
				}.Schedule(entityArray.Length, 64, Dependency);
				Dependency.Complete();
				SpriteRenderer.UnlockBuffer(count);
			}
			if (i < SpriteQuery.Length - 1) foreach (var entity in entityArray) {
				buffer.RemoveComponent<SpriteDrawer>(entity);
			}
			entityArray.Dispose();
			lengthArray.Dispose();
		}
		SpriteRenderer.Draw();
		SpriteRenderer.Clear(SpriteQuery.Length - 1);

		var shadowDrawerLookup = GetBufferLookup<ShadowDrawer>(true);
		for (int i = 0; i < ShadowQuery.Length; i++) if (0 < ShadowQuery[i].CalculateEntityCount()) {
			var entityArray = ShadowQuery[i].ToEntityArray(Allocator.TempJob);
			var lengthArray = new NativeArray<int>(entityArray.Length + 1, Allocator.TempJob);
			lengthArray[0] = 0;
			Dependency = new GetLengthJob<ShadowDrawer> {
				LengthArray  = lengthArray,
				EntityArray  = entityArray,
				DrawerLookup = shadowDrawerLookup,
			}.Schedule(entityArray.Length, 64, Dependency);
			Dependency = new PrefixSumJob {
				LengthArray = lengthArray,
			}.Schedule(Dependency);
			Dependency.Complete();
			var count = lengthArray[^1];
			if (0 < count) {
				Dependency = new DrawShadowJob {
					DrawDataArray   = ShadowRenderer.AllocateAndLockBuffer(i, count),
					EntityArray     = entityArray,
					LengthArray     = lengthArray,
					TransformLookup = transformLookup,
					DrawerLookup    = shadowDrawerLookup,
					LUTable         = ShadowLUTable,
					DataMap         = ShadowDataMap,
					YawGlobal       = CameraManager.EulerRotation.y,
				}.Schedule(entityArray.Length, 64, Dependency);
				Dependency.Complete();
				ShadowRenderer.UnlockBuffer(count);
			}
			if (i < ShadowQuery.Length - 1) foreach (var entity in entityArray) {
				buffer.RemoveComponent<ShadowDrawer>(entity);
			}
			entityArray.Dispose();
			lengthArray.Dispose();
		}
		ShadowRenderer.Draw();
		ShadowRenderer.Clear(ShadowQuery.Length - 1);

		var uiDrawerLookup = GetBufferLookup<UIDrawer>(true);
		for (int i = 0; i < UIQuery.Length; i++) if (0 < UIQuery[i].CalculateEntityCount()) {
			var entityArray = UIQuery[i].ToEntityArray(Allocator.TempJob);
			var lengthArray = new NativeArray<int>(entityArray.Length + 1, Allocator.TempJob);
			lengthArray[0] = 0;
			Dependency = new GetLengthJob<UIDrawer> {
				LengthArray  = lengthArray,
				EntityArray  = entityArray,
				DrawerLookup = uiDrawerLookup,
			}.Schedule(entityArray.Length, 64, Dependency);
			Dependency = new PrefixSumJob {
				LengthArray = lengthArray,
			}.Schedule(Dependency);
			Dependency.Complete();
			var count = lengthArray[^1];
			if (0 < count) {
				Dependency = new DrawUIJob {
					DrawDataArray   = UIRenderer.AllocateAndLockBuffer(i, count),
					EntityArray     = entityArray,
					LengthArray     = lengthArray,
					TransformLookup = transformLookup,
					DrawerLookup    = uiDrawerLookup,
					LUTable         = UILUTable,
					DataMap         = UIDataMap,
					Random          = random,
				}.Schedule(entityArray.Length, 64, Dependency);
				Dependency.Complete();
				UIRenderer.UnlockBuffer(count);
			}
			if (i < UIQuery.Length - 1) foreach (var entity in entityArray) {
				buffer.RemoveComponent<UIDrawer>(entity);
			}
			entityArray.Dispose();
			lengthArray.Dispose();
		}
		UIRenderer.Draw();
		UIRenderer.Clear(UIQuery.Length - 1);
	}



	[BurstCompile]
	partial struct GetLengthJob<T> : IJobParallelFor where T : unmanaged, IBufferElementData {
		[NativeDisableParallelForRestriction] public NativeArray<int> LengthArray;
		[ReadOnly] public NativeArray<Entity> EntityArray;
		[ReadOnly] public BufferLookup<T> DrawerLookup;
		public void Execute(int index) {
			LengthArray[1 + index] = DrawerLookup[EntityArray[index]].Length;
		}
	}

	[BurstCompile]
	partial struct PrefixSumJob : IJob {
		public NativeArray<int> LengthArray;
		public void Execute() {
			for (int i = 1; i < LengthArray.Length; i++) LengthArray[i] += LengthArray[i - 1];
		}
	}



	[BurstCompile]
	partial struct DrawTileJob : IJobParallelFor {
		const float SampleRate = DrawManager.SampleRate;
		[NativeDisableParallelForRestriction] public NativeArray<TileDrawData> DrawDataArray;
		[ReadOnly] public NativeArray<Entity> EntityArray;
		[ReadOnly] public NativeArray<int   > LengthArray;
		[ReadOnly] public ComponentLookup<LocalToWorld> TransformLookup;
		[ReadOnly] public BufferLookup   <TileDrawer  > DrawerLookup;
		[ReadOnly] public NativeHashMap<uint, uint     > LUTable;
		[ReadOnly] public NativeHashMap<uint, AtlasData> DataMap;
		[ReadOnly] public Random Random;

		public void Execute(int index) {
			var transform = TransformLookup[EntityArray[index]];
			var drawer    = DrawerLookup   [EntityArray[index]];
			for (int i = 0; i < drawer.Length; i++) {
				DrawDataArray[LengthArray[index] + i] = GetTileData(transform, drawer[i]);
			}
		}

		public TileDrawData GetTileData(LocalToWorld transform, TileDrawer drawer) {
			var position = transform.Position + math.mul(transform.Rotation, drawer.Position);
			var rotation = math.mul(transform.Rotation, drawer.Rotation);
			var scale    = new float3(1f, 1f, 1f);
			var pivot    = new float3(0f, 0f, 0f);
			var tiling   = new float2(1f, 1f);
			var offset   = new float2(0f, 0f);
			var flip	 = drawer.FlipRandomly ? Random.NextBool2() : drawer.Flip;

			var tileBit   = ((uint)drawer.Tile + 1u) << 22;
			var sampleBit = ((uint)0u          + 1u) <<  0;

			var totalMillisecondsKey = tileBit;
			if (LUTable.TryGetValue(totalMillisecondsKey, out var totalMilliseconds)) {
				var sample = SampleRate * ((uint)(drawer.Offset * 1000f) % totalMilliseconds) * 0.001f;
				sampleBit = ((uint)sample + 1u) <<  0;
			}
			var sampleKey = tileBit | sampleBit;
			if (LUTable.TryGetValue(sampleKey, out var dataKey)) {
				if (DataMap.TryGetValue(dataKey, out var atlas)) {
					scale  = new float3(atlas.scale.x, atlas.scale.y, 1f);
					pivot  = new float3(atlas.pivot.x, atlas.pivot.y, 0f);
					tiling = atlas.tiling;
					offset = atlas.offset;
				}
			}
			var data = new TileDrawData() {
				position  = position + math.mul(drawer.Rotation, pivot),
				rotation  = rotation.value,
				scale     = scale,

				tiling    = tiling,
				offset    = offset,
				basecolor = drawer.BaseColor,
				maskcolor = drawer.MaskColor,
				emission  = drawer.Emission,
			};
			if (flip.x) data.offset.x -= data.tiling.x *= -1f;
			if (flip.y) data.offset.y -= data.tiling.y *= -1f;
			return data;
		}
	}

	[BurstCompile]
	partial struct DrawSpriteJob : IJobParallelFor {
		const float SampleRate = DrawManager.SampleRate;
		[NativeDisableParallelForRestriction] public NativeArray<SpriteDrawData> DrawDataArray;
		[ReadOnly] public NativeArray<Entity> EntityArray;
		[ReadOnly] public NativeArray<int   > LengthArray;
		[ReadOnly] public ComponentLookup<LocalToWorld> TransformLookup;
		[ReadOnly] public BufferLookup   <SpriteDrawer> DrawerLookup;
		[ReadOnly] public NativeHashMap<uint, uint     > LUTable;
		[ReadOnly] public NativeHashMap<uint, AtlasData> DataMap;
		[ReadOnly] public float YawGlobal;

		public void Execute(int index) {
			var transform = TransformLookup[EntityArray[index]];
			var drawer    = DrawerLookup   [EntityArray[index]];
			for (int i = 0; i < drawer.Length; i++) {
				DrawDataArray[LengthArray[index] + i] = GetSpriteData(transform, drawer[i]);
			}
		}

		public SpriteDrawData GetSpriteData(LocalToWorld transform, SpriteDrawer drawer) {
			var position = transform.Position + math.mul(transform.Rotation, drawer.Position);
			var scale    = new float2(1f, 1f);
			var pivot    = new float2(drawer.Pivot.x, drawer.Pivot.y);
			var tiling   = new float2(1f, 1f);
			var offset   = new float2(0f, 0f);
			var flip     = drawer.Flip;

			var spriteBit    = ((uint)drawer.Sprite + 1u) << 22;
			var motionBit    = ((uint)drawer.Motion + 1u) << 17;
			var directionBit = ((uint)0u            + 1u) << 12;
			var sampleBit    = ((uint)0u            + 1u) <<  0;

			var numDirectionsKey = spriteBit | motionBit;
			if (LUTable.TryGetValue(numDirectionsKey, out var numDirections)) {
				var maxDirections = 1u;
				var yaw = drawer.Yaw + (drawer.YawLocal ? 0f : YawGlobal);
				switch (numDirections) {
					case >=  1u and <=  2u: maxDirections =  2u; yaw += 00.00f; break;
					case >=  3u and <=  4u: maxDirections =  4u; yaw += 45.00f; break;
					case >=  5u and <=  8u: maxDirections =  8u; yaw += 22.50f; break;
					case >=  9u and <= 16u: maxDirections = 16u; yaw += 11.25f; break;
				}
				var rotation = transform.Rotation.value;
				var y = 0.0f + 2.0f * (rotation.y * rotation.w + rotation.x * rotation.z);
				var x = 1.0f - 2.0f * (rotation.y * rotation.y + rotation.z * rotation.z);
				yaw -= math.atan2(y, x) * math.TODEGREES;
				yaw -= math.floor(yaw * 0.00277778f) * 360f;
				var normalize = (uint)(yaw * maxDirections * 0.00277778f);
				var direction = math.clamp(normalize, 0u, maxDirections - 1u);
				if (numDirections <= direction) {
					flip.x = !flip.x;
					switch (maxDirections) {
						case  2u: direction =  1u - direction; break;
						case  4u: direction =  4u - direction; break;
						case  8u: direction =  8u - direction; break;
						case 16u: direction = 16u - direction; break;
					}
				}
				directionBit = ((uint)direction + 1u) << 12;
			}
			var totalMillisecondsKey = spriteBit | motionBit | directionBit;
			if (LUTable.TryGetValue(totalMillisecondsKey, out var totalMilliseconds)) {
				var sample = SampleRate * ((uint)(drawer.Offset * 1000f) % totalMilliseconds) * 0.001f;
				sampleBit = ((uint)sample + 1u) <<  0;
			}
			var sampleKey = spriteBit | motionBit | directionBit | sampleBit;
			if (LUTable.TryGetValue(sampleKey, out var dataKey)) {
				if (DataMap.TryGetValue(dataKey, out var atlas)) {
					scale  = atlas.scale;
					pivot += atlas.pivot * new float2(flip.x ? -1f : 1f, 1f);
					tiling = atlas.tiling;
					offset = atlas.offset;
				}
			}
			var data = new SpriteDrawData() {
				position  = position,
				scale     = scale,
				pivot     = pivot,

				tiling    = tiling,
				offset    = offset,
				basecolor = drawer.BaseColor,
				maskcolor = drawer.MaskColor,
				emission  = drawer.Emission,
			};
			if (flip.x) data.offset.x -= data.tiling.x *= -1f;
			if (flip.y) data.offset.y -= data.tiling.y *= -1f;
			return data;
		}
	}

	[BurstCompile]
	partial struct DrawShadowJob : IJobParallelFor {
		const float SampleRate = DrawManager.SampleRate;
		[NativeDisableParallelForRestriction] public NativeArray<ShadowDrawData> DrawDataArray;
		[ReadOnly] public NativeArray<Entity> EntityArray;
		[ReadOnly] public NativeArray<int   > LengthArray;
		[ReadOnly] public ComponentLookup<LocalToWorld> TransformLookup;
		[ReadOnly] public BufferLookup   <ShadowDrawer> DrawerLookup;
		[ReadOnly] public NativeHashMap<uint, uint     > LUTable;
		[ReadOnly] public NativeHashMap<uint, AtlasData> DataMap;
		[ReadOnly] public float YawGlobal;

		public void Execute(int index) {
			var transform = TransformLookup[EntityArray[index]];
			var drawer    = DrawerLookup   [EntityArray[index]];
			for (int i = 0; i < drawer.Length; i++) {
				DrawDataArray[LengthArray[index] + i] = GetShadowData(transform, drawer[i]);
			}
		}

		public ShadowDrawData GetShadowData(LocalToWorld transform, ShadowDrawer drawer) {
			var position = transform.Position + math.mul(transform.Rotation, drawer.Position);
			var scale    = new float2(1f, 1f);
			var pivot    = new float2(0f, 0f);
			var tiling   = new float2(1f, 1f);
			var offset   = new float2(0f, 0f);
			var flip     = drawer.Flip;

			var shadowBit = ((uint)drawer.Shadow + 1u) << 22;
			var motionBit = ((uint)drawer.Motion + 1u) << 17;
			var sampleBit = ((uint)0u            + 1u) <<  0;

			var numDirectionsKey = shadowBit | motionBit;
			if (LUTable.TryGetValue(numDirectionsKey, out var numDirections)) {
				var maxDirections = 1u;
				var yaw = drawer.Yaw + (drawer.YawLocal ? 0f : YawGlobal);
				switch (numDirections) {
					case >=  1u and <=  2u: maxDirections =  2u; yaw += 00.00f; break;
					case >=  3u and <=  4u: maxDirections =  4u; yaw += 45.00f; break;
					case >=  5u and <=  8u: maxDirections =  8u; yaw += 22.50f; break;
					case >=  9u and <= 16u: maxDirections = 16u; yaw += 11.25f; break;
				}
				var rotation = transform.Rotation.value;
				var y = 0.0f + 2.0f * (rotation.y * rotation.w + rotation.x * rotation.z);
				var x = 1.0f - 2.0f * (rotation.y * rotation.y + rotation.z * rotation.z);
				yaw -= math.atan2(y, x) * math.TODEGREES;
				yaw -= math.floor(yaw * 0.00277778f) * 360f;
				var normalize = (uint)(yaw * maxDirections * 0.00277778f);
				var direction = math.clamp(normalize, 0u, maxDirections - 1u);
				if (numDirections <= direction) {
					flip.x = !flip.x;
				}
			}
			var totalMillisecondsKey = shadowBit | motionBit;
			if (LUTable.TryGetValue(totalMillisecondsKey, out var totalMilliseconds)) {
				var sample = SampleRate * ((uint)(drawer.Offset * 1000f) % totalMilliseconds) * 0.001f;
				sampleBit = ((uint)sample + 1u) <<  0;
			}
			var sampleKey = shadowBit | motionBit | sampleBit;
			if (LUTable.TryGetValue(sampleKey, out var dataKey)) {
				if (DataMap.TryGetValue(dataKey, out var atlas)) {
					scale  = atlas.scale;
					pivot += atlas.pivot * new float2(flip.x ? -1f : 1f, 1f);
					tiling = atlas.tiling;
					offset = atlas.offset;
				}
			}
			var data = new ShadowDrawData() {
				position = position,
				scale    = scale,
				pivot    = pivot,

				tiling   = tiling,
				offset   = offset,
			};
			if (flip.x) data.offset.x -= data.tiling.x *= -1f;
			if (flip.y) data.offset.y -= data.tiling.y *= -1f;
			return data;
		}
	}

	[BurstCompile]
	partial struct DrawUIJob : IJobParallelFor {
		const float SampleRate = DrawManager.SampleRate;
		[NativeDisableParallelForRestriction] public NativeArray<UIDrawData> DrawDataArray;
		[ReadOnly] public NativeArray<Entity> EntityArray;
		[ReadOnly] public NativeArray<int   > LengthArray;
		[ReadOnly] public ComponentLookup<LocalToWorld> TransformLookup;
		[ReadOnly] public BufferLookup   <UIDrawer    > DrawerLookup;
		[ReadOnly] public NativeHashMap<uint, uint     > LUTable;
		[ReadOnly] public NativeHashMap<uint, AtlasData> DataMap;
		[ReadOnly] public Random Random;

		public void Execute(int index) {
			var transform = TransformLookup[EntityArray[index]];
			var drawer    = DrawerLookup   [EntityArray[index]];
			for (int i = 0; i < drawer.Length; i++) {
				DrawDataArray[LengthArray[index] + i] = GetUIData(transform, drawer[i]);
			}
		}

		public UIDrawData GetUIData(LocalToWorld transform, UIDrawer drawer) {
			var position = transform.Position + math.mul(transform.Rotation, drawer.Position);
			var scale    = new float2(drawer.Scale.x, drawer.Scale.y);
			var pivot    = new float2(drawer.Pivot.x, drawer.Pivot.y);
			var tiling   = new float2(1f, 1f);
			var offset   = new float2(0f, 0f);
			var flip     = drawer.Flip;

			var uiBit     = ((uint)drawer.UI + 1u) << 22;
			var sampleBit = ((uint)0u        + 1u) <<  0;

			var totalMillisecondsKey = uiBit;
			if (LUTable.TryGetValue(totalMillisecondsKey, out var totalMilliseconds)) {
				var sample = SampleRate * ((uint)(drawer.Offset * 1000f) % totalMilliseconds) * 0.001f;
				sampleBit = ((uint)sample + 1u) <<  0;
			}
			var sampleKey = uiBit | sampleBit;
			if (LUTable.TryGetValue(sampleKey, out var dataKey)) {
				if (DataMap.TryGetValue(dataKey, out var atlas)) {
					scale *= atlas.scale;
					pivot += atlas.pivot;
					tiling = atlas.tiling;
					offset = atlas.offset;
				}
			}
			var data = new UIDrawData() {
				position  = position,
				scale     = scale,
				pivot     = pivot,

				tiling    = tiling,
				offset    = offset,
				basecolor = drawer.BaseColor,
			};
			if (flip.x) data.offset.x -= data.tiling.x *= -1f;
			if (flip.y) data.offset.y -= data.tiling.y *= -1f;
			return data;
		}
	}
}

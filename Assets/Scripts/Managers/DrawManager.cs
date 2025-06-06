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
	public float3     position;
	public quaternion rotation;
	public float3     scale;

	public float2     tiling;
	public float2     offset;
	public color      basecolor;
	public color      maskcolor;
	public color      emission;
}

public struct SpriteDrawData {
	public float3     position;
	public float2     scale;
	public float2     pivot;

	public float2     tiling;
	public float2     offset;
	public color      basecolor;
	public color      maskcolor;
	public color      emission;
}

public struct ShadowDrawData {
	public float3     position;
	public float2     scale;
	public float2     pivot;

	public float2     tiling;
	public float2     offset;
}

public struct UIDrawData {
	public float3     position;
	public float2     scale;
	public float2     pivot;

	public float2     tiling;
	public float2     offset;
	public color      basecolor;
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

				LabelField("Mesh", EditorStyles.boldLabel);
				TileMesh   = ObjectField("Tile Mesh",   TileMesh);
				SpriteMesh = ObjectField("Sprite Mesh", SpriteMesh);
				ShadowMesh = ObjectField("Shadow Mesh", ShadowMesh);
				UIMesh     = ObjectField("UI Mesh",     UIMesh);
				Space();

				LabelField("Material", EditorStyles.boldLabel);
				TileMaterial   = ObjectField("Tile Material",   TileMaterial);
				SpriteMaterial = ObjectField("Sprite Material", SpriteMaterial);
				ShadowMaterial = ObjectField("Shadow Material", ShadowMaterial);
				UIMaterial     = ObjectField("UI Material",     UIMaterial);
				Space();

				LabelField("Atlas Map", EditorStyles.boldLabel);
				TileAtlasMap   = ObjectField("Tile Atlas Map",   TileAtlasMap);
				SpriteAtlasMap = ObjectField("Sprite Atlas Map", SpriteAtlasMap);
				ShadowAtlasMap = ObjectField("Shadow Atlas Map", ShadowAtlasMap);
				UIAtlasMap     = ObjectField("UI Atlas Map",     UIAtlasMap);
				Space();

				LabelField("Map Data", EditorStyles.boldLabel);
				int datamap = Marshal.SizeOf<uint>() + Marshal.SizeOf<AtlasData>();
				int lutable = Marshal.SizeOf<uint>() + Marshal.SizeOf<uint     >();
				BeginHorizontal();
				PrefixLabel("Tile Map Data");
				int tile = TileDataMap.Count * datamap + TileLUTable.Count * lutable;
				if (GUILayout.Button("↻", GUILayout.Width(20))) LoadTileData(TileAtlasMap);
				if (TileAtlasMap) GUILayout.Label($"{TileDataMap.Count}  ({tile:N0} Bytes)");
				EndHorizontal();
				BeginHorizontal();
				PrefixLabel("Sprite Map Data");
				int sprite = SpriteDataMap.Count * datamap + SpriteLUTable.Count * lutable;
				if (GUILayout.Button("↻", GUILayout.Width(20))) LoadSpriteData(SpriteAtlasMap);
				if (SpriteAtlasMap) GUILayout.Label($"{SpriteDataMap.Count}  ({sprite:N0} Bytes)");
				EndHorizontal();
				BeginHorizontal();
				PrefixLabel("Shadow Map Data");
				int shadow = ShadowDataMap.Count * datamap + ShadowLUTable.Count * lutable;
				if (GUILayout.Button("↻", GUILayout.Width(20))) LoadShadowData(ShadowAtlasMap);
				if (ShadowAtlasMap) GUILayout.Label($"{ShadowDataMap.Count}  ({shadow:N0} Bytes)");
				EndHorizontal();
				BeginHorizontal();
				PrefixLabel("UI Map Data");
				int ui = UIDataMap.Count * datamap + UILUTable.Count * lutable;
				if (GUILayout.Button("↻", GUILayout.Width(20))) LoadUIData(UIAtlasMap);
				if (UIAtlasMap) GUILayout.Label($"{UIDataMap.Count}  ({ui:N0} Bytes)");
				EndHorizontal();
				AutoLoad = Toggle("Auto Load", AutoLoad);
				Space();

				LabelField("Editor Mesh", EditorStyles.boldLabel);
				CachePath = TextField("Cache Path", CachePath);
				BeginHorizontal();
				PrefixLabel("Draw Editor Mesh");
				if (Button("Clear")) ClearEditorMeshes();
				if (Button("Draw" )) DrawEditorMeshes ();
				EndHorizontal();
				AutoDraw = Toggle("Auto Draw", AutoDraw);
				Space();

				End();
			}
		}
	#endif



	// Constants

	public const float SampleRate = 60f;



	// Fields

	[SerializeField] Mesh m_TileMesh;
	[SerializeField] Mesh m_SpriteMesh;
	[SerializeField] Mesh m_ShadowMesh;
	[SerializeField] Mesh m_UIMesh;

	[SerializeField] Material m_TileMaterial;
	[SerializeField] Material m_SpriteMaterial;
	[SerializeField] Material m_ShadowMaterial;
	[SerializeField] Material m_UIMaterial;

	#if UNITY_EDITOR
		[SerializeField] AtlasMapSO m_TileAtlasMap;
		[SerializeField] AtlasMapSO m_SpriteAtlasMap;
		[SerializeField] AtlasMapSO m_ShadowAtlasMap;
		[SerializeField] AtlasMapSO m_UIAtlasMap;
	#endif

	[SerializeField] HashMap<uint, uint> m_TileLUTable;
	[SerializeField] HashMap<uint, uint> m_SpriteLUTable;
	[SerializeField] HashMap<uint, uint> m_ShadowLUTable;
	[SerializeField] HashMap<uint, uint> m_UILUTable;

	[SerializeField] HashMap<uint, AtlasData> m_TileDataMap;
	[SerializeField] HashMap<uint, AtlasData> m_SpriteDataMap;
	[SerializeField] HashMap<uint, AtlasData> m_ShadowDataMap;
	[SerializeField] HashMap<uint, AtlasData> m_UIDataMap;

	#if UNITY_EDITOR
		[SerializeField] bool m_AutoLoad = true;
		[SerializeField] bool m_AutoDraw = true;
		[SerializeField] string m_CachePath = "Assets/";
	#endif



	// Properties

	public static Mesh TileMesh {
		get => Instance.m_TileMesh ? Instance.m_TileMesh : Instance.m_TileMesh = GetTileMesh();
		set => Instance.m_TileMesh = value;
	}
	public static Mesh SpriteMesh {
		get => Instance.m_SpriteMesh ? Instance.m_SpriteMesh : Instance.m_SpriteMesh = GetSpriteMesh();
		set => Instance.m_SpriteMesh = value;
	}
	public static Mesh ShadowMesh {
		get => Instance.m_ShadowMesh ? Instance.m_ShadowMesh : Instance.m_ShadowMesh = GetShadowMesh();
		set => Instance.m_ShadowMesh = value;
	}
	public static Mesh UIMesh {
		get => Instance.m_UIMesh ? Instance.m_UIMesh : Instance.m_UIMesh = GetUIMesh();
		set => Instance.m_UIMesh = value;
	}

	public static Material TileMaterial {
		get => Instance.m_TileMaterial;
		set => Instance.m_TileMaterial = value;
	}
	public static Material SpriteMaterial {
		get => Instance.m_SpriteMaterial;
		set => Instance.m_SpriteMaterial = value;
	}
	public static Material ShadowMaterial {
		get => Instance.m_ShadowMaterial;
		set => Instance.m_ShadowMaterial = value;
	}
	public static Material UIMaterial {
		get => Instance.m_UIMaterial;
		set => Instance.m_UIMaterial = value;
	}

	#if UNITY_EDITOR
		public static AtlasMapSO TileAtlasMap {
			get => Instance.m_TileAtlasMap;
			set => Instance.m_TileAtlasMap = value;
		}
		public static AtlasMapSO SpriteAtlasMap {
			get => Instance.m_SpriteAtlasMap;
			set => Instance.m_SpriteAtlasMap = value;
		}
		public static AtlasMapSO ShadowAtlasMap {
			get => Instance.m_ShadowAtlasMap;
			set => Instance.m_ShadowAtlasMap = value;
		}
		public static AtlasMapSO UIAtlasMap {
			get => Instance.m_UIAtlasMap;
			set => Instance.m_UIAtlasMap = value;
		}
	#endif

	public static HashMap<uint, uint> TileLUTable {
		get => Instance.m_TileLUTable;
		set => Instance.m_TileLUTable = value;
	}
	public static HashMap<uint, uint> SpriteLUTable {
		get => Instance.m_SpriteLUTable;
		set => Instance.m_SpriteLUTable = value;
	}
	public static HashMap<uint, uint> ShadowLUTable {
		get => Instance.m_ShadowLUTable;
		set => Instance.m_ShadowLUTable = value;
	}
	public static HashMap<uint, uint> UILUTable {
		get => Instance.m_UILUTable;
		set => Instance.m_UILUTable = value;
	}

	public static HashMap<uint, AtlasData> TileDataMap {
		get => Instance.m_TileDataMap;
		set => Instance.m_TileDataMap = value;
	}
	public static HashMap<uint, AtlasData> SpriteDataMap {
		get => Instance.m_SpriteDataMap;
		set => Instance.m_SpriteDataMap = value;
	}
	public static HashMap<uint, AtlasData> ShadowDataMap {
		get => Instance.m_ShadowDataMap;
		set => Instance.m_ShadowDataMap = value;
	}
	public static HashMap<uint, AtlasData> UIDataMap {
		get => Instance.m_UIDataMap;
		set => Instance.m_UIDataMap = value;
	}

	#if UNITY_EDITOR
		static bool AutoLoad {
			get => Instance.m_AutoLoad;
			set => Instance.m_AutoLoad  = value;
		}
		static bool AutoDraw {
			get => Instance.m_AutoDraw;
			set {
				var flag = AutoDraw != value;
				Instance.m_AutoDraw  = value;
				#if UNITY_EDITOR
					if (flag && !Application.isPlaying) {
						if (value) DrawEditorMeshes ();
						else       ClearEditorMeshes();
					}
				#endif
			}
		}
		static string CachePath {
			get => Instance.m_CachePath;
			set => Instance.m_CachePath = value;
		}
	#endif



	// Editor Methods

	#if UNITY_EDITOR
		[InitializeOnLoadMethod]
		static void UpdateAtlasMap() => EditorApplication.update += () => {
			if (Instance && AutoLoad) {
				uint flag = 0u;
				if (TileAtlasMap   && TileAtlasMap  .IsDirty) flag |= 1u;
				if (SpriteAtlasMap && SpriteAtlasMap.IsDirty) flag |= 2u;
				if (ShadowAtlasMap && ShadowAtlasMap.IsDirty) flag |= 4u;
				if (UIAtlasMap     && UIAtlasMap    .IsDirty) flag |= 8u;
				if (flag != 0u) {
					bool tileFlag   = (flag & 1u) != 0u;
					bool spriteFlag = (flag & 2u) != 0u;
					bool shadowFlag = (flag & 4u) != 0u;
					bool uiFlag     = (flag & 8u) != 0u;
					if (tileFlag  ) { LoadTileData  (TileAtlasMap  ); TileAtlasMap  .IsDirty = false; }
					if (spriteFlag) { LoadSpriteData(SpriteAtlasMap); SpriteAtlasMap.IsDirty = false; }
					if (shadowFlag) { LoadShadowData(ShadowAtlasMap); ShadowAtlasMap.IsDirty = false; }
					if (uiFlag    ) { LoadUIData    (UIAtlasMap    ); UIAtlasMap    .IsDirty = false; }
					if (AutoDraw) DrawEditorMeshes();
				}
			}
		};

		[InitializeOnLoadMethod]
		static void UpdateSelectedEditorMesh() => EditorApplication.update += () => {
			if (Instance && AutoDraw && Selection.activeGameObject) {
				var status = PrefabUtility.GetPrefabInstanceStatus(Selection.activeGameObject);
				var flag   = !Application.isPlaying && status != PrefabInstanceStatus.Connected;
				if (flag) DrawMesh(Selection.activeGameObject);
			}
		};

		public static void ClearEditorMeshes() {
			foreach (var prefab in Resources.LoadAll<GameObject>("Prefabs")) {
				var flag0 = prefab.TryGetComponent(out MeshFilter   meshFilter  );
				var flag1 = prefab.TryGetComponent(out MeshRenderer meshRenderer);
				if (flag0) DestroyImmediate(meshFilter,   true);
				if (flag1) DestroyImmediate(meshRenderer, true);

				var prefabPath = AssetDatabase.GetAssetPath(prefab);
				PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
			}
			AssetDatabase.Refresh();
		}

		public static void DrawEditorMeshes() {
			foreach (var prefab in Resources.LoadAll<GameObject>("Prefabs")) {
				var flag0 = prefab.TryGetComponent(out MeshFilter   meshFilter  );
				var flag1 = prefab.TryGetComponent(out MeshRenderer meshRenderer);
				if (!flag0) prefab.AddComponent<MeshFilter  >();
				if (!flag1) prefab.AddComponent<MeshRenderer>();
				DrawMesh(prefab);
				var prefabPath = AssetDatabase.GetAssetPath(prefab);
				PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
			}
			AssetDatabase.Refresh();
		}

		static void DrawMesh(GameObject gameObject) {
			if (gameObject == null) return;
			if (gameObject.TryGetComponent(out MeshFilter   meshFilter  ) == false) return;
			if (gameObject.TryGetComponent(out MeshRenderer meshRenderer) == false) return;
			var tileArray   = gameObject.GetComponents<TileDrawerAuthoring  >();
			var spriteArray = gameObject.GetComponents<SpriteDrawerAuthoring>();
			var shadowArray = gameObject.GetComponents<ShadowDrawerAuthoring>();
			var uiArray     = gameObject.GetComponents<UIDrawerAuthoring    >();
			var match = false;
			match |= 0 < tileArray  .Length;
			match |= 0 < spriteArray.Length;
			match |= 0 < shadowArray.Length;
			match |= 0 < uiArray    .Length;
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
				var position = tile.Position;
				var rotation = tile.Rotation;
				var data     = GetTileData(gameObject.transform, tile);
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
				material.SetColor ("_Emission",  data.emission );
				materials.Add(material);
			}
			foreach (var sprite in spriteArray) {
				var position = sprite.Position;
				var rotation = Quaternion.Euler(30f, 0f, 0f);
				var data     = GetSpriteData(gameObject.transform, sprite);
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
				material.SetColor ("_Emission",  data.emission );
				materials.Add(material);
			}
			foreach (var shadow in shadowArray) {
				var position = shadow.Position;
				var rotation = Quaternion.Euler(0f, 0f, 0f);
				var data     = GetShadowData(gameObject.transform, shadow);
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
				var position = ui.Position;
				var rotation = Quaternion.Euler(30f, 0f, 0f);
				var data     = GetUIData(gameObject.transform, ui);
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

			meshFilter  .sharedMesh      = mesh;
			meshRenderer.sharedMaterials = materials.ToArray();
		}
	#endif



	// Tile Methods

	// {TileName}.png
	// {TileName}_{Frame}.png
	// {TileName}_{Frame}_{Milliseconds}.png

	// Default Frame:        0
	// Default Milliseconds: 100

	// TileName: 10 bits
	// Frame:    12 bits

	public static Mesh GetTileMesh() {
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

	public static void LoadTileData(AtlasMapSO atlasMap) {
		TileLUTable ??= new();
		TileDataMap ??= new();
		TileLUTable.Clear();
		TileDataMap.Clear();
		foreach (var pair in atlasMap) {
			var split = pair.Key.Split('_');
			var match = true;

			var tile         = Tile.None;
			var frame        = 0u;
			var milliseconds = 100u;
			if (1 <= split.Length) match &= Enum.TryParse(split[ 0], out tile);
			if (3 <= split.Length) match &= uint.TryParse(split[ 1], out frame);
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
			var sampleABit = ((uint)sampleA + 1u) << 0;
			var sampleBBit = ((uint)sampleB + 1u) << 0;
			var sampleAKey = tileBit | sampleABit;
			var sampleBKey = tileBit | sampleBBit;
			for (var i = sampleAKey; i <= sampleBKey; i++) TileLUTable.TryAdd(i, dataKey);
		}
	}

	public static TileDrawData GetTileData(Transform transform, TileDrawerAuthoring drawer) {
		var position = new float3(transform.position + drawer.Position);
		var rotation = math.mul(transform.rotation, drawer.Rotation);
		var scale    = new float3(1f, 1f, 1f);
		var pivot    = new float3(0f, 0f, 0f);
		var tiling   = new float2(1f, 1f);
		var offset   = new float2(0f, 0f);

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
			rotation  = rotation,
			scale     = scale,

			tiling    = tiling,
			offset    = offset,
			basecolor = drawer.BaseColor,
			maskcolor = drawer.MaskColor,
			emission  = drawer.Emission,
		};
		if (drawer.Flip.x) data.offset.x -= data.tiling.x *= -1f;
		if (drawer.Flip.y) data.offset.y -= data.tiling.y *= -1f;
		return data;
	}



	// Sprite Methods
	
	// {SpriteName}.png
	// {SpriteName}_{Frame}_{Milliseconds}.png
	// {SpriteName}_{Direction}_{Frame}_{Milliseconds}.png
	// {SpriteName}_{MotionName}_{Direction}_{Frame}_{Milliseconds}.png

	// Default Motion:       None
	// Default Direction:    0
	// Default Frame:        0
	// Default Milliseconds: 100

	// Direction:
	// 1 ~  2:  2 directions (right, left)
	// 3 ~  4:  4 directions (down, right, up, left)
	// 5 ~  8:  8 directions (down, right-down, right, right-up, up, left-up, left, left-down)
	// 9 ~ 16: 16 directions (down, ...)

	// SpriteName: 10 bits
	// MotionName:  5 bits
	// Direction:   5 bits
	// Frame:      12 bits

	public static Mesh GetSpriteMesh() {
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

	public static void LoadSpriteData(AtlasMapSO atlasMap) {
		SpriteLUTable ??= new();
		SpriteDataMap ??= new();
		SpriteLUTable.Clear();
		SpriteDataMap.Clear();
		foreach (var pair in atlasMap) {
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
	}

	public static SpriteDrawData GetSpriteData(Transform transform, SpriteDrawerAuthoring drawer) {
		var yawGlobal = 0f;
		var position  = new float3(transform.position + drawer.Position);
		var scale     = new float2(1f, 1f);
		var pivot     = new float2(drawer.Pivot.x, drawer.Pivot.y);
		var tiling    = new float2(1f, 1f);
		var offset    = new float2(0f, 0f);
		var xflip     = false;

		var spriteBit    = ((uint)drawer.Sprite + 1u) << 22;
		var motionBit    = ((uint)drawer.Motion + 1u) << 17;
		var directionBit = ((uint)0u            + 1u) << 12;
		var sampleBit    = ((uint)0u            + 1u) <<  0;

		var numDirectionsKey = spriteBit | motionBit;
		if (SpriteLUTable.TryGetValue(numDirectionsKey, out var numDirections)) {
			var maxDirections = 1u;
			var yaw = drawer.Yaw + (drawer.YawLocal ? 0f : yawGlobal);
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
			xflip = numDirections <= direction;
			if (xflip) switch (maxDirections) {
				case  2u: direction =  1u - direction; break;
				case  4u: direction =  4u - direction; break;
				case  8u: direction =  8u - direction; break;
				case 16u: direction = 16u - direction; break;
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
				pivot += atlas.pivot * new float2(xflip ? -1f : 1f, 1f);
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
		if (drawer.Flip.x != xflip) data.offset.x -= data.tiling.x *= -1f;
		if (drawer.Flip.y         ) data.offset.y -= data.tiling.y *= -1f;
		return data;
	}



	// Shadow Methods

	// {SpriteName}.png
	// {SpriteName}_{Frame}_{Milliseconds}.png
	// {SpriteName}_{MotionName}_{Frame}_{Milliseconds}.png

	// Default Motion:       None
	// Default Frame:        0
	// Default Milliseconds: 100

	// SpriteName: 10 bits
	// MotionName:  5 bits
	// Frame:      12 bits

	public static Mesh GetShadowMesh() {
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

	public static void LoadShadowData(AtlasMapSO atlasMap) {
		ShadowLUTable ??= new();
		ShadowDataMap ??= new();
		ShadowLUTable.Clear();
		ShadowDataMap.Clear();
		foreach (var pair in atlasMap) {
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
	}

	public static ShadowDrawData GetShadowData(Transform transform, ShadowDrawerAuthoring drawer) {
		var yawGlobal = 0f;
		var position  = new float3(transform.position + drawer.Position);
		var scale     = new float2(1f, 1f);
		var pivot     = new float2(0f, 0f);
		var tiling    = new float2(1f, 1f);
		var offset    = new float2(0f, 0f);
		var xflip     = false;

		var shadowBit = ((uint)drawer.Shadow + 1u) << 22;
		var motionBit = ((uint)drawer.Motion + 1u) << 17;
		var sampleBit = ((uint)0u            + 1u) <<  0;

		var numDirectionsKey = shadowBit | motionBit;
		if (ShadowLUTable.TryGetValue(numDirectionsKey, out var numDirections)) {
			var maxDirections = 1u;
			var yaw = drawer.Yaw + (drawer.YawLocal ? 0f : yawGlobal);
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
			xflip = numDirections <= direction;
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
				pivot += atlas.pivot * new float2(xflip ? -1f : 1f, 1f);
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
		if (drawer.Flip.x != xflip) data.offset.x -= data.tiling.x *= -1f;
		if (drawer.Flip.y         ) data.offset.y -= data.tiling.y *= -1f;
		return data;
	}



	// UI Methods

	// {UIName}.png
	// {UIName}_{Frame}.png
	// {UIName}_{Frame}_{Milliseconds}.png

	// Default Frame:        0
	// Default Milliseconds: 100

	// UIName: 10 bits
	// Frame:  12 bits

	public static Mesh GetUIMesh() {
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

	public static void LoadUIData(AtlasMapSO atlasMap) {
		UILUTable ??= new();
		UIDataMap ??= new();
		UILUTable.Clear();
		UIDataMap.Clear();
		foreach (var pair in atlasMap) {
			var split = pair.Key.Split('_');
			var match = true;

			var ui           = UI.None;
			var frame        = 0u;
			var milliseconds = 100u;
			if (1 <= split.Length) match &= Enum.TryParse(split[ 0], out ui);
			if (2 <= split.Length) match &= uint.TryParse(split[ 1], out frame);
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
	}

	public static UIDrawData GetUIData(Transform transform, UIDrawerAuthoring drawer) {
		var position = new float3(transform.position + drawer.Position);
		var scale    = new float2(drawer.Scale.x, drawer.Scale.y);
		var pivot    = new float2(drawer.Pivot.x, drawer.Pivot.y);
		var tiling   = new float2(1f, 1f);
		var offset   = new float2(0f, 0f);

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
		if (drawer.Flip.x) data.offset.x -= data.tiling.x *= -1f;
		if (drawer.Flip.y) data.offset.y -= data.tiling.y *= -1f;
		return data;
	}



	// Lifecycle

	#if UNITY_EDITOR
		void OnEnable () { if (this == Instance && AutoDraw) ClearEditorMeshes(); }
		void OnDisable() { if (this == Instance && AutoDraw) DrawEditorMeshes (); }
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
		TileDataMap   = new(DrawManager.TileDataMap  .Count, Allocator.Persistent);
		SpriteDataMap = new(DrawManager.SpriteDataMap.Count, Allocator.Persistent);
		ShadowDataMap = new(DrawManager.ShadowDataMap.Count, Allocator.Persistent);
		UIDataMap     = new(DrawManager.UIDataMap    .Count, Allocator.Persistent);
		foreach (var pair in DrawManager.TileLUTable  ) TileLUTable  .Add(pair.Key, pair.Value);
		foreach (var pair in DrawManager.SpriteLUTable) SpriteLUTable.Add(pair.Key, pair.Value);
		foreach (var pair in DrawManager.ShadowLUTable) ShadowLUTable.Add(pair.Key, pair.Value);
		foreach (var pair in DrawManager.UILUTable    ) UILUTable    .Add(pair.Key, pair.Value);
		foreach (var pair in DrawManager.TileDataMap  ) TileDataMap  .Add(pair.Key, pair.Value);
		foreach (var pair in DrawManager.SpriteDataMap) SpriteDataMap.Add(pair.Key, pair.Value);
		foreach (var pair in DrawManager.ShadowDataMap) ShadowDataMap.Add(pair.Key, pair.Value);
		foreach (var pair in DrawManager.UIDataMap    ) UIDataMap    .Add(pair.Key, pair.Value);

		TileRenderer   = new(DrawManager.TileMaterial,   DrawManager.TileMesh  );
		SpriteRenderer = new(DrawManager.SpriteMaterial, DrawManager.SpriteMesh);
		ShadowRenderer = new(DrawManager.ShadowMaterial, DrawManager.ShadowMesh);
		UIRenderer     = new(DrawManager.UIMaterial,     DrawManager.UIMesh    );
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
		var transform = GetComponentLookup<LocalToWorld>(true);

		var tileDrawer = GetBufferLookup<TileDrawer>(true);
		for (int i = 0; i < TileQuery.Length; i++) if (0 < TileQuery[i].CalculateEntityCount()) {
			var entityArray = TileQuery[i].ToEntityArray(Allocator.TempJob);
			var indexArray  = new NativeArray<int>(entityArray.Length + 1, Allocator.TempJob);
			indexArray[0] = 0;
			new GetLengthJob<TileDrawer> {
				IndexArray  = indexArray,
				EntityArray = entityArray,
				DrawerGroup = tileDrawer,
			}.Schedule(entityArray.Length, 64, Dependency).Complete();
			new PrefixSumJob {
				IndexArray = indexArray,
			}.Schedule(Dependency).Complete();
			var count = indexArray[^1];
			if (0 < count) {
				new DrawTileJob {
					DataArray      = TileRenderer.AllocateAndLockBuffer(i, count),
					IndexArray     = indexArray,
					EntityArray    = entityArray,
					TransformGroup = transform,
					DrawerGroup    = tileDrawer,
					LUTable        = TileLUTable,
					DataMap        = TileDataMap,
					Random         = new Random(1u + (uint)(4801 * SystemAPI.Time.ElapsedTime) % 1000),
				}.Schedule(entityArray.Length, 64, Dependency).Complete();
				TileRenderer.UnlockBuffer(count);
			}
			if (i != TileQuery.Length - 1) foreach (var entity in entityArray) {
				buffer.RemoveComponent<TileDrawer>(entity);
			}
			entityArray.Dispose();
			indexArray .Dispose();
		}
		TileRenderer.Draw();
		TileRenderer.Clear(TileQuery.Length - 1);

		var spriteDrawer = GetBufferLookup<SpriteDrawer>(true);
		for (int i = 0; i < SpriteQuery.Length; i++) if (0 < SpriteQuery[i].CalculateEntityCount()) {
			var entityArray = SpriteQuery[i].ToEntityArray(Allocator.TempJob);
			var indexArray  = new NativeArray<int>(entityArray.Length + 1, Allocator.TempJob);
			indexArray[0] = 0;
			new GetLengthJob<SpriteDrawer> {
				IndexArray  = indexArray,
				EntityArray = entityArray,
				DrawerGroup = spriteDrawer,
			}.Schedule(entityArray.Length, 64, Dependency).Complete();
			new PrefixSumJob {
				IndexArray = indexArray,
			}.Schedule(Dependency).Complete();
			var count = indexArray[^1];
			if (0 < count) {
				new DrawSpriteJob {
					DataArray      = SpriteRenderer.AllocateAndLockBuffer(i, count),
					IndexArray     = indexArray,
					EntityArray    = entityArray,
					TransformGroup = transform,
					DrawerGroup    = spriteDrawer,
					LUTable        = SpriteLUTable,
					DataMap        = SpriteDataMap,
					YawGlobal      = CameraManager.EulerRotation.y,
				}.Schedule(entityArray.Length, 64, Dependency).Complete();
				SpriteRenderer.UnlockBuffer(count);
			}
			if (i != SpriteQuery.Length - 1) foreach (var entity in entityArray) {
				buffer.RemoveComponent<SpriteDrawer>(entity);
			}
			entityArray.Dispose();
			indexArray .Dispose();
		}
		SpriteRenderer.Draw();
		SpriteRenderer.Clear(SpriteQuery.Length - 1);

		var shadowDrawer = GetBufferLookup<ShadowDrawer>(true);
		for (int i = 0; i < ShadowQuery.Length; i++) if (0 < ShadowQuery[i].CalculateEntityCount()) {
			var entityArray = ShadowQuery[i].ToEntityArray(Allocator.TempJob);
			var indexArray  = new NativeArray<int>(entityArray.Length + 1, Allocator.TempJob);
			indexArray[0] = 0;
			new GetLengthJob<ShadowDrawer> {
				IndexArray  = indexArray,
				EntityArray = entityArray,
				DrawerGroup = shadowDrawer,
			}.Schedule(entityArray.Length, 64, Dependency).Complete();
			new PrefixSumJob {
				IndexArray = indexArray,
			}.Schedule(Dependency).Complete();
			var count = indexArray[^1];
			if (0 < count) {
				new DrawShadowJob {
					DataArray      = ShadowRenderer.AllocateAndLockBuffer(i, count),
					IndexArray     = indexArray,
					EntityArray    = entityArray,
					TransformGroup = transform,
					DrawerGroup    = shadowDrawer,
					LUTable        = ShadowLUTable,
					DataMap        = ShadowDataMap,
					YawGlobal      = CameraManager.EulerRotation.y,
				}.Schedule(entityArray.Length, 64, Dependency).Complete();
				ShadowRenderer.UnlockBuffer(count);
			}
			if (i != ShadowQuery.Length - 1) foreach (var entity in entityArray) {
				buffer.RemoveComponent<ShadowDrawer>(entity);
			}
			entityArray.Dispose();
			indexArray .Dispose();
		}
		ShadowRenderer.Draw();
		ShadowRenderer.Clear(ShadowQuery.Length - 1);

		var uiDrawer = GetBufferLookup<UIDrawer>(true);
		for (int i = 0; i < UIQuery.Length; i++) if (0 < UIQuery[i].CalculateEntityCount()) {
			var entityArray = UIQuery[i].ToEntityArray(Allocator.TempJob);
			var indexArray  = new NativeArray<int>(entityArray.Length + 1, Allocator.TempJob);
			indexArray[0] = 0;
			new GetLengthJob<UIDrawer> {
				IndexArray  = indexArray,
				EntityArray = entityArray,
				DrawerGroup = uiDrawer,
			}.Schedule(entityArray.Length, 64, Dependency).Complete();
			new PrefixSumJob {
				IndexArray = indexArray,
			}.Schedule(Dependency).Complete();
			var count = indexArray[^1];
			if (0 < count) {
				new DrawUIJob {
					DataArray      = UIRenderer.AllocateAndLockBuffer(i, count),
					IndexArray     = indexArray,
					EntityArray    = entityArray,
					TransformGroup = transform,
					DrawerGroup    = uiDrawer,
					LUTable        = UILUTable,
					DataMap        = UIDataMap,
					Random         = new Random(1u + (uint)(4801 * SystemAPI.Time.ElapsedTime) % 1000),
				}.Schedule(entityArray.Length, 64, Dependency).Complete();
				UIRenderer.UnlockBuffer(count);
			}
			if (i != UIQuery.Length - 1) foreach (var entity in entityArray) {
				buffer.RemoveComponent<UIDrawer>(entity);
			}
			entityArray.Dispose();
			indexArray .Dispose();
		}
		UIRenderer.Draw();
		UIRenderer.Clear(UIQuery.Length - 1);
	}

	[BurstCompile]
	partial struct GetLengthJob<T> : IJobParallelFor where T : unmanaged, IBufferElementData {
		[NativeDisableParallelForRestriction] public NativeArray<int> IndexArray;
		[ReadOnly] public NativeArray<Entity> EntityArray;
		[ReadOnly] public BufferLookup<T> DrawerGroup;
		public void Execute(int index) {
			IndexArray[1 + index] = DrawerGroup[EntityArray[index]].Length;
		}
	}

	[BurstCompile]
	partial struct PrefixSumJob : IJob {
		public NativeArray<int> IndexArray;
		public void Execute() {
			for (int i = 1; i < IndexArray.Length; i++) IndexArray[i] += IndexArray[i - 1];
		}
	}

	[BurstCompile]
	partial struct DrawTileJob : IJobParallelFor {
		const float SampleRate = DrawManager.SampleRate;
		[NativeDisableParallelForRestriction] public NativeArray<TileDrawData> DataArray;
		[ReadOnly] public NativeArray<int   > IndexArray;
		[ReadOnly] public NativeArray<Entity> EntityArray;
		[ReadOnly] public ComponentLookup<LocalToWorld> TransformGroup;
		[ReadOnly] public BufferLookup   <TileDrawer  > DrawerGroup;
		[ReadOnly] public NativeHashMap<uint, uint     > LUTable;
		[ReadOnly] public NativeHashMap<uint, AtlasData> DataMap;
		[ReadOnly] public Random Random;

		public void Execute(int index) {
			var transform = TransformGroup[EntityArray[index]];
			var drawer    = DrawerGroup   [EntityArray[index]];
			for (int i = 0; i < drawer.Length; i++) {
				DataArray[IndexArray[index] + i] = GetTileData(transform, drawer[i]);
			}
		}

		public TileDrawData GetTileData(LocalToWorld transform, TileDrawer drawer) {
			var position = transform.Position + math.mul(transform.Rotation, drawer.Position);
			var rotation = math.mul(transform.Rotation, drawer.Rotation);
			var scale    = new float3(1f, 1f, 1f);
			var pivot    = new float3(0f, 0f, 0f);
			var tiling   = new float2(1f, 1f);
			var offset   = new float2(0f, 0f);

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
				rotation  = rotation,
				scale     = scale,

				tiling    = tiling,
				offset    = offset,
				basecolor = drawer.BaseColor,
				maskcolor = drawer.MaskColor,
				emission  = drawer.Emission,
			};
			if (drawer.FlipRandomly) drawer.Flip = Random.NextBool2();
			if (drawer.Flip.x) data.offset.x -= data.tiling.x *= -1f;
			if (drawer.Flip.y) data.offset.y -= data.tiling.y *= -1f;
			return data;
		}
	}

	[BurstCompile]
	partial struct DrawSpriteJob : IJobParallelFor {
		const float SampleRate = DrawManager.SampleRate;
		[NativeDisableParallelForRestriction] public NativeArray<SpriteDrawData> DataArray;
		[ReadOnly] public NativeArray<int   > IndexArray;
		[ReadOnly] public NativeArray<Entity> EntityArray;
		[ReadOnly] public ComponentLookup<LocalToWorld> TransformGroup;
		[ReadOnly] public BufferLookup   <SpriteDrawer> DrawerGroup;
		[ReadOnly] public NativeHashMap<uint, uint     > LUTable;
		[ReadOnly] public NativeHashMap<uint, AtlasData> DataMap;
		[ReadOnly] public float YawGlobal;

		public void Execute(int index) {
			var transform = TransformGroup[EntityArray[index]];
			var drawer    = DrawerGroup   [EntityArray[index]];
			for (int i = 0; i < drawer.Length; i++) {
				DataArray[IndexArray[index] + i] = GetSpriteData(transform, drawer[i]);
			}
		}

		public SpriteDrawData GetSpriteData(LocalToWorld transform, SpriteDrawer drawer) {
			var position = transform.Position + math.mul(transform.Rotation, drawer.Position);
			var scale    = new float2(1f, 1f);
			var pivot    = new float2(drawer.Pivot.x, drawer.Pivot.y);
			var tiling   = new float2(1f, 1f);
			var offset   = new float2(0f, 0f);
			var xflip    = false;

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
				xflip = numDirections <= direction;
				if (xflip) switch (maxDirections) {
					case  2u: direction =  1u - direction; break;
					case  4u: direction =  4u - direction; break;
					case  8u: direction =  8u - direction; break;
					case 16u: direction = 16u - direction; break;
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
					pivot += atlas.pivot * new float2(xflip ? -1f : 1f, 1f);
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
			if (drawer.Flip.x != xflip) data.offset.x -= data.tiling.x *= -1f;
			if (drawer.Flip.y         ) data.offset.y -= data.tiling.y *= -1f;
			return data;
		}
	}

	[BurstCompile]
	partial struct DrawShadowJob : IJobParallelFor {
		const float SampleRate = DrawManager.SampleRate;
		[NativeDisableParallelForRestriction] public NativeArray<ShadowDrawData> DataArray;
		[ReadOnly] public NativeArray<int   > IndexArray;
		[ReadOnly] public NativeArray<Entity> EntityArray;
		[ReadOnly] public ComponentLookup<LocalToWorld> TransformGroup;
		[ReadOnly] public BufferLookup   <ShadowDrawer> DrawerGroup;
		[ReadOnly] public NativeHashMap<uint, uint     > LUTable;
		[ReadOnly] public NativeHashMap<uint, AtlasData> DataMap;
		[ReadOnly] public float YawGlobal;

		public void Execute(int index) {
			var transform = TransformGroup[EntityArray[index]];
			var drawer    = DrawerGroup   [EntityArray[index]];
			for (int i = 0; i < drawer.Length; i++) {
				DataArray[IndexArray[index] + i] = GetShadowData(transform, drawer[i]);
			}
		}

		public ShadowDrawData GetShadowData(LocalToWorld transform, ShadowDrawer drawer) {
			var position = transform.Position + math.mul(transform.Rotation, drawer.Position);
			var scale    = new float2(1f, 1f);
			var pivot    = new float2(0f, 0f);
			var tiling   = new float2(1f, 1f);
			var offset   = new float2(0f, 0f);
			var xflip    = false;

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
				xflip = numDirections <= direction;
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
					pivot += atlas.pivot * new float2(xflip ? -1f : 1f, 1f);
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
			if (drawer.Flip.x != xflip) data.offset.x -= data.tiling.x *= -1f;
			if (drawer.Flip.y         ) data.offset.y -= data.tiling.y *= -1f;
			return data;
		}
	}

	[BurstCompile]
	partial struct DrawUIJob : IJobParallelFor {
		const float SampleRate = DrawManager.SampleRate;
		[NativeDisableParallelForRestriction] public NativeArray<UIDrawData> DataArray;
		[ReadOnly] public NativeArray<int   > IndexArray;
		[ReadOnly] public NativeArray<Entity> EntityArray;
		[ReadOnly] public ComponentLookup<LocalToWorld> TransformGroup;
		[ReadOnly] public BufferLookup   <UIDrawer    > DrawerGroup;
		[ReadOnly] public NativeHashMap<uint, uint     > LUTable;
		[ReadOnly] public NativeHashMap<uint, AtlasData> DataMap;
		[ReadOnly] public Random Random;

		public void Execute(int index) {
			var transform = TransformGroup[EntityArray[index]];
			var drawer    = DrawerGroup   [EntityArray[index]];
			for (int i = 0; i < drawer.Length; i++) {
				DataArray[IndexArray[index] + i] = GetUIData(transform, drawer[i]);
			}
		}

		public UIDrawData GetUIData(LocalToWorld transform, UIDrawer drawer) {
			var position = transform.Position + math.mul(transform.Rotation, drawer.Position);
			var scale    = new float2(drawer.Scale.x, drawer.Scale.y);
			var pivot    = new float2(drawer.Pivot.x, drawer.Pivot.y);
			var tiling   = new float2(1f, 1f);
			var offset   = new float2(0f, 0f);

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
			if (drawer.Flip.x) data.offset.x -= data.tiling.x *= -1f;
			if (drawer.Flip.y) data.offset.y -= data.tiling.y *= -1f;
			return data;
		}
	}
}

using UnityEngine;
using System;
using System.Runtime.InteropServices;
using Object = UnityEngine.Object;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Draw Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Singleton Manager/Draw Manager")]
public sealed class DrawManager : MonoSingleton<DrawManager> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(DrawManager))]
	class DrawManagerEditor : EditorExtensions {
		DrawManager I => target as DrawManager;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Material", EditorStyles.boldLabel);
			CanvasMaterial = ObjectField("Canvas Material", CanvasMaterial);
			OpaqueMaterial = ObjectField("Opaque Material", OpaqueMaterial);
			ShadowMaterial = ObjectField("Shadow Material", ShadowMaterial);
			SpriteMaterial = ObjectField("Sprite Material", SpriteMaterial);
			Space();

			LabelField("Atlas Map", EditorStyles.boldLabel);
			CanvasAtlasMapPath = TextField("Canvas Atlas Map Path", CanvasAtlasMapPath);
			OpaqueAtlasMapPath = TextField("Opaque Atlas Map Path", OpaqueAtlasMapPath);
			ShadowAtlasMapPath = TextField("Shadow Atlas Map Path", ShadowAtlasMapPath);
			SpriteAtlasMapPath = TextField("Sprite Atlas Map Path", SpriteAtlasMapPath);
			BeginHorizontal();
			PrefixLabel("Load Hash Map");
			if (Button("Clear")) {
				ClearCanvasData();
				ClearOpaqueData();
				ClearShadowData();
				ClearSpriteData();
			}
			if (Button("Load")) {
				LoadCanvasData();
				LoadOpaqueData();
				LoadShadowData();
				LoadSpriteData();
			}
			EndHorizontal();
			Space();

			LabelField("Hash Map", EditorStyles.boldLabel);
			int size = Marshal.SizeOf<uint>() + Marshal.SizeOf<AtlasData>();
			BeginHorizontal();
			PrefixLabel("Canvas Hash Map Size");
			int canvasCount = CanvasHashMap.Count;
			if (Button("↻", GUILayout.Width(20))) LoadCanvasData();
			LabelField($" {canvasCount:N0}  ({canvasCount * size:N0} bytes)");
			EndHorizontal();
			BeginHorizontal();
			PrefixLabel("Opaque Hash Map Size");
			int opaqueCount = OpaqueHashMap.Count;
			if (Button("↻", GUILayout.Width(20))) LoadOpaqueData();
			LabelField($" {opaqueCount:N0}  ({opaqueCount * size:N0} bytes)");
			EndHorizontal();
			BeginHorizontal();
			PrefixLabel("Shadow Hash Map Size");
			int shadowCount = ShadowHashMap.Count;
			if (Button("↻", GUILayout.Width(20))) LoadShadowData();
			LabelField($" {shadowCount:N0}  ({shadowCount * size:N0} bytes)");
			EndHorizontal();
			BeginHorizontal();
			PrefixLabel("Sprite Hash Map Size");
			int spriteCount = SpriteHashMap.Count;
			if (Button("↻", GUILayout.Width(20))) LoadSpriteData();
			LabelField($" {spriteCount:N0}  ({spriteCount * size:N0} bytes)");
			EndHorizontal();
			Space();

			End();
		}
	}
	#endif



	// Constants

	public const float SampleRate = 60f;
	public const float SampleInterval = 1f / SampleRate;



	// Fields

	[SerializeField] Material m_CanvasMaterial;
	[SerializeField] Material m_OpaqueMaterial;
	[SerializeField] Material m_ShadowMaterial;
	[SerializeField] Material m_SpriteMaterial;

	[SerializeField] string m_CanvasAtlasMapPath = "Assets/Textures/CanvasAtlasMap.asset";
	[SerializeField] string m_OpaqueAtlasMapPath = "Assets/Textures/OpaqueAtlasMap.asset";
	[SerializeField] string m_ShadowAtlasMapPath = "Assets/Textures/ShadowAtlasMap.asset";
	[SerializeField] string m_SpriteAtlasMapPath = "Assets/Textures/SpriteAtlasMap.asset";

	[SerializeField] HashMap<uint, AtlasData> m_CanvasHashMap = new();
	[SerializeField] HashMap<uint, AtlasData> m_OpaqueHashMap = new();
	[SerializeField] HashMap<uint, AtlasData> m_ShadowHashMap = new();
	[SerializeField] HashMap<uint, AtlasData> m_SpriteHashMap = new();

	NativeHashMap<uint, AtlasData> m_CanvasNativeHashMap;
	NativeHashMap<uint, AtlasData> m_OpaqueNativeHashMap;
	NativeHashMap<uint, AtlasData> m_ShadowNativeHashMap;
	NativeHashMap<uint, AtlasData> m_SpriteNativeHashMap;

	Mesh m_QuadMesh;



	// Properties

	public static Material CanvasMaterial {
		get         => Instance.m_CanvasMaterial;
		private set => Instance.m_CanvasMaterial = value;
	}
	public static Material OpaqueMaterial {
		get         => Instance.m_OpaqueMaterial;
		private set => Instance.m_OpaqueMaterial = value;
	}
	public static Material ShadowMaterial {
		get         => Instance.m_ShadowMaterial;
		private set => Instance.m_ShadowMaterial = value;
	}
	public static Material SpriteMaterial {
		get         => Instance.m_SpriteMaterial;
		private set => Instance.m_SpriteMaterial = value;
	}

	static string CanvasAtlasMapPath {
		get => Instance.m_CanvasAtlasMapPath;
		set => Instance.m_CanvasAtlasMapPath = value;
	}
	static string OpaqueAtlasMapPath {
		get => Instance.m_OpaqueAtlasMapPath;
		set => Instance.m_OpaqueAtlasMapPath = value;
	}
	static string ShadowAtlasMapPath {
		get => Instance.m_ShadowAtlasMapPath;
		set => Instance.m_ShadowAtlasMapPath = value;
	}
	static string SpriteAtlasMapPath {
		get => Instance.m_SpriteAtlasMapPath;
		set => Instance.m_SpriteAtlasMapPath = value;
	}

	static HashMap<uint, AtlasData> CanvasHashMap {
		get => Instance.m_CanvasHashMap;
		set => Instance.m_CanvasHashMap = value;
	}
	static HashMap<uint, AtlasData> OpaqueHashMap {
		get => Instance.m_OpaqueHashMap;
		set => Instance.m_OpaqueHashMap = value;
	}
	static HashMap<uint, AtlasData> ShadowHashMap {
		get => Instance.m_ShadowHashMap;
		set => Instance.m_ShadowHashMap = value;
	}
	static HashMap<uint, AtlasData> SpriteHashMap {
		get => Instance.m_SpriteHashMap;
		set => Instance.m_SpriteHashMap = value;
	}

	static NativeHashMap<uint, AtlasData> CanvasNativeHashMap {
		get => Instance.m_CanvasNativeHashMap;
		set => Instance.m_CanvasNativeHashMap = value;
	}
	static NativeHashMap<uint, AtlasData> OpaqueNativeHashMap {
		get => Instance.m_OpaqueNativeHashMap;
		set => Instance.m_OpaqueNativeHashMap = value;
	}
	static NativeHashMap<uint, AtlasData> ShadowNativeHashMap {
		get => Instance.m_ShadowNativeHashMap;
		set => Instance.m_ShadowNativeHashMap = value;
	}
	static NativeHashMap<uint, AtlasData> SpriteNativeHashMap {
		get => Instance.m_SpriteNativeHashMap;
		set => Instance.m_SpriteNativeHashMap = value;
	}

	public static NativeHashMap<uint, AtlasData>.ReadOnly CanvasHashMapReadOnly {
		get => CanvasNativeHashMap.AsReadOnly();
	}
	public static NativeHashMap<uint, AtlasData>.ReadOnly OpaqueHashMapReadOnly {
		get => OpaqueNativeHashMap.AsReadOnly();
	}
	public static NativeHashMap<uint, AtlasData>.ReadOnly ShadowHashMapReadOnly {
		get => ShadowNativeHashMap.AsReadOnly();
	}
	public static NativeHashMap<uint, AtlasData>.ReadOnly SpriteHashMapReadOnly {
		get => SpriteNativeHashMap.AsReadOnly();
	}

	public static Mesh QuadMesh {
		get {
			if (Instance.m_QuadMesh == null) {
				Instance.m_QuadMesh = new() {
					name = "Quad",
					vertices = new Vector3[] {
						new(-0.5f, -0.5f, +0.0f), new(+0.5f, -0.5f, +0.0f),
						new(-0.5f, +0.5f, +0.0f), new(+0.5f, +0.5f, +0.0f),
					},
					uv = new Vector2[] {
						new(0.0f, 0.0f), new(1.0f, 0.0f),
						new(0.0f, 1.0f), new(1.0f, 1.0f),
					},
					normals = new Vector3[] {
						new(+0.0f, -1.0f, +0.0f), new(+0.0f, -1.0f, +0.0f),
						new(+0.0f, -1.0f, +0.0f), new(+0.0f, -1.0f, +0.0f),
					},
					triangles = new int[] {
						0, 3, 1,
						3, 0, 2,
					},
				};
			}
			return Instance.m_QuadMesh;
		}
	}



	// Methods

	#if UNITY_EDITOR
	static T LoadAsset<T>(string path) where T : Object {
		return AssetDatabase.LoadAssetAtPath<T>(path);
	}
	static AtlasMapSO GetCanvasAtlasMap() => LoadAsset<AtlasMapSO>(CanvasAtlasMapPath);
	static AtlasMapSO GetOpaqueAtlasMap() => LoadAsset<AtlasMapSO>(OpaqueAtlasMapPath);
	static AtlasMapSO GetShadowAtlasMap() => LoadAsset<AtlasMapSO>(ShadowAtlasMapPath);
	static AtlasMapSO GetSpriteAtlasMap() => LoadAsset<AtlasMapSO>(SpriteAtlasMapPath);
	#else
	static AtlasMapSO GetCanvasAtlasMap() => null;
	static AtlasMapSO GetOpaqueAtlasMap() => null;
	static AtlasMapSO GetShadowAtlasMap() => null;
	static AtlasMapSO GetSpriteAtlasMap() => null;
	#endif



	// Canvas Methods

	// Variants
	// {Canvas}.png
	// {Canvas}_{Index}_{Millisecond}.png

	#if UNITY_EDITOR
	static void ClearCanvasData() {
		CanvasHashMap.Clear();
	}

	static void LoadCanvasData() {
		ClearCanvasData();
		var atlasMap = GetCanvasAtlasMap();
		if (atlasMap == null) {
			Debug.Log($"Canvas Atlas Map is missing at {CanvasAtlasMapPath}");
		} else {
			atlasMap.GenerateAtlasMap();
			foreach (var (name, data) in atlasMap) {
				var canvas      = default(Canvas);
				var index       = 0u;
				var millisecond = 0u;

				var match = true;
				var split = name.Split('_');
				if (1 <= split.Length) match = match && Enum.TryParse(split[ 0], out canvas);
				if (3 <= split.Length) match = match && uint.TryParse(split[^2], out index);
				if (3 <= split.Length) match = match && uint.TryParse(split[^1], out millisecond);
				if (!match) continue;

				var temp = new CanvasHash() {
					Canvas = canvas,
				};
				CanvasHashMap.TryAdd(temp.Key, new AtlasData());
				var totalFrame = CanvasHashMap[temp.Key];
				var a = (uint)totalFrame.scale.x;
				var b = a + millisecond * SampleRate * 0.001f;
				totalFrame.scale.x = b;
				CanvasHashMap[temp.Key] = totalFrame;

				for (var frame = a; frame <= b; frame++) {
					temp.Tick = frame;
					CanvasHashMap.TryAdd(temp.Key, data);
				}
			}
			CanvasHashMap.TrimExcess();
			if (CanvasMaterial != null) {
				var data = GetCanvasData(new CanvasHash() {
					Canvas = default,
					Tick   = default,
				});
				CanvasMaterial.SetVector(CanvasProperty.Scale,  (Vector2)data.scale);
				CanvasMaterial.SetVector(CanvasProperty.Pivot,  (Vector2)data.pivot);
				CanvasMaterial.SetVector(CanvasProperty.Tiling, (Vector2)data.tiling);
				CanvasMaterial.SetVector(CanvasProperty.Offset, (Vector2)data.offset);
			}
			var properties = Resources.FindObjectsOfTypeAll<CanvasPropertyAuthoring>();
			foreach (var property in properties) property.UpdateProperty();
		}
	}
	#endif

	public static AtlasData GetCanvasData(CanvasHash hash, bool useFallback = true) {
		return GetCanvasData(CanvasHashMap, hash, useFallback);
	}

	static AtlasData GetCanvasData(
		HashMap<uint, AtlasData> CanvasHashMap,
		CanvasHash hash, bool useFallback = true) {
		var temp = new CanvasHash() {
			Canvas = hash.Canvas,
			Flip    = hash.Flip,
		};
		if (CanvasHashMap.TryGetValue(temp.Key, out var totalFrame)) {
			uint time = (uint)totalFrame.scale.x;
			uint tick = hash.Tick;
			temp.Tick = (time == 0u) ? 0u : (time <= tick) ? (tick % time) : tick;
		}
		if (CanvasHashMap.TryGetValue(temp.Key, out var finalData)) {
			if (temp.Flip.x) finalData.pivot.x *= -1f;
			if (temp.Flip.y) finalData.pivot.y *= -1f;
			if (temp.Flip.x) finalData.offset.x -= finalData.tiling.x *= -1f;
			if (temp.Flip.y) finalData.offset.y -= finalData.tiling.y *= -1f;
			return finalData;
		} else if (useFallback) {
			if (hash.Canvas != default) {
				return GetCanvasData(CanvasHashMap, new CanvasHash() {
					Canvas = default,
					Tick   = default,
				}, useFallback);
			} else return new AtlasData() {
				scale  = new(1f, 1f),
				pivot  = new(0f, 0f),
				tiling = new(1f, 1f),
				offset = new(0f, 0f),
			};
		} else return default;
	}



	// Opaque Methods

	// Variants
	// {Opaque}.png
	// {Opaque}_{Index}_{Millisecond}.png

	#if UNITY_EDITOR
	static void ClearOpaqueData() {
		OpaqueHashMap.Clear();
	}

	static void LoadOpaqueData() {
		ClearOpaqueData();
		var atlasMap = GetOpaqueAtlasMap();
		if (atlasMap == null) {
			Debug.Log($"Opaque Atlas Map is missing at {OpaqueAtlasMapPath}");
		} else {
			atlasMap.GenerateAtlasMap();
			foreach (var (name, data) in atlasMap) {
				var opaque      = default(Opaque);
				var index       = 0u;
				var millisecond = 0u;

				var match = true;
				var split = name.Split('_');
				if (1 <= split.Length) match = match && Enum.TryParse(split[ 0], out opaque);
				if (3 <= split.Length) match = match && uint.TryParse(split[^2], out index);
				if (3 <= split.Length) match = match && uint.TryParse(split[^1], out millisecond);
				if (!match) continue;

				var temp = new OpaqueHash() {
					Opaque = opaque,
				};
				OpaqueHashMap.TryAdd(temp.Key, new AtlasData());
				var totalFrame = OpaqueHashMap[temp.Key];
				var a = totalFrame.scale.x;
				var b = a + millisecond * SampleRate * 0.001f;
				totalFrame.scale.x = b;
				OpaqueHashMap[temp.Key] = totalFrame;

				for (var frame = a; frame <= b; frame++) {
					temp.Tick = (uint)frame;
					OpaqueHashMap.TryAdd(temp.Key, data);
				}
			}
			OpaqueHashMap.TrimExcess();
			if (OpaqueMaterial != null) {
				var data = GetOpaqueData(new OpaqueHash() {
					Opaque = default,
					Tick   = default,
				});
				OpaqueMaterial.SetVector(OpaqueProperty.Tiling, (Vector2)data.tiling);
				OpaqueMaterial.SetVector(OpaqueProperty.Offset, (Vector2)data.offset);
			}
			var properties = Resources.FindObjectsOfTypeAll<OpaquePropertyAuthoring>();
			foreach (var property in properties) property.UpdateProperty();
		}
	}
	#endif

	public static AtlasData GetOpaqueData(OpaqueHash hash, bool useFallback = true) {
		return GetOpaqueData(OpaqueHashMap, hash, useFallback);
	}

	static AtlasData GetOpaqueData(
		HashMap<uint, AtlasData> OpaqueHashMap,
		OpaqueHash hash, bool useFallback = true) {
		var temp = new OpaqueHash() {
			Opaque = hash.Opaque,
		};
		if (OpaqueHashMap.TryGetValue(temp.Key, out var totalFrame)) {
			uint time = (uint)totalFrame.scale.x;
			uint tick = hash.Tick;
			temp.Tick = (time == 0u) ? 0u : (time <= tick) ? (tick % time) : tick;
		}
		if (OpaqueHashMap.TryGetValue(temp.Key, out var finalData)) {
			return finalData;
		} else if (useFallback) {
			if (hash.Opaque != default) {
				return GetOpaqueData(OpaqueHashMap, new OpaqueHash() {
					Opaque = default,
					Tick   = default,
				}, useFallback);
			} else return new AtlasData() {
				scale  = new(1f, 1f),
				pivot  = new(0f, 0f),
				tiling = new(1f, 1f),
				offset = new(0f, 0f),
			};
		} else return default;
	}



	// Shadow Methods

	// Variants
	// {Sprite}.png
	// {Sprite}_{Index}_{Millisecond}.png
	// {Sprite}_{Direction}_{Index}_{Millisecond}.png
	// {Sprite}_{Motion}_{Direction}_{Index}_{Millisecond}.png

	#if UNITY_EDITOR
	static void ClearShadowData() {
		ShadowHashMap.Clear();
	}

	static void LoadShadowData() {
		ClearShadowData();
		var atlasMap = GetShadowAtlasMap();
		if (atlasMap == null) {
			Debug.Log($"Shadow Atlas Map is missing at {ShadowAtlasMapPath}");
		} else {
			atlasMap.GenerateAtlasMap();
			foreach (var (name, data) in atlasMap) {
				var sprite      = default(Sprite);
				var motion      = default(Motion);
				var direction   = 0u;
				var index       = 0u;
				var millisecond = 0u;

				var match = true;
				var split = name.Split('_');
				if (1 <= split.Length) match = match && Enum.TryParse(split[ 0], out sprite);
				if (5 <= split.Length) match = match && Enum.TryParse(split[ 1], out motion);
				if (4 <= split.Length) match = match && uint.TryParse(split[^3], out direction);
				if (3 <= split.Length) match = match && uint.TryParse(split[^2], out index);
				if (3 <= split.Length) match = match && uint.TryParse(split[^1], out millisecond);
				if (!match) continue;

				var temp = new ShadowHash() {
					Sprite = sprite,
					Motion = motion,
				};
				ShadowHashMap.TryAdd(temp.Key, new AtlasData());
				var numDirections = ShadowHashMap[temp.Key];
				var n = (uint)Mathf.Max(numDirections.scale.x, direction + 1f);
				numDirections.scale.x = n;
				ShadowHashMap[temp.Key] = numDirections;

				temp.Direction = direction;
				ShadowHashMap.TryAdd(temp.Key, new AtlasData());
				var totalFrame = ShadowHashMap[temp.Key];
				var a = totalFrame.scale.x;
				var b = a + millisecond * SampleRate * 0.001f;
				totalFrame.scale.x = b;
				ShadowHashMap[temp.Key] = totalFrame;

				for (var frame = a; frame <= b; frame++) {
					temp.Tick = (uint)frame;
					ShadowHashMap.TryAdd(temp.Key, data);
				}
			}
			ShadowHashMap.TrimExcess();
			if (ShadowMaterial != null) {
				var data = GetShadowData(new ShadowHash() {
					Sprite    = default,
					Motion    = default,
					Direction = default,
					Tick      = default,
				});
				ShadowMaterial.SetVector(ShadowProperty.Scale,  (Vector2)data.scale);
				ShadowMaterial.SetVector(ShadowProperty.Pivot,  (Vector2)data.pivot);
				ShadowMaterial.SetVector(ShadowProperty.Tiling, (Vector2)data.tiling);
				ShadowMaterial.SetVector(ShadowProperty.Offset, (Vector2)data.offset);
			}
			var properties = Resources.FindObjectsOfTypeAll<ShadowPropertyAuthoring>();
			foreach (var property in properties) property.UpdateProperty();
		}
	}
	#endif

	public static AtlasData GetShadowData(ShadowHash hash, bool useFallback = true) {
		return GetShadowData(ShadowHashMap, hash, useFallback);
	}

	static AtlasData GetShadowData(
		HashMap<uint, AtlasData> ShadowHashMap,
		ShadowHash hash, bool useFallback = true) {
		var temp = new ShadowHash() {
			Sprite = hash.Sprite,
			Motion = hash.Motion,
			Flip   = hash.Flip,
		};
		if (ShadowHashMap.TryGetValue(temp.Key, out var numDirections)) {
			var maxDirections = 1u;
			var yaw = -hash.ObjectYaw;
			switch ((uint)numDirections.scale.x) {
				case >= 01u and <= 02u: maxDirections = 02u; yaw += 00.00f; break;
				case >= 03u and <= 04u: maxDirections = 04u; yaw += 45.00f; break;
				case >= 05u and <= 08u: maxDirections = 08u; yaw += 22.50f; break;
				case >= 09u and <= 16u: maxDirections = 16u; yaw += 11.25f; break;
			}
			yaw = math.clamp(yaw - math.floor(yaw * 0.00277778f) * 360f, 0f, 360f);
			var direction = (uint)(yaw * maxDirections * 0.00277778f);
			direction = math.clamp(direction, 0u, maxDirections - 1u);
			if ((uint)numDirections.scale.x <= direction) {
				temp.Flip.x = !temp.Flip.x;
				switch (maxDirections) {
					case 02u: direction = 01u - direction; break;
					case 04u: direction = 04u - direction; break;
					case 08u: direction = 08u - direction; break;
					case 16u: direction = 16u - direction; break;
				}
			}
			temp.Direction = direction;
		}
		if (ShadowHashMap.TryGetValue(temp.Key, out var totalFrame)) {
			uint time = (uint)totalFrame.scale.x;
			uint tick = hash.Tick;
			temp.Tick = (time == 0u) ? 0u : (time <= tick) ? (tick % time) : tick;
		}
		if (ShadowHashMap.TryGetValue(temp.Key, out var finalData)) {
			if (temp.Flip.x) finalData.pivot.x *= -1f;
			if (temp.Flip.y) finalData.pivot.y *= -1f;
			if (temp.Flip.x) finalData.offset.x -= finalData.tiling.x *= -1f;
			if (temp.Flip.y) finalData.offset.y -= finalData.tiling.y *= -1f;
			return finalData;
		} else if (useFallback) {
			if (hash.Tick != default) {
				return GetShadowData(ShadowHashMap, new ShadowHash() {
					Sprite = default,
					Motion = default,
					Tick   = default,
				}, useFallback);
			} else return new AtlasData() {
				scale  = new(1f, 1f),
				pivot  = new(0f, 0f),
				tiling = new(1f, 1f),
				offset = new(0f, 0f),
			};
		} else return default;
	}



	// Sprite Methods

	// Variants
	// {Sprite}.png
	// {Sprite}_{Index}_{Millisecond}.png
	// {Sprite}_{Direction}_{Index}_{Millisecond}.png
	// {Sprite}_{Motion}_{Direction}_{Index}_{Millisecond}.png

	#if UNITY_EDITOR
	static void ClearSpriteData() {
		SpriteHashMap.Clear();
	}

	static void LoadSpriteData() {
		ClearSpriteData();
		var atlasMap = GetSpriteAtlasMap();
		if (atlasMap == null) {
			Debug.Log($"Sprite Atlas Map is missing at {SpriteAtlasMapPath}");
		} else {
			atlasMap.GenerateAtlasMap();
			foreach (var (name, data) in atlasMap) {
				var sprite      = default(Sprite);
				var motion      = default(Motion);
				var direction   = 0u;
				var index       = 0u;
				var millisecond = 0u;

				var match = true;
				var split = name.Split('_');
				if (1 <= split.Length) match = match && Enum.TryParse(split[ 0], out sprite);
				if (5 <= split.Length) match = match && Enum.TryParse(split[ 1], out motion);
				if (4 <= split.Length) match = match && uint.TryParse(split[^3], out direction);
				if (3 <= split.Length) match = match && uint.TryParse(split[^2], out index);
				if (3 <= split.Length) match = match && uint.TryParse(split[^1], out millisecond);
				if (!match) continue;

				var temp = new SpriteHash() {
					Sprite = sprite,
					Motion = motion,
				};
				SpriteHashMap.TryAdd(temp.Key, new AtlasData());
				var numDirections = SpriteHashMap[temp.Key];
				var n = (uint)Mathf.Max(numDirections.scale.x, direction + 1f);
				numDirections.scale.x = n;
				SpriteHashMap[temp.Key] = numDirections;

				temp.Direction = direction;
				SpriteHashMap.TryAdd(temp.Key, new AtlasData());
				var totalFrame = SpriteHashMap[temp.Key];
				var a = totalFrame.scale.x;
				var b = a + millisecond * SampleRate * 0.001f;
				totalFrame.scale.x = b;
				SpriteHashMap[temp.Key] = totalFrame;

				for (var frame = a; frame <= b; frame++) {
					temp.Tick = (uint)frame;
					SpriteHashMap.TryAdd(temp.Key, data);
				}
			}
			SpriteHashMap.TrimExcess();
			if (SpriteMaterial != null) {
				var data = GetSpriteData(new SpriteHash() {
					Sprite    = default,
					Motion    = default,
					Direction = default,
					Tick      = default,
				});
				SpriteMaterial.SetVector(SpriteProperty.Scale,  (Vector2)data.scale);
				SpriteMaterial.SetVector(SpriteProperty.Pivot,  (Vector2)data.pivot);
				SpriteMaterial.SetVector(SpriteProperty.Tiling, (Vector2)data.tiling);
				SpriteMaterial.SetVector(SpriteProperty.Offset, (Vector2)data.offset);
			}
			var properties = Resources.FindObjectsOfTypeAll<SpritePropertyAuthoring>();
			foreach (var property in properties) property.UpdateProperty();
		}
	}
	#endif

	public static AtlasData GetSpriteData(SpriteHash hash, bool useFallback = true) {
		return GetSpriteData(SpriteHashMap, hash, useFallback);
	}

	static AtlasData GetSpriteData(
		HashMap<uint, AtlasData> SpriteHashMap,
		SpriteHash hash, bool useFallback = true) {
		var temp = new SpriteHash() {
			Sprite = hash.Sprite,
			Motion = hash.Motion,
			Flip   = hash.Flip,
		};
		if (SpriteHashMap.TryGetValue(temp.Key, out var numDirections)) {
			var maxDirections = 1u;
			var yaw = -hash.ObjectYaw;
			switch ((uint)numDirections.scale.x) {
				case >= 01u and <= 02u: maxDirections = 02u; yaw += 00.00f; break;
				case >= 03u and <= 04u: maxDirections = 04u; yaw += 45.00f; break;
				case >= 05u and <= 08u: maxDirections = 08u; yaw += 22.50f; break;
				case >= 09u and <= 16u: maxDirections = 16u; yaw += 11.25f; break;
			}
			yaw = math.clamp(yaw - math.floor(yaw * 0.00277778f) * 360f, 0f, 360f);
			var direction = (uint)(yaw * maxDirections * 0.00277778f);
			direction = math.clamp(direction, 0u, maxDirections - 1u);
			if ((uint)numDirections.scale.x <= direction) {
				temp.Flip.x = !temp.Flip.x;
				switch (maxDirections) {
					case 02u: direction = 01u - direction; break;
					case 04u: direction = 04u - direction; break;
					case 08u: direction = 08u - direction; break;
					case 16u: direction = 16u - direction; break;
				}
			}
			temp.Direction = direction;
		}
		if (SpriteHashMap.TryGetValue(temp.Key, out var totalFrame)) {
			uint time = (uint)totalFrame.scale.x;
			uint tick = hash.Tick;
			temp.Tick = (time == 0u) ? 0u : (time <= tick) ? (tick % time) : tick;
		}
		if (SpriteHashMap.TryGetValue(temp.Key, out var finalData)) {
			if (temp.Flip.x) finalData.pivot.x *= -1f;
			if (temp.Flip.y) finalData.pivot.y *= -1f;
			if (temp.Flip.x) finalData.offset.x -= finalData.tiling.x *= -1f;
			if (temp.Flip.y) finalData.offset.y -= finalData.tiling.y *= -1f;
			return finalData;
		} else if (useFallback) {
			if (hash.Sprite != default) {
				return GetSpriteData(SpriteHashMap, new SpriteHash() {
					Sprite    = default,
					Motion    = default,
					Direction = default,
					Tick      = default,
				}, useFallback);
			} else return new AtlasData() {
				scale  = new(1f, 1f),
				pivot  = new(0f, 0f),
				tiling = new(1f, 1f),
				offset = new(0f, 0f),
			};
		} else return default;
	}



	// Lifecycle

	protected override void Awake() {
		base.Awake();
		CanvasNativeHashMap = new(CanvasHashMap.Count, Allocator.Persistent);
		OpaqueNativeHashMap = new(OpaqueHashMap.Count, Allocator.Persistent);
		ShadowNativeHashMap = new(ShadowHashMap.Count, Allocator.Persistent);
		SpriteNativeHashMap = new(SpriteHashMap.Count, Allocator.Persistent);
		foreach (var (key, data) in CanvasHashMap) CanvasNativeHashMap.Add(key, data);
		foreach (var (key, data) in OpaqueHashMap) OpaqueNativeHashMap.Add(key, data);
		foreach (var (key, data) in ShadowHashMap) ShadowNativeHashMap.Add(key, data);
		foreach (var (key, data) in SpriteHashMap) SpriteNativeHashMap.Add(key, data);
	}

	protected override void OnDestroy() {
		if (CanvasNativeHashMap.IsCreated) CanvasNativeHashMap.Dispose();
		if (OpaqueNativeHashMap.IsCreated) OpaqueNativeHashMap.Dispose();
		if (ShadowNativeHashMap.IsCreated) ShadowNativeHashMap.Dispose();
		if (SpriteNativeHashMap.IsCreated) SpriteNativeHashMap.Dispose();
		base.OnDestroy();
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Draw Manager System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(DOTSPresentationSystemGroup), OrderFirst = true)]
public partial class DrawManagerSystem : SystemBase {

	protected override void OnUpdate() {
		Dependency = new DrawCanvasJob {
			CanvasHashMap = DrawManager.CanvasHashMapReadOnly,
		}.ScheduleParallel(Dependency);
		Dependency = new DrawOpaqueJob {
			OpaqueHashMap = DrawManager.OpaqueHashMapReadOnly,
		}.ScheduleParallel(Dependency);
		Dependency = new DrawShadowJob {
			ShadowHashMap = DrawManager.ShadowHashMapReadOnly,
		}.ScheduleParallel(Dependency);
		Dependency = new DrawSpriteJob {
			SpriteHashMap = DrawManager.SpriteHashMapReadOnly,
		}.ScheduleParallel(Dependency);
	}
}

[BurstCompile]
partial struct DrawCanvasJob : IJobEntity {
	[ReadOnly] public NativeHashMap<uint, AtlasData>.ReadOnly CanvasHashMap;

	public void Execute(
		in CanvasHash hash,
		ref CanvasPropertyScale scale,
		ref CanvasPropertyPivot pivot,
		ref CanvasPropertyTiling tiling,
		ref CanvasPropertyOffset offset) {

		var data = GetCanvasData(CanvasHashMap, hash);
		if (math.any(scale.Value  != data.scale))  scale.Value  = data.scale;
		if (math.any(pivot.Value  != data.pivot))  pivot.Value  = data.pivot;
		if (math.any(tiling.Value != data.tiling)) tiling.Value = data.tiling;
		if (math.any(offset.Value != data.offset)) offset.Value = data.offset;
	}

	public static AtlasData GetCanvasData(
		NativeHashMap<uint, AtlasData>.ReadOnly CanvasHashMap,
		CanvasHash hash, bool useFallback = true) {
		var temp = new CanvasHash() {
			Canvas = hash.Canvas,
			Flip    = hash.Flip,
		};
		if (CanvasHashMap.TryGetValue(temp.Key, out var totalFrame)) {
			uint time = (uint)totalFrame.scale.x;
			uint tick = hash.Tick;
			temp.Tick = (time == 0u) ? 0u : (time <= tick) ? (tick % time) : tick;
		}
		if (CanvasHashMap.TryGetValue(temp.Key, out var finalData)) {
			if (temp.Flip.x) finalData.pivot.x *= -1f;
			if (temp.Flip.y) finalData.pivot.y *= -1f;
			if (temp.Flip.x) finalData.offset.x -= finalData.tiling.x *= -1f;
			if (temp.Flip.y) finalData.offset.y -= finalData.tiling.y *= -1f;
			return finalData;
		} else if (useFallback) {
			if (hash.Canvas != default) {
				return GetCanvasData(CanvasHashMap, new CanvasHash() {
					Canvas = default,
					Tick   = default,
				}, useFallback);
			} else return new AtlasData() {
				scale  = new(1f, 1f),
				pivot  = new(0f, 0f),
				tiling = new(1f, 1f),
				offset = new(0f, 0f),
			};
		} else return default;
	}
}

[BurstCompile]
partial struct DrawOpaqueJob : IJobEntity {
	[ReadOnly] public NativeHashMap<uint, AtlasData>.ReadOnly OpaqueHashMap;

	public void Execute(
		in OpaqueHash hash,
		ref OpaquePropertyTiling tiling,
		ref OpaquePropertyOffset offset) {

		var data = GetOpaqueData(OpaqueHashMap, hash);
		if (math.any(tiling.Value != data.tiling)) tiling.Value = data.tiling;
		if (math.any(offset.Value != data.offset)) offset.Value = data.offset;
	}

	public static AtlasData GetOpaqueData(
		NativeHashMap<uint, AtlasData>.ReadOnly OpaqueHashMap,
		OpaqueHash hash, bool useFallback = true) {
		var temp = new OpaqueHash() {
			Opaque = hash.Opaque,
		};
		if (OpaqueHashMap.TryGetValue(temp.Key, out var totalFrame)) {
			var t = (uint)totalFrame.scale.x;
			temp.Tick = (t == 0u) ? 0u : hash.Tick % t;
		}
		if (OpaqueHashMap.TryGetValue(temp.Key, out var finalData)) {
			return finalData;
		} else if (useFallback) {
			if (hash.Opaque != default) {
				return GetOpaqueData(OpaqueHashMap, new OpaqueHash() {
					Opaque = default,
					Tick   = default,
				}, useFallback);
			} else return new AtlasData() {
				scale  = new(1f, 1f),
				pivot  = new(0f, 0f),
				tiling = new(1f, 1f),
				offset = new(0f, 0f),
			};
		} else return default;
	}
}

[BurstCompile]
partial struct DrawShadowJob : IJobEntity {
	[ReadOnly] public NativeHashMap<uint, AtlasData>.ReadOnly ShadowHashMap;

	public void Execute(
		in ShadowHash hash,
		ref ShadowPropertyScale scale,
		ref ShadowPropertyPivot pivot,
		ref ShadowPropertyTiling tiling,
		ref ShadowPropertyOffset offset) {

		var data = GetShadowData(ShadowHashMap, hash);
		if (math.any(scale.Value  != data.scale))  scale.Value  = data.scale;
		if (math.any(pivot.Value  != data.pivot))  pivot.Value  = data.pivot;
		if (math.any(tiling.Value != data.tiling)) tiling.Value = data.tiling;
		if (math.any(offset.Value != data.offset)) offset.Value = data.offset;
	}

	public static AtlasData GetShadowData(
		NativeHashMap<uint, AtlasData>.ReadOnly ShadowHashMap,
		ShadowHash hash, bool useFallback = true) {
		var temp = new ShadowHash() {
			Sprite = hash.Sprite,
			Motion = hash.Motion,
			Flip   = hash.Flip,
		};
		if (ShadowHashMap.TryGetValue(temp.Key, out var numDirections)) {
			var maxDirections = 1u;
			var yaw = -hash.ObjectYaw;
			switch ((uint)numDirections.scale.x) {
				case >= 01u and <= 02u: maxDirections = 02u; yaw += 00.00f; break;
				case >= 03u and <= 04u: maxDirections = 04u; yaw += 45.00f; break;
				case >= 05u and <= 08u: maxDirections = 08u; yaw += 22.50f; break;
				case >= 09u and <= 16u: maxDirections = 16u; yaw += 11.25f; break;
			}
			yaw = math.clamp(yaw - math.floor(yaw * 0.00277778f) * 360f, 0f, 360f);
			var direction = (uint)(yaw * maxDirections * 0.00277778f);
			direction = math.clamp(direction, 0u, maxDirections - 1u);
			if ((uint)numDirections.scale.x <= direction) {
				temp.Flip.x = !temp.Flip.x;
				switch (maxDirections) {
					case 02u: direction = 01u - direction; break;
					case 04u: direction = 04u - direction; break;
					case 08u: direction = 08u - direction; break;
					case 16u: direction = 16u - direction; break;
				}
			}
			temp.Direction = direction;
		}
		if (ShadowHashMap.TryGetValue(temp.Key, out var totalFrame)) {
			uint time = (uint)totalFrame.scale.x;
			uint tick = hash.Tick;
			temp.Tick = (time == 0u) ? 0u : (time <= tick) ? (tick % time) : tick;
		}
		if (ShadowHashMap.TryGetValue(temp.Key, out var finalData)) {
			if (temp.Flip.x) finalData.pivot.x *= -1f;
			if (temp.Flip.y) finalData.pivot.y *= -1f;
			if (temp.Flip.x) finalData.offset.x -= finalData.tiling.x *= -1f;
			if (temp.Flip.y) finalData.offset.y -= finalData.tiling.y *= -1f;
			return finalData;
		} else if (useFallback) {
			if (hash.Tick != default) {
				return GetShadowData(ShadowHashMap, new ShadowHash() {
					Sprite = default,
					Motion = default,
					Tick   = default,
				}, useFallback);
			} else return new AtlasData() {
				scale  = new(1f, 1f),
				pivot  = new(0f, 0f),
				tiling = new(1f, 1f),
				offset = new(0f, 0f),
			};
		} else return default;
	}
}

[BurstCompile]
partial struct DrawSpriteJob : IJobEntity {
	[ReadOnly] public NativeHashMap<uint, AtlasData>.ReadOnly SpriteHashMap;

	public void Execute(
		in SpriteHash hash,
		ref SpritePropertyScale scale,
		ref SpritePropertyPivot pivot,
		ref SpritePropertyTiling tiling,
		ref SpritePropertyOffset offset) {

		var data = GetSpriteData(SpriteHashMap, hash);
		if (math.any(scale.Value  != data.scale))  scale.Value  = data.scale;
		if (math.any(pivot.Value  != data.pivot))  pivot.Value  = data.pivot;
		if (math.any(tiling.Value != data.tiling)) tiling.Value = data.tiling;
		if (math.any(offset.Value != data.offset)) offset.Value = data.offset;
	}

	public static AtlasData GetSpriteData(
		NativeHashMap<uint, AtlasData>.ReadOnly SpriteHashMap,
		SpriteHash hash, bool useFallback = true) {
		var temp = new SpriteHash() {
			Sprite = hash.Sprite,
			Motion = hash.Motion,
			Flip   = hash.Flip,
		};
		if (SpriteHashMap.TryGetValue(temp.Key, out var numDirections)) {
			var maxDirections = 1u;
			var yaw = -hash.ObjectYaw;
			switch ((uint)numDirections.scale.x) {
				case >= 01u and <= 02u: maxDirections = 02u; yaw += 00.00f; break;
				case >= 03u and <= 04u: maxDirections = 04u; yaw += 45.00f; break;
				case >= 05u and <= 08u: maxDirections = 08u; yaw += 22.50f; break;
				case >= 09u and <= 16u: maxDirections = 16u; yaw += 11.25f; break;
			}
			yaw = math.clamp(yaw - math.floor(yaw * 0.00277778f) * 360f, 0f, 360f);
			var direction = (uint)(yaw * maxDirections * 0.00277778f);
			direction = math.clamp(direction, 0u, maxDirections - 1u);
			if ((uint)numDirections.scale.x <= direction) {
				temp.Flip.x = !temp.Flip.x;
				switch (maxDirections) {
					case 02u: direction = 01u - direction; break;
					case 04u: direction = 04u - direction; break;
					case 08u: direction = 08u - direction; break;
					case 16u: direction = 16u - direction; break;
				}
			}
			temp.Direction = direction;
		}
		if (SpriteHashMap.TryGetValue(temp.Key, out var totalFrame)) {
			uint time = (uint)totalFrame.scale.x;
			uint tick = hash.Tick;
			temp.Tick = (time == 0u) ? 0u : (time <= tick) ? (tick % time) : tick;
		}
		if (SpriteHashMap.TryGetValue(temp.Key, out var finalData)) {
			if (temp.Flip.x) finalData.pivot.x *= -1f;
			if (temp.Flip.y) finalData.pivot.y *= -1f;
			if (temp.Flip.x) finalData.offset.x -= finalData.tiling.x *= -1f;
			if (temp.Flip.y) finalData.offset.y -= finalData.tiling.y *= -1f;
			return finalData;
		} else if (useFallback) {
			if (hash.Sprite != default) {
				return GetSpriteData(SpriteHashMap, new SpriteHash() {
					Sprite    = default,
					Motion    = default,
					Direction = default,
					Tick      = default,
				}, useFallback);
			} else return new AtlasData() {
				scale  = new(1f, 1f),
				pivot  = new(0f, 0f),
				tiling = new(1f, 1f),
				offset = new(0f, 0f),
			};
		} else return default;
	}
}

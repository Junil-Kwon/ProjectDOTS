using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Material Editor
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[CreateAssetMenu(fileName = "MaterialEditor", menuName = "Scriptable Objects/Material Editor")]
public class MaterialEditor : ScriptableObject {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(MaterialEditor))]
	class MaterialEditorSOEditor : EditorExtensions {
		MaterialEditor I => target as MaterialEditor;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Material", EditorStyles.boldLabel);
			I.Material = ObjectField("Material", I.Material);
			Space();

			LabelField("Material Texture", EditorStyles.boldLabel);
			I.TextureProperty = TextField("Texture Property", I.TextureProperty);
			I.SourcePath = TextField("Source Path", I.SourcePath);
			var sources = LoadAssets<Texture2D>(I.SourcePath);
			if (0 < sources.Length) {
				BeginHorizontal();
				PrefixLabel($"Target Texture");
				int index = 0;
				var name = TextField(I.TargetTexture);
				var options = new string[sources.Length];
				for (int i = 0; i < sources.Length; i++) {
					options[i] = sources[i].name;
					if (options[i] == name) index = i;
				}
				index = EditorGUILayout.Popup(index, options);
				I.Material.SetTexture(options[index], sources[index]);
				I.TargetTexture = options[index];
				EndHorizontal();
			}
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] Material m_Material;
	[SerializeField] string m_TextureProperty = "_MainTex";
	[SerializeField] string m_SourcePath = "Assets/Textures";
	[SerializeField] string m_TargetTexture;



	// Properties

	Material Material {
		get => m_Material;
		set => m_Material = value;
	}
	string SourcePath {
		get => m_SourcePath;
		set => m_SourcePath = value;
	}
	string TextureProperty {
		get => m_TextureProperty;
		set => m_TextureProperty = value;
	}
	string TargetTexture {
		get => m_TargetTexture;
		set => m_TargetTexture = value;
	}



	// Utility Methods

	#if UNITY_EDITOR
	static Texture2D CreateTexture(int width, int height, Color color) {
		var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
		var textureData = texture.GetRawTextureData<Color32>();
		for (int i = 0; i < textureData.Length; i++) textureData[i] = color;
		texture.Apply(false);
		return texture;
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
}

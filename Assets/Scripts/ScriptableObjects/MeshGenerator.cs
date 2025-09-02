using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Mesh Generator
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[CreateAssetMenu(fileName = "MeshGenerator", menuName = "Scriptable Objects/Mesh Generator")]
public class MeshGenerator : ScriptableObject {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(MeshGenerator))]
	class MeshGeneratorSOEditor : EditorExtensions {
		MeshGenerator I => target as MeshGenerator;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Mesh Generator", EditorStyles.boldLabel);
			I.TargetPath = TextField("Target Path", I.TargetPath);
			BeginHorizontal();
			PrefixLabel("Generate Mesh");
			if (Button("Generate")) I.GenerateMesh(I.TargetPath);
			EndHorizontal();
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] string m_TargetPath = "Assets/Mesh.asset";



	// Properties

	string TargetPath {
		get => m_TargetPath;
		set => m_TargetPath = value;
	}



	// Methods

	Mesh GenerateQuad() => new() {
		name = "Quad",
		vertices = new Vector3[] {
			new(-0.5f, +0.0f, -0.5f), new(+0.5f, +0.0f, -0.5f),
			new(-0.5f, +0.0f, +0.5f), new(+0.5f, +0.0f, +0.5f),
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

	#if UNITY_EDITOR
	void GenerateMesh(string path) {
		var mesh = GenerateQuad();
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		AssetDatabase.CreateAsset(mesh, path);
		AssetDatabase.SaveAssets(); 
		AssetDatabase.Refresh();
	}
	#endif
}

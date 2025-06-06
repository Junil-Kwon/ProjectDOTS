using UnityEngine;

using Unity.Sentis;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// AI Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/AI Manager")]
public sealed class AIManager : MonoSingleton<AIManager> {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(AIManager))]
		class AIManagerEditor : EditorExtensions {
			AIManager I => target as AIManager;
			public override void OnInspectorGUI() {
				Begin("AI Manager");
				
				LabelField("Model", EditorStyles.boldLabel);
				ModelAsset = ObjectField("Model Asset", ModelAsset);
				Space();

				End();
			}
		}
	#endif



	// Fields

	ModelAsset m_ModelAsset;



	// Properties

	static ModelAsset ModelAsset {
		get => Instance.m_ModelAsset;
		set => Instance.m_ModelAsset = value;
	}



	// Lifecycle

	void Start() {
		if (ModelAsset) {
			var model = ModelLoader.Load(ModelAsset);
			var graph = new FunctionalGraph();
			var inputs = graph.AddInputs(model);
			var outputs = Functional.Forward(model, inputs);
		}
	}

}

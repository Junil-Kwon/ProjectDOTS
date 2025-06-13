using UnityEngine;
using System.Text;

using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Debug Canvas
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI/Debug Canvas")]
public class DebugCanvas : BaseCanvas {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(DebugCanvas))]
	class DebugCanvasEditor : EditorExtensions {
		DebugCanvas I => target as DebugCanvas;
		public override void OnInspectorGUI() {
			Begin("Debug Canvas");

			if (I.Raycaster && I.Raycaster.enabled) {
				LabelField("Selected", EditorStyles.boldLabel);
				I.FirstSelected = ObjectField("First Selected", I.FirstSelected);
				Space();
			}
			LabelField("Content", EditorStyles.boldLabel);
			I.TextUGUI = ObjectField("TextUGUI", I.TextUGUI);
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] TextMeshProUGUI m_TextUGUI;
	StringBuilder m_StringBuilder = new();

	float m_DeltaTime;



	// Properties

	public TextMeshProUGUI TextUGUI {
		get => m_TextUGUI;
		set => m_TextUGUI = value;
	}
	StringBuilder StringBuilder {
		get => m_StringBuilder;
		set => m_StringBuilder = value;
	}

	float DeltaTime {
		get => m_DeltaTime;
		set => m_DeltaTime = value;
	}



	// Lifecycle

	void LateUpdate() {
		StringBuilder.Clear();

		DeltaTime += (Time.unscaledDeltaTime - DeltaTime) * 0.1f;
		StringBuilder.Append("FPS: ");
		StringBuilder.Append((1f / DeltaTime).ToString("F1"));
		StringBuilder.AppendLine();

		StringBuilder.Append("Entities: ");
		StringBuilder.Append(GameManager.NumCreatures);
		StringBuilder.AppendLine();

		TextUGUI.text = StringBuilder.ToString();
	}
}

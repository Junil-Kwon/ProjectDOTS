using UnityEngine;

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

			if (I.Raycaster) {
				LabelField("Selected", EditorStyles.boldLabel);
				I.FirstSelected = ObjectField("First Selected", I.FirstSelected);
				Space();
			}
			LabelField("Elements", EditorStyles.boldLabel);
			I.FrameRateText = ObjectField("Frame Rate Text", I.FrameRateText);
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] TextMeshProUGUI m_FrameRateText;
	float m_Delta;



	// Properties

	public TextMeshProUGUI FrameRateText {
		get => m_FrameRateText;
		set => m_FrameRateText = value;
	}
	float Delta {
		get => m_Delta;
		set => m_Delta = value;
	}



	// Lifecycle

	void Update() {
		Delta += (Time.unscaledDeltaTime - Delta) * 0.1f;
		FrameRateText.text = string.Format("FPS: {0:0.}", 1f / Delta);
	}
}

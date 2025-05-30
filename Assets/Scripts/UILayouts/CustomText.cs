using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

using TMPro;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Custom Text
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI/Custom Text")]
public sealed class CustomText : Selectable {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(CustomText))]
		class CustomTextEditor : SelectableEditorExtensions {
			CustomText I => target as CustomText;
			public override void OnInspectorGUI() {
				Begin("Custom Text");

				LabelField("Selectable", EditorStyles.boldLabel);
				base.OnInspectorGUI();
				Space();
				LabelField("Text Text", EditorStyles.boldLabel);
				I.TextUGUI = ObjectField("Text UGUI", I.TextUGUI);
				if (I.TextUGUI) {
					I.Text = TextField("Text", I.Text);
					BeginHorizontal();
					PrefixLabel("Alignment");
					if (Button("Left"  )) I.TextUGUI.alignment = TextAlignmentOptions.Left;
					if (Button("Center")) I.TextUGUI.alignment = TextAlignmentOptions.Center;
					if (Button("Right" )) I.TextUGUI.alignment = TextAlignmentOptions.Right;
					EndHorizontal();
				}
				Space();
				LabelField("Text Event", EditorStyles.boldLabel);
				PropertyField("m_OnStateUpdated");
				Space();

				End();
			}
		}
	#endif



	// Fields

	[SerializeField] TextMeshProUGUI m_TextUGUI;

	[SerializeField] UnityEvent<CustomText> m_OnStateUpdated = new();



	// Properties

	RectTransform Transform => transform as RectTransform;

	TextMeshProUGUI TextUGUI {
		get => m_TextUGUI;
		set => m_TextUGUI = value;
	}
	public string Text {
		get => TextUGUI.text;
		set => TextUGUI.text = value;
	}

	public UnityEvent<CustomText> OnStateUpdated => m_OnStateUpdated;



	// Methods

	public void Refresh() {
		OnStateUpdated.Invoke(this);
	}



	// Lifecycle

	protected override void OnEnable() {
		base.OnEnable();
		Refresh();
	}
}

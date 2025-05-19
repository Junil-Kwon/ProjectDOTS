using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━

public enum UI : ushort {
	None,
	BarM,
	BarL,
	BarR,
	Bar,
	Unity,
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// UI Drawer Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/UI Drawer")]
public class UIDrawerAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(UIDrawerAuthoring))]
		class UIDrawerAuthoringEditor : EditorExtensions {
			UIDrawerAuthoring I => target as UIDrawerAuthoring;
			public override void OnInspectorGUI() {
				Begin("UI Drawer Authoring");

				var status = PrefabUtility.GetPrefabInstanceStatus(I.gameObject);
				var flag   = !Application.isPlaying && status != PrefabInstanceStatus.Connected;
				if (flag) {
					LabelField("Transform", EditorStyles.boldLabel);
					I.Position = Vector3Field("Position", I.Position);
					I.Scale    = Vector2Field("Scale",    I.Scale);
					I.Pivot    = Vector2Field("Pivot",    I.Pivot);
					Space();
					LabelField("UI", EditorStyles.boldLabel);
					BeginHorizontal();
					PrefixLabel("UI");
					I.UIString = TextField(I.UIString);
					I.UI       = EnumField(I.UI);
					EndHorizontal();
					I.Offset    = FloatField("Offset",     I.Offset);
					I.BaseColor = ColorField("Base Color", I.BaseColor);
					I.Flip      = Toggle2   ("Flip",       I.Flip);
					Space();
				}
				End();
			}
		}
	#endif



	// Fields

	[SerializeField] Vector3 m_Position;
	[SerializeField] Vector2 m_Scale     = Vector2.one;
	[SerializeField] Vector2 m_Pivot;

	[SerializeField] string  m_UI;
	[SerializeField] float   m_Offset;
	[SerializeField] Color32 m_BaseColor = Color.white;
	[SerializeField] bool2   m_Flip;



	// Properties

	public Vector3 Position {
		get => m_Position;
		set => m_Position = value;
	}
	public Vector2 Scale {
		get => m_Scale;
		set => m_Scale = value;
	}
	public Vector2 Pivot {
		get => m_Pivot;
		set => m_Pivot = value;
	}

	public string UIString {
		get => m_UI;
		set => m_UI = value;
	}
	public UI UI {
		get => System.Enum.TryParse(m_UI, out UI ui) ? ui : 0;
		set => m_UI = value.ToString();
	}
	public float Offset {
		get => m_Offset;
		set => m_Offset = value;
	}
	public Color32 BaseColor {
		get => m_BaseColor;
		set => m_BaseColor = value;
	}
	public bool2 Flip {
		get => m_Flip;
		set => m_Flip = value;
	}



	// Baker

	class Baker : Baker<UIDrawerAuthoring> {
		public override void Bake(UIDrawerAuthoring authoring) {
			var components = GetComponents<UIDrawerAuthoring>();
			if (components[0] != authoring) return;
			var entity = GetEntity(TransformUsageFlags.Renderable);
			var buffer = AddBuffer<UIDrawer>(entity);
			foreach (var component in components) buffer.Add(new() {

				Position  = component.Position,
				Scale     = component.Scale,
				Pivot     = component.Pivot,

				UI        = component.UI,
				Offset    = component.Offset,
				BaseColor = component.BaseColor,
				Flip      = component.Flip,

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// UI Drawer
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[InternalBufferCapacity(7)]
public struct UIDrawer : IBufferElementData {

	public float3 Position;
	public float2 Scale;
	public float2 Pivot;

	public UI    UI;
	public float Offset;
	public color BaseColor;
	public bool2 Flip;
}

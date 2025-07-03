using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEditor;
#endif



// UI Images

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
			if (status != PrefabInstanceStatus.Connected && !Application.isPlaying) {
				I.Position = Vector3Field("Position", I.Position);
				I.Scale    = Vector2Field("Scale",    I.Scale);
				I.Pivot    = Vector2Field("Pivot",    I.Pivot);
				Space();
				BeginHorizontal();
				PrefixLabel("UI");
				I.UIString = TextField(I.UIString);
				I.UI       = EnumField(I.UI);
				EndHorizontal();
				I.Offset    = FloatField("Offset",     I.Offset);
				I.BaseColor = ColorField("Base Color", I.BaseColor);
				BeginHorizontal();
				BeginDisabledGroup(I.FlipRandom);
				I.Flip = Toggle2("Flip", I.Flip);
				EndDisabledGroup();
				I.FlipRandom = ToggleLeft("Random", I.FlipRandom);
				EndHorizontal();
			}
			End();
		}
	}
	#endif



	// Fields

	[SerializeField] Vector3 m_Position;
	[SerializeField] Vector2 m_Scale = Vector2.one;
	[SerializeField] Vector2 m_Pivot;

	[SerializeField] string  m_UIString;
	[SerializeField] float   m_Offset;
	[SerializeField] Color32 m_BaseColor = Color.white;
	[SerializeField] bool2   m_Flip;
	[SerializeField] bool    m_FlipRandom;



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
		get => m_UIString;
		set => m_UIString = value;
	}
	public UI UI {
		get => System.Enum.TryParse(UIString, out UI ui) ? ui : default;
		set => UIString = value.ToString();
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
	public bool FlipRandom {
		get => m_FlipRandom;
		set => m_FlipRandom = value;
	}



	// Baker

	class Baker : Baker<UIDrawerAuthoring> {
		public override void Bake(UIDrawerAuthoring authoring) {
			var components = GetComponents<UIDrawerAuthoring>();
			if (components[0] == authoring) {
				var entity = GetEntity(TransformUsageFlags.Renderable);
				var buffer = AddBuffer<UIDrawer>(entity);
				foreach (var component in components) buffer.Add(new() {

					Position   = component.Position,
					Scale      = component.Scale,
					Pivot      = component.Pivot,

					UI         = component.UI,
					Offset     = component.Offset,
					BaseColor  = component.BaseColor,
					Flip       = component.Flip,
					FlipRandom = component.FlipRandom,

				});
			}
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// UI Drawer
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[InternalBufferCapacity(6)]
public struct UIDrawer : IBufferElementData {

	// Constants

	const uint UIMask = 0xFFC00000u;
	const uint AMask  = 0x00200000u;
	const uint BMask  = 0x00100000u;
	const uint CMask  = 0x00080000u;

	const int UIShift = 22;
	const int AShift  = 21;
	const int BShift  = 20;
	const int CShift  = 19;



	// Fields

	public uint Data;

	public float3 Position;
	public float2 Scale;
	public float2 Pivot;

	public UI UI {
		get => (UI)((Data & UIMask) >> UIShift);
		set => Data = (Data & ~UIMask) | ((uint)value << UIShift);
	}
	public float Offset;
	public color BaseColor;
	public bool2 Flip {
		get => new((Data & AMask) != 0u, (Data & BMask) != 0u);
		set => Data = (Data & ~(AMask | BMask)) | (value.x ? AMask : 0u) | (value.y ? BMask : 0u);
	}
	public bool FlipRandom {
		get => (Data & CMask) != 0u;
		set => Data = (Data & ~CMask) | (value ? CMask : 0u);
	}
}

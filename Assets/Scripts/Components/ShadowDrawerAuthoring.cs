using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Shadow Drawer Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Shadow Drawer")]
public class ShadowDrawerAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(ShadowDrawerAuthoring))]
	class ShadowDrawerAuthoringEditor : EditorExtensions {
		ShadowDrawerAuthoring I => target as ShadowDrawerAuthoring;
		public override void OnInspectorGUI() {
			Begin("Shadow Drawer Authoring");

			var status = PrefabUtility.GetPrefabInstanceStatus(I.gameObject);
			if (status != PrefabInstanceStatus.Connected && !Application.isPlaying) {
				I.Position = Vector3Field("Position", I.Position);
				BeginHorizontal();
				float width = EditorGUIUtility.currentViewWidth * 0.64f;
				I.Yaw      = EditorGUILayout.FloatField("Yaw", I.Yaw, GUILayout.Width(width));
				I.YawLocal = ToggleLeft("Local", I.YawLocal);
				EndHorizontal();
				Space();
				BeginHorizontal();
				PrefixLabel("Shadow");
				I.ShadowText = TextField(I.ShadowText);
				I.Shadow     = EnumField(I.Shadow);
				EndHorizontal();
				BeginHorizontal();
				PrefixLabel("Motion");
				I.MotionText = TextField(I.MotionText);
				I.Motion     = EnumField(I.Motion);
				EndHorizontal();
				I.Offset = FloatField("Offset", I.Offset);
				I.Flip   = Toggle2   ("Flip",   I.Flip);
			}
			End();
		}
	}
	#endif



	// Fields

	[SerializeField] Vector3 m_Position;
	[SerializeField] float   m_Yaw;
	[SerializeField] bool    m_YawLocal;

	[SerializeField] string  m_ShadowText;
	[SerializeField] string  m_MotionText;
	[SerializeField] float   m_Offset;
	[SerializeField] bool2   m_Flip;



	// Properties

	public Vector3 Position {
		get => m_Position;
		set => m_Position = value;
	}
	public float Yaw {
		get => m_Yaw;
		set => m_Yaw = value;
	}
	public bool YawLocal {
		get => m_YawLocal;
		set => m_YawLocal = value;
	}

	public string ShadowText {
		get => m_ShadowText;
		set => m_ShadowText = value;
	}
	public Sprite Shadow {
		get => System.Enum.TryParse(ShadowText, out Sprite sprite) ? sprite : default;
		set => ShadowText = value.ToString();
	}
	public string MotionText {
		get => m_MotionText;
		set => m_MotionText = value;
	}
	public Motion Motion {
		get => System.Enum.TryParse(MotionText, out Motion motion) ? motion : default;
		set => MotionText = value.ToString();
	}
	public float Offset {
		get => m_Offset;
		set => m_Offset = value;
	}
	public bool2 Flip {
		get => m_Flip;
		set => m_Flip = value;
	}



	// Baker

	class Baker : Baker<ShadowDrawerAuthoring> {
		public override void Bake(ShadowDrawerAuthoring authoring) {
			var components = GetComponents<ShadowDrawerAuthoring>();
			if (components[0] == authoring) {
				var entity = GetEntity(TransformUsageFlags.Renderable);
				var buffer = AddBuffer<ShadowDrawer>(entity);
				foreach (var component in components) buffer.Add(new() {

					Position = component.Position,
					Yaw      = component.Yaw,
					YawLocal = component.YawLocal,

					Shadow   = component.Shadow,
					Motion   = component.Motion,
					Offset   = component.Offset,
					Flip     = component.Flip,

				});
			}
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Shadow Drawer
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[InternalBufferCapacity(1)]
public struct ShadowDrawer : IBufferElementData {

	// Constants

	const uint SpriteMask = 0xFFC00000u;
	const uint MotionMask = 0x003E0000u;
	const uint AMask      = 0x00010000u;
	const uint BMask      = 0x00008000u;
	const uint CMask      = 0x00004000u;

	const int SpriteShift = 22;
	const int MotionShift = 17;
	const int AShift      = 16;
	const int BShift      = 15;
	const int CShift      = 14;



	// Fields

	public uint Data;

	public float3 Position;
	public float  Yaw;
	public bool   YawLocal {
		get => (Data & AMask) != 0;
		set => Data = (Data & ~AMask) | (value ? AMask : 0);
	}

	public Sprite Shadow {
		get => (Sprite)((Data & SpriteMask) >> SpriteShift);
		set => Data = (Data & ~SpriteMask) | ((uint)value << SpriteShift);
	}
	public Motion Motion {
		get => (Motion)((Data & MotionMask) >> MotionShift);
		set => Data = (Data & ~MotionMask) | ((uint)value << MotionShift);
	}
	public float Offset;
	public bool2 Flip {
		get => new((Data & BMask) != 0u, (Data & CMask) != 0u);
		set => Data = (Data & ~(BMask | CMask)) | (value.x ? BMask : 0u) | (value.y ? CMask : 0u);
	}
}

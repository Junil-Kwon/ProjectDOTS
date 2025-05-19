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
				var flag   = !Application.isPlaying && status != PrefabInstanceStatus.Connected;
				if (flag) {
					LabelField("Transform", EditorStyles.boldLabel);
					I.Position = Vector3Field("Position",  I.Position);
					I.Yaw      = FloatField  ("Yaw",       I.Yaw);
					I.YawLocal = Toggle      ("Yaw Local", I.YawLocal);
					Space();
					LabelField("Shadow", EditorStyles.boldLabel);
					BeginHorizontal();
					PrefixLabel("Shadow");
					I.ShadowString = TextField(I.ShadowString);
					I.Shadow       = EnumField(I.Shadow);
					EndHorizontal();
					BeginHorizontal();
					PrefixLabel("Motion");
					I.MotionString = TextField(I.MotionString);
					I.Motion       = EnumField(I.Motion);
					EndHorizontal();
					I.Offset = FloatField("Offset", I.Offset);
					I.Flip   = Toggle2   ("Flip",   I.Flip);
					Space();
				}
				End();
			}
		}
	#endif



	// Fields

	[SerializeField] Vector3 m_Position;
	[SerializeField] float   m_Yaw;
	[SerializeField] bool    m_YawLocal;

	[SerializeField] string  m_Shadow;
	[SerializeField] string  m_Motion;
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

	public string ShadowString {
		get => m_Shadow;
		set => m_Shadow = value;
	}
	public string MotionString {
		get => m_Motion;
		set => m_Motion = value;
	}
	public Sprite Shadow {
		get => System.Enum.TryParse(m_Shadow, out Sprite sprite) ? sprite : 0;
		set => m_Shadow = value.ToString();
	}
	public Motion Motion {
		get => System.Enum.TryParse(m_Motion, out Motion motion) ? motion : 0;
		set => m_Motion = value.ToString();
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
			if (components[0] != authoring) return;
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



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Shadow Drawer
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[InternalBufferCapacity(1)]
public struct ShadowDrawer : IBufferElementData {

	public float3 Position;
	public float  Yaw;
	public bool   YawLocal;

	public Sprite Shadow;
	public Motion Motion;
	public float  Offset;
	public bool2  Flip;
}

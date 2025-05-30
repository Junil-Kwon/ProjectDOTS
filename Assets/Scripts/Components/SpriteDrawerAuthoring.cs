using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━

public enum Sprite : ushort {
	None,
	Player,

	SmokeMini,
	SmokeTiny,
	Landing,
}

public enum Motion : ushort {
	None,
	Idle,
	Move,
	Jump,
	Dodge,
	Death,
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Sprite Drawer Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Sprite Drawer")]
public class SpriteDrawerAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(SpriteDrawerAuthoring))]
		class SpriteDrawerAuthoringEditor : EditorExtensions {
			SpriteDrawerAuthoring I => target as SpriteDrawerAuthoring;
			public override void OnInspectorGUI() {
				Begin("Sprite Drawer Authoring");

				var status = PrefabUtility.GetPrefabInstanceStatus(I.gameObject);
				var flag   = !Application.isPlaying && status != PrefabInstanceStatus.Connected;
				if (flag) {
					LabelField("Transform", EditorStyles.boldLabel);
					I.Position = Vector3Field("Position",  I.Position);
					I.Pivot    = Vector2Field("Pivot",     I.Pivot);
					I.Yaw      = FloatField  ("Yaw",       I.Yaw);
					I.YawLocal = Toggle      ("Yaw Local", I.YawLocal);
					Space();
					LabelField("Sprite", EditorStyles.boldLabel);
					BeginHorizontal();
					PrefixLabel("Sprite");
					I.SpriteString = TextField(I.SpriteString);
					I.Sprite       = EnumField(I.Sprite);
					EndHorizontal();
					BeginHorizontal();
					PrefixLabel("Motion");
					I.MotionString = TextField(I.MotionString);
					I.Motion       = EnumField(I.Motion);
					EndHorizontal();
					I.Offset    = FloatField("Offset",     I.Offset);
					I.BaseColor = ColorField("Base Color", I.BaseColor);
					I.MaskColor = ColorField("Mask Color", I.MaskColor);
					I.Emission  = ColorField("Emission",   I.Emission);
					I.Flip      = Toggle2   ("Flip",       I.Flip);
					Space();
				}
				End();
			}
		}
	#endif



	// Fields

	[SerializeField] Vector3 m_Position;
	[SerializeField] Vector2 m_Pivot;
	[SerializeField] float   m_Yaw;
	[SerializeField] bool    m_YawLocal;

	[SerializeField] string  m_Sprite;
	[SerializeField] string  m_Motion;
	[SerializeField] float   m_Offset;
	[SerializeField] Color32 m_BaseColor = Color.white;
	[SerializeField] Color32 m_MaskColor;
	[SerializeField] Color32 m_Emission;
	[SerializeField] bool2   m_Flip;



	// Properties

	public Vector3 Position {
		get => m_Position;
		set => m_Position = value;
	}
	public Vector2 Pivot {
		get => m_Pivot;
		set => m_Pivot = value;
	}
	public float Yaw {
		get => m_Yaw;
		set => m_Yaw = value;
	}
	public bool YawLocal {
		get => m_YawLocal;
		set => m_YawLocal = value;
	}

	public string SpriteString {
		get => m_Sprite;
		set => m_Sprite = value;
	}
	public string MotionString {
		get => m_Motion;
		set => m_Motion = value;
	}
	public Sprite Sprite {
		get => System.Enum.TryParse(m_Sprite, out Sprite sprite) ? sprite : 0;
		set => m_Sprite = value.ToString();
	}
	public Motion Motion {
		get => System.Enum.TryParse(m_Motion, out Motion motion) ? motion : 0;
		set => m_Motion = value.ToString();
	}
	public float Offset {
		get => m_Offset;
		set => m_Offset = value;
	}
	public Color32 BaseColor {
		get => m_BaseColor;
		set => m_BaseColor = value;
	}
	public Color32 MaskColor {
		get => m_MaskColor;
		set => m_MaskColor = value;
	}
	public Color32 Emission {
		get => m_Emission;
		set => m_Emission = value;
	}
	public bool2 Flip {
		get => m_Flip;
		set => m_Flip = value;
	}



	// Baker

	class Baker : Baker<SpriteDrawerAuthoring> {
		public override void Bake(SpriteDrawerAuthoring authoring) {
			var components = GetComponents<SpriteDrawerAuthoring>();
			if (components[0] != authoring) return;
			var entity = GetEntity(TransformUsageFlags.Renderable);
			var buffer = AddBuffer<SpriteDrawer>(entity);
			foreach (var component in components) buffer.Add(new() {

				Position  = component.Position,
				Pivot     = component.Pivot,
				Yaw       = component.Yaw,
				YawLocal  = component.YawLocal,

				Sprite    = component.Sprite,
				Motion    = component.Motion,
				Offset    = component.Offset,
				BaseColor = component.BaseColor,
				MaskColor = component.MaskColor,
				Emission  = component.Emission,
				Flip      = component.Flip,

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Sprite Drawer
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[InternalBufferCapacity(1)]
public struct SpriteDrawer : IBufferElementData {

	public float3 Position;
	public float2 Pivot;
	public float  Yaw;
	public bool   YawLocal;

	public Sprite Sprite;
	public Motion Motion;
	public float  Offset;
	public color  BaseColor;
	public color  MaskColor;
	public color  Emission;
	public bool2  Flip;
}

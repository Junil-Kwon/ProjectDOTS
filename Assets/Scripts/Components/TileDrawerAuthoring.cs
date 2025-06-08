using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━

public enum Tile : ushort {
	None,

	TempGround0,
	TempGround1,
	TempGround2,
	TempGround3,

	GoldMetalFront,
	GoldMetalTop,
	GoldMetalSide,
	GoldMetalBack,
	GoldMetalBottom,
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Tile Drawer Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Tile Drawer")]
public class TileDrawerAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(TileDrawerAuthoring))]
		class TileDrawerAuthoringEditor : EditorExtensions {
			TileDrawerAuthoring I => target as TileDrawerAuthoring;
			public override void OnInspectorGUI() {
				Begin("Tile Drawer Authoring");
				var status = PrefabUtility.GetPrefabInstanceStatus(I.gameObject);
				if (status != PrefabInstanceStatus.Connected && !Application.isPlaying) {

					LabelField("Transform", EditorStyles.boldLabel);
					I.Position      = Vector3Field("Position", I.Position);
					I.EulerRotation = Vector3Field("Rotation", I.EulerRotation);
					Space();
					LabelField("Tile", EditorStyles.boldLabel);
					BeginHorizontal();
					PrefixLabel("Tile");
					I.TileString = TextField(I.TileString);
					I.Tile       = EnumField(I.Tile);
					EndHorizontal();
					I.Offset    = FloatField("Offset",     I.Offset);
					I.BaseColor = ColorField("Base Color", I.BaseColor);
					I.MaskColor = ColorField("Mask Color", I.MaskColor);
					I.Emission  = ColorField("Emission",   I.Emission);
					BeginHorizontal();
					BeginDisabledGroup(I.FlipRandomly && !I.gameObject.isStatic);
					I.Flip = Toggle2("Flip", I.Flip);
					EndDisabledGroup();
					BeginDisabledGroup(!I.gameObject.isStatic);
					I.FlipRandomly = ToggleLeft("Random", I.FlipRandomly);
					EndDisabledGroup();
					EndHorizontal();
					Space();
				}
				End();
			}
		}
	#endif



	// Fields

	[SerializeField] Vector3    m_Position;
	[SerializeField] Quaternion m_Rotation;

	[SerializeField] string     m_Tile;
	[SerializeField] float      m_Offset;
	[SerializeField] Color      m_BaseColor = Color.white;
	[SerializeField] Color      m_MaskColor;
	[SerializeField] Color      m_Emission;
	[SerializeField] bool2      m_Flip;
	[SerializeField] bool       m_FlipRandomly;



	// Properties

	public Vector3 Position {
		get => m_Position;
		set => m_Position = value;
	}
	public Quaternion Rotation {
		get => m_Rotation;
		set => m_Rotation = value;
	}
	public Vector3 EulerRotation {
		get => Rotation.eulerAngles;
		set => Rotation = Quaternion.Euler(value);
	}

	public string TileString {
		get => m_Tile;
		set => m_Tile = value;
	}
	public Tile Tile {
		get => System.Enum.TryParse(m_Tile, out Tile tile) ? tile : 0;
		set => m_Tile = value.ToString();
	}
	public float Offset {
		get => m_Offset;
		set => m_Offset = value;
	}
	public Color BaseColor {
		get => m_BaseColor;
		set => m_BaseColor = value;
	}
	public Color MaskColor {
		get => m_MaskColor;
		set => m_MaskColor = value;
	}
	public Color Emission {
		get => m_Emission;
		set => m_Emission = value;
	}
	public bool2 Flip {
		get => m_Flip;
		set => m_Flip = value;
	}
	public bool FlipRandomly {
		get => m_FlipRandomly;
		set => m_FlipRandomly = value;
	}



	// Baker

	class Baker : Baker<TileDrawerAuthoring> {
		public override void Bake(TileDrawerAuthoring authoring) {
			var components = GetComponents<TileDrawerAuthoring>();
			if (components[0] != authoring) return;
			var entity = GetEntity(TransformUsageFlags.Renderable);
			var buffer = AddBuffer<TileDrawer>(entity);
			foreach (var component in components) buffer.Add(new() {

				Position     = component.Position,
				Rotation     = component.Rotation,

				Tile         = component.Tile,
				Offset       = component.Offset,
				BaseColor    = component.BaseColor,
				MaskColor    = component.MaskColor,
				Emission     = component.Emission,
				Flip         = component.Flip,
				FlipRandomly = component.FlipRandomly && component.gameObject.isStatic,

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Tile Drawer
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[InternalBufferCapacity(1)]
public struct TileDrawer : IBufferElementData {

	public float3     Position;
	public quaternion Rotation;

	public Tile  Tile;
	public float Offset;
	public color BaseColor;
	public color MaskColor;
	public color Emission;
	public bool2 Flip;
	public bool  FlipRandomly;
}

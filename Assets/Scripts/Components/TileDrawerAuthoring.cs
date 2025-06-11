using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEditor;
#endif



// Tile Images

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
				I.Position = Vector3Field("Position", I.Position);
				I.Rotation = EulerField  ("Rotation", I.Rotation);
				Space();
				BeginHorizontal();
				PrefixLabel("Tile");
				I.TileText = TextField(I.TileText);
				I.Tile     = EnumField(I.Tile);
				EndHorizontal();
				I.Offset    = FloatField("Offset",     I.Offset);
				I.BaseColor = ColorField("Base Color", I.BaseColor);
				I.MaskColor = ColorField("Mask Color", I.MaskColor);
				I.Emission  = ColorField("Emission",   I.Emission);
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
	[SerializeField] Vector4 m_Rotation;

	[SerializeField] string  m_TileText;
	[SerializeField] float   m_Offset;
	[SerializeField] Color32 m_BaseColor = Color.white;
	[SerializeField] Color32 m_MaskColor;
	[SerializeField] Color32 m_Emission;
	[SerializeField] bool2   m_Flip;
	[SerializeField] bool    m_FlipRandom;



	// Properties

	public Vector3 Position {
		get => m_Position;
		set => m_Position = value;
	}
	public Vector4 Rotation {
		get => m_Rotation;
		set => m_Rotation = value;
	}

	string TileText {
		get => m_TileText;
		set => m_TileText = value;
	}
	public Tile Tile {
		get => System.Enum.TryParse(TileText, out Tile tile) ? tile : default;
		set => TileText = value.ToString();
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
	public bool FlipRandom {
		get => m_FlipRandom;
		set => m_FlipRandom = value;
	}



	// Baker

	class Baker : Baker<TileDrawerAuthoring> {
		public override void Bake(TileDrawerAuthoring authoring) {
			var components = GetComponents<TileDrawerAuthoring>();
			if (components[0] == authoring) {
				var entity = GetEntity(TransformUsageFlags.Renderable);
				var buffer = AddBuffer<TileDrawer>(entity);
				foreach (var component in components) buffer.Add(new() {

					Position   = component.Position,
					Rotation   = component.Rotation,

					Tile       = component.Tile,
					Offset     = component.Offset,
					BaseColor  = component.BaseColor,
					MaskColor  = component.MaskColor,
					Emission   = component.Emission,
					Flip       = component.Flip,
					FlipRandom = component.FlipRandom,

				});
			}
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Tile Drawer
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[InternalBufferCapacity(1)]
public struct TileDrawer : IBufferElementData {

	// Constants

	const uint TileMask = 0xFFC00000u;
	const uint AMask    = 0x00200000u;
	const uint BMask    = 0x00100000u;
	const uint CMask    = 0x00080000u;

	const int TileShift = 22;
	const int AShift    = 21;
	const int BShift    = 20;
	const int CShift    = 19;



	// Fields

	public uint Data;

	public float3 Position;
	public float4 Rotation;

	public Tile Tile {
		get => (Tile)((Data & TileMask) >> TileShift);
		set => Data = (Data & ~TileMask) | ((uint)value << TileShift);
	}
	public float Offset;
	public color BaseColor;
	public color MaskColor;
	public color Emission;
	public bool2 Flip {
		get => new((Data & AMask) != 0u, (Data & BMask) != 0u);
		set => Data = (Data & ~(AMask | BMask)) | (value.x ? AMask : 0u) | (value.y ? BMask : 0u);
	}
	public bool FlipRandom {
		get => (Data & CMask) != 0u;
		set => Data = (Data & ~CMask) | (value ? CMask : 0u);
	}
}

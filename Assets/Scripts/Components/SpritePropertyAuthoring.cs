using UnityEngine;
using UnityEngine.Rendering;
using System;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Properties;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEditor;
#endif



public enum Sprite : uint {
	None,
	Dummy,
	Player,
	NormalSample,
	SmokeTiny,
	SmokeSmall,
}

public enum Motion : uint {
	Idle,
	Move,
	Brake,
	Jump,
}

public static class SpriteProperty {
	public const string Scale     = "_Sprite_Scale";
	public const string Pivot     = "_Sprite_Pivot";
	public const string Tiling    = "_Sprite_Tiling";
	public const string Offset    = "_Sprite_Offset";
	public const string Center    = "_Sprite_Center";
	public const string BaseColor = "_Sprite_BaseColor";
	public const string MaskColor = "_Sprite_MaskColor";
	public const string Emission  = "_Sprite_Emission";
	public const string Billboard = "_Sprite_Billboard";
}

public struct SpriteDrawData {
	public float3 Position;
	public float4 Rotation;
	public float2 Scale;
	public float2 Pivot;
	public float2 Tiling;
	public float2 Offset;
	public float3 Center;
	public float4 BaseColor;
	public float4 MaskColor;
	public float4 Emission;
	public float  Billboard;
}

public static class MaterialPropertyBlockExtensions {
	public static void SetVector(this MaterialPropertyBlock block, string name, float value) {
		block.SetFloat(name, value);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Sprite Property Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Material Property/Sprite Property")]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer)), ExecuteAlways]
public sealed class SpritePropertyAuthoring : MonoComponent<SpritePropertyAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(SpritePropertyAuthoring))]
	class SpritePropertyAuthoringEditor : EditorExtensions {
		SpritePropertyAuthoring I => target as SpritePropertyAuthoring;
		public override void OnInspectorGUI() {
			Begin();

			BeginDisabledGroup(I.IsPrefabConnected);
			if (I.UseHashData = Toggle("Use Hash Data", I.UseHashData)) {
				I.UseAnimation = Toggle("Use Animation", I.UseAnimation);
			}
			Space();
			if (!I.UseHashData) {
				I.Scale  = Vector2Field("Scale",  I.Scale);
				I.Pivot  = Vector2Field("Pivot",  I.Pivot);
				I.Tiling = Vector2Field("Tiling", I.Tiling);
				I.Offset = Vector2Field("Offset", I.Offset);
			} else {
				I.Sprite = TextEnumField("Sprite", I.Sprite);
				I.Motion = TextEnumField("Motion", I.Motion);
				I.ObjectYaw = FloatField("Object Yaw", I.ObjectYaw);
				I.Time = FloatField("Time", I.Time);
				I.Flip = Toggle2("Flip", I.Flip);
			}
			I.Center = Vector3Field("Center", I.Center);
			I.BaseColor = ColorField("Base Color", I.BaseColor);
			I.MaskColor = ColorField("Mask Color", I.MaskColor);
			I.Emission  = ColorField("Emission",   I.Emission);
			I.Billboard = Slider("Billboard", I.Billboard, 0f, 1f);
			I.UpdateProperty();
			EndDisabledGroup();

			End();
		}
	}
	#endif



	// Fields

	MeshRenderer m_MeshRenderer;
	MaterialPropertyBlock m_PropertyBlock;
	[SerializeField] bool m_UseHashData = true;
	[SerializeField] bool m_UseAnimation;

	[SerializeField] Vector2 m_Scale     = new(1f, 1f);
	[SerializeField] Vector2 m_Pivot     = new(0f, 0f);
	[SerializeField] Vector2 m_Tiling    = new(1f, 1f);
	[SerializeField] Vector2 m_Offset    = new(0f, 0f);
	[SerializeField] Vector3 m_Center    = new(0f, 0f, 0f);
	[SerializeField] Vector4 m_BaseColor = new(1f, 1f, 1f, 1f);
	[SerializeField] Vector4 m_MaskColor = new(1f, 1f, 1f, 0f);
	[SerializeField] Vector4 m_Emission  = new(0f, 0f, 0f, 0f);
	[SerializeField] float   m_Billboard = 1f;

	#if UNITY_EDITOR
	[SerializeField] string m_SpriteName;
	[SerializeField] string m_MotionName;
	#endif

	[SerializeField] Sprite m_Sprite;
	[SerializeField] Motion m_Motion;
	[SerializeField] float m_ObjectYaw;
	[SerializeField] float m_Time;
	[SerializeField] bool2 m_Flip;



	// Properties

	MeshRenderer MeshRenderer => !m_MeshRenderer ?
		m_MeshRenderer = GetOwnComponent<MeshRenderer>() :
		m_MeshRenderer;

	MaterialPropertyBlock PropertyBlock {
		get => m_PropertyBlock ??= new();
	}
	bool UseHashData {
		get => m_UseHashData;
		set => m_UseHashData = value;
	}
	bool UseAnimation {
		get => m_UseAnimation;
		set => m_UseAnimation = value;
	}



	Vector2 Scale {
		get => m_Scale;
		set => m_Scale = value;
	}
	Vector2 Pivot {
		get => m_Pivot;
		set => m_Pivot = value;
	}
	Vector2 Tiling {
		get => m_Tiling;
		set => m_Tiling = value;
	}
	Vector2 Offset {
		get => m_Offset;
		set => m_Offset = value;
	}
	Vector3 Center {
		get => m_Center;
		set => m_Center = value;
	}
	Vector4 BaseColor {
		get => m_BaseColor;
		set => m_BaseColor = value;
	}
	Vector4 MaskColor {
		get => m_MaskColor;
		set => m_MaskColor = value;
	}
	Vector4 Emission {
		get => m_Emission;
		set => m_Emission = value;
	}
	float Billboard {
		get => m_Billboard;
		set => m_Billboard = value;
	}



	#if UNITY_EDITOR
	Sprite Sprite {
		get => !Enum.TryParse(m_SpriteName, out Sprite sprite) ?
			Enum.Parse<Sprite>(m_SpriteName = m_Sprite.ToString()) :
			m_Sprite = sprite;
		set => m_SpriteName = (m_Sprite = value).ToString();
	}
	Motion Motion {
		get => !Enum.TryParse(m_MotionName, out Motion motion) ?
			Enum.Parse<Motion>(m_MotionName = m_Motion.ToString()) :
			m_Motion = motion;
		set => m_MotionName = (m_Motion = value).ToString();
	}
	#else
	Sprite Sprite {
		get => m_Sprite;
		set => m_Sprite = value;
	}
	Motion Motion {
		get => m_Motion;
		set => m_Motion = value;
	}
	#endif

	float ObjectYaw {
		get => m_ObjectYaw;
		set => m_ObjectYaw = value;
	}
	float Time {
		get => m_Time;
		set => m_Time = value;
	}
	bool2 Flip {
		get => m_Flip;
		set => m_Flip = value;
	}

	SpriteHash Hash => new() {
		Sprite    = Sprite,
		Motion    = Motion,
		Direction = default,
		ObjectYaw = ObjectYaw,
		Time      = Time,
		Flip      = Flip,
	};



	// Methods

	#if UNITY_EDITOR
	void LoadSpriteData() {
		if (!UseHashData || IsPrefabConnected) return;
		var data = DrawManager.GetSpriteData(Hash);
		Scale  = data.scale;
		Pivot  = data.pivot;
		Tiling = data.tiling;
		Offset = data.offset;
		if (Selection.activeGameObject != gameObject) EditorUtility.SetDirty(this);
	}
	#else
	void LoadSpriteData() {
		if (!UseHashData) return;
		var data = DrawManager.GetSpriteData(Hash);
		Scale  = data.scale;
		Pivot  = data.pivot;
		Tiling = data.tiling;
		Offset = data.offset;
	}
	#endif

	public void UpdateProperty() {
		LoadSpriteData();
		PropertyBlock.SetVector(SpriteProperty.Scale,     Scale);
		PropertyBlock.SetVector(SpriteProperty.Pivot,     Pivot);
		PropertyBlock.SetVector(SpriteProperty.Tiling,    Tiling);
		PropertyBlock.SetVector(SpriteProperty.Offset,    Offset);
		PropertyBlock.SetVector(SpriteProperty.Center,    Center);
		PropertyBlock.SetVector(SpriteProperty.BaseColor, BaseColor);
		PropertyBlock.SetVector(SpriteProperty.MaskColor, MaskColor);
		PropertyBlock.SetVector(SpriteProperty.Emission,  Emission);
		PropertyBlock.SetVector(SpriteProperty.Billboard, Billboard);
		MeshRenderer.SetPropertyBlock(PropertyBlock);
	}



	// Lifecycle

	void Start() {
		MeshRenderer.sharedMaterial = DrawManager.SpriteMaterial;
		UpdateProperty();
	}



	// Baker

	class Baker : Baker<SpritePropertyAuthoring> {
		public override void Bake(SpritePropertyAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.Renderable);
			AddComponent(entity, new SpritePropertyScale     { Value = authoring.Scale });
			AddComponent(entity, new SpritePropertyPivot     { Value = authoring.Pivot });
			AddComponent(entity, new SpritePropertyTiling    { Value = authoring.Tiling });
			AddComponent(entity, new SpritePropertyOffset    { Value = authoring.Offset });
			AddComponent(entity, new SpritePropertyCenter    { Value = authoring.Center });
			AddComponent(entity, new SpritePropertyBaseColor { Value = authoring.BaseColor });
			AddComponent(entity, new SpritePropertyMaskColor { Value = authoring.MaskColor });
			AddComponent(entity, new SpritePropertyEmission  { Value = authoring.Emission });
			AddComponent(entity, new SpritePropertyBillboard { Value = authoring.Billboard });
			if (authoring.UseHashData && authoring.UseAnimation) {
				AddComponent(entity, authoring.Hash);
			}
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Sprite Property
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[MaterialProperty(SpriteProperty.Scale, 8)]
public struct SpritePropertyScale : IComponentData {
	public float2 Value;
}

[MaterialProperty(SpriteProperty.Pivot, 8)]
public struct SpritePropertyPivot : IComponentData {
	public float2 Value;
}

[MaterialProperty(SpriteProperty.Tiling, 8)]
public struct SpritePropertyTiling : IComponentData {
	public float2 Value;
}

[MaterialProperty(SpriteProperty.Offset, 8)]
public struct SpritePropertyOffset : IComponentData {
	public float2 Value;
}

[MaterialProperty(SpriteProperty.Center, 12)]
public struct SpritePropertyCenter : IComponentData {
	public float3 Value;
}

[MaterialProperty(SpriteProperty.BaseColor, 16)]
public struct SpritePropertyBaseColor : IComponentData {
	public float4 Value;
}

[MaterialProperty(SpriteProperty.MaskColor, 16)]
public struct SpritePropertyMaskColor : IComponentData {
	public float4 Value;
}

[MaterialProperty(SpriteProperty.Emission, 16)]
public struct SpritePropertyEmission : IComponentData {
	public float4 Value;
}

[MaterialProperty(SpriteProperty.Billboard, 4)]
public struct SpritePropertyBillboard : IComponentData {
	public float Value;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Sprite Hash
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct SpriteHash : IComponentData {

	// Constants

	const uint SpriteMask    = 0xFFC00000u;
	const uint MotionMask    = 0x003E0000u;
	const uint DirectionMask = 0x0001F000u;
	const uint TickMask      = 0x00000FFFu;

	const int SpriteShift    = 22;
	const int MotionShift    = 17;
	const int DirectionShift = 12;
	const int TickShift      = 00;



	// Fields

	public uint Key;
	public float ObjectYaw;
	public bool2 Flip;



	// Properties

	[CreateProperty] public Sprite Sprite {
		get => (Sprite)(((Key & SpriteMask) >> SpriteShift) - 1u);
		set => Key = (Key & ~SpriteMask) | ((((uint)value + 1u) << SpriteShift) & SpriteMask);
	}
	[CreateProperty] public Motion Motion {
		get => (Motion)(((Key & MotionMask) >> MotionShift) - 1u);
		set => Key = (Key & ~MotionMask) | ((((uint)value + 1u) << MotionShift) & MotionMask);
	}
	[CreateProperty] public uint Direction {
		get => ((Key & DirectionMask) >> DirectionShift) - 1u;
		set => Key = (Key & ~DirectionMask) | (((value + 1u) << DirectionShift) & DirectionMask);
	}
	public uint Tick {
		get => ((Key & TickMask) >> TickShift) - 1u;
		set => Key = (Key & ~TickMask) | (((value + 1u) << TickShift) & TickMask);
	}
	[CreateProperty] public float Time {
		get => Tick * DrawManager.SampleInterval;
		set => Tick = (uint)(value * DrawManager.SampleRate);
	}
}

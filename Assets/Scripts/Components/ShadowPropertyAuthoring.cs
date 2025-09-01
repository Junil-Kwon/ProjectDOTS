using UnityEngine;
using System;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Properties;
using Unity.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif



public static class ShadowProperty {
	public const string Scale  = "_Shadow_Scale";
	public const string Pivot  = "_Shadow_Pivot";
	public const string Center = "_Shadow_Center";
	public const string Tiling = "_Shadow_Tiling";
	public const string Offset = "_Shadow_Offset";
}

public struct ShadowDrawData {
	public float3 Position;
	public float4 Rotation;
	public float2 Scale;
	public float2 Pivot;
	public float2 Tiling;
	public float2 Offset;
	public float3 Center;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Shadow Property Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Material Property/Shadow Property")]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer)), ExecuteAlways]
public sealed class ShadowPropertyAuthoring : MonoComponent<ShadowPropertyAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(ShadowPropertyAuthoring))]
	class ShadowPropertyAuthoringEditor : EditorExtensions {
		ShadowPropertyAuthoring I => target as ShadowPropertyAuthoring;
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

	[SerializeField] Vector2 m_Scale  = new(1f, 1f);
	[SerializeField] Vector2 m_Pivot  = new(0f, 0f);
	[SerializeField] Vector2 m_Tiling = new(1f, 1f);
	[SerializeField] Vector2 m_Offset = new(0f, 0f);
	[SerializeField] Vector3 m_Center = new(0f, 0f, 0f);

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

	ShadowHash Hash => new() {
		Sprite    = Sprite,
		Motion    = Motion,
		Direction = default,
		ObjectYaw = ObjectYaw,
		Time      = Time,
		Flip      = Flip,
	};



	// Methods

	#if UNITY_EDITOR
	void LoadShadowData() {
		if (!UseHashData || IsPrefabConnected) return;
		var data = DrawManager.GetShadowData(Hash);
		Scale  = data.scale;
		Pivot  = data.pivot;
		Tiling = data.tiling;
		Offset = data.offset;
		if (Selection.activeGameObject != gameObject) EditorUtility.SetDirty(this);
	}
	#else
	void LoadShadowData() {
		if (!UseHashData) return;
		var data = DrawManager.GetShadowData(Hash);
		Scale  = data.scale;
		Pivot  = data.pivot;
		Tiling = data.tiling;
		Offset = data.offset;
	}
	#endif

	public void UpdateProperty() {
		LoadShadowData();
		PropertyBlock.SetVector(ShadowProperty.Scale,  Scale);
		PropertyBlock.SetVector(ShadowProperty.Pivot,  Pivot);
		PropertyBlock.SetVector(ShadowProperty.Tiling, Tiling);
		PropertyBlock.SetVector(ShadowProperty.Offset, Offset);
		PropertyBlock.SetVector(ShadowProperty.Center, Center);
		MeshRenderer.SetPropertyBlock(PropertyBlock);
	}



	// Lifecycle

	void Start() {
		MeshRenderer.sharedMaterial = DrawManager.ShadowMaterial;
		UpdateProperty();
	}



	// Baker

	class Baker : Baker<ShadowPropertyAuthoring> {
		public override void Bake(ShadowPropertyAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.Renderable);
			AddComponent(entity, new ShadowPropertyScale  { Value = authoring.Scale });
			AddComponent(entity, new ShadowPropertyPivot  { Value = authoring.Pivot });
			AddComponent(entity, new ShadowPropertyTiling { Value = authoring.Tiling });
			AddComponent(entity, new ShadowPropertyOffset { Value = authoring.Offset });
			AddComponent(entity, new ShadowPropertyCenter { Value = authoring.Center });
			if (authoring.UseHashData && authoring.UseAnimation) {
				AddComponent(entity, authoring.Hash);
			}
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Shadow Property
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[MaterialProperty(ShadowProperty.Scale, 8)]
public struct ShadowPropertyScale : IComponentData {
	public float2 Value;
}

[MaterialProperty(ShadowProperty.Pivot, 8)]
public struct ShadowPropertyPivot : IComponentData {
	public float2 Value;
}

[MaterialProperty(ShadowProperty.Tiling, 8)]
public struct ShadowPropertyTiling : IComponentData {
	public float2 Value;
}

[MaterialProperty(ShadowProperty.Offset, 8)]
public struct ShadowPropertyOffset : IComponentData {
	public float2 Value;
}

[MaterialProperty(ShadowProperty.Center, 12)]
public struct ShadowPropertyCenter : IComponentData {
	public float3 Value;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Shadow Hash
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct ShadowHash : IComponentData {

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

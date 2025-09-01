using UnityEngine;
using System;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Properties;
using Unity.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif



public enum Canvas : uint {
	None,
	Bar,
	BarL,
	BarM,
	BarR,
}

public static class CanvasProperty {
	public const string Scale     = "_Canvas_Scale";
	public const string Pivot     = "_Canvas_Pivot";
	public const string Tiling    = "_Canvas_Tiling";
	public const string Offset    = "_Canvas_Offset";
	public const string Center    = "_Canvas_Center";
	public const string BaseColor = "_Canvas_BaseColor";
	public const string MaskColor = "_Canvas_MaskColor";
}

public struct CanvasDrawData {
	public float3 Position;
	public float2 Scale;
	public float2 Pivot;
	public float2 Tiling;
	public float2 Offset;
	public float3 Center;
	public float4 BaseColor;
	public float4 MaskColor;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Canvas Property Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Material Property/Canvas Property")]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer)), ExecuteAlways]
public sealed class CanvasPropertyAuthoring : MonoComponent<CanvasPropertyAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(CanvasPropertyAuthoring))]
	class CanvasMaterialPropertyAuthoringEditor : EditorExtensions {
		CanvasPropertyAuthoring I => target as CanvasPropertyAuthoring;
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
				I.Canvas = TextEnumField("Canvas", I.Canvas);
				I.Time = FloatField("Time", I.Time);
				I.Flip = Toggle2("Flip", I.Flip);
			}
			I.Center = Vector3Field("Center", I.Center);
			I.BaseColor = ColorField("Base Color", I.BaseColor);
			I.MaskColor = ColorField("Mask Color", I.MaskColor);
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

	#if UNITY_EDITOR
	[SerializeField] string m_CanvasName;
	#endif

	[SerializeField] Canvas m_Canvas;
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



	#if UNITY_EDITOR
	Canvas Canvas {
		get => !Enum.TryParse(m_CanvasName, out Canvas canvas) ?
			Enum.Parse<Canvas>(m_CanvasName = m_Canvas.ToString()) :
			m_Canvas = canvas;
		set => m_CanvasName = (m_Canvas = value).ToString();
	}
	#else
	Canvas Canvas {
		get => m_Canvas;
		set => m_Canvas = value;
	}
	#endif

	float Time {
		get => m_Time;
		set => m_Time = value;
	}
	bool2 Flip {
		get => m_Flip;
		set => m_Flip = value;
	}

	CanvasHash Hash => new() {
		Canvas = Canvas,
		Time   = Time,
		Flip   = Flip,
	};



	// Methods

	#if UNITY_EDITOR
	void LoadCanvasData() {
		if (!UseHashData || IsPrefabConnected) return;
		var data = DrawManager.GetCanvasData(Hash);
		Scale  = data.scale;
		Pivot  = data.pivot;
		Tiling = data.tiling;
		Offset = data.offset;
		if (Selection.activeGameObject != gameObject) EditorUtility.SetDirty(this);
	}
	#else
	void LoadCanvasData() {
		if (!UseHashData) return;
		var data = DrawManager.GetCanvasData(Hash);
		Scale  = data.scale;
		Pivot  = data.pivot;
		Tiling = data.tiling;
		Offset = data.offset;
	}
	#endif

	public void UpdateProperty() {
		LoadCanvasData();
		PropertyBlock.SetVector(CanvasProperty.Scale,     Scale);
		PropertyBlock.SetVector(CanvasProperty.Pivot,     Pivot);
		PropertyBlock.SetVector(CanvasProperty.Tiling,    Tiling);
		PropertyBlock.SetVector(CanvasProperty.Offset,    Offset);
		PropertyBlock.SetVector(CanvasProperty.Center,    Center);
		PropertyBlock.SetVector(CanvasProperty.BaseColor, BaseColor);
		PropertyBlock.SetVector(CanvasProperty.MaskColor, MaskColor);
		MeshRenderer.SetPropertyBlock(PropertyBlock);
	}



	// Lifecycle

	void Start() {
		MeshRenderer.sharedMaterial = DrawManager.CanvasMaterial;
		UpdateProperty();
	}



	// Baker

	class Baker : Baker<CanvasPropertyAuthoring> {
		public override void Bake(CanvasPropertyAuthoring authoring) {
			Entity entity = GetEntity(TransformUsageFlags.Renderable);
			AddComponent(entity, new CanvasPropertyScale     { Value = authoring.Scale });
			AddComponent(entity, new CanvasPropertyPivot     { Value = authoring.Pivot });
			AddComponent(entity, new CanvasPropertyTiling    { Value = authoring.Tiling });
			AddComponent(entity, new CanvasPropertyOffset    { Value = authoring.Offset });
			AddComponent(entity, new CanvasPropertyCenter    { Value = authoring.Center });
			AddComponent(entity, new CanvasPropertyBaseColor { Value = authoring.BaseColor });
			AddComponent(entity, new CanvasPropertyMaskColor { Value = authoring.MaskColor });
			if (authoring.UseHashData && authoring.UseAnimation) {
				AddComponent(entity, authoring.Hash);
			}
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Canvas Property
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[MaterialProperty(CanvasProperty.Scale, 8)]
public struct CanvasPropertyScale : IComponentData {
	public float2 Value;
}

[MaterialProperty(CanvasProperty.Pivot, 8)]
public struct CanvasPropertyPivot : IComponentData {
	public float2 Value;
}

[MaterialProperty(CanvasProperty.Tiling, 8)]
public struct CanvasPropertyTiling : IComponentData {
	public float2 Value;
}

[MaterialProperty(CanvasProperty.Offset, 8)]
public struct CanvasPropertyOffset : IComponentData {
	public float2 Value;
}

[MaterialProperty(CanvasProperty.Center, 12)]
public struct CanvasPropertyCenter : IComponentData {
	public float3 Value;
}

[MaterialProperty(CanvasProperty.BaseColor, 16)]
public struct CanvasPropertyBaseColor : IComponentData {
	public float4 Value;
}

[MaterialProperty(CanvasProperty.MaskColor, 16)]
public struct CanvasPropertyMaskColor : IComponentData {
	public float4 Value;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Canvas Hash
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CanvasHash : IComponentData {

	// Constants

	const uint CanvasMask = 0xFFC00000u;
	const uint TickMask   = 0x00000FFFu;

	const int CanvasShift = 22;
	const int TickShift   = 00;



	// Fields

	public uint Key;
	public bool2 Flip;



	// Properties

	[CreateProperty] public Canvas Canvas {
		get => (Canvas)(((Key & CanvasMask) >> CanvasShift) - 1u);
		set => Key = (Key & ~CanvasMask) | ((((uint)value + 1u) << CanvasShift) & CanvasMask);
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

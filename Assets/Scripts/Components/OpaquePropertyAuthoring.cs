using UnityEngine;
using System;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Properties;
using Unity.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif



public enum Opaque : uint {
	None,
}

public static class OpaqueProperty {
	public const string Tiling = "_Opaque_Tiling";
	public const string Offset = "_Opaque_Offset";
}

public struct OpaqueDrawData {
	public float3 Position;
	public float4 Rotation;
	public float2 Tiling;
	public float2 Offset;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Opaque Property Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Material Property/Opaque Property")]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer)), ExecuteAlways]
public sealed class OpaquePropertyAuthoring : MonoComponent<OpaquePropertyAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(OpaquePropertyAuthoring))]
	class OpaqueMaterialPropertyAuthoringEditor : EditorExtensions {
		OpaquePropertyAuthoring I => target as OpaquePropertyAuthoring;
		public override void OnInspectorGUI() {
			Begin();

			BeginDisabledGroup(I.IsPrefabConnected);
			if (I.UseHashData = Toggle("Use Hash Data", I.UseHashData)) {
				I.UseAnimation = Toggle("Use Animation", I.UseAnimation);
			}
			Space();
			if (!I.UseHashData) {
				I.Tiling = Vector2Field("Tiling", I.Tiling);
				I.Offset = Vector2Field("Offset", I.Offset);
			} else {
				I.Opaque = TextEnumField("Opaque", I.Opaque);
				I.Time = FloatField("Time", I.Time);
			}
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

	[SerializeField] Vector2 m_Tiling = new(1f, 1f);
	[SerializeField] Vector2 m_Offset = new(0f, 0f);

	#if UNITY_EDITOR
	[SerializeField] string m_OpaqueName;
	#endif

	[SerializeField] Opaque m_Opaque;
	[SerializeField] float m_Time;



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



	Vector2 Tiling {
		get => m_Tiling;
		set => m_Tiling = value;
	}
	Vector2 Offset {
		get => m_Offset;
		set => m_Offset = value;
	}



	#if UNITY_EDITOR
	Opaque Opaque {
		get => !Enum.TryParse(m_OpaqueName, out Opaque opaque) ?
			Enum.Parse<Opaque>(m_OpaqueName = m_Opaque.ToString()) :
			m_Opaque = opaque;
		set => m_OpaqueName = (m_Opaque = value).ToString();
	}
	#else
	Opaque Opaque {
		get => m_Opaque;
		set => m_Opaque = value;
	}
	#endif

	float Time {
		get => m_Time;
		set => m_Time = value;
	}

	OpaqueHash Hash => new() {
		Opaque = Opaque,
		Time   = Time,
	};



	// Methods

	#if UNITY_EDITOR
	void LoadOpaqueData() {
		if (!UseHashData || IsPrefabConnected) return;
		var data = DrawManager.GetOpaqueData(Hash);
		Tiling = data.tiling;
		Offset = data.offset;
		if (Selection.activeGameObject != gameObject) EditorUtility.SetDirty(this);
	}
	#else
	void LoadOpaqueData() {
		if (!UseHashData) return;
		var data = DrawManager.GetOpaqueData(Hash);
		Tiling = data.tiling;
		Offset = data.offset;
	}
	#endif

	public void UpdateProperty() {
		LoadOpaqueData();
		PropertyBlock.SetVector(OpaqueProperty.Offset, Offset);
		PropertyBlock.SetVector(OpaqueProperty.Tiling, Tiling);
		MeshRenderer.SetPropertyBlock(PropertyBlock);
	}



	// Lifecycle

	void Start() {
		MeshRenderer.sharedMaterial = DrawManager.OpaqueMaterial;
		UpdateProperty();
	}



	// Baker

	class Baker : Baker<OpaquePropertyAuthoring> {
		public override void Bake(OpaquePropertyAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.Renderable);
			AddComponent(entity, new OpaquePropertyTiling { Value = authoring.Tiling });
			AddComponent(entity, new OpaquePropertyOffset { Value = authoring.Offset });
			if (authoring.UseHashData && authoring.UseAnimation) {
				AddComponent(entity, authoring.Hash);
			}
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Opaque Property
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[MaterialProperty(OpaqueProperty.Tiling, 8)]
public struct OpaquePropertyTiling : IComponentData {
	public float2 Value;
}

[MaterialProperty(OpaqueProperty.Offset, 8)]
public struct OpaquePropertyOffset : IComponentData {
	public float2 Value;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Opaque Hash
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct OpaqueHash : IComponentData {

	// Constants

	const uint OpaqueMask = 0xFFC00000u;
	const uint TickMask   = 0x00000FFFu;

	const int OpaqueShift = 22;
	const int TickShift   = 00;



	// Fields

	public uint Key;



	// Properties

	[CreateProperty] public Opaque Opaque {
		get => (Opaque)(((Key & OpaqueMask) >> OpaqueShift) - 1u);
		set => Key = (Key & ~OpaqueMask) | ((((uint)value + 1u) << OpaqueShift) & OpaqueMask);
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

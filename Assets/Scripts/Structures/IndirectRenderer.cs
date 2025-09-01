using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Runtime.InteropServices;

using Unity.Collections;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Indirect Renderer
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class IndirectRenderer<T> : IDisposable where T : unmanaged {

	// Constants

	const GraphicsBuffer.Target Args = GraphicsBuffer.Target.IndirectArguments;
	const GraphicsBuffer.Target Structured = GraphicsBuffer.Target.Structured;
	const GraphicsBuffer.UsageFlags Locked = GraphicsBuffer.UsageFlags.LockBufferForWrite;
	const NativeArrayOptions Uninitialized = NativeArrayOptions.UninitializedMemory;

	const int MinCapacity = 64;



	// Fields

	Mesh m_CacheMesh;
	int m_Stride;
	int m_PropID;
	int m_Length;

	GraphicsBuffer m_ArgsBuffer;
	GraphicsBuffer m_StructuredBuffer;
	bool m_IsDirty;
	RenderParams m_Param;



	// Properties

	Mesh CacheMesh {
		get => m_CacheMesh;
		set => m_CacheMesh = value;
	}
	int Stride {
		get => m_Stride;
		set => m_Stride = value;
	}
	int PropID {
		get => m_PropID;
		set => m_PropID = value;
	}
	public int Length {
		get => m_Length;
		private set {
			if (m_Length != value) {
				m_Length = value;
				IsDirty = true;
			}
		}
	}
	public int Capacity {
		get => StructuredBuffer.count;
		set {
			value = Mathf.Max(MinCapacity, value);
			if (StructuredBuffer.count != value) {
				Length = Mathf.Min(Length, value);

				var prev = StructuredBuffer.LockBufferForWrite<T>(0, Length);
				var next = new NativeArray<T>(Length, Allocator.Temp, Uninitialized);
				NativeArray<T>.Copy(prev, next, Length);
				StructuredBuffer.UnlockBufferAfterWrite<T>(Length);
				StructuredBuffer.Dispose();

				StructuredBuffer = new GraphicsBuffer(Structured, Locked, value, Stride);
				StructuredBuffer.SetData(next, 0, 0, Length);
				Param.matProps.SetBuffer(PropID, StructuredBuffer);
				next.Dispose();
			}
		}
	}

	GraphicsBuffer ArgsBuffer {
		get => m_ArgsBuffer;
		set => m_ArgsBuffer = value;
	}
	GraphicsBuffer StructuredBuffer {
		get => m_StructuredBuffer;
		set => m_StructuredBuffer = value;
	}
	bool IsDirty {
		get => m_IsDirty;
		set => m_IsDirty = value;
	}
	public ref RenderParams Param {
		get => ref m_Param;
	}



	// Constructors

	public IndirectRenderer(Mesh mesh, Material material, int submesh = 0) {
		CacheMesh = mesh;
		Stride = Marshal.SizeOf<T>();
		PropID = Shader.PropertyToID($"_{typeof(T).Name}");
		Length = 0;

		ArgsBuffer = new GraphicsBuffer(Args, Locked, 5, sizeof(int));
		ArgsBuffer.SetData(new int[5] {
			(int)CacheMesh.GetIndexCount(submesh),
			Length,
			(int)CacheMesh.GetIndexStart(submesh),
			(int)CacheMesh.GetBaseVertex(submesh),
			0,
		});
		StructuredBuffer = new GraphicsBuffer(Structured, Locked, MinCapacity, Stride);

		Param = new RenderParams(material) {
			worldBounds = new Bounds(Vector3.zero, Vector3.one * 1024f),
			shadowCastingMode = ShadowCastingMode.On,
			receiveShadows = true,
			matProps = new MaterialPropertyBlock(),
		};
		Param.matProps.SetBuffer(PropID, StructuredBuffer);
	}

	public void Dispose() {
		ArgsBuffer.Release();
		StructuredBuffer.Release();
	}



	// Methods

	public NativeArray<T> LockBuffer(int count) {
		if (Capacity < count) Capacity = Mathf.Max(Capacity * 2, Capacity + count);
		return StructuredBuffer.LockBufferForWrite<T>(0, Length = count);
	}

	public void UnlockBuffer(int count) {
		StructuredBuffer.UnlockBufferAfterWrite<T>(count);
	}

	public void Clear() {
		Length = 0;
	}

	public void Draw() {
		if (IsDirty) {
			IsDirty = false;
			var args = ArgsBuffer.LockBufferForWrite<int>(0, 5);
			args[1] = Length;
			ArgsBuffer.UnlockBufferAfterWrite<int>(5);
		}
		Graphics.RenderMeshIndirect(Param, CacheMesh, ArgsBuffer);
	}
}

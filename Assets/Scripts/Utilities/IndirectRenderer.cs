using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Unity.Collections;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Indirect Renderer
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class IndirectRenderer<T> : IDisposable where T : unmanaged {

	// Constants

	const GraphicsBuffer.Target Args = GraphicsBuffer.Target.IndirectArguments;
	const GraphicsBuffer.Target Structured = GraphicsBuffer.Target.Structured;
	const GraphicsBuffer.UsageFlags Locked = GraphicsBuffer.UsageFlags.LockBufferForWrite;
	const NativeArrayOptions Uninitialized = NativeArrayOptions.UninitializedMemory;

	const int MinCapacity = 256;



	// Fields

	Mesh cacheMesh;
	int stride;
	int propID;
	int length;

	GraphicsBuffer argsBuffer;
	GraphicsBuffer structuredBuffer;
	Dictionary<int, (int start, int count)> hash = new();
	List<int> list = new();

	public RenderParams param;
	bool isDirty;



	// Properties

	public int Length {
		get => length;
		private set {
			length = value;
			isDirty = true;
		}
	}

	public int Capacity {
		get => structuredBuffer.count;
		set {
			value = Mathf.Max(MinCapacity, value);
			if (structuredBuffer.count != value) {
				Length = Mathf.Min(Length, value);

				var prev = structuredBuffer.LockBufferForWrite<T>(0, Length);
				var next = new NativeArray<T>(Length, Allocator.Temp, Uninitialized);
				NativeArray<T>.Copy(prev, next, Length);
				structuredBuffer.UnlockBufferAfterWrite<T>(Length);
				structuredBuffer.Dispose();

				structuredBuffer = new GraphicsBuffer(Structured, Locked, value, stride);
				structuredBuffer.SetData(next, 0, 0, Length);
				param.matProps.SetBuffer(propID, structuredBuffer);
			}
		}
	}



	// Constructors

	public IndirectRenderer(Material material, Mesh mesh, int submesh = 0) {
		cacheMesh = mesh;
		stride = Marshal.SizeOf<T>();
		propID = Shader.PropertyToID($"_{typeof(T).Name}");
		length = 0;

		argsBuffer = new GraphicsBuffer(Args, Locked, 5, sizeof(int));
		var args = argsBuffer.LockBufferForWrite<int>(0, argsBuffer.count);
		args[0] = (int)cacheMesh.GetIndexCount(submesh);
		args[1] = Length;
		args[2] = (int)cacheMesh.GetIndexStart(submesh);
		args[3] = (int)cacheMesh.GetBaseVertex(submesh);
		args[4] = 0;
		argsBuffer.UnlockBufferAfterWrite<int>(argsBuffer.count);
		structuredBuffer = new GraphicsBuffer(Structured, Locked, MinCapacity, stride);

		param = new RenderParams(material);
		param.worldBounds = new Bounds(Vector3.zero, Vector3.one * 1024f);
		param.shadowCastingMode = ShadowCastingMode.On;
		param.receiveShadows = true;
		param.matProps = new MaterialPropertyBlock();
		param.matProps.SetBuffer(propID, structuredBuffer);
	}

	public void Dispose() {
		structuredBuffer.Release();
		argsBuffer.Release();
	}



	// Buffer Methods

	public NativeArray<T> LockBuffer() {
		return structuredBuffer.LockBufferForWrite<T>(0, Length);
	}

	public NativeArray<T> LockExistingBuffer(int key) {
		if (!hash.TryGetValue(key, out var value)) return default;
		return structuredBuffer.LockBufferForWrite<T>(value.start, value.count);
	}

	public NativeArray<T> AllocateAndLockBuffer(int key, int count) {
		if (!hash.TryGetValue(key, out var value)) {
			int index = 0;
			foreach (var pair in hash) if (pair.Key < key) {
				index = Mathf.Max(index, pair.Value.start + pair.Value.count);
			}
			hash.Add(key, value = (index, 0));
		}
		if (structuredBuffer.count < Length + count) Capacity = Mathf.Max(Length * 2, Length + count);
		if (value.start + value.count < Length) {
			var array = new T[Length - (value.start + value.count)];
			structuredBuffer.GetData(array, 0, value.start + value.count, array.Length);
			structuredBuffer.SetData(array, 0, value.start + value.count + count, array.Length);
		}
		foreach (var i in hash) if (key < i.Key) list.Add(i.Key);
		foreach (var i in list) hash[i] = (hash[i].start + count, hash[i].count);
		list.Clear();
		Length += count;

		hash[key] = (value.start, value.count + count);
		return structuredBuffer.LockBufferForWrite<T>(value.start + value.count, count);
	}



	public void UnlockBuffer() {
		structuredBuffer.UnlockBufferAfterWrite<T>(Length);
	}

	public void UnlockBuffer(int count) {
		structuredBuffer.UnlockBufferAfterWrite<T>(count);
	}



	public void Clear() {
		hash.Clear();
		Length = 0;
	}

	public void Clear(int key) {
		if (!hash.TryGetValue(key, out var value)) return;
		if (value.start + value.count < Length) {
			var array = new T[Length - (value.start + value.count)];
			structuredBuffer.GetData(array, 0, value.start + value.count, array.Length);
			structuredBuffer.SetData(array, 0, value.start, array.Length);
		}
		foreach (var i in hash) if (key < i.Key) list.Add(i.Key);
		foreach (var i in list) hash[i] = (hash[i].start - value.count, hash[i].count);
		list.Clear();
		Length -= value.count;

		hash.Remove(key);
	}



	// Draw Methods

	public void Draw() {
		if (isDirty) {
			isDirty = false;
			var args = argsBuffer.LockBufferForWrite<int>(1, 1);
			args[0] = Length;
			argsBuffer.UnlockBufferAfterWrite<int>(1);
		}
		Graphics.RenderMeshIndirect(in param, cacheMesh, argsBuffer);
	}
}

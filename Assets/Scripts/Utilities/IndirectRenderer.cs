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
	Dictionary<int, (int start, int count)> dictionary = new();
	List<int> list = new();

	public RenderParams param;



	// Properties

	public int Length => length;

	public int Capacity {
		get => structuredBuffer.count;
		set {
			value = Mathf.Max(MinCapacity, value);
			if (structuredBuffer.count != value) {
				length = Mathf.Min(length, value);

				var prev = structuredBuffer.LockBufferForWrite<T>(0, length);
				var temp = new NativeArray<T>(length, Allocator.Temp, Uninitialized);
				NativeArray<T>.Copy(prev, temp, length);
				structuredBuffer.UnlockBufferAfterWrite<T>(length);
				structuredBuffer.Dispose();

				structuredBuffer = new GraphicsBuffer(Structured, Locked, value, stride);
				structuredBuffer.SetData(temp, 0, 0, length);
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
		args[1] = length;
		args[2] = (int)cacheMesh.GetIndexStart(submesh);
		args[3] = (int)cacheMesh.GetBaseVertex(submesh);
		args[4] = 0;
		argsBuffer.UnlockBufferAfterWrite<int>(argsBuffer.count);
		structuredBuffer = new GraphicsBuffer(Structured, Locked, MinCapacity, stride);

		param = new RenderParams(material) {
			worldBounds = new Bounds(Vector3.zero, 1024f * Vector3.one),
			shadowCastingMode = ShadowCastingMode.On,
			receiveShadows = true,
			matProps = new MaterialPropertyBlock(),
		};
		param.matProps.SetBuffer(propID, structuredBuffer);
	}

	public void Dispose() {
		structuredBuffer.Release();
		argsBuffer.Release();
	}



	// Buffer Methods

	public NativeArray<T> LockBuffer() {
		return structuredBuffer.LockBufferForWrite<T>(0, length);
	}

	public NativeArray<T> LockExistingBuffer(int key) {
		if (!dictionary.TryGetValue(key, out var item)) return default;
		return structuredBuffer.LockBufferForWrite<T>(item.start, item.count);
	}

	public NativeArray<T> AllocateAndLockBuffer(int key, int count) {
		if (!dictionary.TryGetValue(key, out var item)) {
			int index = 0;
			foreach (var pair in dictionary) if (pair.Key < key) {
				index = Mathf.Max(index, pair.Value.start + pair.Value.count);
			}
			dictionary.Add(key, item = (index, 0));
		}
		if (structuredBuffer.count < length + count) Capacity = Mathf.Max(length * 2, length + count);
		if (item.start + item.count < length) {
			var array = new T[length - (item.start + item.count)];
			structuredBuffer.GetData(array, 0, item.start + item.count, array.Length);
			structuredBuffer.SetData(array, 0, item.start + item.count + count, array.Length);
		}
		foreach (var pair in dictionary) if (key < pair.Key) list.Add(pair.Key);
		foreach (var i in list) dictionary[i] = (dictionary[i].start + count, dictionary[i].count);
		list.Clear();
		length += count;

		dictionary[key] = (item.start, item.count + count);
		return structuredBuffer.LockBufferForWrite<T>(item.start + item.count, count);
	}



	public void UnlockBuffer() {
		structuredBuffer.UnlockBufferAfterWrite<T>(length);
	}

	public void UnlockBuffer(int count) {
		structuredBuffer.UnlockBufferAfterWrite<T>(count);
	}



	public void Clear() {
		dictionary.Clear();
		length = 0;
	}

	public void Clear(int key) {
		if (!dictionary.TryGetValue(key, out var item)) return;
		if (item.start + item.count < length) {
			var array = new T[length - (item.start + item.count)];
			structuredBuffer.GetData(array, 0, item.start + item.count, array.Length);
			structuredBuffer.SetData(array, 0, item.start, array.Length);
		}
		foreach (var pair in dictionary) if (key < pair.Key) list.Add(pair.Key);
		foreach (var i in list) dictionary[i] = (dictionary[i].start - item.count, dictionary[i].count);
		list.Clear();
		length -= item.count;

		dictionary.Remove(key);
	}



	// Draw Methods

	public void Draw() {
		var args = argsBuffer.LockBufferForWrite<int>(1, 1);
		args[0] = length;
		argsBuffer.UnlockBufferAfterWrite<int>(1);
		Graphics.RenderMeshIndirect(in param, cacheMesh, argsBuffer);
	}
}

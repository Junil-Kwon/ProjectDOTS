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

	const GraphicsBuffer.Target     Args       = GraphicsBuffer.Target    .IndirectArguments;
	const GraphicsBuffer.Target     Structured = GraphicsBuffer.Target    .Structured;
	const GraphicsBuffer.UsageFlags Locked     = GraphicsBuffer.UsageFlags.LockBufferForWrite;

	const int MinCapacity = 256;



	// Fields

	Mesh meshCached;
	int  stride;
	int  propID;
	int  length;

	GraphicsBuffer bufferArgs;
	GraphicsBuffer bufferStructured;
	Dictionary<int, (int, int)> hashmap = new();
	List<int> list = new();

	public RenderParams param;



	// Properties

	public int Length => length;

	public int Capacity {
		get => bufferStructured.count;
		set {
			value = Mathf.Max(MinCapacity, value);
			if (bufferStructured.count != value) {
				var array = new T[length = Mathf.Min(length, value)];
				bufferStructured.GetData(array, 0, 0, length);
				bufferStructured.Release();
				bufferStructured = new GraphicsBuffer(Structured, Locked, value, stride);
				bufferStructured.SetData(array, 0, 0, length);
				param.matProps.SetBuffer(propID, bufferStructured);
			}
		}
	}



	// Constructors

	public IndirectRenderer(Material material, Mesh mesh, int submesh = 0) {
		meshCached = mesh;
		stride     = Marshal.SizeOf<T>();
		propID     = Shader.PropertyToID($"_{typeof(T).Name}");
		length     = 0;

		bufferArgs       = new GraphicsBuffer(Args, Locked, 5, sizeof(int));
		bufferStructured = new GraphicsBuffer(Structured, Locked, MinCapacity, stride);
		var args = bufferArgs.LockBufferForWrite<int>(0, bufferArgs.count);
		args[0] = (int)meshCached.GetIndexCount(submesh);
		args[1] = length;
		args[2] = (int)meshCached.GetIndexStart(submesh);
		args[3] = (int)meshCached.GetBaseVertex(submesh);
		args[4] = 0;
		bufferArgs.UnlockBufferAfterWrite<int>(bufferArgs.count);

		param = new RenderParams(material);
		param.worldBounds	   = new Bounds(Vector3.zero, 1024f * Vector3.one);
		param.shadowCastingMode = ShadowCastingMode.On;
		param.receiveShadows    = true;
		param.matProps          = new MaterialPropertyBlock();
		param.matProps.SetBuffer(propID, bufferStructured);
	}

	public void Dispose() {
		bufferArgs      .Release();
		bufferStructured.Release();
	}



	// Buffer Methods

	public NativeArray<T> LockBuffer() {
		return bufferStructured.LockBufferForWrite<T>(0, length);
	}

	public NativeArray<T> LockBuffer(int key) {
		if (hashmap.TryGetValue(key, out var item)) {
			return bufferStructured.LockBufferForWrite<T>(item.Item1, item.Item2);
		}
		else return default;
	}

	public NativeArray<T> LockBuffer(int key, int count) {
		if (!hashmap.TryGetValue(key, out var item)) {
			int index = 0;
			foreach (var pair in hashmap) if (pair.Key < key) {
				index = Mathf.Max(index, pair.Value.Item1 + pair.Value.Item2);
			}
			hashmap.Add(key, item = (index, 0));
		}
		if (bufferStructured.count < length + count) {
			Capacity = Mathf.Max(length * 2, length + count);
		}
		if (item.Item1 + item.Item2 < length) {
			var array = new T[length - (item.Item1 + item.Item2)];
			bufferStructured.GetData(array, 0, item.Item1 + item.Item2,         array.Length);
			bufferStructured.SetData(array, 0, item.Item1 + item.Item2 + count, array.Length);
		}
		foreach (var pair in hashmap) if (key < pair.Key) list.Add(pair.Key);
		foreach (var i in list) hashmap[i] = (hashmap[i].Item1 + count, hashmap[i].Item2);
		list.Clear();
		length += count;

		hashmap[key] = (item.Item1, item.Item2 + count);
		return bufferStructured.LockBufferForWrite<T>(item.Item1 + item.Item2, count);
	}



	public void UnlockBuffer() {
		bufferStructured.UnlockBufferAfterWrite<T>(length);
	}

	public void UnlockBuffer(int count) {
		bufferStructured.UnlockBufferAfterWrite<T>(count);
	}



	public void Clear() {
		hashmap.Clear();
		length = 0;
	}

	public void Clear(int key) {
		if (!hashmap.TryGetValue(key, out var item)) return;
		if (item.Item1 + item.Item2 < length) {
			var array = new T[length - (item.Item1 + item.Item2)];
			bufferStructured.GetData(array, 0, item.Item1 + item.Item2, array.Length);
			bufferStructured.SetData(array, 0, item.Item1,              array.Length);
		}
		foreach (var pair in hashmap) if (key < pair.Key) list.Add(pair.Key);
		foreach (var i in list) hashmap[i] = (hashmap[i].Item1 - item.Item2, hashmap[i].Item2);
		list.Clear();
		length -= item.Item2;

		hashmap.Remove(key);
	}



	// Draw Methods

	public void Draw() {
		var args = bufferArgs.LockBufferForWrite<int>(0, 5);
		args[1] = length;
		bufferArgs.UnlockBufferAfterWrite<int>(5);
		Graphics.RenderMeshIndirect(in param, meshCached, bufferArgs);
	}
}

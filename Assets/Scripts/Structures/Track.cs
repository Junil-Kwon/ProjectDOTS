using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Track
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[Serializable]
public class Track : IEnumerable<(Vector3 point, bool curve)> {

	// Constants

	const float Epsilon = 0.0001f;
	const float Tolerance = 0.01f;
	const int MaxDepth = 3;



	// Fields

	[SerializeField] List<Vector3> m_Points = new();
	[SerializeField] List<bool> m_Curves = new();
	[SerializeField] List<float> m_Caches = new();
	bool m_IsDirty;



	// Properties

	List<Vector3> Points {
		get => m_Points;
	}
	List<bool> Curves {
		get => m_Curves;
	}
	List<float> Caches {
		get => m_Caches;
	}
	bool IsDirty {
		get => m_IsDirty;
		set => m_IsDirty = value;
	}

	public (Vector3 point, bool curve) this[int index] {
		get => (Points[index], Curves[index]);
		set {
			bool match = false;
			match = match || !Points[index].Equals(value.point);
			match = match || !Curves[index].Equals(value.curve);
			if (match) {
				Points[index] = value.point;
				Curves[index] = value.curve;
				IsDirty = true;
			}
		}
	}
	public int Count {
		get => Points.Count;
	}
	public float Distance {
		get {
			if (IsDirty) CalculateDistance();
			return (0 < Caches.Count) ? Caches[^1] : 0f;
		}
	}



	// List Methods

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}

	public IEnumerator<(Vector3 point, bool curve)> GetEnumerator() {
		for (int i = 0; i < Points.Count; i++) yield return (Points[i], Curves[i]);
	}

	public void Add() {
		var (point, curve) = (Vector3.zero, false);
		if (1 < Points.Count) point = Points[^1] * 2f - Points[^2];
		if (0 < Curves.Count) curve = Curves[^1];
		Add((point, curve));
	}

	public void Add((Vector3 point, bool curve) value) {
		Points.Add(value.point);
		Curves.Add(value.curve);
		Caches.Add(0f);
		IsDirty = true;
	}

	public void Insert(int index, (Vector3 point, bool curve) value) {
		Points.Insert(index, value.point);
		Curves.Insert(index, value.curve);
		Caches.Insert(index, 0f);
		IsDirty = true;
	}

	public void RemoveAt(int index) {
		Points.RemoveAt(index);
		Curves.RemoveAt(index);
		Caches.RemoveAt(index);
		IsDirty = true;
	}

	public void Clear() {
		Points.Clear();
		Curves.Clear();
		Caches.Clear();
		IsDirty = true;
	}

	public void CopyFrom(Track track) {
		Points.Clear();
		Curves.Clear();
		Caches.Clear();
		Points.AddRange(track.Points);
		Curves.AddRange(track.Curves);
		Caches.AddRange(track.Caches);
		IsDirty = track.IsDirty;
	}



	// Calculation Methods

	void CalculateDistance() {
		IsDirty = false;
		if (Caches.Count == 0) return;

		Caches[0] = 0f;
		for (int i = 0; i < Count - 1; i++) {
			float s;
			if (!Curves[i]) s = Vector3.Distance(Points[i], Points[i + 1]);
			else {
				var p0 = (0 <= i - 1) ? Points[i - 1] : Points[i] - (Points[i + 1] - Points[i]);
				var p1 = Points[i];
				var p2 = Points[i + 1];
				var p3 = (i + 2 < Points.Count) ? Points[i + 2] : p2 + (p2 - p1);
				s = ApproximateLength(p0, p1, p2, p3, 0f, 1f, 0);
			}
			Caches[i + 1] = Caches[i] + s;
		}
	}

	public Vector3 Evaluate(float s) {
		if (Points.Count == 0) return default;
		if (Points.Count == 1) return Points[0];
		if (s <= 0f) return Points[0];
		if (Distance <= s) return Points[^1];
	
		int a = 0, b = Points.Count - 1;
		while (a + 1 < b) {
			int m = (a + b) / 2;
			if (Caches[m] < s) a = m;
			else b = m;
		}
		float delta = Caches[b] - Caches[a];
		float t = (Epsilon < delta) ? (s - Caches[a]) / delta : 0f;
		if (!Curves[a]) return Vector3.Lerp(Points[a], Points[b], t);
		else {
			var p0 = (0 <= a - 1) ? Points[a - 1] : Points[a] - (Points[b] - Points[a]);
			var p1 = Points[a];
			var p2 = Points[b];
			var p3 = (b + 1 < Points.Count) ? Points[b + 1] : p2 + (p2 - p1);
			return CatmullRom(p0, p1, p2, p3, t);
		}
	}

	static float ApproximateLength(
		Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t0, float t1, int depth) {
		var a = CatmullRom(p0, p1, p2, p3, t0);
		var b = CatmullRom(p0, p1, p2, p3, t1);
		float chord = Vector3.Distance(a, b);
		float tm = Mathf.Lerp(t0, t1, 0.5f);
		var m = CatmullRom(p0, p1, p2, p3, tm);
		float s = Vector3.Distance(a, m) + Vector3.Distance(m, b);

		if ((s - chord) <= Tolerance || MaxDepth <= depth) return s;
		float i = ApproximateLength(p0, p1, p2, p3, t0, tm, depth + 1);
		float j = ApproximateLength(p0, p1, p2, p3, tm, t1, depth + 1);
		return i + j;
	}

	static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t1) {
		float t2 = t1 * t1;
		float t3 = t1 * t2;
		var a = 2f * p1;
		var b = (p2 - p0) * t1;
		var c = (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2;
		var d = (-p0 + 3f * p1 - 3f * p2 + p3) * t3;
		return 0.5f * (a + b + c + d);
	}
}

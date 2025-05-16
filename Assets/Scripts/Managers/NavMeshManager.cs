using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

using Unity.AI.Navigation;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// NavMesh Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/NavMesh Manager")]
[RequireComponent(typeof(NavMeshSurface))]
public class NavMeshManager : MonoSingleton<NavMeshManager> {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(NavMeshManager))]
		class NavMeshManagerEditor : EditorExtensions {
			NavMeshManager I => target as NavMeshManager;
			public override void OnInspectorGUI() {
				Begin("NavMesh Manager");

				LabelField("NavMesh", EditorStyles.boldLabel);
				BeginHorizontal();
				PrefixLabel("Bake NavMesh");
				if (Button("Clear All")) ClearAll();
				if (Button("Bake All" )) BakeAll ();
				EndHorizontal();

				End();
			}
		}
	#endif



	// Definitions

	const float SampleDistance = 1.0f;
	const float SampleMaxRange = 5.0f;



	// Fields

	NavMeshPath m_Path;
	readonly List<Vector3> m_List = new();



	// Properties

	static NavMeshPath Path {
		get => Instance.m_Path ??= new NavMeshPath();
		set => Instance.m_Path = value;
	}
	static List<Vector3> List => Instance.m_List;



	// Methods

	public static void ClearAll() {
		foreach (var surface in Instance.GetComponents<NavMeshSurface>()) surface.RemoveData();
	}

	public static void BakeAll() {
		foreach (var surface in Instance.GetComponents<NavMeshSurface>()) surface.BuildNavMesh();
	}

	public static List<Vector3> GetPath(Vector3 source, Vector3 target) {
		List.Clear();
		if (TryGetPath(source, target, List)) return List;
		return null;
	}

	public static bool TryGetPath(Vector3 source, Vector3 target, List<Vector3> path) {
		if (!NavMesh.CalculatePath(source, target, NavMesh.AllAreas, Path)) return false;
		if (Path.status != NavMeshPathStatus.PathComplete) return false;
		for (int i = 0; i < Path.corners.Length - 1; i++) {
			float distance = Vector3.Distance(Path.corners[i], Path.corners[i + 1]);
			for (float s = 0; s < distance; s += SampleDistance) {
				Vector3 point = Vector3.Lerp(Path.corners[i], Path.corners[i + 1], s / distance);
				if (NavMesh.SamplePosition(point, out var hit, SampleMaxRange, NavMesh.AllAreas)) {
					path.Add(hit.position);
				}
			}
		}
		Path.ClearCorners();
		return true;
	}
}

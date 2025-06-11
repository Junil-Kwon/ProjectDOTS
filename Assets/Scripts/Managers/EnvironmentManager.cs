using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Environment Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/Environment Manager")]
public sealed class EnvironmentManager : MonoSingleton<EnvironmentManager> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(EnvironmentManager))]
	class EnvironmentManagerEditor : EditorExtensions {
		EnvironmentManager I => target as EnvironmentManager;
		public override void OnInspectorGUI() {
			Begin("Environment Manager");

			if (!DirectionalLight) {
				var t0 = "No light found.";
				var t1 = "Please add a light to child object.";
				HelpBox($"{t0}\n{t1}", MessageType.Warning);
				Space();
			} else {
				LabelField("Directional Light", EditorStyles.boldLabel);
				Intensity = FloatField("Intensity",   Intensity);
				TimeOfDay = FloatField("Time of Day", TimeOfDay);
				Space();
			}
			LabelField("Point Light", EditorStyles.boldLabel);
			LightPrefab = ObjectField("Light Prefab", LightPrefab);
			Space();

			End();
		}
	}
	#endif



	// Fields

	Light m_DirectionalLight;
	[SerializeField] float m_Intensity = 2f;
	[SerializeField] float m_TimeOfDay;

	[SerializeField] Light m_LightPrefab;

	readonly List<Light> m_Lights = new();
	readonly List<Light> m_Pooled = new();



	// Properties

	static Light DirectionalLight =>
		Instance.m_DirectionalLight || TryGetComponentInChildren(out Instance.m_DirectionalLight) ?
		Instance.m_DirectionalLight : null;

	public static float Intensity {
		get => Instance.m_Intensity;
		set => Instance.m_Intensity = value;
	}
	public static float TimeOfDay {
		get => Instance.m_TimeOfDay;
		set {
			Instance.m_TimeOfDay = value;
			float normal = Mathf.Repeat(value, 1f);
			float offset = Mathf.Clamp01(Mathf.Cos((value - (int)value) * 2f * Mathf.PI) + 0.5f);
			DirectionalLight.transform.rotation = Quaternion.Euler(90f + normal * 360f, -90f, -90f);
			DirectionalLight.intensity = Intensity * offset;
		}
	}



	public static Light LightPrefab {
		get => Instance.m_LightPrefab;
		set => Instance.m_LightPrefab = value;
	}
	static List<Light> Lights => Instance.m_Lights;
	static List<Light> Pooled => Instance.m_Pooled;



	// Methods

	public static Light AddLight(Vector3 position, LightType type, float intensity = 2f) {
		Light light;
		if (0 < Pooled.Count) {
			light = Pooled[0];
			Pooled.RemoveAt(0);
			light.gameObject.SetActive(true);
			light.transform.SetPositionAndRotation(position, Quaternion.identity);
		} else {
			light = Instantiate(LightPrefab, position, Quaternion.identity);
		}
		light.type = type;
		light.intensity = intensity;
		Lights.Add(light);
		return light;
	}

	public static void RemoveLight(Light light) {
		if (Lights.Remove(light)) {
			light.gameObject.SetActive(false);
			Pooled.Add(light);
		}
	}



	// weather (rain, snow, fog, etc.)
	// wind direction

}

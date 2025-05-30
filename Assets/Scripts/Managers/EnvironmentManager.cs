using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Environment Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/Environment Manager")]
public class EnvironmentManager : MonoSingleton<EnvironmentManager> {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(EnvironmentManager))]
		class EnvironmentManagerEditor : EditorExtensions {
			EnvironmentManager I => target as EnvironmentManager;
			public override void OnInspectorGUI() {
				Begin("Environment Manager");

				if (!DirectionalLight) {
					HelpBox("No light found. Please add a light to child object.");
					Space();
				} else {
					LabelField("Directional Light", EditorStyles.boldLabel);
					Intensity = Slider    ("Intensity",   Intensity, 0f, 5f);
					TimeOfDay = FloatField("Time of Day", TimeOfDay);
					DayCurve  = CurveField("Day Curve",   DayCurve);
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
	[SerializeField] AnimationCurve m_DayCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField] Light m_LightPrefab;

	readonly List<Light> m_Lights = new();
	readonly List<Light> m_Pooled = new();



	// Properties

	static Transform Transform => Instance.transform;

	static Light DirectionalLight {
		get {
			if (!Instance.m_DirectionalLight) for (int i = 0; i < Transform.childCount; i++) {
				if (Transform.GetChild(i).TryGetComponent(out Instance.m_DirectionalLight)) break;
			}
			return Instance.m_DirectionalLight;
		}
	}
	public static float Intensity {
		get => Instance.m_Intensity;
		set => Instance.m_Intensity = Mathf.Clamp(value, 0f, 5f);
	}
	public static float TimeOfDay {
		get => Instance.m_TimeOfDay;
		set {
			Instance.m_TimeOfDay = value;
			float normal = DayCurve.Evaluate(value - (int)value);
			float offset = Mathf.Clamp01(Mathf.Cos((value - (int)value) * 2f * Mathf.PI) + 0.5f);
			DirectionalLight.transform.rotation = Quaternion.Euler(90f + normal * 360f, -90f, -90f);
			DirectionalLight.intensity = Intensity * offset;
		}
	}
	public static AnimationCurve DayCurve {
		get => Instance.m_DayCurve;
		set => Instance.m_DayCurve = value;
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

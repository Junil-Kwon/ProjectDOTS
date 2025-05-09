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

				LabelField("Fields", EditorStyles.boldLabel);
				Intensity = Slider    ("Intensity",   Intensity, 0f, 5f);
				TimeOfDay = FloatField("Time of Day", TimeOfDay);
				DayCurve  = CurveField("Day Curve",   DayCurve);
				Space();

				LabelField("Light", EditorStyles.boldLabel);
				LightPrefab = ObjectField("Light Prefab", LightPrefab);
				Space();

				End();
			}
		}
	#endif



	// Fields

	[SerializeField] float m_Intensity = 2f;
	[SerializeField] float m_TimeOfDay;
	[SerializeField] AnimationCurve m_DayCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField] Light m_LightPrefab;

	Light m_SunLight;
	List<Light> m_Lights = new();
	List<Light> m_Pooled = new();



	// Properties

	public static float Intensity {
		get => Instance.m_Intensity;
		set => Instance.m_Intensity = Mathf.Clamp(value, 0f, 5f);
	}

	public static float TimeOfDay {
		get => Instance.m_TimeOfDay;
		set {
			Instance.m_TimeOfDay = value;
			value = DayCurve.Evaluate(value - (int)value);
			Instance.transform.rotation = Quaternion.Euler(90f + value * 360f, -90f, -90f);
			value = Mathf.Clamp(Mathf.Cos(value * 2f * Mathf.PI) + 0.5f, 0f, 1f);
			Instance.SunLight.intensity = Intensity * value;
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

	Light SunLight => m_SunLight || TryGetComponent(out m_SunLight) ? m_SunLight : null;
	List<Light> Lights => m_Lights;
	List<Light> Pooled => m_Pooled;



	// Methods

	public static Light AddLight(Vector3 position, LightType type, float intensity = 2f) {
		Light light;
		if (0 < Instance.Pooled.Count) {
			light = Instance.Pooled[0];
			Instance.Pooled.RemoveAt(0);
			light.gameObject.SetActive(true);
			light.transform.SetPositionAndRotation(position, Quaternion.identity);
		}
		else light = Instantiate(LightPrefab, position, Quaternion.identity);
		light.type = type;
		light.intensity = intensity;
		Instance.Lights.Add(light);
		return light;
	}

	public static void RemoveLight(Light light) {
		if (Instance.Lights.Remove(light)) {
			light.gameObject.SetActive(false);
			Instance.Pooled.Add(light);
		}
	}



	// weather (rain, snow, fog, etc.)
	// wind direction

}

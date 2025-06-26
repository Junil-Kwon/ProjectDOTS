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

			if (MainLight == null) {
				HelpBox("Main Light is missing. Please add a Light to children of this GameObject.");
				Space();
			} else {
				LabelField("Main Light", EditorStyles.boldLabel);
				LightIntensity = FloatField("Light Intensity", LightIntensity);
				ShadowStrength = FloatField("Shadow Strength", ShadowStrength);
				PointLight = ObjectField("Point Light", PointLight);
				Space();
				LabelField("Environment", EditorStyles.boldLabel);
				DayLength = FloatField("Day Length",  DayLength);
				TimeOfDay = FloatField("Time of Day", TimeOfDay);
				Space();
			}
			End();
		}
	}
	#endif



	// Fields

	Light m_MainLight;
	[SerializeField] float m_LightIntensity = 1f;
	[SerializeField] float m_ShadowStrength = 1f;
	[SerializeField] Light m_PointLight;
	List<Light> m_Lights = new();
	List<Light> m_Pooled = new();

	[SerializeField] float m_DayLength = 300f;
	[SerializeField] float m_TimeOfDay = 0f;



	// Properties

	static Light MainLight =>
		Instance.m_MainLight || TryGetComponentInChildren(out Instance.m_MainLight) ?
		Instance.m_MainLight : null;

	static float LightIntensity {
		get => Instance.m_LightIntensity;
		set => Instance.m_LightIntensity = value;
	}
	static float ShadowStrength {
		get => Instance.m_ShadowStrength;
		set => Instance.m_ShadowStrength = value;
	}
	static Light PointLight {
		get => Instance.m_PointLight;
		set => Instance.m_PointLight = value;
	}
	static List<Light> Lights => Instance.m_Lights;
	static List<Light> Pooled => Instance.m_Pooled;



	public static float DayLength {
		get         => Instance.m_DayLength;
		private set => Instance.m_DayLength = value;
	}
	public static float TimeOfDay {
		get => Instance.m_TimeOfDay;
		private set {
			Instance.m_TimeOfDay = value;
			float normal = value % 1f;
			float lightRoll = normal switch {
				< 0.20f => Mathf.Lerp(000f, 000f, (normal - 0.00f) / 0.20f),
				< 0.80f => Mathf.Lerp(000f, 180f, (normal - 0.20f) / 0.60f),
				_       => Mathf.Lerp(180f, 360f, (normal - 0.80f) / 0.20f),
			};
			float offset = normal switch {
				< 0.20f => Mathf.Lerp(0.0f, 0.0f, (normal - 0.00f) / 0.20f),
				< 0.30f => Mathf.Lerp(0.0f, 1.0f, (normal - 0.20f) / 0.10f),
				< 0.70f => Mathf.Lerp(1.0f, 1.0f, (normal - 0.30f) / 0.40f),
				< 0.80f => Mathf.Lerp(1.0f, 0.0f, (normal - 0.70f) / 0.10f),
				_       => Mathf.Lerp(0.0f, 0.0f, (normal - 0.80f) / 0.20f),
			};
			float localX = -Mathf.Cos(lightRoll * Mathf.Deg2Rad);
			float localY = -Mathf.Sin(lightRoll * Mathf.Deg2Rad);
			float localZ = 0f;
			var localDirection = new Vector3(localX, localY, localZ).normalized;
			var worldDirection = Instance.transform.rotation * localDirection;

			MainLight.transform.rotation = Quaternion.LookRotation(worldDirection);
			MainLight.intensity = LightIntensity * offset;
		}
	}



	// Methods

	public static Light AddLight(Vector3 position, float intensity = 2f) {
		Light light;
		if (Pooled.Count == 0) {
			light = Instantiate(PointLight, position, Quaternion.identity);
		} else {
			light = Pooled[0];
			Pooled.RemoveAt(0);
			light.gameObject.SetActive(true);
			light.transform.SetPositionAndRotation(position, Quaternion.identity);
		}
		light.color = Color.white;
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



	static void Simulate() {
		TimeOfDay += Time.deltaTime / DayLength;
	}



	// Lifecycle

	void Update() {
		Simulate();
	}
}

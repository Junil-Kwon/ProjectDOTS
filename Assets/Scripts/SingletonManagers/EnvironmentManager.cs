using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif



public enum LightMode {
	DayNightCycle,
	Interior,
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Environment Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Singleton Manager/Environment Manager")]
public sealed class EnvironmentManager : MonoSingleton<EnvironmentManager> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(EnvironmentManager))]
	class EnvironmentManagerEditor : EditorExtensions {
		EnvironmentManager I => target as EnvironmentManager;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Directional Light", EditorStyles.boldLabel);
			DirectionalLight = ObjectField("Directional Light", DirectionalLight);
			if (DirectionalLight == null) {
				var message = string.Empty;
				message += $"Directional Light is missing.\n";
				message += $"Please add Directional Light to child of this object ";
				message += $"and assign here.";
				HelpBox(message, MessageType.Error);
				Space();
			} else {
				LightMode = EnumField("Light Mode", LightMode);
				TimeOfDay = FloatField("Time of Day", TimeOfDay);
				DayLength = FloatField("Day Length", DayLength);
				Space();
			}

			LabelField("Light Instance", EditorStyles.boldLabel);
			LightTemplate = ObjectField("Light Template", LightTemplate);
			if (LightTemplate == null) {
				var message = string.Empty;
				message += $"Light Template is missing.\n";
				message += $"Please assign a Light Template here.";
				HelpBox(message, MessageType.Info);
				Space();
			} else {
				int num = LightInstance.Count;
				int den = LightInstance.Count + LightPool.Count;
				LabelField("Light Pool", $"{num} / {den}");
				Space();
			}

			End();
		}
	}

	void OnDrawGizmos() {
		if (DirectionalLight) {
			var position = DirectionalLight.transform.position;
			var forward  = DirectionalLight.transform.forward;
			Gizmos.color = new Color(250f / 255f, 245f / 255f, 231f / 255f);
			Gizmos.DrawLine(position, position + forward * 1f);
		}
	}
	#endif



	// Fields

	[SerializeField] Light m_DirectionalLight;
	[SerializeField] LightMode m_LightMode = LightMode.DayNightCycle;
	[SerializeField] float m_TimeOfDay = 0.5f;
	[SerializeField] float m_DayLength = 300f;

	[SerializeField] Light m_LightTemplate;
	Dictionary<uint, (Light, float)> m_LightInstance = new();
	Stack<Light> m_LightPool = new();
	List<uint> m_IDBuffer = new();
	uint m_NextID;



	// Properties

	public static Quaternion Rotation {
		get => DirectionalLight.transform.rotation;
	}
	public static Vector3 Right   => Rotation * Vector3.right;
	public static Vector3 Up      => Rotation * Vector3.up;
	public static Vector3 Forward => Rotation * Vector3.forward;



	static Light DirectionalLight {
		get => Instance.m_DirectionalLight;
		set => Instance.m_DirectionalLight = value;
	}

	public static LightMode LightMode {
		get => Instance.m_LightMode;
		set {
			Instance.m_LightMode = value;
			switch (value) {
				case LightMode.DayNightCycle: {
					float normal = TimeOfDay % 1f;
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
					var localDirection = new Vector3(localX, localY, localZ);
					var worldDirection = Instance.transform.rotation * localDirection;
					DirectionalLight.transform.rotation = Quaternion.LookRotation(worldDirection);
					DirectionalLight.intensity = offset;
				} break;
				case LightMode.Interior: {
					DirectionalLight.transform.rotation = Quaternion.LookRotation(Vector3.down);
					DirectionalLight.intensity = 2.5f;
				} break;
			}
		}
	}
	public static float TimeOfDay {
		get => Instance.m_TimeOfDay;
		set => Instance.m_TimeOfDay = value;
	}
	public static float DayLength {
		get         => Instance.m_DayLength;
		private set => Instance.m_DayLength = value;
	}



	static Light LightTemplate {
		get => Instance.m_LightTemplate;
		set => Instance.m_LightTemplate = value;
	}
	static Dictionary<uint, (Light, float)> LightInstance {
		get => Instance.m_LightInstance;
	}
	static Stack<Light> LightPool {
		get => Instance.m_LightPool;
	}
	static List<uint> IDBuffer {
		get => Instance.m_IDBuffer;
	}
	static uint NextID {
		get => Instance.m_NextID;
		set => Instance.m_NextID = value;
	}



	// Instance Methods

	static (uint, Light) GetOrCreateInstance(float intensity, float duration) {
		Light instance;
		while (LightPool.TryPop(out instance) && instance == null);
		if (instance == null) instance = Instantiate(LightTemplate);
		instance.intensity = intensity;
		instance.gameObject.SetActive(true);
		while (++NextID == default || LightInstance.ContainsKey(NextID));
		LightInstance.Add(NextID, (instance, Time.time + duration));
		return (NextID, instance);
	}

	static void UpdateInstances() {
		foreach (var (lightID, (instance, endTime)) in LightInstance) {
			if (instance) {
				if (endTime <= Time.time) IDBuffer.Add(lightID);
			} else IDBuffer.Add(lightID);
		}
		if (0 < IDBuffer.Count) {
			foreach (var lightID in IDBuffer) RemoveInstance(lightID);
			IDBuffer.Clear();
		}
	}

	static void RemoveInstance(uint lightID) {
		var (instance, endTime) = LightInstance[lightID];
		if (instance) {
			var color = LightTemplate.color;
			if (instance.color != color) {
				instance.color = color;
			}
			var intensity = LightTemplate.intensity;
			if (instance.intensity != intensity) {
				instance.intensity = intensity;
			}
			var range = LightTemplate.range;
			if (instance.range != range) {
				instance.range = range;
			}
			instance.gameObject.SetActive(false);
			LightPool.Push(instance);
		}
		LightInstance.Remove(lightID);
	}



	// Light Methods

	public static uint AddLight(
		Color color, float intensity, Vector3 position, float duration = 1f) {
		var (lightID, instance) = GetOrCreateInstance(intensity, duration);
		instance.transform.position = position;
		instance.color = color;
		return lightID;
	}

	public static void SetLightColor(uint lightID, Color color) {
		if (LightInstance.TryGetValue(lightID, out var value)) {
			var (instance, endTime) = value;
			instance.color = color;
		}
	}

	public static void SetLightIntensity(uint lightID, float intensity) {
		if (LightInstance.TryGetValue(lightID, out var value)) {
			var (instance, endTime) = value;
			instance.intensity = intensity;
		}
	}

	public static void SetLightPosition(uint lightID, Vector3 position) {
		if (LightInstance.TryGetValue(lightID, out var value)) {
			var (instance, endTime) = value;
			instance.transform.position = position;
		}
	}

	public static void SetLightDuration(uint lightID, float duration) {
		if (LightInstance.TryGetValue(lightID, out var value)) {
			var (instance, endTime) = value;
			LightInstance[lightID] = (instance, Time.time + duration);
		}
	}

	public static void SetLightRange(uint lightID, float range) {
		if (LightInstance.TryGetValue(lightID, out var value)) {
			var (instance, endTime) = value;
			instance.range = range;
		}
	}

	public static void RemoveLight(uint id) {
		if (LightInstance.ContainsKey(id)) RemoveInstance(id);
	}



	// Lifecycle

	void LateUpdate() {
		//TimeOfDay += Time.deltaTime / DayLength;
		UpdateInstances();
	}
}

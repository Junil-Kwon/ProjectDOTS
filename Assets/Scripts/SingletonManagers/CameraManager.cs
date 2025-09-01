using UnityEngine;
using UnityEngine.UI;

using Unity.Mathematics;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Singleton Manager/Camera Manager")]
[RequireComponent(typeof(AudioListener))]
public sealed class CameraManager : MonoSingleton<CameraManager> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(CameraManager))]
	class CameraManagerEditor : EditorExtensions {
		CameraManager I => target as CameraManager;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Camera", EditorStyles.boldLabel);
			Camera = ObjectField("Camera", Camera);
			TempTextureRenderer = ObjectField("Temp Texture Renderer", TempTextureRenderer);
			Space();

			if (Camera == null) {
				var message = string.Empty;
				message += $"Camera is missing.\n";
				message += $"Please add Camera to child of this object ";
				message += $"and assign here.";
				HelpBox(message, MessageType.Error);
				Space();
			} else {
				LabelField("Camera Properties", EditorStyles.boldLabel);
				DollyDistance = Slider("Dolly Distance", DollyDistance, -128f, 128f);
				FieldOfView = FloatField("Field Of View", FieldOfView);
				OrthographicSize = FloatField("Orthographic Size", OrthographicSize);
				Projection = Slider("Projection", Projection, 0f, 1f);
				BeginHorizontal();
				PrefixLabel(" ");
				var l = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft };
				var r = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight };
				var s = new GUIStyle(GUI.skin.label) { fixedWidth = 50 };
				GUILayout.Label("< Perspective ", l);
				GUILayout.Label("Orthographic >", r);
				GUILayout.Label(" ", s);
				EndHorizontal();
				Space();

				BackgroundColor = ColorField("Background Color", BackgroundColor);
				CullingMask = LayerField("Culling Mask", CullingMask);
				Space();

				LabelField("Constraints", EditorStyles.boldLabel);
				FreezePosition = Toggle3("Freeze Position", FreezePosition);
				FreezeRotation = Toggle3("Freeze Rotation", FreezeRotation);
				Space();
			}

			End();
		}
	}
	#endif



	// Constants

	static readonly Vector2Int ReferenceResolution = new(640, 360);



	// Fields

	[SerializeField] Camera m_Camera;
	[SerializeField] RawImage m_TempTextureRenderer;

	[SerializeField] float m_DollyDistance = -48f;
	[SerializeField] float m_OrthoMultiplier = 1f;
	[SerializeField] float m_Projection = 1f;
	[SerializeField] constraints m_Constraints;
	Vector2Int m_ScreenResolution = default;

	float m_ShakeStrength;
	float m_ShakeDuration;
	bool2 m_ShakeDirection;



	// Properties

	public static Vector3 Position {
		get => Instance.transform.position;
		set {
			if (math.any(FreezePosition)) {
				var prev = Position;
				var next = value;
				if (FreezePosition.x) next.x = prev.x;
				if (FreezePosition.y) next.y = prev.y;
				if (FreezePosition.z) next.z = prev.z;
				value = next;
			}
			Instance.transform.position = value;
		}
	}
	public static Quaternion Rotation {
		get => Instance.transform.rotation;
		set {
			if (math.any(FreezeRotation)) {
				var prev = Rotation.eulerAngles;
				var next = value.eulerAngles;
				if (FreezeRotation.x) next.x = prev.x;
				if (FreezeRotation.y) next.y = prev.y;
				if (FreezeRotation.z) next.z = prev.z;
				value = Quaternion.Euler(next);
			}
			Instance.transform.rotation = value;
		}
	}
	public static Vector3 EulerRotation {
		get => Rotation.eulerAngles;
		set => Rotation = Quaternion.Euler(value);
	}
	public static float Yaw {
		get => EulerRotation.y;
		set {
			var eulerRotation = EulerRotation;
			EulerRotation = new(eulerRotation.x, value, eulerRotation.z);
		}
	}
	public static Vector3 Right   => Rotation * Vector3.right;
	public static Vector3 Up      => Rotation * Vector3.up;
	public static Vector3 Forward => Rotation * Vector3.forward;



	static Camera Camera {
		get => Instance.m_Camera;
		set => Instance.m_Camera = value;
	}
	static RawImage TempTextureRenderer {
		get => Instance.m_TempTextureRenderer;
		set => Instance.m_TempTextureRenderer = value;
	}
	static RenderTexture MainTexture {
		get => Camera.targetTexture;
		set => Camera.targetTexture = value;
	}
	static RenderTexture TempTexture {
		get => TempTextureRenderer.texture as RenderTexture;
		set => TempTextureRenderer.texture = value;
	}

	static Vector2 Offset {
		get => Camera.transform.localPosition;
		set => Camera.transform.localPosition = new(value.x, value.y, DollyDistance);
	}
	public static float DollyDistance {
		get => Instance.m_DollyDistance;
		set {
			Instance.m_DollyDistance = value = Mathf.Clamp(value, -128f, 128f);
			Camera.transform.localPosition = new(Offset.x, Offset.y, value);
		}
	}

	public static float FieldOfView {
		get => Camera.fieldOfView;
		set {
			Camera.fieldOfView = Mathf.Clamp(value, 1f, 179f);
			Projection = Projection;
		}
	}
	public static float OrthographicSize {
		get => Camera.orthographicSize / OrthoMultiplier;
		set {
			value = Mathf.Max(0.01f, value);
			Camera.orthographicSize = value * OrthoMultiplier;
			Projection = Projection;
		}
	}
	static float OrthoMultiplier {
		get => Instance.m_OrthoMultiplier;
		set {
			float orthographicSize = OrthographicSize;
			Instance.m_OrthoMultiplier = value = Mathf.Max(0.01f, value);
			Camera.orthographicSize = orthographicSize * value;
			Projection = Projection;
		}
	}
	public static float Projection {
		get => Instance.m_Projection;
		set {
			float fov    = Camera.fieldOfView;
			float aspect = Camera.aspect;
			float zNear  = Camera.nearClipPlane;
			float zFar   = Camera.farClipPlane;
			float wHalf  = Camera.orthographicSize * aspect;
			float hHalf  = Camera.orthographicSize;

			Instance.m_Projection = value = Mathf.Clamp01(value);
			var matrix = Camera.projectionMatrix;
			var a = Matrix4x4.Perspective(fov, aspect, zNear, zFar);
			var b = Matrix4x4.Ortho(-wHalf, wHalf, -hHalf, hHalf, zNear, zFar);
			float t = Mathf.Pow(Mathf.Max(0.01f, value), 0.03f);
			for (int i = 0; i < 16; i++) matrix[i] = Mathf.Lerp(a[i], b[i], t);
			Camera.projectionMatrix = matrix;
		}
	}

	public static Color BackgroundColor {
		get => Camera.backgroundColor;
		set => Camera.backgroundColor = value;
	}
	public static int CullingMask {
		get => Camera.cullingMask;
		set => Camera.cullingMask = value;
	}

	public static constraints Constraints {
		get => Instance.m_Constraints;
		set => Instance.m_Constraints = value;
	}
	public static bool3 FreezePosition {
		get => Constraints.position;
		set => Constraints = new constraints(value, Constraints.rotation);
	}
	public static bool3 FreezeRotation {
		get => Constraints.rotation;
		set => Constraints = new constraints(Constraints.position, value);
	}



	static Vector2Int ScreenResolution {
		get => Instance.m_ScreenResolution;
		set => Instance.m_ScreenResolution = value;
	}

	static float ShakeStrength {
		get => Instance.m_ShakeStrength;
		set => Instance.m_ShakeStrength = value;
	}
	static float ShakeDuration {
		get => Instance.m_ShakeDuration;
		set => Instance.m_ShakeDuration = value;
	}
	static bool2 ShakeDirection {
		get => Instance.m_ShakeDirection;
		set => Instance.m_ShakeDirection = value;
	}



	// Methods

	public static Vector3 WorldToScreenPoint(Vector3 position) {
		return Camera.WorldToScreenPoint(position);
	}

	public static Vector3 WorldToViewportPoint(Vector3 position) {
		return Camera.WorldToViewportPoint(position);
	}

	public static Vector3 ScreenToWorldPoint(Vector3 position) {
		return Camera.ScreenToWorldPoint(position);
	}

	public static Vector3 ScreenToViewportPoint(Vector3 position) {
		return Camera.ScreenToViewportPoint(position);
	}

	public static Vector3 ViewportToWorldPoint(Vector3 position) {
		return Camera.ViewportToWorldPoint(position);
	}

	public static Vector3 ViewportToScreenPoint(Vector3 position) {
		return Camera.ViewportToScreenPoint(position);
	}

	public static void SetTransition(int cullingMask, float transition = 1f) {
		int mainCullingMask = Camera.cullingMask;
		int tempCullingMask = cullingMask;
		var mainTargetTexture = Camera.targetTexture;
		var tempTargetTexture = TempTexture;
		if (0f < transition && transition < 1f) {
			Camera.cullingMask = tempCullingMask;
			Camera.targetTexture = tempTargetTexture;
			Camera.Render();
			Camera.cullingMask = mainCullingMask;
			Camera.targetTexture = mainTargetTexture;
			TempTextureRenderer.color = new(1f, 1f, 1f, transition);
		} else {
			TempTextureRenderer.color = new(1f, 1f, 1f, 0f);
		}
	}



	static void UpdateScreenResolution() {
		bool match = false;
		match = match || ScreenResolution.x != UnityEngine.Screen.width;
		match = match || ScreenResolution.y != UnityEngine.Screen.height;
		if (match) {
			ScreenResolution = new(UnityEngine.Screen.width, UnityEngine.Screen.height);
			float aspect = (float)UnityEngine.Screen.width / UnityEngine.Screen.height;
			Camera.aspect = aspect;
			float xRatio = (float)UnityEngine.Screen.width / ReferenceResolution.x;
			float yRatio = (float)UnityEngine.Screen.height / ReferenceResolution.y;
			float multiplier = Mathf.Max(1, (int)Mathf.Min(xRatio, yRatio));
			OrthoMultiplier = yRatio / multiplier;

			MainTexture.Release();
			MainTexture.width  = UnityEngine.Screen.width;
			MainTexture.height = UnityEngine.Screen.height;
			MainTexture.Create();
			TempTexture.Release();
			TempTexture.width  = UnityEngine.Screen.width;
			TempTexture.height = UnityEngine.Screen.height;
			TempTexture.Create();
		}
	}



	public static void ShakeCamera(float strength, float duration, bool2 direction = default) {
		ShakeStrength = Mathf.Max(0f, strength);
		ShakeDuration = Mathf.Max(0f, duration);
		ShakeDirection = direction.Equals(default) ? new bool2(true, true) : direction;
	}

	public static void StopShaking() {
		Offset = default;
		ShakeDuration = 0f;
	}

	static void UpdateCameraShake() {

	}



	// Lifecycle

	void LateUpdate() {
		UpdateScreenResolution();
		UpdateCameraShake();
	}
}

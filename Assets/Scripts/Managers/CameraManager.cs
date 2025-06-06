using UnityEngine;
using UnityEngine.Rendering.Universal;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Transforms;
using Unity.Physics;
using Unity.NetCode;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/Camera Manager")]
public sealed class CameraManager : MonoSingleton<CameraManager> {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(CameraManager))]
		class CameraManagerEditor : EditorExtensions {
			CameraManager I => target as CameraManager;
			public override void OnInspectorGUI() {
				Begin("Camera Manager");

				if (!MainCamera) {
					var t0 = "No camera found.";
					var t1 = "Please add a camera to child object.";
					HelpBox($"{t0}\n{t1}", MessageType.Warning);
					Space();
				} else {
					LabelField("Camera", EditorStyles.boldLabel);
					RenderTextureSize = Vector2Field("Render Texture Size", RenderTextureSize);
					FocusDistance     = Slider      ("Focus Distance",      FocusDistance, 0f, 255f);
					FieldOfView       = FloatField  ("Field Of View",       FieldOfView);
					OrthographicSize  = FloatField  ("Orthographic Size",   OrthographicSize);
					Projection        = Slider      ("Projection",          Projection, 0f, 1f);
					BeginHorizontal();
					PrefixLabel(" ");
					var l = new GUIStyle(GUI.skin.label) { alignment  = TextAnchor.MiddleLeft  };
					var r = new GUIStyle(GUI.skin.label) { alignment  = TextAnchor.MiddleRight };
					var s = new GUIStyle(GUI.skin.label) { fixedWidth = 50 };
					GUILayout.Label("< Perspective ", l);
					GUILayout.Label("Orthographic >", r);
					GUILayout.Label(" ", s);
					EndHorizontal();
					Space();
				}
				if (CameraData) {
					PostProcessing = Toggle("Post Processing", PostProcessing);
					BeginDisabledGroup(!PostProcessing);
					AntiAliasing = Toggle("Anti Aliasing", AntiAliasing);
					EndDisabledGroup();
					Space();
				}
				LabelField("Constraints", EditorStyles.boldLabel);
				FreezePosition = Toggle3("Freeze Position", FreezePosition);
				FreezeRotation = Toggle3("Freeze Rotation", FreezeRotation);
				Space();

				End();
			}
		}
	#endif



	// Constants

	public const float DefaultFocusDistance = 64f;
	public const float DefaultProjection    =  1f;



	// Fields

	Camera m_MainCamera;
	UniversalAdditionalCameraData m_CameraData;

	[SerializeField] float       m_FocusDistance = DefaultFocusDistance;
	[SerializeField] float       m_Projection    = DefaultProjection;
	[SerializeField] constraints m_Constraints   = new();



	// Properties

	public static Transform Transform => Instance.transform;

	public static Vector3 Position {
		get => Transform.position;
		set => Transform.position = value;
	}
	public static Quaternion Rotation {
		get => Transform.rotation;
		set => Transform.rotation = value;
	}
	public static Vector3 EulerRotation {
		get => Rotation.eulerAngles;
		set => Rotation = Quaternion.Euler(value);
	}
	public static float Yaw {
		get => EulerRotation.y;
		set => EulerRotation = new Vector3(EulerRotation.x, value, EulerRotation.z);
	}
	public static Vector3 Right   => Rotation * Vector3.right;
	public static Vector3 Up      => Rotation * Vector3.up;
	public static Vector3 Forward => Rotation * Vector3.forward;

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



	static Camera MainCamera =>
		Instance.m_MainCamera || TryGetComponentInChildren(out Instance.m_MainCamera) ?
		Instance.m_MainCamera : null;

	static UniversalAdditionalCameraData CameraData =>
		Instance.m_CameraData || TryGetComponentInChildren(out Instance.m_CameraData) ?
		Instance.m_CameraData : null;

	public static Vector2 RenderTextureSize {
		get {
			var target = MainCamera.targetTexture;
			if (target) return new Vector2(target.width, target.height);
			else        return new Vector2(Screen.width, Screen.height);
		}
		set {
			var target = MainCamera.targetTexture;
			if (target) {
				target.Release();
				target.width  = (int)Mathf.Max(1f, value.x);
				target.height = (int)Mathf.Max(1f, value.y);
				target.Create();
			}
		}
	}
	public static float FocusDistance {
		get => Instance.m_FocusDistance;
		set {
			value = Mathf.Clamp(value, 0f, 255f);
			Instance.m_FocusDistance = value;
			MainCamera.transform.localPosition = new Vector3(0, 0, -value);
		}
	}
	public static float FieldOfView {
		get => MainCamera.fieldOfView;
		set {
			value = Mathf.Clamp(value, 1f, 179f);
			MainCamera.fieldOfView = value;
			Projection = Projection;
		}
	}
	public static float OrthographicSize {
		get => MainCamera.orthographicSize;
		set {
			value = Mathf.Clamp(value, 1f, 179f);
			MainCamera.orthographicSize = value;
			Projection = Projection;
		}
	}
	public static float Projection {
		get => Instance.m_Projection;
		set {
			value = Mathf.Clamp(value, 0f, 1f);
			Instance.m_Projection = value;
			float aspect =  MainCamera.aspect;
			float near   =  MainCamera.nearClipPlane;
			float far    =  MainCamera. farClipPlane;
			float left   = -OrthographicSize * aspect;
			float right  =  OrthographicSize * aspect;
			float bottom = -OrthographicSize;
			float top    =  OrthographicSize;
			var a = Matrix4x4.Perspective(FieldOfView, aspect, near, far);
			var b = Matrix4x4.Ortho(left, right,  bottom, top, near, far);
			var projection = MainCamera.projectionMatrix;
			for (int i = 0; i < 16; i++) projection[i] = Mathf.Lerp(a[i], b[i], value);
			MainCamera.projectionMatrix = projection;
		}
	}

	static bool PostProcessing {
		get => CameraData.renderPostProcessing;
		set => CameraData.renderPostProcessing = value;
	}
	static bool AntiAliasing {
		get => CameraData.antialiasing != AntialiasingMode.None;
		set {
			const AntialiasingMode None = AntialiasingMode.None;
			const AntialiasingMode SMAA = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
			CameraData.antialiasing = value ? SMAA : None;
		}
	}



	// Lifecycle

	void Start() => Projection = Projection;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Camera Manager System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class CameraManagerSystem : SystemBase {

	[BurstCompile]
	protected override void OnCreate() {
		RequireForUpdate<PhysicsWorldSingleton>();
		RequireForUpdate<GhostOwnerIsLocal>();
	}

	[BurstDiscard]
	protected override void OnUpdate() {
		var world = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
		foreach (var (transform, blob) in SystemAPI
			.Query<RefRO<LocalToWorld>, RefRO<CreatureBlob>>().WithAll<GhostOwnerIsLocal>()) {

			var point = transform.ValueRO.Position;
			var ray = new RaycastInput {
				Start = point + new float3(0f,  0.5f, 0f),
				End   = point + new float3(0f, -5.0f, 0f),
				Filter = new CollisionFilter {
					BelongsTo    = ~(0u),
					CollidesWith = ~(1u << (int)PhysicsCategory.Creature),
				}
			};
			if (world.CastRay(ray, out var hit)) point = hit.Position;
			point += new float3(0f, blob.ValueRO.Value.Value.Height + 1f, 0f);

			var delta = (Vector3)point - CameraManager.Position;
			CameraManager.Position += 5f * SystemAPI.Time.DeltaTime * delta;
			CameraManager.Yaw += InputManager.LookDirection.x * InputManager.MouseSensitivity * 0.1f;
			break;
		}
	}
}

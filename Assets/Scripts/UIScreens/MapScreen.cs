using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Map Screen
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("UI Screen/Map Screen")]
public sealed class MapScreen : ScreenBase {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(MapScreen))]
	class MapScreenEditor : EditorExtensions {
		MapScreen I => target as MapScreen;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Selected", EditorStyles.boldLabel);
			I.DefaultSelected = ObjectField("Default Selected", I.DefaultSelected);
			Space();

			LabelField("Map", EditorStyles.boldLabel);
			I.MapScale = FloatField("Map Scale", I.MapScale);
			I.Anchor = ObjectField("Anchor", I.Anchor);
			for (int i = 0; i < I.MapImage.Length; i++) {
				I.MapImage[i] = ObjectField($"Map Image {i:00}", I.MapImage[i]);
			}
			I.IconTemplate = ObjectField("Icon Template", I.IconTemplate);
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] float m_MapScale = 1f;
	[SerializeField] GameObject m_Anchor;
	[SerializeField] Image[] m_MapImage = new Image[32];

	[SerializeField] Image m_IconTemplate;
	List<Image> m_IconList = new();
	Stack<Image> m_IconPool = new();



	// Properties

	public override bool IsPrimary => false;
	public override bool IsOverlay => true;
	public override bool UseScreenBlur => true;



	float MapScale {
		get => m_MapScale;
		set => m_MapScale = value;
	}
	GameObject Anchor {
		get => m_Anchor;
		set => m_Anchor = value;
	}
	Image[] MapImage {
		get => m_MapImage;
	}



	Image IconTemplate {
		get => m_IconTemplate;
		set => m_IconTemplate = value;
	}
	List<Image> IconList {
		get => m_IconList;
	}
	Stack<Image> IconPool {
		get => m_IconPool;
	}



	// Instance Methods

	Image GetOrCreateInstance() {
		Image instance;
		while (IconPool.TryPop(out instance) && instance == null);
		if (instance == null) instance = Instantiate(IconTemplate, Anchor.transform);
		instance.gameObject.SetActive(true);
		return instance;
	}

	void RemoveInstance(Image instance) {
		instance.gameObject.SetActive(false);
		IconPool.Push(instance);
	}



	// Lifecycle

	protected override void Update() {
		bool match = true;
		match = match && !InputManager.GetKey(KeyAction.Map);
		match = match && UIManager.CurrentScreen == Screen.Map;
		if (match) Back();
	}

	void LateUpdate() {
		var cameraPosition = CameraManager.Position;
		var point = CameraManager.WorldToScreenPoint(cameraPosition);
		var north = CameraManager.WorldToScreenPoint(cameraPosition + Vector3.forward);
		var direction = (north - point).normalized;
		float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
		var anchorPosition = new Vector3(-cameraPosition.x, -cameraPosition.z, 0f);
		var anchorRotation = Quaternion.Euler(0f, 0f, angle);
		anchorPosition = anchorRotation * anchorPosition * MapScale;
		Anchor.transform.SetLocalPositionAndRotation(anchorPosition, anchorRotation);
		var rotation = Quaternion.Euler(0f, 0f, -angle);

		int layer = 0;
		for (int i = 0; i < MapImage.Length; i++) {
			bool match = (CameraManager.CullingMask & (1 << i)) != 0;
			float color = match ? 1f - (0.05f * layer++) : 0f;
			float alpha = match ? 1f : 0f;
			if (MapImage[i].color.a != alpha) {
				MapImage[i].color = new Color(color, color, color, alpha);
			}
		}
		int count = 0;
		Image GetInstance() {
			if (IconList.Count == count) IconList.Add(GetOrCreateInstance());
			return IconList[count++];
		}
		if (GameManager.LocalPlayer != null) {
			var instance = GetInstance();
			var position = GameManager.LocalPlayer.Value.Transform.Position;
			position = new Vector3(position.x, position.z, 0f) * MapScale;
			instance.transform.SetLocalPositionAndRotation(position, rotation);
		}
		for (int i = 0; i < GameManager.RemotePlayers.Count; i++) {
			var instance = GetInstance();
			var position = GameManager.RemotePlayers[i].Transform.Position;
			position = new Vector3(position.x, position.z, 0f) * MapScale;
			instance.transform.SetLocalPositionAndRotation(position, rotation);
		}
		while (count < IconList.Count) {
			int l = IconList.Count - 1;
			RemoveInstance(IconList[l]);
			IconList.RemoveAt(l);
		}
	}
}

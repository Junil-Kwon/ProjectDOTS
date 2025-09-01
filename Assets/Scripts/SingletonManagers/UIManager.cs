using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif



public enum Screen {
	Achievements,
	Alert,
	Bestiary,
	Confirmation,
	Credits,
	Debug,
	Dialogue,
	Fade,
	Game,
	MainMenu,
	Map,
	Menu,
	Multiplayer,
	Options,
}

public static class ScreenExtensions {
	public static Type ToType(this Screen screen) => screen switch {
		Screen.Achievements => typeof(AchievementsScreen),
		Screen.Alert        => typeof(AlertScreen),
		Screen.Bestiary     => typeof(BestiaryScreen),
		Screen.Confirmation => typeof(ConfirmationScreen),
		Screen.Credits      => typeof(CreditsScreen),
		Screen.Debug        => typeof(DebugScreen),
		Screen.Dialogue     => typeof(DialogueScreen),
		Screen.Fade         => typeof(FadeScreen),
		Screen.Game         => typeof(GameScreen),
		Screen.MainMenu     => typeof(MainMenuScreen),
		Screen.Map          => typeof(MapScreen),
		Screen.Menu         => typeof(MenuScreen),
		Screen.Multiplayer  => typeof(MultiplayerScreen),
		Screen.Options      => typeof(OptionsScreen),
		_ => default,
	};

	public static Screen ToScreen(this ScreenBase screenBase) => screenBase switch {
		_ when screenBase is AchievementsScreen => Screen.Achievements,
		_ when screenBase is AlertScreen        => Screen.Alert,
		_ when screenBase is BestiaryScreen     => Screen.Bestiary,
		_ when screenBase is ConfirmationScreen => Screen.Confirmation,
		_ when screenBase is CreditsScreen      => Screen.Credits,
		_ when screenBase is DebugScreen        => Screen.Debug,
		_ when screenBase is DialogueScreen     => Screen.Dialogue,
		_ when screenBase is FadeScreen         => Screen.Fade,
		_ when screenBase is GameScreen         => Screen.Game,
		_ when screenBase is MainMenuScreen     => Screen.MainMenu,
		_ when screenBase is MapScreen          => Screen.Map,
		_ when screenBase is MenuScreen         => Screen.Menu,
		_ when screenBase is MultiplayerScreen  => Screen.Multiplayer,
		_ when screenBase is OptionsScreen      => Screen.Options,
		_ => default,
	};
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// UI Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Singleton Manager/UI Manager")]
[RequireComponent(typeof(UnityEngine.Screen), typeof(CanvasScaler))]
public sealed class UIManager : MonoSingleton<UIManager> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(UIManager))]
	class UIManagerEditor : EditorExtensions {
		UIManager I => target as UIManager;
		public override void OnInspectorGUI() {
			Begin();

			LabelField("Renderer", EditorStyles.boldLabel);
			MainTextureRenderer = ObjectField("Main Texture Renderer", MainTextureRenderer);
			TempTextureRenderer = ObjectField("Temp Texture Renderer", TempTextureRenderer);
			Space();

			LabelField("Text Instance", EditorStyles.boldLabel);
			TextTemplate = ObjectField("Text Template", TextTemplate);
			if (TextTemplate == null) {
				var message = string.Empty;
				message += $"Text Template is missing.\n";
				message += $"Please assign a Text Template here.";
				HelpBox(message, MessageType.Info);
				Space();
			} else {
				int num = TextInstance.Count;
				int den = TextInstance.Count + TextPool.Count;
				LabelField("Text Pool", $"{num} / {den}");
				Space();
			}

			LabelField("Screen", EditorStyles.boldLabel);
			if (ScreenBases?.Length != ScreenCount) ScreenBases = LoadScreenBases();
			foreach (var screenBase in ScreenBases) {
				BeginHorizontal();
				if (screenBase == null) {
					var screen = (Screen)Array.IndexOf(ScreenBases, screenBase);
					PrefixLabel($"{screen} Screen");
					bool value = EditorGUILayout.Toggle(false, GUILayout.Width(14));
					if (value) ScreenBases = LoadScreenBases();
					ObjectField(null as GameObject);
					EndHorizontal();
					var message = string.Empty;
					message += $"{screen} Screen is missing.\n";
					message += $"Please add {screen} Screen to child of this object ";
					HelpBox(message, MessageType.Error);
					BeginHorizontal();
				} else {
					var screen = screenBase.ToScreen();
					PrefixLabel($"{screen} Screen");
					bool match = screenBase.gameObject.activeSelf;
					bool value = EditorGUILayout.Toggle(match, GUILayout.Width(14));
					ObjectField(screenBase.gameObject);
					if (match != value) foreach (var screen8ase in ScreenBases) {
						if (!screen8ase) continue;
						screen8ase.gameObject.SetActive(screenBase == screen8ase && value);
					}
				}
				EndHorizontal();
			}
			Space();

			End();
		}
	}
	#endif



	// Constants

	static readonly int ScreenCount = Enum.GetValues(typeof(Screen)).Length;

	public static readonly Vector2Int ReferenceResolution = new(640, 360);
	public static readonly Vector2Int[] ResolutionPresets = new Vector2Int[] {
		new(0640, 0360),
		new(1280, 0720),
		new(1920, 1080),
		new(2560, 1440),
		new(3840, 2160),
	};

	static readonly int BlurOffset = Shader.PropertyToID("_Blur_Offset");



	// Fields

	[SerializeField] RawImage m_MainTextureRenderer;
	[SerializeField] RawImage m_TempTextureRenderer;
	bool m_ScreenBlur = false;

	[SerializeField] TextMeshPro m_TextTemplate;
	Dictionary<uint, (TextMeshPro, float)> m_TextInstance = new();
	Stack<TextMeshPro> m_TextPool = new();
	List<uint> m_IDBuffer = new();
	uint m_NextID;

	CanvasScaler m_ScreenScaler;
	Vector2Int m_ScreenResolution;
	ScreenBase[] m_ScreenBases;
	Stack<ScreenBase> m_ScreenStack = new();



	// Properties

	static RawImage MainTextureRenderer {
		get => Instance.m_MainTextureRenderer;
		set => Instance.m_MainTextureRenderer = value;
	}
	static RawImage TempTextureRenderer {
		get => Instance.m_TempTextureRenderer;
		set => Instance.m_TempTextureRenderer = value;
	}
	static bool ScreenBlur {
		get => Instance.m_ScreenBlur;
		set {
			Instance.m_ScreenBlur = value;
			MainTextureRenderer.material.SetFloat(BlurOffset, value ? CanvasScale : 0f);
			TempTextureRenderer.material.SetFloat(BlurOffset, value ? CanvasScale : 0f);
		}
	}



	static TextMeshPro TextTemplate {
		get => Instance.m_TextTemplate;
		set => Instance.m_TextTemplate = value;
	}
	static Dictionary<uint, (TextMeshPro, float)> TextInstance {
		get => Instance.m_TextInstance;
	}
	static Stack<TextMeshPro> TextPool {
		get => Instance.m_TextPool;
	}
	static List<uint> IDBuffer {
		get => Instance.m_IDBuffer;
	}
	static uint NextID {
		get => Instance.m_NextID;
		set => Instance.m_NextID = value;
	}



	static CanvasScaler CanvasScaler => !Instance.m_ScreenScaler ?
		Instance.m_ScreenScaler = Instance.GetOwnComponent<CanvasScaler>() :
		Instance.m_ScreenScaler;

	static float CanvasScale {
		get => CanvasScaler.scaleFactor;
		set => CanvasScaler.scaleFactor = value;
	}
	public static Vector2Int ScreenResolution {
		get         => Instance.m_ScreenResolution;
		private set => Instance.m_ScreenResolution = value;
	}

	static ScreenBase[] ScreenBases {
		get => Instance.m_ScreenBases;
		set => Instance.m_ScreenBases = value;
	}
	static Stack<ScreenBase> ScreenStack {
		get => Instance.m_ScreenStack;
	}
	public static Screen? CurrentScreen {
		get => ScreenStack.TryPeek(out var overlay) ? overlay.ToScreen() : null;
	}



	static GameObject SelectedGameObject {
		get => EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
		set => EventSystem.current.SetSelectedGameObject(value);
	}
	public static Selectable Selected {
		get {
			var gameObject = SelectedGameObject;
			if (gameObject && gameObject.TryGetComponent(out Selectable selectable)) {
				return selectable;
			} else return null;
		}
		set => SelectedGameObject = !value ? null : value.gameObject;
	}



	// Instance Methods

	static (uint, TextMeshPro) GetOrCreateInstance(float duration, int layer) {
		TextMeshPro instance;
		while (TextPool.TryPop(out instance) && instance == null);
		if (instance == null) instance = Instantiate(TextTemplate);
		instance.gameObject.layer = layer;
		instance.gameObject.SetActive(true);
		while (++NextID == default || TextInstance.ContainsKey(NextID));
		TextInstance.Add(NextID, (instance, Time.time + duration));
		return (NextID, instance);
	}

	static void UpdateInstances() {
		foreach (var (textID, (instance, endTime)) in TextInstance) {
			if (instance) {
				instance.transform.rotation = CameraManager.Rotation;
				if (endTime <= Time.time) IDBuffer.Add(textID);
			} else IDBuffer.Add(textID);
		}
		if (0 < IDBuffer.Count) {
			foreach (var textID in IDBuffer) RemoveInstance(textID);
			IDBuffer.Clear();
		}
	}

	static void RemoveInstance(uint textID) {
		var (instance, endTime) = TextInstance[textID];
		if (instance) {
			instance.gameObject.SetActive(false);
			TextPool.Push(instance);
		}
		TextInstance.Remove(textID);
	}



	// Text Methods

	public static uint AddText(
		string text, Vector3 position, float duration = 1f, int layer = default) {
		var (textID, instance) = GetOrCreateInstance(duration, layer);
		instance.text = text;
		instance.transform.position = position;
		return textID;
	}

	public static uint AddText(
		StringBuilder builder, Vector3 position, float duration = 1f, int layer = default) {
		var (textID, instance) = GetOrCreateInstance(duration, layer);
		instance.SetText(builder);
		instance.transform.position = position;
		return textID;
	}

	public static void SetTextValue(uint textID, string text) {
		if (TextInstance.TryGetValue(textID, out var value)) {
			var (instance, endTime) = value;
			instance.text = text;
		}
	}

	public static void SetTextValue(uint textID, StringBuilder builder) {
		if (TextInstance.TryGetValue(textID, out var value)) {
			var (instance, endTime) = value;
			instance.SetText(builder);
		}
	}

	public static void SetTextPosition(uint textID, Vector3 position) {
		if (TextInstance.TryGetValue(textID, out var value)) {
			var (instance, endTime) = value;
			instance.transform.position = position;
		}
	}

	public static void SetTextDuration(uint textID, float duration) {
		if (TextInstance.TryGetValue(textID, out var value)) {
			var (instance, endTime) = value;
			TextInstance[textID] = (instance, Time.time + duration);
		}
	}

	public static void SetTextLayer(uint textID, int layer) {
		if (TextInstance.TryGetValue(textID, out var value)) {
			var (instance, endTime) = value;
			instance.gameObject.layer = layer;
		}
	}

	public static void RemoveText(uint textID) {
		if (TextInstance.ContainsKey(textID)) RemoveInstance(textID);
	}



	// Initialization Methods

	static ScreenBase[] LoadScreenBases() {
		var screenBases = new ScreenBase[ScreenCount];
		for (int i = 0; i < ScreenCount; i++) {
			var screen = (Screen)i;
			var screenBase = GetChildComponentRecursive(Instance.transform, screen.ToType());
			screenBases[i] = (ScreenBase)screenBase;
		}
		return screenBases;
	}

	static Component GetChildComponentRecursive(Transform parent, Type type) {
		if (parent.TryGetComponent(type, out var component)) return component;
		for (int i = 0; i < parent.childCount; i++) {
			var child = parent.GetChild(i);
			component = GetChildComponentRecursive(child, type);
			if (component) return component;
		}
		return null;
	}



	// Screen Methods

	static void UpdateScreenResolution() {
		bool match = false;
		match = match || ScreenResolution.x != UnityEngine.Screen.width;
		match = match || ScreenResolution.y != UnityEngine.Screen.height;
		if (match) {
			ScreenResolution = new(UnityEngine.Screen.width, UnityEngine.Screen.height);
			float xRatio = UnityEngine.Screen.width / ReferenceResolution.x;
			float yRatio = UnityEngine.Screen.height / ReferenceResolution.y;
			float multiplier = Mathf.Max(1, (int)Mathf.Min(xRatio, yRatio));
			CanvasScale = multiplier;
		}
	}

	public static void OpenScreen(Screen screen) {
		OpenScreen(ScreenBases[(int)screen]);
	}

	public static void OpenScreen(ScreenBase screenBase) {
		if (screenBase.IsPrimary) {
			while (ScreenStack.TryPop(out var screen8ase)) {
				screen8ase.Hide();
			}
		} else if (ScreenStack.TryPeek(out var screen8ase)) {
			if (screen8ase.IsOverlay) screen8ase.Hide();
		}
		ScreenStack.Push(screenBase);
		screenBase.Show();
		ScreenBlur = screenBase.UseScreenBlur;
	}

	public static void CloseScreen(ScreenBase screenBase) {
		if (ScreenStack.TryPop(out var screen8ase)) {
			if (screenBase == screen8ase) screen8ase.Hide();
		}
		if (ScreenStack.TryPeek(out screen8ase)) {
			screen8ase.Show();
			ScreenBlur = screen8ase.UseScreenBlur;
		}
	}

	public static void Back() {
		if (ScreenStack.TryPeek(out var screenBase)) {
			screenBase.Back();
		}
	}



	// Selected Methods

	static void UpdateSelectedScroll() {
		if (Selected == null) return;
		var component = GetParentComponentRecursive(Selected.transform, typeof(ScrollRect));
		if (component is ScrollRect scrollRect) {
			var selectedTransform = (RectTransform)Selected.transform;
			var viewportTransform = scrollRect.viewport;
			var viewRect = viewportTransform.rect;
			var contentTransform = scrollRect.content;
			var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
				viewportTransform, selectedTransform);

			float xDelta = 0f;
			if (scrollRect.horizontal) {
				if (bounds.min.x < viewRect.xMin) xDelta = viewRect.xMin - bounds.min.x;
				if (bounds.max.x > viewRect.xMax) xDelta = viewRect.xMax - bounds.max.x;
			}
			float yDelta = 0f;
			if (scrollRect.vertical) {
				if (bounds.min.y < viewRect.yMin) yDelta = viewRect.yMin - bounds.min.y;
				if (bounds.max.y > viewRect.yMax) yDelta = viewRect.yMax - bounds.max.y;
			}
			if (xDelta != 0f || yDelta != 0f) {
				contentTransform.anchoredPosition += new Vector2(xDelta, yDelta);
			}
		}
	}

	static Component GetParentComponentRecursive(Transform child, Type type) {
		if (child.TryGetComponent(type, out var component)) return component;
		if (child.parent) return GetParentComponentRecursive(child.parent, type);
		return null;
	}



	// Alert Screen Methods

	static AlertScreen AlertScreen {
		get => (AlertScreen)ScreenBases[(int)Screen.Alert];
	}

	public static LocalizedString AlertContentReference {
		get => AlertScreen.ContentReference;
		set => AlertScreen.ContentReference = value;
	}
	public static LocalizedString AlertCloseReference {
		get => AlertScreen.CloseReference;
		set => AlertScreen.CloseReference = value;
	}

	public static Action OnAlertClosed {
		get => AlertScreen.OnClosed;
		set => AlertScreen.OnClosed = value;
	}



	// Confirmation Screen Methods

	static ConfirmationScreen ConfirmationScreen {
		get => (ConfirmationScreen)ScreenBases[(int)Screen.Confirmation];
	}

	public static LocalizedString ConfirmationHeaderReference {
		get => ConfirmationScreen.HeaderReference;
		set => ConfirmationScreen.HeaderReference = value;
	}
	public static LocalizedString ConfirmationContentReference {
		get => ConfirmationScreen.ContentReference;
		set => ConfirmationScreen.ContentReference = value;
	}
	public static LocalizedString ConfirmationConfirmReference {
		get => ConfirmationScreen.ConfirmReference;
		set => ConfirmationScreen.ConfirmReference = value;
	}
	public static LocalizedString ConfirmationCancelReference {
		get => ConfirmationScreen.CancelReference;
		set => ConfirmationScreen.CancelReference = value;
	}

	public static Action OnConfirmationConfirmed {
		get => ConfirmationScreen.OnConfirmed;
		set => ConfirmationScreen.OnConfirmed = value;
	}
	public static Action OnConfirmationCancelled {
		get => ConfirmationScreen.OnCancelled;
		set => ConfirmationScreen.OnCancelled = value;
	}



	// Lifecycle

	protected override void Awake() {
		base.Awake();
		ScreenBases = LoadScreenBases();
		foreach (var screenBase in ScreenBases) {
			screenBase.gameObject.SetActive(false);
		}
	}

	void Update() {
		UpdateInstances();
	}

	void LateUpdate() {
		UpdateScreenResolution();
		UpdateSelectedScroll();
	}

	protected override void OnDestroy() {
		ScreenBlur = false;
		base.OnDestroy();
	}
}

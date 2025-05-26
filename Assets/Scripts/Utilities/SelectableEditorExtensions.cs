using UnityEngine;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
	using UnityEditor;
	using UnityEditor.UI;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Selectable Editor Extensions
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

#if UNITY_EDITOR
	public class SelectableEditorExtensions : SelectableEditor {

		// Initialization Methods

		public void Begin(string className) {
			serializedObject.Update();
			Undo.RecordObject(target, $"Change {className} Properties");
		}
		public void End() {
			serializedObject.ApplyModifiedProperties();
			if (GUI.changed) EditorUtility.SetDirty(target);
		}



		// Layout Methods

		public void LabelField(string value) {
			EditorGUILayout.LabelField(value);
		}
		public void LabelField(string label, string value) {
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(label);
			EditorGUILayout.LabelField($" {value}");
			EditorGUILayout.EndHorizontal();
		}
		public void LabelField(string label, GUIStyle style) {
			EditorGUILayout.LabelField(label, style);
		}

		public void PrefixLabel(string label) {
			EditorGUILayout.PrefixLabel(label);
		}
		public void PrefixLabel(string label, GUIStyle style) {
			EditorGUILayout.PrefixLabel(label, style);
		}

		public bool Foldout(string label, bool foldout) {
			return EditorGUILayout.Foldout(foldout, label, true);
		}
		public bool Foldout(string label, bool foldout, GUIStyle style) {
			return EditorGUILayout.Foldout(foldout, label, true, style);
		}

		public void BeginDisabledGroup(bool disabled = true) {
			EditorGUI.BeginDisabledGroup(disabled);
		}
		public void EndDisabledGroup() {
			EditorGUI.EndDisabledGroup();
		}

		public void BeginHorizontal() {
			EditorGUILayout.BeginHorizontal();
		}
		public void EndHorizontal() {
			EditorGUILayout.EndHorizontal();
		}

		public void BeginVertical() {
			EditorGUILayout.BeginVertical();
		}
		public void EndVertical() {
			EditorGUILayout.EndVertical();
		}

		public bool Button(string label) {
			return GUILayout.Button(label);
		}

		public void HelpBox(string message, MessageType type = MessageType.None) {
			EditorGUILayout.HelpBox(message, type);
		}

		public void Space() {
			EditorGUILayout.Space();
		}

		public int IntentLevel {
			get => EditorGUI.indentLevel;
			set => EditorGUI.indentLevel = value;
		}



		// Field Methods

		public int IntField(int value) {
			return EditorGUILayout.IntField(value);
		}
		public int IntField(string label, int value) {
			return EditorGUILayout.IntField(label, value);
		}
		public int IntSlider(int value, int min, int max) {
			return EditorGUILayout.IntSlider(value, min, max);
		}
		public int IntSlider(string label, int value, int min, int max) {
			return EditorGUILayout.IntSlider(label, value, min, max);
		}

		public float FloatField(float value) {
			return EditorGUILayout.FloatField(value);
		}
		public float FloatField(string label, float value) {
			return EditorGUILayout.FloatField(label, value);
		}
		public float Slider(float value, float min, float max) {
			return EditorGUILayout.Slider(value, min, max);
		}
		public float Slider(string label, float value, float min, float max) {
			return EditorGUILayout.Slider(label, value, min, max);
		}

		public bool Toggle(bool value) {
			return EditorGUILayout.Toggle(value);
		}
		public bool Toggle(string label, bool value) {
			return EditorGUILayout.Toggle(label, value);
		}
		public bool ToggleLeft(bool value) {
			return EditorGUILayout.ToggleLeft(string.Empty, value);
		}
		public bool ToggleLeft(string label, bool value) {
			return EditorGUILayout.ToggleLeft(label, value);
		}

		public byte ByteField(byte value) {
			return (byte)EditorGUILayout.IntField(value);
		}
		public byte ByteField(string label, byte value) {
			return (byte)EditorGUILayout.IntField(label, value);
		}
		public sbyte SByteField(sbyte value) {
			return (sbyte)EditorGUILayout.IntField(value);
		}
		public sbyte SByteField(string label, sbyte value) {
			return (sbyte)EditorGUILayout.IntField(label, value);
		}

		public short ShortField(short value) {
			return (short)EditorGUILayout.IntField(value);
		}
		public short ShortField(string label, short value) {
			return (short)EditorGUILayout.IntField(label, value);
		}
		public ushort UShortField(ushort value) {
			return (ushort)EditorGUILayout.IntField(value);
		}
		public ushort UShortField(string label, ushort value) {
			return (ushort)EditorGUILayout.IntField(label, value);
		}

		public T EnumField<T>(T value) where T : Enum {
			return (T)EditorGUILayout.EnumPopup(value);
		}
		public T EnumField<T>(string label, T value) where T : Enum {
			return (T)EditorGUILayout.EnumPopup(label, value);
		}
		public uint FlagField<T>(string label, uint value, uint mask = ~0u) where T : Enum {
			int temp = 0;
			var strings = new List<string>();
			var indices = new List<int   >();
			int length = Mathf.Min(Enum.GetValues(typeof(T)).Length, 32);
			for (int i = 0; i < length; i++) if ((mask & (1u << i)) != 0) {
				if ((value & (1u << i)) != 0) temp |= 1 << indices.Count;
				strings.Add(Enum.GetName(typeof(T), i));
				indices.Add(i);
			}
			uint result = 0u;
			temp = EditorGUILayout.MaskField(label, temp, strings.ToArray());
			for (int i = 0; i < indices.Count; i++) if ((temp & (1u << i)) != 0) {
				result |= 1u << indices[i];
			}
			return result;
		}

		public int LayerField(int layer) {
			var layers = new string[32];
			for (int i = 0; i < layers.Length; i++) layers[i] = LayerMask.LayerToName(i);
			return EditorGUILayout.MaskField(layer, layers);
		}
		public int LayerField(string label, int layer) {
			var layers = new string[32];
			for (int i = 0; i < layers.Length; i++) layers[i] = LayerMask.LayerToName(i);
			return EditorGUILayout.MaskField(label, layer, layers);
		}

		public int SceneField(string label, int value) {
			var scenes = EditorBuildSettings.scenes;
			var popups = new string[EditorBuildSettings.scenes.Length];
			for (int i = 0; i < scenes.Length; i++) popups[i] = scenes[i].path.Split('/')[^1][..^6];
			EditorGUILayout.BeginHorizontal();
			PrefixLabel(label);
			value = EditorGUILayout.Popup(value, popups);
			EditorGUILayout.EndHorizontal();
			return value;
		}

		public Vector2 Vector2Field(Vector2 value) {
			return EditorGUILayout.Vector2Field(string.Empty, value);
		}
		public Vector2 Vector2Field(string label, Vector2 value) {
			return EditorGUILayout.Vector2Field(label, value);
		}
		public Vector2Int Vector2IntField(Vector2Int value) {
			return EditorGUILayout.Vector2IntField(string.Empty, value);
		}
		public Vector2Int Vector2IntField(string label, Vector2Int value) {
			return EditorGUILayout.Vector2IntField(label, value);
		}

		public Vector3 Vector3Field(Vector3 value) {
			return EditorGUILayout.Vector3Field(string.Empty, value);
		}
		public Vector3 Vector3Field(string label, Vector3 value) {
			return EditorGUILayout.Vector3Field(label, value);
		}
		public Vector3Int Vector3IntField(Vector3Int value) {
			return EditorGUILayout.Vector3IntField(string.Empty, value);
		}
		public Vector3Int Vector3IntField(string label, Vector3Int value) {
			return EditorGUILayout.Vector3IntField(label, value);
		}

		public Vector4 Vector4Field(Vector4 value) {
			return EditorGUILayout.Vector4Field(string.Empty, value);
		}
		public Vector4 Vector4Field(string label, Vector4 value) {
			return EditorGUILayout.Vector4Field(label, value);
		}
		public Quaternion QuaternionField(Quaternion value) {
			Vector4 vector = new(value.x, value.y, value.z, value.w);
			vector = EditorGUILayout.Vector4Field(string.Empty, vector);
			return new Quaternion(vector.x, vector.y, vector.z, vector.w);
		}
		public Quaternion QuaternionField(string label, Quaternion value) {
			Vector4 vector = new(value.x, value.y, value.z, value.w);
			vector = EditorGUILayout.Vector4Field(label, vector);
			return new Quaternion(vector.x, vector.y, vector.z, vector.w);
		}
		public Quaternion EulerField(Quaternion value) {
			Vector3 vector = value.eulerAngles;
			vector = EditorGUILayout.Vector3Field(string.Empty, vector);
			return Quaternion.Euler(vector);
		}
		public Quaternion EulerField(string label, Quaternion value) {
			Vector3 vector = value.eulerAngles;
			vector = EditorGUILayout.Vector3Field(label, vector);
			return Quaternion.Euler(vector);
		}

		public Color ColorField(Color value) {
			return EditorGUILayout.ColorField(string.Empty, value);
		}
		public Color ColorField(string label, Color value) {
			return EditorGUILayout.ColorField(label, value);
		}

		public string TextField(string value) {
			return EditorGUILayout.TextField(value);
		}
		public string TextField(string label, string value) {
			return EditorGUILayout.TextField(label, value);
		}

		public AnimationCurve CurveField(AnimationCurve curve) {
			return EditorGUILayout.CurveField(curve);
		}
		public AnimationCurve CurveField(string label, AnimationCurve curve) {
			return EditorGUILayout.CurveField(label, curve);
		}

		public T ObjectField<T>(T value) where T : UnityEngine.Object {
			return (T)EditorGUILayout.ObjectField(value, typeof(T), true);
		}
		public T ObjectField<T>(string label, T value) where T : UnityEngine.Object {
			return (T)EditorGUILayout.ObjectField(label, value, typeof(T), true);
		}

		public void PropertyField(string name) {
			EditorGUILayout.PropertyField(serializedObject.FindProperty(name));
		}
	}
#endif

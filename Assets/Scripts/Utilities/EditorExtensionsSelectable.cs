using UnityEngine;
using System;
using System.Collections.Generic;

using Unity.Mathematics;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Editor Extensions Selectable
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

#if UNITY_EDITOR
[CanEditMultipleObjects]
public class EditorExtensionsSelectable : SelectableEditor {

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
		EditorGUILayout.LabelField(value);
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

	public void BeginDisabledGroup(bool disable = true) {
		EditorGUI.BeginDisabledGroup(disable);
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
		int intValue = EditorGUILayout.IntField(value);
		return (byte)Mathf.Clamp(intValue, byte.MinValue, byte.MaxValue);
	}
	public byte ByteField(string label, byte value) {
		int intValue = EditorGUILayout.IntField(label, value);
		return (byte)Mathf.Clamp(intValue, byte.MinValue, byte.MaxValue);
	}
	public sbyte SByteField(sbyte value) {
		int intValue = EditorGUILayout.IntField(value);
		return (sbyte)Mathf.Clamp(intValue, sbyte.MinValue, sbyte.MaxValue);
	}
	public sbyte SByteField(string label, sbyte value) {
		int intValue = EditorGUILayout.IntField(label, value);
		return (sbyte)Mathf.Clamp(intValue, sbyte.MinValue, sbyte.MaxValue);
	}

	public short ShortField(short value) {
		int intValue = EditorGUILayout.IntField(value);
		return (short)Mathf.Clamp(intValue, short.MinValue, short.MaxValue);
	}
	public short ShortField(string label, short value) {
		int intValue = EditorGUILayout.IntField(label, value);
		return (short)Mathf.Clamp(intValue, short.MinValue, short.MaxValue);
	}
	public ushort UShortField(ushort value) {
		int intValue = EditorGUILayout.IntField(value);
		return (ushort)Mathf.Clamp(intValue, ushort.MinValue, ushort.MaxValue);
	}
	public ushort UShortField(string label, ushort value) {
		int intValue = EditorGUILayout.IntField(label, value);
		return (ushort)Mathf.Clamp(intValue, ushort.MinValue, ushort.MaxValue);
	}

	public T EnumField<T>(T value) where T : Enum {
		return (T)EditorGUILayout.EnumPopup(value);
	}
	public T EnumField<T>(string label, T value) where T : Enum {
		return (T)EditorGUILayout.EnumPopup(label, value);
	}
	public uint FlagField<T>(uint value, uint mask = uint.MaxValue) where T : Enum {
		return FlagField<T>(string.Empty, value, mask);
	}
	public uint FlagField<T>(string label, uint value, uint mask = uint.MaxValue) where T : Enum {
		var options = new List<string>();
		var indices = new List<int>();
		int temp = 0;
		int length = Mathf.Min(Enum.GetValues(typeof(T)).Length, 32);
		for (int i = 0; i < length; i++) if ((mask & (1u << i)) != 0u) {
			if ((value & (1u << i)) != 0) temp |= 1 << indices.Count;
			options.Add(Enum.GetName(typeof(T), i));
			indices.Add(i);
		}
		uint result = 0u;
		temp = EditorGUILayout.MaskField(label, temp, options.ToArray());
		for (int i = 0; i < indices.Count; i++) if ((temp & (1u << i)) != 0) {
			result |= 1u << indices[i];
		}
		return result;
	}

	public int LayerField(int layer) {
		var options = new string[32];
		for (int i = 0; i < options.Length; i++) options[i] = LayerMask.LayerToName(i);
		return EditorGUILayout.MaskField(layer, options);
	}
	public int LayerField(string label, int layer) {
		var options = new string[32];
		for (int i = 0; i < options.Length; i++) options[i] = LayerMask.LayerToName(i);
		return EditorGUILayout.MaskField(label, layer, options);
	}

	public int SceneField(int value) {
		return SceneField(string.Empty, value);
	}
	public int SceneField(string label, int value) {
		var options = new string[EditorBuildSettings.scenes.Length];
		for (int i = 0; i < options.Length; i++) {
			options[i] = EditorBuildSettings.scenes[i].path.Split('/')[^1][..^6];
		}
		EditorGUILayout.BeginHorizontal();
		if (label != string.Empty) PrefixLabel(label);
		value = EditorGUILayout.Popup(value, options);
		EditorGUILayout.EndHorizontal();
		return value;
	}

	public Vector2 Vector2Field(Vector2 value) {
		return Vector2Field(string.Empty, value);
	}
	public Vector2 Vector2Field(string label, Vector2 value) {
		return EditorGUILayout.Vector2Field(label, value);
	}
	public Vector2Int Vector2IntField(Vector2Int value) {
		return Vector2IntField(string.Empty, value);
	}
	public Vector2Int Vector2IntField(string label, Vector2Int value) {
		return EditorGUILayout.Vector2IntField(label, value);
	}

	public Vector3 Vector3Field(Vector3 value) {
		return Vector3Field(string.Empty, value);
	}
	public Vector3 Vector3Field(string label, Vector3 value) {
		return EditorGUILayout.Vector3Field(label, value);
	}
	public Vector3Int Vector3IntField(Vector3Int value) {
		return Vector3IntField(string.Empty, value);
	}
	public Vector3Int Vector3IntField(string label, Vector3Int value) {
		return EditorGUILayout.Vector3IntField(label, value);
	}

	public Vector4 Vector4Field(Vector4 value) {
		return Vector4Field(string.Empty, value);
	}
	public Vector4 Vector4Field(string label, Vector4 value) {
		return EditorGUILayout.Vector4Field(label, value);
	}
	public Vector4 EulerField(Vector4 value) {
		return EulerField(string.Empty, value);
	}
	public Vector4 EulerField(string label, Vector4 value) {
		var vector = new Quaternion(value.x, value.y, value.z, value.w).eulerAngles;
		vector = EditorGUILayout.Vector3Field(label, vector);
		var quaternion = Quaternion.Euler(vector);
		return new Vector4(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
	}

	public Quaternion QuaternionField(Quaternion value) {
		return QuaternionField(string.Empty, value);
	}
	public Quaternion QuaternionField(string label, Quaternion value) {
		var vector = new Vector4(value.x, value.y, value.z, value.w);
		vector = EditorGUILayout.Vector4Field(label, vector);
		return new Quaternion(vector.x, vector.y, vector.z, vector.w);
	}
	public Quaternion EulerField(Quaternion value) {
		return EulerField(string.Empty, value);
	}
	public Quaternion EulerField(string label, Quaternion value) {
		var vector = value.eulerAngles;
		vector = EditorGUILayout.Vector3Field(label, vector);
		return Quaternion.Euler(vector);
	}

	public Color ColorField(Color value) {
		return EditorGUILayout.ColorField(value);
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
	public string TextArea(string value) {
		return TextArea(string.Empty, value);
	}
	public string TextArea(string label, string value) {
		BeginHorizontal();
		if (label != string.Empty) PrefixLabel(label);
		var style = new GUIStyle(EditorStyles.textArea) { wordWrap = true, stretchHeight = true };
		value = EditorGUILayout.TextArea(value, style, GUILayout.ExpandHeight(true));
		EndHorizontal();
		return value;
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



	// Mathematics Field Methods

	public int2 Int2Field(int2 value) {
		return Int2Field(string.Empty, value);
	}
	public int2 Int2Field(string label, int2 value) {
		var result = EditorGUILayout.Vector2IntField(label, new Vector2Int(value.x, value.y));
		return new int2(result.x, result.y);
	}

	public int3 Int3Field(int3 value) {
		return Int3Field(string.Empty, value);
	}
	public int3 Int3Field(string label, int3 value) {
		var result = Vector3IntField(label, new Vector3Int(value.x, value.y, value.z));
		return new int3(result.x, result.y, result.z);
	}

	public bool2 Toggle2(bool2 value) {
		return Toggle2(string.Empty, value);
	}
	public bool2 Toggle2(string label, bool2 value) {
		EditorGUILayout.BeginHorizontal();
		if (!string.IsNullOrEmpty(label)) EditorGUILayout.PrefixLabel(label);
		value.x = EditorGUILayout.ToggleLeft("X", value.x, GUILayout.Width(28));
		value.y = EditorGUILayout.ToggleLeft("Y", value.y, GUILayout.Width(28));
		EditorGUILayout.EndHorizontal();
		return value;
	}

	public bool3 Toggle3(bool3 value) {
		return Toggle3(string.Empty, value);
	}
	public bool3 Toggle3(string label, bool3 value) {
		EditorGUILayout.BeginHorizontal();
		if (!string.IsNullOrEmpty(label)) EditorGUILayout.PrefixLabel(label);
		value.x = EditorGUILayout.ToggleLeft("X", value.x, GUILayout.Width(28));
		value.y = EditorGUILayout.ToggleLeft("Y", value.y, GUILayout.Width(28));
		value.z = EditorGUILayout.ToggleLeft("Z", value.z, GUILayout.Width(28));
		EditorGUILayout.EndHorizontal();
		return value;
	}
}
#endif

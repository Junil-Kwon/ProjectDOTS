using UnityEngine;
using System.Collections.Generic;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.NetCode;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// UI Manager
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/UI Manager")]
public class UIManager : MonoSingleton<UIManager> {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(UIManager))]
		class UIManagerEditor : EditorExtensions {
			UIManager I => target as UIManager;
			public override void OnInspectorGUI() {
				Begin("UI Manager");
				
				End();
			}
		}
	#endif



	// Fields

	TitleCanvas    m_TitleCanvas;
	GameCanvas     m_GameCanvas;
	DialogueCanvas m_DialogueCanvas;
	MenuCanvas     m_MenuCanvas;
	SettingsCanvas m_SettingsCanvas;
	FadeCanvas     m_FadeCanvas;



	// Properties

	TitleCanvas TitleCanvas {
		get {
			if (!m_TitleCanvas) for (int i = 0; i < transform.childCount; i++) {
				if (transform.GetChild(i).TryGetComponent(out m_TitleCanvas)) break;
			}
			return m_TitleCanvas;
		}
	}
	GameCanvas GameCanvas {
		get {
			if (!m_GameCanvas) for (int i = 0; i < transform.childCount; i++) {
				if (transform.GetChild(i).TryGetComponent(out m_GameCanvas)) break;
			}
			return m_GameCanvas;
		}
	}
	DialogueCanvas DialogueCanvas {
		get {
			if (!m_DialogueCanvas) for (int i = 0; i < transform.childCount; i++) {
				if (transform.GetChild(i).TryGetComponent(out m_DialogueCanvas)) break;
			}
			return m_DialogueCanvas;
		}
	}
	MenuCanvas MenuCanvas {
		get {
			if (!m_MenuCanvas) for (int i = 0; i < transform.childCount; i++) {
				if (transform.GetChild(i).TryGetComponent(out m_MenuCanvas)) break;
			}
			return m_MenuCanvas;
		}
	}
	SettingsCanvas SettingsCanvas {
		get {
			if (!m_SettingsCanvas) for (int i = 0; i < transform.childCount; i++) {
				if (transform.GetChild(i).TryGetComponent(out m_SettingsCanvas)) break;
			}
			return m_SettingsCanvas;
		}
	}
	FadeCanvas FadeCanvas {
		get {
			if (!m_FadeCanvas) for (int i = 0; i < transform.childCount; i++) {
				if (transform.GetChild(i).TryGetComponent(out m_FadeCanvas)) break;
			}
			return m_FadeCanvas;
		}
	}



	// Methods

}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// UI Manager System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class UIManagerSystem : SystemBase {

	[BurstCompile]
	protected override void OnCreate() {
	}

	[BurstDiscard]
	protected override void OnUpdate() {

	}
}

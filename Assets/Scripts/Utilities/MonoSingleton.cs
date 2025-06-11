using UnityEngine;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Mono Singleton
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[DisallowMultipleComponent]
public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour {

	// Fields

	static T instance;



	// Properties

	public static T Instance => instance ??= FindAnyObjectByType<T>();



	// Methods

	public static bool TryGetComponentInChildren<K>(out K component) where K : Component {
		var transform = Instance.transform;
		for (int i = 0; i < transform.childCount; i++) {
			if (transform.GetChild(i).TryGetComponent(out component)) {
				return true;
			}
		}
		component = null;
		return false;
	}



	// Lifecycle

	protected virtual void Awake() {
		instance ??= this as T;
		if (Instance == this) DontDestroyOnLoad(gameObject);
		else DestroyImmediate(gameObject);
	}

	protected virtual void OnDestroy() {
		if (Instance == this) instance = null;
	}
}

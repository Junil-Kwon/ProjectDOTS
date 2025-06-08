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
		var childCount = transform.childCount;
		for (int i = 0; i < childCount; i++) {
			if (transform.GetChild(i).TryGetComponent(out component)) return true;
		}
		component = null;
		return false;
	}



	// Lifecycle

	void Awake() {
		if (instance == null) instance = this as T;
		if (Instance == this) DontDestroyOnLoad(gameObject);
		else                  DestroyImmediate (gameObject);
	}

	void OnDestroy() {
		if (Instance == this) instance = null;
	}
}

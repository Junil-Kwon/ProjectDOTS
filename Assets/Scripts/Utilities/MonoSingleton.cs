using UnityEngine;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Mono Singleton
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[DisallowMultipleComponent]
public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour {

	// Fields

	static T instance;



	// Properties

	protected static T Instance => instance ??= FindAnyObjectByType<T>();



	// Methods

	protected void TrySetInstance() => instance ??= this as T;

	protected static bool TryGetComponentInChildren<K>(out K component) where K : Component {
		var transform = Instance.transform;
		for (int i = 0; i < Instance.transform.childCount; i++) {
			if (transform.GetChild(i).TryGetComponent(out component)) {
				return true;
			}
		}
		component = null;
		return false;
	}



	// Lifecycle

	protected virtual void Awake() {
		TrySetInstance();
		if (Instance == this) DontDestroyOnLoad(gameObject);
		else DestroyImmediate(gameObject);
	}

	protected virtual void OnDestroy() {
		if (Instance == this) instance = null;
	}
}

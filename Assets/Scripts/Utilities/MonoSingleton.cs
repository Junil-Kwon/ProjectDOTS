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



	// Lifecycle

	void Awake() {
		if (instance == null) instance = this as T;
		if (Instance == this) DontDestroyOnLoad(gameObject);
		else                  Destroy          (gameObject);
	}

	void OnDestroy() {
		if (Instance == this) instance = null;
	}
}

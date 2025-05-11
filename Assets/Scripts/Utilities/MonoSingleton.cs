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

	static T GetOrCreateInstance() {
		T instance = FindAnyObjectByType<T>();
		if (!instance) {
			var gameObject = new GameObject() { name = typeof(T).Name };
			instance = gameObject.AddComponent<T>();
		}
		return instance;
	}



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

using UnityEngine;

using Unity.Entities;
using Unity.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif



public static class BakerExtensions {
	public static BlobAssetReference<K> AddBlobAsset<T, K>(
		this Baker<T> baker, K blobData) where T : Component where K : unmanaged {
		var blobBuilder = new BlobBuilder(Allocator.Temp);
		ref K data = ref blobBuilder.ConstructRoot<K>();
		data = blobData;
		var blobReference = blobBuilder.CreateBlobAssetReference<K>(Allocator.Persistent);
		baker.AddBlobAsset(ref blobReference, out _);
		blobBuilder.Dispose();
		return blobReference;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Mono Component
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[DisallowMultipleComponent]
public abstract class MonoComponent<T> : MonoBehaviour where T : MonoBehaviour {

	// Properties

	#if UNITY_EDITOR
	protected bool IsPrefabConnected {
		get {
			const PrefabInstanceStatus Connected = PrefabInstanceStatus.Connected;
			return PrefabUtility.GetPrefabInstanceStatus(gameObject) == Connected;
		}
	}
	#endif



	// Methods

	protected K GetOwnComponent<K>() where K : Component {
		return TryGetComponent(out K component) ? component : null;
	}

	protected K GetChildComponent<K>() where K : Component {
		return TryGetChildComponentRecursive(transform, out K component) ? component : null;
	}

	bool TryGetChildComponentRecursive<K>(Transform parent, out K component) where K : Component {
		for (int i = 0; i < parent.childCount; i++) {
			var child = parent.GetChild(i);
			if (child.TryGetComponent(out component)) return true;
			if (TryGetChildComponentRecursive(child, out component)) return true;
		}
		component = null;
		return false;
	}
}

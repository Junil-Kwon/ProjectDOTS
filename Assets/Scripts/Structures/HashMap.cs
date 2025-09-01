using UnityEngine;
using System;
using System.Collections.Generic;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Hash Map
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[Serializable]
public class HashMap<K, V> : Dictionary<K, V>, ISerializationCallbackReceiver {

	// Fields

	[SerializeField] List<K> m_KList = new();
	[SerializeField] List<V> m_VList = new();



	// Properties

	List<K> KList {
		get => m_KList;
	}
	List<V> VList {
		get => m_VList;
	}



	// Methods

	public void OnBeforeSerialize() {
		KList.Clear();
		VList.Clear();
		foreach (var (k, v) in this) {
			KList.Add(k);
			VList.Add(v);
		}
	}

	public void OnAfterDeserialize() {
		Clear();
		for (int i = 0; i < KList.Count; i++) {
			Add(KList[i], VList[i]);
		}
	}
}

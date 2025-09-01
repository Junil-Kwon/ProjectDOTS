using UnityEngine;
using System.Runtime.InteropServices;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Physics;
using Unity.NetCode;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Dummy Head Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Character Head/Dummy Head")]
[RequireComponent(typeof(CharacterCoreAuthoring))]
public sealed class DummyHeadAuthoring : MonoComponent<DummyHeadAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(DummyHeadAuthoring))]
	class DummyHeadAuthoringEditor : EditorExtensions {
		DummyHeadAuthoring I => target as DummyHeadAuthoring;
		public override void OnInspectorGUI() {
			Begin();

			if (I.CharacterCore.Head == default) I.CharacterCore.Head = Head.Dummy;
			else if (I.CharacterCore.Head != Head.Dummy) DestroyImmediate(I, true);
			BeginDisabledGroup(I.IsPrefabConnected);
			var data = new DummyHeadBlob { Data = I.Data };
			I.Data = data.Data;
			EndDisabledGroup();

			End();
		}
	}
	#endif



	// Fields

	CharacterCoreAuthoring m_CharacterCore;



	// Properties

	CharacterCoreAuthoring CharacterCore => !m_CharacterCore ?
		m_CharacterCore = GetOwnComponent<CharacterCoreAuthoring>() :
		m_CharacterCore;

	ref FixedBytes30 Data {
		get => ref CharacterCore.HeadData;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Dummy Head Data
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[StructLayout(LayoutKind.Explicit)]
public struct DummyHeadBlob {

	// Fields

	[FieldOffset(00)] public FixedBytes30 Data;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Dummy Head Data
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct DummyHeadData : IComponentData {

	// Fields

	public byte Data;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Dummy Head Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSServerSimulationSystemGroup))]
partial struct DummyHeadSimulationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<DummyHeadData>();
	}

	public void OnUpdate(ref SystemState state) {
		state.Dependency = new DummyHeadSimulationJob() {
		}.ScheduleParallel(state.Dependency);
	}
}

[BurstCompile, WithAll(typeof(Simulate))]
partial struct DummyHeadSimulationJob : IJobEntity {

	public void Execute(
		ref CharacterInput input,
		in CharacterHeadBlob headBlob,
		ref DummyHeadData headData) {

	}
}

using UnityEngine;
using System;
using System.Text.RegularExpressions;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.NetCode;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEditor;
#endif



public enum Immunity : uint {
	Weak,
	None,
	Half,
	Full,
}

public static class ImmunityExtensions {
	public static float ToValue(this Immunity immunity) => immunity switch {
		Immunity.Weak => -1.00f,
		Immunity.None => +0.00f,
		Immunity.Half => +0.50f,
		Immunity.Full => +1.00f,
		_ => 0f,
	};
}

public enum Effect : byte {
	Damage,
	HealthBoost,
	EnergyBoost,
	DamageBoost,
	Resistance,
	Speed,
	Slowness,
	Stun,
	Burn,
	Freeze,
}

public static class EffectExtensions {
	public static bool IsValueType(this Effect effect) => effect switch {
		Effect.Damage      => true,
		Effect.HealthBoost => true,
		Effect.EnergyBoost => true,
		Effect.DamageBoost => true,
		_ => false,
	};
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Core Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Character Effect")]
[RequireComponent(typeof(CharacterCoreAuthoring), typeof(CharacterStatusAuthoring))]
public sealed class CharacterEffectAuthoring : MonoComponent<CharacterEffectAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(CharacterEffectAuthoring))]
	class CharacterEffectAuthoringEditor : EditorExtensions {
		CharacterEffectAuthoring I => target as CharacterEffectAuthoring;
		static bool foldout = false;
		public override void OnInspectorGUI() {
			Begin();

			BeginDisabledGroup(I.IsPrefabConnected);
			if (foldout = Foldout("Immunity", foldout)) {
				int n = ImmunityCount - 1;
				IntentLevel++;
				for (int i = 0; i < EffectCount; i++) {
					BeginHorizontal();
					var effect = (Effect)i;
					var text = effect.ToString();
					var name = Regex.Replace(text, @"(?<!^)(?=[A-Z])", " ");
					PrefixLabel(name);
					I.SetImmunity(effect, (Immunity)IntSlider((int)I.GetImmunity(effect), 0, n));
					I.SetImmunity(effect, EnumField(I.GetImmunity(effect)));
					EndHorizontal();
				}
				IntentLevel--;
			}
			EndDisabledGroup();

			End();
		}
	}
	#endif



	// Constants

	static readonly int ImmunityCount = Enum.GetValues(typeof(Immunity)).Length;

	static readonly int EffectCount = Enum.GetValues(typeof(Effect)).Length;



	// Fields

	[SerializeField] uint m_Immunity = 0x55555555u;



	// Properties

	uint Immunity {
		get => m_Immunity;
		set => m_Immunity = value;
	}



	// Methods

	Immunity GetImmunity(Effect effect) {
		int shift = (int)effect * 2;
		return (Immunity)((Immunity >> shift) & 0b11u);
	}
	void SetImmunity(Effect effect, Immunity immunity) {
		int shift = (int)effect * 2;
		Immunity = (Immunity & ~(0b11u << shift)) | ((uint)immunity << shift);
	}



	// Baker

	class Baker : Baker<CharacterEffectAuthoring> {
		public override void Bake(CharacterEffectAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.Dynamic);
			AddComponent(entity, new CharacterEffectBlob {
				Value = this.AddBlobAsset(new CharacterEffectBlobData {

					Immunity = authoring.Immunity,

				})
			});
			AddBuffer<CharacterEffectData>(entity);
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Effect Blob
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CharacterEffectBlob : IComponentData {

	// Fields

	public BlobAssetReference<CharacterEffectBlobData> Value;
}



public struct CharacterEffectBlobData {

	// Fields

	public uint Immunity;



	// Methods

	public Immunity GetImmunity(Effect effect) {
		int shift = (int)effect * 2;
		return (Immunity)((Immunity >> shift) & 0b11u);
	}

	public void SetImmunity(Effect effect, Immunity immunity) {
		int shift = (int)effect * 2;
		Immunity = (Immunity & ~(0b11u << shift)) | ((uint)immunity << shift);
	}
}



public static class CharacterEffectBlobExtensions {

	// Methods

	public static Immunity GetImmunity(
		this CharacterEffectBlob effectBlob, Effect effect) {
		return effectBlob.Value.Value.GetImmunity(effect);
	}

	public static void SetImmunity(
		this CharacterEffectBlob effectBlob, Effect effect, Immunity immunity) {
		effectBlob.Value.Value.SetImmunity(effect, immunity);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Effect Data
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[GhostComponent, InternalBufferCapacity(5)]
public struct CharacterEffectData : IBufferElementData {

	// Fields

	[GhostField] public Effect Effect;
	[GhostField] public ushort RawStrength;
	[GhostField] public ushort RawDuration;



	// Properties

	public float Strength => Effect.IsValueType() ? RawStrength : RawStrength * 0.001f;
	public float Duration => RawDuration * 0.001f;
}



public static class CharacterEffectDataExtensions {

	// Methods

	public static void AddEffect(
		this DynamicBuffer<CharacterEffectData> buffer, Immunity immunity, Effect effect,
		float strength, float duration, float maxStrength = 65535f, float maxDuration = 65535f) {
		float value = math.max(0f, 1f - immunity.ToValue());
		if (value <= 0f) return;
		strength *= effect.IsValueType() ? value : 1000f * value;
		duration *= effect.IsValueType() ? 1000f : 1000f * value;
		var temp = new CharacterEffectData() { Effect = effect };
		if (buffer.TryGetIndex(effect, out int index)) {
			var element = buffer[index];
			temp.RawStrength = (ushort)math.min(strength + element.RawStrength, maxStrength);
			temp.RawDuration = (ushort)math.min(duration + element.RawDuration, maxDuration);
			buffer.ElementAt(index) = temp;
		} else {
			temp.RawStrength = (ushort)math.min(strength, maxStrength);
			temp.RawDuration = (ushort)math.min(duration, maxDuration);
			buffer.Add(temp);
		}
	}

	public static void RemoveEffect(
		this DynamicBuffer<CharacterEffectData> buffer, Effect effect) {
		if (TryGetIndex(buffer, effect, out int index)) buffer.RemoveAt(index);
	}

	public static void AddDamage(
		this DynamicBuffer<CharacterEffectData> buffer, Immunity immunity, int value) {
		AddEffect(buffer, immunity, Effect.Damage, value, 0.2f, 65535f, 0.2f);
	}

	static bool TryGetIndex(
		this DynamicBuffer<CharacterEffectData> buffer, Effect effect, out int index) {
		for (int i = 0; i < buffer.Length; i++) if (buffer[i].Effect == effect) {
			index = i;
			return true;
		}
		index = -1;
		return false;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Effect Predicted Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(DOTSPredictedSimulationSystemGroup), OrderLast = true)]
partial struct CharacterEffectPredictedSimulationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<CharacterEffectData>();
	}

	public void OnUpdate(ref SystemState state) {
		state.Dependency = new CharacterEffectSimulationJob {
			DeltaTime = SystemAPI.Time.DeltaTime,
		}.ScheduleParallel(state.Dependency);
	}
}

[BurstCompile]
partial struct CharacterEffectSimulationJob : IJobEntity {
	[ReadOnly] public float DeltaTime;

	public void Execute(
		in CharacterEffectBlob effectBlob,
		DynamicBuffer<CharacterEffectData> effectData) {

		for (int i = 0; i < effectData.Length; i++) {
			var effect = effectData.ElementAt(i);
			float rawDuration = effect.RawDuration - (ushort)(DeltaTime * 1000f);
			effect.RawDuration = (ushort)math.max(0f, rawDuration);
			if (effect.RawDuration == 0) effectData.RemoveAt(i);
		}
	}
}

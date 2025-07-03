using UnityEngine;
using System;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Extensions;
using Unity.NetCode;
using Unity.Properties;
using Random = Unity.Mathematics.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif



// Creature Cores

public enum Head : uint {
	Dummy,
	Player,
}
public static class HeadExtensions {
	public static Head ToEnum(this ComponentType type) => type switch {
		_ when type == ComponentType.ReadWrite<DummyHead>()  => Head.Dummy,
		_ when type == ComponentType.ReadWrite<PlayerHead>() => Head.Player,
		_ => default,
	};
	public static ComponentType ToComponent(this Head head) => head switch {
		Head.Dummy  => ComponentType.ReadWrite<DummyHead>(),
		Head.Player => ComponentType.ReadWrite<PlayerHead>(),
		_ => default,
	};
}

public enum Body : uint {
	Dummy,
	Player,
}
public static class BodyExtensions {
	public static Body ToEnum(this ComponentType type) => type switch {
		_ when type == ComponentType.ReadWrite<DummyBody>()  => Body.Dummy,
		_ when type == ComponentType.ReadWrite<PlayerBody>() => Body.Player,
		_ => default,
	};
	public static ComponentType ToComponent(this Body body) => body switch {
		Body.Dummy  => ComponentType.ReadWrite<DummyBody>(),
		Body.Player => ComponentType.ReadWrite<PlayerBody>(),
		_ => default,
	};
}

public enum Flag : byte {
	Pinned,
	Floating,
	Piercing,
	Invulnerable,
}

public enum Team : byte {
	Players,
	Monsters,
}



// Creature Blobs

public enum Tag : byte {
	Undead,
	Boss,
}



// Creature Effects

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

public enum Immunity : byte {
	Weak,
	None,
	Half,
	Full,
}
public static class ImmunityExtensions {
	public static float ToValue(this Immunity immunity) => immunity switch {
		Immunity.Weak => -1.00f,
		Immunity.None =>  0.00f,
		Immunity.Half =>  0.50f,
		Immunity.Full =>  1.00f,
		_ => 0f,
	};
}



// Physics

public enum PhysicsCategory : byte {
	Creature,
}
public static class Physics {
	public const float PinnedMass = 1000000f;
	public const float GravityMultiplier = -9.81f * NetworkManager.Ticktime;
	public const float KnockMultiplier = 256.00f * NetworkManager.Ticktime;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Creature")]
[RequireComponent(typeof(PhysicsBodyAuthoring), typeof(PhysicsShapeAuthoring))]
public class CreatureAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(CreatureAuthoring))]
	class CreatureAuthoringEditor : EditorExtensions {
		CreatureAuthoring I => target as CreatureAuthoring;
		static bool foldout = false;
		public override void OnInspectorGUI() {
			Begin("Creature Authoring");

			LabelField($"{I.Prefab} Blob", EditorStyles.boldLabel);
			I.RoughHeight = FloatField("Rough Height", I.RoughHeight);
			I.RoughRadius = FloatField("Rough Radius", I.RoughRadius);
			I.Mass        = FloatField("Mass",         I.Mass);
			Space();
			I.MaxShield = (ushort)IntSlider("Max Shield", I.MaxShield, 0, I.MaxHealth);
			I.MaxHealth = UShortField("Max Health", I.MaxHealth);
			I.MaxEnergy = UShortField("Max Energy", I.MaxEnergy);
			I.Tag = (byte)FlagField<Tag>("Tag", I.Tag);
			if (foldout = Foldout("Immunity", foldout)) {
				IntentLevel++;
				int a = 0;
				int b = Enum.GetValues(typeof(Immunity)).Length - 1;
				foreach (Effect effect in Enum.GetValues(typeof(Effect))) {
					BeginHorizontal();
					PrefixLabel(effect.ToString());
					I.SetImmunity(effect, (Immunity)IntSlider((int)I.GetImmunity(effect), a, b));
					I.SetImmunity(effect, EnumField(I.GetImmunity(effect)));
					EndHorizontal();
				}
				IntentLevel--;
			}
			Space();

			LabelField("Creature Core", EditorStyles.boldLabel);
			BeginHorizontal();
			PrefixLabel("Head Component");
			I.HeadName = TextField(I.HeadName);
			I.Head     = EnumField(I.Head);
			EndHorizontal();
			BeginHorizontal();
			PrefixLabel("Body Component");
			I.BodyName = TextField(I.BodyName);
			I.Body     = EnumField(I.Body);
			EndHorizontal();
			I.Flag = FlagField<Flag>("Flag", I.Flag);
			I.Team = FlagField<Team>("Team", I.Team);
			I.Health = UShortField("Initial Health", I.Health);
			I.Energy = UShortField("Initial Energy", I.Energy);
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] float m_RoughHeight = 1f;
	[SerializeField] float m_RoughRadius = 1f;
	[SerializeField] float m_Mass = 1f;
	[SerializeField] ushort m_MaxShield;
	[SerializeField] ushort m_MaxHealth;
	[SerializeField] ushort m_MaxEnergy;
	[SerializeField] byte m_Tag;
	[SerializeField] uint m_Immunity = 0x55555555u;

	[SerializeField] string m_HeadName;
	[SerializeField] string m_BodyName;
	[SerializeField] uint m_Flag;
	[SerializeField] uint m_Team;
	[SerializeField] ushort m_Health;
	[SerializeField] ushort m_Energy;



	// Properties

	Prefab Prefab => Enum.TryParse(name, out Prefab prefab) ? prefab : default;

	PhysicsBodyAuthoring PhysicsBody => TryGetComponent(out PhysicsBodyAuthoring body) ? body : null;



	// Blob Properties

	float RoughHeight {
		get => m_RoughHeight;
		set => m_RoughHeight = value;
	}
	float RoughRadius {
		get => m_RoughRadius;
		set => m_RoughRadius = value;
	}
	float Mass {
		get => m_Mass;
		set {
			m_Mass = value;
			if (!GetFlag(global::Flag.Pinned)) PhysicsBody.Mass = value;
		}
	}

	ushort MaxShield {
		get => m_MaxShield;
		set => m_MaxShield = value;
	}
	ushort MaxHealth {
		get => m_MaxHealth;
		set => m_MaxHealth = (MaxHealth == value) ? MaxHealth : Health = value;
	}
	ushort MaxEnergy {
		get => m_MaxEnergy;
		set => m_MaxEnergy = (MaxEnergy == value) ? MaxEnergy : Energy = value;
	}
	byte Tag {
		get => m_Tag;
		set => m_Tag = value;
	}
	uint Immunity {
		get => m_Immunity;
		set => m_Immunity = value;
	}



	// Core Properties

	string HeadName {
		get => m_HeadName;
		set => m_HeadName = value;
	}
	string BodyName {
		get => m_BodyName;
		set => m_BodyName = value;
	}
	Head Head {
		get => Enum.TryParse(HeadName, out Head head) ? head : default;
		set => HeadName = value.ToString();
	}
	Body Body {
		get => Enum.TryParse(BodyName, out Body body) ? body : default;
		set => BodyName = value.ToString();
	}
	uint Flag {
		get => m_Flag;
		set {
			var tempPinned = (m_Flag & (1 << (int)global::Flag.Pinned)) != 0;
			var corePinned = (value  & (1 << (int)global::Flag.Pinned)) != 0;
			if (corePinned != tempPinned) {
				PhysicsBody.Mass = corePinned switch {
					true  => Physics.PinnedMass,
					false => Mass,
				};
			}
			var tempPiercing = (m_Flag & (1 << (int)global::Flag.Piercing)) != 0;
			var corePiercing = (value  & (1 << (int)global::Flag.Piercing)) != 0;
			if (corePiercing != tempPiercing) {
				foreach (var shape in GetComponents<PhysicsShapeAuthoring>()) {
					var collidesWith = shape.CollidesWith;
					collidesWith.Value = corePiercing switch {
						true  => collidesWith.Value & ~(1u << (int)PhysicsCategory.Creature),
						false => collidesWith.Value |  (1u << (int)PhysicsCategory.Creature),
					};
					shape.CollidesWith = collidesWith;
				}
			}
			m_Flag = value;
		}
	}
	uint Team {
		get => m_Team;
		set => m_Team = value;
	}

	ushort Health {
		get => m_Health;
		set => m_Health = value;
	}
	ushort Energy {
		get => m_Energy;
		set => m_Energy = value;
	}



	// Methods

	bool GetFlag(Flag flag) => (Flag & (1u << (int)flag)) != 0u;
	void SetFlag(Flag flag, bool value) => Flag = value switch {
		true  => Flag |  (1u << (int)flag),
		false => Flag & ~(1u << (int)flag),
	};

	bool GetTeam(Team team) => (Team & (1u << (int)team)) != 0u;
	void SetTeam(Team team, bool value) => Team = value switch {
		true  => Team |  (1u << (int)team),
		false => Team & ~(1u << (int)team),
	};

	bool GetTag(Tag tag) => (Tag & (1u << (int)tag)) != 0u;
	void SetTag(Tag tag, bool value) => Tag = value switch {
		true  => (byte)(Tag |  (1 << (int)tag)),
		false => (byte)(Tag & ~(1 << (int)tag)),
	};

	Immunity GetImmunity(Effect effect) {
		int shift = (int)effect * 2;
		return (Immunity)((Immunity >> shift) & 0b11u);
	}
	void SetImmunity(Effect effect, Immunity immunity) {
		int shift = (int)effect * 2;
		Immunity = (Immunity & ~(0b11u << shift)) | ((uint)immunity << shift);
	}



	// Baker

	public class Baker : Baker<CreatureAuthoring> {
		public override void Bake(CreatureAuthoring authoring) {
			Entity entity = GetEntity(TransformUsageFlags.Dynamic);
			AddComponent(entity, new CreatureInitialize());
			AddComponent(entity, new CreatureInput());
			using var builder = new BlobBuilder(Allocator.Temp);
			ref var blob = ref builder.ConstructRoot<CreatureBlobData>();
			blob = new CreatureBlobData {

				RoughHeight = authoring.RoughHeight,
				RoughRadius = authoring.RoughRadius,
				Mass        = authoring.Mass,

				MaxShield   = authoring.MaxShield,
				MaxHealth   = authoring.MaxHealth,
				MaxEnergy   = authoring.MaxEnergy,
				Tag         = authoring.Tag,
				Immunity    = authoring.Immunity,

			};
			var blobValue = builder.CreateBlobAssetReference<CreatureBlobData>(Allocator.Persistent);
			AddBlobAsset(ref blobValue, out _);
			AddComponent(entity, new CreatureBlob { Value = blobValue });
			AddComponent(entity, authoring.Head.ToComponent());
			AddComponent(entity, authoring.Body.ToComponent());
			AddComponent(entity, new CreatureCore {

				Head   = authoring.Head,
				Body   = authoring.Body,
				Flag   = authoring.Flag,
				Team   = authoring.Team,

				Health = authoring.Health,
				Energy = authoring.Energy,

			});
			AddComponent(entity, new CreatureTemp {

				Head   = authoring.Head,
				Body   = authoring.Body,
				Flag   = authoring.Flag,
				Team   = authoring.Team,

				Health = authoring.Health,
				Energy = authoring.Energy,

			});
			AddBuffer<CreatureEffect>(entity);

			bool hasTileDrawer   = authoring.TryGetComponent(out TileDrawerAuthoring   tileDrawer);
			bool hasSpriteDrawer = authoring.TryGetComponent(out SpriteDrawerAuthoring spriteDrawer);
			bool hasShadowDrawer = authoring.TryGetComponent(out ShadowDrawerAuthoring shadowDrawer);
			bool hasUIDrawer     = authoring.TryGetComponent(out UIDrawerAuthoring     uiDrawer);
			if (!hasTileDrawer   || !tileDrawer.enabled)   AddBuffer<TileDrawer>(entity);
			if (!hasSpriteDrawer || !spriteDrawer.enabled) AddBuffer<SpriteDrawer>(entity);
			if (!hasShadowDrawer || !shadowDrawer.enabled) AddBuffer<ShadowDrawer>(entity);
			if (!hasUIDrawer     || !uiDrawer.enabled)     AddBuffer<UIDrawer>(entity);
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Initialize
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CreatureInitialize : IComponentData, IEnableableComponent { }



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Input
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CreatureInput : IComponentData {

	// Fields

	public uint Key;
	public float2 MoveDirection;



	// Methods

	public bool GetKey(KeyAction key) => (Key & (1 << (int)key)) != 0;
	public void SetKey(KeyAction key, bool value) => Key = value switch {
		true  => Key |  (1u << (int)key),
		false => Key & ~(1u << (int)key),
	};
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Blob
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CreatureBlob : IComponentData {

	public BlobAssetReference<CreatureBlobData> Value;
}



public struct CreatureBlobData {

	// Fields

	public float RoughHeight;
	public float RoughRadius;
	public float Mass;

	public ushort MaxShield;
	public ushort MaxHealth;
	public ushort MaxEnergy;
	public byte Tag;
	public uint Immunity;



	// Methods

	public bool GetTag(Tag tag) => (Tag & (1u << (int)tag)) != 0;
	public void SetTag(Tag tag, bool value) => Tag = value switch {
		true  => (byte)(Tag |  (1u << (int)tag)),
		false => (byte)(Tag & ~(1u << (int)tag)),
	};

	public Immunity GetImmunity(Effect effect) {
		int shift = (int)effect * 2;
		return (Immunity)((Immunity >> shift) & 0b11u);
	}
	public void SetImmunity(Effect effect, Immunity immunity) {
		int shift = (int)effect * 2;
		Immunity = (Immunity & ~(0b11u << shift)) | ((uint)immunity << shift);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Core
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[GhostComponent]
public struct CreatureCore : IComponentData {

	// Constants

	const uint HeadMask = 0xFC000000u;
	const uint BodyMask = 0x03FF0000u;
	const uint FlagMask = 0x0000FF00u;
	const uint TeamMask = 0x000000FFu;

	const uint HealthMask = 0xFFFF0000u;
	const uint EnergyMask = 0x0000FFFFu;

	const uint MotionXMask     = 0xF8000000u;
	const uint MotionYMask     = 0x07C00000u;
	const uint MotionXTickMask = 0x003FF800u;
	const uint MotionYTickMask = 0x000007FFu;

	const uint GravityFMask = 0xFF000000u;
	const uint KnockFMask   = 0x00FC0000u;
	const uint KnockXMask   = 0x0003F000u;
	const uint KnockYMask   = 0x00000FC0u;
	const uint KnockZMask   = 0x0000003Fu;

	const int HeadShift = 26;
	const int BodyShift = 16;
	const int FlagShift =  8;
	const int TeamShift =  0;

	const int HealthShift = 16;
	const int EnergyShift =  0;

	const int MotionXShift     = 27;
	const int MotionYShift     = 22;
	const int MotionTickXShift = 11;
	const int MotionTickYShift =  0;

	const int GravityFShift = 24;
	const int KnockFShift   = 18;
	const int KnockXShift   = 12;
	const int KnockYShift   =  6;
	const int KnockZShift   =  0;



	// Fields

	[GhostField] public uint Data0;
	[GhostField] public uint Data1;
	[GhostField] public uint Data2;
	[GhostField] public uint Data3;



	// Properties

	[CreateProperty] public Head Head {
		get => (Head)((Data0 & HeadMask) >> HeadShift);
		set => Data0 = (Data0 & ~HeadMask) | ((uint)value << HeadShift);
	}
	[CreateProperty] public Body Body {
		get => (Body)((Data0 & BodyMask) >> BodyShift);
		set => Data0 = (Data0 & ~BodyMask) | ((uint)value << BodyShift);
	}
	[CreateProperty] public uint Flag {
		get => (Data0 & FlagMask) >> FlagShift;
		set => Data0 = (Data0 & ~FlagMask) | (value << FlagShift);
	}
	[CreateProperty] public uint Team {
		get => (Data0 & TeamMask) >> TeamShift;
		set => Data0 = (Data0 & ~TeamMask) | (value << TeamShift);
	}



	[CreateProperty] public ushort Health {
		get => (ushort)((Data1 & HealthMask) >> HealthShift);
		set => Data1 = (Data1 & ~HealthMask) | ((uint)value << HealthShift);
	}
	[CreateProperty] public ushort Energy {
		get => (ushort)((Data1 & EnergyMask) >> EnergyShift);
		set => Data1 = (Data1 & ~EnergyMask) | ((uint)value << EnergyShift);
	}



	[CreateProperty] public Motion MotionX {
		get => (Motion)((Data2 & MotionXMask) >> MotionXShift);
		set {
			if (MotionX != value) MotionXTick = 0;
			Data2 = (Data2 & ~MotionXMask) | ((uint)value << MotionXShift);
		}
	}
	[CreateProperty] public Motion MotionY {
		get => (Motion)((Data2 & MotionYMask) >> MotionYShift);
		set {
			if (MotionY != value) MotionYTick = 0;
			Data2 = (Data2 & ~MotionYMask) | ((uint)value << MotionYShift);
		}
	}
	[CreateProperty] public int MotionXTick {
		get => (int)((Data2 & MotionXTickMask) >> MotionTickXShift);
		set => Data2 = (Data2 & ~MotionXTickMask) | ((uint)(value % 2048) << MotionTickXShift);
	}
	[CreateProperty] public int MotionYTick {
		get => (int)((Data2 & MotionYTickMask) >> MotionTickYShift);
		set => Data2 = (Data2 & ~MotionYTickMask) | ((uint)(value % 2048) << MotionTickYShift);
	}
	public float MotionXOffset {
		get => MotionXTick * NetworkManager.Ticktime;
		set => MotionXTick = (int)math.round(value * NetworkManager.Tickrate);
	}
	public float MotionYOffset {
		get => MotionYTick * NetworkManager.Ticktime;
		set => MotionYTick = (int)math.round(value * NetworkManager.Tickrate);
	}



	[CreateProperty] public int GravityFactor {
		get => (int)((Data3 & GravityFMask) >> GravityFShift);
		set => Data3 = (Data3 & ~GravityFMask) | ((uint)math.clamp(value, 0, 255) << GravityFShift);
	}
	[CreateProperty] public float3 GravityVector {
		get => new(0f, (GravityFactor == 0) ? 0f : GravityFactor + 10f, 0f);
		set => GravityFactor = (int)math.round(math.max(0f, value.y - 10f));
	}
	public bool IsGrounded => (Data3 & GravityFMask) == 0u;

	[CreateProperty] public int KnockFactor {
		get => (int)((Data3 & KnockFMask) >> KnockFShift);
		set => Data3 = (Data3 & ~KnockFMask) | ((uint)math.clamp(value, 0, 63) << KnockFShift);
	}
	[CreateProperty] public float3 KnockVector {
		get {
			float x = (((Data3 & KnockXMask) >> KnockXShift) - 31f) * 0.0322581f;
			float y = (((Data3 & KnockYMask) >> KnockYShift) - 31f) * 0.0322581f;
			float z = (((Data3 & KnockZMask) >> KnockZShift) - 31f) * 0.0322581f;
			return KnockFactor * 0.125f * new float3(x, y, z);
		}
		set {
			KnockFactor = (int)(math.length(value) * 8f);
			float3 normalized = math.normalize(value);
			uint x = (uint)(math.round(normalized.x * 31f) + 31f) << KnockXShift;
			uint y = (uint)(math.round(normalized.y * 31f) + 31f) << KnockYShift;
			uint z = (uint)(math.round(normalized.z * 31f) + 31f) << KnockZShift;
			Data3 = (Data3 & ~(KnockXMask | KnockYMask | KnockZMask)) | x | y | z;
		}
	}
	public bool IsKnocked => (Data3 & KnockFMask) != 0u;



	// Methods

	public bool GetFlag(Flag flag) => (Flag & (1u << (int)flag)) != 0u;
	public void SetFlag(Flag flag, bool value) => Flag = value switch {
		true  => Flag |  (1u << (int)flag),
		false => Flag & ~(1u << (int)flag),
	};

	public bool GetTeam(Team team) => (Team & (1u << (int)team)) != 0u;
	public void SetTeam(Team team, bool value) => Team = value switch {
		true  => Team |  (1u << (int)team),
		false => Team & ~(1u << (int)team),
	};
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Temp
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CreatureTemp : IComponentData {

	// Constants

	const uint HeadMask = 0xFC000000u;
	const uint BodyMask = 0x03FF0000u;
	const uint FlagMask = 0x0000FF00u;
	const uint TeamMask = 0x000000FFu;

	const uint HealthMask = 0xFFFF0000u;
	const uint EnergyMask = 0x0000FFFFu;

	const uint MotionXMask     = 0xF8000000u;
	const uint MotionYMask     = 0x07C00000u;
	const uint MotionXTickMask = 0x003FF800u;
	const uint MotionYTickMask = 0x000007FFu;

	const uint GravityFMask = 0xFF000000u;
	const uint KnockFMask   = 0x00FC0000u;
	const uint KnockXMask   = 0x0003F000u;
	const uint KnockYMask   = 0x00000FC0u;
	const uint KnockZMask   = 0x0000003Fu;

	const int HeadShift = 26;
	const int BodyShift = 16;
	const int FlagShift =  8;
	const int TeamShift =  0;

	const int HealthShift = 16;
	const int EnergyShift =  0;

	const int MotionXShift     = 27;
	const int MotionYShift     = 22;
	const int MotionTickXShift = 11;
	const int MotionTickYShift =  0;

	const int GravityFShift = 24;
	const int KnockFShift   = 18;
	const int KnockXShift   = 12;
	const int KnockYShift   =  6;
	const int KnockZShift   =  0;



	// Fields

	public uint Data0;
	public uint Data1;
	public uint Data2;
	public uint Data3;



	// Properties

	public Head Head {
		get => (Head)((Data0 & HeadMask) >> HeadShift);
		set => Data0 = (Data0 & ~HeadMask) | ((uint)value << HeadShift);
	}
	public Body Body {
		get => (Body)((Data0 & BodyMask) >> BodyShift);
		set => Data0 = (Data0 & ~BodyMask) | ((uint)value << BodyShift);
	}
	public uint Flag {
		get => (Data0 & FlagMask) >> FlagShift;
		set => Data0 = (Data0 & ~FlagMask) | (value << FlagShift);
	}
	public uint Team {
		get => (Data0 & TeamMask) >> TeamShift;
		set => Data0 = (Data0 & ~TeamMask) | (value << TeamShift);
	}



	public ushort Health {
		get => (ushort)((Data1 & HealthMask) >> HealthShift);
		set => Data1 = (Data1 & ~HealthMask) | ((uint)value << HealthShift);
	}
	public ushort Energy {
		get => (ushort)((Data1 & EnergyMask) >> EnergyShift);
		set => Data1 = (Data1 & ~EnergyMask) | ((uint)value << EnergyShift);
	}



	public Motion MotionX {
		get => (Motion)((Data2 & MotionXMask) >> MotionXShift);
		set {
			if (MotionX != value) MotionXTick = 0;
			Data2 = (Data2 & ~MotionXMask) | ((uint)value << MotionXShift);
		}
	}
	public Motion MotionY {
		get => (Motion)((Data2 & MotionYMask) >> MotionYShift);
		set {
			if (MotionY != value) MotionYTick = 0;
			Data2 = (Data2 & ~MotionYMask) | ((uint)value << MotionYShift);
		}
	}
	public int MotionXTick {
		get => (int)((Data2 & MotionXTickMask) >> MotionTickXShift);
		set => Data2 = (Data2 & ~MotionXTickMask) | ((uint)(value % 2048) << MotionTickXShift);
	}
	public int MotionYTick {
		get => (int)((Data2 & MotionYTickMask) >> MotionTickYShift);
		set => Data2 = (Data2 & ~MotionYTickMask) | ((uint)(value % 2048) << MotionTickYShift);
	}
	public float MotionXOffset {
		get => MotionXTick * NetworkManager.Ticktime;
		set => MotionXTick = (int)math.round(value * NetworkManager.Tickrate);
	}
	public float MotionYOffset {
		get => MotionYTick * NetworkManager.Ticktime;
		set => MotionYTick = (int)math.round(value * NetworkManager.Tickrate);
	}



	public int GravityFactor {
		get => (int)((Data3 & GravityFMask) >> GravityFShift);
		set => Data3 = (Data3 & ~GravityFMask) | ((uint)math.clamp(value, 0, 255) << GravityFShift);
	}
	public float3 GravityVector {
		get => new(0f, (GravityFactor == 0) ? 0f : GravityFactor + 10f, 0f);
		set => GravityFactor = (int)math.round(math.max(0f, value.y - 10f));
	}
	public bool IsGrounded => (Data3 & GravityFMask) == 0u;

	public int KnockFactor {
		get => (int)((Data3 & KnockFMask) >> KnockFShift);
		set => Data3 = (Data3 & ~KnockFMask) | ((uint)math.clamp(value, 0, 63) << KnockFShift);
	}
	public float3 KnockVector {
		get {
			float x = (((Data3 & KnockXMask) >> KnockXShift) - 31f) * 0.0322581f;
			float y = (((Data3 & KnockYMask) >> KnockYShift) - 31f) * 0.0322581f;
			float z = (((Data3 & KnockZMask) >> KnockZShift) - 31f) * 0.0322581f;
			return KnockFactor * 0.125f * new float3(x, y, z);
		}
		set {
			KnockFactor = (int)(math.length(value) * 8f);
			float3 normalized = math.normalize(value);
			uint x = (uint)(math.round(normalized.x * 31f) + 31f) << KnockXShift;
			uint y = (uint)(math.round(normalized.y * 31f) + 31f) << KnockYShift;
			uint z = (uint)(math.round(normalized.z * 31f) + 31f) << KnockZShift;
			Data3 = (Data3 & ~(KnockXMask | KnockYMask | KnockZMask)) | x | y | z;
		}
	}
	public bool IsKnocked => (Data3 & KnockFMask) != 0u;



	// Methods

	public bool GetFlag(Flag flag) => (Flag & (1u << (int)flag)) != 0u;
	public void SetFlag(Flag flag, bool value) => Flag = value switch {
		true  => Flag |  (1u << (int)flag),
		false => Flag & ~(1u << (int)flag),
	};

	public bool GetTeam(Team team) => (Team & (1u << (int)team)) != 0u;
	public void SetTeam(Team team, bool value) => Team = value switch {
		true  => Team |  (1u << (int)team),
		false => Team & ~(1u << (int)team),
	};
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Effect
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[GhostComponent, InternalBufferCapacity(5)]
public struct CreatureEffect : IBufferElementData {

	// Fields

	[GhostField] public Effect Effect;
	[GhostField] public ushort Strength;
	[GhostField] public ushort Duration;
}



public static class CreatureEffectBufferExtensions {

	public static bool TryGetIndex(
		this DynamicBuffer<CreatureEffect> buffer, Effect effect, out int index) {
		for (int i = 0; i < buffer.Length; i++) if (buffer[i].Effect == effect) {
			index = i;
			return true;
		}
		index = -1;
		return false;
	}

	public static void AddEffect(
		this DynamicBuffer<CreatureEffect> buffer, in CreatureBlob blob, Effect effect,
		float strength, float duration, float maxStrength = 0f, float maxDuration = 0f) {
		float multiplier = math.max(0f, 1f - blob.Value.Value.GetImmunity(effect).ToValue());
		if (multiplier == 0f) return;
		strength *= (effect.IsValueType() ? 1000f : 1f) * multiplier;
		duration *= (effect.IsValueType() ? 1f : multiplier) * NetworkManager.Tickrate;

		var element = new CreatureEffect() { Effect = effect };
		if (buffer.TryGetIndex(effect, out int index)) {
			var data = buffer[index];
			if (maxStrength == 0f) maxStrength = math.max(data.Strength, strength);
			if (maxDuration == 0f) maxDuration = math.max(data.Duration, duration);
			element.Strength = (ushort)math.min(data.Strength + strength, maxStrength);
			element.Duration = (ushort)math.min(data.Duration + duration, maxDuration);
			buffer.ElementAt(index) = element;
		} else {
			element.Strength = (ushort)strength;
			element.Duration = (ushort)duration;
			buffer.Add(element);
		}
	}

	public static void AddDamage(
		this DynamicBuffer<CreatureEffect> buffer, in CreatureBlob blob, int value) {
		AddEffect(buffer, in blob, Effect.Damage, value, 0.2f, float.MaxValue, float.MaxValue);
	}

	public static void RemoveEffect(
		this DynamicBuffer<CreatureEffect> buffer, Effect effect) {
		if (TryGetIndex(buffer, effect, out int index)) buffer.RemoveAt(index);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Initialization System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSInitializationSystemGroup))]
partial struct CreatureInitializationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
		state.RequireForUpdate<PrefabContainer>();
	}

	public void OnUpdate(ref SystemState state) {
		state.Dependency = new CreatureFieldsComparisonJob {
		}.ScheduleParallel(state.Dependency);

		var singleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
		state.Dependency = new CreatureInitializationJob {
			Buffer          = singleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
			PrefabContainer = SystemAPI.GetSingletonBuffer<PrefabContainer>(true),
			BlobLookup      = SystemAPI.GetComponentLookup<CreatureBlob>(),
			CoreLookup      = SystemAPI.GetComponentLookup<CreatureCore>(),
			TempLookup      = SystemAPI.GetComponentLookup<CreatureTemp>(),
			EffectLookup    = SystemAPI.GetBufferLookup<CreatureEffect>(),
			MassLookup      = SystemAPI.GetComponentLookup<PhysicsMass>(),
			ColliderLookup  = SystemAPI.GetComponentLookup<PhysicsCollider>(),
			TileLookup      = SystemAPI.GetBufferLookup<TileDrawer>(),
			SpriteLookup    = SystemAPI.GetBufferLookup<SpriteDrawer>(),
			ShadowLookup    = SystemAPI.GetBufferLookup<ShadowDrawer>(),
			UILookup        = SystemAPI.GetBufferLookup<UIDrawer>(),
		}.ScheduleParallel(state.Dependency);
	}



	[BurstCompile, WithPresent(typeof(CreatureInitialize))]
	partial struct CreatureFieldsComparisonJob : IJobEntity {
		public void Execute(
			in CreatureCore core,
			in CreatureTemp temp,
			EnabledRefRW<CreatureInitialize> initialize) {

			if (temp.Head != core.Head) initialize.ValueRW = true;
			if (temp.Body != core.Body) initialize.ValueRW = true;
			if (temp.Flag != core.Flag) initialize.ValueRW = true;
			if (temp.Team != core.Team) initialize.ValueRW = true;
		}
	}

	[BurstCompile, WithAll(typeof(CreatureInitialize))]
	partial struct CreatureInitializationJob : IJobEntity {
		public EntityCommandBuffer.ParallelWriter Buffer;
		[ReadOnly] public DynamicBuffer<PrefabContainer> PrefabContainer;
		[NativeDisableParallelForRestriction] public ComponentLookup<CreatureBlob> BlobLookup;
		[NativeDisableParallelForRestriction] public ComponentLookup<CreatureCore> CoreLookup;
		[NativeDisableParallelForRestriction] public ComponentLookup<CreatureTemp> TempLookup;
		[NativeDisableParallelForRestriction] public BufferLookup<CreatureEffect> EffectLookup;
		[NativeDisableParallelForRestriction] public ComponentLookup<PhysicsMass> MassLookup;
		[NativeDisableParallelForRestriction] public ComponentLookup<PhysicsCollider> ColliderLookup;
		[NativeDisableParallelForRestriction] public BufferLookup<TileDrawer> TileLookup;
		[NativeDisableParallelForRestriction] public BufferLookup<SpriteDrawer> SpriteLookup;
		[NativeDisableParallelForRestriction] public BufferLookup<ShadowDrawer> ShadowLookup;
		[NativeDisableParallelForRestriction] public BufferLookup<UIDrawer> UILookup;
		public void Execute(
			Entity entity,
			[ChunkIndexInQuery] int sortKey,
			EnabledRefRW<CreatureInitialize> initialize) {

			var blob = BlobLookup[entity];
			var core = CoreLookup[entity];
			var temp = TempLookup[entity];
			var mass = MassLookup[entity];
			var collider = ColliderLookup[entity];

			var target = Entity.Null;
			foreach (var prefab in PrefabContainer.Reinterpret<Entity>()) {
				if (CoreLookup.HasComponent(prefab) && CoreLookup[prefab].Body == core.Body) {
					target = prefab;
					break;
				}
			}

			if (temp.Head != core.Head) {
				var tempHead = temp.Head.ToComponent();
				var coreHead = core.Head.ToComponent();
				if (tempHead != default) Buffer.RemoveComponent(sortKey, entity, tempHead);
				if (coreHead != default) Buffer.AddComponent(sortKey, entity, coreHead);
				temp.Head = core.Head;
			}
			if (temp.Body != core.Body) {
				var tempBody = temp.Body.ToComponent();
				var coreBody = core.Body.ToComponent();
				if (tempBody != default) Buffer.RemoveComponent(sortKey, entity, tempBody);
				if (coreBody != default) Buffer.AddComponent(sortKey, entity, coreBody);
				temp.Body = core.Body;

				blob.Value       = BlobLookup[target].Value;
				core.Flag        = CoreLookup[target].Flag;
				core.MotionX     = CoreLookup[target].MotionX;
				core.MotionY     = CoreLookup[target].MotionY;
				core.MotionXTick = CoreLookup[target].MotionXTick;
				core.MotionYTick = CoreLookup[target].MotionYTick;
				EffectLookup[entity].Clear();

				mass.InverseMass = MassLookup[target].InverseMass;
				collider.Value = ColliderLookup[target].Value;

				TileLookup[entity].CopyFrom(TileLookup[target]);
				SpriteLookup[entity].CopyFrom(SpriteLookup[target]);
				ShadowLookup[entity].CopyFrom(ShadowLookup[target]);
				UILookup[entity].CopyFrom(UILookup[target]);
			}
			if (temp.Flag != core.Flag) {
				var tempPinned = temp.GetFlag(Flag.Pinned);
				var corePinned = core.GetFlag(Flag.Pinned);
				if (corePinned != tempPinned) {
					mass.InverseMass = corePinned switch {
						true  => 1f / Physics.PinnedMass,
						false => 1f / blob.Value.Value.Mass,
					};
				}
				var tempPiercing = temp.GetFlag(Flag.Piercing);
				var corePiercing = core.GetFlag(Flag.Piercing);
				if (corePiercing != tempPiercing) {
					var filter = collider.Value.Value.GetCollisionFilter();
					filter.CollidesWith = corePiercing switch {
						true  => filter.CollidesWith & ~(1u << (int)PhysicsCategory.Creature),
						false => filter.CollidesWith |  (1u << (int)PhysicsCategory.Creature),
					};
					if (!collider.Value.Value.GetCollisionFilter().Equals(filter)) {
						if (!collider.IsUnique) collider.MakeUnique(entity, Buffer, sortKey);
						collider.Value.Value.SetCollisionFilter(filter);
					}
				}
				temp.Flag = core.Flag;
			}
			if (temp.Team != core.Team) {
				temp.Team = core.Team;
			}
			mass.InverseInertia = float3.zero;
			initialize.ValueRW = false;

			CoreLookup[entity] = core;
			TempLookup[entity] = temp;
			BlobLookup[entity] = blob;
			MassLookup[entity] = mass;
			ColliderLookup[entity] = collider;
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Begin Predicted Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSPredictedSimulationSystemGroup), OrderFirst = true)]
partial struct CreatureBeginPredictedSimulationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<SimulationSingleton>();
		state.RequireForUpdate<CreatureCore>();
	}

	public void OnUpdate(ref SystemState state) {
		state.Dependency = new CreatureBeginSimulationJob {
		}.ScheduleParallel(state.Dependency);

		state.Dependency = new CreatureGravityRemovalJob {
			CoreLookup = SystemAPI.GetComponentLookup<CreatureCore>(),
		}.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
	}



	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct CreatureBeginSimulationJob : IJobEntity {
		public void Execute(
			ref CreatureCore core,
			ref PhysicsVelocity velocity) {

			if (!core.GetFlag(Flag.Floating)) core.GravityFactor++;
			if (core.IsKnocked) core.KnockFactor--;
			velocity.Linear = float3.zero;
		}
	}

	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct CreatureGravityRemovalJob : ICollisionEventsJob {
		public ComponentLookup<CreatureCore> CoreLookup;
		public void Execute(CollisionEvent collisionEvent) {
			Execute(collisionEvent.EntityA,  collisionEvent.Normal);
			Execute(collisionEvent.EntityB, -collisionEvent.Normal);
		}
		public void Execute(Entity entity, float3 normal) {
			var angle = math.degrees(math.acos(normal.y));
			if (CoreLookup.HasComponent(entity) && angle < 45f) {
				var core = CoreLookup[entity];
				core.GravityFactor = 0;
				CoreLookup[entity] = core;
			}
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature End Predicted Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSPredictedSimulationSystemGroup), OrderLast = true)]
partial struct CreatureEndPredictedSimulationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<CreatureCore>();
	}

	public void OnUpdate(ref SystemState state) {
		state.Dependency = new EndCreatureSimulationJob {
		}.ScheduleParallel(state.Dependency);
	}



	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct EndCreatureSimulationJob : IJobEntity {
		public void Execute(
			in CreatureCore core,
			ref PhysicsVelocity velocity) {

			if (!core.GetFlag(Flag.Floating)) {
				float multiplier = Physics.GravityMultiplier;
				velocity.Linear += multiplier * core.GravityVector;
			}
			if (core.IsKnocked) {
				float multiplier = Physics.KnockMultiplier;
				velocity.Linear += multiplier * core.KnockVector;
			}
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Client Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSClientSimulationSystemGroup))]
partial struct CreatureSimulationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
		state.RequireForUpdate<CameraManagerBridge>();
		state.RequireForUpdate<PrefabContainer>();
	}

	public void OnUpdate(ref SystemState state) {
		var singleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
		state.Dependency = new CreatureLandingParticleJob {
			Buffer          = singleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
			CameraManager   = SystemAPI.GetSingletonRW<CameraManagerBridge>(),
			PrefabContainer = SystemAPI.GetSingletonBuffer<PrefabContainer>(true),
			Random          = new Random(1u + (uint)(4801 * SystemAPI.Time.ElapsedTime) % 1000),
		}.ScheduleParallel(state.Dependency);
	}



	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct CreatureLandingParticleJob : IJobEntity {
		public EntityCommandBuffer.ParallelWriter Buffer;
		[NativeDisableUnsafePtrRestriction] public RefRW<CameraManagerBridge> CameraManager;
		[ReadOnly] public DynamicBuffer<PrefabContainer> PrefabContainer;
		[ReadOnly] public Random Random;
		public void Execute([ChunkIndexInQuery] int sortKey,
			in CreatureBlob blob,
			in CreatureCore core,
			ref CreatureTemp temp,
			in LocalToWorld transform,
			in PhysicsMass mass) {

			if (core.GravityFactor == 0 && temp.GravityFactor != 0) {
				var impactEnergy = 0f;
				impactEnergy += temp.GravityFactor * Physics.GravityMultiplier;
				impactEnergy += core.KnockFactor * Physics.KnockMultiplier;
				if (impactEnergy < -3.2f) {
					var right   = CameraManager.ValueRO.Right;
					var forward = CameraManager.ValueRO.Forward;
					var radius  = blob.Value.Value.RoughRadius;
					var position0 = transform.Position + right * radius - forward * radius;
					var position1 = transform.Position - right * radius - forward * radius;
					var prefab = PrefabContainer[(int)Prefab.SmokeTiny].Prefab;
					var smoke0 = Buffer.Instantiate(sortKey, prefab);
					var smoke1 = Buffer.Instantiate(sortKey, prefab);
					Buffer.SetComponent(sortKey, smoke0, LocalTransform.FromPosition(position0));
					Buffer.SetComponent(sortKey, smoke1, LocalTransform.FromPosition(position1));
				}
			}
			temp.GravityFactor = core.GravityFactor;
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Presentation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSPresentationSystemGroup))]
partial struct CreaturePresentationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<CreatureCore>();
	}

	public void OnUpdate(ref SystemState state) {
		state.Dependency = new CreaturePresentationJob {
		}.ScheduleParallel(state.Dependency);
	}



	[BurstCompile]
	partial struct CreaturePresentationJob : IJobEntity {
		public void Execute(
			in CreatureBlob blob,
			in CreatureCore core,
			DynamicBuffer<UIDrawer> ui) {

			if (ui.Length < 6) {
				while (ui.Length < 6) ui.Add(new UIDrawer {
					Position  = new float3(0f, blob.Value.Value.RoughHeight + 1f, 0f),
					Scale     = new float2(1f, 1f),
					BaseColor = color.white,
				});
				ui.ElementAt(0).UI = UI.BarM;
				ui.ElementAt(1).UI = UI.BarL;
				ui.ElementAt(2).UI = UI.BarR;
				ui.ElementAt(5).UI = UI.Bar;
				ui.ElementAt(4).UI = UI.Bar;
				ui.ElementAt(3).UI = UI.Bar;
				ui.ElementAt(5).BaseColor = new color(0xFFFFFF);
				ui.ElementAt(4).BaseColor = new color(0x2F2F2F);
				ui.ElementAt(3).BaseColor = new color(0x2277BB);
			}
			float maxShield = blob.Value.Value.MaxShield;
			float maxHealth = blob.Value.Value.MaxHealth;
			float health = core.Health;
			float pureHealth = math.min(health, maxHealth - maxShield);
			float pureShield = math.clamp(health - pureHealth, 0f, maxShield);
			float overHealth = math.max(0f, health - pureHealth - pureShield);
			if (0f < overHealth) overHealth = maxHealth * (1f - math.exp(-overHealth / maxHealth));
			float ratio = blob.Value.Value.RoughRadius * 2f / maxHealth;
			float width = math.max(pureHealth + pureShield + overHealth, maxHealth) * ratio;

			ui.ElementAt(0).Scale.x = width;
			ui.ElementAt(1).Pivot.x = width * -0.5f - 0.5f;
			ui.ElementAt(2).Pivot.x = width * +0.5f + 0.5f;
			ui.ElementAt(5).Scale.x = pureHealth * ratio;
			ui.ElementAt(4).Scale.x = pureShield * ratio;
			ui.ElementAt(3).Scale.x = overHealth * ratio;
			ui.ElementAt(5).Pivot.x = -width * 0.5f + ui[5].Scale.x * 0.5f;
			ui.ElementAt(4).Pivot.x = ui[5].Pivot.x + ui[5].Scale.x * 0.5f + ui[4].Scale.x * 0.5f;
			ui.ElementAt(3).Pivot.x = ui[4].Pivot.x + ui[4].Scale.x * 0.5f + ui[3].Scale.x * 0.5f;
		}
	}
}

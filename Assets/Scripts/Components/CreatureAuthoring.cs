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
using Random = Unity.Mathematics.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif



// Creature Cores

public enum Tag : byte {
	Undead,
	Boss,
}

public enum Head : ushort {
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

public enum Body : ushort {
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
	Interactable,
}

public enum Team : byte {
	Players,
	Monsters,
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
	None,
	Half,
	Full,
	Weak,
}
public static class ImmunityExtensions {
	public static float ToValue(this Immunity immunity) => immunity switch {
		Immunity.None =>  0.00f,
		Immunity.Half =>  0.50f,
		Immunity.Full =>  1.00f,
		Immunity.Weak => -1.00f,
		_ => 0f,
	};
}



// Creature Inputs

public enum Ping : byte {
	None,
	Look,
	Danger,
	GroupUp,
	OnMyWay,
	NeedHelp,
	Countdown,
	Charge,
	Fallback,
}

public enum Emotion : byte {
	None,
	Lol,
	Sad,
	Angry,
	Happy,
	Greeting,
	Thanks,
}



// Physics

public enum PhysicsCategory : byte {
	Creature,
}
public static class CreaturePhysics {
	public const float InitialMass = 1f;
	public const float PinnedMass  = 1f * 1000000f;
	public const float GravityMultiplier =  -9.81f * NetworkManager.Ticktime;
	public const float KnockMultiplier   = 256.00f * NetworkManager.Ticktime;
	public const float DustThreshold  = -3.2f;
	public const float Dust2Threshold = -6.4f;
	public const float Dust3Threshold = -9.6f;
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

			LabelField("Local Data", EditorStyles.boldLabel);
			I.Radius = FloatField("Radius", I.Radius);
			I.Height = FloatField("Height", I.Height);
			I.MaxShield = UShortField("Max Shield", I.MaxShield);
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

			LabelField("Ghost Data", EditorStyles.boldLabel);
			BeginHorizontal();
			PrefixLabel("Head Component");
			I.HeadText = TextField(I.HeadText);
			I.Head     = EnumField(I.Head);
			EndHorizontal();
			BeginHorizontal();
			PrefixLabel("Body Component");
			I.BodyText = TextField(I.BodyText);
			I.Body     = EnumField(I.Body);
			EndHorizontal();
			I.Flag = FlagField<Flag>("Flag", I.Flag);
			I.Team = FlagField<Team>("Team", I.Team);
			Space();

			LabelField("Physics", EditorStyles.boldLabel);
			BeginDisabledGroup(I.GetFlag(global::Flag.Pinned));
			I.Mass = FloatField("Mass", I.Mass);
			EndDisabledGroup();
			Space();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] float m_Radius;
	[SerializeField] float m_Height;
	[SerializeField] ushort m_MaxEnergy;
	[SerializeField] ushort m_MaxShield;
	[SerializeField] ushort m_MaxHealth;
	[SerializeField] byte m_Tag;
	[SerializeField] uint m_Immunity;

	[SerializeField] string m_HeadText;
	[SerializeField] string m_BodyText;
	[SerializeField] uint m_Flag;
	[SerializeField] uint m_Team;



	// Local Properties

	public string Name {
		get => gameObject.name;
		set => gameObject.name = value;
	}

	public float Radius {
		get => m_Radius;
		set => m_Radius = value;
	}
	public float Height {
		get => m_Height;
		set => m_Height = value;
	}

	public ushort MaxShield {
		get => m_MaxShield;
		set => m_MaxShield = value;
	}
	public ushort MaxHealth {
		get => m_MaxHealth;
		set => m_MaxHealth = value;
	}
	public ushort MaxEnergy {
		get => m_MaxEnergy;
		set => m_MaxEnergy = value;
	}

	public byte Tag {
		get => m_Tag;
		set => m_Tag = value;
	}
	public bool GetTag(Tag tag) => (Tag & (1u << (int)tag)) != 0u;
	public void SetTag(Tag tag, bool value) => Tag = value switch {
		true  => (byte)(Tag |  (1 << (int)tag)),
		false => (byte)(Tag & ~(1 << (int)tag)),
	};

	public uint Immunity {
		get => m_Immunity;
		set => m_Immunity = value;
	}
	public Immunity GetImmunity(Effect effect) =>
		(Immunity)((Immunity >> ((int)effect * 2)) & 0b11u);
	public void SetImmunity(Effect effect, Immunity immunity) =>
		Immunity = Immunity & ~(0b11u << ((int)effect * 2)) | (uint)immunity << ((int)effect * 2);



	// Ghost Properties

	public string HeadText {
		get => m_HeadText;
		set => m_HeadText = value;
	}
	public string BodyText {
		get => m_BodyText;
		set => m_BodyText = value;
	}
	public Head Head {
		get => Enum.TryParse(HeadText, out Head head) ? head : default;
		set => m_HeadText = value.ToString();
	}
	public Body Body {
		get => Enum.TryParse(BodyText, out Body body) ? body : default;
		set => m_BodyText = value.ToString();
	}

	public uint Flag {
		get => m_Flag;
		set {
			for (int i = 0; i < 8; i++) {
				var a = (m_Flag & (1u << i)) != 0u;
				var b = (value  & (1u << i)) != 0u;
				if (a == b) continue;
				switch ((Flag)i) {
					case global::Flag.Pinned:
						if (TryGetComponent(out PhysicsBodyAuthoring body)) body.Mass = b switch {
							true  => CreaturePhysics.PinnedMass,
							false => CreaturePhysics.InitialMass,
						};
						break;
					case global::Flag.Piercing:
						foreach (var shape in GetComponents<PhysicsShapeAuthoring>()) {
							var collidesWith = shape.CollidesWith;
							collidesWith.Value = b switch {
								true  => collidesWith.Value & ~(1u << (int)PhysicsCategory.Creature),
								false => collidesWith.Value |  (1u << (int)PhysicsCategory.Creature),
							};
							shape.CollidesWith = collidesWith;
						}
						break;
				}
			}
			m_Flag = value;
		}
	}
	public bool GetFlag(Flag flag) => (Flag & (1u << (int)flag)) != 0u;
	public void SetFlag(Flag flag, bool value) => Flag = value switch {
		true  => Flag |  (1u << (int)flag),
		false => Flag & ~(1u << (int)flag),
	};

	public uint Team {
		get => m_Team;
		set => m_Team = value;
	}
	public bool GetTeam(Team team) => (Team & (1u << (int)team)) != 0u;
	public void SetTeam(Team team, bool value) => Team = value switch {
		true  => Team |  (1u << (int)team),
		false => Team & ~(1u << (int)team),
	};



	// Physics Properties

	PhysicsBodyAuthoring  PhysicsBody  => GetComponent<PhysicsBodyAuthoring>();
	PhysicsShapeAuthoring PhysicsShape => GetComponent<PhysicsShapeAuthoring>();

	public float Mass {
		get => PhysicsBody.Mass;
		set => PhysicsBody.Mass = value;
	}



	// Baker

	public class Baker : Baker<CreatureAuthoring> {
		public override void Bake(CreatureAuthoring authoring) {
			Entity entity = GetEntity(TransformUsageFlags.Dynamic);
			AddComponent(entity, new CreatureInitialize());
			using var builder = new BlobBuilder(Allocator.Temp);
			ref var blob = ref builder.ConstructRoot<CreatureBlobData>();
			blob = new CreatureBlobData {

				Name        = authoring.Name,
				Radius      = authoring.Radius,
				Height      = authoring.Height,
				MaxShield   = authoring.MaxShield,
				MaxHealth   = authoring.MaxHealth,
				MaxEnergy   = authoring.MaxEnergy,
				Tag         = authoring.Tag,
				Immunity    = authoring.Immunity,
				InitialHead = authoring.Head,
				InitialBody = authoring.Body,

			};
			var value = builder.CreateBlobAssetReference<CreatureBlobData>(Allocator.Persistent);
			AddBlobAsset(ref value, out _);
			AddComponent(entity, new CreatureBlob { Value = value });
			AddComponent(entity, new CreatureCore {

				Head = authoring.Head,
				Body = authoring.Body,
				Flag = authoring.Flag,
				Team = authoring.Team,

			});
			AddComponent(entity, new CreatureTemp {

				Flag = ~authoring.Team,
				Team = ~authoring.Flag,

			});
			AddComponent(entity, new CreatureStatus {

				Shield = authoring.MaxShield,
				Health = authoring.MaxHealth,
				Energy = authoring.MaxEnergy,

			});
			AddBuffer<CreatureEffect>(entity);
			AddComponent(entity, new CreatureInput());

			bool hasTileDrawer   = authoring.TryGetComponent(out TileDrawerAuthoring   tileDrawer);
			bool hasSpriteDrawer = authoring.TryGetComponent(out SpriteDrawerAuthoring spriteDrawer);
			bool hasShadowDrawer = authoring.TryGetComponent(out ShadowDrawerAuthoring shadowDrawer);
			bool hasUIDrawer     = authoring.TryGetComponent(out UIDrawerAuthoring     uiDrawer);
			if (!hasTileDrawer   || !tileDrawer.enabled)   AddBuffer<TileDrawer>(entity);
			if (!hasSpriteDrawer || !spriteDrawer.enabled) AddBuffer<SpriteDrawer>(entity);
			if (!hasShadowDrawer || !shadowDrawer.enabled) AddBuffer<ShadowDrawer>(entity);
			if (!hasUIDrawer     || !uiDrawer.enabled)     AddBuffer<UIDrawer>(entity);

			bool hasInteractable = authoring.TryGetComponent(out InteractableAuthoring interactable);
			if (!hasInteractable || !interactable.enabled) AddComponent(entity, new Interactable());
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Initialize
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CreatureInitialize : IComponentData, IEnableableComponent { }



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Blob
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CreatureBlob : IComponentData {

	public BlobAssetReference<CreatureBlobData> Value;
}



public struct CreatureBlobData {

	// Fields

	public FixedString512Bytes Name;
	public float Radius;
	public float Height;
	public ushort MaxShield;
	public ushort MaxHealth;
	public ushort MaxEnergy;
	public byte Tag;
	public uint Immunity;
	public Head InitialHead;
	public Body InitialBody;



	// Methods

	public bool GetTag(Tag tag) => (Tag & (1u << (int)tag)) != 0;
	public void SetTag(Tag tag, bool value) => Tag = value switch {
		true  => (byte)(Tag |  (1u << (int)tag)),
		false => (byte)(Tag & ~(1u << (int)tag)),
	};

	public Immunity GetImmunity(Effect effect) =>
		(Immunity)((Immunity >> ((int)effect * 2)) & 0b11u);
	public void SetImmunity(Effect effect, Immunity immunity) =>
		Immunity = Immunity & ~(0b11u << ((int)effect * 2)) | (uint)immunity << ((int)effect * 2);
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

	const int HeadShift = 26;
	const int BodyShift = 16;
	const int FlagShift =  8;
	const int TeamShift =  0;

	const uint MotionXMask     = 0xF8000000u;
	const uint MotionYMask     = 0x07C00000u;
	const uint MotionXTickMask = 0x003FF800u;
	const uint MotionYTickMask = 0x000007FFu;

	const int MotionXShift     = 27;
	const int MotionYShift     = 22;
	const int MotionTickXShift = 11;
	const int MotionTickYShift =  0;

	const uint GravityFMask = 0xFF000000u;
	const uint KnockFMask   = 0x00FC0000u;
	const uint KnockXMask   = 0x0003F000u;
	const uint KnockYMask   = 0x00000FC0u;
	const uint KnockZMask   = 0x0000003Fu;

	const int GravityFShift = 24;
	const int KnockFShift   = 18;
	const int KnockXShift   = 12;
	const int KnockYShift   =  6;
	const int KnockZShift   =  0;



	// Fields

	[GhostField] public uint Data0;
	[GhostField] public uint Data1;
	[GhostField] public uint Data2;



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
	public bool GetFlag(Flag flag) => (Flag & (1u << (int)flag)) != 0u;
	public void SetFlag(Flag flag, bool value) => Flag = value switch {
		false => Flag & ~(1u << (int)flag),
		true  => Flag |  (1u << (int)flag),
	};

	public uint Team {
		get => (Data0 & TeamMask) >> TeamShift;
		set => Data0 = (Data0 & ~TeamMask) | (value << TeamShift);
	}
	public bool GetTeam(Team team) => (Team & (1u << (int)team)) != 0u;
	public void SetTeam(Team team, bool value) => Team = value switch {
		false => Team & ~(1u << (int)team),
		true  => Team |  (1u << (int)team),
	};



	public Motion MotionX {
		get => (Motion)((Data1 & MotionXMask) >> MotionXShift);
		set {
			if (MotionX != value) MotionXTick = 0;
			Data1 = (Data1 & ~MotionXMask) | ((uint)value << MotionXShift);
		}
	}
	public Motion MotionY {
		get => (Motion)((Data1 & MotionYMask) >> MotionYShift);
		set {
			if (MotionY != value) MotionYTick = 0;
			Data1 = (Data1 & ~MotionYMask) | ((uint)value << MotionYShift);
		}
	}
	public int MotionXTick {
		get => (int)((Data1 & MotionXTickMask) >> MotionTickXShift);
		set => Data1 = (Data1 & ~MotionXTickMask) | ((uint)(value % 2048) << MotionTickXShift);
	}
	public int MotionYTick {
		get => (int)((Data1 & MotionYTickMask) >> MotionTickYShift);
		set => Data1 = (Data1 & ~MotionYTickMask) | ((uint)(value % 2048) << MotionTickYShift);
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
		get => (int)((Data2 & GravityFMask) >> GravityFShift);
		set => Data2 = (Data2 & ~GravityFMask) | ((uint)math.clamp(value, 0, 255) << GravityFShift);
	}
	public float3 GravityVector {
		get => new(0f, (GravityFactor == 0) ? 0f : GravityFactor + 10f, 0f);
		set => GravityFactor = (int)math.round(math.max(0f, value.y - 10f));
	}
	public bool IsGrounded => (Data2 & GravityFMask) == 0u;

	public int KnockFactor {
		get => (int)((Data2 & KnockFMask) >> KnockFShift);
		set => Data2 = (Data2 & ~KnockFMask) | ((uint)math.clamp(value, 0, 63) << KnockFShift);
	}
	public float3 KnockVector {
		get {
			float x = (((Data2 & KnockXMask) >> KnockXShift) - 31f) * 0.0322581f;
			float y = (((Data2 & KnockYMask) >> KnockYShift) - 31f) * 0.0322581f;
			float z = (((Data2 & KnockZMask) >> KnockZShift) - 31f) * 0.0322581f;
			return KnockFactor * 0.125f * new float3(x, y, z);
		}
		set {
			KnockFactor = (int)(math.length(value) * 8f);
			float3 normalized = math.normalize(value);
			uint x = (uint)(math.round(normalized.x * 31f) + 31f) << KnockXShift;
			uint y = (uint)(math.round(normalized.y * 31f) + 31f) << KnockYShift;
			uint z = (uint)(math.round(normalized.z * 31f) + 31f) << KnockZShift;
			Data2 = (Data2 & ~(KnockXMask | KnockYMask | KnockZMask)) | x | y | z;
		}
	}
	public bool IsKnocked => (Data2 & KnockFMask) != 0u;
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

	const int HeadShift = 26;
	const int BodyShift = 16;
	const int FlagShift =  8;
	const int TeamShift =  0;

	const uint GravityFMask = 0xFF000000u;

	const int GravityFShift = 24;



	// Fields

	public uint Data0;
	public uint Data1;



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
	public bool GetFlag(Flag flag) => (Flag & (1u << (int)flag)) != 0u;
	public void SetFlag(Flag flag, bool value) => Flag = value switch {
		false => Flag & ~(1u << (int)flag),
		true  => Flag |  (1u << (int)flag),
	};

	public uint Team {
		get => (Data0 & TeamMask) >> TeamShift;
		set => Data0 = (Data0 & ~TeamMask) | (value << TeamShift);
	}
	public bool GetTeam(Team team) => (Team & (1u << (int)team)) != 0u;
	public void SetTeam(Team team, bool value) => Team = value switch {
		true  => Team |  (1u << (int)team),
		false => Team & ~(1u << (int)team),
	};



	public int GravityFactor {
		get => (int)((Data1 & GravityFMask) >> GravityFShift);
		set => Data1 = (Data1 & ~GravityFMask) | ((uint)math.clamp(value, 0, 255) << GravityFShift);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Status
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[GhostComponent]
public struct CreatureStatus : IComponentData {

	// Fields

	[GhostField] public ushort Shield;
	[GhostField] public ushort Health;
	[GhostField] public ushort Energy;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Effect
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[GhostComponent, InternalBufferCapacity(5)]
public struct CreatureEffect : IBufferElementData {

	// Constants

	const uint EffectMask   = 0xF0000000u;
	const uint StrengthMask = 0x0FFFF000u;
	const uint DurationMask = 0x00000FFFu;

	const int EffectShift   = 28;
	const int StrengthShift = 12;
	const int DurationShift =  0;



	// Fields

	[GhostField] public uint Data;



	// Properties

	[GhostField] public Effect Effect {
		get => (Effect)((Data & EffectMask) >> EffectShift);
		set => Data = (Data & ~EffectMask) | ((uint)value << EffectShift);
	}
	[GhostField] public ushort Value {
		get => (ushort)((Data & StrengthMask) >> StrengthShift);
		set => Data = (Data & ~StrengthMask) | ((uint)value << StrengthShift);
	}
	[GhostField] public int Tick {
		get => (int)((Data & DurationMask) >> DurationShift);
		set => Data = (Data & ~DurationMask) | ((uint)value << DurationShift);
	}
	[GhostField] public float Strength {
		get => (float)(Value * (Effect.IsValueType() ? 1f : 0.0001f));
		set => Value = (ushort)math.round(value * (Effect.IsValueType() ? 1f : 1000f));
	}
	[GhostField] public float Duration {
		get => (float)(Tick * NetworkManager.Ticktime);
		set => Tick = (int)math.round(value * NetworkManager.Tickrate);
	}
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
		strength *= multiplier;
		duration *= effect.IsValueType() ? 1f : multiplier;

		var element = new CreatureEffect() { Effect = effect };
		if (buffer.TryGetIndex(effect, out int index)) {
			var data = buffer[index];
			if (maxStrength == 0f) maxStrength = math.max(data.Strength, strength);
			if (maxDuration == 0f) maxDuration = math.max(data.Duration, duration);
			element.Strength = math.min(data.Strength + strength, maxStrength);
			element.Duration = math.min(data.Duration + duration, maxDuration);
			buffer.ElementAt(index) = element;
		} else {
			element.Strength = strength;
			element.Duration = duration;
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
// Creature Input
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CreatureInput : IInputComponentData {

	// Constants

	const uint KeyMask     = 0xFFFF0000u;
	const uint MoveFMask   = 0x0000C000u;
	const uint MoveDMask   = 0x00003F00u;
	const uint PingMask    = 0x000000F0u;
	const uint EmotionMask = 0x0000000Fu;

	const int KeyShift     = 16;
	const int MoveFShift   = 14;
	const int MoveDShift   =  8;
	const int PingShift    =  4;
	const int EmotionShift =  0;



	// Fields

	public uint Data;



	// Properties

	public uint Key {
		get => (Data & KeyMask) >> KeyShift;
		set => Data = (Data & ~KeyMask) | (value << KeyShift);
	}
	public bool GetKey(KeyAction key) => (Key & (1 << (int)key)) != 0;
	public void SetKey(KeyAction key, bool value) => Key = value switch {
		false => Key & ~(1u << (int)key),
		true  => Key |  (1u << (int)key),
	};

	public float MoveFactor {
		get => ((Data & MoveFMask) >> MoveFShift) * 0.333333f;
		set {
			uint moveFactor = (uint)math.round(math.saturate(value) * 3f);
			Data = (Data & ~MoveFMask) | (moveFactor << MoveFShift);
		}
	}
	public float3 MoveVector {
		get {
			if (MoveFactor == 0f) return float3.zero;
			else {
				float yawRadians = ((Data & MoveDMask) >> MoveDShift) * 5.625f * math.TORADIANS;
				return MoveFactor * new float3(math.sin(yawRadians), 0f, math.cos(yawRadians));
			}
		}
		set {
			MoveFactor = math.length(value);
			if (0f < MoveFactor) {
				float yaw = (math.atan2(value.x, value.z) * math.TODEGREES + 360f + 2.8125f) % 360f;
				Data = (Data & ~MoveDMask) | ((uint)(yaw * 0.177777f) << MoveDShift);
			}
		}
	}

	public uint Ping {
		get => (Data & PingMask) >> PingShift;
		set => Data = (Data & ~PingMask) | (value << PingShift);
	}
	public uint Emotion {
		get => (Data & EmotionMask) >> EmotionShift;
		set => Data = (Data & ~EmotionMask) | (value << EmotionShift);
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
		state.RequireForUpdate<SimulationSingleton>();
		state.RequireForUpdate<PrefabContainer>();
		state.RequireForUpdate<CreatureCore>();
	}

	public void OnUpdate(ref SystemState state) {
		var singleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
		state.Dependency = new CreatureFieldsComparisonJob {
		}.ScheduleParallel(state.Dependency);
		state.Dependency = new CreatureComponentsModificationJob {
			Buffer          = singleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
			PrefabContainer = SystemAPI.GetSingletonBuffer<PrefabContainer>(),
			BlobLookup      = SystemAPI.GetComponentLookup<CreatureBlob>(),
			CoreLookup      = SystemAPI.GetComponentLookup<CreatureCore>(),
			TempLookup      = SystemAPI.GetComponentLookup<CreatureTemp>(),
			StatusLookup    = SystemAPI.GetComponentLookup<CreatureStatus>(),
			EffectLookup    = SystemAPI.GetBufferLookup<CreatureEffect>(),
			ColliderLookup  = SystemAPI.GetComponentLookup<PhysicsCollider>(),
			MassLookup      = SystemAPI.GetComponentLookup<PhysicsMass>(),
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

			bool match = false;
			if      (temp.Head != core.Head) match = true;
			else if (temp.Body != core.Body) match = true;
			else if (temp.Team != core.Team) match = true;
			else if (temp.Flag != core.Flag) match = true;
			if (match) initialize.ValueRW = true;
		}
	}

	[BurstCompile, WithAll(typeof(CreatureInitialize))]
	partial struct CreatureComponentsModificationJob : IJobEntity {
		public EntityCommandBuffer.ParallelWriter Buffer;
		[ReadOnly] public DynamicBuffer<PrefabContainer> PrefabContainer;
		[NativeDisableParallelForRestriction] public ComponentLookup<CreatureBlob> BlobLookup;
		[NativeDisableParallelForRestriction] public ComponentLookup<CreatureCore> CoreLookup;
		[NativeDisableParallelForRestriction] public ComponentLookup<CreatureTemp> TempLookup;
		[NativeDisableParallelForRestriction] public ComponentLookup<CreatureStatus>  StatusLookup;
		[NativeDisableParallelForRestriction] public BufferLookup<CreatureEffect>     EffectLookup;
		[NativeDisableParallelForRestriction] public ComponentLookup<PhysicsMass>     MassLookup;
		[NativeDisableParallelForRestriction] public ComponentLookup<PhysicsCollider> ColliderLookup;
		[NativeDisableParallelForRestriction] public BufferLookup<TileDrawer>   TileLookup;
		[NativeDisableParallelForRestriction] public BufferLookup<SpriteDrawer> SpriteLookup;
		[NativeDisableParallelForRestriction] public BufferLookup<ShadowDrawer> ShadowLookup;
		[NativeDisableParallelForRestriction] public BufferLookup<UIDrawer>     UILookup;
		public void Execute([ChunkIndexInQuery] int sortKey,
			Entity entity,
			EnabledRefRW<CreatureInitialize> initialize) {

			var blob = BlobLookup[entity];
			var core = CoreLookup[entity];
			var temp = TempLookup[entity];
			var mass = MassLookup[entity];
			var collider = ColliderLookup[entity];

			initialize.ValueRW = false;
			mass.InverseInertia = float3.zero;
			var target = Entity.Null;
			foreach (var prefab in PrefabContainer.Reinterpret<Entity>()) {
				if (BlobLookup.HasComponent(prefab)) {
					if (BlobLookup[prefab].Value.Value.InitialBody == core.Body) {
						target = prefab;
						break;
					}
				}
			}
			if (temp.Head != core.Head) {
				var a = temp.Head.ToComponent();
				var b = core.Head.ToComponent();
				if (a != default) Buffer.RemoveComponent(sortKey, entity, a);
				if (b != default) Buffer.AddComponent(sortKey, entity, b);
				temp.Head = core.Head;
			}
			if (temp.Body != core.Body) {
				var a = temp.Body.ToComponent();
				var b = core.Body.ToComponent();
				if (a != default) Buffer.RemoveComponent(sortKey, entity, a);
				if (b != default) Buffer.AddComponent(sortKey, entity, b);
				temp.Body = core.Body;

				blob.Value       = BlobLookup[target].Value;
				core.MotionX     = CoreLookup[target].MotionX;
				core.MotionY     = CoreLookup[target].MotionY;
				core.MotionXTick = CoreLookup[target].MotionXTick;
				core.MotionYTick = CoreLookup[target].MotionYTick;
				mass.InverseMass = MassLookup[target].InverseMass;
				collider.Value   = ColliderLookup[target].Value;
				EffectLookup[entity].Clear();

				TileLookup[entity].Clear();
				SpriteLookup[entity].Clear();
				ShadowLookup[entity].Clear();
				UILookup[entity].Clear();
				foreach (var element in TileLookup[target])   TileLookup[entity].Add(element);
				foreach (var element in SpriteLookup[target]) SpriteLookup[entity].Add(element);
				foreach (var element in ShadowLookup[target]) ShadowLookup[entity].Add(element);
				foreach (var element in UILookup[target])     UILookup[entity].Add(element);
			}
			if (temp.Flag != core.Flag) {
				for (int i = 0; i < 8; i++) {
					var flag = (Flag)i;
					var a = temp.GetFlag(flag);
					var b = core.GetFlag(flag);
					if (a == b) continue;
					temp.SetFlag(flag, b);
					switch (flag) {
						case Flag.Pinned:
							mass.InverseMass = b switch {
								false => MassLookup[target].InverseMass,
								true  => 1f / CreaturePhysics.PinnedMass,
							};
							break;
						case Flag.Piercing:
							var filter = ColliderLookup[target].Value.Value.GetCollisionFilter();
							filter.CollidesWith = b switch {
								false => filter.CollidesWith |  (1u << (int)PhysicsCategory.Creature),
								true  => filter.CollidesWith & ~(1u << (int)PhysicsCategory.Creature),
							};
							if (!collider.Value.Value.GetCollisionFilter().Equals(filter)) {
								if (!collider.IsUnique) collider.MakeUnique(entity, Buffer, sortKey);
								collider.Value.Value.SetCollisionFilter(filter);
							}
							break;
						case Flag.Interactable:
							Buffer.SetComponentEnabled<Interactable>(sortKey, entity, b);
							break;
					}
				}
			}
			if (temp.Team != core.Team) {
				for (int i = 0; i < 8; i++) {
					var team = (Team)i;
					var a = core.GetTeam(team);
					var b = core.GetTeam(team);
					if (a == b) continue;
					temp.SetTeam(team, b);
				}
			}
			TempLookup[entity] = temp;
			CoreLookup[entity] = core;
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
			velocity.Linear = new float3(0f, 0f, 0f);
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
				float multiplier = CreaturePhysics.GravityMultiplier;
				velocity.Linear += multiplier * core.GravityVector;
			}
			if (core.IsKnocked) {
				float multiplier = CreaturePhysics.KnockMultiplier;
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
		state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
		state.RequireForUpdate<CameraManagerBridge>();
		state.RequireForUpdate<PrefabContainer>();
	}

	public void OnUpdate(ref SystemState state) {
		var system = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
		state.Dependency = new CreatureLandingParticleJob {
			buffer          = system.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
			CameraManager   = SystemAPI.GetSingletonRW<CameraManagerBridge>(),
			PrefabContainer = SystemAPI.GetSingletonBuffer<PrefabContainer>(true),
			Random          = new Random(1u + (uint)(4801 * SystemAPI.Time.ElapsedTime) % 1000),
		}.ScheduleParallel(state.Dependency);
	}

	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct CreatureLandingParticleJob : IJobEntity {
		public EntityCommandBuffer.ParallelWriter buffer;
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
				impactEnergy += temp.GravityFactor * CreaturePhysics.GravityMultiplier;
				impactEnergy += core.KnockFactor * CreaturePhysics.KnockMultiplier;
				if (impactEnergy < CreaturePhysics.DustThreshold) {
					var right   = CameraManager.ValueRO.Right;
					var forward = CameraManager.ValueRO.Forward;
					var radius  = blob.Value.Value.Radius;
					var position0 = transform.Position + right * radius - forward * radius;
					var position1 = transform.Position - right * radius - forward * radius;
					var prefab = PrefabContainer[(int)Prefab.SmokeTiny].Prefab;
					var smoke0 = buffer.Instantiate(sortKey, prefab);
					var smoke1 = buffer.Instantiate(sortKey, prefab);
					buffer.SetComponent(sortKey, smoke0, LocalTransform.FromPosition(position0));
					buffer.SetComponent(sortKey, smoke1, LocalTransform.FromPosition(position1));
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
			in CreatureBlob   blob,
			in CreatureStatus status,
			DynamicBuffer<UIDrawer> ui) {

			//if (core.BarTick == 0) {
			//	if (!drawer.UIs.IsEmpty) drawer.UIs.Clear();
			//} else {
				var data = blob.Value.Value;

				if (ui.Length < 7) {
					while (ui.Length < 7) ui.Add(new UIDrawer {
						Position  = new float3(0f, data.Height + 1f, 0f),
						Scale     = new float2(1f, 1f),
						BaseColor = color.white,
					});
					ui.ElementAt(0).UI = UI.BarM;
					ui.ElementAt(1).UI = UI.BarL;
					ui.ElementAt(2).UI = UI.BarR;
					ui.ElementAt(6).UI = UI.Bar;
					ui.ElementAt(5).UI = UI.Bar;
					ui.ElementAt(4).UI = UI.Bar;
					ui.ElementAt(3).UI = UI.Bar;
					ui.ElementAt(6).BaseColor = new color(0xFFFFFF);
					ui.ElementAt(5).BaseColor = new color(0xFFD36B);
					ui.ElementAt(4).BaseColor = new color(0x7F7F7F);
					ui.ElementAt(3).BaseColor = new color(0x5F5F5F);
				}
				//float a = 2f * core.BarCooldown;
				//for (int i = 0; i < drawer.Length; i++) {
				//	drawer.UIs.ElementAt(i).BaseColor.a = a;
				//}
				float ratio = data.Radius * 2f / (data.MaxHealth + data.MaxShield);
				float maxHealth = math.max((float)status.Health, data.MaxHealth);
				float maxShield = math.max((float)status.Shield, data.MaxShield);
				float max = (maxHealth + maxShield) * ratio;

				float pureHealth = math.min((float)status.Health, data.MaxHealth) * ratio;
				float pureShield = math.min((float)status.Shield, data.MaxShield) * ratio;
				float overHealth = math.max(0, status.Health - data.MaxHealth) * ratio;
				float overShield = math.max(0, status.Shield - data.MaxShield) * ratio;
				float pureHealthPivot = max * -0.5f + pureHealth * 0.5f;
				float overHealthPivot = pureHealthPivot + pureHealth;
				float pureShieldPivot = overHealthPivot + overHealth;
				float overShieldPivot = pureShieldPivot + pureShield;

				ui.ElementAt(0).Scale.x = max;
				ui.ElementAt(1).Pivot.x = max * -0.5f - 0.5f;
				ui.ElementAt(2).Pivot.x = max *  0.5f + 0.5f;
				ui.ElementAt(6).Scale.x = pureHealth;
				ui.ElementAt(5).Scale.x = overHealth;
				ui.ElementAt(4).Scale.x = pureShield;
				ui.ElementAt(3).Scale.x = overShield;
				ui.ElementAt(6).Pivot.x = pureHealthPivot;
				ui.ElementAt(5).Pivot.x = overHealthPivot;
				ui.ElementAt(4).Pivot.x = pureShieldPivot;
				ui.ElementAt(3).Pivot.x = overShieldPivot;
			//}
		}
	}
}

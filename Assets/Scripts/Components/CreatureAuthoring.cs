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



// Creature Cores

public enum Tag : byte {
	Undead,
	Boss,
}

public enum Head : ushort {
	None,
	Player,
}
public static class HeadExtensions {
	public static Head ToEnum(this ComponentType type) => type switch {
		_ when type == ComponentType.ReadWrite<PlayerHead>() => Head.Player,
		_ => default,
	};
	public static ComponentType ToComponent(this Head head) => head switch {
		Head.Player => ComponentType.ReadWrite<PlayerHead>(),
		_ => default,
	};
}

public enum Body : ushort {
	None,
	Player,
}
public static class BodyExtensions {
	public static Body ToEnum(this ComponentType type) => type switch {
		_ when type == ComponentType.ReadWrite<PlayerBody>() => Body.Player,
		_ => default,
	};
	public static ComponentType ToComponent(this Body body) => body switch {
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



// Physics

public enum PhysicsCategory : byte {
	Creature,
}
public static class CreaturePhysics {
	public const float InitialMass = 1f;
	public const float PinnedMass  = 1f * 1000000f;
	public const float GravityMultiplier =  -9.81f * NetworkManager.Ticktime;
	public const float KnockMultiplier   = 256.00f * NetworkManager.Ticktime;
	public const float DustThreshold = -0.4f;
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
				I.Radius    = FloatField ("Radius",     I.Radius);
				I.Height    = FloatField ("Height",     I.Height);
				I.MaxShield = UShortField("Max Shield", I.MaxShield);
				I.MaxHealth = UShortField("Max Health", I.MaxHealth);
				I.MaxEnergy = UShortField("Max Energy", I.MaxEnergy);
				I.Tag       = (byte)FlagField<Tag>("Tag", I.Tag);
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
				I.MaskColor = ColorField("Mask Color", I.MaskColor);
				BeginHorizontal();
				PrefixLabel("Head Component");
				I.HeadString = TextField(I.HeadString);
				I.Head       = EnumField(I.Head);
				EndHorizontal();
				BeginHorizontal();
				PrefixLabel("Body Component");
				I.BodyString = TextField(I.BodyString);
				I.Body       = EnumField(I.Body);
				EndHorizontal();
				I.Flag = FlagField<Flag>("Flag", I.Flag);
				I.Team = FlagField<Team>("Team", I.Team);
				Space();

				LabelField("Physics", EditorStyles.boldLabel);
				BeginDisabledGroup(I.HasFlag(global::Flag.Pinned));
				I.Mass = FloatField("Mass", I.Mass);
				EndDisabledGroup();
				Space();

				End();
			}
		}
	#endif



	// Fields

	[SerializeField] float  m_Radius;
	[SerializeField] float  m_Height;
	[SerializeField] ushort m_MaxEnergy;
	[SerializeField] ushort m_MaxShield;
	[SerializeField] ushort m_MaxHealth;
	[SerializeField] byte   m_Tag;
	[SerializeField] uint   m_Immunity;

	[SerializeField] color  m_MaskColor;
	[SerializeField] string m_HeadString;
	[SerializeField] string m_BodyString;
	[SerializeField] uint   m_Flag;
	[SerializeField] uint   m_Team;



	// Properties

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
	public bool HasTag   (Tag tag) => GetTag(tag);
	public void AddTag   (Tag tag) => SetTag(tag, true);
	public void RemoveTag(Tag tag) => SetTag(tag, false);

	public uint Immunity {
		get => m_Immunity;
		set => m_Immunity = value;
	}
	public Immunity GetImmunity(Effect effect) {
		return (Immunity)((Immunity >> ((int)effect * 2)) & 0b11u);
	}
	public void SetImmunity(Effect effect, Immunity immunity) {
		Immunity &= ~(0b11u << ((int)effect * 2));
		Immunity |= (uint)immunity << ((int)effect * 2);
	}



	public color MaskColor {
		get => m_MaskColor;
		set => m_MaskColor = value;
	}

	public string HeadString {
		get => m_HeadString;
		set => m_HeadString = value;
	}
	public string BodyString {
		get => m_BodyString;
		set => m_BodyString = value;
	}
	public Head Head {
		get => Enum.TryParse(HeadString, out Head head) ? head : default;
		set => m_HeadString = value.ToString();
	}
	public Body Body {
		get => Enum.TryParse(BodyString, out Body body) ? body : default;
		set => m_BodyString = value.ToString();
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
	public bool HasFlag   (Flag flag) => GetFlag(flag);
	public void AddFlag   (Flag flag) => SetFlag(flag, true);
	public void RemoveFlag(Flag flag) => SetFlag(flag, false);

	public uint Team {
		get => m_Team;
		set => m_Team = value;
	}
	public bool GetTeam(Team team) => (Team & (1u << (int)team)) != 0u;
	public void SetTeam(Team team, bool value) => Team = value switch {
		true  => Team |  (1u << (int)team),
		false => Team & ~(1u << (int)team),
	};
	public bool HasTeam   (Team team) => GetTeam(team);
	public void AddTeam   (Team team) => SetTeam(team, true);
	public void RemoveTeam(Team team) => SetTeam(team, false);



	public float Mass {
		get   =>  TryGetComponent(out PhysicsBodyAuthoring body) ? body.Mass : default;
		set { if (TryGetComponent(out PhysicsBodyAuthoring body))  body.Mass = value; }
	}



	// Baker

	public class Baker : Baker<CreatureAuthoring> {
		public override void Bake(CreatureAuthoring authoring) {
			var creatureBlob = new CreatureBlobData {

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
			var creatureCore = new CreatureCore {

				MaskColor = authoring.MaskColor,
				Head      = authoring.Head,
				Body      = authoring.Body,
				Flag      = authoring.Flag,
				Team      = authoring.Team,

			};
			var creatureTemp = new CreatureTemp {

				Flag = ~authoring.Team,
				Team = ~authoring.Flag,

			};
			var creatureStatus = new CreatureStatus {

				Shield = authoring.MaxShield,
				Health = authoring.MaxHealth,
				Energy = authoring.MaxEnergy,

			};
			Entity entity = GetEntity(TransformUsageFlags.Dynamic);
			AddComponent(entity, new CreatureInitialize());
			AddComponent(entity, new CreatureInput());

			using (var builder = new BlobBuilder(Allocator.Temp)) {
				ref var blob = ref builder.ConstructRoot<CreatureBlobData>();
				blob = creatureBlob;
				var value = builder.CreateBlobAssetReference<CreatureBlobData>(Allocator.Persistent);
				AddBlobAsset(ref value, out var hash);
				AddComponent(entity, new CreatureBlob { Value = value, });
			}
			AddComponent(entity, creatureCore);
			AddComponent(entity, creatureTemp);
			AddComponent(entity, creatureStatus);
			AddBuffer<CreatureEffect>(entity);

			bool hasTileDrawer   = authoring.TryGetComponent(out TileDrawerAuthoring   tileDrawer);
			bool hasSpriteDrawer = authoring.TryGetComponent(out SpriteDrawerAuthoring spriteDrawer);
			bool hasShadowDrawer = authoring.TryGetComponent(out ShadowDrawerAuthoring shadowDrawer);
			bool hasUIDrawer     = authoring.TryGetComponent(out UIDrawerAuthoring     uiDrawer);
			if (!hasTileDrawer   || !tileDrawer  .enabled) AddBuffer<TileDrawer  >(entity);
			if (!hasSpriteDrawer || !spriteDrawer.enabled) AddBuffer<SpriteDrawer>(entity);
			if (!hasShadowDrawer || !shadowDrawer.enabled) AddBuffer<ShadowDrawer>(entity);
			if (!hasUIDrawer     || !uiDrawer    .enabled) AddBuffer<UIDrawer    >(entity);

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
// Creature Input
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CreatureInput : IInputComponentData {

	public uint Data;



	public uint Key {
		get => this.GetKey();
		set => this.SetKey(value);
	}
	public float MoveFactor {
		get => this.GetMoveFactor();
		set => this.SetMoveFactor(value);
	}
	public float3 MoveVector {
		get => this.GetMoveVector();
		set => this.SetMoveVector(value);
	}
	public uint Ping {
		get => this.GetPing();
		set => this.SetPing(value);
	}
	public uint Emotion {
		get => this.GetEmotion();
		set => this.SetEmotion(value);
	}
}



public static class CreatureInputExtensions {

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



	public static uint GetKey(this in CreatureInput input) {
		return (input.Data & KeyMask) >> KeyShift;
	}
	public static void SetKey(this ref CreatureInput input, uint value) {
		input.Data = (input.Data & ~KeyMask) | (value << KeyShift);
	}
	public static bool GetKey(this in CreatureInput input, KeyAction key) {
		return (input.GetKey() & (1 << (int)key)) != 0;
	}
	public static void SetKey(this ref CreatureInput input, KeyAction key, bool value) {
		input.SetKey(value switch {
			true  => input.GetKey() |  (1u << (int)key),
			false => input.GetKey() & ~(1u << (int)key),
		});
	}

	public static float GetMoveFactor(this in CreatureInput input) {
		return ((input.Data & MoveFMask) >> MoveFShift) * 0.333333f;
	}
	public static void SetMoveFactor(this ref CreatureInput input, float value) {
		uint moveFactor = (uint)math.round(math.saturate(value) * 3f);
		input.Data = (input.Data & ~MoveFMask) | (moveFactor << MoveFShift);
	}
	public static float3 GetMoveVector(this in CreatureInput input) {
		if (input.GetMoveFactor() == 0f) return float3.zero;
		else {
			float yawRadians = ((input.Data & MoveDMask) >> MoveDShift) * 5.625f * math.TORADIANS;
			return input.GetMoveFactor() * new float3(math.sin(yawRadians), 0f, math.cos(yawRadians));
		}
	}
	public static void SetMoveVector(this ref CreatureInput input, float3 value) {
		input.SetMoveFactor(math.length(value));
		if (0f < input.GetMoveFactor()) {
			float yaw = (math.atan2(value.x, value.z) * math.TODEGREES + 360f + 2.8125f) % 360f;
			input.Data = (input.Data & ~MoveDMask) | ((uint)(yaw * 0.177777f) << MoveDShift);
		}
	}

	public static uint GetPing(this in CreatureInput input) {
		return (input.Data & PingMask) >> PingShift;
	}
	public static void SetPing(this ref CreatureInput input, uint value) {
		input.Data = (input.Data & ~PingMask) | (value << PingShift);
	}

	public static uint GetEmotion(this in CreatureInput input) {
		return (input.Data & EmotionMask) >> EmotionShift;
	}
	public static void SetEmotion(this ref CreatureInput input, uint value) {
		input.Data = (input.Data & ~EmotionMask) | (value << EmotionShift);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Blob
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CreatureBlob : IComponentData {

	public BlobAssetReference<CreatureBlobData> Value;
}



public struct CreatureBlobData {

	public FixedString512Bytes Name;
	public float  Radius;
	public float  Height;
	public ushort MaxShield;
	public ushort MaxHealth;
	public ushort MaxEnergy;
	public byte   Tag;
	public uint   Immunity;
	public Head   InitialHead;
	public Body   InitialBody;
}



public static class CreatureBlobAssetExtensions {

	public static bool GetTag(this in CreatureBlobData data, Tag tag) {
		return (data.Tag & (1 << (int)tag)) != 0;
	}
	public static void SetTag(this ref CreatureBlobData data, Tag tag, bool value) {
		data.Tag = value switch {
			true  => (byte)(data.Tag |  (1u << (int)tag)),
			false => (byte)(data.Tag & ~(1u << (int)tag)),
		};
	}
	public static bool HasTag   (this in  CreatureBlobData data, Tag tag) => data.GetTag(tag);
	public static void AddTag   (this ref CreatureBlobData data, Tag tag) => data.SetTag(tag, true);
	public static void RemoveTag(this ref CreatureBlobData data, Tag tag) => data.SetTag(tag, false);

	public static Immunity GetImmunity(this in CreatureBlobData data, Effect effect) {
		return (Immunity)((data.Immunity >> ((int)effect * 2)) & 0b11u);
	}
	public static void SetImmunity(this ref CreatureBlobData data, Effect effect, Immunity immunity) {
		data.Immunity &= ~(0b11u << ((int)effect * 2));
		data.Immunity |= (uint)immunity << ((int)effect * 2);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Core
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[GhostComponent]
public struct CreatureCore : IComponentData {

	[GhostField] public color MaskColor;
	[GhostField] public uint  Data0;
	[GhostField] public uint  Data1;
	[GhostField] public uint  Data2;



	public Head Head {
		get => CreatureCoreExtensions.GetHead(in this);
		set => CreatureCoreExtensions.SetHead(ref this, value);
	}
	public Body Body {
		get => CreatureCoreExtensions.GetBody(in this);
		set => CreatureCoreExtensions.SetBody(ref this, value);
	}
	public uint Flag {
		get => CreatureCoreExtensions.GetFlag(in this);
		set => CreatureCoreExtensions.SetFlag(ref this, value);
	}
	public uint Team {
		get => CreatureCoreExtensions.GetTeam(in this);
		set => CreatureCoreExtensions.SetTeam(ref this, value);
	}

	public Motion MotionX {
		get => CreatureCoreExtensions.GetMotionX(in this);
		set => CreatureCoreExtensions.SetMotionX(ref this, value);
	}
	public Motion MotionY {
		get => CreatureCoreExtensions.GetMotionY(in this);
		set => CreatureCoreExtensions.SetMotionY(ref this, value);
	}
	public int MotionXTick {
		get => CreatureCoreExtensions.GetMotionXTick(in this);
		set => CreatureCoreExtensions.SetMotionXTick(ref this, value);
	}
	public int MotionYTick {
		get => CreatureCoreExtensions.GetMotionYTick(in this);
		set => CreatureCoreExtensions.SetMotionYTick(ref this, value);
	}
	public float MotionXOffset {
		get => CreatureCoreExtensions.GetMotionXOffset(in this);
		set => CreatureCoreExtensions.SetMotionXOffset(ref this, value);
	}
	public float MotionYOffset {
		get => CreatureCoreExtensions.GetMotionYOffset(in this);
		set => CreatureCoreExtensions.SetMotionYOffset(ref this, value);
	}

	public int GravityFactor {
		get => CreatureCoreExtensions.GetGravityFactor(	this);
		set => CreatureCoreExtensions.SetGravityFactor(ref this, value);
	}
	public float3 GravityVector {
		get => CreatureCoreExtensions.GetGravityVector(in this);
		set => CreatureCoreExtensions.SetGravityVector(ref this, value);
	}
	public int KnockFactor {
		get => CreatureCoreExtensions.GetKnockFactor(in this);
		set => CreatureCoreExtensions.SetKnockFactor(ref this, value);
	}
	public float3 KnockVector {
		get => CreatureCoreExtensions.GetKnockVector(in this);
		set => CreatureCoreExtensions.SetKnockVector(ref this, value);
	}
}



public static class CreatureCoreExtensions {

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



	public static Head GetHead(this in CreatureCore core) {
		return (Head)((core.Data0 & HeadMask) >> HeadShift);
	}
	public static void SetHead(this ref CreatureCore core, Head value) {
		core.Data0 = (core.Data0 & ~HeadMask) | ((uint)value << HeadShift);
	}

	public static Body GetBody(this in CreatureCore core) {
		return (Body)((core.Data0 & BodyMask) >> BodyShift);
	}
	public static void SetBody(this ref CreatureCore core, Body value) {
		core.Data0 = (core.Data0 & ~BodyMask) | ((uint)value << BodyShift);
	}

	public static uint GetFlag(this in CreatureCore core) {
		return (core.Data0 & FlagMask) >> FlagShift;
	}
	public static void SetFlag(this ref CreatureCore core, uint value) {
		core.Data0 = (core.Data0 & ~FlagMask) | (value << FlagShift);
	}
	public static bool GetFlag(this in CreatureCore core, Flag flag) {
		return (core.GetFlag() & (1u << (int)flag)) != 0u;
	}
	public static void SetFlag(this ref CreatureCore core, Flag flag, bool value) {
		core.SetFlag(value switch {
			true  => core.GetFlag() |  (1u << (int)flag),
			false => core.GetFlag() & ~(1u << (int)flag),
		});
	}
	public static bool HasFlag   (this in  CreatureCore core, Flag flag) => core.GetFlag(flag);
	public static void AddFlag   (this ref CreatureCore core, Flag flag) => core.SetFlag(flag, true);
	public static void RemoveFlag(this ref CreatureCore core, Flag flag) => core.SetFlag(flag, false);

	public static uint GetTeam(this in CreatureCore core) {
		return (core.Data0 & TeamMask) >> TeamShift;
	}
	public static void SetTeam(this ref CreatureCore core, uint value) {
		core.Data0 = (core.Data0 & ~TeamMask) | (value << TeamShift);
	}
	public static bool GetTeam(this in CreatureCore core, Team team) {
		return (core.GetTeam() & (1u << (int)team)) != 0u;
	}
	public static void SetTeam(this ref CreatureCore core, Team team, bool value) {
		core.SetTeam(value switch {
			true  => core.GetTeam() |  (1u << (int)team),
			false => core.GetTeam() & ~(1u << (int)team),
		});
	}
	public static bool HasTeam   (this in  CreatureCore core, Team team) => core.GetTeam(team);
	public static void AddTeam   (this ref CreatureCore core, Team team) => core.SetTeam(team, true);
	public static void RemoveTeam(this ref CreatureCore core, Team team) => core.SetTeam(team, false);



	public static Motion GetMotionX(this in CreatureCore core) {
		return (Motion)((core.Data1 & MotionXMask) >> MotionXShift);
	}
	public static void SetMotionX(this ref CreatureCore core, Motion value) {
		core.Data1 = (core.Data1 & ~MotionXMask) | ((uint)value << MotionXShift);
		core.SetMotionXTick(0);
	}
	public static Motion GetMotionY(this in CreatureCore core) {
		return (Motion)((core.Data1 & MotionYMask) >> MotionYShift);
	}
	public static void SetMotionY(this ref CreatureCore core, Motion value) {
		core.Data1 = (core.Data1 & ~MotionYMask) | ((uint)value << MotionYShift);
		core.SetMotionYTick(0);
	}

	public static int GetMotionXTick(this in CreatureCore core) {
		return (int)((core.Data1 & MotionXTickMask) >> MotionTickXShift);
	}
	public static void SetMotionXTick(this ref CreatureCore core, int value) {
		core.Data1 = (core.Data1 & ~MotionXTickMask) | ((uint)(value % 2048) << MotionTickXShift);
	}
	public static int GetMotionYTick(this in CreatureCore core) {
		return (int)((core.Data1 & MotionYTickMask) >> MotionTickYShift);
	}
	public static void SetMotionYTick(this ref CreatureCore core, int value) {
		core.Data1 = (core.Data1 & ~MotionYTickMask) | ((uint)(value % 2048) << MotionTickYShift);
	}

	public static float GetMotionXOffset(this in CreatureCore core) {
		return (float)(core.GetMotionXTick() * NetworkManager.Ticktime);
	}
	public static void SetMotionXOffset(this ref CreatureCore core, float value) {
		core.SetMotionXTick((int)math.round(value * NetworkManager.Tickrate));
	}
	public static float GetMotionYOffset(this in CreatureCore core) {
		return (float)(core.GetMotionYTick() * NetworkManager.Ticktime);
	}
	public static void SetMotionYOffset(this ref CreatureCore core, float value) {
		core.SetMotionYTick((int)math.round(value * NetworkManager.Tickrate));
	}



	public static int GetGravityFactor(this in CreatureCore core) {
		return (int)((core.Data2 & GravityFMask) >> GravityFShift);
	}
	public static void SetGravityFactor(this ref CreatureCore core, int value) {
		core.Data2 = (core.Data2 & ~GravityFMask) | ((uint)math.clamp(value, 0, 255) << GravityFShift);
	}
	public static float3 GetGravityVector(this in CreatureCore core) {
		return new(0f, (core.GetGravityFactor() == 0) ? 0f : 10f + core.GetGravityFactor(), 0f);
	}
	public static void SetGravityVector(this ref CreatureCore core, float3 value) {
		core.SetGravityFactor(0);
	}
	public static bool IsGrounded(this in CreatureCore core) {
		return (core.Data2 & GravityFMask) == 0u;
	}

	public static int GetKnockFactor(this in CreatureCore core) {
		return (int)((core.Data2 & KnockFMask) >> KnockFShift);
	}
	public static void SetKnockFactor(this ref CreatureCore core, int value) {
		core.Data2 = (core.Data2 & ~KnockFMask) | ((uint)math.clamp(value, 0, 63) << KnockFShift);
	}
	public static float3 GetKnockVector(this in CreatureCore core) {
		float x = (((core.Data2 & KnockXMask) >> KnockXShift) - 31f) * 0.0322581f;
		float y = (((core.Data2 & KnockYMask) >> KnockYShift) - 31f) * 0.0322581f;
		float z = (((core.Data2 & KnockZMask) >> KnockZShift) - 31f) * 0.0322581f;
		return core.GetKnockFactor() * 0.125f * new float3(x, y, z);
	}
	public static void SetKnockVector(this ref CreatureCore core, float3 value) {
		core.SetKnockFactor((int)(math.length(value) * 8f));
		float3 normalized = math.normalize(value);
		uint x = (uint)(math.round(normalized.x * 31f) + 31f) << KnockXShift;
		uint y = (uint)(math.round(normalized.y * 31f) + 31f) << KnockYShift;
		uint z = (uint)(math.round(normalized.z * 31f) + 31f) << KnockZShift;
		core.Data2 = (core.Data2 & ~(KnockXMask | KnockYMask | KnockZMask)) | x | y | z;
	}
	public static bool IsKnocked(this in CreatureCore core) {
		return (core.Data2 & KnockFMask) != 0u;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Temp
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CreatureTemp : IComponentData {

	public uint Data0;
	public uint Data1;



	public Head Head {
		get => CreatureTempExtensions.GetHead(in this);
		set => CreatureTempExtensions.SetHead(ref this, value);
	}
	public Body Body {
		get => CreatureTempExtensions.GetBody(in this);
		set => CreatureTempExtensions.SetBody(ref this, value);
	}
	public uint Flag {
		get => CreatureTempExtensions.GetFlag(in this);
		set => CreatureTempExtensions.SetFlag(ref this, value);
	}
	public uint Team {
		get => CreatureTempExtensions.GetTeam(in this);
		set => CreatureTempExtensions.SetTeam(ref this, value);
	}
}



public static class CreatureTempExtensions {

	const uint HeadMask = 0xFC000000u;
	const uint BodyMask = 0x03FF0000u;
	const uint FlagMask = 0x0000FF00u;
	const uint TeamMask = 0x000000FFu;

	const int HeadShift = 26;
	const int BodyShift = 16;
	const int FlagShift =  8;
	const int TeamShift =  0;



	public static Head GetHead(this in CreatureTemp temp) {
		return (Head)((temp.Data0 & HeadMask) >> HeadShift);
	}
	public static void SetHead(this ref CreatureTemp temp, Head value) {
		temp.Data0 = (temp.Data0 & ~HeadMask) | ((uint)value << HeadShift);
	}

	public static Body GetBody(this in CreatureTemp temp) {
		return (Body)((temp.Data0 & BodyMask) >> BodyShift);
	}
	public static void SetBody(this ref CreatureTemp temp, Body value) {
		temp.Data0 = (temp.Data0 & ~BodyMask) | ((uint)value << BodyShift);
	}

	public static uint GetFlag(this in CreatureTemp temp) {
		return (temp.Data0 & FlagMask) >> FlagShift;
	}
	public static void SetFlag(this ref CreatureTemp temp, uint value) {
		temp.Data0 = (temp.Data0 & ~FlagMask) | (value << FlagShift);
	}
	public static bool GetFlag(this in CreatureTemp temp, Flag flag) {
		return (temp.GetFlag() & (1u << (int)flag)) != 0u;
	}
	public static void SetFlag(this ref CreatureTemp temp, Flag flag, bool value) {
		temp.SetFlag(value switch {
			true  => temp.GetFlag() |  (1u << (int)flag),
			false => temp.GetFlag() & ~(1u << (int)flag),
		});
	}
	public static bool HasFlag   (this in  CreatureTemp temp, Flag flag) => temp.GetFlag(flag);
	public static void AddFlag   (this ref CreatureTemp temp, Flag flag) => temp.SetFlag(flag, true);
	public static void RemoveFlag(this ref CreatureTemp temp, Flag flag) => temp.SetFlag(flag, false);

	public static uint GetTeam(this in CreatureTemp temp) {
		return (temp.Data0 & TeamMask) >> TeamShift;
	}
	public static void SetTeam(this ref CreatureTemp temp, uint value) {
		temp.Data0 = (temp.Data0 & ~TeamMask) | (value << TeamShift);
	}
	public static bool GetTeam(this in CreatureTemp temp, Team team) {
		return (temp.GetTeam() & (1u << (int)team)) != 0u;
	}
	public static void SetTeam(this ref CreatureTemp temp, Team team, bool value) {
		temp.SetTeam(value switch {
			true  => temp.GetTeam() |  (1u << (int)team),
			false => temp.GetTeam() & ~(1u << (int)team),
		});
	}
	public static bool HasTeam   (this in  CreatureTemp temp, Team team) => temp.GetTeam(team);
	public static void AddTeam   (this ref CreatureTemp temp, Team team) => temp.SetTeam(team, true);
	public static void RemoveTeam(this ref CreatureTemp temp, Team team) => temp.SetTeam(team, false);
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Status
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[GhostComponent]
public struct CreatureStatus : IComponentData {

	[GhostField] public ushort Shield;
	[GhostField] public ushort Health;
	[GhostField] public ushort Energy;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Effect
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[GhostComponent, InternalBufferCapacity(5)]
public struct CreatureEffect : IBufferElementData {

	[GhostField] public uint Data;
}



public static class CreatureEffectBufferExtensions {

	const uint EffectMask   = 0xF0000000u;
	const uint StrengthMask = 0x0FFFF000u;
	const uint DurationMask = 0x00000FFFu;

	const int EffectShift   = 28;
	const int StrengthShift = 12;
	const int DurationShift =  0;



	public static Effect GetEffect(this in CreatureEffect effect) {
		return (Effect)((effect.Data & EffectMask) >> EffectShift);
	}
	public static void SetEffect(this ref CreatureEffect effect, Effect value) {
		effect.Data = (effect.Data & ~EffectMask) | ((uint)value << EffectShift);
	}

	public static ushort GetValue(this in CreatureEffect effect) {
		return (ushort)((effect.Data & StrengthMask) >> StrengthShift);
	}
	public static void SetValue(this ref CreatureEffect effect, ushort value) {
		effect.Data = (effect.Data & ~StrengthMask) | ((uint)value << StrengthShift);
	}
	public static float GetStrength(this in CreatureEffect effect) {
		return (float)(effect.GetValue() * (effect.GetEffect().IsValueType() ? 1f : 0.0001f));
	}
	public static void SetStrength(this ref CreatureEffect effect, float value) {
		effect.SetValue((ushort)math.round(value * (effect.GetEffect().IsValueType() ? 1f : 1000f)));
	}

	public static int GetTick(this in CreatureEffect effect) {
		return (int)((effect.Data & DurationMask) >> DurationShift);
	}
	public static void SetTick(this ref CreatureEffect effect, int value) {
		effect.Data = (effect.Data & ~DurationMask) | ((uint)value << DurationShift);
	}
	public static float GetDuration(this in CreatureEffect effect) {
		return (float)(effect.GetTick() * NetworkManager.Ticktime);
	}
	public static void SetDuration(this ref CreatureEffect effect, float value) {
		effect.SetTick((int)math.round(value * NetworkManager.Tickrate));
	}



	public static bool TryGetIndex(
		this DynamicBuffer<CreatureEffect> buffer, Effect effect, out int index) {
		for (int i = 0; i < buffer.Length; i++) if (buffer[i].GetEffect() == effect) {
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

		var element = new CreatureEffect();
		element.SetEffect(effect);
		if (buffer.TryGetIndex(effect, out int index)) {
			var data = buffer[index];
			if (maxStrength == 0f) maxStrength = math.max(data.GetStrength(), strength);
			if (maxDuration == 0f) maxDuration = math.max(data.GetDuration(), duration);
			element.SetStrength(math.min(data.GetStrength() + strength, maxStrength));
			element.SetDuration(math.min(data.GetDuration() + duration, maxDuration));
			buffer.ElementAt(index) = element;
		} else {
			element.SetStrength(strength);
			element.SetDuration(duration);
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
			BlobGroup       = SystemAPI.GetComponentLookup<CreatureBlob   >(),
			CoreGroup       = SystemAPI.GetComponentLookup<CreatureCore   >(),
			TempGroup       = SystemAPI.GetComponentLookup<CreatureTemp   >(),
			StatusGroup     = SystemAPI.GetComponentLookup<CreatureStatus >(),
			EffectGroup     = SystemAPI.GetBufferLookup   <CreatureEffect >(),
			ColliderGroup   = SystemAPI.GetComponentLookup<PhysicsCollider>(),
			MassGroup       = SystemAPI.GetComponentLookup<PhysicsMass    >(),
			TileGroup       = SystemAPI.GetBufferLookup<TileDrawer  >(),
			SpriteGroup     = SystemAPI.GetBufferLookup<SpriteDrawer>(),
			ShadowGroup     = SystemAPI.GetBufferLookup<ShadowDrawer>(),
			UIGroup         = SystemAPI.GetBufferLookup<UIDrawer    >(),
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
		[NativeDisableParallelForRestriction] public ComponentLookup<CreatureBlob> BlobGroup;
		[NativeDisableParallelForRestriction] public ComponentLookup<CreatureCore> CoreGroup;
		[NativeDisableParallelForRestriction] public ComponentLookup<CreatureTemp> TempGroup;
		[NativeDisableParallelForRestriction] public ComponentLookup<CreatureStatus > StatusGroup;
		[NativeDisableParallelForRestriction] public BufferLookup   <CreatureEffect > EffectGroup;
		[NativeDisableParallelForRestriction] public ComponentLookup<PhysicsCollider> ColliderGroup;
		[NativeDisableParallelForRestriction] public ComponentLookup<PhysicsMass    > MassGroup;
		[NativeDisableParallelForRestriction] public BufferLookup<TileDrawer  > TileGroup;
		[NativeDisableParallelForRestriction] public BufferLookup<SpriteDrawer> SpriteGroup;
		[NativeDisableParallelForRestriction] public BufferLookup<ShadowDrawer> ShadowGroup;
		[NativeDisableParallelForRestriction] public BufferLookup<UIDrawer    > UIGroup;

		public void Execute(
			[ChunkIndexInQuery] int sortKey,
			Entity entity,
			EnabledRefRW<CreatureInitialize> initialize) {

			var blob = BlobGroup[entity];
			var core = CoreGroup[entity];
			var temp = TempGroup[entity];
			var collider = ColliderGroup[entity];
			var mass     = MassGroup    [entity];

			initialize.ValueRW = false;
			mass.InverseInertia = float3.zero;
			var target = Entity.Null;
			var body = core.Body;
			foreach (var prefab in PrefabContainer.Reinterpret<Entity>()) {
				if (BlobGroup.HasComponent(prefab)) {
					if (BlobGroup[prefab].Value.Value.InitialBody == body) {
						target = prefab;
						break;
					}
				}
			}
			if (temp.Head != core.Head) {
				var a = temp.Head.ToComponent();
				var b = core.Head.ToComponent();
				if (a != default) Buffer.RemoveComponent(sortKey, entity, a);
				if (b != default) Buffer.   AddComponent(sortKey, entity, b);
				temp.Head = core.Head;
			}
			if (temp.Body != core.Body) {
				var a = temp.Body.ToComponent();
				var b = core.Body.ToComponent();
				if (a != default) Buffer.RemoveComponent(sortKey, entity, a);
				if (b != default) Buffer.   AddComponent(sortKey, entity, b);
				temp.Body = core.Body;

				blob.Value       = BlobGroup[target].Value;
				core.MotionX     = CoreGroup[target].MotionX;
				core.MotionY     = CoreGroup[target].MotionY;
				core.MotionXTick = CoreGroup[target].MotionXTick;
				core.MotionYTick = CoreGroup[target].MotionYTick;
				collider.Value   = ColliderGroup[target].Value;
				mass.InverseMass = MassGroup[target].InverseMass;
				EffectGroup[entity].Clear();

				TileGroup  [entity].Clear();
				SpriteGroup[entity].Clear();
				ShadowGroup[entity].Clear();
				UIGroup    [entity].Clear();
				foreach (var element in TileGroup  [target]) TileGroup  [entity].Add(element);
				foreach (var element in SpriteGroup[target]) SpriteGroup[entity].Add(element);
				foreach (var element in ShadowGroup[target]) ShadowGroup[entity].Add(element);
				foreach (var element in UIGroup    [target]) UIGroup    [entity].Add(element);
			}
			if (temp.Flag != core.Flag) {
				for (int i = 0; i < 8; i++) {
					var flag = (Flag)i;
					var a = temp.HasFlag(flag);
					var b = core.HasFlag(flag);
					if (a == b) continue;
					temp.SetFlag(flag, b);

					switch (flag) {
						case Flag.Pinned:
							if (b) mass.InverseMass = 1f / CreaturePhysics.PinnedMass;
							else   mass.InverseMass = MassGroup[target].InverseMass;
							break;
						case Flag.Piercing:
							var filter = ColliderGroup[target].Value.Value.GetCollisionFilter();
							if (b) filter.CollidesWith &= ~(1u << (int)PhysicsCategory.Creature);
							else   filter.CollidesWith |=  (1u << (int)PhysicsCategory.Creature);
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
					var a = core.HasTeam(team);
					var b = core.HasTeam(team);
					if (a == b) continue;
					temp.SetTeam(team, b);
				}
			}
			TempGroup[entity] = temp;
			CoreGroup[entity] = core;
			BlobGroup[entity] = blob;
			ColliderGroup[entity] = collider;
			MassGroup    [entity] = mass;
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
			CoreGroup    = SystemAPI.GetComponentLookup<CreatureCore>(),
			TriggerGroup = SystemAPI.GetComponentLookup<EventTrigger>(true),
		}.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
	}

	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct CreatureBeginSimulationJob : IJobEntity {
		public void Execute(
			ref CreatureCore    core,
			ref PhysicsVelocity velocity) {

			if (!core.HasFlag(Flag.Floating)) core.GravityFactor++;
			if (core.IsKnocked())             core.KnockFactor--;
			velocity.Linear = new float3(0f, 0f, 0f);
		}
	}

	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct CreatureGravityRemovalJob : ITriggerEventsJob {
		public ComponentLookup<CreatureCore> CoreGroup;
		[ReadOnly] public ComponentLookup<EventTrigger> TriggerGroup;

		public void Execute(TriggerEvent triggerEvent) {
			Execute(triggerEvent.EntityA, triggerEvent.EntityB);
			Execute(triggerEvent.EntityB, triggerEvent.EntityA);
		}

		public void Execute(Entity entity, Entity target) {
			if (CoreGroup.HasComponent(entity) && !TriggerGroup.HasComponent(target)) {
				var core = CoreGroup[entity];
				core.GravityFactor = 0;
				CoreGroup[entity] = core;
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
			in  CreatureCore    core,
			ref PhysicsVelocity velocity) {

			if (!core.HasFlag(Flag.Floating)) {
				float multiplier = CreaturePhysics.GravityMultiplier;
				velocity.Linear += multiplier * core.GravityVector;
			}
			if (core.IsKnocked()) {
				float multiplier = CreaturePhysics.KnockMultiplier;
				velocity.Linear += multiplier * core.KnockVector;
			}
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSSimulationSystemGroup))]
partial struct CreatureSimulationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
		state.RequireForUpdate<SimulationSingleton>();
		state.RequireForUpdate<CameraManagerBridge>();
		state.RequireForUpdate<PrefabContainer>();
		state.RequireForUpdate<CreatureCore>();
	}

	public void OnUpdate(ref SystemState state) {
		var system = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
		state.Dependency = new CreatureLandingParticleJob {
			buffer          = system.CreateCommandBuffer(state.WorldUnmanaged),
			CameraManager   = SystemAPI.GetSingletonRW<CameraManagerBridge>(),
			PrefabContainer = SystemAPI.GetSingletonBuffer<PrefabContainer>(true),
			TransformGroup  = SystemAPI.GetComponentLookup<LocalToWorld>(true),
			BlobGroup       = SystemAPI.GetComponentLookup<CreatureBlob>(true),
			CoreGroup       = SystemAPI.GetComponentLookup<CreatureCore>(true),
			MassGroup       = SystemAPI.GetComponentLookup<PhysicsMass >(true),
			TriggerGroup    = SystemAPI.GetComponentLookup<EventTrigger>(true),
			Random          = new Random(1u + (uint)(4801 * SystemAPI.Time.ElapsedTime) % 1000),
		}.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
	}

	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct CreatureLandingParticleJob : ITriggerEventsJob {
		public EntityCommandBuffer buffer;
		[NativeDisableUnsafePtrRestriction] public RefRW<CameraManagerBridge> CameraManager;
		[ReadOnly] public DynamicBuffer<PrefabContainer> PrefabContainer;
		[ReadOnly] public ComponentLookup<LocalToWorld> TransformGroup;
		[ReadOnly] public ComponentLookup<CreatureBlob> BlobGroup;
		[ReadOnly] public ComponentLookup<CreatureCore> CoreGroup;
		[ReadOnly] public ComponentLookup<PhysicsMass > MassGroup;
		[ReadOnly] public ComponentLookup<EventTrigger> TriggerGroup;
		[ReadOnly] public Random Random;

		public void Execute(TriggerEvent triggerEvent) {
			Execute(triggerEvent.EntityA, triggerEvent.EntityB);
			Execute(triggerEvent.EntityB, triggerEvent.EntityA);
		}

		public void Execute(Entity entity, Entity target) {
			if (CoreGroup.HasComponent(entity) && !TriggerGroup.HasComponent(target)) {
				var entityCore = CoreGroup[entity];
				var gravity = entityCore.GravityFactor * CreaturePhysics.GravityMultiplier;
				var knock   = entityCore.KnockFactor   * CreaturePhysics.KnockMultiplier;
				var match = true;
				match &= gravity + knock < CreaturePhysics.DustThreshold;
				match &= !entityCore.HasFlag(Flag.Pinned);
				if (match && !CoreGroup.HasComponent(target)) {

					var position = TransformGroup[entity].Position;
					var radius = BlobGroup[entity].Value.Value.Radius;
					var right   = CameraManager.ValueRO.Right();
					var up      = CameraManager.ValueRO.Up();
					var forward = CameraManager.ValueRO.Forward();

					var smoke0 = buffer.Instantiate(PrefabContainer[(int)Prefab.SmokeTiny].Prefab);
					var smoke1 = buffer.Instantiate(PrefabContainer[(int)Prefab.SmokeTiny].Prefab);
					var position0 = position + right * radius - forward * radius;
					var position1 = position - right * radius - forward * radius;
					buffer.SetComponent(smoke0, LocalTransform.FromPosition(position0));
					buffer.SetComponent(smoke1, LocalTransform.FromPosition(position1));

				}
			}
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
				float maxHealth = math.max((int)status.Health, (int)data.MaxHealth);
				float maxShield = math.max((int)status.Shield, (int)data.MaxShield);
				float max = (maxHealth + maxShield) * ratio;

				float pureHealth = math.min((int)status.Health,  data.MaxHealth) * ratio;
				float overHealth = math.max(0,   status.Health - data.MaxHealth) * ratio;
				float pureShield = math.min((int)status.Shield,  data.MaxShield) * ratio;
				float overShield = math.max(0,   status.Shield - data.MaxShield) * ratio;
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

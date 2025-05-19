using UnityEngine;
using System;
using UnityRandom = UnityEngine.Random;

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

public enum Head : uint {
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

public enum Body : uint {
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

public static class ColorExtensions {
	public const byte Clear = 0;
	public const byte Black = 1;
	public const byte Gray  = 2;
	public const byte White = 3;

	public static color ToColor(bool x, byte y) => !x ? y switch {
		Black => new color(0x000000),
		Gray  => new color(0x505050),
		White => new color(0xFFFFFF),
		_ => color.clear,
	} : color.HSVtoRGB(y * 2.8125f, 0.59f, 0.85f);
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
				BeginHorizontal();
				PrefixLabel("Mask Color");
				I.MaskColorX = EditorGUILayout.Toggle(I.MaskColorX, GUILayout.Width(16f));
				I.MaskColorY = (byte)IntSlider(I.MaskColorY, 0, 127);
				var color = ColorExtensions.ToColor(I.MaskColorX, I.MaskColorY);
				EditorGUILayout.ColorField(color, GUILayout.Width(64f));
				EndHorizontal();
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

	[SerializeField] float m_Radius;
	[SerializeField] float m_Height;
	[SerializeField] ushort m_MaxEnergy;
	[SerializeField] ushort m_MaxShield;
	[SerializeField] ushort m_MaxHealth;
	[SerializeField] byte m_Tag;
	[SerializeField] uint m_Immunity = 0x55555555u;

	[SerializeField] string m_HeadString;
	[SerializeField] string m_BodyString;
	[SerializeField] uint m_Flag;
	[SerializeField] uint m_Team;
	[SerializeField] bool m_ColorX;
	[SerializeField] byte m_ColorY;



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
		set => m_MaxShield = (ushort)Mathf.Clamp(value, 0, ushort.MaxValue);
	}
	public ushort MaxHealth {
		get => m_MaxHealth;
		set => m_MaxHealth = (ushort)Mathf.Clamp(value, 0, ushort.MaxValue);
	}
	public ushort MaxEnergy {
		get => m_MaxEnergy;
		set => m_MaxEnergy = (ushort)Mathf.Clamp(value, 0, ushort.MaxValue);
	}

	public byte Tag {
		get => m_Tag;
		set => m_Tag = (byte)value;
	}
	public bool GetTag(Tag tag) => (Tag & (1u << (int)tag)) != 0u;
	public void SetTag(Tag tag, bool value) {
		if (value) Tag |= (byte) (1 << (int)tag);
		else       Tag &= (byte)~(1 << (int)tag);
	}
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
						if (TryGetComponent(out PhysicsBodyAuthoring body)) {
							if (b) body.Mass = CreatureCore.PinnedMass;
							else   body.Mass = CreatureCore.BaseMass;
						};
						break;
					case global::Flag.Piercing:
						foreach (var shape in GetComponents<PhysicsShapeAuthoring>()) {
							var collidesWith = shape.CollidesWith;
							if (b) collidesWith.Value &= ~(1u << (int)PhysicsCategory.Creature);
							else   collidesWith.Value |=  (1u << (int)PhysicsCategory.Creature);
							shape.CollidesWith = collidesWith;
						}
						break;
				}
			}
			m_Flag = value;
		}
	}
	public bool GetFlag(Flag flag) => (Flag & (1u << (int)flag)) != 0u;
	public void SetFlag(Flag flag, bool value) {
		Flag = value ? (Flag | (1u << (int)flag)) : (Flag & ~(1u << (int)flag));
	}
	public bool HasFlag   (Flag flag) => GetFlag(flag);
	public void AddFlag   (Flag flag) => SetFlag(flag, true);
	public void RemoveFlag(Flag flag) => SetFlag(flag, false);

	public uint Team {
		get => m_Team;
		set => m_Team = value;
	}
	public bool GetTeam(Team team) => (Team & (1u << (int)team)) != 0u;
	public void SetTeam(Team team, bool value) {
		Team = value ? (Team | (1u << (int)team)) : (Team & ~(1u << (int)team));
	}
	public bool HasTeam   (Team team) => GetTeam(team);
	public void AddTeam   (Team team) => SetTeam(team, true);
	public void RemoveTeam(Team team) => SetTeam(team, false);

	public bool MaskColorX {
		get => m_ColorX;
		set => m_ColorX = value;
	}
	public byte MaskColorY {
		get => m_ColorY;
		set => m_ColorY = value;
	}
	public color MaskColor => ColorExtensions.ToColor(MaskColorX, MaskColorY);



	public float Mass {
		get   =>  TryGetComponent(out PhysicsBodyAuthoring body) ? body.Mass : default;
		set { if (TryGetComponent(out PhysicsBodyAuthoring body))  body.Mass = value; }
	}



	// Baker

	public class Baker : Baker<CreatureAuthoring> {
		public override void Bake(CreatureAuthoring authoring) {
			Entity entity = GetEntity(TransformUsageFlags.Dynamic);
			AddComponent(entity, new CreatureInitialize());
			AddComponent(entity, new CreatureInput());
			using var blob = new BlobBuilder(Allocator.Temp);
			ref var asset = ref blob.ConstructRoot<CreatureBlobAsset>();

			asset.Name      = authoring.Name;
			asset.Radius    = authoring.Radius;
			asset.Height    = authoring.Height;
			asset.MaxShield = authoring.MaxShield;
			asset.MaxHealth = authoring.MaxHealth;
			asset.MaxEnergy = authoring.MaxEnergy;
			asset.Tag       = authoring.Tag;
			asset.Immunity  = authoring.Immunity;

			var value = blob.CreateBlobAssetReference<CreatureBlobAsset>(Allocator.Persistent);
			AddComponent(entity, new CreatureData {

				Value = value,

			});
			AddComponent(entity, new CreatureCore {

				Head       = authoring.Head,
				Body       = authoring.Body,
				Flag       = authoring.Flag,
				Team       = authoring.Team,
				MaskColorX = authoring.MaskColorX,
				MaskColorY = authoring.MaskColorY,

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

			bool hasTileDrawer   = authoring.TryGetComponent(out TileDrawerAuthoring   tileDrawer  );
			bool hasSpriteDrawer = authoring.TryGetComponent(out SpriteDrawerAuthoring spriteDrawer);
			bool hasShadowDrawer = authoring.TryGetComponent(out ShadowDrawerAuthoring shadowDrawer);
			bool hasUIDrawer     = authoring.TryGetComponent(out UIDrawerAuthoring     uiDrawer    );
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

	public ushort Key;
	public uint Data;

	public float MoveFactor {
		get => CreatureInputExtensions.GetMoveFactor(this);
		set => CreatureInputExtensions.SetMoveFactor(ref this, value);
	}
	public float3 MoveVector {
		get => CreatureInputExtensions.GetMoveVector(this);
		set => CreatureInputExtensions.SetMoveVector(ref this, value);
	}
}



public static class CreatureInputExtensions {

	public static bool GetKey(this in CreatureInput input, KeyAction key) {
		return (input.Key & (1 << (int)key)) != 0;
	}
	public static void SetKey(this ref CreatureInput input, KeyAction key, bool value) {
		if (value) input.Key |= (ushort) (1u << (int)key);
		else       input.Key &= (ushort)~(1u << (int)key);
	}

	// Constants

	const uint MoveFMask   = 0x0000C000u;
	const uint MoveDMask   = 0x00003F00u;

	const int MoveFShift   = 14;
	const int MoveDShift   =  8;



	// Methods

	public static float GetMoveFactor(this in CreatureInput input) {
		return ((input.Data & MoveFMask) >> MoveFShift) * 0.333333f;
	}
	public static void SetMoveFactor(this ref CreatureInput input, float value) {
		uint moveFactor = (uint)math.round(math.saturate(value) * 3f);
		input.Data = (input.Data & ~MoveFMask) | (moveFactor << MoveFShift);
	}

	public static float3 GetMoveVector(this in CreatureInput input) {
		if (input.MoveFactor == 0f) return float3.zero;
		else {
			float yawRadians = ((input.Data & MoveDMask) >> MoveDShift) * 5.625f * math.TORADIANS;
			return input.MoveFactor * new float3(math.sin(yawRadians), 0f, math.cos(yawRadians));
		}
	}
	public static void SetMoveVector(this ref CreatureInput input, float3 value) {
		input.MoveFactor = math.length(value);
		if (0f < input.MoveFactor) {
			float yaw = (math.atan2(value.x, value.z) * math.TODEGREES + 360f + 2.8125f) % 360f;
			input.Data = (input.Data & ~MoveDMask) | ((uint)(yaw * 0.177777f) << MoveDShift);
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Data
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CreatureData : IComponentData {

	public BlobAssetReference<CreatureBlobAsset> Value;
}



public struct CreatureBlobAsset {

	public FixedString512Bytes Name;
	public float Radius;
	public float Height;
	public ushort MaxShield;
	public ushort MaxHealth;
	public ushort MaxEnergy;
	public byte Tag;
	public uint Immunity;
}



public static class CreatureBlobAssetExtensions {

	public static bool GetTag
		(this in CreatureBlobAsset data, Tag tag) {
		return (data.Tag & (1 << (int)tag)) != 0;
	}
	public static void SetTag
		(this ref CreatureBlobAsset data, Tag tag, bool value) {
		if (value) data.Tag |= (byte) (1 << (int)tag);
		else       data.Tag &= (byte)~(1 << (int)tag);
	}
	public static bool HasTag   (this in  CreatureBlobAsset data, Tag tag) => data.GetTag(tag);
	public static void AddTag   (this ref CreatureBlobAsset data, Tag tag) => data.SetTag(tag, true);
	public static void RemoveTag(this ref CreatureBlobAsset data, Tag tag) => data.SetTag(tag, false);

	public static Immunity GetImmunity
		(this in CreatureBlobAsset data, Effect effect) {
		return (Immunity)((data.Immunity >> ((int)effect * 2)) & 0b11u);
	}
	public static void SetImmunity
		(this ref CreatureBlobAsset data, Effect effect, Immunity immunity) {
		data.Immunity &= ~(0b11u << ((int)effect * 2));
		data.Immunity |= (uint)immunity << ((int)effect * 2);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Core
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[GhostComponent]
public struct CreatureCore : IComponentData {

	// Constants

	public const float BaseMass   =    1.00f;
	public const float PinnedMass = 1000.00f;
	public const float GravityMultiplier =  -9.81f * NetworkManager.Ticktime;
	public const float KnockMultiplier   = 256.00f * NetworkManager.Ticktime;



	const uint HeadMask        = 0xFC000000u;
	const uint BodyMask        = 0x03FF0000u;
	const uint FlagMask        = 0x0000FC00u;
	const uint TeamMask        = 0x00000300u;
	const uint ColorXMask      = 0x00000080u;
	const uint ColorYMask      = 0x0000007Fu;

	const uint MotionXMask     = 0xF8000000u;
	const uint MotionYMask     = 0x07C00000u;
	const uint MotionXTickMask = 0x003FF800u;
	const uint MotionYTickMask = 0x000007FFu;
	const uint GravityFMask    = 0xFF000000u;
	const uint KnockFMask      = 0x00FC0000u;
	const uint KnockXMask      = 0x0003F000u;
	const uint KnockYMask      = 0x00000FC0u;
	const uint KnockZMask      = 0x0000003Fu;

	const int HeadShift        = 26;
	const int BodyShift        = 16;
	const int FlagShift        = 10;
	const int TeamShift        =  8;
	const int ColorXShift      =  7;
	const int ColorYShift      =  0;

	const int MotionXShift     = 27;
	const int MotionYShift     = 22;
	const int TickXShift       = 11;
	const int TickYShift       =  0;
	const int GravityFShift    = 24;
	const int KnockFShift      = 18;
	const int KnockXShift      = 12;
	const int KnockYShift      =  6;
	const int KnockZShift      =  0;



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
	public uint Team {
		get => (Data0 & TeamMask) >> TeamShift;
		set => Data0 = (Data0 & ~TeamMask) | (value << TeamShift);
	}

	public bool MaskColorX {
		get => (Data0 & ColorXMask) != 0u;
		set => Data0 = (Data0 & ~ColorXMask) | (value ? ColorXMask : 0u);
	}
	public byte MaskColorY {
		get => (byte)(Data0 & ColorYMask);
		set => Data0 = (Data0 & ~ColorYMask) | ((uint)(value % 127) << ColorYShift);
	}
	public color MaskColor => ColorExtensions.ToColor(MaskColorX, MaskColorY);



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
		get => (int)((Data1 & MotionXTickMask) >> TickXShift);
		set {
			uint motionXTick = (uint)math.max(0, value % 2048);
			Data1 = (Data1 & ~MotionXTickMask) | (motionXTick << TickXShift);
		}
	}
	public int MotionYTick {
		get => (int)((Data1 & MotionYTickMask) >> TickYShift);
		set {
			uint motionYTick = (uint)math.max(0, value % 2048);
			Data1 = (Data1 & ~MotionYTickMask) | (motionYTick << TickYShift);
		}
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
		get => (int)((Data2 & GravityFMask) >> GravityFShift);
		set {
			uint gravityFactor = (uint)math.clamp(value, 0, 255);
			Data2 = (Data2 & ~GravityFMask) | (gravityFactor << GravityFShift);
		}
	}
	[CreateProperty] public float3 GravityVector {
		get => new(0f, (GravityFactor == 0) ? 0f : 10f + GravityFactor, 0f);
	}
	public bool IsGrounded => (Data2 & GravityFMask) == 0u;

	[CreateProperty] public int KnockFactor {
		get => (int)((Data2 & KnockFMask) >> KnockFShift);
		set {
			uint knockFactor = (uint)math.clamp(value, 0, 63);
			Data2 = (Data2 & ~KnockFMask) | (knockFactor << KnockFShift);
		}
	}
	[CreateProperty] public float3 KnockVector {
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



public static class CreatureCoreExtensions {

	public static bool GetFlag
		(this in CreatureCore core, Flag flag) {
		return (core.Flag & (1u << (int)flag)) != 0u;
	}
	public static void SetFlag
		(this ref CreatureCore core, Flag flag, bool value) {
		if (value) core.Flag |= (uint) (1u << (int)flag);
		else       core.Flag &= (uint)~(1u << (int)flag);
	}
	public static bool HasFlag   (this in  CreatureCore core, Flag flag) => core.GetFlag(flag);
	public static void AddFlag   (this ref CreatureCore core, Flag flag) => core.SetFlag(flag, true);
	public static void RemoveFlag(this ref CreatureCore core, Flag flag) => core.SetFlag(flag, false);

	public static bool GetTeam
		(this in CreatureCore core, Team team) {
		return (core.Team & (1u << (int)team)) != 0u;
	}
	public static void SetTeam
		(this ref CreatureCore core, Team team, bool value) {
		if (value) core.Team |= (uint) (1u << (int)team);
		else       core.Team &= (uint)~(1u << (int)team);
	}
	public static bool HasTeam   (this in  CreatureCore core, Team team) => core.GetTeam(team);
	public static void AddTeam   (this ref CreatureCore core, Team team) => core.SetTeam(team, true);
	public static void RemoveTeam(this ref CreatureCore core, Team team) => core.SetTeam(team, false);
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Temp
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CreatureTemp : IComponentData {

	// Constants

	const uint HeadMask     = 0xFC000000u;
	const uint BodyMask     = 0x03FF0000u;
	const uint FlagMask     = 0x0000FC00u;
	const uint TeamMask     = 0x00000300u;
	const uint GravityFMask = 0x000000FFu;
	const uint BarTickMask  = 0xFF000000u;

	const int HeadShift     = 26;
	const int BodyShift     = 16;
	const int FlagShift     = 10;
	const int TeamShift     =  8;
	const int GravityFShift =  0;
	const int BarTickShift  = 24;



	// Fields

	public uint Data0;



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
}



public static class CreatureTempExtensions {

	public static bool GetFlag
		(this in CreatureTemp temp, Flag flag) {
		return (temp.Flag & (1u << (int)flag)) != 0u;
	}
	public static void SetFlag
		(this ref CreatureTemp temp, Flag flag, bool value) {
		if (value) temp.Flag |=  (uint)(1u << (int)flag);
		else       temp.Flag &= ~(uint)(1u << (int)flag);
	}
	public static bool HasFlag   (this in  CreatureTemp temp, Flag flag) => temp.GetFlag(flag);
	public static void AddFlag   (this ref CreatureTemp temp, Flag flag) => temp.SetFlag(flag, true);
	public static void RemoveFlag(this ref CreatureTemp temp, Flag flag) => temp.SetFlag(flag, false);

	public static bool GetTeam
		(this in CreatureTemp temp, Team team) {
		return (temp.Team & (1u << (int)team)) != 0u;
	}
	public static void SetTeam
		(this ref CreatureTemp temp, Team team, bool value) {
		if (value) temp.Team |=  (uint)(1u << (int)team);
		else       temp.Team &= ~(uint)(1u << (int)team);
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

	[GhostField] public Effect Effect;
	[GhostField] public ushort Strength;
	[GhostField] public ushort Duration;
}



public static class CreatureEffectBufferExtensions {

	public static bool IsValueType(this Effect effect) => effect switch {
		Effect.Damage      => true,
		Effect.DamageBoost => true,
		Effect.HealthBoost => true,
		Effect.EnergyBoost => true,
		_ => false,
	};

	public static bool TryGetIndex
		(this DynamicBuffer<CreatureEffect> buffer, Effect effect, out int index) {
		for (int i = 0; i < buffer.Length; i++) if (buffer[i].Effect == effect) {
			index = i;
			return true;
		}
		index = -1;
		return false;
	}

	public static void AddEffect
		(this DynamicBuffer<CreatureEffect> buffer, in CreatureData data, Effect effect,
		float strength, float duration, float maxStrength = 0f, float maxDuration = 0f) {
		var multiplier = math.max(0f, 1f - data.Value.Value.GetImmunity(effect).ToValue());
		if (multiplier == 0f) return;

		var isValue = effect.IsValueType();
		var element = new CreatureEffect() {
			Effect = effect,
			Strength = (ushort)(strength * (isValue ? 1f : 1000f) * multiplier),
			Duration = (ushort)(duration * (isValue ? 1f : multiplier)),
		};
		if (buffer.TryGetIndex(effect, out int index)) {
			var previous = buffer[index];
			if (maxStrength == 0f) maxStrength = math.max((int)previous.Strength, element.Strength);
			if (maxDuration == 0f) maxDuration = math.max((int)previous.Duration, element.Duration);
			element.Strength = (ushort)math.min(previous.Strength + element.Strength, (int)maxStrength);
			element.Duration = (ushort)math.min(previous.Duration + element.Duration, (int)maxDuration);
			buffer.ElementAt(index) = element;
		} else buffer.Add(element);
	}

	public static void AddDamage
		(this DynamicBuffer<CreatureEffect> buffer, in CreatureData data, int value) {
		AddEffect(buffer, in data, Effect.Damage, value, 0.2f, float.MaxValue, float.MaxValue);
	}

	public static void RemoveEffect
		(this DynamicBuffer<CreatureEffect> buffer, Effect effect) {
		if (TryGetIndex(buffer, effect, out int index)) buffer.RemoveAt(index);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Initialization System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSInitializationSystemGroup))]
partial struct CreatureInitializationSystem : ISystem {

	[BurstCompile]
	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
		state.RequireForUpdate<SimulationSingleton>();
		state.RequireForUpdate<PrefabContainer>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state) {
		var singleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
		state.Dependency = new CreatureFieldsComparisonJob {
		}.ScheduleParallel(state.Dependency);
		state.Dependency = new CreatureComponentsModificationJob {
			entityManager   = state.EntityManager,
			buffer          = singleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
			prefabContainer = SystemAPI.GetSingletonBuffer<PrefabContainer>(true),
			configs         = SystemAPI.GetComponentLookup<CreatureData >(true),
			cores           = SystemAPI.GetComponentLookup<CreatureCore   >(true),
			temps           = SystemAPI.GetComponentLookup<CreatureTemp   >(true),
			statuses        = SystemAPI.GetComponentLookup<CreatureStatus >(true),
			effects         = SystemAPI.GetBufferLookup   <CreatureEffect >(true),
			colliders       = SystemAPI.GetComponentLookup<PhysicsCollider>(true),
			masses          = SystemAPI.GetComponentLookup<PhysicsMass    >(true),
			tiles           = SystemAPI.GetBufferLookup<TileDrawer  >(true),
			sprites         = SystemAPI.GetBufferLookup<SpriteDrawer>(true),
			shadows         = SystemAPI.GetBufferLookup<ShadowDrawer>(true),
			uis             = SystemAPI.GetBufferLookup<UIDrawer    >(true),
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
			if (temp.Team != core.Team) initialize.ValueRW = true;
			if (temp.Flag != core.Flag) initialize.ValueRW = true;
		}
	}

	[BurstCompile]
	partial struct CreatureComponentsModificationJob : IJobEntity {
		[NativeDisableContainerSafetyRestriction] public EntityManager entityManager;
		public EntityCommandBuffer.ParallelWriter buffer;
		[ReadOnly] public DynamicBuffer<PrefabContainer> prefabContainer;
		[NativeDisableContainerSafetyRestriction] public ComponentLookup<CreatureData> configs;
		[NativeDisableContainerSafetyRestriction] public ComponentLookup<CreatureCore> cores;
		[NativeDisableContainerSafetyRestriction] public ComponentLookup<CreatureTemp> temps;
		[NativeDisableContainerSafetyRestriction] public ComponentLookup<CreatureStatus> statuses;
		[NativeDisableContainerSafetyRestriction] public BufferLookup<CreatureEffect> effects;
		[NativeDisableContainerSafetyRestriction] public ComponentLookup<PhysicsCollider> colliders;
		[NativeDisableContainerSafetyRestriction] public ComponentLookup<PhysicsMass> masses;
		[NativeDisableContainerSafetyRestriction] public BufferLookup<TileDrawer> tiles;
		[NativeDisableContainerSafetyRestriction] public BufferLookup<SpriteDrawer> sprites;
		[NativeDisableContainerSafetyRestriction] public BufferLookup<ShadowDrawer> shadows;
		[NativeDisableContainerSafetyRestriction] public BufferLookup<UIDrawer> uis;

		public void Execute(
			[ChunkIndexInQuery] int sortKey,
			Entity entity,
			ref CreatureData config,
			ref CreatureCore core,
			ref CreatureTemp temp,
			ref CreatureStatus status,
			DynamicBuffer<CreatureEffect> effect,
			ref PhysicsMass mass,
			ref PhysicsCollider collider,
			DynamicBuffer<TileDrawer> tile,
			DynamicBuffer<SpriteDrawer> sprite,
			DynamicBuffer<ShadowDrawer> shadow,
			DynamicBuffer<UIDrawer> ui,
			EnabledRefRW<CreatureInitialize> initialize) {
			mass.InverseInertia = float3.zero;
			initialize.ValueRW = false;

			if (temp.Head != core.Head) {
				var a = temp.Head.ToComponent();
				var b = core.Head.ToComponent();
				var aMatch = (a != default) &&  entityManager.HasComponent(entity, a);
				var bMatch = (b != default) && !entityManager.HasComponent(entity, b);
				if (aMatch) buffer.RemoveComponent(sortKey, entity, a);
				if (bMatch) buffer.   AddComponent(sortKey, entity, b);
				temp.Head = core.Head;
			}
			if (temp.Body != core.Body) {
				var a = temp.Body.ToComponent();
				var b = core.Body.ToComponent();
				var aMatch = (a != default) &&  entityManager.HasComponent(entity, a);
				var bMatch = (b != default) && !entityManager.HasComponent(entity, b);
				if (aMatch) buffer.RemoveComponent(sortKey, entity, a);
				if (bMatch) buffer.   AddComponent(sortKey, entity, b);
				temp.Body = core.Body;

				var prefab = Entity.Null;
				foreach (var element in prefabContainer.Reinterpret<Entity>()) {
					if (cores.HasComponent(element) && cores[element].Body == core.Body) {
						prefab = element;
						break;
					}
				}
				config.Value = configs[prefab].Value;
				core.MotionX     = cores[prefab].MotionX;
				core.MotionY     = cores[prefab].MotionY;
				core.MotionXTick = cores[prefab].MotionXTick;
				core.MotionYTick = cores[prefab].MotionYTick;
				mass.InverseMass = masses[prefab].InverseMass;
				collider.Value   = colliders[prefab].Value;
				effect.Clear();
				tile  .Length = tiles  [prefab].Length;
				sprite.Length = sprites[prefab].Length;
				shadow.Length = shadows[prefab].Length;
				ui    .Length = uis    [prefab].Length;
				for (int i = 0; i < tile  .Length; i++) tile  [i] = tiles  [prefab][i];
				for (int i = 0; i < sprite.Length; i++) sprite[i] = sprites[prefab][i];
				for (int i = 0; i < shadow.Length; i++) shadow[i] = shadows[prefab][i];
				for (int i = 0; i < ui    .Length; i++) ui    [i] = uis    [prefab][i];
			}
			if (temp.Flag != core.Flag) {
				for (int i = 0; i < 8; i++) {
					var flag = (Flag)i;
					var a = temp.HasFlag(flag);
					var b = core.HasFlag(flag);
					if (a == b) continue;
					temp.SetFlag(flag, b);

					var prefab = Entity.Null;
					foreach (var element in prefabContainer.Reinterpret<Entity>()) {
						if (cores.HasComponent(element) && cores[element].Body == core.Body) {
							prefab = element;
							break;
						}
					}
					switch (flag) {
						case Flag.Pinned:
							if (b) mass.InverseMass = 1f / CreatureCore.PinnedMass;
							else   mass.InverseMass = masses[prefab].InverseMass;
							break;
						case Flag.Piercing:
							var filter = colliders[prefab].Value.Value.GetCollisionFilter();
							if (b) filter.CollidesWith &= ~(1u << (int)PhysicsCategory.Creature);
							else   filter.CollidesWith |=  (1u << (int)PhysicsCategory.Creature);
							if (!collider.Value.Value.GetCollisionFilter().Equals(filter)) {
								if (!collider.IsUnique) collider.MakeUnique(entity, buffer, sortKey);
								collider.Value.Value.SetCollisionFilter(filter);
							}
							break;
						case Flag.Interactable:
							buffer.SetComponentEnabled<Interactable>(sortKey, entity, b);
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
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Begin Predicted Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSPredictedSimulationSystemGroup), OrderFirst = true)]
partial struct CreatureBeginPredictedSimulationSystem : ISystem {

	[BurstCompile]
	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<SimulationSingleton>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state) {
		state.Dependency = new CreatureBeginSimulationJob() {
		}.ScheduleParallel(state.Dependency);
		state.Dependency = new CreatureGravityRemovalJob {
			cores    = SystemAPI.GetComponentLookup<CreatureCore>(),
			triggers = SystemAPI.GetComponentLookup<EventTrigger>(true),
		}.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
	}

	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct CreatureBeginSimulationJob : IJobEntity {
		public void Execute(ref CreatureCore core, ref PhysicsVelocity velocity) {

			if (!core.HasFlag(Flag.Floating)) {
				core.GravityFactor++;
			}
			if (core.IsKnocked) {
				core.KnockFactor--;
			}
			velocity.Linear = new float3(0f, 0f, 0f);
		}
	}

	[BurstCompile]
	partial struct CreatureGravityRemovalJob : ITriggerEventsJob {
		public ComponentLookup<CreatureCore> cores;
		[ReadOnly] public ComponentLookup<EventTrigger> triggers;

		public void Execute(TriggerEvent triggerEvent) {
			Execute(triggerEvent.EntityA, triggerEvent.EntityB);
			Execute(triggerEvent.EntityB, triggerEvent.EntityA);
		}
		public void Execute(Entity entity, Entity target) {
			if (cores.HasComponent(entity) && !triggers.HasComponent(target)) {
				var core = cores[entity];
				core.GravityFactor = 0;
				cores[entity] = core;
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

	[BurstCompile]
	public void OnUpdate(ref SystemState state) {
		state.Dependency = new EndCreatureSimulationJob {
		}.ScheduleParallel(state.Dependency);
	}

	[BurstCompile]
	partial struct EndCreatureSimulationJob : IJobEntity {
		public void Execute(in CreatureCore core, ref PhysicsVelocity velocity) {

			if (!core.HasFlag(Flag.Floating)) {
				float multiplier = CreatureCore.GravityMultiplier;
				velocity.Linear += multiplier * core.GravityVector;
			}
			if (core.IsKnocked) {
				float multiplier = CreatureCore.KnockMultiplier;
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

	[BurstCompile]
	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
		state.RequireForUpdate<SimulationSingleton>();
		state.RequireForUpdate<CameraManagerBridge>();
		state.RequireForUpdate<PrefabContainer>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state) {
		var system = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
		state.Dependency = new CreatureLandingParticleJob {
			buffer        = system.CreateCommandBuffer(state.WorldUnmanaged),
			cameraManager = SystemAPI.GetSingletonRW<CameraManagerBridge>(),
			prefabs       = SystemAPI.GetSingletonBuffer<PrefabContainer>(true),
			transforms    = SystemAPI.GetComponentLookup<LocalTransform>(true),
			bases         = SystemAPI.GetComponentLookup<CreatureData>(true),
			cores         = SystemAPI.GetComponentLookup<CreatureCore>(true),
			masses        = SystemAPI.GetComponentLookup<PhysicsMass>(true),
			triggers      = SystemAPI.GetComponentLookup<EventTrigger>(true),
			random        = new Random((uint)UnityRandom.Range(1, 1000)),
		}.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
	}

	[BurstCompile]
	partial struct CreatureLandingParticleJob : ITriggerEventsJob {
		public const float DustThreshold = -0.00001f;
		public EntityCommandBuffer buffer;
		[NativeDisableUnsafePtrRestriction] public RefRW<CameraManagerBridge> cameraManager;
		[ReadOnly] public DynamicBuffer<PrefabContainer> prefabs;
		[ReadOnly] public ComponentLookup<LocalTransform> transforms;
		[ReadOnly] public ComponentLookup<CreatureData> bases;
		[ReadOnly] public ComponentLookup<CreatureCore> cores;
		[ReadOnly] public ComponentLookup<PhysicsMass> masses;
		[ReadOnly] public ComponentLookup<EventTrigger> triggers;
		[ReadOnly] public Random random;

		public void Execute(TriggerEvent triggerEvent) {
			Execute(triggerEvent.EntityA, triggerEvent.EntityB);
			Execute(triggerEvent.EntityB, triggerEvent.EntityA);
		}
		public void Execute(Entity entity, Entity target) {
			if (cores.HasComponent(entity) && !triggers.HasComponent(target)) {
				var entityCore = cores[entity];
				var gravity = entityCore.GravityFactor * CreatureCore.GravityMultiplier;
				var knock   = entityCore.KnockFactor   * CreatureCore.KnockMultiplier;
				var match = true;
				match &= gravity + knock < DustThreshold;
				match &= !entityCore.HasFlag(Flag.Pinned);
				if (match && !cores.HasComponent(target)) {

					var position = transforms[entity].Position;
					var radius = bases[entity].Value.Value.Radius;
					var right   = cameraManager.ValueRO.Right();
					var up      = cameraManager.ValueRO.Up();
					var forward = cameraManager.ValueRO.Forward();

					var smoke0 = buffer.Instantiate(prefabs[(int)Prefab.SmokeTiny].Prefab);
					var smoke1 = buffer.Instantiate(prefabs[(int)Prefab.SmokeTiny].Prefab);
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
/*
[BurstCompile]
[UpdateInGroup(typeof(DOTSPresentationSystemGroup))]
partial struct CreaturePresentationSystem : ISystem {

	[BurstCompile]
	public void OnUpdate(ref SystemState state) {
		state.Dependency = new CreaturePresentationJob {
		}.ScheduleParallel(state.Dependency);
	}

	[BurstCompile]
	partial struct CreaturePresentationJob : IJobEntity {
		public void Execute(ref CreatureCore core, DynamicBuffer<UIDrawer> ui) {
			//if (core.BarTick == 0) {
			//	if (!drawer.UIs.IsEmpty) drawer.UIs.Clear();
			//} else {
				if (ui.Length < 7) {
					while (ui.Length < 7) ui.Add(new UIDrawer {
						Position  = new float3(0f, core.Height + 1f, 0f),
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
				float ratio = core.Radius * 2f / (core.MaxHealth + core.MaxShield);
				float maxHealth = math.max(core.Health, core.MaxHealth);
				float maxShield = math.max(core.Shield, core.MaxShield);
				float max = (maxHealth + maxShield) * ratio;

				float pureHealth = core.PureHealth * ratio;
				float overHealth = core.OverHealth * ratio;
				float pureShield = core.PureShield * ratio;
				float overShield = core.OverShield * ratio;
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
*/
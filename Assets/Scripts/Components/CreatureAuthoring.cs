using UnityEngine;
using System;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Extensions;
using Unity.NetCode;
using Unity.Properties;

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
	InstantDamage,
	InstantHeal,
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
public struct PhysicsExtensions {
	public const float DefaultMass       = 1.0000000f;
	public const float PinnedMass        = 1000000.0f;
	public const float GravityMultiplier =  -9.81f;
	public const float KnockMultiplier   = 256.00f;
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
				I.Tag       = FlagField<Tag>("Tag", I.Tag);
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

	[SerializeField] float  m_Radius;
	[SerializeField] float  m_Height;
	[SerializeField] ushort m_MaxEnergy;
	[SerializeField] ushort m_MaxShield;
	[SerializeField] ushort m_MaxHealth;
	[SerializeField] uint   m_Tag;
	[SerializeField] uint   m_Immunity = 0x55555555u;

	[SerializeField] string m_HeadString;
	[SerializeField] string m_BodyString;
	[SerializeField] uint   m_Flag;
	[SerializeField] uint   m_Team;
	[SerializeField] bool   m_ColorX;
	[SerializeField] byte   m_ColorY;



	// Properties

	public float Radius {
		get => m_Radius;
		set => m_Radius = Mathf.Clamp(Mathf.Round(value * 10f) * 0.1f, 0.1f, 12.8f);
	}
	public float Height {
		get => m_Height;
		set => m_Height = Mathf.Clamp(Mathf.Round(value * 10f) * 0.1f, 0.1f, 25.6f);
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

	public uint Tag {
		get => m_Tag;
		set => m_Tag = value;
	}
	public bool GetTag(Tag tag) => (Tag & (1u << (int)tag)) != 0u;
	public void SetTag(Tag tag, bool value) {
		Tag = value ? (Tag | (1u << (int)tag)) : (Tag & ~(1u << (int)tag));
	}
	public bool HasTag   (Tag tag) => GetTag(tag);
	public void AddTag   (Tag tag) => SetTag(tag, true );
	public void RemoveTag(Tag tag) => SetTag(tag, false);

	public uint Immunity {
		get => m_Immunity;
		set => m_Immunity = value;
	}
	public Immunity GetImmunity(Effect effect) {
		return (Immunity)((Immunity >> ((int)effect * 2)) & 0b11u);
	}
	public void SetImmunity(Effect effect, Immunity immunity) {
		Immunity = (Immunity & ~(0b11u << ((int)effect * 2))) | ((uint)immunity << ((int)effect * 2));
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
		get => Enum.TryParse(HeadString, out Head head) ? head : 0;
		set => m_HeadString = value.ToString();
	}
	public Body Body {
		get => Enum.TryParse(BodyString, out Body body) ? body : 0;
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
							if (b) body.Mass = PhysicsExtensions. PinnedMass;
							else   body.Mass = PhysicsExtensions.DefaultMass;
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
	public void AddFlag   (Flag flag) => SetFlag(flag, true );
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
	public void AddTeam   (Team team) => SetTeam(team, true );
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
			Entity entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new CreatureInitialize());
			AddComponent(entity, new CreatureInput());
			AddComponent(entity, new CreatureCore {

				Radius     = authoring.Radius,
				Height     = authoring.Height,
				MaxShield  = authoring.MaxShield,
				MaxHealth  = authoring.MaxHealth,
				MaxEnergy  = authoring.MaxEnergy,
				Tag        = authoring.Tag,
				Immunity   = authoring.Immunity,
				TempTeam   = ~authoring.Team,
				TempFlag   = ~authoring.Flag,

				Head       = authoring.Head,
				Body       = authoring.Body,
				Flag       = authoring.Flag,
				Team       = authoring.Team,
				MaskColorX = authoring.MaskColorX,
				MaskColorY = authoring.MaskColorY,

				Shield     = authoring.MaxShield,
				Health     = authoring.MaxHealth,
				Energy     = authoring.MaxEnergy,

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

	public uint data;



	// Properties

	[CreateProperty] public uint Key {
		get => (data & KeyMask) >> KeyShift;
		set => data = (data & ~KeyMask) | (value << KeyShift);
	}
	public bool GetKey(KeyAction key) => (Key & (1u << (int)key)) != 0u;
	public void SetKey(KeyAction key, bool value) {
		Key = value ? (Key | (1u << (int)key)) : (Key & ~(1u << (int)key));
	}

	public float MoveFactor {
		get => ((data & MoveFMask) >> MoveFShift) * 0.333333f;
		set {
			uint moveFactor = (uint)math.round(math.saturate(value) * 3f);
			data = (data & ~MoveFMask) | (moveFactor << MoveFShift);
		}
	}
	[CreateProperty] public float3 MoveVector {
		get {
			if (MoveFactor == 0f) return float3.zero;
			else {
				float yawRadians = ((data & MoveDMask) >> MoveDShift) * 5.625f * math.TORADIANS;
				return MoveFactor * new float3(math.sin(yawRadians), 0f, math.cos(yawRadians));
			}
		}
		set {
			MoveFactor = math.length(value);
			if (0f < MoveFactor) {
				float yaw = (math.atan2(value.x, value.z) * math.TODEGREES + 360f + 2.8125f) % 360f;
				data = (data & ~MoveDMask) | ((uint)(yaw * 0.177777f) << MoveDShift);
			}
		}
	}

	[CreateProperty] public Ping Ping {
		get => (Ping)((data & PingMask) >> PingShift);
		set => data = (data & ~PingMask) | ((uint)value << PingShift);
	}
	[CreateProperty] public Emotion Emotion {
		get => (Emotion)((data & EmotionMask) >> EmotionShift);
		set => data = (data & ~EmotionMask) | ((uint)value << EmotionShift);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Core
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CreatureCore : IComponentData {

	// Constants

	const uint RadiusMask      = 0x7F000000u;
	const uint HeightMask      = 0x00FF0000u;
	const uint MaxShieldMask   = 0xFFFF0000u;
	const uint MaxHealthMask   = 0x0000FFFFu;
	const uint MaxEnergyMask   = 0xFFFF0000u;
	const uint TagMask         = 0x0000FF00u;
	const uint ImmunityMask    = 0xFFFFFFFFu;
	const uint TempHeadMask    = 0xFC000000u;
	const uint TempBodyMask    = 0x03FF0000u;
	const uint TempFlagMask    = 0x0000FC00u;
	const uint TempTeamMask    = 0x00000300u;
	const uint BarTickMask     = 0x000000FFu;

	const uint HeadMask        = 0xFC000000u;
	const uint BodyMask        = 0x03FF0000u;
	const uint FlagMask        = 0x0000FC00u;
	const uint TeamMask        = 0x00000300u;
	const uint ColorXMask      = 0x00000080u;
	const uint ColorYMask      = 0x0000007Fu;
	const uint ShieldMask      = 0xFFFF0000u;
	const uint HealthMask      = 0x0000FFFFu;
	const uint EnergyMask      = 0xFFFF0000u;
	const uint ShieldTickMask  = 0x0000FF00u;
	const uint EnergyTickMask  = 0x000000FFu;
	const uint MotionXMask     = 0xF8000000u;
	const uint MotionYMask     = 0x07C00000u;
	const uint MotionXTickMask = 0x003FF800u;
	const uint MotionYTickMask = 0x000007FFu;
	const uint GravityFMask    = 0xFF000000u;
	const uint KnockFMask      = 0x00FC0000u;
	const uint KnockXMask      = 0x0003F000u;
	const uint KnockYMask      = 0x00000FC0u;
	const uint KnockZMask      = 0x0000003Fu;

	const int RadiusShift      = 24;
	const int HeightShift      = 16;
	const int MaxShieldShift   = 16;
	const int MaxHealthShift   =  0;
	const int MaxEnergyShift   = 16;
	const int TagShift         =  8;
	const int ImmunityShift    =  0;
	const int TempHeadShift    = 26;
	const int TempBodyShift    = 16;
	const int TempFlagShift    = 10;
	const int TempTeamShift    =  8;
	const int BarTickShift     =  0;

	const int HeadShift        = 26;
	const int BodyShift        = 16;
	const int FlagShift        = 10;
	const int TeamShift        =  8;
	const int ColorXShift      =  7;
	const int ColorYShift      =  0;
	const int ShieldShift      = 16;
	const int HealthShift      =  0;
	const int EnergyShift      = 16;
	const int ShieldTickShift  =  8;
	const int EnergyTickShift  =  0;
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

	public uint data0;
	public uint data1;
	public uint data2;
	public uint data3;
	public uint data4;

	[GhostField] public uint data5;
	[GhostField] public uint data6;
	[GhostField] public uint data7;
	[GhostField] public uint data8;
	[GhostField] public uint data9;



	// Local Properties

	[CreateProperty] public float Radius {
		get => ((data0 & RadiusMask) >> RadiusShift) * 0.1f + 0.1f;
		set {
			uint radius = (uint)math.clamp(math.round((value - 0.1f) * 10f), 0f, 127f);
			data0 = (data0 & ~RadiusMask) | (radius << RadiusShift);
		}
	}
	[CreateProperty] public float Height {
		get => ((data0 & HeightMask) >> HeightShift) * 0.1f + 0.1f;
		set {
			uint height = (uint)math.clamp(math.round((value - 0.1f) * 10f), 0f, 255f);
			data0 = (data0 & ~HeightMask) | (height << HeightShift);
		}
	}

	[CreateProperty] public int MaxShield {
		get => (int)((data1 & MaxShieldMask) >> MaxShieldShift);
		set {
			uint maxShield = (uint)math.clamp(value, 0, 65535);
			data1 = (data1 & ~MaxShieldMask) | (maxShield << MaxShieldShift);
		}
	}
	[CreateProperty] public int MaxHealth {
		get => (int)((data2 & MaxHealthMask) >> MaxHealthShift);
		set {
			uint maxHealth = (uint)math.clamp(value, 0, 65535);
			data2 = (data2 & ~MaxHealthMask) | (maxHealth << MaxHealthShift);
		}
	}
	[CreateProperty] public int MaxEnergy {
		get => (int)((data2 & MaxEnergyMask) >> MaxEnergyShift);
		set {
			uint maxEnergy = (uint)math.clamp(value, 0, 65535);
			data2 = (data2 & ~MaxEnergyMask) | (maxEnergy << MaxEnergyShift);
		}
	}

	[CreateProperty] public uint Tag {
		get => (data2 & TagMask) >> TagShift;
		set => data2 = (data2 & ~TagMask) | (value << TagShift);
	}
	public bool GetTag(Tag tag) => (Tag & (1u << (int)tag)) != 0u;
	public void SetTag(Tag tag, bool value) {
		Tag = value ? (Tag | (1u << (int)tag)) : (Tag & ~(1u << (int)tag));
	}
	public bool HasTag   (Tag tag) => GetTag(tag);
	public void AddTag   (Tag tag) => SetTag(tag, true );
	public void RemoveTag(Tag tag) => SetTag(tag, false);

	[CreateProperty] public uint Immunity {
		get => (data3 & ImmunityMask) >> ImmunityShift;
		set => data3 = (data3 & ~ImmunityMask) | (value << ImmunityShift);
	}
	public Immunity GetImmunity(Effect effect) {
		return (Immunity)((Immunity >> ((int)effect * 2)) & 0b11u);
	}
	public void SetImmunity(Effect effect, Immunity immunity) {
		Immunity = (Immunity & ~(0b11u << ((int)effect * 2))) | ((uint)immunity << ((int)effect * 2));
	}



	public Head TempHead {
		get => (Head)((data4 & TempHeadMask) >> TempHeadShift);
		set => data4 = (data4 & ~TempHeadMask) | ((uint)value << TempHeadShift);
	}
	public Body TempBody {
		get => (Body)((data4 & TempBodyMask) >> TempBodyShift);
		set => data4 = (data4 & ~TempBodyMask) | ((uint)value << TempBodyShift);
	}
	[CreateProperty] public uint TempFlag {
		get => (data4 & TempFlagMask) >> TempFlagShift;
		set => data4 = (data4 & ~TempFlagMask) | (value << TempFlagShift);
	}
	public bool GetTempFlag(Flag flag) => (TempFlag & (1u << (int)flag)) != 0u;
	public void SetTempFlag(Flag flag, bool value) {
		TempFlag = value ? (TempFlag | (1u << (int)flag)) : (TempFlag & ~(1u << (int)flag));
	}
	public bool HasTempFlag   (Flag flag) => GetTempFlag(flag);
	public void AddTempFlag   (Flag flag) => SetTempFlag(flag, true );
	public void RemoveTempFlag(Flag flag) => SetTempFlag(flag, false);

	[CreateProperty] public uint TempTeam {
		get => (data4 & TempTeamMask) >> TempTeamShift;
		set => data4 = (data4 & ~TempTeamMask) | (value << TempTeamShift);
	}
	public bool GetTempTeam(Team team) => (TempTeam & (1u << (int)team)) != 0u;
	public void SetTempTeam(Team team, bool value) {
		TempTeam = value ? (TempTeam | (1u << (int)team)) : (TempTeam & ~(1u << (int)team));
	}
	public bool HasTempTeam   (Team team) => GetTempTeam(team);
	public void AddTempTeam   (Team team) => SetTempTeam(team, true );
	public void RemoveTempTeam(Team team) => SetTempTeam(team, false);

	public int BarTick {
		get => (int)((data4 & BarTickMask) >> BarTickShift);
		set {
			uint barTick = (uint)math.clamp(value, 0, 255);
			data4 = (data4 & ~BarTickMask) | (barTick << BarTickShift);
		}
	}
	public float BarCooldown {
		get => BarTick * NetworkManager.Ticktime;
		set => BarTick = (int)math.round(value * NetworkManager.Tickrate);
	}



	// Ghost Properties

	[CreateProperty] public Head Head {
		get => (Head)((data5 & HeadMask) >> HeadShift);
		set => data5 = (data5 & ~HeadMask) | ((uint)value << HeadShift);
	}
	[CreateProperty] public Body Body {
		get => (Body)((data5 & BodyMask) >> BodyShift);
		set => data5 = (data5 & ~BodyMask) | ((uint)value << BodyShift);
	}
	[CreateProperty] public uint Flag {
		get => (data5 & FlagMask) >> FlagShift;
		set => data5 = (data5 & ~FlagMask) | (value << FlagShift);
	}
	public bool GetFlag(Flag flag) => (Flag & (1u << (int)flag)) != 0u;
	public void SetFlag(Flag flag, bool value) {
		Flag = value ? (Flag | (1u << (int)flag)) : (Flag & ~(1u << (int)flag));
	}
	public bool HasFlag   (Flag flag) => GetFlag(flag);
	public void AddFlag   (Flag flag) => SetFlag(flag, true );
	public void RemoveFlag(Flag flag) => SetFlag(flag, false);

	[CreateProperty] public uint Team {
		get => (data5 & TeamMask) >> TeamShift;
		set => data5 = (data5 & ~TeamMask) | (value << TeamShift);
	}
	public bool GetTeam(Team team) => (Team & (1u << (int)team)) != 0u;
	public void SetTeam(Team team, bool value) {
		Team = value ? (Team | (1u << (int)team)) : (Team & ~(1u << (int)team));
	}
	public bool HasTeam   (Team team) => GetTeam(team);
	public void AddTeam   (Team team) => SetTeam(team, true );
	public void RemoveTeam(Team team) => SetTeam(team, false);

	[CreateProperty] public bool MaskColorX {
		get => (data5 & ColorXMask) != 0u;
		set => data5 = (data5 & ~ColorXMask) | (value ? ColorXMask : 0u);
	}
	[CreateProperty] public byte MaskColorY {
		get => (byte)(data5 & ColorYMask);
		set => data5 = (data5 & ~ColorYMask) | ((uint)(value % 127) << ColorYShift);
	}
	[CreateProperty] public color MaskColor => ColorExtensions.ToColor(MaskColorX, MaskColorY);



	[CreateProperty] public int Shield {
		get => (int)((data6 & ShieldMask) >> ShieldShift);
		set {
			uint shield = (uint)math.clamp(value, 0, 65535);
			data6 = (data6 & ~ShieldMask) | (shield << ShieldShift);
		}
	}
	[CreateProperty] public int Health {
		get => (int)((data7 & HealthMask) >> HealthShift);
		set {
			uint health = (uint)math.clamp(value, 0, 65535);
			data7 = (data7 & ~HealthMask) | (health << HealthShift);
		}
	}
	[CreateProperty] public int Energy {
		get => (int)((data7 & EnergyMask) >> EnergyShift);
		set {
			uint energy = (uint)math.clamp(value, 0, 65535);
			data7 = (data7 & ~EnergyMask) | (energy << EnergyShift);
		}
	}
	public int PureShield => math.min(Shield, MaxShield);
	public int PureHealth => math.min(Health, MaxHealth);
	public int PureEnergy => math.min(Energy, MaxEnergy);

	public int OverShield => math.max(0, Shield - MaxShield);
	public int OverHealth => math.max(0, Health - MaxHealth);
	public int OverEnergy => math.max(0, Energy - MaxEnergy);

	public void AdjustHealth(int value) {
		if (value < 0) {
			if (0 < Shield) Shield -= value;
			else            Health -= value;
		}
		else {
			int delta = MaxHealth - Health;
			if (Health < MaxHealth) Health += value;
			if (Shield < MaxShield) Shield += math.max(0, value - delta);
		}
	}

	public int ShieldTick {
		get => (int)((data6 & ShieldTickMask) >> ShieldTickShift);
		set {
			uint shieldTick = (uint)math.clamp(value, 0, 255);
			data6 = (data6 & ~ShieldTickMask) | (shieldTick << ShieldTickShift);
		}
	}
	public int EnergyTick {
		get => (int)((data6 & EnergyTickMask) >> EnergyTickShift);
		set {
			uint energyTick = (uint)math.clamp(value, 0, 255);
			data6 = (data6 & ~EnergyTickMask) | (energyTick << EnergyTickShift);
		}
	}
	[CreateProperty] public float ShieldCooldown {
		get => ShieldTick * NetworkManager.Ticktime;
		set => ShieldTick = (int)math.round(value * NetworkManager.Tickrate);
	}
	[CreateProperty] public float EnergyCooldown {
		get => EnergyTick * NetworkManager.Ticktime;
		set => EnergyTick = (int)math.round(value * NetworkManager.Tickrate);
	}



	[CreateProperty] public Motion MotionX {
		get => (Motion)((data8 & MotionXMask) >> MotionXShift);
		set {
			if (MotionX != value) MotionXTick = 0;
			data8 = (data8 & ~MotionXMask) | ((uint)value << MotionXShift);
		}
	}
	[CreateProperty] public Motion MotionY {
		get => (Motion)((data8 & MotionYMask) >> MotionYShift);
		set {
			if (MotionY != value) MotionYTick = 0;
			data8 = (data8 & ~MotionYMask) | ((uint)value << MotionYShift);
		}
	}
	public int MotionXTick {
		get => (int)((data8 & MotionXTickMask) >> TickXShift);
		set {
			uint motionXTick = (uint)math.max(0, value % 2048);
			data8 = (data8 & ~MotionXTickMask) | (motionXTick << TickXShift);
		}
	}
	public int MotionYTick {
		get => (int)((data8 & MotionYTickMask) >> TickYShift);
		set {
			uint motionYTick = (uint)math.max(0, value % 2048);
			data8 = (data8 & ~MotionYTickMask) | (motionYTick << TickYShift);
		}
	}
	[CreateProperty] public float MotionXOffset {
		get => MotionXTick * NetworkManager.Ticktime;
		set => MotionXTick = (int)math.round(value * NetworkManager.Tickrate);
	}
	[CreateProperty] public float MotionYOffset {
		get => MotionYTick * NetworkManager.Ticktime;
		set => MotionYTick = (int)math.round(value * NetworkManager.Tickrate);
	}



	[CreateProperty] public int GravityFactor {
		get => (int)((data9 & GravityFMask) >> GravityFShift);
		set {
			uint gravityFactor = (uint)math.clamp(value, 0, 255);
			data9 = (data9 & ~GravityFMask) | (gravityFactor << GravityFShift);
		}
	}
	[CreateProperty] public float3 GravityVector {
		get => new(0f, GravityFactor, 0f);
	}
	public bool IsGrounded => (data9 & GravityFMask) == 0u;

	[CreateProperty] public int KnockFactor {
		get => (int)((data9 & KnockFMask) >> KnockFShift);
		set {
			uint knockFactor = (uint)math.clamp(value, 0, 63);
			data9 = (data9 & ~KnockFMask) | (knockFactor << KnockFShift);
		}
	}
	[CreateProperty] public float3 KnockVector {
		get {
			float x = (((data9 & KnockXMask) >> KnockXShift) - 31f) * 0.0322581f;
			float y = (((data9 & KnockYMask) >> KnockYShift) - 31f) * 0.0322581f;
			float z = (((data9 & KnockZMask) >> KnockZShift) - 31f) * 0.0322581f;
			return KnockFactor * 0.125f * new float3(x, y, z);
		}
		set {
			KnockFactor = (int)(math.length(value) * 8f);
			float3 normalized = math.normalize(value);
			uint x = (uint)(math.round(normalized.x * 31f) + 31f) << KnockXShift;
			uint y = (uint)(math.round(normalized.y * 31f) + 31f) << KnockYShift;
			uint z = (uint)(math.round(normalized.z * 31f) + 31f) << KnockZShift;
			data9 = (data9 & ~(KnockXMask | KnockYMask | KnockZMask)) | x | y | z;
		}
	}
	public bool IsKnocked => (data9 & KnockFMask) != 0u;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Effect
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[InternalBufferCapacity(5)]
public struct CreatureEffect : IBufferElementData {

	// Constants

	const uint ValueMask
		= (1u << (int)Effect.InstantDamage)
		& (1u << (int)Effect.InstantHeal)
		& (1u << (int)Effect.HealthBoost)
		& (1u << (int)Effect.EnergyBoost)
		& (1u << (int)Effect.DamageBoost);

	const uint EffectMask   = 0xF0000000u;
	const uint StrengthMask = 0x0FFFF000u;
	const uint DurationMask = 0x00000FFFu;

	const int EffectShift   = 28;
	const int StrengthShift = 12;
	const int DurationShift =  0;



	// Fields

	[GhostField] public uint data;



	// Properties

	public bool IsValueType => (ValueMask & (1u << (int)Effect)) != 0;

	[CreateProperty, GhostField] public Effect Effect {
		get => (Effect)((data & EffectMask) >> EffectShift);
		set => data = (data & ~EffectMask) | ((uint)value << EffectShift);
	}
	[GhostField] public int Temp {
		get => (int)((data & StrengthMask) >> StrengthShift);
		set {
			uint temp = (uint)math.clamp(value, 0, 65535);
			data = (data & ~StrengthMask) | (temp << StrengthShift);
		}
	}
	[GhostField] public int Tick {
		get => (int)((data & DurationMask) >> DurationShift);
		set {
			uint tick = (uint)math.clamp(value, 0,  2047);
			data = (data & ~DurationMask) | (tick << DurationShift);
		}
	}
	[CreateProperty, GhostField] public float Strength {
		get => Temp * (IsValueType ? 1f : 0.001f);
		set => Temp = (int)math.round(value * (IsValueType ? 1f : 1000f));
	}
	[CreateProperty, GhostField] public float Duration {
		get => Tick * NetworkManager.Ticktime;
		set => Tick = (int)math.round(value * NetworkManager.Tickrate);
	}
}



public static class DynamicBufferExtensions {
	public static bool TryGetIndex(this DynamicBuffer<CreatureEffect> buffer,
		Effect effect, out int index) {
		index = -1;
		for (int i = 0; i < buffer.Length; i++) if (buffer[i].Effect == effect) {
			index = i;
			return true;
		}
		return false;
	}

	public static void AddEffect(this DynamicBuffer<CreatureEffect> buffer, in CreatureCore core,
		Effect effect, float strength, float duration, float maxStrength = 0f, float maxDuration = 0f) {
		var multiplier = math.max(0f, 1f - core.GetImmunity(effect).ToValue());
		if (multiplier == 0f) return;

		var element = new CreatureEffect();
		element.Effect   = effect;
		element.Strength = strength * multiplier;
		element.Duration = duration * (element.IsValueType ? 1f : multiplier);

		if (TryGetIndex(buffer, effect, out int index)) {
			if (maxStrength == 0f) maxStrength = math.max(element.Strength, buffer[index].Strength);
			if (maxDuration == 0f) maxDuration = math.max(element.Duration, buffer[index].Duration);
			element.Strength = math.min(buffer[index].Strength + element.Strength, maxStrength);
			element.Duration = math.min(buffer[index].Duration + element.Duration, maxDuration);
			buffer.ElementAt(index) = element;
		}
		else buffer.Add(element);
	}

	public static void AddDamage(this DynamicBuffer<CreatureEffect> buffer, in CreatureCore core,
		int value) {
		AddEffect(buffer, in core, Effect.InstantDamage, value, 0.2f, float.MaxValue, float.MaxValue);
	}

	public static void RemoveEffect(this DynamicBuffer<CreatureEffect> buffer, Effect effect) {
		if (TryGetIndex(buffer, effect, out int index)) buffer.RemoveAt(index);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Initialization System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
[UpdateBefore(typeof(EndInitializationEntityCommandBufferSystem))]
partial struct CreatureInitializationSystem : ISystem {

	[BurstCompile]
	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
		state.RequireForUpdate<PrefabContainer>();
		state.RequireForUpdate<SimulationSingleton>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state) {
		var singleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
		var buffer    = singleton.CreateCommandBuffer(state.WorldUnmanaged);

		var fieldsComparisonJob = new CreatureFieldsComparisonJob();
		state.Dependency = fieldsComparisonJob.ScheduleParallel(state.Dependency);

		var componentsModificationJob = new CreatureComponentsModificationJob {
			entityManager = state.EntityManager,
			buffer        = buffer.AsParallelWriter(),
			prefabArray   = SystemAPI.GetSingletonBuffer<PrefabContainer>(true),
			coreArray     = SystemAPI.GetComponentLookup<CreatureCore   >(true),
			colliderArray = SystemAPI.GetComponentLookup<PhysicsCollider>(true),
			massArray     = SystemAPI.GetComponentLookup<PhysicsMass    >(true),
			tileArray     = SystemAPI.GetBufferLookup<TileDrawer  >(true),
			spriteArray   = SystemAPI.GetBufferLookup<SpriteDrawer>(true),
			shadowArray   = SystemAPI.GetBufferLookup<ShadowDrawer>(true),
			uiArray       = SystemAPI.GetBufferLookup<UIDrawer    >(true),
		};
		state.Dependency = componentsModificationJob.ScheduleParallel(state.Dependency);
	}

	[BurstCompile, WithAll(typeof(CreatureInitialize))]
	[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
	partial struct CreatureFieldsComparisonJob : IJobEntity {

		public void Execute(
			in Simulate simulate,
			in CreatureCore core,
			EnabledRefRW<CreatureInitialize> initialize) {

			if (core.TempHead != core.Head) initialize.ValueRW = true;
			if (core.TempBody != core.Body) initialize.ValueRW = true;
			if (core.TempTeam != core.Team) initialize.ValueRW = true;
			if (core.TempFlag != core.Flag) initialize.ValueRW = true;
		}
	}

	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct CreatureComponentsModificationJob : IJobEntity {
		[NativeDisableContainerSafetyRestriction] public EntityManager entityManager;
		[NativeDisableContainerSafetyRestriction] public EntityCommandBuffer.ParallelWriter buffer;
		[NativeDisableContainerSafetyRestriction] public DynamicBuffer  <PrefabContainer> prefabArray;
		[NativeDisableContainerSafetyRestriction] public ComponentLookup<CreatureCore   > coreArray;
		[NativeDisableContainerSafetyRestriction] public ComponentLookup<PhysicsCollider> colliderArray;
		[NativeDisableContainerSafetyRestriction] public ComponentLookup<PhysicsMass    > massArray;
		[NativeDisableContainerSafetyRestriction] public BufferLookup<TileDrawer  > tileArray;
		[NativeDisableContainerSafetyRestriction] public BufferLookup<SpriteDrawer> spriteArray;
		[NativeDisableContainerSafetyRestriction] public BufferLookup<ShadowDrawer> shadowArray;
		[NativeDisableContainerSafetyRestriction] public BufferLookup<UIDrawer    > uiArray;

		public void Execute(
			Entity entity,
			[ChunkIndexInQuery] int sortKey,
			ref CreatureCore    core,
			ref PhysicsMass     mass,
			ref PhysicsCollider collider,
			DynamicBuffer<TileDrawer  > tile,
			DynamicBuffer<SpriteDrawer> sprite,
			DynamicBuffer<ShadowDrawer> shadow,
			DynamicBuffer<UIDrawer    > ui,
			EnabledRefRW<CreatureInitialize> initialize) {

			mass.InverseInertia = float3.zero;
			initialize.ValueRW = false;

			if (core.TempHead != core.Head) {
				var a = core.TempHead.ToComponent();
				var b = core.    Head.ToComponent();
				var aMatch = (a != default) &&  entityManager.HasComponent(entity, a);
				var bMatch = (b != default) && !entityManager.HasComponent(entity, b);
				if (aMatch) buffer.RemoveComponent(sortKey, entity, a);
				if (bMatch) buffer.   AddComponent(sortKey, entity, b);
				core.TempHead = core.Head;
			}

			if (core.TempBody != core.Body) {
				var a = core.TempBody.ToComponent();
				var b = core.    Body.ToComponent();
				var aMatch = (a != default) &&  entityManager.HasComponent(entity, a);
				var bMatch = (b != default) && !entityManager.HasComponent(entity, b);
				if (aMatch) buffer.RemoveComponent(sortKey, entity, a);
				if (bMatch) buffer.   AddComponent(sortKey, entity, b);
				core.TempBody = core.Body;

				var prefab = prefabArray[(int)core.Body].Prefab;
				core.Radius    = coreArray[prefab].Radius;
				core.Height    = coreArray[prefab].Height;
				core.MaxShield = coreArray[prefab].MaxShield;
				core.MaxHealth = coreArray[prefab].MaxHealth;
				core.MaxEnergy = coreArray[prefab].MaxEnergy;
				core.Tag       = coreArray[prefab].Tag;
				core.Immunity  = coreArray[prefab].Immunity;

				core.MotionX     = coreArray[prefab].MotionX;
				core.MotionY     = coreArray[prefab].MotionY;
				core.MotionXTick = coreArray[prefab].MotionXTick;
				core.MotionYTick = coreArray[prefab].MotionYTick;
				mass.InverseMass = massArray[prefab].InverseMass;
				collider.Value   = colliderArray[prefab].Value;

				tile  .Length = tileArray  [prefab].Length;
				sprite.Length = spriteArray[prefab].Length;
				shadow.Length = shadowArray[prefab].Length;
				ui    .Length = uiArray    [prefab].Length;
				for (int i = 0; i < tile  .Length; i++) tile  [i] = tileArray  [prefab][i];
				for (int i = 0; i < sprite.Length; i++) sprite[i] = spriteArray[prefab][i];
				for (int i = 0; i < shadow.Length; i++) shadow[i] = shadowArray[prefab][i];
				for (int i = 0; i < ui    .Length; i++) ui    [i] = uiArray    [prefab][i];
			}

			if (core.TempFlag != core.Flag) {
				for (int i = 0; i < 8; i++) {
					var flag = (Flag)i;
					var x = core.HasTempFlag(flag);
					var y = core.    HasFlag(flag);
					if (x == y) continue;
					core.SetTempFlag(flag, y);

					var prefab = prefabArray[(int)core.Body].Prefab;
					switch (flag) {
						case Flag.Pinned:
							if (y) mass.InverseMass = 1f / PhysicsExtensions.PinnedMass;
							else   mass.InverseMass = massArray[prefab].InverseMass;
							break;
						case Flag.Piercing:
							var filter = colliderArray[prefab].Value.Value.GetCollisionFilter();
							if (y) filter.CollidesWith &= ~(1u << (int)PhysicsCategory.Creature);
							else   filter.CollidesWith |=  (1u << (int)PhysicsCategory.Creature);
							if (!collider.Value.Value.GetCollisionFilter().Equals(filter)) {
								if (!collider.IsUnique) collider.MakeUnique(entity, buffer, sortKey);
								collider.Value.Value.SetCollisionFilter(filter);
							}
							break;
						case Flag.Interactable:
							buffer.SetComponentEnabled<Interactable>(sortKey, entity, y);
							break;
					}
				}
			}

			if (core.TempTeam != core.Team) {
				for (int i = 0; i < 8; i++) {
					var team = (Team)i;
					var x = core.HasTempTeam(team);
					var y = core.    HasTeam(team);
					if (x == y) continue;
					core.SetTempTeam(team, y);
				}
			}
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Begin Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderFirst = true)]
[UpdateAfter(typeof(PredictedFixedStepSimulationSystemGroup))]
partial struct CreatureBeginSimulationSystem : ISystem {

	[BurstCompile]
	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<PrefabContainer>();
		state.RequireForUpdate<SimulationSingleton>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state) {
		var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
		var gravityRemovalJob = new CreatureGravityRemovalJob {
			core    = SystemAPI.GetComponentLookup<CreatureCore>(),
			trigger = SystemAPI.GetComponentLookup<EventTrigger>(true),
		};
		state.Dependency = gravityRemovalJob.Schedule(simulationSingleton, state.Dependency);

		var beginSimulationJob = new CreatureBeginSimulationJob();
		state.Dependency = beginSimulationJob.ScheduleParallel(state.Dependency);
	}

	[BurstCompile]
	partial struct CreatureGravityRemovalJob : ITriggerEventsJob {
		public ComponentLookup<CreatureCore> core;
		[ReadOnly] public ComponentLookup<EventTrigger> trigger;

		public void Execute(TriggerEvent triggerEvent) {
			Execute(triggerEvent.EntityA, triggerEvent.EntityB);
			Execute(triggerEvent.EntityB, triggerEvent.EntityA);
		}

		public void Execute(Entity entity, Entity target) {
			if (core.HasComponent(entity) && !trigger.HasComponent(target)) {
				var temp = core[entity];
				temp.GravityFactor = 0;
				core[entity] = temp;
			}
		}
	}

	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct CreatureBeginSimulationJob : IJobEntity {
		public void Execute(in CreatureCore core, ref PhysicsVelocity velocity) {
			velocity.Linear = float3.zero;
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature End Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
partial struct CreatureEndSimulationSystem : ISystem {

	[BurstCompile]
	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<NetworkTime>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state) {
		var networkTime = SystemAPI.GetSingleton<NetworkTime>();
		var endSimulationJob = new EndCreatureSimulationJob {
			isFirstFullTick = networkTime.IsFirstTimeFullyPredictingTick,
			deltaTime       = SystemAPI.Time.DeltaTime,
		};
		state.Dependency = endSimulationJob.ScheduleParallel(state.Dependency);
	}

	[BurstCompile, WithAll(typeof(Simulate))]
	partial struct EndCreatureSimulationJob : IJobEntity {
		public bool  isFirstFullTick;
		public float deltaTime;

		public void Execute(ref CreatureCore core, DynamicBuffer<CreatureEffect> effect,
			ref PhysicsVelocity velocity) {

			if (isFirstFullTick) {
				/*if (core.Shield < core.MaxShield && --core.ShieldTick == 0) {
					core.ShieldCooldown = 0.5f;
					core.Shield++;
				}
				if (core.Energy < core.MaxEnergy && --core.EnergyTick == 0) {
					core.EnergyCooldown = 0.5f;
					core.Energy++;
				}*/

				foreach (var element in effect) switch (element.Effect) {
					case Effect.InstantDamage:
						// core.Health--
						break;
				}
				for (int i = effect.Length - 1; -1 < i; i--) {
					if (--effect.ElementAt(i).Tick == 0) effect.RemoveAt(i);
				}
			}

			if (!core.HasFlag(Flag.Floating)) {
				float multiplier = PhysicsExtensions.GravityMultiplier;
				velocity.Linear += multiplier * deltaTime * core.GravityVector;
				if (isFirstFullTick) core.GravityFactor++;
			}
			if (core.IsKnocked) {
				float multiplier = PhysicsExtensions.KnockMultiplier;
				velocity.Linear += multiplier * deltaTime * core.KnockVector;
				if (isFirstFullTick) core.KnockFactor--;
			}
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature Presentation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
partial struct CreaturePresentationSystem : ISystem {

	[BurstCompile]
	public void OnUpdate(ref SystemState state) {
		var presentationJob = new CreaturePresentationJob();
		state.Dependency = presentationJob.ScheduleParallel(state.Dependency);
	}

	[BurstCompile]
	partial struct CreaturePresentationJob : IJobEntity {
		public void Execute(ref CreatureCore core, ref DynamicBuffer<UIDrawer> ui) {
			//if (core.BarTick == 0) {
			//	if (!drawer.UIs.IsEmpty) drawer.UIs.Clear();
			//}
			//else {
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

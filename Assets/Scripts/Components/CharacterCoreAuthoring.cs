using UnityEngine;
using UnityEngine.Rendering;
using System;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Properties;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Extensions;
using Unity.Physics.GraphicsIntegration;
using Unity.NetCode;
using Unity.Jobs;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif



public enum Head : uint {
	None,
	Dummy,
	Player,
}

public static class HeadExtensions {
	public static Type ToAuthoring(this Head head) => head switch {
		Head.Dummy  => typeof(DummyHeadAuthoring),
		Head.Player => typeof(PlayerHeadAuthoring),
		_ => null,
	};

	public static ComponentType ToComponent(this Head head) => head switch {
		Head.Dummy  => ComponentType.ReadOnly<DummyHeadData>(),
		Head.Player => ComponentType.ReadOnly<PlayerHeadData>(),
		_ => default,
	};

	public static bool TryGetPrefab(
		this Head head, DynamicBuffer<CharacterContainer> container,
		ComponentLookup<CharacterCoreData> coreDataLookup, out Entity prefab) {
		foreach (var entity in container.Reinterpret<Entity>()) {
			bool match = entity != Entity.Null;
			match &= coreDataLookup.TryGetComponent(entity, out var component);
			match &= component.Head == head;
			if (match) {
				prefab = entity;
				return true;
			}
		}
		prefab = default;
		return false;
	}
}

public enum Body : uint {
	None,
	Dummy,
	Player,
}

public static class BodyExtensions {
	public static Type ToAuthoring(this Body body) => body switch {
		Body.Dummy  => typeof(DummyBodyAuthoring),
		Body.Player => typeof(PlayerBodyAuthoring),
		_ => null,
	};

	public static ComponentType ToComponent(this Body body) => body switch {
		Body.Dummy  => ComponentType.ReadOnly<DummyBodyData>(),
		Body.Player => ComponentType.ReadOnly<PlayerBodyData>(),
		_ => default,
	};

	public static bool TryGetPrefab(
		this Body body, DynamicBuffer<CharacterContainer> container,
		ComponentLookup<CharacterCoreData> coreDataLookup, out Entity prefab) {
		foreach (var entity in container.Reinterpret<Entity>()) {
			bool match = entity != Entity.Null;
			match &= coreDataLookup.TryGetComponent(entity, out var component);
			match &= component.Body == body;
			if (match) {
				prefab = entity;
				return true;
			}
		}
		prefab = default;
		return false;
	}
}

public enum Flag : byte {
	Pinned,
	Floating,
	Piercing,
}

public struct FlagPinned : IComponentData, IEnableableComponent { }
public struct FlagFloating : IComponentData, IEnableableComponent { }
public struct FlagPiercing : IComponentData, IEnableableComponent { }

public static class FlagExtensions {
	public static ComponentType ToComponent(this Flag flag) => flag switch {
		Flag.Pinned   => ComponentType.ReadOnly<FlagPinned>(),
		Flag.Floating => ComponentType.ReadOnly<FlagFloating>(),
		Flag.Piercing => ComponentType.ReadOnly<FlagPiercing>(),
		_ => default,
	};
}

public enum Team : byte {
	Players,
	Monsters,
}

public struct TeamPlayers : IComponentData, IEnableableComponent { }
public struct TeamMonsters : IComponentData, IEnableableComponent { }

public static class TeamExtensions {
	public static ComponentType ToComponent(this Team team) => team switch {
		Team.Players  => ComponentType.ReadOnly<TeamPlayers>(),
		Team.Monsters => ComponentType.ReadOnly<TeamMonsters>(),
		_ => default,
	};
}

public enum Tag : byte {
	Undead,
	Boss,
}

public struct TagUndead : IComponentData, IEnableableComponent { }
public struct TagBoss : IComponentData, IEnableableComponent { }

public static class TagExtensions {
	public static ComponentType ToComponent(this Tag tag) => tag switch {
		Tag.Undead => ComponentType.ReadOnly<TagUndead>(),
		Tag.Boss   => ComponentType.ReadOnly<TagBoss>(),
		_ => default,
	};
}

public enum PhysicsCategory : byte {
	Character,
	EventTrigger,
	Particle,
	RenderArea,
	Terrain,
}

public static class Physics {
	public const float PinnedMass = 10000f;
	public const float InitialGravity = -3.00f;
	public const float GravityScale = -9.81f;
	public const float KnockScale = 10.00f;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Core Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Character Core")]
[RequireComponent(typeof(PhysicsBodyAuthoring), typeof(PhysicsShapeAuthoring))]
[RequireComponent(typeof(GhostAuthoringComponent))]
public sealed class CharacterCoreAuthoring : MonoComponent<CharacterCoreAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(CharacterCoreAuthoring))]
	class CharacterCoreAuthoringEditor : EditorExtensions {
		CharacterCoreAuthoring I => target as CharacterCoreAuthoring;
		public override void OnInspectorGUI() {
			Begin();

			if (Enum.TryParse(I.name, out Character character)) {
				BeginDisabledGroup();
				EnumField("Character", character);
				EndDisabledGroup();
				Space();
			}

			BeginDisabledGroup(I.IsPrefabConnected);
			I.Head = TextEnumField("Head Component", I.Head);
			I.Body = TextEnumField("Body Component", I.Body);
			I.Flag = FlagField<Flag>("Flag", I.Flag);
			I.Team = FlagField<Team>("Team", I.Team);
			Space();
			I.Tag = FlagField<Tag>("Tag", I.Tag);
			I.RoughHeight = FloatField("Rough Height", I.RoughHeight);
			I.RoughRadius = FloatField("Rough Radius", I.RoughRadius);
			I.DefaultMass = FloatField("Default Mass", I.DefaultMass);
			EndDisabledGroup();

			End();
		}
	}

	void OnDrawGizmosSelected() {
		const float Sample = 8.0f;
		float o = 0f;
		float h = RoughHeight;
		float r = RoughRadius;
		var position = transform.position;
		int segments = Mathf.Max(3, Mathf.RoundToInt(Sample * 2f * Mathf.PI * r));
		float step = 2f * Mathf.PI / segments;

		var prev = position + new Vector3(Mathf.Sin(0) * r, 0f, Mathf.Cos(0) * r);
		for (int i = 0; i < segments; i++) {
			float f = (i + 1) * step;
			var next = position + new Vector3(Mathf.Sin(f) * r, 0f, Mathf.Cos(f) * r);
			Gizmos.color = new Color(0.5f, 1.0f, 0.5f, 1.0f);
			Gizmos.DrawLine(prev + new Vector3(0f, o, 0f), next + new Vector3(0f, o, 0f));
			Gizmos.DrawLine(prev + new Vector3(0f, h, 0f), next + new Vector3(0f, h, 0f));
			Gizmos.color = new Color(0.5f, 1.0f, 0.5f, 0.1f);
			Gizmos.DrawLine(next + new Vector3(0f, o, 0f), next + new Vector3(0f, h, 0f));
			prev = next;
		}
	}
	#endif



	// Fields

	#if UNITY_EDITOR
	[SerializeField] string m_HeadName;
	[SerializeField] string m_BodyName;
	#endif

	[SerializeField] Head m_Head;
	[SerializeField] Body m_Body;
	[SerializeField] uint m_Flag;
	[SerializeField] uint m_Team;

	[SerializeField] byte m_Tag;
	[SerializeField] float m_RoughHeight = 1.2f;
	[SerializeField] float m_RoughRadius = 0.4f;
	PhysicsBodyAuthoring m_PhysicsBody;

	[SerializeField] Vector3 m_Center;
	[SerializeField] Sprite m_Sprite;

	[SerializeField] FixedBytes30 m_HeadData;
	[SerializeField] FixedBytes30 m_BodyData;



	// Properties

	#if UNITY_EDITOR
	public Head Head {
		get => !Enum.TryParse(m_HeadName, out Head head) ?
			Enum.Parse<Head>(m_HeadName = m_Head.ToString()) :
			m_Head = head;
		set {
			if (m_Head != value) SwitchHeadAuthoring(m_Head, value);
			m_HeadName = (m_Head = value).ToString();
		}
	}
	public Body Body {
		get => !Enum.TryParse(m_BodyName, out Body body) ?
			Enum.Parse<Body>(m_BodyName = m_Body.ToString()) :
			m_Body = body;
		set {
			if (m_Body != value) SwitchBodyAuthoring(m_Body, value);
			m_BodyName = (m_Body = value).ToString();
		}
	}
	#else
	public Head Head {
		get => m_Head;
		set => m_Head = value;
	}
	public Body Body {
		get => m_Body;
		set => m_Body = value;
	}
	#endif

	uint Flag {
		get => m_Flag;
		set => m_Flag = value;
	}
	uint Team {
		get => m_Team;
		set => m_Team = value;
	}



	byte Tag {
		get => m_Tag;
		set => m_Tag = value;
	}
	float RoughHeight {
		get => m_RoughHeight;
		set => m_RoughHeight = Mathf.Max(0.01f, value);
	}
	float RoughRadius {
		get => m_RoughRadius;
		set => m_RoughRadius = Mathf.Max(0.01f, value);
	}

	PhysicsBodyAuthoring PhysicsBody => !m_PhysicsBody ?
		m_PhysicsBody = GetOwnComponent<PhysicsBodyAuthoring>() :
		m_PhysicsBody;

	float DefaultMass {
		get => PhysicsBody.Mass;
		set => PhysicsBody.Mass = value;
	}



	public ref FixedBytes30 HeadData {
		get => ref m_HeadData;
	}
	public ref FixedBytes30 BodyData {
		get => ref m_BodyData;
	}



	// Methods

	#if UNITY_EDITOR
	void SwitchHeadAuthoring(Head prev, Head next) {
		var prevHead = prev.ToAuthoring();
		if (prevHead != null && TryGetComponent(prevHead, out var component)) {
			DestroyImmediate(component, true);
		}
		var nextHead = next.ToAuthoring();
		if (nextHead != null && !HasComponent(nextHead)) {
			if (HasComponent<CharacterEffectAuthoring>()) {
				AddComponentAfter(nextHead, typeof(CharacterEffectAuthoring));
			} else AddComponentAfter(nextHead, typeof(CharacterCoreAuthoring));
		}
		HeadData = default;
	}

	void SwitchBodyAuthoring(Body prev, Body next) {
		var prevBody = prev.ToAuthoring();
		if (prevBody != null && TryGetComponent(prevBody, out var component)) {
			DestroyImmediate(component, true);
		}
		var nextBody = next.ToAuthoring();
		if (nextBody != null && !HasComponent(nextBody)) {
			if (HasComponent(Head.ToAuthoring())) {
				AddComponentAfter(nextBody, Head.ToAuthoring());
			} else if (HasComponent<CharacterEffectAuthoring>()) {
				AddComponentAfter(nextBody, typeof(CharacterEffectAuthoring));
			} else AddComponentAfter(nextBody, typeof(CharacterCoreAuthoring));
		}
		BodyData = default;
	}

	bool HasComponent<T>() where T : Component => TryGetComponent<T>(out _);
	bool HasComponent(Type componentType) => TryGetComponent(componentType, out _);

	Component AddComponentAfter(Type componentType, Type target) {
		var component = gameObject.AddComponent(componentType);
		var components = gameObject.GetComponents<Component>();
		int a = -1;
		int b = -1;
		for (int i = 0; i < components.Length; i++) {
			if (components[i].GetType() == target) a = i;
			if (components[i] == component) b = i;
		}
		if (a != -1 && b != -1) {
			int n = b - (a + 1);
			for (int i = 0; i < n; i++) ComponentUtility.MoveComponentUp(component);
		}
		return component;
	}
	#endif



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

	bool GetTag(Tag tag) => (Tag & (1 << (int)tag)) != 0;
	void SetTag(Tag tag, bool value) => Tag = value switch {
		true  => (byte)(Tag |  (1 << (int)tag)),
		false => (byte)(Tag & ~(1 << (int)tag)),
	};



	// Baker

	class Baker : Baker<CharacterCoreAuthoring> {
		public override void Bake(CharacterCoreAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.Dynamic);
			AddComponent(entity, new CharacterInitialize());
			AddComponent(entity, new CharacterSwitchHead());
			AddComponent(entity, new CharacterSwitchBody());
			AddComponent(entity, new CharacterSwitchFlag());
			AddComponent(entity, new CharacterSwitchTeam());
			SetComponentEnabled<CharacterSwitchHead>(entity, false);
			SetComponentEnabled<CharacterSwitchBody>(entity, false);
			for (int i = 0; i < 8; i++) {
				var flag = ((Flag)i).ToComponent();
				var team = ((Team)i).ToComponent();
				if (flag != default) AddComponent(entity, flag);
				if (team != default) AddComponent(entity, team);
			}
			for (int i = 0; i < 8; i++) {
				var tag = ((Tag)i).ToComponent();
				if (tag != default) AddComponent(entity, tag);
			}
			AddComponent(entity, new CharacterInput());
			AddComponent(entity, new CharacterCoreBlob {
				Value = this.AddBlobAsset(new CharacterCoreBlobData {

					Tag         = authoring.Tag,
					RoughHeight = authoring.RoughHeight,
					RoughRadius = authoring.RoughRadius,
					DefaultMass = authoring.DefaultMass,

				})
			});
			AddComponent(entity, new CharacterCoreData {

				Head = authoring.Head,
				Body = authoring.Body,
				Flag = authoring.Flag,
				Team = authoring.Team,

			});
			AddComponent(entity, new CharacterTempData {

				Head = authoring.Head,
				Body = authoring.Body,
				Flag = authoring.Flag,
				Team = authoring.Team,

			});
			AddComponent(entity, new CharacterHeadBlob {
				Value = this.AddBlobAsset(new CharacterHeadBlobData {

					Data = authoring.HeadData,

				})
			});
			AddComponent(entity, new CharacterBodyBlob {
				Value = this.AddBlobAsset(new CharacterBodyBlobData {

					Data = authoring.BodyData,

				})
			});
			AddComponent(entity, authoring.Head.ToComponent());
			AddComponent(entity, authoring.Body.ToComponent());
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Initialize
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CharacterInitialize : IComponentData, IEnableableComponent { }

public struct CharacterSwitchHead : IComponentData, IEnableableComponent { }
public struct CharacterSwitchBody : IComponentData, IEnableableComponent { }
public struct CharacterSwitchFlag : IComponentData, IEnableableComponent { }
public struct CharacterSwitchTeam : IComponentData, IEnableableComponent { }



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Input
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CharacterInput : IComponentData {

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



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Core Blob
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CharacterCoreBlob : IComponentData {

	// Fields

	public BlobAssetReference<CharacterCoreBlobData> Value;
}



public struct CharacterCoreBlobData {

	// Fields

	public byte Tag;
	public float RoughHeight;
	public float RoughRadius;
	public float DefaultMass;



	// Methods

	public bool GetTag(Tag tag) => (Tag & (1 << (int)tag)) != 0;
	public void SetTag(Tag tag, bool value) => Tag = value switch {
		true  => (byte)(Tag |  (1 << (int)tag)),
		false => (byte)(Tag & ~(1 << (int)tag)),
	};
}



public static class CharacterCoreBlobExtensions {

	// Methods

	public static bool GetTag(
		this CharacterCoreBlob coreblob, Tag tag) {
		return coreblob.Value.Value.GetTag(tag);
	}

	public static void SetTag(
		this CharacterCoreBlob coreblob, Tag tag, bool value) {
		coreblob.Value.Value.SetTag(tag, value);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Core Data
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[GhostComponent]
public struct CharacterCoreData : IComponentData {

	// Constants

	const uint HeadMask = 0xFC000000u;
	const uint BodyMask = 0x03FF0000u;
	const uint FlagMask = 0x0000FF00u;
	const uint TeamMask = 0x000000FFu;

	const int HeadShift = 26;
	const int BodyShift = 16;
	const int FlagShift = 08;
	const int TeamShift = 00;

	const uint MotionMask = 0xF8000000u;
	const uint TimeMask   = 0x07FFFF00u;
	const uint DataMask   = 0x000000FFu;

	const int MotionShift = 27;
	const int TimeShift   = 08;
	const int DataShift   = 00;

	const uint GravityFMask = 0xFF000000u;
	const uint KnockFMask   = 0x00FC0000u;
	const uint KnockXMask   = 0x0003F000u;
	const uint KnockYMask   = 0x00000FC0u;
	const uint KnockZMask   = 0x0000003Fu;

	const int GravityFShift = 24;
	const int KnockFShift   = 18;
	const int KnockXShift   = 12;
	const int KnockYShift   = 06;
	const int KnockZShift   = 00;



	// Fields

	[GhostField, HideInInspector] public uint Data0;
	[GhostField, HideInInspector] public uint Data1;
	[GhostField, HideInInspector] public uint Data2;



	// Properties

	[CreateProperty] public Head Head {
		get => (Head)((Data0 & HeadMask) >> HeadShift);
		set => Data0 = (Data0 & ~HeadMask) | (((uint)value << HeadShift) & HeadMask);
	}
	[CreateProperty] public Body Body {
		get => (Body)((Data0 & BodyMask) >> BodyShift);
		set => Data0 = (Data0 & ~BodyMask) | (((uint)value << BodyShift) & BodyMask);
	}
	[CreateProperty] public uint Flag {
		get => (Data0 & FlagMask) >> FlagShift;
		set => Data0 = (Data0 & ~FlagMask) | ((value << FlagShift) & FlagMask);
	}
	[CreateProperty] public uint Team {
		get => (Data0 & TeamMask) >> TeamShift;
		set => Data0 = (Data0 & ~TeamMask) | ((value << TeamShift) & TeamMask);
	}



	[CreateProperty] public Motion Motion {
		get => (Motion)((Data1 & MotionMask) >> MotionShift);
		set => Data1 = (Data1 & ~MotionMask) | (((uint)value << MotionShift) & MotionMask);
	}
	[CreateProperty] public float Time {
		get => ((Data1 & TimeMask) >> TimeShift) * 0.001f;
		set => Data1 = (Data1 & ~TimeMask) | (((uint)(value * 1000f) << TimeShift) & TimeMask);
	}
	[CreateProperty] public uint Data {
		get => (Data1 & DataMask) >> DataShift;
		set => Data1 = (Data1 & ~DataMask) | ((value << DataShift) & DataMask);
	}



	public int GravityFactor {
		get => (int)((Data2 & GravityFMask) >> GravityFShift);
		set {
			uint factor = (uint)math.clamp(value, 0, GravityFMask >> GravityFShift);
			Data2 = (Data2 & ~GravityFMask) | (factor << GravityFShift);
		}
	}
	public float3 GravityVector {
		get => new(0f, GravityFactor * NetworkManager.TickInterval, 0f);
		set => GravityFactor = (int)(value.y * NetworkManager.TickRate);
	}
	public bool IsGrounded {
		get => (Data2 & GravityFMask) == 0u;
	}

	[CreateProperty] public float3 FinalGravity {
		get {
			var initialGravity = new float3(0f, IsGrounded ? 0f : Physics.InitialGravity, 0f);
			return initialGravity + GravityVector * Physics.GravityScale;
		}
	}

	public int KnockFactor {
		get => (int)((Data2 & KnockFMask) >> KnockFShift);
		set {
			uint factor = (uint)math.clamp(value, 0, KnockFMask >> KnockFShift);
			Data2 = (Data2 & ~KnockFMask) | (factor << KnockFShift);
		}
	}
	public float3 KnockVector {
		get {
			float x = (((Data2 & KnockXMask) >> KnockXShift) - 31f) * 0.0322581f;
			float y = (((Data2 & KnockYMask) >> KnockYShift) - 31f) * 0.0322581f;
			float z = (((Data2 & KnockZMask) >> KnockZShift) - 31f) * 0.0322581f;
			return KnockFactor * new float3(x, y, z) * 3.0f * NetworkManager.TickInterval;
		}
		set {
			var normalized = math.normalize(value);
			uint x = (uint)(math.round(normalized.x * 31f) + 31f) << KnockXShift;
			uint y = (uint)(math.round(normalized.y * 31f) + 31f) << KnockYShift;
			uint z = (uint)(math.round(normalized.z * 31f) + 31f) << KnockZShift;
			Data2 = (Data2 & ~(KnockXMask | KnockYMask | KnockZMask)) | x | y | z;
			KnockFactor = (int)math.length(value * 0.333333f * NetworkManager.TickRate);
		}
	}
	public bool IsKnocked {
		get => (Data2 & KnockFMask) != 0u;
	}

	[CreateProperty] public float3 FinalKnock {
		get => KnockVector * Physics.KnockScale;
	}



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



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Temp Data
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CharacterTempData : IComponentData {

	// Constants

	const uint HeadMask = 0xFC000000u;
	const uint BodyMask = 0x03FF0000u;
	const uint FlagMask = 0x0000FF00u;
	const uint TeamMask = 0x000000FFu;

	const int HeadShift = 26;
	const int BodyShift = 16;
	const int FlagShift = 08;
	const int TeamShift = 00;

	const uint GravityFMask = 0xFF000000u;
	const uint KnockFMask   = 0x00FC0000u;
	const uint KnockXMask   = 0x0003F000u;
	const uint KnockYMask   = 0x00000FC0u;
	const uint KnockZMask   = 0x0000003Fu;

	const int GravityFShift = 24;
	const int KnockFShift   = 18;
	const int KnockXShift   = 12;
	const int KnockYShift   = 06;
	const int KnockZShift   = 00;



	// Fields

	[HideInInspector] public uint Data0;
	[HideInInspector] public uint Data2;



	// Properties

	[CreateProperty] public Head Head {
		get => (Head)((Data0 & HeadMask) >> HeadShift);
		set => Data0 = (Data0 & ~HeadMask) | (((uint)value << HeadShift) & HeadMask);
	}
	[CreateProperty] public Body Body {
		get => (Body)((Data0 & BodyMask) >> BodyShift);
		set => Data0 = (Data0 & ~BodyMask) | (((uint)value << BodyShift) & BodyMask);
	}
	[CreateProperty] public uint Flag {
		get => (Data0 & FlagMask) >> FlagShift;
		set => Data0 = (Data0 & ~FlagMask) | ((value << FlagShift) & FlagMask);
	}
	[CreateProperty] public uint Team {
		get => (Data0 & TeamMask) >> TeamShift;
		set => Data0 = (Data0 & ~TeamMask) | ((value << TeamShift) & TeamMask);
	}



	public int GravityFactor {
		get => (int)((Data2 & GravityFMask) >> GravityFShift);
		set {
			uint factor = (uint)math.clamp(value, 0, GravityFMask >> GravityFShift);
			Data2 = (Data2 & ~GravityFMask) | (factor << GravityFShift);
		}
	}
	public float3 GravityVector {
		get => new(0f, GravityFactor * NetworkManager.TickInterval, 0f);
		set => GravityFactor = (int)(value.y * NetworkManager.TickRate);
	}
	public bool IsGrounded {
		get => (Data2 & GravityFMask) == 0u;
	}

	[CreateProperty] public float3 FinalGravity {
		get {
			var initialGravity = new float3(0f, IsGrounded ? 0f : Physics.InitialGravity, 0f);
			return initialGravity + GravityVector * Physics.GravityScale;
		}
	}

	public int KnockFactor {
		get => (int)((Data2 & KnockFMask) >> KnockFShift);
		set {
			uint factor = (uint)math.clamp(value, 0, KnockFMask >> KnockFShift);
			Data2 = (Data2 & ~KnockFMask) | (factor << KnockFShift);
		}
	}
	public float3 KnockVector {
		get {
			float x = (((Data2 & KnockXMask) >> KnockXShift) - 31f) * 0.0322581f;
			float y = (((Data2 & KnockYMask) >> KnockYShift) - 31f) * 0.0322581f;
			float z = (((Data2 & KnockZMask) >> KnockZShift) - 31f) * 0.0322581f;
			return KnockFactor * new float3(x, y, z) * 3.0f * NetworkManager.TickInterval;
		}
		set {
			var normalized = math.normalize(value);
			uint x = (uint)(math.round(normalized.x * 31f) + 31f) << KnockXShift;
			uint y = (uint)(math.round(normalized.y * 31f) + 31f) << KnockYShift;
			uint z = (uint)(math.round(normalized.z * 31f) + 31f) << KnockZShift;
			Data2 = (Data2 & ~(KnockXMask | KnockYMask | KnockZMask)) | x | y | z;
			KnockFactor = (int)math.length(value * 0.333333f * NetworkManager.TickRate);
		}
	}
	public bool IsKnocked {
		get => (Data2 & KnockFMask) != 0u;
	}

	[CreateProperty] public float3 FinalKnock {
		get => KnockVector * Physics.KnockScale;
	}



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



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Head Blob
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CharacterHeadBlob : IComponentData {

	// Fields

	public BlobAssetReference<CharacterHeadBlobData> Value;
}



public struct CharacterHeadBlobData {

	// Fields

	public FixedBytes30 Data;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Body Blob
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CharacterBodyBlob : IComponentData {

	// Fields

	public BlobAssetReference<CharacterBodyBlobData> Value;
}



public struct CharacterBodyBlobData {

	// Fields

	public FixedBytes30 Data;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Initialization System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSInitializationSystemGroup))]
partial struct CharacterCoreInitializationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
		state.RequireForUpdate<CharacterContainer>();
		state.RequireForUpdate<CharacterCoreData>();
	}

	public void OnUpdate(ref SystemState state) {
		var bufferSystem =
			SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
		var buffer = bufferSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

		state.Dependency = new CharacterCoreInitializationJob {
		}.ScheduleParallel(state.Dependency);
		state.Dependency = new CharacterCoreDataComparisonJob {
		}.ScheduleParallel(state.Dependency);
		state.Dependency = new CharacterHeadSwitchJob {
			Buffer              = buffer,
			CharacterContainer = SystemAPI.GetSingletonBuffer<CharacterContainer>(true),
			CoreData           = SystemAPI.GetComponentLookup<CharacterCoreData>(),
			TempData           = SystemAPI.GetComponentLookup<CharacterTempData>(),
			HeadBlob           = SystemAPI.GetComponentLookup<CharacterHeadBlob>(),
		}.ScheduleParallel(state.Dependency);
		state.Dependency = new CharacterBodySwitchJob {
			Buffer             = buffer,
			CharacterContainer = SystemAPI.GetSingletonBuffer<CharacterContainer>(true),
			CoreBlob           = SystemAPI.GetComponentLookup<CharacterCoreBlob>(),
			CoreData           = SystemAPI.GetComponentLookup<CharacterCoreData>(),
			TempData           = SystemAPI.GetComponentLookup<CharacterTempData>(),
			BodyBlob           = SystemAPI.GetComponentLookup<CharacterBodyBlob>(),
			StatusBlob         = SystemAPI.GetComponentLookup<CharacterStatusBlob>(),
			StatusData         = SystemAPI.GetComponentLookup<CharacterStatusData>(),
			EffectBlob         = SystemAPI.GetComponentLookup<CharacterEffectBlob>(),
			EffectData         = SystemAPI.GetBufferLookup<CharacterEffectData>(),
			Center             = SystemAPI.GetComponentLookup<SpritePropertyCenter>(),
			BaseColor          = SystemAPI.GetComponentLookup<SpritePropertyBaseColor>(),
			MaskColor          = SystemAPI.GetComponentLookup<SpritePropertyMaskColor>(),
			Emission           = SystemAPI.GetComponentLookup<SpritePropertyEmission>(),
			Billboard          = SystemAPI.GetComponentLookup<SpritePropertyBillboard>(),
			Hash               = SystemAPI.GetComponentLookup<SpriteHash>(),
			Mass               = SystemAPI.GetComponentLookup<PhysicsMass>(),
			Collider           = SystemAPI.GetComponentLookup<PhysicsCollider>(),
		}.ScheduleParallel(state.Dependency);
		state.Dependency = new CharacterFlagSwitchJob {
			Buffer = buffer,
		}.ScheduleParallel(state.Dependency);
		state.Dependency = new CharacterTeamSwitchJob {
		}.ScheduleParallel(state.Dependency);
		state.Dependency = new CharacterTagSwitchJob {
		}.ScheduleParallel(state.Dependency);
	}
}

[BurstCompile]
partial struct CharacterCoreInitializationJob : IJobEntity {

	public void Execute(
		EnabledRefRW<CharacterInitialize> initialize,
		in LocalTransform transform,
		ref RenderBounds bounds,
		ref PhysicsMass mass,
		ref PhysicsGraphicalInterpolationBuffer buffer) {

		initialize.ValueRW = false;
		bounds.Value.Extents = new float3(8f, 8f, 8f);
		mass.InverseInertia = default;
		buffer.PreviousTransform.pos = transform.Position;
		buffer.PreviousTransform.rot = transform.Rotation;
	}
}

[BurstCompile]
[WithPresent(typeof(CharacterSwitchHead))]
[WithPresent(typeof(CharacterSwitchBody))]
[WithPresent(typeof(CharacterSwitchFlag))]
[WithPresent(typeof(CharacterSwitchTeam))]
partial struct CharacterCoreDataComparisonJob : IJobEntity {

	public void Execute(
		EnabledRefRW<CharacterSwitchHead> switchHead,
		EnabledRefRW<CharacterSwitchBody> switchBody,
		EnabledRefRW<CharacterSwitchFlag> switchFlag,
		EnabledRefRW<CharacterSwitchTeam> switchTeam,
		in CharacterCoreData coreData,
		in CharacterTempData tempData) {

		if (tempData.Head != coreData.Head) switchHead.ValueRW = true;
		if (tempData.Body != coreData.Body) switchBody.ValueRW = true;
		if (tempData.Flag != coreData.Flag) switchFlag.ValueRW = true;
		if (tempData.Team != coreData.Team) switchTeam.ValueRW = true;
	}
}

[BurstCompile]
partial struct CharacterHeadSwitchJob : IJobEntity {
	public EntityCommandBuffer.ParallelWriter Buffer;
	[ReadOnly] public DynamicBuffer<CharacterContainer> CharacterContainer;
	[NativeDisableParallelForRestriction] public ComponentLookup<CharacterCoreData> CoreData;
	[NativeDisableParallelForRestriction] public ComponentLookup<CharacterTempData> TempData;
	[NativeDisableParallelForRestriction] public ComponentLookup<CharacterHeadBlob> HeadBlob;

	public void Execute(
		EnabledRefRW<CharacterSwitchHead> switchHead,
		Entity entity,
		[ChunkIndexInQuery] int chunkIndex) {

		var coreData = CoreData.GetRefRW(entity);
		var tempData = TempData.GetRefRW(entity);
		var headBlob = HeadBlob.GetRefRW(entity);

		var tempHead = tempData.ValueRO.Head.ToComponent();
		var coreHead = coreData.ValueRO.Head.ToComponent();
		if (tempHead != default) Buffer.RemoveComponent(chunkIndex, entity, tempHead);
		if (coreHead != default) Buffer.AddComponent(chunkIndex, entity, coreHead);
		tempData.ValueRW.Head = coreData.ValueRO.Head;
		switchHead.ValueRW = false;

		if (coreData.ValueRO.Head.TryGetPrefab(CharacterContainer, CoreData, out var prefab)) {
			headBlob.ValueRW.Value = HeadBlob[prefab].Value;
		}
	}
}

[BurstCompile]
partial struct CharacterBodySwitchJob : IJobEntity {
	public EntityCommandBuffer.ParallelWriter Buffer;
	[ReadOnly] public DynamicBuffer<CharacterContainer> CharacterContainer;
	[NativeDisableParallelForRestriction] public ComponentLookup<CharacterCoreBlob> CoreBlob;
	[NativeDisableParallelForRestriction] public ComponentLookup<CharacterCoreData> CoreData;
	[NativeDisableParallelForRestriction] public ComponentLookup<CharacterTempData> TempData;
	[NativeDisableParallelForRestriction] public ComponentLookup<CharacterBodyBlob> BodyBlob;
	[NativeDisableParallelForRestriction] public ComponentLookup<CharacterStatusBlob> StatusBlob;
	[NativeDisableParallelForRestriction] public ComponentLookup<CharacterStatusData> StatusData;
	[NativeDisableParallelForRestriction] public ComponentLookup<CharacterEffectBlob> EffectBlob;
	[NativeDisableParallelForRestriction] public BufferLookup<CharacterEffectData> EffectData;
	[NativeDisableParallelForRestriction] public ComponentLookup<SpritePropertyCenter> Center;
	[NativeDisableParallelForRestriction] public ComponentLookup<SpritePropertyBaseColor> BaseColor;
	[NativeDisableParallelForRestriction] public ComponentLookup<SpritePropertyMaskColor> MaskColor;
	[NativeDisableParallelForRestriction] public ComponentLookup<SpritePropertyEmission> Emission;
	[NativeDisableParallelForRestriction] public ComponentLookup<SpritePropertyBillboard> Billboard;
	[NativeDisableParallelForRestriction] public ComponentLookup<SpriteHash> Hash;
	[NativeDisableParallelForRestriction] public ComponentLookup<PhysicsMass> Mass;
	[NativeDisableParallelForRestriction] public ComponentLookup<PhysicsCollider> Collider;

	public void Execute(
		EnabledRefRW<CharacterSwitchBody> switchBody,
		Entity entity,
		[ChunkIndexInQuery] int chunkIndex) {

		var coreBlob = CoreBlob.GetRefRW(entity);
		var coreData = CoreData.GetRefRW(entity);
		var tempData = TempData.GetRefRW(entity);
		var bodyBlob = BodyBlob.GetRefRW(entity);
		var center = Center.GetRefRW(entity);
		var baseColor = BaseColor.GetRefRW(entity);
		var maskColor = MaskColor.GetRefRW(entity);
		var emission = Emission.GetRefRW(entity);
		var billboard = Billboard.GetRefRW(entity);
		var hash = Hash.GetRefRW(entity);
		var mass = Mass.GetRefRW(entity);
		var collider = Collider.GetRefRW(entity);

		var tempBody = tempData.ValueRO.Body.ToComponent();
		var coreBody = coreData.ValueRO.Body.ToComponent();
		if (tempBody != default) Buffer.RemoveComponent(chunkIndex, entity, tempBody);
		if (coreBody != default) Buffer.AddComponent(chunkIndex, entity, coreBody);
		tempData.ValueRW.Body = coreData.ValueRO.Body;
		switchBody.ValueRW = false;

		if (coreData.ValueRO.Body.TryGetPrefab(CharacterContainer, CoreData, out var prefab)) {
			coreBlob.ValueRW.Value   = CoreBlob[prefab].Value;
			coreData.ValueRW.Flag    = CoreData[prefab].Flag;
			coreData.ValueRW.Motion  = CoreData[prefab].Motion;
			coreData.ValueRW.Time    = CoreData[prefab].Time;
			coreData.ValueRW.Data    = CoreData[prefab].Data;
			bodyBlob.ValueRW.Value   = BodyBlob[prefab].Value;
			center.ValueRW.Value     = Center[prefab].Value;
			baseColor.ValueRW.Value  = BaseColor[prefab].Value;
			emission.ValueRW.Value   = Emission[prefab].Value;
			billboard.ValueRW.Value  = Billboard[prefab].Value;
			hash.ValueRW.Sprite      = Hash[prefab].Sprite;
			hash.ValueRW.Motion      = Hash[prefab].Motion;
			hash.ValueRW.Tick        = Hash[prefab].Tick;
			hash.ValueRW.Flip        = Hash[prefab].Flip;
			mass.ValueRW.InverseMass = Mass[prefab].InverseMass;
			collider.ValueRW.Value   = Collider[prefab].Value;

			if (StatusBlob.HasComponent(entity) && StatusBlob.HasComponent(prefab)) {
				StatusBlob.GetRefRW(entity).ValueRW.Value     = StatusBlob[prefab].Value;
				StatusData.GetRefRW(entity).ValueRW.RawHealth = StatusData[prefab].RawHealth;
			}
			if (EffectBlob.HasComponent(entity) && EffectBlob.HasComponent(prefab)) {
				EffectBlob.GetRefRW(entity).ValueRW.Value = EffectBlob[prefab].Value;
				EffectData[entity].CopyFrom(EffectData[prefab]);
			}
		}
	}
}

[BurstCompile]
[WithPresent(typeof(FlagPinned))]
[WithPresent(typeof(FlagFloating))]
[WithPresent(typeof(FlagPiercing))]
partial struct CharacterFlagSwitchJob : IJobEntity {
	public EntityCommandBuffer.ParallelWriter Buffer;

	public void Execute(
		EnabledRefRW<CharacterSwitchFlag> switchFlag,
		EnabledRefRW<FlagPinned> pinned,
		EnabledRefRW<FlagFloating> floating,
		EnabledRefRW<FlagPiercing> piercing,
		in CharacterCoreBlob coreBlob,
		ref CharacterCoreData coreData,
		ref CharacterTempData tempData,
		ref PhysicsMass mass,
		ref PhysicsCollider collider,
		Entity entity,
		[ChunkIndexInQuery] int chunkIndex) {

		bool tempPinned = tempData.GetFlag(Flag.Pinned);
		bool corePinned = coreData.GetFlag(Flag.Pinned);
		pinned.ValueRW = corePinned;
		if (corePinned != tempPinned) {
			mass.InverseMass = corePinned switch {
				true  => 1f / Physics.PinnedMass,
				false => 1f / coreBlob.Value.Value.DefaultMass,
			};
		}
		bool tempFloating = tempData.GetFlag(Flag.Floating);
		bool coreFloating = coreData.GetFlag(Flag.Floating);
		floating.ValueRW = coreFloating;
		if (coreFloating != tempFloating) {
			if (coreFloating) coreData.GravityFactor = 0;
		}
		bool tempPiercing = tempData.GetFlag(Flag.Piercing);
		bool corePiercing = coreData.GetFlag(Flag.Piercing);
		piercing.ValueRW = corePiercing;
		if (corePiercing != tempPiercing) {
			var filter = collider.Value.Value.GetCollisionFilter();
			filter.CollidesWith = corePiercing switch {
				true  => filter.CollidesWith & ~(1u << (int)PhysicsCategory.Character),
				false => filter.CollidesWith |  (1u << (int)PhysicsCategory.Character),
			};
			if (collider.Value.Value.GetCollisionFilter().Equals(filter) == false) {
				if (!collider.IsUnique) collider.MakeUnique(entity, Buffer, chunkIndex);
				collider.Value.Value.SetCollisionFilter(filter);
			}
		}
		tempData.Flag = coreData.Flag;
		switchFlag.ValueRW = false;
	}
}

[BurstCompile]
[WithPresent(typeof(TeamPlayers))]
[WithPresent(typeof(TeamMonsters))]
partial struct CharacterTeamSwitchJob : IJobEntity {

	public void Execute(
		EnabledRefRW<CharacterSwitchTeam> switchTeam,
		EnabledRefRW<TeamPlayers> teamPlayers,
		EnabledRefRW<TeamMonsters> teamMonsters,
		in CharacterCoreData coreData,
		ref CharacterTempData tempData) {

		teamPlayers.ValueRW  = coreData.GetTeam(Team.Players);
		teamMonsters.ValueRW = coreData.GetTeam(Team.Monsters);
		tempData.Team = coreData.Team;
		switchTeam.ValueRW = false;
	}
}

[BurstCompile]
[WithChangeFilter(typeof(CharacterCoreBlob))]
[WithPresent(typeof(TagUndead))]
[WithPresent(typeof(TagBoss))]
partial struct CharacterTagSwitchJob : IJobEntity {

	public void Execute(
		in CharacterCoreBlob coreBlob,
		EnabledRefRW<TagUndead> undead,
		EnabledRefRW<TagBoss> boss) {

		undead.ValueRW = coreBlob.GetTag(Tag.Undead);
		boss.ValueRW   = coreBlob.GetTag(Tag.Boss);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Begin Predicted Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSPredictedSimulationSystemGroup), OrderFirst = true)]
partial struct CharacterBeginPredictedSimulationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<SimulationSingleton>();
		state.RequireForUpdate<CharacterCoreData>();
	}

	public void OnUpdate(ref SystemState state) {
		var simulation = SystemAPI.GetSingleton<SimulationSingleton>();

		state.Dependency = new CharacterBeginSimulationJob {
			DeltaTime = SystemAPI.Time.DeltaTime,
		}.ScheduleParallel(state.Dependency);
		state.Dependency = new CharacterGravityRemovalJob {
			CoreDataLookup = SystemAPI.GetComponentLookup<CharacterCoreData>(),
		}.Schedule(simulation, state.Dependency);
	}
}

[BurstCompile, WithAll(typeof(Simulate))]
partial struct CharacterBeginSimulationJob : IJobEntity {
	[ReadOnly] public float DeltaTime;

	public void Execute(
		ref CharacterCoreData coreData,
		ref PhysicsVelocity velocity) {

		velocity.Linear = float3.zero;
		coreData.Time += DeltaTime;
	}
}

[BurstCompile, WithAll(typeof(Simulate))]
partial struct CharacterGravityRemovalJob : ICollisionEventsJob {
	public ComponentLookup<CharacterCoreData> CoreDataLookup;

	public void Execute(CollisionEvent collisionEvent) {
		Execute(collisionEvent.EntityA, +collisionEvent.Normal);
		Execute(collisionEvent.EntityB, -collisionEvent.Normal);
	}

	public void Execute(Entity entity, float3 normal) {
		if (CoreDataLookup.HasComponent(entity)) {
			if (math.degrees(math.acos(normal.y)) < 45f) {
				var coreData = CoreDataLookup.GetRefRW(entity);
				coreData.ValueRW.GravityFactor = 0;
			}
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character End Predicted Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSPredictedSimulationSystemGroup), OrderLast = true)]
partial struct CharacterEndPredictedSimulationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<NetworkTime>();
		state.RequireForUpdate<CharacterCoreData>();
	}

	public void OnUpdate(ref SystemState state) {
		state.Dependency = new EndCharacterSimulationJob {
			NetworkTime = SystemAPI.GetSingleton<NetworkTime>(),
			DeltaTime   = SystemAPI.Time.DeltaTime,
		}.ScheduleParallel(state.Dependency);
	}
}

[BurstCompile]
[WithAll(typeof(Simulate))]
partial struct EndCharacterSimulationJob : IJobEntity {
	[ReadOnly] public NetworkTime NetworkTime;
	[ReadOnly] public float DeltaTime;

	public void Execute(
		ref CharacterCoreData coreData,
		ref PhysicsVelocity velocity) {

		if (coreData.GetFlag(Flag.Floating) == false) {
			velocity.Linear += coreData.FinalGravity;
			if (!NetworkTime.IsPartialTick) coreData.GravityFactor++;
		}
		if (coreData.IsKnocked) {
			velocity.Linear += coreData.FinalKnock;
			if (!NetworkTime.IsPartialTick) coreData.KnockFactor--;
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Client Simulation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(DOTSClientSimulationSystemGroup))]
partial struct CharacterClientSimulationSystem : ISystem {

	public void OnCreate(ref SystemState state) {
		state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
		state.RequireForUpdate<CameraBridgeSystem.Singleton>();
		state.RequireForUpdate<ParticleContainer>();
		state.RequireForUpdate<CharacterCoreData>();
	}

	public void OnUpdate(ref SystemState state) {
		var bufferSystem =
			SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
		var buffer = bufferSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
		var cameraBridge = SystemAPI.GetSingleton<CameraBridgeSystem.Singleton>();

		state.Dependency = new CharacterClientSimulationJob {
			Buffer               = buffer,
			PredictedLookup      = SystemAPI.GetComponentLookup<PredictedGhost>(true),
			NetworkTime          = SystemAPI.GetSingleton<NetworkTime>(),
			CameraBridgeProperty = cameraBridge.Property[0],
			ParticleContainer    = SystemAPI.GetSingletonBuffer<ParticleContainer>(true),
		}.ScheduleParallel(state.Dependency);
	}
}

[BurstCompile, WithAll(typeof(Simulate))]
partial struct CharacterClientSimulationJob : IJobEntity {
	public EntityCommandBuffer.ParallelWriter Buffer;
	[ReadOnly] public ComponentLookup<PredictedGhost> PredictedLookup;
	[ReadOnly] public NetworkTime NetworkTime;
	[ReadOnly] public CameraBridge.Property CameraBridgeProperty;
	[ReadOnly] public DynamicBuffer<ParticleContainer> ParticleContainer;

	public void Execute(
		in CharacterCoreBlob coreBlob,
		in CharacterCoreData coreData,
		ref CharacterTempData tempData,
		in LocalTransform transform,
		ref SpriteHash hash,
		Entity entity,
		[ChunkIndexInQuery] int chunkIndex) {

		var rotation = transform.Rotation.value;
		float x = 0.0f + 2.0f * (rotation.y * rotation.w + rotation.x * rotation.z);
		float z = 1.0f - 2.0f * (rotation.y * rotation.y + rotation.z * rotation.z);
		float yaw = math.atan2(x, z) * math.TODEGREES;
		hash.Motion = coreData.Motion;
		hash.ObjectYaw = yaw - CameraBridgeProperty.Yaw;
		hash.Time = coreData.Time;

		bool isPredicted = PredictedLookup.HasComponent(entity);
		bool isFullTick = !NetworkTime.IsPartialTick;
		if (isPredicted != isFullTick) {
			if (coreData.IsGrounded && !tempData.IsGrounded) {
				var right = CameraBridgeProperty.Right;
				var up = CameraBridgeProperty.Up;
				var forward = CameraBridgeProperty.Forward;
				float radius = coreBlob.Value.Value.RoughRadius;
				var position = transform.Position;
				var velocity = tempData.FinalGravity + tempData.FinalKnock;
				if (velocity.y < -5f) {
					var position0 = position - right * radius - forward * radius;
					var position1 = position + right * radius - forward * radius;
					CreateParticle(Particle.SmokeTiny, position0, chunkIndex);
					CreateParticle(Particle.SmokeTiny, position1, chunkIndex);
				}
			}
			tempData.GravityVector = coreData.GravityVector;
			tempData.KnockVector = coreData.KnockVector;
		}
	}

	void CreateParticle(Particle particle, float3 position, int chunkIndex) {
		var transform = LocalTransform.FromPosition(position);
		var prefab = ParticleContainer.GetPrefab(particle);
		var entity = Buffer.Instantiate(chunkIndex, prefab);
		Buffer.SetComponent(chunkIndex, entity, transform);
	}

	void CreateParticle(Particle particle, float3 position, float3 velocity, int chunkIndex) {
		var transform = LocalTransform.FromPosition(position);
		var prefab = ParticleContainer.GetPrefab(particle);
		var entity = Buffer.Instantiate(chunkIndex, prefab);
		Buffer.SetComponent(chunkIndex, entity, transform);
		Buffer.SetComponent(chunkIndex, entity, new ParticleCoreData { Velocity = velocity });
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Renderer Presentation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(DOTSPresentationSystemGroup))]
[UpdateAfter(typeof(RenderAreaPresentationSystem))]
public partial class CharacterRendererMainPresentationSystem : SystemBase {
	IndirectRenderer<SpriteDrawData> RiseShadowRenderer;
	IndirectRenderer<ShadowDrawData> FlatShadowRenderer;
	EntityQuery CharacterQuery;

	protected override void OnCreate() {
		RiseShadowRenderer = new(DrawManager.QuadMesh, DrawManager.SpriteMaterial);
		FlatShadowRenderer = new(DrawManager.QuadMesh, DrawManager.ShadowMaterial);
		RiseShadowRenderer.Param.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
		FlatShadowRenderer.Param.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
		RiseShadowRenderer.Param.layer = RenderArea.MainLayer;
		FlatShadowRenderer.Param.layer = RenderArea.MainLayer;
		RequireForUpdate<RenderAreaPresentationSystem.Singleton>();
		CharacterQuery = GetEntityQuery(
			ComponentType.ReadOnly<LocalToWorld>(),
			ComponentType.ReadOnly<RenderFilter>(),
			ComponentType.ReadOnly<SpritePropertyCenter>(),
			ComponentType.ReadOnly<SpriteHash>());
		RequireForUpdate(CharacterQuery);
	}

	protected override void OnUpdate() {
		var renderAreaSystem = SystemAPI.GetSingleton<RenderAreaPresentationSystem.Singleton>();
		var renderArea = renderAreaSystem.MainRenderArea[0];
		var entityArray = CharacterQuery.ToEntityArray(Allocator.TempJob);
		int count = entityArray.Length;

		var riseShadowBuffer = RiseShadowRenderer.LockBuffer(count);
		new CharacterRiseShadowPresentationJob {
			SpriteBuffer    = riseShadowBuffer,
			EntityArray     = entityArray,
			TransformLookup = GetComponentLookup<LocalToWorld>(true),
			FilterLookup    = GetComponentLookup<RenderFilter>(true),
			CenterLookup    = GetComponentLookup<SpritePropertyCenter>(true),
			HashLookup      = GetComponentLookup<SpriteHash>(true),
			SpriteHashMap   = DrawManager.SpriteHashMapReadOnly,
			GlobalYaw       = EnvironmentManager.Rotation.eulerAngles.y,
			Mask            = renderArea.CullingMask,
		}.Schedule(entityArray.Length, 64, Dependency).Complete();
		RiseShadowRenderer.UnlockBuffer(count);
		RiseShadowRenderer.Draw();
		RiseShadowRenderer.Clear();

		var flatShadowBuffer = FlatShadowRenderer.LockBuffer(count);
		new CharacterFlatShadowPresentationJob {
			ShadowBuffer    = flatShadowBuffer,
			EntityArray     = entityArray,
			TransformLookup = GetComponentLookup<LocalToWorld>(true),
			FilterLookup    = GetComponentLookup<RenderFilter>(true),
			CenterLookup    = GetComponentLookup<SpritePropertyCenter>(true),
			HashLookup      = GetComponentLookup<SpriteHash>(true),
			ShadowHashMap   = DrawManager.ShadowHashMapReadOnly,
			GlobalYaw       = CameraManager.Yaw,
			Mask            = renderArea.CullingMask,
		}.Schedule(entityArray.Length, 64, Dependency).Complete();
		FlatShadowRenderer.UnlockBuffer(count);
		FlatShadowRenderer.Draw();
		FlatShadowRenderer.Clear();

		entityArray.Dispose();
	}

	protected override void OnDestroy() {
		RiseShadowRenderer.Dispose();
		FlatShadowRenderer.Dispose();
	}
}

[UpdateInGroup(typeof(DOTSPresentationSystemGroup))]
[UpdateBefore(typeof(RenderAreaPresentationSystem))]
public partial class CharacterRendererTempPresentationSystem : SystemBase {
	IndirectRenderer<SpriteDrawData> RiseShadowRenderer;
	IndirectRenderer<ShadowDrawData> FlatShadowRenderer;
	EntityQuery CharacterQuery;

	protected override void OnCreate() {
		RiseShadowRenderer = new(DrawManager.QuadMesh, DrawManager.SpriteMaterial);
		FlatShadowRenderer = new(DrawManager.QuadMesh, DrawManager.ShadowMaterial);
		RiseShadowRenderer.Param.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
		FlatShadowRenderer.Param.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
		RiseShadowRenderer.Param.layer = RenderArea.TempLayer;
		FlatShadowRenderer.Param.layer = RenderArea.TempLayer;
		RequireForUpdate<RenderAreaPresentationSystem.Singleton>();
		CharacterQuery = GetEntityQuery(
			ComponentType.ReadOnly<LocalToWorld>(),
			ComponentType.ReadOnly<RenderFilter>(),
			ComponentType.ReadOnly<SpritePropertyCenter>(),
			ComponentType.ReadOnly<SpritePropertyBaseColor>(),
			ComponentType.ReadOnly<SpritePropertyMaskColor>(),
			ComponentType.ReadOnly<SpritePropertyEmission>(),
			ComponentType.ReadOnly<SpriteHash>());
		RequireForUpdate(CharacterQuery);
	}

	protected override void OnUpdate() {
		var renderAreaSystem = SystemAPI.GetSingleton<RenderAreaPresentationSystem.Singleton>();
		if (renderAreaSystem.Transition[0] == 0f) return;
		var renderArea = renderAreaSystem.TempRenderArea[0];
		EnvironmentManager.LightMode = renderArea.LightMode;
		var entityArray = CharacterQuery.ToEntityArray(Allocator.TempJob);
		int count = entityArray.Length;

		var riseShadowBuffer = RiseShadowRenderer.LockBuffer(count);
		new CharacterRiseShadowPresentationJob {
			SpriteBuffer    = riseShadowBuffer,
			EntityArray     = entityArray,
			SpriteHashMap   = DrawManager.SpriteHashMapReadOnly,
			TransformLookup = GetComponentLookup<LocalToWorld>(true),
			FilterLookup    = GetComponentLookup<RenderFilter>(true),
			CenterLookup    = GetComponentLookup<SpritePropertyCenter>(true),
			HashLookup      = GetComponentLookup<SpriteHash>(true),
			GlobalYaw       = EnvironmentManager.Rotation.eulerAngles.y,
			Mask            = renderArea.CullingMask,
		}.Schedule(entityArray.Length, 64, Dependency).Complete();
		RiseShadowRenderer.UnlockBuffer(count);
		RiseShadowRenderer.Draw();
		RiseShadowRenderer.Clear();

		var flatShadowBuffer = FlatShadowRenderer.LockBuffer(count);
		new CharacterFlatShadowPresentationJob {
			ShadowBuffer    = flatShadowBuffer,
			EntityArray     = entityArray,
			ShadowHashMap   = DrawManager.ShadowHashMapReadOnly,
			TransformLookup = GetComponentLookup<LocalToWorld>(true),
			FilterLookup    = GetComponentLookup<RenderFilter>(true),
			CenterLookup    = GetComponentLookup<SpritePropertyCenter>(true),
			HashLookup      = GetComponentLookup<SpriteHash>(true),
			GlobalYaw       = CameraManager.Yaw,
			Mask            = renderArea.CullingMask,
		}.Schedule(entityArray.Length, 64, Dependency).Complete();
		FlatShadowRenderer.UnlockBuffer(count);
		FlatShadowRenderer.Draw();
		FlatShadowRenderer.Clear();

		entityArray.Dispose();
	}

	protected override void OnDestroy() {
		RiseShadowRenderer.Dispose();
		FlatShadowRenderer.Dispose();
	}
}

[BurstCompile]
partial struct CharacterRiseShadowPresentationJob : IJobParallelFor {
	[NativeDisableParallelForRestriction] public NativeArray<SpriteDrawData> SpriteBuffer;
	[ReadOnly] public NativeArray<Entity> EntityArray;
	[ReadOnly] public NativeHashMap<uint, AtlasData>.ReadOnly SpriteHashMap;
	[ReadOnly] public ComponentLookup<LocalToWorld> TransformLookup;
	[ReadOnly] public ComponentLookup<RenderFilter> FilterLookup;
	[ReadOnly] public ComponentLookup<SpritePropertyCenter> CenterLookup;
	[ReadOnly] public ComponentLookup<SpriteHash> HashLookup;
	[ReadOnly] public float GlobalYaw;
	[ReadOnly] public int Mask;

	public void Execute(int index) {
		var entity = EntityArray[index];
		var renderer = FilterLookup[entity];
		var hash = HashLookup[entity];
		var data = DrawSpriteJob.GetSpriteData(SpriteHashMap, new SpriteHash() {
			Sprite    = hash.Sprite,
			Motion    = hash.Motion,
			Direction = default,
			ObjectYaw = hash.ObjectYaw - GlobalYaw,
			Time      = hash.Time,
		}, false);
		SpriteBuffer[index] = new SpriteDrawData() {
			Position  = TransformLookup[entity].Position,
			Rotation  = quaternion.Euler(new float3(0f, GlobalYaw, 0f) * math.TORADIANS).value,
			Scale     = (renderer.MainCullingMask & Mask) != 0 ? data.scale : default,
			Pivot     = data.pivot,
			Tiling    = data.tiling,
			Offset    = data.offset,
			Center    = CenterLookup[entity].Value,
			BaseColor = new float4(1f, 1f, 1f, 1f),
			MaskColor = new float4(1f, 1f, 1f, 0f),
			Emission  = new float4(0f, 0f, 0f, 0f),
			Billboard = 0f,
		};
	}
}

[BurstCompile]
partial struct CharacterFlatShadowPresentationJob : IJobParallelFor {
	[NativeDisableParallelForRestriction] public NativeArray<ShadowDrawData> ShadowBuffer;
	[ReadOnly] public NativeArray<Entity> EntityArray;
	[ReadOnly] public NativeHashMap<uint, AtlasData>.ReadOnly ShadowHashMap;
	[ReadOnly] public ComponentLookup<LocalToWorld> TransformLookup;
	[ReadOnly] public ComponentLookup<RenderFilter> FilterLookup;
	[ReadOnly] public ComponentLookup<SpritePropertyCenter> CenterLookup;
	[ReadOnly] public ComponentLookup<SpriteHash> HashLookup;
	[ReadOnly] public float GlobalYaw;
	[ReadOnly] public int Mask;

	public void Execute(int index) {
		var entity = EntityArray[index];
		var renderer = FilterLookup[entity];
		var hash = HashLookup[entity];
		var data = DrawShadowJob.GetShadowData(ShadowHashMap, new ShadowHash() {
			Sprite    = hash.Sprite,
			Motion    = hash.Motion,
			Direction = default,
			ObjectYaw = hash.ObjectYaw,
			Time      = hash.Time,
		}, false);
		ShadowBuffer[index] = new ShadowDrawData() {
			Position  = TransformLookup[entity].Position,
			Rotation  = quaternion.Euler(new float3(90f, GlobalYaw, 0f) * math.TORADIANS).value,
			Scale     = (renderer.MainCullingMask & Mask) != 0 ? data.scale : default,
			Pivot     = data.pivot,
			Tiling    = data.tiling,
			Offset    = data.offset,
			Center    = new(0f, 0.1f, 0f),
		};
	}
}

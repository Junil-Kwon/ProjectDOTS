using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Jobs;
using Unity.Burst;

#if UNITY_EDITOR
using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Status Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Component/Character Status")]
[RequireComponent(typeof(CharacterCoreAuthoring), typeof(CharacterEffectAuthoring))]
public sealed class CharacterStatusAuthoring : MonoComponent<CharacterStatusAuthoring> {

	// Editor

	#if UNITY_EDITOR
	[CustomEditor(typeof(CharacterStatusAuthoring))]
	class CharacterStatusAuthoringEditor : EditorExtensions {
		CharacterStatusAuthoring I => target as CharacterStatusAuthoring;
		public override void OnInspectorGUI() {
			Begin();

			BeginDisabledGroup(I.IsPrefabConnected);
			I.MaxHealth = UShortField("Max Health", I.MaxHealth);
			I.MaxShield = UShortField("Max Shield", I.MaxShield);
			I.MaxEnergy = UShortField("Max Energy", I.MaxEnergy);
			EndDisabledGroup();

			End();
		}
	}
	#endif



	// Fields

	[SerializeField] ushort m_MaxHealth = 1;
	[SerializeField] ushort m_MaxShield = 0;
	[SerializeField] ushort m_MaxEnergy = 0;



	// Properties

	ushort MaxHealth {
		get => m_MaxHealth;
		set => m_MaxHealth = (ushort)Mathf.Max(1, value);
	}
	ushort MaxShield {
		get => m_MaxShield;
		set => m_MaxShield = (ushort)Mathf.Max(0, value);
	}
	ushort MaxEnergy {
		get => m_MaxEnergy;
		set => m_MaxEnergy = (ushort)Mathf.Max(0, value);
	}

	ushort RawHealth => (ushort)(MaxHealth + MaxShield);
	ushort RawEnergy => MaxEnergy;



	// Baker

	class Baker : Baker<CharacterStatusAuthoring> {
		public override void Bake(CharacterStatusAuthoring authoring) {
			var entity = GetEntity(TransformUsageFlags.Dynamic);
			AddComponent(entity, new DrawCharacterStatus());
			SetComponentEnabled<DrawCharacterStatus>(entity, false);
			AddComponent(entity, new CharacterStatusBlob {
				Value = this.AddBlobAsset(new CharacterStatusBlobData {

					MaxHealth = authoring.MaxHealth,
					MaxShield = authoring.MaxShield,
					MaxEnergy = authoring.MaxEnergy,

				})
			});
			AddComponent(entity, new CharacterStatusData {

				RawHealth = authoring.RawHealth,
				RawEnergy = authoring.RawEnergy,

			});
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Draw Character Status
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct DrawCharacterStatus : IComponentData, IEnableableComponent { }



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Status Blob
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct CharacterStatusBlob : IComponentData {

	// Fields

	public BlobAssetReference<CharacterStatusBlobData> Value;
}



public struct CharacterStatusBlobData {

	// Fields

	public ushort MaxHealth;
	public ushort MaxShield;
	public ushort MaxEnergy;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Status Data
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[GhostComponent]
public struct CharacterStatusData : IComponentData {

	// Fields

	[GhostField] public ushort RawHealth;
	[GhostField] public ushort RawEnergy;
}



public static class CharacterStatusDataExtensions {

	// Methods

	public static (float health, float shield, float excess) GetHealthSet(
		this CharacterStatusData statusData, CharacterStatusBlobData statusBlob) {
		uint maxHealth = statusBlob.MaxHealth;
		uint maxShield = statusBlob.MaxShield;
		uint rawHealth = statusData.RawHealth;
		uint health = math.min(rawHealth, maxHealth);
		uint shield = math.clamp(rawHealth - health, 0, maxShield);
		uint excess = math.max(0, rawHealth - health - shield);
		return (health, shield, excess);
	}

	public static (float energy, float excess) GetEnergySet(
		this CharacterStatusData statusData, CharacterStatusBlobData statusBlob) {
		uint maxEnergy = statusBlob.MaxEnergy;
		uint rawEnergy = statusData.RawEnergy;
		uint energy = math.min(rawEnergy, maxEnergy);
		uint excess = math.max(0, rawEnergy - energy);
		return (energy, excess);
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Character Status Presentation System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(DOTSPresentationSystemGroup))]
public partial class CharacterStatusPresentationSystem : SystemBase {
	IndirectRenderer<CanvasDrawData> CanvasRenderer;
	EntityQuery CanvasQuery;

	protected override void OnCreate() {
		CanvasRenderer = new(DrawManager.QuadMesh, DrawManager.CanvasMaterial, 0);
		CanvasQuery = GetEntityQuery(
			ComponentType.ReadOnly<CharacterCoreBlob>(),
			ComponentType.ReadOnly<DrawCharacterStatus>(),
			ComponentType.ReadOnly<CharacterStatusBlob>(),
			ComponentType.ReadOnly<CharacterStatusData>(),
			ComponentType.ReadOnly<LocalToWorld>());
		RequireForUpdate(CanvasQuery);
	}

	protected override void OnUpdate() {
		var entityArray = CanvasQuery.ToEntityArray(Allocator.TempJob);
		int count = entityArray.Length * 7;
		var canvasBuffer = CanvasRenderer.LockBuffer(count);
		Dependency = new CharacterStatusDrawJob {
			CanvasHashMap             = DrawManager.CanvasHashMapReadOnly,
			EntityArray               = entityArray,
			CharacterCoreBlobLookup   = GetComponentLookup<CharacterCoreBlob>(true),
			CharacterStatusBlobLookup = GetComponentLookup<CharacterStatusBlob>(true),
			CharacterStatusDataLookup = GetComponentLookup<CharacterStatusData>(true),
			TransformLookup           = GetComponentLookup<LocalToWorld>(true),
			CanvasBuffer              = canvasBuffer,
		}.Schedule(entityArray.Length, 64, Dependency);
		Dependency.Complete();
		entityArray.Dispose();
		CanvasRenderer.UnlockBuffer(count);
		CanvasRenderer.Draw();
		CanvasRenderer.Clear();
	}

	protected override void OnDestroy() {
		CanvasRenderer.Dispose();
	}
}

[BurstCompile]
partial struct CharacterStatusDrawJob : IJobParallelFor {
	[ReadOnly] public NativeHashMap<uint, AtlasData>.ReadOnly CanvasHashMap;
	[ReadOnly] public NativeArray<Entity> EntityArray;
	[ReadOnly] public ComponentLookup<CharacterCoreBlob> CharacterCoreBlobLookup;
	[ReadOnly] public ComponentLookup<CharacterStatusBlob> CharacterStatusBlobLookup;
	[ReadOnly] public ComponentLookup<CharacterStatusData> CharacterStatusDataLookup;
	[ReadOnly] public ComponentLookup<LocalToWorld> TransformLookup;
	[NativeDisableParallelForRestriction] public NativeArray<CanvasDrawData> CanvasBuffer;

	public void Execute(int index) {
		var entity = EntityArray[index];
		var coreBlob = CharacterCoreBlobLookup[entity].Value.Value;
		var statusBlob = CharacterStatusBlobLookup[entity].Value.Value;
		var statusData = CharacterStatusDataLookup[entity];
		var transform = TransformLookup[entity];

		float roughHeight = coreBlob.RoughHeight;
		float roughRadius = coreBlob.RoughRadius;
		float maxHealth = statusBlob.MaxHealth + statusBlob.MaxShield;
		var (health, shield, excess) = statusData.GetHealthSet(statusBlob);
		if (0f < excess) excess = maxHealth * (1f - math.exp(-excess / maxHealth));
		float ratio = roughRadius * 2f / maxHealth;
		float width = math.max(health + shield + excess, maxHealth) * ratio;

		float healthScale = health * ratio;
		float shieldScale = shield * ratio;
		float excessScale = excess * ratio;
		float effectScale = 0f;
		float healthPivot = -width * 0.5f + healthScale * 0.5f;
		float shieldPivot = healthPivot + healthScale * 0.5f + shieldScale * 0.5f;
		float excessPivot = shieldPivot + shieldScale * 0.5f + excessScale * 0.5f;
		float effectPivot = 0f;

		var list = new FixedList128Bytes<(Canvas, float, float, color)> {
			(Canvas.BarL, 1f, width * -0.5f - 0.5f, Color.white),
			(Canvas.BarR, 1f, width * +0.5f + 0.5f, Color.white),
			(Canvas.BarM, width, 0f, Color.white),
			(Canvas.Bar, healthScale, healthPivot, new(0xFFFFFF)),
			(Canvas.Bar, shieldScale, shieldPivot, new(0x2F2F2F)),
			(Canvas.Bar, excessScale, excessPivot, new(0x2277BB)),
			(Canvas.Bar, effectScale, effectPivot, new(0xFFAA00)),
		};
		for (int i = 0; i < list.Length; i++) {
			var (canvas, scaleX, pivotX, color) = list[i];
			var data = DrawCanvasJob.GetCanvasData(CanvasHashMap, new CanvasHash() {
				Canvas = canvas,
				Tick   = default,
			}, false);
			CanvasBuffer[index * 7 + i] = new CanvasDrawData {
				Position  = transform.Position,
				Scale     = data.scale * new float2(scaleX, 1f),
				Pivot     = data.pivot + new float2(pivotX, 0f),
				Tiling    = data.tiling,
				Offset    = data.offset,
				Center    = new float3(0f, roughHeight + 1f, 0f),
				BaseColor = color,
				MaskColor = default,
			};
		}
	}
}

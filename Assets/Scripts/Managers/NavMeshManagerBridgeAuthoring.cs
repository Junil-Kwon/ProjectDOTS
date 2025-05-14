using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

#if UNITY_EDITOR
	using UnityEditor;
#endif



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// NavMesh Manager Bridge Authoring
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[AddComponentMenu("Manager/NavMesh Manager Bridge")]
public class NavMeshManagerBridgeAuthoring : MonoBehaviour {

	// Editor

	#if UNITY_EDITOR
		[CustomEditor(typeof(NavMeshManagerBridgeAuthoring))]
		class AIManagerBridgeAuthoringEditor : EditorExtensions {
			NavMeshManagerBridgeAuthoring I => target as NavMeshManagerBridgeAuthoring;
			public override void OnInspectorGUI() {
				Begin("NavMesh Manager Bridge Authoring");

				End();
			}
		}
	#endif



	// Baker

	public class Baker : Baker<NavMeshManagerBridgeAuthoring> {
		public override void Bake(NavMeshManagerBridgeAuthoring authoring) {
			Entity entity = GetEntity(TransformUsageFlags.None);
			AddComponent(entity, new NavMeshManagerBridge());
		}
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// NavMesh Manager Bridge
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public struct NavMeshManagerBridge : IComponentData {

	// Fields

	uint m_Flag;



	// Properties

	public uint Flag {
		get => m_Flag;
		set => m_Flag = value;
	}


	
	// Methods

	public Entity GetPath_Entity0;
	public Entity GetPath_Entity1;
	public Entity GetPath_Entity2;
	public Entity GetPath_Entity3;
	public Entity GetPath_Entity4;
	public Entity GetPath_Entity5;
	public Entity GetPath_Entity6;
	public Entity GetPath_Entity7;
	public FixedList512Bytes<float3> GetPath_List0;
	public FixedList512Bytes<float3> GetPath_List1;
	public FixedList512Bytes<float3> GetPath_List2;
	public FixedList512Bytes<float3> GetPath_List3;
	public FixedList512Bytes<float3> GetPath_List4;
	public FixedList512Bytes<float3> GetPath_List5;
	public FixedList512Bytes<float3> GetPath_List6;
	public FixedList512Bytes<float3> GetPath_List7;
	public byte GetPath_Flag;
	public byte GetPath_Temp;

	public FixedList512Bytes<float3> GetPath(Entity entity, float3 source, float3 target) {
		if (entity == GetPath_Entity0) { GetPath_Flag &= 0b11111110; return GetPath_List0; }
		if (entity == GetPath_Entity1) { GetPath_Flag &= 0b11111101; return GetPath_List1; }
		if (entity == GetPath_Entity2) { GetPath_Flag &= 0b11111011; return GetPath_List2; }
		if (entity == GetPath_Entity3) { GetPath_Flag &= 0b11110111; return GetPath_List3; }
		if (entity == GetPath_Entity4) { GetPath_Flag &= 0b11101111; return GetPath_List4; }
		if (entity == GetPath_Entity5) { GetPath_Flag &= 0b11011111; return GetPath_List5; }
		if (entity == GetPath_Entity6) { GetPath_Flag &= 0b10111111; return GetPath_List6; }
		if (entity == GetPath_Entity7) { GetPath_Flag &= 0b01111111; return GetPath_List7; }

		if ((GetPath_Flag & 0b00000001) == 0) {
			GetPath_Flag |= 0b00000001;
			GetPath_Temp &= 0b11111110;
			GetPath_Entity0 = entity;
			GetPath_List0.Clear();
			GetPath_List0.Add(source);
			GetPath_List0.Add(target);
		}
		if ((GetPath_Flag & 0b00000010) == 0) {
			GetPath_Flag |= 0b00000010;
			GetPath_Temp &= 0b11111101;
			GetPath_Entity1 = entity;
			GetPath_List1.Clear();
			GetPath_List1.Add(source);
			GetPath_List1.Add(target);
		}
		if ((GetPath_Flag & 0b00000100) == 0) {
			GetPath_Flag |= 0b00000100;
			GetPath_Temp &= 0b11111011;
			GetPath_Entity2 = entity;
			GetPath_List2.Clear();
			GetPath_List2.Add(source);
			GetPath_List2.Add(target);
		}
		if ((GetPath_Flag & 0b00001000) == 0) {
			GetPath_Flag |= 0b00001000;
			GetPath_Temp &= 0b11110111;
			GetPath_Entity3 = entity;
			GetPath_List3.Clear();
			GetPath_List3.Add(source);
			GetPath_List3.Add(target);
		}
		if ((GetPath_Flag & 0b00010000) == 0) {
			GetPath_Flag |= 0b00010000;
			GetPath_Temp &= 0b11101111;
			GetPath_Entity4 = entity;
			GetPath_List4.Clear();
			GetPath_List4.Add(source);
			GetPath_List4.Add(target);
		}
		if ((GetPath_Flag & 0b00100000) == 0) {
			GetPath_Flag |= 0b00100000;
			GetPath_Temp &= 0b11011111;
			GetPath_Entity5 = entity;
			GetPath_List5.Clear();
			GetPath_List5.Add(source);
			GetPath_List5.Add(target);
		}
		if ((GetPath_Flag & 0b01000000) == 0) {
			GetPath_Flag |= 0b01000000;
			GetPath_Temp &= 0b10111111;
			GetPath_Entity6 = entity;
			GetPath_List6.Clear();
			GetPath_List6.Add(source);
			GetPath_List6.Add(target);
		}
		if ((GetPath_Flag & 0b10000000) == 0) {
			GetPath_Flag |= 0b10000000;
			GetPath_Temp &= 0b01111111;
			GetPath_Entity7 = entity;
			GetPath_List7.Clear();
			GetPath_List7.Add(source);
			GetPath_List7.Add(target);
		}
		Flag |= 0x0100u;
		return default;
	}
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// NavMesh Manager Bridge System
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[BurstCompile]
[UpdateInGroup(typeof(SingletonBridgeSystemGroup))]
public partial class NavMeshManagerBridgeSystem : SystemBase {

	[BurstCompile]
	protected override void OnCreate() {
		RequireForUpdate<NavMeshManagerBridge>();
	}

	[BurstDiscard]
	protected override void OnUpdate() {
		var bridge = SystemAPI.GetSingletonRW<NavMeshManagerBridge>();
		var flag   = bridge.ValueRO.Flag;

		if ((flag & 0x0100u) != 0u) {
			if ((bridge.ValueRO.GetPath_Flag & 0b00000001) != 0) {
				if ((bridge.ValueRO.GetPath_Temp & 0b00000001) == 0) {
					bridge.ValueRW.GetPath_Temp |= 0b00000001;
					var source = bridge.ValueRO.GetPath_List0[0];
					var target = bridge.ValueRO.GetPath_List0[1];
					bridge.ValueRW.GetPath_List0.Clear();
					var path = NavMeshManager.GetPath(source, target);
					var size = math.min(path.Count, bridge.ValueRO.GetPath_List0.Capacity);
					for (int i = 0; i < size; i++) bridge.ValueRW.GetPath_List0.Add(path[i]);
				}
				else {
					bridge.ValueRW.GetPath_Flag &= 0b11111110;
					bridge.ValueRW.GetPath_Temp &= 0b11111110;
				}
			}
			if ((bridge.ValueRO.GetPath_Flag & 0b00000010) != 0) {
				if ((bridge.ValueRO.GetPath_Temp & 0b00000010) == 0) {
					bridge.ValueRW.GetPath_Temp |= 0b00000010;
					var source = bridge.ValueRO.GetPath_List1[0];
					var target = bridge.ValueRO.GetPath_List1[1];
					bridge.ValueRW.GetPath_List1.Clear();
					var path = NavMeshManager.GetPath(source, target);
					var size = math.min(path.Count, bridge.ValueRO.GetPath_List1.Capacity);
					for (int i = 0; i < size; i++) bridge.ValueRW.GetPath_List1.Add(path[i]);
				}
				else {
					bridge.ValueRW.GetPath_Flag &= 0b11111101;
					bridge.ValueRW.GetPath_Temp &= 0b11111101;
				}
			}
			if ((bridge.ValueRO.GetPath_Flag & 0b00000100) != 0) {
				if ((bridge.ValueRO.GetPath_Temp & 0b00000100) == 0) {
					bridge.ValueRW.GetPath_Temp |= 0b00000100;
					var source = bridge.ValueRO.GetPath_List2[0];
					var target = bridge.ValueRO.GetPath_List2[1];
					bridge.ValueRW.GetPath_List2.Clear();
					var path = NavMeshManager.GetPath(source, target);
					var size = math.min(path.Count, bridge.ValueRO.GetPath_List2.Capacity);
					for (int i = 0; i < size; i++) bridge.ValueRW.GetPath_List2.Add(path[i]);
				}
				else {
					bridge.ValueRW.GetPath_Flag &= 0b11111011;
					bridge.ValueRW.GetPath_Temp &= 0b11111011;
				}
			}
			if ((bridge.ValueRO.GetPath_Flag & 0b00001000) != 0) {
				if ((bridge.ValueRO.GetPath_Temp & 0b00001000) == 0) {
					bridge.ValueRW.GetPath_Temp |= 0b00001000;
					var source = bridge.ValueRO.GetPath_List3[0];
					var target = bridge.ValueRO.GetPath_List3[1];
					bridge.ValueRW.GetPath_List3.Clear();
					var path = NavMeshManager.GetPath(source, target);
					var size = math.min(path.Count, bridge.ValueRO.GetPath_List3.Capacity);
					for (int i = 0; i < size; i++) bridge.ValueRW.GetPath_List3.Add(path[i]);
				}
				else {
					bridge.ValueRW.GetPath_Flag &= 0b11110111;
					bridge.ValueRW.GetPath_Temp &= 0b11110111;
				}
			}
			if ((bridge.ValueRO.GetPath_Flag & 0b00010000) != 0) {
				if ((bridge.ValueRO.GetPath_Temp & 0b00010000) == 0) {
					bridge.ValueRW.GetPath_Temp |= 0b00010000;
					var source = bridge.ValueRO.GetPath_List4[0];
					var target = bridge.ValueRO.GetPath_List4[1];
					bridge.ValueRW.GetPath_List4.Clear();
					var path = NavMeshManager.GetPath(source, target);
					var size = math.min(path.Count, bridge.ValueRO.GetPath_List4.Capacity);
					for (int i = 0; i < size; i++) bridge.ValueRW.GetPath_List4.Add(path[i]);
				}
				else {
					bridge.ValueRW.GetPath_Flag &= 0b11101111;
					bridge.ValueRW.GetPath_Temp &= 0b11101111;
				}
			}
			if ((bridge.ValueRO.GetPath_Flag & 0b00100000) != 0) {
				if ((bridge.ValueRO.GetPath_Temp & 0b00100000) == 0) {
					bridge.ValueRW.GetPath_Temp |= 0b00100000;
					var source = bridge.ValueRO.GetPath_List5[0];
					var target = bridge.ValueRO.GetPath_List5[1];
					bridge.ValueRW.GetPath_List5.Clear();
					var path = NavMeshManager.GetPath(source, target);
					var size = math.min(path.Count, bridge.ValueRO.GetPath_List5.Capacity);
					for (int i = 0; i < size; i++) bridge.ValueRW.GetPath_List5.Add(path[i]);
				}
				else {
					bridge.ValueRW.GetPath_Flag &= 0b11011111;
					bridge.ValueRW.GetPath_Temp &= 0b11011111;
				}
			}
			if ((bridge.ValueRO.GetPath_Flag & 0b01000000) != 0) {
				if ((bridge.ValueRO.GetPath_Temp & 0b01000000) == 0) {
					bridge.ValueRW.GetPath_Temp |= 0b01000000;
					var source = bridge.ValueRO.GetPath_List6[0];
					var target = bridge.ValueRO.GetPath_List6[1];
					bridge.ValueRW.GetPath_List6.Clear();
					var path = NavMeshManager.GetPath(source, target);
					var size = math.min(path.Count, bridge.ValueRO.GetPath_List6.Capacity);
					for (int i = 0; i < size; i++) bridge.ValueRW.GetPath_List6.Add(path[i]);
				}
				else {
					bridge.ValueRW.GetPath_Flag &= 0b10111111;
					bridge.ValueRW.GetPath_Temp &= 0b10111111;
				}
			}
			if ((bridge.ValueRO.GetPath_Flag & 0b10000000) != 0) {
				if ((bridge.ValueRO.GetPath_Temp & 0b10000000) == 0) {
					bridge.ValueRW.GetPath_Temp |= 0b10000000;
					var source = bridge.ValueRO.GetPath_List7[0];
					var target = bridge.ValueRO.GetPath_List7[1];
					bridge.ValueRW.GetPath_List7.Clear();
					var path = NavMeshManager.GetPath(source, target);
					var size = math.min(path.Count, bridge.ValueRO.GetPath_List7.Capacity);
					for (int i = 0; i < size; i++) bridge.ValueRW.GetPath_List7.Add(path[i]);
				}
				else {
					bridge.ValueRW.GetPath_Flag &= 0b01111111;
					bridge.ValueRW.GetPath_Temp &= 0b01111111;
				}
			}
		}

		if (flag != 0u) bridge.ValueRW.Flag = 0u;
	}
}

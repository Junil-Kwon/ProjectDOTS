using Unity.Entities;
using Unity.NetCode;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// DOTS System Group
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
public partial class DOTSInitializationSystemGroup : ComponentSystemGroup { }

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class DOTSSimulationSystemGroup : ComponentSystemGroup { }

[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
public partial class DOTSPresentationSystemGroup : ComponentSystemGroup { }



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Singleton Bridge System Group
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

// (Local | Client), (Local | Client | Thin Client)
[WorldSystemFilter((WorldSystemFilterFlags)0x500u, (WorldSystemFilterFlags)0xD00u)]
[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
[UpdateBefore(typeof(EndInitializationEntityCommandBufferSystem))]
public partial class SingletonBridgeSystemGroup : ComponentSystemGroup { }



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Creature System Group
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

// (Local | Server)
[WorldSystemFilter((WorldSystemFilterFlags)0x300u)]
[UpdateInGroup(typeof(GhostSimulationSystemGroup))]
public partial class CreatureHeadSystemGroup : ComponentSystemGroup { }

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial class CreatureBodySystemGroup : ComponentSystemGroup { }

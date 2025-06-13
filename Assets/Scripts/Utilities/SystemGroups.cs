using Unity.Entities;
using Unity.NetCode;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Singleton Bridge System Group
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

// (Local World | Client World), (Local World | Client World | Thin Client World)
[WorldSystemFilter((WorldSystemFilterFlags)0x500u, (WorldSystemFilterFlags)0xD00u)]
[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
[UpdateBefore(typeof(EndInitializationEntityCommandBufferSystem))]
public partial class SingletonBridgeSystemGroup : ComponentSystemGroup { }



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// DOTS System Group
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
public partial class DOTSInitializationSystemGroup : ComponentSystemGroup { }

// (Local World | Server World)
[WorldSystemFilter((WorldSystemFilterFlags)0x300u)]
[UpdateInGroup(typeof(GhostSimulationSystemGroup))]
public partial class DOTSGhostSimulationSystemGroup : ComponentSystemGroup { }

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial class DOTSPredictedSimulationSystemGroup : ComponentSystemGroup { }

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class DOTSSimulationSystemGroup : ComponentSystemGroup { }

// (Local World | Server World)
[WorldSystemFilter((WorldSystemFilterFlags)0x300u)]
[UpdateAfter(typeof(DOTSSimulationSystemGroup))]
public partial class DOTSServerSimulationSystemGroup : ComponentSystemGroup { }

// (Local World | Client World), (Local World | Client World | Thin Client World)
[WorldSystemFilter((WorldSystemFilterFlags)0x500u, (WorldSystemFilterFlags)0xD00u)]
[UpdateAfter(typeof(DOTSSimulationSystemGroup))]
public partial class DOTSClientSimulationSystemGroup : ComponentSystemGroup { }

[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
public partial class DOTSPresentationSystemGroup : ComponentSystemGroup { }

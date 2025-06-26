using Unity.Entities;
using Unity.NetCode;



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Singleton Bridge System Group
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation, WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
public partial class SingletonBridgeSystemGroup : ComponentSystemGroup { }



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// DOTS System Group
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
public partial class DOTSInitializationSystemGroup : ComponentSystemGroup { }

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial class DOTSPredictedSimulationSystemGroup : ComponentSystemGroup { }

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation, WorldSystemFilterFlags.ServerSimulation)]
public partial class DOTSServerSimulationSystemGroup : ComponentSystemGroup { }

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation, WorldSystemFilterFlags.ClientSimulation)]
public partial class DOTSClientSimulationSystemGroup : ComponentSystemGroup { }

[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
public partial class DOTSPresentationSystemGroup : ComponentSystemGroup { }

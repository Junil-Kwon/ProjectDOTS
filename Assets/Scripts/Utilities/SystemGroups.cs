using Unity.Entities;
using Unity.NetCode;



public static class SystemFilter {
	public const WorldSystemFilterFlags ServerSimulation = WorldSystemFilterFlags.ServerSimulation;
	public const WorldSystemFilterFlags ClientSimulation = WorldSystemFilterFlags.ClientSimulation;
}



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Singleton Bridge System Group
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[WorldSystemFilter(SystemFilter.ClientSimulation, SystemFilter.ClientSimulation)]
[UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
[UpdateBefore(typeof(DOTSInitializationSystemGroup))]
public partial class SingletonBridgeSystemGroup : ComponentSystemGroup { }



// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// DOTS System Group
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
public partial class DOTSInitializationSystemGroup : ComponentSystemGroup { }

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial class DOTSPredictedSimulationSystemGroup : ComponentSystemGroup { }

[WorldSystemFilter(SystemFilter.ServerSimulation, SystemFilter.ServerSimulation)]
public partial class DOTSServerSimulationSystemGroup : ComponentSystemGroup { }

[WorldSystemFilter(SystemFilter.ClientSimulation, SystemFilter.ClientSimulation)]
public partial class DOTSClientSimulationSystemGroup : ComponentSystemGroup { }

[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
public partial class DOTSPresentationSystemGroup : ComponentSystemGroup { }

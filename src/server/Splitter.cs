using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Linq;

using LogicAPI.Server.Components;
using LogicAPI.Services;
using LogicAPI.Data;
using LogicLog;

using LogicWorld.SharedCode.Components;
using LogicWorld.Server.Circuitry;
using LogicWorld.SharedCode;

using WireBundle.Components;
using WireBundle.Server;

using LICC;

namespace WireBundle.Components
{
    public class Splitter : LogicComponent
    {
        private PegAddress pegAddress;
        private List<Bundler> connectedBundlers = new List<Bundler>();

        // Reflection is nice
        private static Assembly SharedCodeAssembly = AppDomain.CurrentDomain.GetAssemblies().First(x => x.GetName().Name == "LogicWorld.SharedCode");
        private static Type worldDataType = SharedCodeAssembly.GetType("LogicWorld.SharedCode.WorldData");
        private static MethodInfo LookupPegWires = worldDataType.GetMethod("LookupPegWires");
        private static MethodInfo LookupWire = worldDataType.GetMethod("Lookup", new Type[]{ typeof(WireAddress) });
        private static MethodInfo LookupComponent = worldDataType.GetMethod("Lookup", new Type[]{ typeof(ComponentAddress) });
        private static object worldDataInstance = LogicAPI.Service.Get<IWorldData>();
        private static PropertyInfo clusterProperty = typeof(InputPeg).GetProperty("Cluster", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo linkerField = typeof(Cluster).GetField("Linker", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo linkedLeaders = typeof(ClusterLinker).GetField("LinkedLeaders", BindingFlags.NonPublic | BindingFlags.Instance);


        private static Delegate lookupPegWiresDelegate = LookupPegWires.CreateDelegate(Expression.GetDelegateType(
           (from parameter in LookupPegWires.GetParameters() select parameter.ParameterType)
           .Concat(new[] { LookupPegWires.ReturnType })
           .ToArray()), worldDataInstance);
        private static Delegate lookupWireDelegate = LookupWire.CreateDelegate(Expression.GetDelegateType(
            (from parameter in LookupWire.GetParameters() select parameter.ParameterType)
            .Concat(new[] { LookupWire.ReturnType })
            .ToArray()), worldDataInstance);
        private static Delegate lookupComponentDelegate = LookupComponent.CreateDelegate(Expression.GetDelegateType(
            (from parameter in LookupComponent.GetParameters() select parameter.ParameterType)
            .Concat(new[] { LookupComponent.ReturnType })
            .ToArray()), worldDataInstance);

        protected override void Initialize()
        {
            pegAddress = base.Inputs[0].Address;
        }
 
        protected override void DoLogicUpdate()
        {
            List<Bundler> connectedBundlersToTest = new List<Bundler>();
            Cluster cluster = (Cluster)clusterProperty.GetValue(base.Inputs[0]);
            ClusterLinker linker = (ClusterLinker)linkerField.GetValue(cluster);
            foreach (OutputPeg pegTC in cluster.ConnectedOutputs)
			{
                Bundler connectedComponent;
                bool result = Bundlers.Components.TryGetValue(pegTC.oAddress, out connectedComponent);
                if (result)
                {
                    connectedBundlersToTest.Add(connectedComponent);
                }
            }
            if (linker != null)
			{
                foreach (ClusterLinker connectedClusterLinker in (List<ClusterLinker>)linkedLeaders.GetValue(linker))
                {
                    foreach (OutputPeg pegTC in connectedClusterLinker.ClusterBeingLinked.ConnectedOutputs)
                    {
                        Bundler connectedComponent;
                        bool result = Bundlers.Components.TryGetValue(pegTC.oAddress, out connectedComponent);
                        if (result)
                        {
                            connectedBundlersToTest.Add(connectedComponent);
                        }
                    }
                }
            }
            IEnumerable<Bundler> Unchanged = connectedBundlers.Intersect(connectedBundlersToTest);
            if (!((connectedBundlers.Count() == Unchanged.Count()) & (connectedBundlersToTest.Count() == Unchanged.Count())))
            {
                foreach (Bundler toRemove in connectedBundlers.Except(connectedBundlersToTest))
            	{
                    for (int i = 1; i < base.Inputs.Count; i++)
                    {
                        try
                        {
                            toRemove.Inputs[toRemove.Inputs.Count - i].RemoveOneWayPhasicLinkTo(base.Inputs[base.Inputs.Count - i]);
                        }
                        catch (ArgumentOutOfRangeException) { break; }
                    }
                }
                foreach (Bundler toAdd in connectedBundlersToTest.Except(connectedBundlers))
                {
                    for (int i = 1; i < base.Inputs.Count; i++)
                    {
                        try
                        {
                            toAdd.Inputs[toAdd.Inputs.Count - i].AddOneWayPhasicLinkTo(base.Inputs[base.Inputs.Count - i]);
                        }
                        catch (ArgumentOutOfRangeException) { break; }
                    }
                }
                connectedBundlers = connectedBundlersToTest;
            }
            base.QueueLogicUpdate();
        }
    }
}
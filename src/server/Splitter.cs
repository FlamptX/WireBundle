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

        private void checkPeg(PegAddress pegToCheck, List<Bundler> ListToMake, List<PegAddress> Checked)
        {
            Checked.Add(pegToCheck);
            HashSet<WireAddress> wireAddresses = (HashSet<WireAddress>)lookupPegWiresDelegate.DynamicInvoke(pegToCheck);
            if (wireAddresses != null)
            {
                foreach (WireAddress wa in wireAddresses)
                {
                    Wire wire = (Wire)lookupWireDelegate.DynamicInvoke(wa);
                    PegAddress connectedPeg;
                    if (wire.Point1.ToString() != pegToCheck.ToString())
                    {
                        connectedPeg = wire.Point1;
                    }
                    else
                    {
                        connectedPeg = wire.Point2;
                    }
                    IComponentInWorld connectedComponentInWorldToTest = (IComponentInWorld)lookupComponentDelegate.DynamicInvoke(connectedPeg.ComponentAddress);
                    Bundler connectedComponent;
                    bool result = Bundlers.Components.TryGetValue(connectedComponentInWorldToTest, out connectedComponent);
                    if (result)
                    {
                        ListToMake.Add(connectedComponent);
                    }
                    if (!Checked.Contains(connectedPeg))
                    {
                        checkPeg(connectedPeg, ListToMake, Checked);
                    }
                }
            }
        }
        protected override void Initialize()
        {
            pegAddress = base.Inputs[0].Address;
        }
 
        protected override void DoLogicUpdate()
        {
            List<Bundler> connectedBundlersToTest = new List<Bundler>();
            List<PegAddress> Checked = new List<PegAddress>();
            checkPeg(pegAddress, connectedBundlersToTest, Checked);
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
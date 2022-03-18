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

namespace WireBundle.Components
{
    public class Splitter : LogicComponent
    {
        private IComponentInWorld connectedComponentInWorld;
        private PegAddress pegAddress;
        private PegAddress connectedPeg;
        private bool connected = false;

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

        protected override void Initialize()
        {
            pegAddress = base.Inputs[0].Address;
        }
 
        protected override void DoLogicUpdate()
        {
            HashSet<WireAddress> wireAddresses = (HashSet<WireAddress>)lookupPegWiresDelegate.DynamicInvoke(pegAddress);
            if (base.Inputs[0].On)
            {
                Wire wire = (Wire)lookupWireDelegate.DynamicInvoke(wireAddresses.First());
                if (wire.Point1.ToString() != pegAddress.ToString())
                {
                    if (connectedPeg != wire.Point1) { connected = false; }
                    connectedPeg = wire.Point1;
                }
                else
                {
                    if (connectedPeg != wire.Point2) { connected = false; }
                    connectedPeg = wire.Point2;
                }
                if (!connected)
                {
                    connectedComponentInWorld = (IComponentInWorld)lookupComponentDelegate.DynamicInvoke(connectedPeg.ComponentAddress);
                    Bundler connectedComponent;
                    bool result = Bundlers.Components.TryGetValue(connectedComponentInWorld, out connectedComponent);
                    if (result)
                    {
                        for (int i = 1; i < base.Inputs.Count; i++)
                        {
                            try
                            {
                                connectedComponent.Inputs[connectedComponent.Inputs.Count - i].AddOneWayPhasicLinkTo(base.Inputs[base.Inputs.Count - i]);
                            }
                            catch (ArgumentOutOfRangeException) { break; }
                        }
                        connected = true;
                    }
                }
            }
            else if (wireAddresses != null)
            {
                if (connected & wireAddresses.Count == 1)
                {
                    Bundler connectedComponent;
                    bool result = Bundlers.Components.TryGetValue(connectedComponentInWorld, out connectedComponent);
                    if (result)
                    {
                        for (int i = 1; i < base.Inputs.Count; i++)
                        {
                            try
                            {
                                connectedComponent.Inputs[connectedComponent.Inputs.Count - i].RemoveOneWayPhasicLinkTo(base.Inputs[base.Inputs.Count - i]);
                            }
                            catch (ArgumentOutOfRangeException) { break; }
                        }
                        connected = false;
                    }
                }
                else if (connected & wireAddresses.Count == 0)
                {
                    FieldInfo pegField = typeof(InputPeg).GetField("CircuitStates", BindingFlags.NonPublic | BindingFlags.Instance);
                    for (int i = 1; i < base.Inputs.Count; i++)
                    {
                        InputPeg inputPegObj = (InputPeg)base.Inputs[i];
                        CircuitStates pegFieldValue = (CircuitStates)pegField.GetValue(inputPegObj);
                        pegFieldValue[inputPegObj.StateID] = false;
                        pegField.SetValue(inputPegObj, pegFieldValue);
                    }
                }
            }
        }
    }
}
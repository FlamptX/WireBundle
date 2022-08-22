using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using LogicAPI.Server.Components;
using LogicAPI.Services;
using LogicAPI.Data;
using LogicLog;

using LogicWorld.SharedCode.Components;
using LogicWorld.Server;
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

        private static readonly IWorldData worldData;
        
        static Splitter()
        {
            worldData = Program.Get<IWorldData>();
            if(worldData == null)
            {
                throw new Exception("Could not get service IWorldData. Report this issue to the developer of this mod.");
            }
        }
        
        protected override void Initialize()
        {
            pegAddress = base.Inputs[0].Address;
        }
 
        protected override void DoLogicUpdate()
        {
            HashSet<WireAddress> wireAddresses = worldData.LookupPegWires(pegAddress);
            if (base.Inputs[0].On)
            {
                Wire wire = worldData.Lookup(wireAddresses.First());
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
                    connectedComponentInWorld = worldData.Lookup(connectedPeg.ComponentAddress);
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
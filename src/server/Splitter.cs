using System;
using System.Collections.Generic;
using System.Reflection;

using LogicAPI.Server.Components;
using LogicAPI.Services;
using LogicAPI.Data;

using LogicWorld.Server;
using LogicWorld.Server.Circuitry;

namespace WireBundle.Components
{
    public class Splitter : LogicComponent
    {
        private Bundler remoteBundler;

        private static readonly IWorldData worldData;
        private static readonly ICircuitryManager circuitryManager;

        private static readonly PropertyInfo clusterProperty;
        private static readonly FieldInfo linkerField;
        
        static Splitter()
        {
            //Get the services, and confirm, that they are available:
            worldData = Program.Get<IWorldData>();
            if (worldData == null)
            {
                throw new Exception("Could not get service IWorldData. Report this issue to the developer of this mod.");
            }
            circuitryManager = Program.Get<ICircuitryManager>();
            if (circuitryManager == null)
            {
                throw new Exception("Could not get service ICircuitryManager. Report this issue to the developer of this mod.");
            }
            //Reflection, to cleanup after LogicWorld, when deleting a Bundler:
            clusterProperty = typeof(InputPeg).GetProperty("Cluster", BindingFlags.NonPublic | BindingFlags.Instance);
            if(clusterProperty == null)
            {
                throw new Exception("Could not find property 'Cluster' in class InputPeg. Report this issue to the developer of this mod.");
            }
            linkerField = typeof(Cluster).GetField("Linker", BindingFlags.NonPublic | BindingFlags.Instance);
            if(linkerField == null)
            {
                throw new Exception("Could not find field 'Linker' in class Cluster. Report this issue to the developer of this mod.");
            }
        }

        //Only update the logic when the connection peg has some change. Else it is the data pegs, which do not matter for the linking:
        public override bool InputAtIndexShouldTriggerComponentLogicUpdates(int inputIndex)
        {
            return inputIndex == 0;
        }

        private Bundler getRemoteBundler()
        {
            HashSet<WireAddress> connectionWires = worldData.LookupPegWires(Inputs[0].Address);
            if (connectionWires == null)
            {
                //This is null on server start at least.
                return null;
            }
            foreach (WireAddress wireAddress in connectionWires)
            {
                Wire wire = worldData.Lookup(wireAddress);
                PegAddress remotePegAddress = wire.Point1 == Inputs[0].Address ? wire.Point2 : wire.Point1;
                LogicComponent logicComponent = circuitryManager.LookupComponent(remotePegAddress.ComponentAddress);
                if (logicComponent != null && logicComponent is Bundler bundler)
                {
                    return bundler; //Found the (first) bundler.
                }
            }
            return null;
        }

        protected override void DoLogicUpdate()
        {
            Bundler currentBundler = getRemoteBundler();
            if (currentBundler == remoteBundler)
            {
                //The bundler has not changed, all good.
                return;
            }
            //The bundler has changed! Update phasic links:
            
            //Unlink old bundler:
            unlinkBundler();
            remoteBundler = currentBundler; //Done unlinking, apply new bundler.

            //Link new bundler:
            linkBundler();
        }

        public override void OnComponentDestroyed()
        {
            remoteBundler.unregisterSplitter(this);
            remoteBundler = null;
        }

        public void bundlerDestroyed()
        {
            cleanupWhatLogicWorldDoesNotWantToCleanUp();
            remoteBundler = null;
        }

        private void linkBundler()
        {
            if (remoteBundler != null)
            {
                int maxLocal = base.Inputs.Count - 1; //Splitter
                int maxRemote = remoteBundler.Inputs.Count; //Bundler
                int maxLinks = maxRemote < maxLocal ? maxRemote : maxLocal;
                maxRemote -= 1;
                for (int i = 0; i < maxLinks; i++)
                {
                    remoteBundler.Inputs[maxRemote - i].AddOneWayPhasicLinkTo(base.Inputs[maxLocal - i]);
                }
                remoteBundler.registerSplitter(this);
            }
        }

        private void unlinkBundler()
        {
            if (remoteBundler != null)
            {
                int maxLocal = base.Inputs.Count - 1; //Splitter
                int maxRemote = remoteBundler.Inputs.Count; //Bundler
                int maxLinks = maxRemote < maxLocal ? maxRemote : maxLocal;
                maxRemote -= 1;
                for (int i = 0; i < maxLinks; i++)
                {
                    remoteBundler.Inputs[maxRemote - i].RemoveOneWayPhasicLinkTo(base.Inputs[maxLocal - i]);
                }
                remoteBundler.unregisterSplitter(this);
            }
        }

        private void cleanupWhatLogicWorldDoesNotWantToCleanUp()
        {
            //Or basically cause a linker uncertain propagation update of the output clusters.
            for(int i = 1; i < Inputs.Count; i++)
            {
                IInputPeg output = Inputs[i];
                Cluster cluster = (Cluster) clusterProperty.GetValue(output);
                if(cluster == null)
                {
                    continue;
                }
                ClusterLinker linker = (ClusterLinker) linkerField.GetValue(cluster);
                if(linker == null)
                {
                    continue;
                }
                linker.QueueUncertaintyPropagation();
            }
        }
    }
}
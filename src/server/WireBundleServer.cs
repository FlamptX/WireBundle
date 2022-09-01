using System.Collections.Generic;

using LogicWorld.Server.Circuitry;
using LogicAPI.Server.Components;
using LogicAPI.Server;
using LogicAPI.Data;
using LogicLog;
using LogicWorld.LogicCode;


using WireBundle.Components;

using LICC;

namespace WireBundle.Server
{
    class WireBundle : ServerMod
    {
        protected override void Initialize()
        {
            Logger.Info("WireBundle mod is ready to bundle up some wires!");
        }
    }

    public static class Bundlers
    {
        public static Dictionary<OutputAddress, Bundler> Components = new Dictionary<OutputAddress, Bundler>();
    }
}
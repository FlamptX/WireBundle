using System.Collections.Generic;

using LogicAPI.Server;
using LogicAPI.Data;
using LogicLog;

using WireBundle.Components;

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
        public static Dictionary<IComponentInWorld, Bundler> Components = new Dictionary<IComponentInWorld, Bundler>();
    }
}
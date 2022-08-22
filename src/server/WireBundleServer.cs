using LogicAPI.Server;

namespace WireBundle.Server
{
    class WireBundle : ServerMod
    {
        protected override void Initialize()
        {
            Logger.Info("WireBundle mod is ready to bundle up some wires!");
        }
    }
}
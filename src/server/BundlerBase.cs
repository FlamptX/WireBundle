using System;
using System.Linq;
using System.Collections.Generic;

using LogicWorld.Server.Circuitry;
using LogicAPI.Server.Components;

using WireBundle.Server;

namespace WireBundle.Components
{
    public class BundlerBase : LogicComponent
    {
        protected override void Initialize()
        {
            Bundlers.Components.Add(this.Component, this);
        }

        public override void OnComponentDestroyed()
        {
            Bundlers.Components.Remove(this.Component);
        }

        protected override void DoLogicUpdate()
        {
            bool active = false;
            for (int i = 0; i < base.Inputs.Count; i++)
            {
                if (base.Inputs[i].On) { active = true; }
            }
            base.Outputs[0].On = active;
        }
    }
}
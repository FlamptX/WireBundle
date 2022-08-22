using System.Collections.Generic;

using LogicAPI.Server.Components;

namespace WireBundle.Components
{
    public class Bundler : LogicComponent
    {
        private readonly List<Splitter> linkedSplitters = new List<Splitter>();

        public override void OnComponentDestroyed()
        {
            foreach (Splitter linkedSplitter in linkedSplitters)
            {
                linkedSplitter.bundlerDestroyed();
            }
            linkedSplitters.Clear();
        }

        protected override void DoLogicUpdate()
        {
            //TODO: This will unfortunately always be 1 tick delayed. Make it instant regardless. If technically even possible... Probably would need a hidden peg. Or make this output a peg.
            bool active = false;
            for (int i = 0; i < Inputs.Count; i++)
            {
                if (Inputs[i].On)
                {
                    active = true;
                }
            }
            Outputs[0].On = active;
        }

        public void unregisterSplitter(Splitter splitter)
        {
            linkedSplitters.Remove(splitter);
        }

        public void registerSplitter(Splitter splitter)
        {
            linkedSplitters.Add(splitter);
        }
    }
}
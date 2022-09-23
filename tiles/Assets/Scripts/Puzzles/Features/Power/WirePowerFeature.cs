using System.Collections;
using System.Collections.Generic;
using Tiles.Puzzles.Power;
using UnityEngine;

namespace Tiles.Puzzles.Features.Power
{
    public class WirePowerFeature : PowerFeature
    {
        [SerializeField]
        private PowerNode input, output;

        [SerializeField]
        private bool isDiode;

        public override IReadOnlyCollection<PowerNode> Inputs
        {
            get
            {
                PowerNode[] inputs;
                if (isDiode) inputs = new[] { input };
                else inputs = new[] { input, output };
                return inputs;
            }
        }

        public override IReadOnlyCollection<PowerNode> Outputs
        {
            get
            {
                PowerNode[] outputs;
                if (isDiode) outputs = new[] { output };
                else outputs = new[] { input, output };
                return outputs;
            }
        }

        protected internal override void OnTransmit(PowerNetwork.ITilePower power)
        {
        }
    }
}
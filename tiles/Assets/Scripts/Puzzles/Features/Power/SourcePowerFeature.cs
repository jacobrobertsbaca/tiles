using System.Collections;
using System.Collections.Generic;
using Tiles.Puzzles.Power;
using UnityEngine;

namespace Tiles.Puzzles.Features.Power
{
    public class SourcePowerFeature : PowerFeature
    {
        [SerializeField]
        private PowerNode output;

        public override IReadOnlyCollection<PowerNode> Inputs => new PowerNode[0];
        public override IReadOnlyCollection<PowerNode> Outputs => new[] { output };

        protected internal override void OnTransmit(PowerNetwork.ITilePower power)
        {
        }
    }
}
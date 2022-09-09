using System.Collections;
using System.Collections.Generic;
using Tiles.Puzzles.Features;
using UnityEngine;

namespace Tiles.Puzzles.Power
{
    public abstract class PowerFeature : TileFeature
    {
        public IEnumerable<PowerNode> Inputs { get; }
        public IEnumerable<PowerNode> Outputs { get; }
    }
}

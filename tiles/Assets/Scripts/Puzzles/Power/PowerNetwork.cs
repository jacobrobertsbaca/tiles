using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Tiles.Puzzles.Power
{
    public class PowerNetwork
    {
        public class TilePower
        {
            private PowerNetwork network;
            public TilePower(PowerNetwork network) => this.network = network;
        }

        private readonly Dictionary<PowerFeature, ISet<PowerFeature>> adjacencies = new();
        private readonly HashSet<PowerSourceFeature> sources = new();

        private readonly Dictionary<Vector2Int, PowerInfo> powerMap = new();
        private readonly Dictionary<Vector2Int, ISet<PowerFeature>> inputMap = new();
        private readonly Dictionary<Vector2Int, ISet<PowerFeature>> outputMap = new();

        public void Attach (PowerFeature feature)
        {
            foreach (var input in feature.Inputs)
            {
                var absolute = input.ToAbsolute(feature.Tile);
                if (!inputMap.ContainsKey(absolute)) inputMap[absolute] = new HashSet<PowerFeature>();
                inputMap[absolute].Add(feature);

                if (outputMap.TryGetValue(absolute, out var outputs))
                {
                    foreach (var output in outputs)
                    {
                        if (output == feature) continue;
                        adjacencies[output].Add(feature);
                    }
                }
            }

            if (!adjacencies.ContainsKey(feature)) adjacencies[feature] = new HashSet<PowerFeature>();

            foreach (var output in feature.Outputs)
            {
                var absolute = output.ToAbsolute(feature.Tile);
                if (!outputMap.ContainsKey(absolute)) outputMap[absolute] = new HashSet<PowerFeature>();
                outputMap[absolute].Add(feature);

                if (inputMap.TryGetValue(absolute, out var inputs))
                {
                    foreach (var input in inputs)
                    {
                        if (input == feature) continue;
                        adjacencies[feature].Add(input);
                    }
                }
            }

            if (feature is PowerSourceFeature source) sources.Add(source);
        }

        public void Detach(PowerFeature feature)
        {
            foreach (var input in feature.Inputs)
            {
                var absolute = input.ToAbsolute(feature.Tile);
                if (outputMap.TryGetValue(absolute, out var outputs))
                {
                    foreach (var output in outputs)
                    {
                        adjacencies[output].Remove(feature);
                    }
                }

                if (inputMap.TryGetValue(absolute, out var inputs))
                    inputs.Remove(feature);
            }
            
            foreach (var output in feature.Outputs)
            {
                var absolute = output.ToAbsolute(feature.Tile);
                if (outputMap.TryGetValue(absolute, out var outputs))
                    outputs.Remove(feature);
            }

            adjacencies.Remove(feature);
            if (feature is PowerSourceFeature source) sources.Remove(source);
        }
    }
}

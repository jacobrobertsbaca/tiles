using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tiles.Core;
using Tiles.Core.Events;
using Tiles.Puzzles.Features;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;

namespace Tiles.Puzzles.Power
{
    public class PowerNetwork : Actor
    {
        public interface IReadOnlyTilePower
        {
            public PowerInfo this[PowerNode node] { get; }
            public Tile Tile { get; }
        }

        public interface ITilePower : IReadOnlyTilePower
        {
            public new PowerInfo this[PowerNode node] { get; set; }
        }

        public class TilePower : ITilePower
        {
            internal IReadOnlyDictionary<PowerNode, PowerInfo> Changes => changes;
            public Tile Tile => feature.Tile;

            private readonly Dictionary<PowerNode, PowerInfo> changes = new();
            private readonly PowerNetwork network;
            private PowerFeature feature;

            public PowerInfo this[PowerNode node]
            {
                get
                {
                    if (changes.TryGetValue(node, out var power)) return power;
                    return network.GetPower(node.ToAbsolute(feature.Tile));
                }

                set
                {
                    if (value is null) value = PowerInfo.None;
                    changes[node] = value;
                }
            }

            internal TilePower(PowerNetwork network) => this.network = network;
            
            internal void SetFeature(PowerFeature feature)
            {
                changes.Clear();
                this.feature = feature;
            }
        }

        private enum TransmitState
        {
            None,
            One,
            All
        }

        private readonly Dictionary<Vector2Int, PowerInfo> powerMap = new();
        private readonly Dictionary<Vector2Int, PowerInfo> powerMapUpdates = new();
        private readonly Dictionary<Vector2Int, List<PowerFeature>> inputFeatures = new();
        private readonly List<PowerFeature> features = new();
        private readonly HashSet<PowerFeature> transmittedFeatures = new();
        private TilePower tilePower;
        private TransmitState state;

        protected override void OnAwake()
        {
            base.OnAwake();
            Game.Current.OnInitialized(this);
        }

        protected override bool OnInitialize()
        {
            tilePower = new TilePower(this);
            Subscribe(TileFeature.FeatureAdded, OnFeatureAdded);
            Subscribe(TileFeature.FeatureRemoved, OnFeatureRemoved);
            Subscribe(Tile.TileRotating, OnTileRotating);
            Subscribe(Tile.TileRotated, OnTileRotated);
            Subscribe(PowerFeature.NeedsTransmit, OnFeatureNeedsTransmit);
            return base.OnInitialize();
        }

        private void OnFeatureAdded(EventContext context, TileFeature feature)
        {
            if (feature is not PowerFeature pf) return;
            AddFeature(pf);
            TransmitOne(pf, true);
        }

        private void OnFeatureRemoved(EventContext context, TileFeature feature)
        {
            if (feature is not PowerFeature pf) return;
            RemoveFeature(pf);
            TransmitAll();
        }

        private void OnTileRotating(EventContext context, Tile tile)
        {
        }

        private void OnTileRotated(EventContext context, Tile tile)
        {
        }

        private void OnFeatureNeedsTransmit(EventContext context, PowerFeature feature)
        {
            TransmitOne(feature);
        }

        private void AddFeature(PowerFeature feature)
        {
            Assert.IsNotNull(feature);
            if (features.Contains(feature)) return;
            features.Add(feature);
            foreach (var input in feature.Inputs)
            {
                var absolute = input.ToAbsolute(feature.Tile);
                if (!inputFeatures.ContainsKey(absolute)) inputFeatures[absolute] = new();
                var featureList = inputFeatures[absolute];
                if (featureList.Contains(feature)) continue;
                featureList.Add(feature);
            }
        }

        private void RemoveFeature(PowerFeature feature)
        {
            Assert.IsNotNull(feature);
            if (!features.Contains(feature)) return;
            features.Remove(feature);
            foreach (var featureList in inputFeatures.Values)
                featureList.Remove(feature);
        }

        private PowerInfo GetPower(Vector2Int absolute)
        {
            if (powerMapUpdates.TryGetValue(absolute, out var updatedPower)) return updatedPower;
            if (state != TransmitState.All && powerMap.TryGetValue(absolute, out var power)) return power;
            return PowerInfo.None;
        }

        private void TransmitOne(PowerFeature feature, bool alwaysUpdate = false)
        {
            BeginTransmit(TransmitState.One);
            DoTransmit(feature);
            EndTransmit(alwaysUpdate ? feature : null);
        }

        private void TransmitAll()
        {
            BeginTransmit(TransmitState.All);
            foreach (var feature in features) DoTransmit(feature);
            EndTransmit();
        }

        // Called before a transmission cycle
        private void BeginTransmit(TransmitState state)
        {
            this.state = state;
            powerMapUpdates.Clear();
            transmittedFeatures.Clear();
        }

        // Called for each power element that needing an update during a transmission cycle
        private void DoTransmit(PowerFeature powerFeature)
        {
            Assert.IsFalse(state == TransmitState.None);
            Assert.IsNotNull(powerFeature);

            // TODO: Pool/cache Queue?
            Queue<PowerFeature> updates = new();
            updates.Enqueue(powerFeature);

            while (updates.Count > 0)
            {
                PowerFeature feature = updates.Dequeue();
                tilePower.SetFeature(feature);

                if (transmittedFeatures.Add(feature))
                    feature.OnBeforeTransmit(tilePower);
                feature.OnTransmit(tilePower);

                foreach (var (node, power) in tilePower.Changes)
                {
                    var absolute = node.ToAbsolute(feature.Tile);
                    if (!PowerInfo.StrictEquals.Equals(GetPower(absolute), power))
                    {
                        if (inputFeatures.TryGetValue(absolute, out var inputs))
                        {
                            foreach (var input in inputs)
                            {
                                if (input == feature) continue; // Don't let feature transmit to itself
                                updates.Enqueue(input);
                            }
                        }

                        powerMapUpdates[absolute] = power;
                    }
                }
            }
        }

        // Called at the end of a transmission cycle
        private void EndTransmit(PowerFeature alwaysUpdate = null)
        {
            // OnAfterTransmit must be invoked for every element that had
            // power transmitted to it
            foreach (var feature in transmittedFeatures)
            {
                tilePower.SetFeature(feature);
                feature.OnAfterTransmit(tilePower);
            }

            // TODO: Pool/cache List?
            List<Vector2Int> updatedNodes = new();

            // Mark for update all nodes which had a change in power state 
            foreach (var (absolute, power) in powerMapUpdates)
            {
                var existingPower = PowerInfo.None;
                if (powerMap.ContainsKey(absolute)) existingPower = powerMap[absolute];
                if (power != existingPower) updatedNodes.Add(absolute);
            }

            if (state == TransmitState.All)
            {
                // If transmitting through all nodes, then any nodes which originally had power
                // but no longer do are considered updated
                //
                // Example: Cut Wire
                // 
                // S---x---
                //
                // Key: S=source, -=wire, x=removed wire
                //
                // If we did not do this pass, then the wires to the right of the cut would not
                // get updated. These wires had power previously from the source S, but no longer would
                // after removing the wire, and therefore need an update.
                foreach (var absolute in powerMap.Keys)
                {
                    if (!powerMapUpdates.ContainsKey(absolute))
                    {
                        updatedNodes.Add(absolute);
                    }
                }

                // We clear the network when transmitting through all nodes, so as to avoid power
                // states from past transmissions lingering through the network
                powerMap.Clear();
            }

            // Synchronize changes with the network
            foreach (var (absolute, power) in powerMapUpdates)
            {
                powerMap[absolute] = power;
            }

            // We can re-use transmittedFeatures to keep track of which inputs have had their inputs updated
            // so we don't call OnInputsUpdated twice
            transmittedFeatures.Clear();

            foreach (var absolute in updatedNodes)
            {
                if (inputFeatures.TryGetValue(absolute, out var inputs))
                {
                    foreach (var input in inputs)
                    {
                        if (!transmittedFeatures.Add(input)) continue;
                        if (input == alwaysUpdate) alwaysUpdate = null;
                        tilePower.SetFeature(input);
                        input.OnInputsUpdated(tilePower);
                    }
                }
            }

            if (alwaysUpdate)
            {
                tilePower.SetFeature(alwaysUpdate);
                alwaysUpdate.OnInputsUpdated(tilePower);
            }

            state = TransmitState.None;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Unsubscribe(TileFeature.FeatureAdded);
            Unsubscribe(TileFeature.FeatureRemoved);
            Unsubscribe(PowerFeature.NeedsTransmit);
        }
    }
}

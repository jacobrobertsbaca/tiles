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
                    var absolute = node.ToAbsolute(Tile);
                    var absolute3D = new Vector3Int(absolute.x, absolute.y, network.GetRotationLayer(Tile));
                    return network.GetPower(absolute3D);
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

        private readonly Dictionary<Vector3Int, PowerInfo> powerMap = new();
        private readonly Dictionary<Vector3Int, PowerInfo> powerMapUpdates = new();
        private readonly Dictionary<Vector3Int, List<PowerFeature>> inputFeatures = new();
        private readonly List<PowerFeature> features = new();
        private readonly HashSet<PowerFeature> transmittedFeatures = new();
        private TilePower tilePower;
        private TransmitState state;

        private int rotationLayer = 0;
        private readonly Dictionary<Tile, int> rotationLayers = new();

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
            Subscribe(Puzzle.TilesSwapping, OnTilesSwapping);
            Subscribe(Puzzle.TilesSwapped, OnTilesSwapped);
            return base.OnInitialize();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Unsubscribe(TileFeature.FeatureAdded);
            Unsubscribe(TileFeature.FeatureRemoved);
            Unsubscribe(Tile.TileRotating);
            Unsubscribe(Tile.TileRotated);
            Unsubscribe(PowerFeature.NeedsTransmit);
            Unsubscribe(Puzzle.TilesSwapping);
            Unsubscribe(Puzzle.TilesSwapped);
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
            rotationLayers[tile] = ++rotationLayer;

            // Move the inputs of features on this tile onto a new layer so they are isolated from the rest of the network
            foreach (var pf in tile.GetFeatures<PowerFeature>())
            {
                // TODO: Somehow refactor this code to use Remove/Add/Attach methods?
                foreach (var input in pf.Inputs)
                {
                    // 1) Remove the old inputs
                    var absolute = input.ToAbsolute(tile);
                    var absolute3D = new Vector3Int(absolute.x, absolute.y, 0);
                    if (inputFeatures.TryGetValue(absolute3D, out var inputs)) inputs.Remove(pf);

                    // 2) Add the new inputs onto adjusted layer
                    absolute3D.z = rotationLayer;
                    if (!inputFeatures.ContainsKey(absolute3D)) inputFeatures[absolute3D] = new();
                    inputs = inputFeatures[absolute3D];
                    if (!inputs.Contains(pf)) inputs.Add(pf);
                }
            }

            TransmitAll();
        }

        private void OnTileRotated(EventContext context, Tile tile)
        {
            // Move the inputs of features on this tile from the adjusted layer to the main layer so they are part of the network again
            var layer = rotationLayers[tile];
            rotationLayers[tile] = 0;
            Assert.IsTrue(layer > 0);

            // Clear all inputs on the tile's rotation layer, and then re-attach the tile to the main layer
            foreach (var kv in inputFeatures.Where(kv => kv.Key.z == layer).ToList()) inputFeatures.Remove(kv.Key);
            foreach (var pf in tile.GetFeatures<PowerFeature>()) AttachFeature(pf);

            TransmitAll();
        }

        private void OnFeatureNeedsTransmit(EventContext context, PowerFeature feature)
        {
            TransmitOne(feature);
        }

        private void OnTilesSwapping(EventContext context, (Tile Swapper, Tile Swappee) data)
        {
            // Remove all features on both tiles
            foreach (var pf in data.Swapper.GetFeatures<PowerFeature>()) RemoveFeature(pf);
            foreach (var pf in data.Swappee.GetFeatures<PowerFeature>()) RemoveFeature(pf);
            TransmitAll();
        }

        private void OnTilesSwapped(EventContext context, (Tile Swapper, Tile Swappee) data)
        {
            // Add all features on both tiles
            foreach (var pf in data.Swapper.GetFeatures<PowerFeature>()) AddFeature(pf);
            foreach (var pf in data.Swappee.GetFeatures<PowerFeature>()) AddFeature(pf);
            TransmitAll();
        }

        private void AddFeature(PowerFeature feature)
        {
            Assert.IsNotNull(feature);
            if (features.Contains(feature)) return;
            features.Add(feature);
            AttachFeature(feature);
        }

        private void AttachFeature(PowerFeature feature)
        {
            Assert.IsNotNull(feature);
            Assert.IsTrue(features.Contains(feature));
            foreach (var input in feature.Inputs)
            {
                var absolute = input.ToAbsolute(feature.Tile);
                var absolute3D = new Vector3Int(absolute.x, absolute.y, GetRotationLayer(feature.Tile));
                if (!inputFeatures.ContainsKey(absolute3D)) inputFeatures[absolute3D] = new();
                var featureList = inputFeatures[absolute3D];
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

        private PowerInfo GetPower(Vector3Int absolute)
        {
            if (powerMapUpdates.TryGetValue(absolute, out var updatedPower)) return updatedPower;
            if (state != TransmitState.All && powerMap.TryGetValue(absolute, out var power)) return power;
            return PowerInfo.None;
        }

        private int GetRotationLayer(Tile tile)
        {
            if (rotationLayers.ContainsKey(tile)) return rotationLayers[tile];
            return 0;
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
                    var absolute3D = new Vector3Int(absolute.x, absolute.y, GetRotationLayer(feature.Tile));
                    var currentPower = GetPower(absolute3D);

                    if (!PowerInfo.StrictEquals.Equals(currentPower, power))
                    {
                        if (inputFeatures.TryGetValue(absolute3D, out var inputs))
                        {
                            foreach (var input in inputs)
                            {
                                if (input == feature) continue; // Don't let feature transmit to itself
                                updates.Enqueue(input);
                            }
                        }

                        powerMapUpdates[absolute3D] = currentPower.Combine(power);
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
            List<Vector3Int> updatedNodes = new();

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
                foreach (var absolute in inputFeatures.Keys)
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
    }
}

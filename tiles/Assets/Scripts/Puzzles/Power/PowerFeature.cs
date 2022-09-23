using System;
using System.Collections;
using System.Collections.Generic;
using Tiles.Core.Events;
using Tiles.Puzzles.Features;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tiles.Puzzles.Power
{
    public abstract class PowerFeature : TileFeature
    {
        public static readonly Event<PowerFeature> NeedsTransmit = new($"{nameof(PowerFeature)}::{nameof(NeedsTransmit)}");

        public abstract IReadOnlyCollection<PowerNode> Inputs { get; }

        public abstract IReadOnlyCollection<PowerNode> Outputs { get; }

        /// <summary>
        /// Called immediately before <see cref="OnTransmit(PowerNetwork.ITilePower)"/> is called
        /// for the first time during a transmission cycle on this <see cref="PowerFeature"/>.
        /// </summary>
        /// <param name="power">Allows reading power on the current <see cref="Tile"/></param>
        protected internal virtual void OnBeforeTransmit(PowerNetwork.IReadOnlyTilePower power) {}

        /// <summary>
        /// Called to transmit power from the inputs of this <see cref="PowerFeature"/> to its outputs.
        /// May be called multipe times during a transmission cycle.
        /// </summary>
        /// <param name="power">Allows reading and writing power on the current <see cref="Tile"/></param>
        protected internal abstract void OnTransmit(PowerNetwork.ITilePower power);

        /// <summary>
        /// Called immediately after a transmission cycle has finished. When this method is called,
        /// <see cref="OnTransmit(PowerNetwork.ITilePower)"/> will have been called at least once.
        /// </summary>
        /// <param name="power">Allows reading power on the current <see cref="Tile"/></param>
        protected internal virtual void OnAfterTransmit(PowerNetwork.IReadOnlyTilePower power) {}

        /// <summary>
        /// Called when this <see cref="PowerFeature"/> is added to the power network or when the power supplied to
        /// <see cref="Inputs"/> has changed at the end of a transmission cycle.
        /// </summary>
        /// <param name="power">Allows reading power on the current <see cref="Tile"/></param>
        protected internal virtual void OnInputsUpdated(PowerNetwork.IReadOnlyTilePower power) {}

#if UNITY_EDITOR
        private const float nodeRadius = 0.1f;
        private static readonly float rootTwoInverse = 1 / Mathf.Sqrt(2);
#endif

        protected virtual void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (!Tile || !Tile.Puzzle) return;
            Vector3 up = Tile.Puzzle.transform.up;

            // Draw circles at inputs
            foreach (var input in Inputs)
            {
                var position = input.ToAbsoluteWorld(Tile);
                Handles.DrawWireDisc(position, up, nodeRadius);

                // Double up input circles if no outputs
                if (Outputs.Count == 0)
                {
                    Handles.DrawWireDisc(position, up, 0.75f * nodeRadius);
                }
            }

            // Draw circles at ouputs if no inputs
            if (Inputs.Count == 0)
            {
                Vector3 forward = rootTwoInverse * (Tile.Puzzle.transform.forward + Tile.Puzzle.transform.right);
                Vector3 right = rootTwoInverse * (Tile.Puzzle.transform.forward - Tile.Puzzle.transform.right);

                foreach (var output in Outputs)
                {
                    var position = output.ToAbsoluteWorld(Tile);
                    Handles.DrawWireDisc(position, up, nodeRadius);
                    Handles.DrawWireDisc(position, up, 0.75f * nodeRadius);
                    Handles.DrawLine(position + forward * nodeRadius, position - forward * nodeRadius);
                    Handles.DrawLine(position + right * nodeRadius, position - right * nodeRadius);
                }
            }

            // Draw arrows
            if (Inputs.Count == 0 || Outputs.Count == 0) return;

            // If only one input and one output, draw from input to ouput
            if (Inputs.Count == 1 && Outputs.Count == 1)
            {
                DrawArrow(Inputs.First().ToAbsoluteWorld(Tile),
                    Outputs.First().ToAbsoluteWorld(Tile),
                    up, nodeRadius, nodeRadius);
            }
            else
            {
                // Draw lines from inputs to centroid of inputs/outputs
                // and then arrows from centroid to outputs
                Vector3 centroid = Vector3.zero;
                var count = 0;
                foreach (var node in Inputs.Concat(Outputs).Distinct())
                {
                    centroid += node.ToAbsoluteWorld(Tile);
                    count++;
                }

                centroid /= count;

                foreach (var input in Inputs)
                {
                    var origin = input.ToAbsoluteWorld(Tile);
                    var dir = (centroid - origin).normalized;
                    origin += dir * nodeRadius;
                    Handles.DrawLine(origin, centroid);
                }

                foreach (var output in Outputs)
                {
                    DrawArrow(centroid, output.ToAbsoluteWorld(Tile), up, 0, nodeRadius);
                }
            }
#endif
        }

        private static void DrawArrow(
            Vector3 from,
            Vector3 to,
            Vector3 normal,
            float marginFrom = 0f,
            float marginTo = 0f,
            float tipLength = 0.05f)
        {
            Vector3 dir = (to - from).normalized;
            from += dir * marginFrom;
            to -= dir * marginTo;
            Vector3 cross = Vector3.Cross(dir, normal).normalized;
            Vector3 t1 = to - tipLength * (dir + cross).normalized;
            Vector3 t2 = to - tipLength * (dir - cross).normalized;
            Handles.DrawLine(from, to);
            Handles.DrawLine(to, t1);
            Handles.DrawLine(to, t2);
        }
    }
}

using System;
using UnityEngine;

namespace Tiles.Puzzles.Power
{
    [Serializable]
    public struct PowerNode : IEquatable<PowerNode>
    {
        // The number of nodes per cell side. Change at your peril
        private const int kGridSize = 5;
        private const float kInverseGridSize = 1f / kGridSize;

        private int nodeIndex;

        public int X => nodeIndex % kGridSize;
        public int Y => nodeIndex / kGridSize;

        public Vector2 Offset => GetOffset();
        public Vector2 CenterOffset => Offset - 0.5f * Vector2.one;
        public bool OnEdge => X == 0 || X == kGridSize - 1 || Y == 0 || Y == kGridSize - 1;

        public PowerNode(int x, int y)
        {
            if (x < 0 || x >= kGridSize ||
                y < 0 || y >= kGridSize)
                throw new ArgumentOutOfRangeException($"{nameof(x)} and {nameof(y)} must be within [0, {kGridSize})");
            nodeIndex = y * kGridSize + x;
        }

        /// <summary>
        /// Converts this <see cref="PowerNode"/> to one that is absolutely positioned within the grid space and accounts for tile rotation
        /// </summary>
        /// <param name="tile">The tile that this <see cref="PowerNode"/> belongs to</param>
        /// <returns>An absolute position for this <see cref="PowerNode"/></returns>
        internal Vector2Int ToAbsolute(Tile tile)
        {
#pragma warning disable CS0162 // Unreachable code detected
            if (kGridSize <= 1) return tile.Index;
#pragma warning restore CS0162 // Unreachable code detected
            // TODO: Take into account tile rotation
            return (kGridSize - 1) * tile.Index + new Vector2Int(X, Y);
        }

        private Vector2 GetOffset ()
        {
#pragma warning disable CS0162 // Unreachable code detected
            if (kGridSize <= 1) return 0.5f * Vector2.one;
#pragma warning restore CS0162 // Unreachable code detected
            return new Vector2(kInverseGridSize * X, kInverseGridSize * Y);
        }

        public bool Equals(PowerNode other) => nodeIndex == other.nodeIndex;
        public override bool Equals(object o) => o is PowerNode node && Equals(node);
        public override int GetHashCode() => nodeIndex.GetHashCode();
        public static bool operator ==(PowerNode left, PowerNode right) => left.Equals(right);
        public static bool operator !=(PowerNode left, PowerNode right) => !(left == right);
        public override string ToString() => $"{nameof(PowerNode)}({X}, {Y})";
    }
}

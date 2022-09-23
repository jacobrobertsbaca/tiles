using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Tiles.Puzzles.Power
{
    [Serializable]
    public struct PowerNode : IEquatable<PowerNode>
    {
        // The number of nodes per cell side. Change at your peril
        private static readonly int kGridSize = 5;
        private static readonly float kInverseGridSize = 1f / (kGridSize - 1);

        private static PowerNode[] allNodes;
        public static IReadOnlyList<PowerNode> AllNodes
        {
            get
            {
                if (allNodes is not null) return allNodes;
                allNodes = new PowerNode[kGridSize * kGridSize];
                for (int i = 0; i < allNodes.Length; i++)
                    allNodes[i] = new PowerNode(i);
                return allNodes;
            }
        }

        public static int GridSize => kGridSize;

        [SerializeField]
        private int nodeIndex;

        public int X => nodeIndex % kGridSize;
        public int Y => nodeIndex / kGridSize;

        public Vector2 Offset => GetOffset();
        public Vector2 CenterOffset => Offset - 0.5f * Vector2.one;
        public bool OnEdge => X == 0 || X == kGridSize - 1 || Y == 0 || Y == kGridSize - 1;

        public PowerNode(int index)
        {
            Assert.IsFalse(index < 0 || index >= kGridSize * kGridSize,
                $"{nameof(index)} must be within [0, {kGridSize * kGridSize - 1}]");
            nodeIndex = index;
        }

        public PowerNode(int x, int y)
        {
            Assert.IsFalse(x < 0 || x >= kGridSize || y < 0 || y >= kGridSize,
                $"{nameof(x)} and {nameof(y)} must be within [0, {kGridSize - 1}]");
            nodeIndex = y * kGridSize + x;
        }

        /// <summary>
        /// Converts this <see cref="PowerNode"/> to one that is absolutely positioned within the grid space and accounts for tile rotation
        /// </summary>
        /// <param name="tile">The tile that this <see cref="PowerNode"/> belongs to</param>
        /// <returns>An absolute position for this <see cref="PowerNode"/></returns>
        internal Vector2Int ToAbsolute(Tile tile)
        {
            Assert.IsNotNull(tile);
            if (kGridSize <= 1) return tile.Index;
            // TODO: Take into account tile rotation
            return (kGridSize - 1) * tile.Index + new Vector2Int(X, Y);
        }

        internal Vector3 ToAbsoluteWorld(Tile tile)
        {
            Assert.IsNotNull(tile.Puzzle);
            if (kGridSize <= 1) return tile.Puzzle.GridToWorld(tile.Index);
            Vector3 forward = tile.Puzzle.transform.forward;
            Vector3 right = tile.Puzzle.transform.right;
            Vector3 absoluteOrigin = tile.Puzzle.transform.position - 0.5f * tile.Puzzle.TileSize * (forward + right);
            Vector2Int absolute = ToAbsolute(tile);
            return absoluteOrigin + kInverseGridSize * absolute.x * right + kInverseGridSize * absolute.y * forward;
        }

        private Vector2 GetOffset ()
        {
            if (kGridSize <= 1) return 0.5f * Vector2.one;
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

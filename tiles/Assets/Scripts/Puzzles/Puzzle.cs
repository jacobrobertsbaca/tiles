using System;
using System.Collections.Generic;
using Tiles.Core.Events;
using UnityEngine;

namespace Tiles.Puzzles
{
    public class Puzzle : Actor
    {
        [SerializeField] private float tileSize = 1;
        public float TileSize => tileSize;

        private readonly Dictionary<Vector2Int, Tile> tiles = new Dictionary<Vector2Int, Tile>(); 

        protected override bool OnInitialize()
        {
            Subscribe(Tile.TileAdded, OnTileAdded);
            Subscribe(Tile.TileRemoved, OnTileRemoved);
            return true;
        }

        private void OnTileAdded(EventContext context, Tile tile)
        {
            if (tiles.ContainsKey(tile.index))
            {
                Debug.LogWarning($"Duplicate tile at index {tile.index} will be deleted.");
                Destroy(tile.gameObject);
                return;
            }

            Debug.Log($"Added tile at {tile.Index}");
            tiles[tile.Index] = tile;
        }

        private void OnTileRemoved(EventContext context, Tile tile)
        {
            tiles.Remove(tile.Index);
        }

        protected override void Destroy()
        {
            Unsubscribe(Tile.TileAdded);
            Unsubscribe(Tile.TileRemoved);
        }

        /// <summary>
        /// Gets the closest grid index to a given world point.
        /// </summary>
        /// <param name="worldPosition">A position in world space</param>
        /// <returns>The grid index closest to <paramref name="worldPosition"/></returns>
        public Vector2Int WorldToGrid(Vector3 worldPosition)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            Vector2 xzNorm = new Vector2(local.x, local.z) / tileSize;
            return new Vector2Int(Mathf.RoundToInt(xzNorm.x), Mathf.RoundToInt(xzNorm.y));
        }

        /// <summary>
        /// Returns the world position of a point in grid coordinates.
        /// </summary>
        /// <param name="gridIndex">The index of the point in grid coordinates</param>
        /// <param name="height">The height of the resulting world position above or below the grid</param>
        /// <returns>The world position at <paramref name="gridIndex"/></returns>
        public Vector3 GridToWorld(Vector2Int gridIndex, float height = 0)
        {
            return transform.TransformPoint(new Vector3(gridIndex.x * TileSize, height, gridIndex.y * TileSize));
        }

        /// <summary>
        /// Aligns a <see cref="Tile"/> to conform with its <see cref="Tile.Index"/>
        /// </summary>
        /// <remarks>
        /// The tile's height above or below the grid will be preserved.
        /// </remarks>
        public void AlignToGrid(Tile tile)
        {
            var puzzleLocal = transform.InverseTransformPoint(transform.position);
            tile.transform.position = transform.TransformPoint(new Vector3(
                tile.index.x * TileSize,
                puzzleLocal.y,
                tile.index.y * TileSize));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (tileSize <= 0.01f) tileSize = 0.01f;
        }
#endif
    }
}
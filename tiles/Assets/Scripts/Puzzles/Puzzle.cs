using UnityEngine;

namespace Tiles.Puzzles
{
    public class Puzzle : Actor
    {
        [SerializeField] private float tileSize = 1;
        public float TileSize => tileSize;

        protected override bool OnInitialize()
        {
            Debug.Log("Initialized Puzzle");
            return true;
        }

        internal void AddTile(Tile tile)
        {

        }

        /// <summary>
        /// Gets the closest grid index to a given point.
        /// </summary>
        /// <param name="worldPosition">A position in world space</param>
        /// <returns>The grid index closest to <paramref name="worldPosition"/></returns>
        public Vector2Int GridIndex(Vector3 worldPosition)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            Vector2 xzNorm = new Vector2(local.x, local.z) / tileSize;
            return new Vector2Int(Mathf.RoundToInt(xzNorm.x), Mathf.RoundToInt(xzNorm.y));
        }
    }
}
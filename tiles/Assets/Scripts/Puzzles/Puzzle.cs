using Sirenix.OdinInspector;
using System.Collections.Generic;
using Tiles.Core;
using Tiles.Core.Events;
using Tiles.Puzzles.Power;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Tiles.Puzzles
{
    public class Puzzle : Actor
    {
        /// <summary>
        /// The tile that the player is currently hovering over, or <c>null</c> if no tile is currently being hovered.
        /// </summary>
        public Tile HoveredTile { get; private set; }

        [SerializeField] private float tileSize = 1;
        public float TileSize => tileSize;

        [FoldoutGroup("References")]
        [SerializeField] private DecalProjector tileSelectedProjectorPrefab;
        private DecalProjector tileSelectedProjector;

        private readonly Dictionary<Vector2Int, Tile> tiles = new Dictionary<Vector2Int, Tile>();

        protected override void OnAwake()
        {
            base.OnAwake();
            Game.Current.OnInitialized(this);
        }

        protected override bool OnInitialize()
        {
            // Add power network
            if (!GetComponent<PowerNetwork>())
                gameObject.AddComponent<PowerNetwork>();

            // Add selected decal projector
            tileSelectedProjector = SetupProjector();
            HideProjector();

            Subscribe(Tile.TileAdded, OnTileAdded);
            Subscribe(Tile.TileRemoved, OnTileRemoved);
            return true;
        }

        private void OnTileAdded(EventContext context, Tile tile)
        {
            if (tiles.ContainsKey(tile.Index))
            {
                Debug.LogWarning($"Duplicate tile at index {tile.Index} will be deleted.");
                Destroy(tile.gameObject);
                return;
            }

            Debug.Log($"Added tile at {tile.Index}");
            tiles[tile.Index] = tile;
        }

        private void Update()
        {
            HoveredTile = GetHoveredTile();
            if (HoveredTile is null) HideProjector();
            else ShowProjector(HoveredTile);

            if (HoveredTile is not null)
            {
                if (Input.GetMouseButtonUp(0)) HoveredTile.RotateLeft();
                else if (Input.GetMouseButtonUp(1)) HoveredTile.RotateRight();
            }
        }

        private void OnTileRemoved(EventContext context, Tile tile)
        {
            tiles.Remove(tile.Index);
        }

        protected override void OnDestroy()
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

        private DecalProjector SetupProjector()
        {
            var projector = Instantiate(tileSelectedProjectorPrefab.gameObject, transform).GetComponent<DecalProjector>();
            projector.size = new Vector3(TileSize, TileSize, projector.size.z);
            projector.pivot = Vector3.zero;
            projector.scaleMode = DecalScaleMode.InheritFromHierarchy;
            projector.transform.localRotation = Quaternion.Euler(90, 0, 0);
            return projector;
        }

        private void HideProjector()
        {
            tileSelectedProjector.gameObject.SetActive(false);
        }

        private void ShowProjector(Tile tile)
        {
            tileSelectedProjector.gameObject.SetActive(true);
            tileSelectedProjector.transform.localPosition = tile.transform.localPosition;
        }

        private Tile GetHoveredTile()
        {
            Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            Tile hoveredTile = null;
            float bestDistance = Mathf.Infinity;

            // Compute directions of puzzle plane in order to compute tile corners
            Vector3 dir = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(transform.up, dir)) > 0.99) dir = Vector3.forward;
            Vector3 v1 = 0.5f * TileSize * Vector3.Cross(transform.up, dir).normalized;
            Vector3 v2 = 0.5f * TileSize * Vector3.Cross(transform.up, v1).normalized;

            foreach (var tile in tiles.Values)
            {
                Vector3 p0 = tile.transform.position + v1 + v2;
                Vector3 p1 = tile.transform.position + v1 - v2;
                Vector3 p2 = tile.transform.position - v1 - v2;
                Vector3 p3 = tile.transform.position - v1 + v2;

                float distance;
                if (IntersectRayTriangle(cameraRay, p0, p1, p2, out float d1)) distance = d1;
                else if (IntersectRayTriangle(cameraRay, p2, p3, p0, out float d2)) distance = d2;
                else continue;

                if (distance > 0 && distance < bestDistance)
                {
                    hoveredTile = tile;
                    bestDistance = distance;
                }
            }

            return hoveredTile;
        }

        //
        // Transcribed from the C++ code at
        // https://en.m.wikipedia.org/wiki/M%C3%B6ller%E2%80%93Trumbore_intersection_algorithm
        //
        private static bool IntersectRayTriangle(Ray ray,
                           Vector3 p0, Vector3 p1, Vector3 p2,
                           out float distance)
        {
            const float EPSILON = 0.0000001f;

            distance = 0f;
            Vector3 edge1, edge2, h, s, q;
            float a, f, u, v;
            edge1 = p1 - p0;
            edge2 = p2 - p0;
            h = Vector3.Cross(ray.direction, edge2);
            a = Vector3.Dot(edge1, h);
            if (a > -EPSILON && a < EPSILON) return false;    // This ray is parallel to this triangle.
            f = 1.0f / a;
            s = ray.origin - p0;
            u = f * Vector3.Dot(s, h);
            if (u < 0.0 || u > 1.0) return false;
            q = Vector3.Cross(s, edge1);
            v = f * Vector3.Dot(ray.direction, q);
            if (v < 0.0 || u + v > 1.0) return false;

            // At this stage we can compute t to find out where the intersection point is on the line.
            float t = f * Vector3.Dot(edge2, q);
            if (t > EPSILON) // ray intersection
            {
                distance = t;
                return true;
            }
            return false; // This means that there is a line intersection but not a ray intersection.
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (tileSize <= 0.01f) tileSize = 0.01f;
        }
#endif
    }
}
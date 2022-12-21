using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Tiles.Puzzles.Editor
{
    [CustomEditor(typeof(Puzzle))]
    public class PuzzleEditor : OdinEditor
    {
        public static readonly Color gridLineColor = new Color(1, 1, 1, 0.3f);
        const int gridMargin = 1;

        private Puzzle puzzle;
        private Tile[] tiles;

        protected override void OnEnable()
        {
            puzzle = target as Puzzle;
            tiles = puzzle.GetComponentsInChildren<Tile>();
        }

        private (Vector2Int, Vector2Int) GetGridExtents()
        {
            Vector2Int min = Vector2Int.zero;
            Vector2Int max = Vector2Int.zero;

            foreach (var tile in tiles)
            {
                var index = puzzle.WorldToGrid(tile.transform.position);
                if (index.x <= min.x) min.x = index.x - gridMargin;
                if (index.y <= min.y) min.y = index.y - gridMargin;
                if (index.x >= max.x) max.x = index.x + gridMargin;
                if (index.y >= max.y) max.y = index.y + gridMargin;
            }

            return (min, max);
        }

        private void DrawGrid(Vector2Int minCell, Vector2Int maxCell)
        {
            float minZ = puzzle.TileSize * (minCell.y - 0.5f);
            float maxZ = puzzle.TileSize * (maxCell.y + 0.5f);
            for (int xi = minCell.x + 1; xi <= maxCell.x; xi++)
            {
                float x = puzzle.TileSize * (xi - 0.5f);
                Vector3 startLocal = new Vector3(x, 0, minZ);
                Vector3 endLocal = new Vector3(x, 0, maxZ);
                Handles.DrawLine(puzzle.transform.TransformPoint(startLocal), puzzle.transform.TransformPoint(endLocal));
            }

            float minX = puzzle.TileSize * (minCell.x - 0.5f);
            float maxX = puzzle.TileSize * (maxCell.x + 0.5f);
            for (int zi = minCell.y + 1; zi <= maxCell.y; zi++)
            {
                float z = puzzle.TileSize * (zi - 0.5f);
                Vector3 startLocal = new Vector3(minX, 0, z);
                Vector3 endLocal = new Vector3(maxX, 0, z);
                Handles.DrawLine(puzzle.transform.TransformPoint(startLocal), puzzle.transform.TransformPoint(endLocal));
            }
        }

        private void OnSceneGUI()
        {
            Handles.color = gridLineColor;
            var (minCell, maxCell) = GetGridExtents();
            DrawGrid(minCell, maxCell);
        }
    }
}
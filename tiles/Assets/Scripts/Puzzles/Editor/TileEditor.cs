using UnityEditor;
using UnityEngine;

namespace Tiles.Puzzles.Editor
{
    [CustomEditor(typeof(Tile))]
    public class TileEditor : UnityEditor.Editor
    {
        private static class Styles
        {
            public static readonly GUIContent AlignNearest = new GUIContent("Align to Nearest", "Aligns this tile to the grid location which it is nearest to");
            public static readonly GUIContent AlignIndex = new GUIContent("Align to Index", "Aligns this tile to its current grid location");
        }

        private Tile tile;

        private void OnEnable()
        {
            tile = target as Tile;
        }

        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledScope(!tile.Puzzle))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(Styles.AlignNearest, EditorStyles.miniButtonLeft))
                    {
                        var gridIndex = tile.Puzzle.WorldToGrid(tile.transform.position);
                        tile.index = gridIndex;
                        tile.Puzzle.AlignToGrid(tile);
                        EditorUtility.SetDirty(tile);
                    }

                    if (GUILayout.Button(Styles.AlignIndex, EditorStyles.miniButtonRight))
                    {
                        tile.Puzzle.AlignToGrid(tile);
                        EditorUtility.SetDirty(tile);
                    }
                }
            }

            DrawDefaultInspector();
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy)]
        private static void OnDrawGizmo(Tile tile, GizmoType type)
        {
            if (!tile.Puzzle) return;

            float height = tile.Puzzle.transform.InverseTransformPoint(tile.transform.position).y;
            Vector3 minCorner = new Vector3(
                tile.Puzzle.TileSize * (tile.index.x - 0.5f), 
                height,
                tile.Puzzle.TileSize * (tile.index.y - 0.5f));
            Vector3 maxCorner = new Vector3(
                tile.Puzzle.TileSize * (tile.index.x + 0.5f),
                height,
                tile.Puzzle.TileSize * (tile.index.y + 0.5f));

            Vector3 ll = tile.Puzzle.transform.TransformPoint(minCorner);
            Vector3 ul = tile.Puzzle.transform.TransformPoint(new Vector3(minCorner.x, height, maxCorner.z));
            Vector3 ur = tile.Puzzle.transform.TransformPoint(maxCorner);
            Vector3 lr = tile.Puzzle.transform.TransformPoint(new Vector3(maxCorner.x, height, minCorner.z));

            if (!Mathf.Approximately(height, 0))
            {
                Vector3 llBase = tile.Puzzle.transform.TransformPoint(new Vector3(minCorner.x, 0, minCorner.z));
                Vector3 ulBase = tile.Puzzle.transform.TransformPoint(new Vector3(minCorner.x, 0, maxCorner.z));
                Vector3 urBase = tile.Puzzle.transform.TransformPoint(new Vector3(maxCorner.x, 0, maxCorner.z));
                Vector3 lrBase = tile.Puzzle.transform.TransformPoint(new Vector3(maxCorner.x, 0, minCorner.z));

                Handles.DrawLine(llBase, ll);
                Handles.DrawLine(ulBase, ul);
                Handles.DrawLine(urBase, ur);
                Handles.DrawLine(lrBase, lr);
            }

            Handles.color = Color.white;
            Handles.DrawLine(ll, ul);
            Handles.DrawLine(ul, ur);
            Handles.DrawLine(ur, lr);
            Handles.DrawLine(lr, ll);

            Handles.color = new Color(1, 1, 1, 0.2f);
            Handles.DrawAAConvexPolygon(ll, ul, ur, lr);
        }
    }
}

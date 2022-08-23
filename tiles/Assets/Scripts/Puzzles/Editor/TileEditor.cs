using UnityEditor;
using UnityEngine;

namespace Tiles.Puzzles.Editor
{
    [CustomEditor(typeof(Tile))]
    public class TileEditor : UnityEditor.Editor
    {
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
                    if (GUILayout.Button("Align to Nearest", EditorStyles.miniButtonLeft))
                    {
                        var gridIndex = tile.Puzzle.GridIndex(tile.transform.position);
                        tile.gridIndex = gridIndex;
                        tile.AlignToGrid();
                        EditorUtility.SetDirty(tile);
                    }

                    if (GUILayout.Button("Align To Index", EditorStyles.miniButtonRight))
                    {
                        tile.AlignToGrid();
                        EditorUtility.SetDirty(tile);
                    }
                }
            }

            DrawDefaultInspector();
        }
    }
}

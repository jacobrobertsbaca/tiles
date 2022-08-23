using UnityEditor;
using UnityEngine;

namespace Tiles.Puzzles.Editor
{
    [CustomEditor(typeof(Puzzle))]
    public class PuzzleEditor : UnityEditor.Editor
    {
        const int kGridMargin = 1;

        private Puzzle puzzle;
        private Tile[] tiles;

        private void OnEnable()
        {
            puzzle = target as Puzzle;
            tiles = puzzle.GetComponentsInChildren<Tile>();
        }

        private (Vector2Int, Vector2Int) GetGridExtents()
        {
            Vector2Int min = Vector2Int.zero;
            Vector2Int max = Vector2Int.zero;
            return (min, max);
        }

        private void OnSceneGUI()
        {
            
        }
    }
}
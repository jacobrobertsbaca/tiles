using UnityEngine;

namespace Tiles.Puzzles
{
    public class Tile : Actor
    {
        [SerializeField] internal Vector2Int gridIndex;
        public Vector2Int GridIndex => gridIndex;

        private Puzzle puzzle;
        public Puzzle Puzzle
        {
            get
            {
                if (!puzzle) puzzle = GetComponentInParent<Puzzle>();
                return puzzle;
            }
        }

        protected override void OnAwake()
        {
            if (!Puzzle)
            {
                Debug.LogError($"{nameof(Tile)} without parent {nameof(Puzzles.Puzzle)} found. This {nameof(Tile)} will be destroyed.");
                Destroy(gameObject);
                return;
            }

            Puzzle.OnInitialized(this);
        }

        protected override bool OnInitialize()
        {
            Debug.Log("Initialized Tile");
            Puzzle.AddTile(this);
            return true;
        }

        /// <summary>
        /// Aligns this <see cref="Tile"/> to conform with its <see cref="GridIndex"/>
        /// </summary>
        /// <remarks>
        /// The tile's height above or below the grid will be preserved.
        /// </remarks>
        public void AlignToGrid()
        {
            var puzzleLocal = Puzzle.transform.InverseTransformPoint(transform.position);
            transform.position = Puzzle.transform.TransformPoint(new Vector3(
                gridIndex.x * Puzzle.TileSize,
                puzzleLocal.y,
                gridIndex.y * Puzzle.TileSize));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            AlignToGrid();
        }
#endif
    }
}

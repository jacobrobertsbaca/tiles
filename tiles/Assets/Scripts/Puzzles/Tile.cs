using System;
using System.Collections.Generic;
using Tiles.Core;
using Tiles.Core.Events;
using Tiles.Puzzles.Features;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tiles.Puzzles
{
    public class Tile : Actor
    {
        public enum TileRotation
        {
            North,
            East,
            West,
            South,
        }

        public static readonly Event<Tile> TileAdded = new($"{nameof(Tile)}::{nameof(TileAdded)}");
        public static readonly Event<Tile> TileRemoved = new($"{nameof(Tile)}::{nameof(TileRemoved)}");

        /// <summary>
        /// Called when a tile starts to rotate, after <see cref="Rotating"/> becomes <c>true</c> but before <see cref="Rotation"/> has been updated.
        /// </summary>
        public static readonly Event<Tile> TileRotating = new($"{nameof(Tile)}::{nameof(TileRotating)}");

        /// <summary>
        /// Called when a tile finishes rotating, after <see cref="Rotating"/> becomes <c>false</c> and <see cref="Rotation"/> has been updated.
        /// </summary>
        public static readonly Event<Tile> TileRotated = new($"{nameof(Tile)}::{nameof(TileRotated)}");

        [DisableInPlayMode]
        [SerializeField] internal Vector2Int index;
        public Vector2Int Index => index;

        [DisableInPlayMode]
        [SerializeField] private TileRotation rotation;
        public TileRotation Rotation => rotation;
        public bool Rotating { get; private set; }
        private Tween rotateTween;

        private Puzzle puzzle;
        public Puzzle Puzzle
        {
            get
            {
                if (!puzzle) puzzle = GetComponentInParent<Puzzle>();
                return puzzle;
            }
        }

        private readonly List<TileFeature> features = new();
        public IReadOnlyList<TileFeature> Features => features;

        protected override void OnAwake()
        {
            base.OnAwake();
            if (!Puzzle) Debug.LogWarning($"{nameof(Tile)} without parent {nameof(Puzzles.Puzzle)} found.");
            else Puzzle.OnInitialized(this);
        }

        protected override bool OnInitialize()
        {
            Subscribe(TileFeature.FeatureAdded, OnTileFeatureAdded);
            Subscribe(TileFeature.FeatureRemoved, OnTileFeatureRemoved);
            return true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            OnInitialized(() => TileAdded.Execute(this, this));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            OnInitialized(() => TileRemoved.Execute(this, this));
        }

        private void OnTileFeatureAdded(EventContext context, TileFeature feature)
        {
            if (features.Contains(feature)) return;
            features.Add(feature);
        }

        private void OnTileFeatureRemoved(EventContext context, TileFeature feature)
        {
            features.Remove(feature);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Unsubscribe(TileFeature.FeatureAdded);
            Unsubscribe(TileFeature.FeatureRemoved);
        }

        public void RotateLeft()
        {
            RotateTo(rotation switch
            {
                TileRotation.North => TileRotation.West,
                TileRotation.East => TileRotation.North,
                TileRotation.South => TileRotation.East,
                TileRotation.West => TileRotation.South,
                _ => TileRotation.North
            });
        }
        
        public void RotateRight()
        {
            RotateTo(rotation switch
            {
                TileRotation.North => TileRotation.East,
                TileRotation.East => TileRotation.South,
                TileRotation.South => TileRotation.West,
                TileRotation.West => TileRotation.North,
                _ => TileRotation.North
            });
        }

        public IEnumerable<TFeature> GetFeatures<TFeature>()
        {
            foreach (var feature in features)
            {
                if (feature is TFeature tf) yield return tf;
            }
        }

        private void RotateTo(TileRotation rotation, bool animate = true)
        {
            if (Rotating || this.rotation == rotation) return;

            Rotating = true;
            TileRotating.Execute(this, this);
            this.rotation = rotation;
            Vector3 endRotation = FromRotation(rotation);

            void OnComplete()
            {
                Rotating = false;
                TileRotated.Execute(this, this);
            }

            if (!animate)
            {
                transform.localRotation = Quaternion.Euler(endRotation);
                OnComplete();
            } else
            {
                rotateTween = transform.DOLocalRotate(endRotation, 0.6f);
                rotateTween.OnComplete(OnComplete);
            }
        }

        private static Vector3 FromRotation(TileRotation rotation) => rotation switch
        {
            TileRotation.North => new(0,0,0),
            TileRotation.East => new(0, 90, 0),
            TileRotation.South => new(0, 180, 0),
            TileRotation.West => new(0, 270, 0),
            _ => throw new ArgumentException("Invalid rotation")
        };

#if UNITY_EDITOR
        private void OnValidate()
        {
            AlignToGrid();
            AlignToRotation();
        }

        [DisableIf("@!Puzzle"), ButtonGroup, Button("Align to Nearest", DirtyOnClick = true), PropertyOrder(-1)]
        private void AlignToNearest()
        {
            var gridIndex = Puzzle.WorldToGrid(transform.position);
            index = gridIndex;
            AlignToGrid();
        }

        [DisableIf("@!Puzzle"), ButtonGroup, Button("Align to Index", DirtyOnClick = true), PropertyOrder(-1)]
        private void AlignToIndex() => AlignToGrid();

        private void AlignToGrid()
        {
            if (!Puzzle) return;
            var puzzleLocal = Puzzle.transform.InverseTransformPoint(transform.position);
            transform.position = Puzzle.transform.TransformPoint(new Vector3(
                Index.x * Puzzle.TileSize,
                puzzleLocal.y,
                Index.y * Puzzle.TileSize));
        }

        private void AlignToRotation()
        {
            transform.localRotation = Quaternion.Euler(FromRotation(rotation));
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy)]
        private static void OnDrawGizmo(Tile tile, GizmoType type)
        {
            if (!tile.Puzzle) return;

            float height = tile.Puzzle.transform.InverseTransformPoint(tile.transform.position).y;
            Vector3 minCorner = new Vector3(
                tile.Puzzle.TileSize * (tile.Index.x - 0.5f),
                height,
                tile.Puzzle.TileSize * (tile.Index.y - 0.5f));
            Vector3 maxCorner = new Vector3(
                tile.Puzzle.TileSize * (tile.Index.x + 0.5f),
                height,
                tile.Puzzle.TileSize * (tile.Index.y + 0.5f));

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
#endif
    }
}

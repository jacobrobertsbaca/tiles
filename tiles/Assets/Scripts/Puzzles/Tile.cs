using System;
using System.Collections.Generic;
using Tiles.Core;
using Tiles.Core.Events;
using Tiles.Puzzles.Features;
using UnityEngine;

namespace Tiles.Puzzles
{
    public class Tile : Actor
    {
        public static readonly Event<Tile> TileAdded = new($"{nameof(Tile)}::{nameof(TileAdded)}");
        public static readonly Event<Tile> TileRemoved = new($"{nameof(Tile)}::{nameof(TileRemoved)}");

        [SerializeField] internal Vector2Int index;
        public Vector2Int Index => index;

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

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Puzzle) Puzzle.AlignToGrid(this);
        }
#endif
    }
}

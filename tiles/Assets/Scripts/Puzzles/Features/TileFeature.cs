using System.Collections;
using System.Collections.Generic;
using Tiles.Core.Events;
using UnityEngine;

namespace Tiles.Puzzles.Features
{
    public class TileFeature : Actor
    {
        public static readonly Event<TileFeature> FeatureAdded = new($"{nameof(TileFeature)}::{nameof(FeatureAdded)}");
        public static readonly Event<TileFeature> FeatureRemoved = new($"{nameof(TileFeature)}::{nameof(FeatureRemoved)}");

        private Tile tile;
        public Tile Tile
        {
            get
            {
                if (!tile) tile = GetComponentInParent<Tile>();
                return tile;
            }
        }

        protected override void OnAwake()
        {
            if (!Tile) Debug.LogWarning($"{GetType().Name} found without parent ${nameof(Puzzles.Tile)}");
            else Tile.OnInitialized(this);
        }

        protected override bool OnInitialize()
        {
            FeatureAdded.Execute(this, this);
            return true;
        }

        protected override void Destroy()
        {
            base.Destroy();
            FeatureRemoved.Execute(this, this);
        }
    }
}

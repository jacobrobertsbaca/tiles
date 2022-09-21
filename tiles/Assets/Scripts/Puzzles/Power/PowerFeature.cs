using System;
using System.Collections;
using System.Collections.Generic;
using Tiles.Core.Events;
using Tiles.Puzzles.Features;
using UnityEngine;

namespace Tiles.Puzzles.Power
{
    public abstract class PowerFeature : TileFeature
    {
        public static readonly Event<PowerNetwork.IReadOnlyTilePower> InputsUpdated = new($"{nameof(PowerFeature)}::{nameof(InputsUpdated)}");

        public IReadOnlyCollection<PowerNode> Inputs { get; }

        public abstract void Transmit(PowerNetwork.ITilePower power);

        protected override bool OnInitialize()
        {
            Subscribe(InputsUpdated, HandleInputsUpdated);
            return base.OnInitialize();
        }

        private void HandleInputsUpdated(EventContext context, PowerNetwork.IReadOnlyTilePower data) => OnInputsUpdated(data);

        protected virtual void OnInputsUpdated(PowerNetwork.IReadOnlyTilePower power) {}

        protected override void Destroy()
        {
            base.Destroy();
            Unsubscribe(InputsUpdated);
        }
    }
}

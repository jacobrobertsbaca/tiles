using System.Collections;
using System.Collections.Generic;
using Tiles.Puzzles.Power;
using UnityEngine;

namespace Tiles.Puzzles.Features.Power
{
    public class SourcePowerFeature : PowerFeature
    {
        [SerializeField]
        private bool isPowered;

        [SerializeField]
        private PowerNode output;

        [SerializeField]
        private MeshRenderer mesh;

        public override IReadOnlyCollection<PowerNode> Inputs => new PowerNode[0];
        public override IReadOnlyCollection<PowerNode> Outputs => new[] { output };

        private Material meshMaterial;
        private Color defaultColor;

        protected override bool OnInitialize()
        {
            meshMaterial = new Material(mesh.sharedMaterial);
            mesh.sharedMaterial = meshMaterial;
            defaultColor = meshMaterial.color;
            return base.OnInitialize();
        }

        protected internal override void OnTransmit(PowerNetwork.ITilePower power)
        {
            power[output] = power[output].SetSource(this, isPowered);
        }

        protected internal override void OnAfterTransmit(PowerNetwork.IReadOnlyTilePower power)
        {
            base.OnAfterTransmit(power);
            meshMaterial.color = power[output].HasPower() ? Color.red : defaultColor;
        }

        private void OnGUI()
        {
            if (GUI.Button(new Rect(0, 0, 100, 100), "Toggle"))
            {
                isPowered = !isPowered;
                PowerFeature.NeedsTransmit.Execute(this, this);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Destroy(meshMaterial);
        }
    }
}
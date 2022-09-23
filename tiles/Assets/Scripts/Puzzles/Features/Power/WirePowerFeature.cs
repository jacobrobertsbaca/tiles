using System.Collections;
using System.Collections.Generic;
using Tiles.Puzzles.Power;
using UnityEngine;

namespace Tiles.Puzzles.Features.Power
{
    public class WirePowerFeature : PowerFeature
    {
        [SerializeField]
        private PowerNode input, output;

        [SerializeField]
        private bool isDiode;

        [SerializeField]
        private MeshRenderer mesh;

        public override IReadOnlyCollection<PowerNode> Inputs
        {
            get
            {
                PowerNode[] inputs;
                if (isDiode) inputs = new[] { input };
                else inputs = new[] { input, output };
                return inputs;
            }
        }

        public override IReadOnlyCollection<PowerNode> Outputs
        {
            get
            {
                PowerNode[] outputs;
                if (isDiode) outputs = new[] { output };
                else outputs = new[] { input, output };
                return outputs;
            }
        }

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
            var combined = power[output].Combine(power[input]);
            if (isDiode) power[output] = combined;
            else
            {
                power[input] = combined;
                power[output] = combined;
            }
        }

        protected internal override void OnInputsUpdated(PowerNetwork.IReadOnlyTilePower power)
        {
            base.OnInputsUpdated(power);
            bool powered = power[input].HasPower();
            if (!isDiode) powered |= power[output].HasPower();
            meshMaterial.color = powered ? Color.red : defaultColor;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Destroy(meshMaterial);
        }
    }
}
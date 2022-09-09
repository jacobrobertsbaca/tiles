using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tiles.Puzzles.Power
{
    public class PowerType : ScriptableObject
    {
        [SerializeField] private Color powerColor;
        public Color PowerColor => powerColor;
    }
}

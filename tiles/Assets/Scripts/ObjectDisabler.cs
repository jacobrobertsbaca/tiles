using System.Collections;
using System.Collections.Generic;
using Tiles.Core;
using UnityEngine;

namespace Tiles
{
    public class ObjectDisabler : Actor
    {
        public GameObject Object;

        private void OnGUI()
        {
            if (GUI.Button(new Rect(0, 100, 100, 100), "Disable"))
            {
                if (Object) Object.SetActive(!Object.activeSelf);
            }
        }
    }
}

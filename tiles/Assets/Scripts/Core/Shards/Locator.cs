using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tiles.Core.Shards
{
    /// <summary>
    /// Gives a <see cref="GameObject"/> a persistent identifier to locate it across save files.
    /// </summary>
    [DisallowMultipleComponent]
    public class Locator : MonoBehaviour, ISerializationCallbackReceiver
    {
        public System.Guid Id { get; private set; } = System.Guid.Empty;
        [SerializeField] private byte[] serializedId;

        public void OnAfterDeserialize()
        {

        }

        public void OnBeforeSerialize()
        {
        }
    }
}

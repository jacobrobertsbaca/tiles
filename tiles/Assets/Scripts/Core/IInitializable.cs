using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tiles.Core
{
    public enum InitStatus : byte
    {
        Uninitialized,
        Initializing,
        Initialized
    }

    public interface IInitializable
    {
        /// <summary>
        /// Initializes this object
        /// </summary>
        void Initialize();

        /// <summary>
        /// The current initialization state of this object
        /// </summary>
        InitStatus InitStatus { get; }
    }
}

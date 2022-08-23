using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tiles.Core
{
    /// <summary>
    /// Represents the status of <see cref="IInitializable"/>
    /// </summary>
    public enum InitStatus : byte
    {
        /// <summary>
        /// The object has not yet had <see cref="IInitializable.Initialize"/> called
        /// </summary>
        Uninitialized,

        /// <summary>
        /// The object has had <see cref="IInitializable.Initialize"/> called, but initialization has not yet completed
        /// </summary>
        Initializing,

        /// <summary>
        /// The object has had <see cref="IInitializable.Initialize"/> called and initialization has completed
        /// </summary>
        Initialized
    }

    /// <summary>
    /// An object that can be initialized
    /// </summary>
    public interface IInitializable
    {
        /// <summary>
        /// Initializes the object
        /// </summary>
        void Initialize();

        /// <summary>
        /// The current initialization status of the object
        /// </summary>
        InitStatus InitStatus { get; }
    }
}

﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Godot;

namespace Microsoft.MixedReality.Toolkit.Input
{
    /// <summary>
    /// Interface to implement to react to focus enter/exit.
    /// </summary>
    public interface IMixedRealityFocusHandler : IMixedRealityEventHandler
    {
        /// <summary>
        /// The Focus Enter event is raised on this Spatial whenever a TargetTool's focus enters this Parent Actor.
        /// </summary>
        [Signal]
        delegate void OnFocusEnter(MixedRealityFocusEventData eventData);

        /// <summary>
        /// The Focus Exit event is raised on this Spatial whenever a TargetTool's focus leaves this  Parent Actor.
        /// </summary>
        [Signal]
        delegate void OnFocusExit(MixedRealityFocusEventData eventData);
    }
}
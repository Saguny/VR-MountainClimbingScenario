using System;
using UnityEngine;

namespace MountainRescue.UI
{
    // Feature 2: Direction Enum
    public enum VerticalGuidanceState
    {
        Neutral, // Within tolerance
        TargetIsAbove,
        TargetIsBelow
    }

    // Feature 1: Distance Contract
    public interface IDistanceProvider
    {
        float GetDistanceToTarget();
        bool HasValidTarget();
    }

    // Feature 2: Vertical Direction Contract
    public interface IVerticalGuidanceProvider
    {
        VerticalGuidanceState GetCurrentState();
        // Event is useful here so we don't have to check enum changes every frame in the UI
        event Action<VerticalGuidanceState> OnStateChanged;
    }

    // Feature 3: Pressure Contract
    public interface IPressureProvider
    {
        float GetPressureHPa();
        float GetAltitudeMeters();
    }
}
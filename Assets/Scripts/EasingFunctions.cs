// EasingFunctions.cs
using UnityEngine;

public static class EasingFunctions
{
    public static float ApplyEasing(float t, EasingFunctionType type)
    {
        switch (type)
        {
            case EasingFunctionType.EaseOutQuad:
                return EaseOutQuad(t);
            case EasingFunctionType.EaseInOutQuad:
                return EaseInOutQuad(t);
            case EasingFunctionType.None:
            default:
                return t; // Linear (or rather, the value itself if t is used to scale speed)
        }
    }

    // t is progress from 0 to 1
    public static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    public static float EaseInOutQuad(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }

    // If you need just the easing factor to multiply speed:
    // For EaseOut, speed should decrease. A simple way is to use (1-t) for factor, but
    // for direct speed modulation, we need to consider how the easing function shapes the *displacement* curve.
    // The derivative of the ease function gives the speed curve.
    // For now, we'll use the easing function to determine the *current proportion of total movement/speed*.
}
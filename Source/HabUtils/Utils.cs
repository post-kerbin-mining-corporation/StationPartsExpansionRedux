using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;



namespace HabUtils
{
  public static class Utils
  {

    public static string modName = "HabUtils";

    /// Sets up an animation for KSP purposes and returns it
    public static AnimationState SetUpAnimation(string animationName, Part part, int layer)
    {
        Animation animation  = part.FindModelAnimators(animationName).First();
        AnimationState animationState = animation[animationName];
        animationState.speed = 0;
        animationState.layer = layer;
        animationState.enabled = true;
        animationState.wrapMode = WrapMode.ClampForever;

        animation.Blend(animationName);

        return animationState;
    }

    public static void Log(string str)
    {
        Debug.Log(String.Format("[{0}]: {1}", modName, str));
    }
    public static void LogError(string str)
    {
        Debug.LogError(String.Format("[{0}]: {1}", modName, str));
    }
    public static void LogWarning(string str)
    {
        Debug.LogWarning(String.Format("[{0}]: {1}", modName, str));
    }
  }
}

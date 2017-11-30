using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace HabUtils
{
  public class ModuleDependantAnimation: ModuleAnimateGeneric
  {
    // Index of the ModuleAnimateGeneric to use as a locker
    [KSPField(isPersistant = false)]
    public int AnimationModuleIndex = 0;

    // Animation timestamp to lock at
    [KSPField(isPersistant = false)]
    public float AnimationLockedTime = 1.0f;

    protected ModuleAnimateGeneric animator;

    protected void Start()
    {
      if (HighLogic.LoadedSceneIsFlight)
      {
        animator = (ModuleAnimateGeneric)part.Modules[AnimationModuleIndex];
      }
    }

    protected void Update()
    {
      if (animator != null)
      {
        if (animator.animTime == AnimationLockedTime)
        {
          Fields["deployPercent"].guiActive = false;
          this.animationIsDisabled = true;
        } else
        {
          Fields["deployPercent"].guiActive = true;
          this.animationIsDisabled = false;
        }
      }
    }

  }
}

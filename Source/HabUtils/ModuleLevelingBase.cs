using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;


namespace HabUtils
{

  public class ModuleLevelingBase: PartModule
  {
    // Is the module deployd
    [KSPField(isPersistant = true)]
    public bool Automated = true;

    // Associated legs
    private List<ModuleAdjustableLeg> legs = new List<ModuleAdjustableLeg>();

  }
}

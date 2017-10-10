using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;


namespace HabUtils
{
  public class ModuleAdjustableLeg:PartModule
  {

    // Lcoalized leg display name
    public string LegDisplayName = "Leg01";
    // Name to refer to the leg
    public string LegID = "Leg01";

    // Name of the object that will be extending
    public string ExtenderTransformName;

    // Current fraction of maximum extension
    public float ExtensionFraction = 0.0f;

    // Minimum extension, from model local position, in units
    public float ExtenderMin = 0.0f;

    // Maximum extension, from model local position, in units
    public float ExtenderMax = 1.0f;

    private Transform extenderTransform;
  }
}

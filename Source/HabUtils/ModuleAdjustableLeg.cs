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
    [KSPField(isPersistant = false)]
    public string LegDisplayName = "Leg01";

    // Name to refer to the leg
    [KSPField(isPersistant = false)]
    public string LegID = "Leg01";

    // Name of the object that will be extending
    [KSPField(isPersistant = false)]
    public string ExtenderTransformName;

    // Name of the object that is the origin of the extender
    [KSPField(isPersistant = false)]
    public string BaseTransformName;

    // Name of the object that is the foot
    [KSPField(isPersistant = false)]
    public string FootTransformName;

    // Current fraction of maximum extension
    [KSPField(isPersistant = true, guiActive = true, guiName = "Leg X Extension"), UI_FloatRange(minValue = 0f, maxValue = 100f, stepIncrement = 1f)]
    public float LegExtension = 0f;

    // Minimum extension, from model local position, in units
    [KSPField(isPersistant = false)]
    public float ExtenderMin = 0.0f;

    // Maximum extension, from model local position, in units
    [KSPField(isPersistant = false)]
    public float ExtenderMax = 1.0f;

    // Extension Rate
    [KSPField(isPersistant = false)]
    public float ExtensionRate = 1.0f;

    private Vector3 legZeroPosition;
    private Vector3 relativePosition;

    private float LegExtensionGoal = 0f;
    private Transform extenderTransform;
    private Transform baseTransform;
    private Transform footTransform;

    public Vector3 BaseTransform {
      get {return baseTransform;}
    }

    public virtual void Start()
    {
      if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
      {
        SetupTransform();
        SetupExtension();
        SetupUI();
      }
    }

    public virtual void Update()
    {
      if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
      {}
    }

    public virtual void FixedUpdate()
    {
      if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
      {
        HandleLegMovement();
      }
    }

    // Sets up the ui/localizations
    protected void SetupUI()
    {
      string legName = Localizer.Format(LegDisplayName);
      Fields["LegExtension"].guiName = Localizer.Format("#LOC_SSPX_ModuleAdjustableLeg_BaseName", legName);
    }

    // Sets up the model transform
    protected void SetupTransform()
    {
      extenderTransform = part.FindModelTransform(ExtenderTransformName);
      baseTransform = part.FindModelTransform(BaseTransformName);
      footTransform = part.FindModelTransform(FootTransformName);

      if (extenderTransform == null)
        Utils.LogError(String.Format("[ModuleAdjustableLeg]: Could not find ExtenderTransformName {0}", ExtenderTransformName));
      if (baseTransform == null)
        Utils.LogError(String.Format("[ModuleAdjustableLeg]: Could not find BaseTransformName {0}", BaseTransformName));
      if (footTransform == null)
        Utils.LogError(String.Format("[ModuleAdjustableLeg]: Could not find FootTransformName {0}", FootTransformName));

        legZeroPosition = extenderTransform.localPosition;
    }

    // Sets up the model extension from startup
    protected void SetupExtension()
    {
      SetExtension(LegExtension);
      extenderTransform.localPosition = legZeroPosition + Vector3.up * LegExtension;
    }

    // Does the actual leg movement
    protected void HandleLegMovement()
    {
      extenderTransform.localPosition = Vector3.MoveTowards(extenderTransform.localPosition,
        legZeroPosition + Vector3.up * legExtensionGoal,
        ExtensionRate * TimeWarp.fixedDeltaTime);
    }

    // Sets the surface normal for the foot
    public void SetSurfaceNormal(Vector3 norm)
    {

    }

    // Sets the leg extension by fraction
    public void SetExtension(float extension)
    {
      dist = Mathf.Clamp(dist, 0f, 100f);
      LegExtension = extension;
      legExtensionGoal = (ExtenderMax - ExtenderMin) * (extension/100f) + ExtenderMin;
    }
    // Sets the leg extension by distance
    public void SetExtensionDistance(float dist)
    {
      extension = Mathf.Clamp(extension, 0f, ExtenderMax - ExtenderMin);
      float fraction = extension/(ExtenderMax - ExtenderMin)
      SetExtension(fraction);
    }

    // Sets the relative position, which is the difference between the base transform position and the current position
    public void SetRelativePosition(Vector3 hostPosition)
    {
      relativePosition = baseTransform.position - hostPosition;
    }

    // Toggle the UI
    public void SetUIVisibility(bool enabled)
    {
      Fields["LegExtension"].guiActive = enabled;
    }

  }
}

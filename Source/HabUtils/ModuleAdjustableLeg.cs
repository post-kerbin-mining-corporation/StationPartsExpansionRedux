using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;
using VisualDebugUtils;

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

    // Extension Rate
    [KSPField(isPersistant = false)]
    public float FootRotationRate = 10.0f;

    [KSPField(isPersistant = false)]
    public float PhysicsBounce = 0.0f;

    [KSPField(isPersistant = false)]
    public float PhysicsStaticFriction = 0.5f;

    [KSPField(isPersistant = false)]
    public float PhysicsDynamicFriction = 0.5f;

    [KSPField(isPersistant = false)]
    public bool EnableDebug = false;

    [KSPField(isPersistant = false)]
    public bool FeetPointOutwards = false;

    private Vector3 legZeroPosition;
    private Vector3 relativePosition;

    private float legExtensionGoal = 0f;
    private Transform extenderTransform;
    private Transform baseTransform;
    private Transform footTransform;

    private PhysicMaterial phys;

    public DebugAxisTripod D_extenderXform;
    public DebugAxisTripod D_autoXform;
    public DebugAxisTripod D_baseXform;
    public DebugAxisTripod D_footXform;

    Quaternion footRotationGoal;

    public Transform BaseTransform {
      get {return baseTransform;}
    }


    public virtual void Start()
    {
      if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
      {
          SetupTransform();
          SetupExtension();
        SetupUI();
        if (EnableDebug)
            SetupDebug();
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
      if (HighLogic.LoadedSceneIsFlight)
      {
          CalculateFootAngle();
        HandleFootMovement();
      }
    }

    // Sets up the ui/localizations
    protected void SetupUI()
    {
      string legName = Localizer.Format(LegDisplayName);
      Fields["LegExtension"].guiName = Localizer.Format("#LOC_SSPX_ModuleAdjustableLeg_BaseName", legName);
      UI_FloatRange slider = (UI_FloatRange)Fields["LegExtension"].uiControlEditor;
      slider.onFieldChanged = OnChangeExtension;
      slider = (UI_FloatRange)Fields["LegExtension"].uiControlFlight;
      slider.onFieldChanged = OnChangeExtension;
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

        Collider legCollider = extenderTransform.GetComponentInChildren<Collider>();
        phys = legCollider.material;
        legCollider.enabled = false;
        if (phys == null)
        {
          phys = new PhysicMaterial();
          legCollider.material = phys;
        }

        phys.bounciness = PhysicsBounce;
        phys.staticFriction = PhysicsStaticFriction;
        phys.dynamicFriction = PhysicsDynamicFriction;

        legZeroPosition = baseTransform.localPosition;
    }

    // Sets up the model extension from startup
    protected void SetupExtension()
    {
      SetExtension(LegExtension);
      extenderTransform.localPosition = legZeroPosition - Vector3.forward * legExtensionGoal;
      Collider legCollider = extenderTransform.GetComponentInChildren<Collider>();
      legCollider.enabled = true;
    }

    // Sets up the model extension from startup
    protected void SetupDebug()
    {
      D_extenderXform = new DebugAxisTripod(1f);
      D_extenderXform.AssignTransform(extenderTransform);


      D_footXform = new DebugAxisTripod(0.05f);
      D_footXform.AssignTransform(footTransform);

      D_baseXform = new DebugAxisTripod(0.2f);
      D_baseXform.AssignTransform(baseTransform);

      D_autoXform = new DebugAxisTripod(1f);
      D_autoXform.AssignTransform(baseTransform);
    }

    protected void OnChangeExtension(BaseField field, object what)
    {
        SetExtension(LegExtension);
    }
    // Does the actual leg movement
    protected void HandleLegMovement()
    {

      extenderTransform.localPosition = Vector3.MoveTowards(extenderTransform.localPosition,
        legZeroPosition - Vector3.forward * legExtensionGoal,
        ExtensionRate * TimeWarp.fixedDeltaTime);


    }
    protected void HandleFootMovement()
    {
      footTransform.rotation = Quaternion.RotateTowards(footTransform.rotation, footRotationGoal, FootRotationRate * TimeWarp.fixedDeltaTime);
    }
    protected int ticker = 0;
    protected void CalculateFootAngle()
    {
      if (part.checkLanded())
      {
        if (ticker > 10)
        {
          RaycastHit hit;
          if (RaycastSurface(footTransform.position, vessel.mainBody.bodyTransform.position - footTransform.position, out hit))
          {
            if (FeetPointOutwards)
            {
              footRotationGoal = Quaternion.LookRotation(hit.normal, footTransform.position - part.partTransform.position);
            }
            else
            {
              footRotationGoal = Quaternion.LookRotation(hit.normal, footTransform.up);
            }
          }
          else
          {
            if (FeetPointOutwards)
            {
              footRotationGoal = Quaternion.LookRotation(part.partTransform.up, footTransform.position - part.partTransform.position);
            }
            else
            {
              footRotationGoal = Quaternion.LookRotation(part.partTransform.up);
            }
          }
          ticker = 0;
        }
        ticker++;
      } else
      {
        if (FeetPointOutwards)
        {
          footRotationGoal = Quaternion.LookRotation(part.partTransform.up, footTransform.position - part.partTransform.position);
        }
        else
        {
          footRotationGoal = Quaternion.LookRotation(part.partTransform.up);
        }
      }
    }

    // Raycasts against the KSP surface
    protected bool RaycastSurface(Vector3 position, Vector3 down, out RaycastHit outHit)
    {
      // Only cast against terrain
      LayerMask surfaceLayerMask;
      LayerMask maskT = 1 << LayerMask.NameToLayer("TerrainColliders");
      LayerMask maskS = 1 << LayerMask.NameToLayer("Local Scenery");
      surfaceLayerMask = maskT | maskS;

      return Physics.Raycast(position, down, out outHit, 0.25f, surfaceLayerMask, QueryTriggerInteraction.Ignore);
    }

    // Sets the surface normal for the foot
    public void SetSurfaceNormal(Vector3 norm)
    {
      //footRotationGoal = Quaternion.LookRotation(norm);
    }

    // Sets the leg extension by fraction
    public void SetExtension(float extension)
    {
      extension = Mathf.Clamp(extension, 0f, 100f);
      LegExtension = extension;
      legExtensionGoal = (ExtenderMax - ExtenderMin) * (extension/100f) + ExtenderMin;
    }
    // Sets the leg extension by distance
    public void SetExtensionDistance(float dist)
    {
      dist = Mathf.Clamp(dist, 0f, ExtenderMax - ExtenderMin);
      float fraction = dist / (ExtenderMax - ExtenderMin);
      SetExtension(fraction*100f);
    }
    public float GetExtensionDistance()
    {
      return legExtensionGoal;
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
      Fields["LegExtension"].guiActiveEditor = enabled;
    }

  }
}

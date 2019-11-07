using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;
using VisualDebugUtils;

namespace HabUtils
{

  public class ModuleLevelingBase: PartModule
  {
    // What level of automation is desired?
    [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Leveling Mode")]
    [UI_ChooseOption(affectSymCounterparts = UI_Scene.None, scene = UI_Scene.All, suppressEditorShipModified = true)]
    public int currentModeIndex = 0;

    // Display base angle
    [KSPField(isPersistant = false, guiActive = true, guiName = "Levelling Angle")]
    public string AbsoluteAngle;

    // The transform to use for leveling. Should be positioned at a similar height to the leg baseTransforms
    [KSPField(isPersistant = true)]
    public string LevelingTransformName;

    // Whether to allow auto-levelling or not
    [KSPField(isPersistant = false)]
    public bool CanAutoLevel = true;

    // Whether to allow the autolevel tweaker or not
    [KSPField(isPersistant = false)]
    public bool CanTweakAutoLevel = false;

    // Whether to show deploy/retract in the UI or not
    [KSPField(isPersistant = false)]
    public bool ShowDeployRetract = false;

    // Maximum linked offset
    [KSPField(isPersistant = true)]
    public float MaxLegDistance = 0.0f;

    // Current linked offset
    [KSPField(isPersistant = true, guiActive = true, advancedTweakable = true, guiName = "Auto-Level Minimum"), UI_FloatRange(minValue = 0f, maxValue = 100f, stepIncrement = 1f)]
    public float MinimumAutoLevelFraction = 10f;

    // Current linked offset
    [KSPField(isPersistant = true, guiActive = true, guiName = "Leg Extension"), UI_FloatRange(minValue = 0f, maxValue = 100f, stepIncrement = 1f)]
    public float LinkedExtension = 0f;

    // Maximum linked offset
    [KSPField(isPersistant = false)]
    public float LinkedExtensionMax = 1.0f;

    [KSPField(isPersistant = false)]
    public bool EnableDebug = false;

    [KSPField(isPersistant = true)]
    public float previousOffset = 0f;

    // Fire the auto-level event
    [KSPEvent(guiActive = true, guiName = "Auto-Level")]
    public void AutoLevel()
    {
        if (CanAutoLevel)
            DoAutoLevel();
    }

    /// GUI Actions
    // ACTIONS
    [KSPAction("Auto-Level")]
    public void AutoLevelAction(KSPActionParam param) { AutoLevel(); }
    
    [KSPAction("Deploy")]
    public void DeployAction(KSPActionParam param)
    {
      Deploy();
    }

    [KSPAction("Retract")]
    public void RetractAction(KSPActionParam param)
    {
      Retract();
    }

    [KSPAction("Toggle Retraction", actionGroup=KSPActionGroup.Gear)]
    public void ToggleAction(KSPActionParam param)
    {
      
      if (IsExtended())
        Retract();
      else
        Deploy();
    }

    // 'Retracts' the legs
    [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Deploy")]
    public void Retract()
    {
      for (int i = 0; i < legs.Length; i++)
      {
        legs[i].SetExtension(0f);
      }
    }
    // 'Deploys' the legs
    [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Deploy")]
    public void Deploy()
    {
      AutoLevel();
     }

    private float currentOffset = 0f;

    // Associated legs on the part
    private ModuleAdjustableLeg[] legs;
    private Transform levelingTransform;

    // Visual Debug Objects
    private DebugPoint D_pivotPoint;

    public virtual void Start()
    {
      if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
      {

        SetupTransforms();
        SetupLegs();
        if (EnableDebug)
            SetupDebug();
        SetupUI();
      }
    }

    public virtual void Update()
    {
      if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
      {
        if (ShowDeployRetract)
        {
          bool extended = IsExtended();
          Events["Deploy"].active = !extended;
          Events["Retract"].active = extended;
        }
      }
    }

    public virtual void FixedUpdate()
    {
      if (HighLogic.LoadedSceneIsFlight)
      {
        ComputeLeveling();

      }
    }

    protected bool IsExtended()
    {
      bool anyExtended = false;
      for (int i = 0; i < legs.Length; i++)
      {
        if (legs[i].LegExtension > 0f)
        {
          anyExtended = true;
        }
      }
      return anyExtended;
    }
    // Sets up the transforms
    protected void SetupTransforms()
    {
      levelingTransform = part.FindModelTransform(LevelingTransformName);
      if (levelingTransform == null)
        Utils.LogError(String.Format("[ModuleLevelingBase]: Could not find LevelingTransformName {0}", LevelingTransformName));
    }

    // Sets up the ModuleAdjustableLeg components
    protected void SetupLegs()
    {
      legs = part.GetComponents<ModuleAdjustableLeg>();
      
      foreach (ModuleAdjustableLeg leg in legs)
      {
        if (leg.ExtenderMax > MaxLegDistance)
        {
          MaxLegDistance = leg.ExtenderMax;
        }
      }
    }
    protected void SetupDebug()
    {
      D_pivotPoint = new DebugPoint(0.15f, Color.green);
      D_pivotPoint.XForm.SetParent(levelingTransform);
      D_pivotPoint.XForm.localPosition = Vector3.zero;
      D_pivotPoint.XForm.localRotation = Quaternion.identity;
    }
    protected void SetupUI()
    {
      Fields["MinimumAutoLevelFraction"].guiActive = CanTweakAutoLevel;
      if (ShowDeployRetract)
      {
        bool extended = IsExtended();
        Events["Deploy"].active = !extended;
        Events["Retract"].active = extended;
      }

      Events["AutoLevel"].guiActive = CanAutoLevel;
      Events["AutoLevel"].guiName = Localizer.Format("#LOC_SSPX_ModuleLevelingBase_Event_Auto-Level");

      Events["Deploy"].guiName = Localizer.Format("#LOC_SSPX_ModuleLevelingBase_Event_Deploy");
      Fields["MinimumAutoLevelFraction"].guiName = Localizer.Format("#LOC_SSPX_ModuleLevelingBase_Event_TweakLevel");
      Events["Retract"].guiName = Localizer.Format("#LOC_SSPX_ModuleLevelingBase_Event_Retract");

      Actions["DeployAction"].guiName = Localizer.Format("#LOC_SSPX_ModuleLevelingBase_Action_Deploy");
      Actions["RetractAction"].guiName = Localizer.Format("#LOC_SSPX_ModuleLevelingBase_Action_Retract");
      Actions["ToggleAction"].guiName = Localizer.Format("#LOC_SSPX_ModuleLevelingBase_Action_Toggle");
      Actions["AutoLevelAction"].guiName = Localizer.Format("#LOC_SSPX_ModuleLevelingBase_Action_Auto-Level");
      

      HandleModeChange(null, null);

      var chooseField = Fields["currentModeIndex"];
      chooseField.guiName = Localizer.Format("#LOC_SSPX_ModuleLevelingBase_Field_LevelingMode");
      var chooseOption = (UI_ChooseOption)chooseField.uiControlEditor;
      chooseOption.options = new string[] {Localizer.Format("#LOC_SSPX_ModuleLevelingBase_Field_LevelingMode_Linked"),
          Localizer.Format("#LOC_SSPX_ModuleLevelingBase_Field_LevelingMode_Manual")};
      chooseOption.onFieldChanged = HandleModeChange;
      chooseOption = (UI_ChooseOption)chooseField.uiControlFlight;
      chooseOption.options = new string[] {Localizer.Format("#LOC_SSPX_ModuleLevelingBase_Field_LevelingMode_Linked"),
          Localizer.Format("#LOC_SSPX_ModuleLevelingBase_Field_LevelingMode_Manual")};
      chooseOption.onFieldChanged = HandleModeChange;

      Fields["LinkedExtension"].guiName = Localizer.Format("#LOC_SSPX_ModuleLevelingBase_Field_LinkedExtension");
      UI_FloatRange slider = (UI_FloatRange)Fields["LinkedExtension"].uiControlEditor;
      slider.onFieldChanged = OnChangeExtension;
      slider = (UI_FloatRange)Fields["LinkedExtension"].uiControlFlight;
      slider.onFieldChanged = OnChangeExtension;

    }
    protected void HandleModeChange(BaseField field, object what)
    {
      if (currentModeIndex == 0)
      {
        Fields["LinkedExtension"].guiActive = true;
        foreach (ModuleAdjustableLeg leg in legs)
        {
            leg.SetUIVisibility(false);
        }
      }
      else if (currentModeIndex == 1)
      {
        Fields["LinkedExtension"].guiActive = false;
        foreach (ModuleAdjustableLeg leg in legs)
        {
            leg.SetUIVisibility(true);
        }
      }

    }

    protected void OnChangeExtension(BaseField field, object what)
    {
      currentOffset = LinkedExtension/100f * LinkedExtensionMax;
      for (int i = 0; i < legs.Length ;i++)
      {
        // Get the current distance, subtract the old offset, add the current offset
        float realDist = legs[i].GetExtensionDistance() - previousOffset;
        legs[i].SetExtensionDistance(realDist+currentOffset);
      }
      previousOffset = currentOffset;
     
    }
    protected void DoAutoLevel()
    {

      Vector3 downVector = vessel.mainBody.bodyTransform.position - levelingTransform.position;

      // Get rotation required to level the base
      //Quaternion startRotation = levelingTransform.rotation;
      //Quaternion endRotation = Quaternion.LookRotation(downVector);
      // Get rotation delta to get to that lvel
      Quaternion diffRotation = Quaternion.FromToRotation(levelingTransform.up, downVector);

      List<float> distanceDeltas = new List<float>();
      for (int i = 0; i < legs.Length ;i++)
      {
        // Rotate the leg transforms around the pivot by the rotation delta
        Vector3 newPos = RotatePointAroundPivot(legs[i].BaseTransform.position, levelingTransform.position, diffRotation);
        if (legs[i].EnableDebug)
            legs[i].D_autoXform.XForm.position = newPos;

        // Get distance of this new position to the surface
        RaycastHit hit;
        if (RaycastSurface(newPos, downVector, out hit))
        {
          distanceDeltas.Add(hit.distance);
          //Utils.Log(String.Format("Hit found for leg {0}, distance {1}, normal vector {2}", legs[i].LegDisplayName, hit.distance, hit.normal));
          legs[i].SetSurfaceNormal(hit.normal);
        }
        else
        {
          // If no hit
          distanceDeltas.Add(1000f);
          legs[i].SetSurfaceNormal(Vector3.up);
        }
      }

      // Determine the lowest extension
      float min = distanceDeltas.Min();
     // Utils.Log(String.Format("Minimum distance is {0}, offset is {1}",min, currentOffset));
      currentOffset = 0f;
      for (int i = 0; i < legs.Length ;i++)
      {
        //Utils.Log(String.Format("Setting extension of leg {0} to {1}, plus {2}", legs[i].LegDisplayName, distanceDeltas[i] - min, currentOffset));
        legs[i].SetExtensionDistance(MinimumAutoLevelFraction/100f*MaxLegDistance+ distanceDeltas[i]-min);
      }
      LinkedExtension = MinimumAutoLevelFraction;

      
    }
    // Rotates a position around a pivot point given a rotation
    public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation) {
      return rotation * (point - pivot) + pivot;
    }

    // Raycasts against the KSP surface
    protected bool RaycastSurface(Vector3 position, Vector3 down, out RaycastHit outHit)
    {
      // Only cast against terrain
      LayerMask surfaceLayerMask;
      LayerMask maskT = 1 << LayerMask.NameToLayer("TerrainColliders");
      LayerMask maskS = 1 << LayerMask.NameToLayer("Local Scenery");
      surfaceLayerMask = maskT | maskS;

      return Physics.Raycast(position, down, out outHit, 5f, surfaceLayerMask, QueryTriggerInteraction.Ignore);
    }

    protected void ComputeLeveling()
    {
      // Only level if landed
      if (part.vessel.LandedOrSplashed)
      {
          float angle = Vector3.Angle(levelingTransform.up, vessel.mainBody.bodyTransform.position - levelingTransform.position);
          AbsoluteAngle = Localizer.Format("#LOC_SSPX_ModuleLevelingBase_Field_AbsoluteAngle_Normal", angle.ToString("F1"));

      } else
      {
          AbsoluteAngle = Localizer.Format("#LOC_SSPX_ModuleLevelingBase_Field_AbsoluteAngle_None");
      }
    }



  }

}

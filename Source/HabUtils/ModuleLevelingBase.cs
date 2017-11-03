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
      }
    }

    public virtual void FixedUpdate()
    {
      if (HighLogic.LoadedSceneIsFlight)
      {
        ComputeLeveling();
      }
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
        Events["AutoLevel"].guiActive = CanAutoLevel;
        Events["AutoLevel"].guiName = Localizer.Format("#LOC_SSPX_ModuleLevelingBase_Event_Auto-Level");

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
          Utils.Log(String.Format("Hit found for leg {0}, distance {1}, normal vector {2}", legs[i].LegDisplayName, hit.distance, hit.normal));
          legs[i].SetSurfaceNormal(hit.normal);
        }
        else
        {
          distanceDeltas.Add(1000f);
          legs[i].SetSurfaceNormal(Vector3.up);
        }
      }

      // Determine the lowest extension
      float min = distanceDeltas.Min();
      Utils.Log(String.Format("Minimum distance is {0}",min));
      for (int i = 0; i < legs.Length ;i++)
      {
          Utils.Log(String.Format("Setting extension of leg {0} to {1}, plus {2}", legs[i].LegDisplayName, distanceDeltas[i] - min + currentOffset));
          legs[i].SetExtensionDistance(distanceDeltas[i]-min + currentOffset);
      }
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

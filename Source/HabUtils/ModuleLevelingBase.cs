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
    [KSPField(isPersistant = true)]
    public bool Automated = true;

    // Angular delta display
    [KSPField(isPersistant = false, guiActive = true, guiName = "Abs. Level angle")]
    public string AbsoluteAngle;

    // The transform to use for leveling
    [KSPField(isPersistant = true)]
    public string LevelingTransformName;

    // Fire the auto-level event
    [KSPEvent(guiActive = true, guiName = "Auto-Level")]
    public void AutoLevel()
    {
        DoAutoLevel();
    }

    // Associated legs on the part
    private ModuleAdjustableLeg[] legs;
    private Transform levelingTransform;
    private Transform rotatingTransform;

    private DebugPoint D_pivotPoint;

    public virtual void Start()
    {
      if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
      {

        SetupTransforms();
        SetupLegs();
        SetupDebug();
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
      D_pivotPoint = new DebugPoint(0.25f, Color.green);
      D_pivotPoint.XForm.SetParent(levelingTransform);
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
          Utils.Log(String.Format("Setting extension of leg {0} to {1}", legs[i].LegDisplayName, distanceDeltas[i]));
          legs[i].SetExtensionDistance(distanceDeltas[i]);
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

      return Physics.Raycast(position, down, out outHit, 50f, surfaceLayerMask, QueryTriggerInteraction.Ignore);
    }

    protected void ComputeLeveling()
    {
      // Only level if landed
      if (part.vessel.LandedOrSplashed)
      {
          float angle = Vector3.Angle(levelingTransform.up,levelingTransform.position - vessel.mainBody.bodyTransform.position);
        AbsoluteAngle = String.Format("{0} deg", angle);

      }
    }



  }

}

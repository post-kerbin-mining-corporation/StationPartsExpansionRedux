using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace HabUtils
{
  public class ModuleScalarLinkedLight : PartModule
  {


    [KSPField]
    public string transformName = "";
    [KSPField]
    public int scalarModuleIndex = -1;

    private Light[] lights;
    private float[] cachedLightIntensity;
    private Transform trf;
    private bool setup;
    private IScalarModule scalarModule;

    public override void OnStart(StartState state)
    {
      setup = false;
      trf = base.part.FindModelTransform(transformName);
      if (trf == null)
      {

        Utils.LogError($"[ModuleScalarLinkedLight]: No Transform exists in part {base.part.partName} called {transformName}");
        return;
      }
      lights = trf.GetComponentsInChildren<Light>(includeInactive: true);
      if (lights == null)
      {
        Debug.LogError("[ModuleScalarLinkedLight]: No Light components found in " + transformName + " on part " + base.part.partName, base.gameObject);
        return;
      } else
      {
        cachedLightIntensity = lights.ToList().Select(x => x.intensity).ToArray();
      }
      if (scalarModuleIndex != -1)
      {

        scalarModule = base.part.Modules[scalarModuleIndex] as IScalarModule;
        if (scalarModule == null)
        {

          Debug.LogError("[ModuleScalarLinkedLight]: Module at index " + scalarModuleIndex + " is not an IScalarModule on part " + base.part.partName, base.gameObject);
          return;

        }
      }

      if (scalarModule != null)
      {
        SetLightValues(scalarModule.GetScalar);
      }



      setup = true;

    }

    void LateUpdate()
    {
      if (!HighLogic.LoadedSceneIsFlight && !HighLogic.LoadedSceneIsEditor)
        return;
      if (!setup)
        return;

      if (scalarModule != null)
      {
        SetLightValues(scalarModule.GetScalar);
      }
    }

    void SetLightValues(float val)
    {
      if (lights == null)
        return;

      int num = lights.Length;
      while (num-- > 0)
      {
        if (val <= 0.001f && lights[num].enabled)
        {
          lights[num].enabled = false;
        }
        if (val > 0.001f && !lights[num].enabled)
        {
          lights[num].enabled = true;
        }
        if (lights[num].enabled)
        {
          lights[num].intensity = val*cachedLightIntensity[num];
        }
      }

    }
  }
}

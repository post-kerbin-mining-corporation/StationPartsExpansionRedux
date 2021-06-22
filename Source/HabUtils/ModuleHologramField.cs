using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HabUtils
{
  public class ModuleHologramField: PartModule
  {

    [KSPField]
    public string transformName = "";
    [KSPField]
    public int scalarModuleIndex = -1;

    [KSPField]
    public float flickerScale = 0.02f;

    [KSPField]
    public string colorName = "_TintColor";

    private Material[] materials;
    private Transform trf;
    private bool setup;
    private IScalarModule scalarModule;

    public override void OnStart(StartState state)
    {
      setup = false;
      trf = base.part.FindModelTransform(transformName);
      if (trf == null)
      {

        Utils.LogError($"[ModuleHologramField]: No Transform exists in part {base.part.partName} called {transformName}");
        return;
      }
      materials = trf.GetComponentsInChildren<Renderer>(includeInactive: true).ToList().Select(x => x.material).ToArray();
      if (materials == null)
      {
        Debug.LogError("[ModuleHologramField]: No Material components found in " + transformName + " on part " + base.part.partName, base.gameObject);
        return;
      }

      if (scalarModuleIndex != -1)
      {

        scalarModule = base.part.Modules[scalarModuleIndex] as IScalarModule;
        if (scalarModule == null)
        {

          Debug.LogError("[ModuleHologramField]: Module at index " + scalarModuleIndex + " is not an IScalarModule on part " + base.part.partName, base.gameObject);
          return;

        }
      }

      if (scalarModule != null)
      {
        SetMaterialValues(scalarModule.GetScalar);
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
        if (scalarModule.GetScalar < 1)
        {
          SetMaterialValues(0f);
        }
        else
        {
          SetMaterialValues(1f);
        }
      }
    }

    void SetMaterialValues(float val)
    {
      if (materials == null)
        return;
      

      int num = materials.Length;
      while (num-- > 0)
      {
        val *= UnityEngine.Random.Range(1f- flickerScale, 1f + flickerScale);
        materials[num].SetColor(colorName, new Color(val, val, val, val));
      }

    }
  }
}

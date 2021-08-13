using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HabUtils
{
  public class ModuleDisableEditorColliders: PartModule
  {
    [KSPField]
    public string transformName = "";


    [KSPEvent(active = true, guiActiveEditor = true,  guiActive = false, guiName = "Toggle Dome Attach")]
    public void ToggleEditorCollision()
    {
      SetAttach(!attachAllowed);
    }

    private Collider[] cols;

    private Transform trf;
    private bool attachAllowed = true;

  
    public override void OnStart(StartState state)
    {
      if (!HighLogic.LoadedSceneIsEditor)
      {
        return;
      }

      trf = base.part.FindModelTransform(transformName);
      if (trf == null)
      {

        Utils.LogError($"[ModuleDisableEditorColliders]: No Transform exists in part {base.part.partName} called {transformName}");
        return;

      }
      cols = trf.GetComponentsInChildren<Collider>(includeInactive: true);
      if (cols == null)
      {

        Debug.LogError("[ModuleDisableEditorColliders]: No Collider components found in " + transformName + " on part " + base.part.partName, base.gameObject);
        return;


      }

    }

    public void SetAttach(bool on)
    {
      
      int num = cols.Length;
      while (num-- > 0)
      {
        cols[num].enabled = on;
      }
      attachAllowed = on;
    }
  }
}

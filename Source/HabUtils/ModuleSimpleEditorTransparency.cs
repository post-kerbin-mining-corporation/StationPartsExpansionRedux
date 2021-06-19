using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HabUtils
{
  public class ModuleSimpleEditorTransparency : PartModule
  {


    [KSPField]
    public string transformName = "";

    [KSPField]
    public string shaderName = "KSP/Specular (Transparent)";

    [KSPField]
    public float screenRadius = 1f;

    [KSPField]
    public float proximityBias = 1.4f;

    [KSPField]
    public float minOpacity = 0.4f;
    private MeshRenderer[] mrs;
    private Transform trf;
    private bool setup;
    private float opacity;
    private float screenHeightRecip = 1f;
    private Shader seeThroughShader;


    public override void OnStart(StartState state)
    {
      setup = false;
      if (!HighLogic.LoadedSceneIsEditor)
      {
        return;
      }

      trf = base.part.FindModelTransform(transformName);
      if (trf == null)
      {

        Utils.LogError($"[ModuleSimpleEditorTransparency]: No Transform exists in part {base.part.partName} called {transformName}");
        return;

      }
      mrs = trf.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
      if (mrs == null)
      {

        Debug.LogError("[ModuleSimpleEditorTransparency]: No MeshRenderer components found in " + transformName + " on part " + base.part.partName, base.gameObject);
        return;


      }

      seeThroughShader = Shader.Find(shaderName);
      if (seeThroughShader != null)
      {

        int num = mrs.Length;
        while (num-- > 0)
        {
          mrs[num].material.shader = seeThroughShader;
        }

        opacity = 1f;

  
        SetOpacity(opacity);

        screenHeightRecip = 1f / (float)Screen.height;
        setup = true;
        return;
      }

    }


    private void LateUpdate()
    {
      if (EditorLogic.fetch == null)
      {
        return;
      }
      if (!setup)
      {
        return;
      }

      if (!EditorLogic.fetch.ship.Contains(base.part))
      {
        return;
      }

      MouseFadeUpdate();

    }

    private void MouseFadeUpdate()
    {
      float cursorProximity = GetCursorProximity(Input.mousePosition, screenRadius, trf, Camera.main);
      cursorProximity = Mathf.Pow(Mathf.Clamp01(cursorProximity), proximityBias);
      opacity = Mathf.Max(1f - cursorProximity, minOpacity);
  
      SetOpacity(opacity);
    }

    private float GetCursorProximity(Vector3 cursorPosition, float range, Transform trf, Camera referenceCamera)
    {
      float num = Mathf.Tan(referenceCamera.fieldOfView * 0.5f * ((float)Math.PI / 180f)) * (base.part.partTransform.transform.position - referenceCamera.transform.position).sqrMagnitude;
      float num2 = range * range / num;
      cursorPosition *= screenHeightRecip;
      Vector3 b = referenceCamera.WorldToScreenPoint(trf.position) * screenHeightRecip;
      Vector3 vector = cursorPosition - b;
      float sqrMagnitude = Vector3.ProjectOnPlane(vector, Vector3.forward).sqrMagnitude;
      return Mathf.Clamp01(1f - sqrMagnitude / num2);
    }

    public void SetOpacity(float o)
    {
      opacity = o;
      int num = mrs.Length;
      while (num-- > 0)
      {
        mrs[num].material.SetFloat(PropertyIDs._Opacity, o);
      }
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;


namespace HabUtils
{
  public class ModuleDeployableCentrifuge: ModuleDeployableHabitat
  {
    [KSPField(isPersistant = true)]
    public bool Rotating = false;

    // Speed of the deploy animation
    [KSPField(isPersistant = false)]
    public float SpinRate = 1.0f;

    // Speed of the deploy animation
    [KSPField(isPersistant = false)]
    public float SpinAccelerationRate = 1.0f;

    // Speed of the deploy animation
    [KSPField(isPersistant = false)]
    public string SpinTransformName = "";

    [KSPField(isPersistant = false)]
    public string CounterweightTransformName = "";

    // Speed of the deploy animation
    [KSPField(isPersistant = false)]
    public float CounterweightSpinRate = 1.0f;

    // Speed of the deploy animation
    [KSPField(isPersistant = false)]
    public float CounterweightSpinAccelerationRate = 1.0f;

    // private
    private Transform spinTransform;
    private Transform counterweightTransform;

    public override void Start()
    {
      base.Start();
    }

    public override void Update()
    {
      base.Update();
    }

    public override void FixedUpdate()
    {
      base.FixedUpdate();
    }


    protected override void StartRetract()
    {
      base.StartRetract();
      StopSpin();
    }
    protected override void StartDeploy()
    {
      base.StartDeploy();
      StartSpin();
    }
    protected override void FinishRetract()
    {
      base.FinishRetract();
    }
    protected override void FinishDeploy()
    {
      base.FinishDeploy();
    }

    /// Starts the spin
    void StartSpin()
    {

    }

    /// Starts slowing down the spin
    void StopSpin()
    {

    }
  }
}

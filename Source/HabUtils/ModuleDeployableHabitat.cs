using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;


namespace HabUtils
{
  public enum DeployState
  {
    Retracted, Deploying, Deployed, Retracting
  }

  public class ModuleDeployableHabitat: PartModule
  {
    // Name of the deploy Animation
    [KSPField(isPersistant = false)]
    public string DeployAnimationName = "";

    // Speed of the deploy animation
    [KSPField(isPersistant = false)]
    public float AnimationSpeed = 1.0f;

    // Layer of the deploy animation
    [KSPField(isPersistant = false)]
    public int AnimationLayer = 1;

    // Crew capacity when deployd
    [KSPField(isPersistant = false)]
    public int DeployedCrewCapacity = 2;

    // Is the module deployd
    [KSPField(isPersistant = true)]
    public bool Deployed = false;

    [KSPField(isPersistant = false)]
    public string DeployActionName = "";
    [KSPField(isPersistant = false)]
    public string RetractActionName = "";
    [KSPField(isPersistant = false)]
    public string ToggleActionName = "";
    /// GUI Fields
    // Current status of deploy
    [KSPField(isPersistant = false, guiActive = false, guiName = "Status")]
    public string DeployStatus = "N/A";

    // GUI Events
    [KSPEvent(guiActive = false, guiName = "Deploy", active = true)]
    public void Deploy()
    {
        TryDeploy();
    }
    [KSPEvent(guiActive = false, guiName = "Retract", active = false)]
    public void Retract()
    {
        TryRetract();
    }

    /// GUI Actions
    // ACTIONS
    [KSPAction("Deploy")]
    public void DeployAction(KSPActionParam param) { Deploy(); }

    [KSPAction("Retract")]
    public void RetractAction(KSPActionParam param) { Retract(); }

    [KSPAction("Toggle Deploy")]
    public void ToggleAction(KSPActionParam param)
    {
        if (Deployed)
          Retract();
        else
          Deploy();
    }

    /// protected
    protected float vabSpeedScale = 10f;
    protected DeployState deployState;
    protected AnimationState deployAnimation;

    public virtual void Start()
    {
      if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
      {
        SetupAnimation();
        SetupUI();
        SetupState();
      }
    }

    public virtual void Update()
    {
      if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
      {
        HandleUI();
      }
    }

    public virtual void FixedUpdate()
    {
      if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
      {
        HandleAnimation();
        EvaluateAnimation();
      }
    }

    /// Setup the animation
    protected void SetupAnimation()
    {
        if (DeployAnimationName != "")
          deployAnimation = Utils.SetUpAnimation(this.part, DeployAnimationName, AnimationLayer);
        else
          Utils.LogError("[ModuleDeployableHabitat]: Could not find animation");
    }

    /// Do localization of UI components
    protected void SetupUI()
    {
      Events["Deploy"].guiName = Localizer.Format(DeployActionName);
      Events["Retract"].guiName = Localizer.Format(RetractActionName);
      Actions["DeployAction"].guiName = Localizer.Format(DeployActionName);
      Actions["RetractAction"].guiName = Localizer.Format(RetractActionName);
      Actions["ToggleAction"].guiName = Localizer.Format(ToggleActionName);
    }

    /// Sets up the first load of the module
    protected void SetupState()
    {
      if (Deployed)
      {
        deployState = DeployState.Deployed;
      }
      else
      {
        deployState = DeployState.Retracted;
        DestroyIVA();
      }
      SetCrewCapacity(Deployed);
    }

    /// Handle updating the UI
    protected void HandleUI()
    {
      switch (deployState)
      {
        case DeployState.Retracted:
          DeployStatus = "Retracted";
          Events["Deploy"].active = true;
          Events["Retract"].active = false;
          break;
        case DeployState.Deployed:
          DeployStatus = "Deployed";
          Events["Deploy"].active = false;
          Events["Retract"].active = true;
            break;
        case DeployState.Deploying:
          DeployStatus = "Deploying";
          Events["Deploy"].active = false;
          Events["Retract"].active = true;
          break;
        case DeployState.Retracting:
          DeployStatus = "Retracting";
          Events["Deploy"].active = true;
          Events["Retract"].active = false;
          break;
      }
    }

    /// Handle updating the animation
    protected void HandleAnimation()
    {
      switch (deployState)
      {
        case DeployState.Retracted:
          break;
        case DeployState.Deployed:
            break;
        case DeployState.Deploying:
          deployAnimation.normalizedTime = Mathf.MoveTowards(deployAnimation.normalizedTime, 1.0f, AnimationSpeed*TimeWarp.fixedDeltaTime);
          break;
        case DeployState.Retracting:
          deployAnimation.normalizedTime = Mathf.MoveTowards(deployAnimation.normalizedTime, 0.0f, AnimationSpeed*TimeWarp.fixedDeltaTime);
          break;
      }
    }

    protected void EvaluateAnimation()
    {
      if (deployState == DeployState.Deploying && deployAnimation.normalizedTime >= 1.0)
      {
        FinishDeploy();

      }
      if (deployState == DeployState.Retracting && deployAnimation.normalizedTime <= 0.0)
      {
        FinishRetract();

      }
    }

    /// Deploys the part if it passes the check
    public void TryDeploy()
    {
      if (CanDeploy())
      {
        StartDeploy();
      }
    }

    /// Retracts the part if it passes the check
    public void TryRetract()
    {
      if (CanRetract())
      {
        StartRetract();
      }
    }

    /// Called to start deflation
    protected virtual void StartRetract()
    {
      Utils.Log("[ModuleDeployableHabitat]: Retract Started");
      deployState = DeployState.Retracting;
      SetCrewCapacity(false);
      DestroyIVA();
    }
    /// Called to start deploy
    protected virtual void StartDeploy()
    {
      Utils.Log("[ModuleDeployableHabitat]: Deploy Started");
      deployState = DeployState.Deploying;
    }

    /// Execute actions on deflation completion
    protected virtual void FinishRetract()
    {
      Utils.Log("[ModuleDeployableHabitat]: Retract Finished");
      deployState = DeployState.Retracted;
      Deployed = false;
    }

    /// Execute actions on deploy completion
    protected virtual void FinishDeploy()
    {
      Utils.Log("[ModuleDeployableHabitat]: Deploy Finished");
      deployState = DeployState.Deployed;
      Deployed = true;
      CreateIVA();
      SetCrewCapacity(Deployed);
    }

    /// Set the crew capacity of the part
    /// TODO: Implement me
    protected void SetCrewCapacity(bool canUseCrew)
    {
        if (canUseCrew)
        {
          part.crewCapacity = DeployedCrewCapacity;
        }
        else
        {
          part.crewCapacity = RetractedCrewCapacity;
        }
    }

    /// Creates the IVA space
    /// TODO: Implement me
    protected void CreateIVA()
    {}

    /// Destroys the IVA space
    /// TODO: Implement me
    protected void DestroyIVA()
    {}

    /// Checks to see if we can deploy
    protected bool CanDeploy()
    {
      if (deployState == DeployState.Retracted || deployState == DeployState.Retracting)
        return true;

      return false;
    }

    /// Checks to see if we can deflate or not
    /// TODO: Disable deploy if crew are present
    protected bool CanRetract()
    {
      if (deployState == DeployState.Deployed || deployState == DeployState.Deploying)
        return true;

      return false;
    }
  }
}

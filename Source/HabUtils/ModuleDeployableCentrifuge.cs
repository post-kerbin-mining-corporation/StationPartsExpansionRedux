using System;
using System.Collections;

using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;
using VisualDebugUtils;

namespace HabUtils
{
    public class ModuleDeployableCentrifuge : ModuleDeployableHabitat
    {


        // Is the centrifuge rotation enabled or not?
        [KSPField(isPersistant = true)]
        public bool Rotating = false;

        // The radius for gravity generation purposes
        [KSPField(isPersistant = false)]
        public float Radius = 0.0f;

        // The CurrentSpinRate
        [KSPField(isPersistant = false)]
        public float CurrentSpinRate = 0.0f;

        // Speed of the centrifuge rotation in deg/s
        [KSPField(isPersistant = false)]
        public float SpinRate = 10.0f;

        // Rate at which the SpinRate accelerates (deg/s/s)
        [KSPField(isPersistant = false)]
        public float SpinAccelerationRate = 1.0f;

        // Transform to rotate for the centrifuge
        [KSPField(isPersistant = false)]
        public string SpinTransformName = "";

        // The CurrentSpinRate
        [KSPField(isPersistant = false)]
        public float CurrentCounterweightSpinRate = 0.0f;

        // Rate at which the counterweight spins in deg/s
        [KSPField(isPersistant = false)]
        public float CounterweightSpinRate = 20.0f;

        // Rate at which the counterweight accelerates (deg/s/s)
        [KSPField(isPersistant = false)]
        public float CounterweightSpinAccelerationRate = 1.0f;

        // The resouce required to spin
        [KSPField(isPersistant = false)]
        public string SpinResource = "";

        // The amount required per s
        [KSPField(isPersistant = false)]
        public float SpinResourceRate = 0f;

        // Transform to rotate for the counterweight
        [KSPField(isPersistant = false)]
        public string CounterweightTransformName = "";

        // Name of the start action
        [KSPField(isPersistant = false)]
        public string StartSpinActionName = "";

        // Name of the stop action
        [KSPField(isPersistant = false)]
        public string StopSpinActionName = "";

        // Name of the toggle action
        [KSPField(isPersistant = false)]
        public string ToggleSpinActionName = "";

        // The maximum rate of spinning
        [KSPField(isPersistant = false)]
        public float MaxTimewarpSpinRate = 10f;

        // The spi
        [KSPField(isPersistant = false)]
        public int InternalSpinMapping = 2;

        /// GUI Fields
        // Current status of deploy
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Spin Gravity")]
        public string GravityStatus = "0g";

        // GUI Events
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Start Spin", active = true)]
        public void EventStartSpin()
        {
            StartSpin();
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Stop Spin", active = false)]
        public void EventStopSpin()
        {
            StopSpin();
        }

        /// GUI Actions
        // ACTIONS
        [KSPAction("Start Spin")]
        public void StartAction(KSPActionParam param) { EventStartSpin(); }

        [KSPAction("Stop Spin")]
        public void StopAction(KSPActionParam param) { EventStopSpin(); }

        [KSPAction("Toggle Spin")]
        public void ToggleSpinAction(KSPActionParam param)
        {
            if (Rotating)
                EventStartSpin();
            else
                EventStopSpin();
        }

        // private
        private Quaternion baseAngles;
        private Vector3 baseAnglesEuler;
        private float rotationRateGoal = 0f;
        private Transform spinTransform;
        private Transform counterweightTransform;
        Vector3 orig;
        private Transform IVARotationRoot;
        private System.Collections.Generic.Dictionary<Transform, Transform> propDict;

        // Calculates the period in s given the spin rate
        protected float Period(float spinRate)
        {
            return 360f / spinRate;
        }

        // Calculates the gravity in g
        protected float CalculateGravity(float radius, float period)
        {
            return (radius * Mathf.Pow((2f * Mathf.PI) / period, 2f)) / 9.81f;
        }

        public override string GetInfo()
        {
            string baseInfo = Localizer.Format("#LOC_SSPX_ModuleDeployableCentrifuge_PartInfo", DeployedCrewCapacity.ToString("F0"), CalculateGravity(Radius, Period(Mathf.Abs(SpinRate))).ToString("F2"));
            if (SpinResource != "")
            {
                PartResourceDefinition defn = PartResourceLibrary.Instance.GetDefinition(SpinResource);
                baseInfo += Localizer.Format("#LOC_SSPX_ModuleDeployableCentrifuge_PartInfo_SpinResources", defn.GetShortName(2), SpinResourceRate.ToString("F1"));
            }
            if (!Retractable)
                baseInfo += "\n\n" + Localizer.Format("#LOC_SSPX_ModuleDeployableHabitat_PartInfo_NoRetract");
            if (DeployResource != "")
            {
                PartResourceDefinition defn = PartResourceLibrary.Instance.GetDefinition(DeployResource);
                baseInfo += "\n\n" + Localizer.Format("#LOC_SSPX_ModuleDeployableHabitat_PartInfo_Resources", defn.displayName, DeployResourceAmount.ToString("F2"));
            }
            if (CrewToDeploy > 0)
            {
                if (CrewSkillNeeded == "")
                {
                    baseInfo += "\n\n" + Localizer.Format("#LOC_SSPX_ModuleDeployableHabitat_PartInfo_NeedsCrewUnskilled", CrewToDeploy.ToString("F0"));
                }
                else
                {
                    baseInfo += "\n\n" + Localizer.Format("#LOC_SSPX_ModuleDeployableHabitat_PartInfo_NeedsCrew", CrewToDeploy.ToString("F0"), Localizer.Format(CrewSkillNeededName));
                }
            }

            return baseInfo;
        }
        public string GetModuleTitle()
        {
            return "Expandable Centrifuge";
        }
        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_SSPX_ModuleDeployableCentrifuge_ModuleTitle");
        }

        public override void Start()
        {
            base.Start();
            if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
            {
                spinTransform = part.FindModelTransform(SpinTransformName);
                baseAngles = spinTransform.localRotation;
                baseAnglesEuler = spinTransform.localEulerAngles;
                counterweightTransform = part.FindModelTransform(CounterweightTransformName);

                if (Rotating)
                {
                    rotationRateGoal = 1.0f;
                    CurrentSpinRate = SpinRate;
                    CurrentCounterweightSpinRate = CounterweightSpinRate;
                }
                else
                {
                    rotationRateGoal = 0.0f;
                    CurrentSpinRate = 0f;
                    CurrentCounterweightSpinRate = 0f;
                }
            }
            if (HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.onCrewTransferred.Add(new EventData<GameEvents.HostedFromToAction<ProtoCrewMember, Part>>.OnEvent(ResetIVATransform));

                if (part.internalModel != null)
                {
                    //DebugAxisTripod exter = new DebugAxisTripod(3f);
                    //exter.AssignTransform(spinTransform);
                    //DebugAxisTripod inter = new DebugAxisTripod(5f);

                    orig = part.internalModel.transform.localEulerAngles;
                    DoIVASetup();
                    //inter.AssignTransform(transformer);
                }
            }
        }
        public void ResetIVATransform(GameEvents.HostedFromToAction<ProtoCrewMember, Part> dat)
        {
            DoIVASetup();


        }
        private void DoIVASetup()
        {
            if (part.internalModel != null)
            {
                IVARotationRoot = new GameObject("IVA Rotator").transform;
                IVARotationRoot.SetParent(part.internalModel.transform);
                IVARotationRoot.gameObject.layer = 16;
                IVARotationRoot.localPosition = Vector3.zero;
                IVARotationRoot.localRotation = Quaternion.identity;
                Transform themodel = part.internalModel.FindModelTransform("model");
                IVARotationRoot.localRotation = themodel.localRotation;

                propDict = new System.Collections.Generic.Dictionary<Transform, Transform>();
                foreach (Transform child in part.internalModel.transform)
                {
                    Transform proxy = new GameObject("IVA Proxy").transform;
                    proxy.gameObject.layer = 16;
                    proxy.SetParent(IVARotationRoot);
                    proxy.position = child.position;
                    proxy.rotation = child.rotation;
                    propDict.Add(proxy, child);

                }

            }
        }
        public IEnumerator WaitRotate()
        {
            yield return new WaitForSeconds(2.0f);
            float angle = Quaternion.Angle(baseAngles, spinTransform.localRotation);
            Utils.Log(angle.ToString());
            part.internalModel.transform.Rotate(Vector3.forward * (angle + 90f));
        }
        public override void Update()
        {
            base.Update();
            if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
            {
                HandleUI();

            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
            {
                DoSpin();
            }
        }

        protected override void SetupUI()
        {
            base.SetupUI();

            Fields["GravityStatus"].guiName = Localizer.Format("#LOC_SSPX_ModuleDeployableCentrifuge_Field_GravityStatus_Name");
            Events["EventStartSpin"].guiName = Localizer.Format(StartSpinActionName);
            Events["EventStopSpin"].guiName = Localizer.Format(StopSpinActionName);

            Actions["StartAction"].guiName = Localizer.Format(StartSpinActionName);
            Actions["StopAction"].guiName = Localizer.Format(StopSpinActionName);
            Actions["ToggleSpinAction"].guiName = Localizer.Format(ToggleSpinActionName);
        }


        // Fired when retraction starts
        protected override void StartRetract()
        {
            base.StartRetract();
            StopSpin();
        }

        // Fired when deployment starts
        protected override void StartDeploy()
        {
            base.StartDeploy();
            StartSpin();
        }

        // Fired when retraction is complete
        protected override void FinishRetract()
        {
            base.FinishRetract();
        }

        // Fired when deployment is complete
        protected override void FinishDeploy()
        {
            base.FinishDeploy();
        }

        // Updates the UI values
        protected override void HandleUI()
        {
            base.HandleUI();
            GravityStatus = Localizer.Format("#LOC_SSPX_ModuleDeployableCentrifuge_Field_GravityStatus_Normal", CalculateGravity(Radius, Period(Mathf.Abs(CurrentSpinRate))).ToString("F2"));

            if (base.deployState == DeployState.Retracted || base.deployState == DeployState.Retracting || base.deployState == DeployState.Deploying)
            {
                Events["EventStartSpin"].active = false;
                Events["EventStopSpin"].active = false;
            }
            else
            {
                if (Events["EventStartSpin"].active == Rotating || Events["EventStopSpin"].active != Rotating)
                {
                    Events["EventStopSpin"].active = Rotating;
                    Events["EventStartSpin"].active = !Rotating;
                }
            }
        }

        // Does the spinning of the centrifuge
        void DoSpin()
        {
            float spinAccel = SpinAccelerationRate;
            if (HighLogic.LoadedSceneIsEditor)
                spinAccel = spinAccel * 10f;

            if (HighLogic.LoadedSceneIsFlight && SpinResource != "" && Rotating)
            {
                double requested = part.RequestResource(SpinResource, (double)(SpinResourceRate * TimeWarp.fixedDeltaTime));
                if (requested < 0.00001)
                {
                    StopSpin();
                    PartResourceDefinition defn = PartResourceLibrary.Instance.GetDefinition(SpinResource);
                    var msg = Localizer.Format("#LOC_SSPX_ModuleDeployableCentrifuge_Message_CantSpinResources",
                                part.partInfo.title, defn.displayName);
                    ScreenMessages.PostScreenMessage(msg, 5f, ScreenMessageStyle.UPPER_CENTER);
                }
            }

            // If headed to zero
            if (rotationRateGoal == 0.0)
            {
                float returnSpin = 10f;
                if (HighLogic.LoadedSceneIsEditor)
                    returnSpin = returnSpin * 10f;
                // If we are not right on target, don't let spin rate fall below 5f
                if (CurrentSpinRate > returnSpin)
                {
                    CurrentSpinRate = Mathf.MoveTowards(CurrentSpinRate, rotationRateGoal * returnSpin, TimeWarp.fixedDeltaTime * spinAccel);
                }
                else
                {
                    if (Quaternion.Angle(baseAngles, spinTransform.localRotation) <= 2f)
                    {
                        CurrentSpinRate = Mathf.MoveTowards(CurrentSpinRate, 0f, TimeWarp.fixedDeltaTime * spinAccel * 20f);
                    }

                }
            }
            else
            {
                CurrentSpinRate = Mathf.MoveTowards(CurrentSpinRate, rotationRateGoal * SpinRate, TimeWarp.fixedDeltaTime * spinAccel);
            }

            CurrentCounterweightSpinRate = Mathf.MoveTowards(CurrentCounterweightSpinRate, rotationRateGoal * CounterweightSpinRate, TimeWarp.fixedDeltaTime * CounterweightSpinAccelerationRate);

            float spin = Mathf.Clamp(TimeWarp.fixedDeltaTime * CurrentSpinRate, -MaxTimewarpSpinRate, MaxTimewarpSpinRate);

            spinTransform.Rotate(Vector3.forward * spin);
            counterweightTransform.Rotate(Vector3.forward * TimeWarp.fixedDeltaTime * CurrentCounterweightSpinRate);


            if (part.internalModel != null)
            {

                //part.internalModel.transform.up = part.transform.up;


                Vector3 spinCorrection = Vector3.zero;
                if (InternalSpinMapping == 2)
                    spinCorrection = new Vector3(spinTransform.localEulerAngles.x, spinTransform.localEulerAngles.y, -spinTransform.localEulerAngles.z);
                if (InternalSpinMapping == 1)
                    spinCorrection = new Vector3(spinTransform.localEulerAngles.x - 270f, spinTransform.localEulerAngles.z, -spinTransform.localEulerAngles.y);
                if (InternalSpinMapping == 0)
                    spinCorrection = new Vector3(spinTransform.localEulerAngles.y, spinTransform.localEulerAngles.z, spinTransform.localEulerAngles.x);

                if (IVARotationRoot)
                {
                    IVARotationRoot.localEulerAngles = spinCorrection;
                    foreach (System.Collections.Generic.KeyValuePair<Transform, Transform> entry in propDict)
                    {
                        entry.Value.position = entry.Key.position;
                        entry.Value.rotation = entry.Key.rotation;
                    }
                }
                else
                {
                    DoIVASetup();
                }
            }
        }

        /// Starts the spin
        void StartSpin()
        {
            if (base.deployState == DeployState.Retracted || base.deployState == DeployState.Retracting || base.deployState == DeployState.Deploying)
                return;

            Utils.Log("[ModuleDeployableCentrifuge]: Initiating Spin");
            rotationRateGoal = 1.0f;
            Rotating = true;
        }

        /// Starts slowing down the spin
        void StopSpin()
        {
            Utils.Log("[ModuleDeployableCentrifuge]: Stopping Spin");
            rotationRateGoal = 0.0f;
            Rotating = false;
        }
    }
}

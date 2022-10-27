using com.ootii.Actors.AnimationControllers;
using com.ootii.Cameras;
using com.ootii.Geometry;
using com.ootii.Helpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WildWalrus.Actors.AnimationControllers
{
	[MotionName("Rifle AP Basic Idle")]
	[MotionDescription("Kubold's RIfle Anim Set Pro. Simple idle motion to be used as a default motion. It can also rotate the actor with the camera view.")]
	public class RifleAP_BasicIdle : MotionControllerMotion
	{
		#region MotionPhases

		public int PHASE_UNKNOWN = 0;
		public int PHASE_START = 73000;

		#endregion MotionPhases


		#region MotionProperties

		public override bool VerifyTransition
		{
			get { return false; }
		}

		#endregion MotionProperties
		
		
		#region Properties
		
		public bool _RotateWithCamera = false;
		public bool RotateWithCamera
		{
			get { return _RotateWithCamera; }
			
			set
			{
				_RotateWithCamera = value;
				
				// Register this motion with the camera
				if (mMotionController != null && mMotionController.CameraRig is BaseCameraRig)
				{
					((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
					if (_RotateWithCamera) { ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate += OnCameraUpdated; }
				}
			}
		}
		
		
		public float _RotationToCameraSpeed = 360f;
		public float RotationToCameraSpeed
		{
			get { return _RotationToCameraSpeed; }
			set { _RotationToCameraSpeed = value; }
		}
		
		
		public bool _RotateWithInput = false;
		public bool RotateWithInput
		{
			get { return _RotateWithInput; }
			set { _RotateWithInput = value; }
		}
		
		
		public float _RotationSpeed = 120f;
		public float RotationSpeed
		{
			get { return _RotationSpeed; }
			set { _RotationSpeed = value; }
		}
		
		
		public float _RotationSmoothing = 0.1f;
		public virtual float RotationSmoothing
		{
			get { return _RotationSmoothing; }
			set { _RotationSmoothing = value; }
		}
		
		#endregion Properties
		
		
		#region Members
		
		protected bool mLinkRotation = false;
		
		protected float mYaw = 0f;
		protected float mYawTarget = 0f;
		protected float mYawVelocity = 0f;
		
		// Used to force a change if needed
		protected int mActiveForm = 0;

		#endregion Members


		#region Constructors
		
		public RifleAP_BasicIdle() : base()
		{
			_Category = EnumMotionCategories.IDLE;
			
			_Priority = 0;
			
#if UNITY_EDITOR
			if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "RifleAP_BasicIdle-SM"; }
#endif
		}
		
		
		public RifleAP_BasicIdle(MotionController rController) : base(rController)
		{
			_Category = EnumMotionCategories.IDLE;
			
			_Priority = 0;
			
#if UNITY_EDITOR
			if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "RifleAP_BasicIdle-SM"; }
#endif
		}

		#endregion Constructors


		#region MotionFunctions
		
		public override void Awake()
		{
			base.Awake();
		}
		
		
		#region Tests
		
		public override bool TestActivate()
		{
			// This is a catch all. If there are no motions found to match
			// the controller's state, we default to this motion.
			if (mMotionLayer.ActiveMotion == null)
			{
				// We used different timing based on the grounded flag
				if (mMotionController.IsGrounded)
				{
					return true;
				}
			}
			
			// Handle the disqualifiers
			if (!mIsStartable) { return false; }
			if (!mMotionController.IsGrounded) { return false; }
			if (mMotionController.State.InputMagnitudeTrend.Average != 0f) { return false; }
			
			return true;
		}
		
		
		public override bool TestUpdate()
		{
			// Exit if we hit an exit node
			if (mMotionLayer.AnimatorTransitionID == 0 &&
				mMotionController.State.AnimatorStates[mMotionLayer._AnimatorLayerIndex].StateInfo.IsTag("Exit"))
			{
				return false;
			}
			
			return true;
		}

		#endregion Tests
		
		
		public override bool Activate(MotionControllerMotion rPrevMotion)
		{
			// Reset the yaw info for smoothing
			mYaw = 0f;
			mYawTarget = 0f;
			mYawVelocity = 0f;
			mLinkRotation = false;
			
			// Trigger the transition
			mActiveForm = (_Form > 0 ? _Form : mMotionController.CurrentForm);
			mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, mActiveForm, mParameter, true);
			
			// Register this motion with the camera
			if (_RotateWithCamera && mMotionController.CameraRig is BaseCameraRig)
			{
				((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
				((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate += OnCameraUpdated;
			}
			
			// Finalize the activation
			return base.Activate(rPrevMotion);
		}
		
		
		public override void Deactivate()
		{
			mParameter = 0;
			
			// Register this motion with the camera
			if (mMotionController.CameraRig is BaseCameraRig)
			{
				((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
			}
			
			// Continue with the deactivation
			base.Deactivate();
		}
		
		
		public override void UpdateRootMotion(float rDeltaTime, int rUpdateIndex, ref Vector3 rMovement, ref Quaternion rRotation)
		{
			rMovement = Vector3.zero;
			rRotation = Quaternion.identity;
		}
		
		
		public override void Update(float rDeltaTime, int rUpdateIndex)
		{
			mVelocity = Vector3.zero;
			mMovement = Vector3.zero;
			mAngularVelocity = Vector3.zero;
			mRotation = Quaternion.identity;
			
			// Check if we're rotating with the camera
			bool lRotateWithCamera = false;
			if (_RotateWithCamera && mMotionController._CameraTransform != null)
			{
				if (mMotionController._InputSource.IsPressed(_ActionAlias))
				{
					lRotateWithCamera = true;
					
					// If we're meant to rotate with the camera (and OnCameraUpdate isn't already attached), do it here
					if (!(mMotionController.CameraRig is BaseCameraRig))
					{
						OnCameraUpdated(rDeltaTime, rUpdateIndex, null);
					}
				}
			}
			
			// If we're not rotating with the camera, rotate with the input
			if (!lRotateWithCamera && _RotateWithInput)
			{
				mLinkRotation = false;
				RotateUsingInput(rDeltaTime, ref mRotation);
			}
			
			// Force a style change if needed
			if (_Form <= 0 && mActiveForm != mMotionController.CurrentForm)
			{
				mActiveForm = mMotionController.CurrentForm;
				mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, mActiveForm, 0, true);
			}
		}

		#endregion MotionFunctions


		#region Functions
		
		private void RotateUsingInput(float rDeltaTime, ref Quaternion rRotation)
		{
			// If we don't have an input source, stop
			if (mMotionController._InputSource == null) { return; }
			
			// Determine this frame's rotation
			float lYawDelta = 0f;
			if (_RotateWithInput && mMotionController._InputSource.IsViewingActivated)
			{
				lYawDelta = mMotionController._InputSource.ViewX * _RotationSpeed * rDeltaTime;
			}
			
			mYawTarget = mYawTarget + lYawDelta;
			
			// Smooth the rotation
			lYawDelta = (_RotationSmoothing <= 0f ? mYawTarget : Mathf.SmoothDampAngle(mYaw, mYawTarget, ref mYawVelocity, _RotationSmoothing)) - mYaw;
			mYaw = mYaw + lYawDelta;
			
			// Use this frame's smoothed rotation
			if (lYawDelta != 0f)
			{
				rRotation = Quaternion.Euler(0f, lYawDelta, 0f);
			}
		}
		
		
		private void OnCameraUpdated(float rDeltaTime, int rUpdateIndex, BaseCameraRig rCamera)
		{
			if (!_RotateWithCamera) { return; }
			if (mMotionController._CameraTransform == null) { return; }
			
			float lToCameraAngle = Vector3Ext.HorizontalAngleTo(mMotionController._Transform.forward, mMotionController._CameraTransform.forward, mMotionController._Transform.up);
			if (!mLinkRotation && Mathf.Abs(lToCameraAngle) <= _RotationToCameraSpeed * rDeltaTime) { mLinkRotation = true; }
			
			if (!mLinkRotation)
			{
				float lRotationAngle = Mathf.Abs(lToCameraAngle);
				float lRotationSign = Mathf.Sign(lToCameraAngle);
				lToCameraAngle = lRotationSign * Mathf.Min(_RotationToCameraSpeed * rDeltaTime, lRotationAngle);
			}
			
			Quaternion lRotation = Quaternion.AngleAxis(lToCameraAngle, Vector3.up);
			mActorController.Yaw = mActorController.Yaw * lRotation;
			mActorController._Transform.rotation = mActorController.Tilt * mActorController.Yaw;
		}

		#endregion Functions


		#region EditorFunctions

#if UNITY_EDITOR
		
		public override bool OnInspectorGUI()
		{
			bool lIsDirty = false;
			
			if (EditorHelper.IntField("Form",
				"Sets the LXMotionForm animator property to determine the animation for the motion. If value is < 0, we use the Actor Core's 'Default Form' state.",
				Form, mMotionController))
			{
				lIsDirty = true;
				Form = EditorHelper.FieldIntValue;
			}
			
			GUILayout.Space(5f);
			
			if (EditorHelper.BoolField("Rotate With Camera",
				"Determines if we rotate to match the camera.",
				RotateWithCamera, mMotionController))
			{
				lIsDirty = true;
				RotateWithCamera = EditorHelper.FieldBoolValue;
			}
			
			if (EditorHelper.TextField("Rotate Action Alias",
				"Action alias determines if rotation is activated. This typically matches the input source's View Activator.",
				ActionAlias, mMotionController))
			{
				lIsDirty = true;
				ActionAlias = EditorHelper.FieldStringValue;
			}
			
			if (EditorHelper.FloatField("Rotation Speed",
				"Degrees per second to rotate to the camera's direction.",
				RotationToCameraSpeed, mMotionController))
			{
				lIsDirty = true;
				RotationToCameraSpeed = EditorHelper.FieldFloatValue;
			}
			
			GUILayout.Space(5f);
			
			if (EditorHelper.BoolField("Rotate With View Input",
				"Determines if we rotate based on user input (view x).",
				RotateWithInput, mMotionController))
			{
				lIsDirty = true;
				RotateWithInput = EditorHelper.FieldBoolValue;
			}
			
			if (EditorHelper.FloatField("Rotation Speed",
				"Degrees per second to rotate the actor.",
				RotationSpeed, mMotionController))
			{
				lIsDirty = true;
				RotationSpeed = EditorHelper.FieldFloatValue;
			}
			
			if (EditorHelper.FloatField("Rotation Smoothing",
				"Smoothing factor applied to rotation (0 disables).",
				RotationSmoothing, mMotionController))
			{
				lIsDirty = true;
				RotationSmoothing = EditorHelper.FieldFloatValue;
			}
			
			return lIsDirty;
		}

#endif

		#endregion EditorFunctions
		
		
		#region PackMethods
		
		public static string GroupName()
		{
			return "Basic";
		}

		#endregion PackMethods


		#region Auto-Generated

		// ************************************ START AUTO GENERATED ************************************

		/// <summary>
		/// These declarations go inside the class so you can test for which state
		/// and transitions are active. Testing hash values is much faster than strings.
		/// </summary>
		public int STATE_Start = -1;
		public int STATE_UnarmedIdlePose = -1;
		public int STATE_RifleIdlePose = -1;
		public int STATE_PistolIdlePose = -1;
		public int TRANS_AnyState_UnarmedIdlePose = -1;
		public int TRANS_EntryState_UnarmedIdlePose = -1;
		public int TRANS_AnyState_RifleIdlePose = -1;
		public int TRANS_EntryState_RifleIdlePose = -1;
		public int TRANS_AnyState_PistolIdlePose = -1;
		public int TRANS_EntryState_PistolIdlePose = -1;

		/// <summary>
		/// Determines if we're using auto-generated code
		/// </summary>
		public override bool HasAutoGeneratedCode
		{
			get { return true; }
		}

		/// <summary>
		/// Used to determine if the actor is in one of the states for this motion
		/// </summary>
		/// <returns></returns>
		public override bool IsInMotionState
		{
			get
			{
				int lStateID = mMotionLayer._AnimatorStateID;
				int lTransitionID = mMotionLayer._AnimatorTransitionID;

				if (lTransitionID == 0)
				{
					if (lStateID == STATE_Start) { return true; }
					if (lStateID == STATE_UnarmedIdlePose) { return true; }
					if (lStateID == STATE_RifleIdlePose) { return true; }
					if (lStateID == STATE_PistolIdlePose) { return true; }
				}

				if (lTransitionID == TRANS_AnyState_UnarmedIdlePose) { return true; }
				if (lTransitionID == TRANS_EntryState_UnarmedIdlePose) { return true; }
				if (lTransitionID == TRANS_AnyState_RifleIdlePose) { return true; }
				if (lTransitionID == TRANS_EntryState_RifleIdlePose) { return true; }
				if (lTransitionID == TRANS_AnyState_PistolIdlePose) { return true; }
				if (lTransitionID == TRANS_EntryState_PistolIdlePose) { return true; }
				return false;
			}
		}

		/// <summary>
		/// Used to determine if the actor is in one of the states for this motion
		/// </summary>
		/// <returns></returns>
		public override bool IsMotionState(int rStateID)
		{
			if (rStateID == STATE_Start) { return true; }
			if (rStateID == STATE_UnarmedIdlePose) { return true; }
			if (rStateID == STATE_RifleIdlePose) { return true; }
			if (rStateID == STATE_PistolIdlePose) { return true; }
			return false;
		}

		/// <summary>
		/// Used to determine if the actor is in one of the states for this motion
		/// </summary>
		/// <returns></returns>
		public override bool IsMotionState(int rStateID, int rTransitionID)
		{
			if (rTransitionID == 0)
			{
				if (rStateID == STATE_Start) { return true; }
				if (rStateID == STATE_UnarmedIdlePose) { return true; }
				if (rStateID == STATE_RifleIdlePose) { return true; }
				if (rStateID == STATE_PistolIdlePose) { return true; }
			}

			if (rTransitionID == TRANS_AnyState_UnarmedIdlePose) { return true; }
			if (rTransitionID == TRANS_EntryState_UnarmedIdlePose) { return true; }
			if (rTransitionID == TRANS_AnyState_RifleIdlePose) { return true; }
			if (rTransitionID == TRANS_EntryState_RifleIdlePose) { return true; }
			if (rTransitionID == TRANS_AnyState_PistolIdlePose) { return true; }
			if (rTransitionID == TRANS_EntryState_PistolIdlePose) { return true; }
			return false;
		}

		/// <summary>
		/// Preprocess any animator data so the motion can use it later
		/// </summary>
		public override void LoadAnimatorData()
		{
			string lLayer = mMotionController.Animator.GetLayerName(mMotionLayer._AnimatorLayerIndex);
			TRANS_AnyState_UnarmedIdlePose = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".RifleAP_BasicIdle-SM.Unarmed Idle Pose");
			TRANS_EntryState_UnarmedIdlePose = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".RifleAP_BasicIdle-SM.Unarmed Idle Pose");
			TRANS_AnyState_RifleIdlePose = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".RifleAP_BasicIdle-SM.Rifle Idle Pose");
			TRANS_EntryState_RifleIdlePose = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".RifleAP_BasicIdle-SM.Rifle Idle Pose");
			TRANS_AnyState_PistolIdlePose = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".RifleAP_BasicIdle-SM.PistolIdlePose");
			TRANS_EntryState_PistolIdlePose = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".RifleAP_BasicIdle-SM.PistolIdlePose");
			STATE_Start = mMotionController.AddAnimatorName("" + lLayer + ".Start");
			STATE_UnarmedIdlePose = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicIdle-SM.Unarmed Idle Pose");
			STATE_RifleIdlePose = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicIdle-SM.Rifle Idle Pose");
			STATE_PistolIdlePose = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicIdle-SM.PistolIdlePose");
		}

#if UNITY_EDITOR

		/// <summary>
		/// New way to create sub-state machines without destroying what exists first.
		/// </summary>
		protected override void CreateStateMachine()
		{
			int rLayerIndex = mMotionLayer._AnimatorLayerIndex;
			MotionController rMotionController = mMotionController;

			UnityEditor.Animations.AnimatorController lController = null;

			Animator lAnimator = rMotionController.Animator;
			if (lAnimator == null) { lAnimator = rMotionController.gameObject.GetComponent<Animator>(); }
			if (lAnimator != null) { lController = lAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController; }
			if (lController == null) { return; }

			while (lController.layers.Length <= rLayerIndex)
			{
				UnityEditor.Animations.AnimatorControllerLayer lNewLayer = new UnityEditor.Animations.AnimatorControllerLayer();
				lNewLayer.name = "Layer " + (lController.layers.Length + 1);
				lNewLayer.stateMachine = new UnityEditor.Animations.AnimatorStateMachine();
				lController.AddLayer(lNewLayer);
			}

			UnityEditor.Animations.AnimatorControllerLayer lLayer = lController.layers[rLayerIndex];

			UnityEditor.Animations.AnimatorStateMachine lLayerStateMachine = lLayer.stateMachine;

			UnityEditor.Animations.AnimatorStateMachine lSSM_N317128 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "RifleAP_BasicIdle-SM");
			if (lSSM_N317128 == null) { lSSM_N317128 = lLayerStateMachine.AddStateMachine("RifleAP_BasicIdle-SM", new Vector3(-190, -950, 0)); }

			UnityEditor.Animations.AnimatorState lState_N317122 = MotionControllerMotion.EditorFindState(lSSM_N317128, "Unarmed Idle Pose");
			if (lState_N317122 == null) { lState_N317122 = lSSM_N317128.AddState("Unarmed Idle Pose", new Vector3(312, 84, 0)); }
			lState_N317122.speed = 1f;
			lState_N317122.mirror = false;
			lState_N317122.tag = "";
			lState_N317122.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdlePose");

			UnityEditor.Animations.AnimatorState lState_N317124 = MotionControllerMotion.EditorFindState(lSSM_N317128, "Rifle Idle Pose");
			if (lState_N317124 == null) { lState_N317124 = lSSM_N317128.AddState("Rifle Idle Pose", new Vector3(312, 324, 0)); }
			lState_N317124.speed = 1f;
			lState_N317124.mirror = false;
			lState_N317124.tag = "";
			lState_N317124.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_Idle_Pose");

			UnityEditor.Animations.AnimatorState lState_N317126 = MotionControllerMotion.EditorFindState(lSSM_N317128, "PistolIdlePose");
			if (lState_N317126 == null) { lState_N317126 = lSSM_N317128.AddState("PistolIdlePose", new Vector3(312, 396, 0)); }
			lState_N317126.speed = 1f;
			lState_N317126.mirror = false;
			lState_N317126.tag = "";
			lState_N317126.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/PistolAnimsetPro/Animations/PistolAnimsetPro.fbx", "Pistol_Idle_Pose");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N317396 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N317122, 0);
			if (lAnyTransition_N317396 == null) { lAnyTransition_N317396 = lLayerStateMachine.AddAnyStateTransition(lState_N317122); }
			lAnyTransition_N317396.isExit = false;
			lAnyTransition_N317396.hasExitTime = false;
			lAnyTransition_N317396.hasFixedDuration = true;
			lAnyTransition_N317396.exitTime = 0.75f;
			lAnyTransition_N317396.duration = 0f;
			lAnyTransition_N317396.offset = 0f;
			lAnyTransition_N317396.mute = false;
			lAnyTransition_N317396.solo = false;
			lAnyTransition_N317396.canTransitionToSelf = true;
			lAnyTransition_N317396.orderedInterruption = true;
			lAnyTransition_N317396.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_N317396.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N317396.RemoveCondition(lAnyTransition_N317396.conditions[i]); }
			lAnyTransition_N317396.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73000f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_N317396.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_N317396.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N317444 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N317124, 0);
			if (lAnyTransition_N317444 == null) { lAnyTransition_N317444 = lLayerStateMachine.AddAnyStateTransition(lState_N317124); }
			lAnyTransition_N317444.isExit = false;
			lAnyTransition_N317444.hasExitTime = false;
			lAnyTransition_N317444.hasFixedDuration = true;
			lAnyTransition_N317444.exitTime = 0.75f;
			lAnyTransition_N317444.duration = 0.25f;
			lAnyTransition_N317444.offset = 0f;
			lAnyTransition_N317444.mute = false;
			lAnyTransition_N317444.solo = false;
			lAnyTransition_N317444.canTransitionToSelf = true;
			lAnyTransition_N317444.orderedInterruption = true;
			lAnyTransition_N317444.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_N317444.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N317444.RemoveCondition(lAnyTransition_N317444.conditions[i]); }
			lAnyTransition_N317444.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73000f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_N317444.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 500f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_N317444.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N317492 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N317126, 0);
			if (lAnyTransition_N317492 == null) { lAnyTransition_N317492 = lLayerStateMachine.AddAnyStateTransition(lState_N317126); }
			lAnyTransition_N317492.isExit = false;
			lAnyTransition_N317492.hasExitTime = false;
			lAnyTransition_N317492.hasFixedDuration = true;
			lAnyTransition_N317492.exitTime = 0.75f;
			lAnyTransition_N317492.duration = 0f;
			lAnyTransition_N317492.offset = 0f;
			lAnyTransition_N317492.mute = false;
			lAnyTransition_N317492.solo = false;
			lAnyTransition_N317492.canTransitionToSelf = true;
			lAnyTransition_N317492.orderedInterruption = true;
			lAnyTransition_N317492.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_N317492.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N317492.RemoveCondition(lAnyTransition_N317492.conditions[i]); }
			lAnyTransition_N317492.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73000f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_N317492.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 550f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_N317492.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");


			// Run any post processing after creating the state machine
			OnStateMachineCreated();
		}

#endif

		// ************************************ END AUTO GENERATED ************************************

		#endregion


		#region Definition

		/// <summary>
		/// New way to create sub-state machines without destroying what exists first.
		/// </summary>
		public static void ExtendBasicMotion(MotionController rMotionController, int rLayerIndex)
		{
			UnityEditor.Animations.AnimatorController lController = null;

			Animator lAnimator = rMotionController.Animator;
			if (lAnimator == null) { lAnimator = rMotionController.gameObject.GetComponent<Animator>(); }
			if (lAnimator != null) { lController = lAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController; }
			if (lController == null) { return; }

			while (lController.layers.Length <= rLayerIndex)
			{
				UnityEditor.Animations.AnimatorControllerLayer lNewLayer = new UnityEditor.Animations.AnimatorControllerLayer();
				lNewLayer.name = "Layer " + (lController.layers.Length + 1);
				lNewLayer.stateMachine = new UnityEditor.Animations.AnimatorStateMachine();
				lController.AddLayer(lNewLayer);
			}

			UnityEditor.Animations.AnimatorControllerLayer lLayer = lController.layers[rLayerIndex];

			UnityEditor.Animations.AnimatorStateMachine lLayerStateMachine = lLayer.stateMachine;

			UnityEditor.Animations.AnimatorStateMachine lSSM_N317128 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "RifleAP_BasicIdle-SM");
			if (lSSM_N317128 == null) { lSSM_N317128 = lLayerStateMachine.AddStateMachine("RifleAP_BasicIdle-SM", new Vector3(-190, -950, 0)); }

			UnityEditor.Animations.AnimatorState lState_N317122 = MotionControllerMotion.EditorFindState(lSSM_N317128, "Unarmed Idle Pose");
			if (lState_N317122 == null) { lState_N317122 = lSSM_N317128.AddState("Unarmed Idle Pose", new Vector3(312, 84, 0)); }
			lState_N317122.speed = 1f;
			lState_N317122.mirror = false;
			lState_N317122.tag = "";
			lState_N317122.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdlePose");

			UnityEditor.Animations.AnimatorState lState_N317124 = MotionControllerMotion.EditorFindState(lSSM_N317128, "Rifle Idle Pose");
			if (lState_N317124 == null) { lState_N317124 = lSSM_N317128.AddState("Rifle Idle Pose", new Vector3(312, 324, 0)); }
			lState_N317124.speed = 1f;
			lState_N317124.mirror = false;
			lState_N317124.tag = "";
			lState_N317124.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_Idle_Pose");

			UnityEditor.Animations.AnimatorState lState_N317126 = MotionControllerMotion.EditorFindState(lSSM_N317128, "PistolIdlePose");
			if (lState_N317126 == null) { lState_N317126 = lSSM_N317128.AddState("PistolIdlePose", new Vector3(312, 396, 0)); }
			lState_N317126.speed = 1f;
			lState_N317126.mirror = false;
			lState_N317126.tag = "";
			lState_N317126.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/PistolAnimsetPro/Animations/PistolAnimsetPro.fbx", "Pistol_Idle_Pose");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N317396 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N317122, 0);
			if (lAnyTransition_N317396 == null) { lAnyTransition_N317396 = lLayerStateMachine.AddAnyStateTransition(lState_N317122); }
			lAnyTransition_N317396.isExit = false;
			lAnyTransition_N317396.hasExitTime = false;
			lAnyTransition_N317396.hasFixedDuration = true;
			lAnyTransition_N317396.exitTime = 0.75f;
			lAnyTransition_N317396.duration = 0f;
			lAnyTransition_N317396.offset = 0f;
			lAnyTransition_N317396.mute = false;
			lAnyTransition_N317396.solo = false;
			lAnyTransition_N317396.canTransitionToSelf = true;
			lAnyTransition_N317396.orderedInterruption = true;
			lAnyTransition_N317396.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_N317396.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N317396.RemoveCondition(lAnyTransition_N317396.conditions[i]); }
			lAnyTransition_N317396.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73000f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_N317396.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_N317396.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N317444 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N317124, 0);
			if (lAnyTransition_N317444 == null) { lAnyTransition_N317444 = lLayerStateMachine.AddAnyStateTransition(lState_N317124); }
			lAnyTransition_N317444.isExit = false;
			lAnyTransition_N317444.hasExitTime = false;
			lAnyTransition_N317444.hasFixedDuration = true;
			lAnyTransition_N317444.exitTime = 0.75f;
			lAnyTransition_N317444.duration = 0.25f;
			lAnyTransition_N317444.offset = 0f;
			lAnyTransition_N317444.mute = false;
			lAnyTransition_N317444.solo = false;
			lAnyTransition_N317444.canTransitionToSelf = true;
			lAnyTransition_N317444.orderedInterruption = true;
			lAnyTransition_N317444.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_N317444.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N317444.RemoveCondition(lAnyTransition_N317444.conditions[i]); }
			lAnyTransition_N317444.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73000f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_N317444.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 500f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_N317444.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N317492 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N317126, 0);
			if (lAnyTransition_N317492 == null) { lAnyTransition_N317492 = lLayerStateMachine.AddAnyStateTransition(lState_N317126); }
			lAnyTransition_N317492.isExit = false;
			lAnyTransition_N317492.hasExitTime = false;
			lAnyTransition_N317492.hasFixedDuration = true;
			lAnyTransition_N317492.exitTime = 0.75f;
			lAnyTransition_N317492.duration = 0f;
			lAnyTransition_N317492.offset = 0f;
			lAnyTransition_N317492.mute = false;
			lAnyTransition_N317492.solo = false;
			lAnyTransition_N317492.canTransitionToSelf = true;
			lAnyTransition_N317492.orderedInterruption = true;
			lAnyTransition_N317492.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_N317492.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N317492.RemoveCondition(lAnyTransition_N317492.conditions[i]); }
			lAnyTransition_N317492.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73000f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_N317492.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 550f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_N317492.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

		}

		#endregion Definition

	}
}
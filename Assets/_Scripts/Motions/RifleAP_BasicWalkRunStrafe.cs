using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Combat;
using com.ootii.Cameras;
using com.ootii.Geometry;
using com.ootii.Helpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WildWalrus.Actors.AnimationControllers
{
	[MotionName("Rifle AP Basic Walk Run Strafe")]
	[MotionDescription("Kubold's RIfle Anim Set Pro. Shooter game style movement that can be expanded. Uses no transitions.")]
	public class RifleAP_BasicWalkRunStrafe : MotionControllerMotion, IWalkRunMotion, IStrafeMotion
	{
		#region MotionPhases
		
		public int PHASE_UNKNOWN = 0;
		public int PHASE_START = 73100;
		public int PHASE_STOP = 73105;

		#endregion MotionPhases


		#region IWalkRunMotion
		
		public virtual bool IsRunActive
		{
			get
			{
				if (mMotionController.TargetNormalizedSpeed > 0f && mMotionController.TargetNormalizedSpeed <= 0.5f) { return false; }
				if (mMotionController._InputSource == null) { return _DefaultToRun; }
				return ((_DefaultToRun && !mMotionController._InputSource.IsPressed(_ActionAlias)) || (!_DefaultToRun && mMotionController._InputSource.IsPressed(_ActionAlias)));
			}
		}
		
		
		private bool mStartInMove = false;
		public bool StartInMove
		{
			get { return mStartInMove; }
			set { mStartInMove = value; }
		}
		
		
		private bool mStartInWalk = false;
		public bool StartInWalk
		{
			get { return mStartInWalk; }
			set { mStartInWalk = value; }
		}
		
		
		private bool mStartInRun = false;
		public bool StartInRun
		{
			get { return mStartInRun; }
			set { mStartInRun = value; }
		}
		
		
		public bool _DefaultToRun = false;
		public bool DefaultToRun
		{
			get { return _DefaultToRun; }
			set { _DefaultToRun = value; }
		}

		#endregion IWalkRunMotion


		#region MotionProperties

		public override bool VerifyTransition
		{
			get { return false; }
		}

		#endregion MotionProperties


		#region Properties

		public bool _RequireTarget = true;
		public bool RequireTarget
		{
			get { return _RequireTarget; }
			set { _RequireTarget = value; }
		}
		
		
		public string _ActivationAlias = "";
		public string ActivationAlias
		{
			get { return _ActivationAlias; }
			set { _ActivationAlias = value; }
		}
		

		public string _ActorStances = "";
		public string ActorStances
		{
			get { return _ActorStances; }
			
			set
			{
				_ActorStances = value;
				
				if (_ActorStances.Length == 0)
				{
					if (mActorStances != null)
					{
						mActorStances.Clear();
					}
				}
				else
				{
					if (mActorStances == null) { mActorStances = new List<int>(); }
					mActorStances.Clear();
					
					int lStanceID = 0;
					string[] lStanceIDs = _ActorStances.Split(',');
					for (int i = 0; i < lStanceIDs.Length; i++)
					{
						if (int.TryParse(lStanceIDs[i], out lStanceID))
						{
							if (!mActorStances.Contains(lStanceID))
							{
								mActorStances.Add(lStanceID);
							}
						}
					}
				}
			}
		}
		
		
		public float _WalkSpeed = 0f;
		public virtual float WalkSpeed
		{
			get { return _WalkSpeed; }
			set { _WalkSpeed = value; }
		}
		
		
		public float _RunSpeed = 0f;
		public virtual float RunSpeed
		{
			get { return _RunSpeed; }
			set { _RunSpeed = value; }
		}
		
		
		public bool _RotateWithInput = false;
		public bool RotateWithInput
		{
			get { return _RotateWithInput; }
			
			set
			{
				_RotateWithInput = value;
				if (_RotateWithInput) { _RotateWithCamera = false; }
			}
		}
		
		
		public bool _RotateWithCamera = true;
		public bool RotateWithCamera
		{
			get { return _RotateWithCamera; }
			set
			{
				_RotateWithCamera = value;
				if (_RotateWithCamera) { _RotateWithInput = false; }
			}
		}
		
		
		public float _RotationSpeed = 360f;
		public float RotationSpeed
		{
			get { return _RotationSpeed; }
			set { _RotationSpeed = value; }
		}
		
		
		public int _SmoothingSamples = 10;
		public int SmoothingSamples
		{
			get { return _SmoothingSamples; }
			
			set
			{
				_SmoothingSamples = value;
				
				mInputX.SampleCount = _SmoothingSamples;
				mInputY.SampleCount = _SmoothingSamples;
				mInputMagnitude.SampleCount = _SmoothingSamples;
			}
		}
		
		
		protected ICombatant mCombatant = null;
		public ICombatant Combatant
		{
			get { return mCombatant; }
			set { mCombatant = value; }
		}

		#endregion Properties


		#region Members
		
		[SerializeField]
		protected List<int> mActorStances = new List<int>();
		
		protected bool mLinkRotation = false;
		
		protected float mYaw = 0f;
		protected float mYawTarget = 0f;
		protected float mYawVelocity = 0f;
		
		protected FloatValue mInputX = new FloatValue(0f, 10);
		protected FloatValue mInputY = new FloatValue(0f, 10);
		protected FloatValue mInputMagnitude = new FloatValue(0f, 15);
		
		protected float mIdleTime = 0f;
		
		protected bool mIsRotationLocked = false;
		
		protected int mActiveForm = 0;

		#endregion Members


		#region Constructors
		
		public RifleAP_BasicWalkRunStrafe() : base()
		{
			_Category = EnumMotionCategories.WALK;
			
			_Priority = 6;
			_ActionAlias = "Run";
			_Form = -1;
			
#if UNITY_EDITOR
			if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "RifleAP_BasicWalkRunStrafe-SM"; }
#endif
		}
		
		
		public RifleAP_BasicWalkRunStrafe(MotionController rController) : base(rController)
		{
			_Category = EnumMotionCategories.WALK;
			
			_Priority = 6;
			_ActionAlias = "Run";
			_Form = -1;
			
#if UNITY_EDITOR
			if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "RifleAP_BasicWalkRunStrafe-SM"; }
#endif
		}

		#endregion Constructors


		#region MotionFunctions
		
		public override void Awake()
		{
			base.Awake();
			
			// Initialize the smoothing variables
			SmoothingSamples = _SmoothingSamples;
			
			// Extract the combatant if we can
			mCombatant = mMotionController.gameObject.GetComponent<ICombatant>();
		}


		#region Tests
		
		public override bool TestActivate()
		{
			if (!mIsStartable) { return false; }
			if (!mMotionController.IsGrounded) { return false; }
			
			// We need some minimum input before we can move
			if (mMotionController.State.InputMagnitudeTrend.Value < 0.49f)
			{
				return false;
			}
			
			bool lIsFree = (!_RequireTarget && _ActivationAlias.Length == 0);
			bool lIsTargetValid = (_RequireTarget && mCombatant != null && mCombatant.IsTargetLocked);
			bool lIsAliasValid = (_ActivationAlias.Length > 0 && mMotionController._InputSource != null && mMotionController._InputSource.IsPressed(_ActivationAlias));
			
			if (!lIsFree && !lIsTargetValid && !lIsAliasValid)
			{
				return false;
			}
			
			// Ensure we're in a valid stance
			if (mActorStances != null && mActorStances.Count > 0)
			{
				if (!mActorStances.Contains(mMotionController.Stance))
				{
					return false;
				}
			}
			
			// We're good to move
			return true;
		}
		
		
		public override bool TestUpdate()
		{
			if (mIsActivatedFrame) { return true; }
			if (!mMotionController.IsGrounded) { return false; }
			
			bool lIsFree = (!_RequireTarget && _ActivationAlias.Length == 0);
			bool lIsTargetValid = (_RequireTarget && mCombatant != null && mCombatant.IsTargetLocked);
			bool lIsAliasValid = (_ActivationAlias.Length > 0 && mMotionController._InputSource != null && mMotionController._InputSource.IsPressed(_ActivationAlias));
			
			if (!lIsFree && !lIsTargetValid && !lIsAliasValid)
			{
				// If we're moving, but transitioning to another WR motion, force the input
				if (mMotionController.State.InputMagnitudeTrend.Value > 0.2f && _ActivationAlias.Length > 0)
				{
					mMotionController.ForcedInput.x = mInputX.Average;
					mMotionController.ForcedInput.y = mInputY.Average;
				}
				
				return false;
			}
			
			// If we're down to no movement, we can exit
			if (mInputMagnitude.Average == 0f)
			{
				// However, add a small delay to ensure we're not coming back
				mIdleTime = mIdleTime + Time.deltaTime;
				if (mIdleTime > 0.2f)
				{
					return false;
				}
			}
			else
			{
				mIdleTime = 0f;
			}
			
			// Ensure we're in a valid stance
			if (mActorStances != null && mActorStances.Count > 0)
			{
				if (!mActorStances.Contains(mMotionController.Stance))
				{
					return false;
				}
			}
			
			// Stay in
			return true;
		}
		
		
		public override bool TestInterruption(MotionControllerMotion rMotion)
		{
			// Since we're dealing with a blend tree, keep the value until the transition completes
			mMotionController.ForcedInput.x = mInputX.Average;
			mMotionController.ForcedInput.y = mInputY.Average;
			
			return true;
		}

		#endregion Tests
		
		
		public override bool Activate(MotionControllerMotion rPrevMotion)
		{
			mIdleTime = 0f;
			mLinkRotation = false;
			
			mInputX.Clear();
			mInputY.Clear();
			mInputMagnitude.Clear();
			
			// Used to make the sample frame rate independent
			float lFPSPercent = Mathf.Clamp((0.0166f / Time.deltaTime), 0.5f, 5.0f);
			int lSmoothingSamples = (int)(_SmoothingSamples * lFPSPercent);
			mInputX.SampleCount = lSmoothingSamples;
			mInputY.SampleCount = lSmoothingSamples;
			mInputMagnitude.SampleCount = lSmoothingSamples;
			
			// Update the max speed based on our animation
			mMotionController.MaxSpeed = 5.668f;
			
			// Determine how we'll start our animation
			mActiveForm = (_Form >= 0 ? _Form : mMotionController.CurrentForm);
			mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, mActiveForm, Parameter, true);
			
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
			// Clear out the start
			mStartInRun = false;
			mStartInWalk = false;
			
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
			rRotation = Quaternion.identity;
			
			// Override root motion if we're meant to
			float lMovementSpeed = (IsRunActive ? _RunSpeed : _WalkSpeed);
			if (lMovementSpeed > 0f)
			{
				rMovement.x = mMotionController.State.InputX;
				rMovement.y = 0f;
				rMovement.z = mMotionController.State.InputY;
				rMovement = rMovement.normalized * (lMovementSpeed * rDeltaTime);
			}
			// Handle movement manually
			else
			{
				// Get rid of root-motion that is not aligned with our input
				if (mMotionController._InputSource != null)
				{
					lMovementSpeed = rMovement.magnitude;
					
					rMovement.x = mMotionController.State.InputX;
					rMovement.y = 0f;
					rMovement.z = mMotionController.State.InputY;
					rMovement = rMovement.normalized * lMovementSpeed;
				}
			}
		}
		
		
		public override void Update(float rDeltaTime, int rUpdateIndex)
		{
			mMovement = Vector3.zero;
			mRotation = Quaternion.identity;
			
			// Smooth the input so we don't start and stop immediately in the blend tree.
			SmoothInput();
			
			if (mMotionLayer._AnimatorTransitionID == 0)
			{
				int lHash = Animator.StringToHash("Base Layer.RifleAP_BasicWalkRunStrafe-SM.Unarmed BlendTree");
				if (mMotionController.State.AnimatorStates[mMotionLayer._AnimatorLayerIndex].StateInfo.fullPathHash == lHash)
				{
					mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, 0);
				}
			}
			
			// Rotate using the target direction
			if (_RequireTarget && mCombatant != null && mCombatant.IsTargetLocked)
			{
				if (!mCombatant.ForceActorRotation)
				{
					Vector3 lDirection = (mCombatant.Target.position - mMotionController._Transform.position).normalized;
					RotateToDirection(lDirection, _RotationSpeed, rDeltaTime, ref mRotation);
				}
			}
			// If set, rotate most of the way to the camera direction
			else if (_RotateWithCamera && mMotionController._CameraTransform != null)
			{
				RotateToDirection(mMotionController._CameraTransform.forward, _RotationSpeed, rDeltaTime, ref mRotation);
			}
			// Otherwise, rotate using input
			else if (!_RotateWithCamera)
			{
				if (_RotateWithInput) { RotateUsingInput(_RotationSpeed, rDeltaTime, ref mRotation); }
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
		
		protected void SmoothInput()
		{
			MotionState lState = mMotionController.State;
			
			// Convert the input to radial so we deal with keyboard and gamepad input the same.
			float lInputMax = (IsRunActive ? 1f : 0.5f);
			
			float lInputX = Mathf.Clamp(lState.InputX, -lInputMax, lInputMax);
			float lInputY = Mathf.Clamp(lState.InputY, -lInputMax, lInputMax);
			float lInputMagnitude = Mathf.Clamp(lState.InputMagnitudeTrend.Value, 0f, lInputMax);
			InputManagerHelper.ConvertToRadialInput(ref lInputX, ref lInputY, ref lInputMagnitude);
			
			// Smooth the input
			mInputX.Add(lInputX);
			mInputY.Add(lInputY);
			mInputMagnitude.Add(lInputMagnitude);
			
			// Modify the input values to add some lag
			mMotionController.State.InputX = mInputX.Average;
			mMotionController.State.InputY = mInputY.Average;
			mMotionController.State.InputMagnitudeTrend.Replace(mInputMagnitude.Average);
		}
		
		
		private void RotateUsingInput(float rSpeed, float rDeltaTime, ref Quaternion rRotation)
		{
			// If we don't have an input source, stop
			if (mMotionController._InputSource == null) { return; }
			
			// Determine this frame's rotation
			float lYawDelta = 0f;
			float lYawSmoothing = 0.1f;
			
			if (mMotionController._InputSource.IsViewingActivated)
			{
				lYawDelta = mMotionController._InputSource.ViewX * rSpeed * rDeltaTime;
			}
			
			mYawTarget = mYawTarget + lYawDelta;
			
			// Smooth the rotation
			lYawDelta = (lYawSmoothing <= 0f ? mYawTarget : Mathf.SmoothDampAngle(mYaw, mYawTarget, ref mYawVelocity, lYawSmoothing)) - mYaw;
			mYaw = mYaw + lYawDelta;
			
			// Use this frame's smoothed rotation
			if (lYawDelta != 0f)
			{
				rRotation = Quaternion.Euler(0f, lYawDelta, 0f);
			}
		}
		
		
		protected void RotateToDirection(Vector3 rForward, float rSpeed, float rDeltaTime, ref Quaternion rRotation)
		{
			// We do the inverse tilt so we calculate the rotation in "natural up" space vs. "actor up" space.
			Quaternion lInvTilt = QuaternionExt.FromToRotation(mMotionController._Transform.up, Vector3.up);
			
			// Forward direction of the actor in "natural up"
			Vector3 lActorForward = lInvTilt * mMotionController._Transform.forward;
			
			// Camera forward in "natural up"
			Vector3 lTargetForward = lInvTilt * rForward;
			
			// Ensure we don't exceed our rotation speed
			float lActorToCameraAngle = NumberHelper.GetHorizontalAngle(lActorForward, lTargetForward);
			if (rSpeed > 0f && Mathf.Abs(lActorToCameraAngle) > rSpeed * rDeltaTime)
			{
				lActorToCameraAngle = Mathf.Sign(lActorToCameraAngle) * rSpeed * rDeltaTime;
			}
			
			// We only want to do this is we're very very close to the desired angle. This will remove any stuttering
			rRotation = Quaternion.AngleAxis(lActorToCameraAngle, Vector3.up);
		}
		
		
		protected virtual void OnCameraUpdated(float rDeltaTime, int rUpdateIndex, BaseCameraRig rCamera)
		{
			if (!_RotateWithCamera) { return; }
			if (_RequireTarget && mCombatant != null && mCombatant.IsTargetLocked) { return; }
			if (mMotionController._CameraTransform == null) { return; }
			
			// Get out early if we we aren't modifying the view.
			if (mMotionController._InputSource != null && mMotionController._InputSource.ViewX == 0f) { return; }
			
			// We do the inverse tilt so we calculate the rotation in "natural up" space vs. "actor up" space.
			Quaternion lInvTilt = QuaternionExt.FromToRotation(mMotionController._Transform.up, Vector3.up);
			
			// Forward direction of the actor in "natural up"
			Vector3 lActorForward = lInvTilt * mMotionController._Transform.forward;
			
			// Camera forward in "natural up"
			Vector3 lCameraForward = lInvTilt * mMotionController._CameraTransform.forward;
			
			// Get the rotation angle to the camera
			float lActorToCameraAngle = NumberHelper.GetHorizontalAngle(lActorForward, lCameraForward);
			
			// Clear the link if we're out of rotation range
			if (Mathf.Abs(lActorToCameraAngle) > _RotationSpeed * rDeltaTime * 5f) { mIsRotationLocked = false; }
			
			// We only want to do this is we're very very close to the desired angle. This will remove any stuttering
			if (_RotationSpeed == 0f || mIsRotationLocked || Mathf.Abs(lActorToCameraAngle) < _RotationSpeed * rDeltaTime * 1f)
			{
				mIsRotationLocked = true;
				
				// Since we're after the camera update, we have to force the rotation outside the normal flow
				Quaternion lRotation = Quaternion.AngleAxis(lActorToCameraAngle, Vector3.up);
				mActorController.Yaw = mActorController.Yaw * lRotation;
				mActorController._Transform.rotation = mActorController.Tilt * mActorController.Yaw;
			}
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
			
			if (EditorHelper.BoolField("Activate With Target",
				"Determines if the motion will activate when the Combatant component has a target. If checked we will automatically rotate to the target as long as there is one.",
				RequireTarget, mMotionController))
			{
				lIsDirty = true;
				RequireTarget = EditorHelper.FieldBoolValue;
			}
			
			if (EditorHelper.TextField("Activation Alias",
				"If set, the action alias that will activate this motion when pressed.",
				ActivationAlias, mMotionController))
			{
				lIsDirty = true;
				ActivationAlias = EditorHelper.FieldStringValue;
			}
			
			if (EditorHelper.TextField("Valid Actor Stances",
				"Comma delimited list of stance IDs that the transition will work in. Leave empty to ignore this condition.",
				ActorStances, mMotionController))
			{
				lIsDirty = true;
				ActorStances = EditorHelper.FieldStringValue;
			}
			
			GUILayout.Space(5f);
			
			if (EditorHelper.BoolField("Default to Run",
				"Determines if the default is to run or walk.",
				DefaultToRun, mMotionController))
			{
				lIsDirty = true;
				DefaultToRun = EditorHelper.FieldBoolValue;
			}
			
			if (EditorHelper.TextField("Run Action Alias",
				"Action alias that triggers a run or walk (which ever is opposite the default).",
				ActionAlias, mMotionController))
			{
				lIsDirty = true;
				ActionAlias = EditorHelper.FieldStringValue;
			}
			
			GUILayout.Space(5f);
			
			if (EditorHelper.FloatField("Walk Speed",
				"Speed (units per second) to move when walking. Set to 0 to use root-motion.",
				WalkSpeed, mMotionController))
			{
				lIsDirty = true;
				WalkSpeed = EditorHelper.FieldFloatValue;
			}
			
			if (EditorHelper.FloatField("Run Speed",
				"Speed (units per second) to move when running. Set to 0 to use root-motion.",
				RunSpeed, mMotionController))
			{
				lIsDirty = true;
				RunSpeed = EditorHelper.FieldFloatValue;
			}
			
			GUILayout.Space(5f);
			
			if (EditorHelper.BoolField("Rotate With Input",
				"Determines if we rotate based on user input.",
				RotateWithInput, mMotionController))
			{
				lIsDirty = true;
				RotateWithInput = EditorHelper.FieldBoolValue;
			}
			
			if (EditorHelper.BoolField("Rotate With Camera",
				"Determines if we rotate to match the camera.",
				RotateWithCamera, mMotionController))
			{
				lIsDirty = true;
				RotateWithCamera = EditorHelper.FieldBoolValue;
			}
			
			if (EditorHelper.FloatField("Rotation Speed",
				"Degrees per second to rotate the actor.",
				RotationSpeed, mMotionController))
			{
				lIsDirty = true;
				RotationSpeed = EditorHelper.FieldFloatValue;
			}
			
			GUILayout.Space(5f);
			
			if (EditorHelper.IntField("Smoothing Samples",
				"Smoothing factor for input. The more samples the smoother, but the less responsive (0 disables).",
				SmoothingSamples, mMotionController))
			{
				lIsDirty = true;
				SmoothingSamples = EditorHelper.FieldIntValue;
			}
			
			return lIsDirty;
		}

#endif

		#endregion EditorFunctions


		#region Auto-Generated
		// ************************************ START AUTO GENERATED ************************************

		/// <summary>
		/// These declarations go inside the class so you can test for which state
		/// and transitions are active. Testing hash values is much faster than strings.
		/// </summary>
		public int STATE_Start = -1;
		public int STATE_UnarmedBlendTree = -1;
		public int STATE_RifleBlendTree = -1;
		public int STATE_PistolBlendTree = -1;
		public int TRANS_AnyState_RifleBlendTree = -1;
		public int TRANS_EntryState_RifleBlendTree = -1;
		public int TRANS_AnyState_PistolBlendTree = -1;
		public int TRANS_EntryState_PistolBlendTree = -1;
		public int TRANS_AnyState_UnarmedBlendTree = -1;
		public int TRANS_EntryState_UnarmedBlendTree = -1;

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
					if (lStateID == STATE_UnarmedBlendTree) { return true; }
					if (lStateID == STATE_RifleBlendTree) { return true; }
					if (lStateID == STATE_PistolBlendTree) { return true; }
				}

				if (lTransitionID == TRANS_AnyState_RifleBlendTree) { return true; }
				if (lTransitionID == TRANS_EntryState_RifleBlendTree) { return true; }
				if (lTransitionID == TRANS_AnyState_PistolBlendTree) { return true; }
				if (lTransitionID == TRANS_EntryState_PistolBlendTree) { return true; }
				if (lTransitionID == TRANS_AnyState_UnarmedBlendTree) { return true; }
				if (lTransitionID == TRANS_EntryState_UnarmedBlendTree) { return true; }
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
			if (rStateID == STATE_UnarmedBlendTree) { return true; }
			if (rStateID == STATE_RifleBlendTree) { return true; }
			if (rStateID == STATE_PistolBlendTree) { return true; }
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
				if (rStateID == STATE_UnarmedBlendTree) { return true; }
				if (rStateID == STATE_RifleBlendTree) { return true; }
				if (rStateID == STATE_PistolBlendTree) { return true; }
			}

			if (rTransitionID == TRANS_AnyState_RifleBlendTree) { return true; }
			if (rTransitionID == TRANS_EntryState_RifleBlendTree) { return true; }
			if (rTransitionID == TRANS_AnyState_PistolBlendTree) { return true; }
			if (rTransitionID == TRANS_EntryState_PistolBlendTree) { return true; }
			if (rTransitionID == TRANS_AnyState_UnarmedBlendTree) { return true; }
			if (rTransitionID == TRANS_EntryState_UnarmedBlendTree) { return true; }
			return false;
		}

		/// <summary>
		/// Preprocess any animator data so the motion can use it later
		/// </summary>
		public override void LoadAnimatorData()
		{
			string lLayer = mMotionController.Animator.GetLayerName(mMotionLayer._AnimatorLayerIndex);
			TRANS_AnyState_RifleBlendTree = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".RifleAP_BasicWalkRunStrafe-SM.Rifle BlendTree");
			TRANS_EntryState_RifleBlendTree = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".RifleAP_BasicWalkRunStrafe-SM.Rifle BlendTree");
			TRANS_AnyState_PistolBlendTree = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".RifleAP_BasicWalkRunStrafe-SM.Pistol BlendTree");
			TRANS_EntryState_PistolBlendTree = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".RifleAP_BasicWalkRunStrafe-SM.Pistol BlendTree");
			TRANS_AnyState_UnarmedBlendTree = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".RifleAP_BasicWalkRunStrafe-SM.Unarmed BlendTree");
			TRANS_EntryState_UnarmedBlendTree = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".RifleAP_BasicWalkRunStrafe-SM.Unarmed BlendTree");
			STATE_Start = mMotionController.AddAnimatorName("" + lLayer + ".Start");
			STATE_UnarmedBlendTree = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicWalkRunStrafe-SM.Unarmed BlendTree");
			STATE_RifleBlendTree = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicWalkRunStrafe-SM.Rifle BlendTree");
			STATE_PistolBlendTree = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicWalkRunStrafe-SM.Pistol BlendTree");
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

			UnityEditor.Animations.AnimatorStateMachine lSSM_N74384 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "RifleAP_BasicWalkRunStrafe-SM");
			if (lSSM_N74384 == null) { lSSM_N74384 = lLayerStateMachine.AddStateMachine("RifleAP_BasicWalkRunStrafe-SM", new Vector3(-190, -1010, 0)); }

			UnityEditor.Animations.AnimatorState lState_N74382 = MotionControllerMotion.EditorFindState(lSSM_N74384, "Unarmed BlendTree");
			if (lState_N74382 == null) { lState_N74382 = lSSM_N74384.AddState("Unarmed BlendTree", new Vector3(340, 20, 0)); }
			lState_N74382.speed = 1f;
			lState_N74382.mirror = false;
			lState_N74382.tag = "";

			UnityEditor.Animations.BlendTree lM_N74368 = MotionControllerMotion.EditorCreateBlendTree("Move Blend Tree", lController, rLayerIndex);
			lM_N74368.blendType = UnityEditor.Animations.BlendTreeType.Simple1D;
			lM_N74368.blendParameter = "InputMagnitude";
			lM_N74368.blendParameterY = "InputX";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
			lM_N74368.useAutomaticThresholds = false;
#endif
			lM_N74368.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdlePose"), 0f);

			UnityEditor.Animations.BlendTree lM_N74362 = MotionControllerMotion.EditorCreateBlendTree("WalkTree", lController, rLayerIndex);
			lM_N74362.blendType = UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;
			lM_N74362.blendParameter = "InputX";
			lM_N74362.blendParameterY = "InputY";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
			lM_N74362.useAutomaticThresholds = true;
#endif
			lM_N74362.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Walking/unity_WalkFWD_v2.fbx", "WalkForward"), new Vector2(0f, 0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74362_0_Children = lM_N74362.children;
			lM_N74362_0_Children[lM_N74362_0_Children.Length - 1].mirror = false;
			lM_N74362_0_Children[lM_N74362_0_Children.Length - 1].timeScale = 1.1f;
			lM_N74362.children = lM_N74362_0_Children;

			lM_N74362.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Walking/unity_SWalk_v2.fbx", "SWalkForwardRight"), new Vector2(0.35f, 0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74362_1_Children = lM_N74362.children;
			lM_N74362_1_Children[lM_N74362_1_Children.Length - 1].mirror = false;
			lM_N74362_1_Children[lM_N74362_1_Children.Length - 1].timeScale = 1.2f;
			lM_N74362.children = lM_N74362_1_Children;

			lM_N74362.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Walking/unity_SWalk_v2.fbx", "SWalkForwardLeft"), new Vector2(-0.35f, 0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74362_2_Children = lM_N74362.children;
			lM_N74362_2_Children[lM_N74362_2_Children.Length - 1].mirror = false;
			lM_N74362_2_Children[lM_N74362_2_Children.Length - 1].timeScale = 1.2f;
			lM_N74362.children = lM_N74362_2_Children;

			lM_N74362.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Walking/unity_SWalk_v2.fbx", "SWalkLeft"), new Vector2(-0.35f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74362_3_Children = lM_N74362.children;
			lM_N74362_3_Children[lM_N74362_3_Children.Length - 1].mirror = false;
			lM_N74362_3_Children[lM_N74362_3_Children.Length - 1].timeScale = 1.2f;
			lM_N74362.children = lM_N74362_3_Children;

			lM_N74362.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Walking/unity_SWalk_v2.fbx", "SWalkRight"), new Vector2(0.35f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74362_4_Children = lM_N74362.children;
			lM_N74362_4_Children[lM_N74362_4_Children.Length - 1].mirror = false;
			lM_N74362_4_Children[lM_N74362_4_Children.Length - 1].timeScale = 1.2f;
			lM_N74362.children = lM_N74362_4_Children;

			lM_N74362.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2Strafe_AllAngles.fbx", "WalkStrafeBackwardsLeft"), new Vector2(-0.35f, -0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74362_5_Children = lM_N74362.children;
			lM_N74362_5_Children[lM_N74362_5_Children.Length - 1].mirror = false;
			lM_N74362_5_Children[lM_N74362_5_Children.Length - 1].timeScale = 1.1f;
			lM_N74362.children = lM_N74362_5_Children;

			lM_N74362.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2Strafe_AllAngles.fbx", "WalkStrafeBackwardsRight"), new Vector2(0.35f, -0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74362_6_Children = lM_N74362.children;
			lM_N74362_6_Children[lM_N74362_6_Children.Length - 1].mirror = false;
			lM_N74362_6_Children[lM_N74362_6_Children.Length - 1].timeScale = 1.1f;
			lM_N74362.children = lM_N74362_6_Children;

			lM_N74362.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Walking/unity_BWalk.fbx", "WalkBackwards"), new Vector2(0f, -0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74362_7_Children = lM_N74362.children;
			lM_N74362_7_Children[lM_N74362_7_Children.Length - 1].mirror = false;
			lM_N74362_7_Children[lM_N74362_7_Children.Length - 1].timeScale = 1f;
			lM_N74362.children = lM_N74362_7_Children;

			lM_N74368.AddChild(lM_N74362, 0.5f);

			UnityEditor.Animations.BlendTree lM_N74360 = MotionControllerMotion.EditorCreateBlendTree("RunTree", lController, rLayerIndex);
			lM_N74360.blendType = UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;
			lM_N74360.blendParameter = "InputX";
			lM_N74360.blendParameterY = "InputY";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
			lM_N74360.useAutomaticThresholds = true;
#endif
			lM_N74360.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Running/RunForward_v2.fbx", "RunForward"), new Vector2(0f, 0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74360_0_Children = lM_N74360.children;
			lM_N74360_0_Children[lM_N74360_0_Children.Length - 1].mirror = false;
			lM_N74360_0_Children[lM_N74360_0_Children.Length - 1].timeScale = 1f;
			lM_N74360.children = lM_N74360_0_Children;

			lM_N74360.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeForwardRight"), new Vector2(0.7f, 0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74360_1_Children = lM_N74360.children;
			lM_N74360_1_Children[lM_N74360_1_Children.Length - 1].mirror = false;
			lM_N74360_1_Children[lM_N74360_1_Children.Length - 1].timeScale = 1.1f;
			lM_N74360.children = lM_N74360_1_Children;

			lM_N74360.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeForwardLeft"), new Vector2(-0.7f, 0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74360_2_Children = lM_N74360.children;
			lM_N74360_2_Children[lM_N74360_2_Children.Length - 1].mirror = false;
			lM_N74360_2_Children[lM_N74360_2_Children.Length - 1].timeScale = 1.1f;
			lM_N74360.children = lM_N74360_2_Children;

			lM_N74360.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeLeft"), new Vector2(-0.7f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74360_3_Children = lM_N74360.children;
			lM_N74360_3_Children[lM_N74360_3_Children.Length - 1].mirror = false;
			lM_N74360_3_Children[lM_N74360_3_Children.Length - 1].timeScale = 1f;
			lM_N74360.children = lM_N74360_3_Children;

			lM_N74360.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeRight"), new Vector2(0.7f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74360_4_Children = lM_N74360.children;
			lM_N74360_4_Children[lM_N74360_4_Children.Length - 1].mirror = false;
			lM_N74360_4_Children[lM_N74360_4_Children.Length - 1].timeScale = 1f;
			lM_N74360.children = lM_N74360_4_Children;

			lM_N74360.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeBackwardLeft"), new Vector2(-0.7f, -0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74360_5_Children = lM_N74360.children;
			lM_N74360_5_Children[lM_N74360_5_Children.Length - 1].mirror = false;
			lM_N74360_5_Children[lM_N74360_5_Children.Length - 1].timeScale = 1.1f;
			lM_N74360.children = lM_N74360_5_Children;

			lM_N74360.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeBackwardRight"), new Vector2(0.7f, -0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74360_6_Children = lM_N74360.children;
			lM_N74360_6_Children[lM_N74360_6_Children.Length - 1].mirror = false;
			lM_N74360_6_Children[lM_N74360_6_Children.Length - 1].timeScale = 1.1f;
			lM_N74360.children = lM_N74360_6_Children;

			lM_N74360.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Running/RunBackward.fbx", "RunBackwards"), new Vector2(0f, -0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74360_7_Children = lM_N74360.children;
			lM_N74360_7_Children[lM_N74360_7_Children.Length - 1].mirror = false;
			lM_N74360_7_Children[lM_N74360_7_Children.Length - 1].timeScale = 1f;
			lM_N74360.children = lM_N74360_7_Children;

			lM_N74368.AddChild(lM_N74360, 1f);
			lState_N74382.motion = lM_N74368;

			UnityEditor.Animations.AnimatorState lState_N74380 = MotionControllerMotion.EditorFindState(lSSM_N74384, "Rifle BlendTree");
			if (lState_N74380 == null) { lState_N74380 = lSSM_N74384.AddState("Rifle BlendTree", new Vector3(340, 240, 0)); }
			lState_N74380.speed = 1f;
			lState_N74380.mirror = false;
			lState_N74380.tag = "";

			UnityEditor.Animations.BlendTree lM_N74366 = MotionControllerMotion.EditorCreateBlendTree("Move Blend Tree", lController, rLayerIndex);
			lM_N74366.blendType = UnityEditor.Animations.BlendTreeType.Simple1D;
			lM_N74366.blendParameter = "InputMagnitude";
			lM_N74366.blendParameterY = "InputX";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
			lM_N74366.useAutomaticThresholds = false;
#endif
			lM_N74366.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_Idle_Single_Frame"), 0f);

			UnityEditor.Animations.BlendTree lM_N74364 = MotionControllerMotion.EditorCreateBlendTree("WalkTree", lController, rLayerIndex);
			lM_N74364.blendType = UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;
			lM_N74364.blendParameter = "InputX";
			lM_N74364.blendParameterY = "InputY";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
			lM_N74364.useAutomaticThresholds = true;
#endif
			lM_N74364.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_WalkFwdLoop"), new Vector2(0f, 0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74364_0_Children = lM_N74364.children;
			lM_N74364_0_Children[lM_N74364_0_Children.Length - 1].mirror = false;
			lM_N74364_0_Children[lM_N74364_0_Children.Length - 1].timeScale = 1.1f;
			lM_N74364.children = lM_N74364_0_Children;

			lM_N74364.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro_Diagonals.fbx", "Rifle_StrafeRight45Loop"), new Vector2(0.35f, 0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74364_1_Children = lM_N74364.children;
			lM_N74364_1_Children[lM_N74364_1_Children.Length - 1].mirror = false;
			lM_N74364_1_Children[lM_N74364_1_Children.Length - 1].timeScale = 1.2f;
			lM_N74364.children = lM_N74364_1_Children;

			lM_N74364.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_StrafeLeft45Loop"), new Vector2(-0.35f, 0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74364_2_Children = lM_N74364.children;
			lM_N74364_2_Children[lM_N74364_2_Children.Length - 1].mirror = false;
			lM_N74364_2_Children[lM_N74364_2_Children.Length - 1].timeScale = 1.2f;
			lM_N74364.children = lM_N74364_2_Children;

			lM_N74364.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_StrafeLeftLoop"), new Vector2(-0.35f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74364_3_Children = lM_N74364.children;
			lM_N74364_3_Children[lM_N74364_3_Children.Length - 1].mirror = false;
			lM_N74364_3_Children[lM_N74364_3_Children.Length - 1].timeScale = 1.2f;
			lM_N74364.children = lM_N74364_3_Children;

			lM_N74364.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_StrafeRightLoop"), new Vector2(0.35f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74364_4_Children = lM_N74364.children;
			lM_N74364_4_Children[lM_N74364_4_Children.Length - 1].mirror = false;
			lM_N74364_4_Children[lM_N74364_4_Children.Length - 1].timeScale = 1.2f;
			lM_N74364.children = lM_N74364_4_Children;

			lM_N74364.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro_Diagonals.fbx", "Rifle_StrafeLeft135Loop"), new Vector2(-0.35f, -0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74364_5_Children = lM_N74364.children;
			lM_N74364_5_Children[lM_N74364_5_Children.Length - 1].mirror = false;
			lM_N74364_5_Children[lM_N74364_5_Children.Length - 1].timeScale = 1.1f;
			lM_N74364.children = lM_N74364_5_Children;

			lM_N74364.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_StrafeRight135Loop"), new Vector2(0.35f, -0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74364_6_Children = lM_N74364.children;
			lM_N74364_6_Children[lM_N74364_6_Children.Length - 1].mirror = false;
			lM_N74364_6_Children[lM_N74364_6_Children.Length - 1].timeScale = 1.1f;
			lM_N74364.children = lM_N74364_6_Children;

			lM_N74364.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_WalkBwdLoop"), new Vector2(0f, -0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74364_7_Children = lM_N74364.children;
			lM_N74364_7_Children[lM_N74364_7_Children.Length - 1].mirror = false;
			lM_N74364_7_Children[lM_N74364_7_Children.Length - 1].timeScale = 1f;
			lM_N74364.children = lM_N74364_7_Children;

			lM_N74366.AddChild(lM_N74364, 0.5f);

			UnityEditor.Animations.BlendTree lM_N74374 = MotionControllerMotion.EditorCreateBlendTree("RunTree", lController, rLayerIndex);
			lM_N74374.blendType = UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;
			lM_N74374.blendParameter = "InputX";
			lM_N74374.blendParameterY = "InputY";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
			lM_N74374.useAutomaticThresholds = true;
#endif
			lM_N74374.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro_Additionals.fbx", "Rifle_RunFwdLoop"), new Vector2(0f, 0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74374_0_Children = lM_N74374.children;
			lM_N74374_0_Children[lM_N74374_0_Children.Length - 1].mirror = false;
			lM_N74374_0_Children[lM_N74374_0_Children.Length - 1].timeScale = 1f;
			lM_N74374.children = lM_N74374_0_Children;

			lM_N74374.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro_Diagonals.fbx", "Rifle_StrafeRun45RightLoop"), new Vector2(0.7f, 0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74374_1_Children = lM_N74374.children;
			lM_N74374_1_Children[lM_N74374_1_Children.Length - 1].mirror = false;
			lM_N74374_1_Children[lM_N74374_1_Children.Length - 1].timeScale = 1.1f;
			lM_N74374.children = lM_N74374_1_Children;

			lM_N74374.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro_Additionals.fbx", "Rifle_StrafeRun45LeftLoop"), new Vector2(-0.7f, 0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74374_2_Children = lM_N74374.children;
			lM_N74374_2_Children[lM_N74374_2_Children.Length - 1].mirror = false;
			lM_N74374_2_Children[lM_N74374_2_Children.Length - 1].timeScale = 1.1f;
			lM_N74374.children = lM_N74374_2_Children;

			lM_N74374.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro_Additionals.fbx", "Rifle_StrafeRunLeftLoop"), new Vector2(-0.7f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74374_3_Children = lM_N74374.children;
			lM_N74374_3_Children[lM_N74374_3_Children.Length - 1].mirror = false;
			lM_N74374_3_Children[lM_N74374_3_Children.Length - 1].timeScale = 1f;
			lM_N74374.children = lM_N74374_3_Children;

			lM_N74374.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro_Additionals.fbx", "Rifle_StrafeRunRightLoop"), new Vector2(0.7f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74374_4_Children = lM_N74374.children;
			lM_N74374_4_Children[lM_N74374_4_Children.Length - 1].mirror = false;
			lM_N74374_4_Children[lM_N74374_4_Children.Length - 1].timeScale = 1f;
			lM_N74374.children = lM_N74374_4_Children;

			lM_N74374.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro_Diagonals.fbx", "Rifle_StrafeRun135LeftLoop"), new Vector2(-0.7f, -0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74374_5_Children = lM_N74374.children;
			lM_N74374_5_Children[lM_N74374_5_Children.Length - 1].mirror = false;
			lM_N74374_5_Children[lM_N74374_5_Children.Length - 1].timeScale = 1.1f;
			lM_N74374.children = lM_N74374_5_Children;

			lM_N74374.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro_Additionals.fbx", "Rifle_StrafeRun135LeftLoop"), new Vector2(0.7f, -0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74374_6_Children = lM_N74374.children;
			lM_N74374_6_Children[lM_N74374_6_Children.Length - 1].mirror = false;
			lM_N74374_6_Children[lM_N74374_6_Children.Length - 1].timeScale = 1.1f;
			lM_N74374.children = lM_N74374_6_Children;

			lM_N74374.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro_Additionals.fbx", "Rifle_RunBwdLoop"), new Vector2(0f, -0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74374_7_Children = lM_N74374.children;
			lM_N74374_7_Children[lM_N74374_7_Children.Length - 1].mirror = false;
			lM_N74374_7_Children[lM_N74374_7_Children.Length - 1].timeScale = 1f;
			lM_N74374.children = lM_N74374_7_Children;

			lM_N74366.AddChild(lM_N74374, 1f);
			lState_N74380.motion = lM_N74366;

			UnityEditor.Animations.AnimatorState lState_N74378 = MotionControllerMotion.EditorFindState(lSSM_N74384, "Pistol BlendTree");
			if (lState_N74378 == null) { lState_N74378 = lSSM_N74384.AddState("Pistol BlendTree", new Vector3(340, 300, 0)); }
			lState_N74378.speed = 1f;
			lState_N74378.mirror = false;
			lState_N74378.tag = "";

			UnityEditor.Animations.BlendTree lM_N74372 = MotionControllerMotion.EditorCreateBlendTree("Move Blend Tree", lController, rLayerIndex);
			lM_N74372.blendType = UnityEditor.Animations.BlendTreeType.Simple1D;
			lM_N74372.blendParameter = "InputMagnitude";
			lM_N74372.blendParameterY = "InputX";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
			lM_N74372.useAutomaticThresholds = false;
#endif
			lM_N74372.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolIdlePose.anim", "PistolIdlePose"), 0f);

			UnityEditor.Animations.BlendTree lM_N74376 = MotionControllerMotion.EditorCreateBlendTree("WalkTree", lController, rLayerIndex);
			lM_N74376.blendType = UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;
			lM_N74376.blendParameter = "InputX";
			lM_N74376.blendParameterY = "InputY";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
			lM_N74376.useAutomaticThresholds = true;
#endif
			lM_N74376.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolWalk.anim", "PistolWalk"), new Vector2(0f, 0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74376_0_Children = lM_N74376.children;
			lM_N74376_0_Children[lM_N74376_0_Children.Length - 1].mirror = false;
			lM_N74376_0_Children[lM_N74376_0_Children.Length - 1].timeScale = 1f;
			lM_N74376.children = lM_N74376_0_Children;

			lM_N74376.AddChild(null, new Vector2(-0.35f, 0.35f));
			lM_N74376.AddChild(null, new Vector2(0.35f, 0.35f));
			lM_N74376.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolWalkLeft.anim", "PistolWalkLeft"), new Vector2(-0.35f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74376_3_Children = lM_N74376.children;
			lM_N74376_3_Children[lM_N74376_3_Children.Length - 1].mirror = false;
			lM_N74376_3_Children[lM_N74376_3_Children.Length - 1].timeScale = 1.1f;
			lM_N74376.children = lM_N74376_3_Children;

			lM_N74376.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolWalkRight.anim", "PistolWalkRight"), new Vector2(0.35f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74376_4_Children = lM_N74376.children;
			lM_N74376_4_Children[lM_N74376_4_Children.Length - 1].mirror = false;
			lM_N74376_4_Children[lM_N74376_4_Children.Length - 1].timeScale = 1.1f;
			lM_N74376.children = lM_N74376_4_Children;

			lM_N74376.AddChild(null, new Vector2(-0.35f, -0.35f));
			lM_N74376.AddChild(null, new Vector2(0.35f, -0.35f));
			lM_N74376.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolWalkBackwards.anim", "PistolWalkBackwards"), new Vector2(0f, -0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74376_7_Children = lM_N74376.children;
			lM_N74376_7_Children[lM_N74376_7_Children.Length - 1].mirror = false;
			lM_N74376_7_Children[lM_N74376_7_Children.Length - 1].timeScale = 1f;
			lM_N74376.children = lM_N74376_7_Children;

			lM_N74372.AddChild(lM_N74376, 0.5f);

			UnityEditor.Animations.BlendTree lM_N74370 = MotionControllerMotion.EditorCreateBlendTree("RunTree", lController, rLayerIndex);
			lM_N74370.blendType = UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;
			lM_N74370.blendParameter = "InputX";
			lM_N74370.blendParameterY = "InputY";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
			lM_N74370.useAutomaticThresholds = true;
#endif
			lM_N74370.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolRun.anim", "PistolRun"), new Vector2(0f, 0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74370_0_Children = lM_N74370.children;
			lM_N74370_0_Children[lM_N74370_0_Children.Length - 1].mirror = false;
			lM_N74370_0_Children[lM_N74370_0_Children.Length - 1].timeScale = 0.9f;
			lM_N74370.children = lM_N74370_0_Children;

			lM_N74370.AddChild(null, new Vector2(-0.7f, 0.7f));
			lM_N74370.AddChild(null, new Vector2(0.7f, 0.7f));
			lM_N74370.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolRunLeft.anim", "PistolRunLeft"), new Vector2(-0.7f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74370_3_Children = lM_N74370.children;
			lM_N74370_3_Children[lM_N74370_3_Children.Length - 1].mirror = false;
			lM_N74370_3_Children[lM_N74370_3_Children.Length - 1].timeScale = 1f;
			lM_N74370.children = lM_N74370_3_Children;

			lM_N74370.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolRunRight.anim", "PistolRunRight"), new Vector2(0.7f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74370_4_Children = lM_N74370.children;
			lM_N74370_4_Children[lM_N74370_4_Children.Length - 1].mirror = false;
			lM_N74370_4_Children[lM_N74370_4_Children.Length - 1].timeScale = 1f;
			lM_N74370.children = lM_N74370_4_Children;

			lM_N74370.AddChild(null, new Vector2(-0.7f, -0.7f));
			lM_N74370.AddChild(null, new Vector2(0.7f, -0.7f));
			lM_N74370.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolRunBackwards.anim", "PistolRunBackwards"), new Vector2(0f, -0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74370_7_Children = lM_N74370.children;
			lM_N74370_7_Children[lM_N74370_7_Children.Length - 1].mirror = false;
			lM_N74370_7_Children[lM_N74370_7_Children.Length - 1].timeScale = 1f;
			lM_N74370.children = lM_N74370_7_Children;

			lM_N74372.AddChild(lM_N74370, 1f);
			lState_N74378.motion = lM_N74372;

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N79224 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N74380, 0);
			if (lAnyTransition_N79224 == null) { lAnyTransition_N79224 = lLayerStateMachine.AddAnyStateTransition(lState_N74380); }
			lAnyTransition_N79224.isExit = false;
			lAnyTransition_N79224.hasExitTime = false;
			lAnyTransition_N79224.hasFixedDuration = true;
			lAnyTransition_N79224.exitTime = 0.75f;
			lAnyTransition_N79224.duration = 0.25f;
			lAnyTransition_N79224.offset = 0f;
			lAnyTransition_N79224.mute = false;
			lAnyTransition_N79224.solo = false;
			lAnyTransition_N79224.canTransitionToSelf = true;
			lAnyTransition_N79224.orderedInterruption = true;
			lAnyTransition_N79224.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_N79224.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N79224.RemoveCondition(lAnyTransition_N79224.conditions[i]); }
			lAnyTransition_N79224.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73100f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_N79224.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 500f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N79256 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N74378, 0);
			if (lAnyTransition_N79256 == null) { lAnyTransition_N79256 = lLayerStateMachine.AddAnyStateTransition(lState_N74378); }
			lAnyTransition_N79256.isExit = false;
			lAnyTransition_N79256.hasExitTime = false;
			lAnyTransition_N79256.hasFixedDuration = true;
			lAnyTransition_N79256.exitTime = 0.75f;
			lAnyTransition_N79256.duration = 0.25f;
			lAnyTransition_N79256.offset = 0f;
			lAnyTransition_N79256.mute = false;
			lAnyTransition_N79256.solo = false;
			lAnyTransition_N79256.canTransitionToSelf = true;
			lAnyTransition_N79256.orderedInterruption = true;
			lAnyTransition_N79256.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_N79256.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N79256.RemoveCondition(lAnyTransition_N79256.conditions[i]); }
			lAnyTransition_N79256.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73100f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_N79256.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 550f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N79892 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N74382, 0);
			if (lAnyTransition_N79892 == null) { lAnyTransition_N79892 = lLayerStateMachine.AddAnyStateTransition(lState_N74382); }
			lAnyTransition_N79892.isExit = false;
			lAnyTransition_N79892.hasExitTime = false;
			lAnyTransition_N79892.hasFixedDuration = true;
			lAnyTransition_N79892.exitTime = 0.9f;
			lAnyTransition_N79892.duration = 0.2f;
			lAnyTransition_N79892.offset = 0f;
			lAnyTransition_N79892.mute = false;
			lAnyTransition_N79892.solo = false;
			lAnyTransition_N79892.canTransitionToSelf = true;
			lAnyTransition_N79892.orderedInterruption = true;
			lAnyTransition_N79892.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_N79892.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N79892.RemoveCondition(lAnyTransition_N79892.conditions[i]); }
			lAnyTransition_N79892.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73100f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_N79892.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");


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

			UnityEditor.Animations.AnimatorStateMachine lSSM_N74384 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "RifleAP_BasicWalkRunStrafe-SM");
			if (lSSM_N74384 == null) { lSSM_N74384 = lLayerStateMachine.AddStateMachine("RifleAP_BasicWalkRunStrafe-SM", new Vector3(-190, -1010, 0)); }

			UnityEditor.Animations.AnimatorState lState_N74382 = MotionControllerMotion.EditorFindState(lSSM_N74384, "Unarmed BlendTree");
			if (lState_N74382 == null) { lState_N74382 = lSSM_N74384.AddState("Unarmed BlendTree", new Vector3(340, 20, 0)); }
			lState_N74382.speed = 1f;
			lState_N74382.mirror = false;
			lState_N74382.tag = "";

			UnityEditor.Animations.BlendTree lM_N74368 = MotionControllerMotion.EditorCreateBlendTree("Move Blend Tree", lController, rLayerIndex);
			lM_N74368.blendType = UnityEditor.Animations.BlendTreeType.Simple1D;
			lM_N74368.blendParameter = "InputMagnitude";
			lM_N74368.blendParameterY = "InputX";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
			lM_N74368.useAutomaticThresholds = false;
#endif
			lM_N74368.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdlePose"), 0f);

			UnityEditor.Animations.BlendTree lM_N74362 = MotionControllerMotion.EditorCreateBlendTree("WalkTree", lController, rLayerIndex);
			lM_N74362.blendType = UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;
			lM_N74362.blendParameter = "InputX";
			lM_N74362.blendParameterY = "InputY";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
			lM_N74362.useAutomaticThresholds = true;
#endif
			lM_N74362.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Walking/unity_WalkFWD_v2.fbx", "WalkForward"), new Vector2(0f, 0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74362_0_Children = lM_N74362.children;
			lM_N74362_0_Children[lM_N74362_0_Children.Length - 1].mirror = false;
			lM_N74362_0_Children[lM_N74362_0_Children.Length - 1].timeScale = 1.1f;
			lM_N74362.children = lM_N74362_0_Children;

			lM_N74362.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Walking/unity_SWalk_v2.fbx", "SWalkForwardRight"), new Vector2(0.35f, 0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74362_1_Children = lM_N74362.children;
			lM_N74362_1_Children[lM_N74362_1_Children.Length - 1].mirror = false;
			lM_N74362_1_Children[lM_N74362_1_Children.Length - 1].timeScale = 1.2f;
			lM_N74362.children = lM_N74362_1_Children;

			lM_N74362.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Walking/unity_SWalk_v2.fbx", "SWalkForwardLeft"), new Vector2(-0.35f, 0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74362_2_Children = lM_N74362.children;
			lM_N74362_2_Children[lM_N74362_2_Children.Length - 1].mirror = false;
			lM_N74362_2_Children[lM_N74362_2_Children.Length - 1].timeScale = 1.2f;
			lM_N74362.children = lM_N74362_2_Children;

			lM_N74362.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Walking/unity_SWalk_v2.fbx", "SWalkLeft"), new Vector2(-0.35f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74362_3_Children = lM_N74362.children;
			lM_N74362_3_Children[lM_N74362_3_Children.Length - 1].mirror = false;
			lM_N74362_3_Children[lM_N74362_3_Children.Length - 1].timeScale = 1.2f;
			lM_N74362.children = lM_N74362_3_Children;

			lM_N74362.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Walking/unity_SWalk_v2.fbx", "SWalkRight"), new Vector2(0.35f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74362_4_Children = lM_N74362.children;
			lM_N74362_4_Children[lM_N74362_4_Children.Length - 1].mirror = false;
			lM_N74362_4_Children[lM_N74362_4_Children.Length - 1].timeScale = 1.2f;
			lM_N74362.children = lM_N74362_4_Children;

			lM_N74362.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2Strafe_AllAngles.fbx", "WalkStrafeBackwardsLeft"), new Vector2(-0.35f, -0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74362_5_Children = lM_N74362.children;
			lM_N74362_5_Children[lM_N74362_5_Children.Length - 1].mirror = false;
			lM_N74362_5_Children[lM_N74362_5_Children.Length - 1].timeScale = 1.1f;
			lM_N74362.children = lM_N74362_5_Children;

			lM_N74362.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2Strafe_AllAngles.fbx", "WalkStrafeBackwardsRight"), new Vector2(0.35f, -0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74362_6_Children = lM_N74362.children;
			lM_N74362_6_Children[lM_N74362_6_Children.Length - 1].mirror = false;
			lM_N74362_6_Children[lM_N74362_6_Children.Length - 1].timeScale = 1.1f;
			lM_N74362.children = lM_N74362_6_Children;

			lM_N74362.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Walking/unity_BWalk.fbx", "WalkBackwards"), new Vector2(0f, -0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74362_7_Children = lM_N74362.children;
			lM_N74362_7_Children[lM_N74362_7_Children.Length - 1].mirror = false;
			lM_N74362_7_Children[lM_N74362_7_Children.Length - 1].timeScale = 1f;
			lM_N74362.children = lM_N74362_7_Children;

			lM_N74368.AddChild(lM_N74362, 0.5f);

			UnityEditor.Animations.BlendTree lM_N74360 = MotionControllerMotion.EditorCreateBlendTree("RunTree", lController, rLayerIndex);
			lM_N74360.blendType = UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;
			lM_N74360.blendParameter = "InputX";
			lM_N74360.blendParameterY = "InputY";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
			lM_N74360.useAutomaticThresholds = true;
#endif
			lM_N74360.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Running/RunForward_v2.fbx", "RunForward"), new Vector2(0f, 0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74360_0_Children = lM_N74360.children;
			lM_N74360_0_Children[lM_N74360_0_Children.Length - 1].mirror = false;
			lM_N74360_0_Children[lM_N74360_0_Children.Length - 1].timeScale = 1f;
			lM_N74360.children = lM_N74360_0_Children;

			lM_N74360.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeForwardRight"), new Vector2(0.7f, 0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74360_1_Children = lM_N74360.children;
			lM_N74360_1_Children[lM_N74360_1_Children.Length - 1].mirror = false;
			lM_N74360_1_Children[lM_N74360_1_Children.Length - 1].timeScale = 1.1f;
			lM_N74360.children = lM_N74360_1_Children;

			lM_N74360.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeForwardLeft"), new Vector2(-0.7f, 0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74360_2_Children = lM_N74360.children;
			lM_N74360_2_Children[lM_N74360_2_Children.Length - 1].mirror = false;
			lM_N74360_2_Children[lM_N74360_2_Children.Length - 1].timeScale = 1.1f;
			lM_N74360.children = lM_N74360_2_Children;

			lM_N74360.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeLeft"), new Vector2(-0.7f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74360_3_Children = lM_N74360.children;
			lM_N74360_3_Children[lM_N74360_3_Children.Length - 1].mirror = false;
			lM_N74360_3_Children[lM_N74360_3_Children.Length - 1].timeScale = 1f;
			lM_N74360.children = lM_N74360_3_Children;

			lM_N74360.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeRight"), new Vector2(0.7f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74360_4_Children = lM_N74360.children;
			lM_N74360_4_Children[lM_N74360_4_Children.Length - 1].mirror = false;
			lM_N74360_4_Children[lM_N74360_4_Children.Length - 1].timeScale = 1f;
			lM_N74360.children = lM_N74360_4_Children;

			lM_N74360.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeBackwardLeft"), new Vector2(-0.7f, -0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74360_5_Children = lM_N74360.children;
			lM_N74360_5_Children[lM_N74360_5_Children.Length - 1].mirror = false;
			lM_N74360_5_Children[lM_N74360_5_Children.Length - 1].timeScale = 1.1f;
			lM_N74360.children = lM_N74360_5_Children;

			lM_N74360.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeBackwardRight"), new Vector2(0.7f, -0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74360_6_Children = lM_N74360.children;
			lM_N74360_6_Children[lM_N74360_6_Children.Length - 1].mirror = false;
			lM_N74360_6_Children[lM_N74360_6_Children.Length - 1].timeScale = 1.1f;
			lM_N74360.children = lM_N74360_6_Children;

			lM_N74360.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Running/RunBackward.fbx", "RunBackwards"), new Vector2(0f, -0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74360_7_Children = lM_N74360.children;
			lM_N74360_7_Children[lM_N74360_7_Children.Length - 1].mirror = false;
			lM_N74360_7_Children[lM_N74360_7_Children.Length - 1].timeScale = 1f;
			lM_N74360.children = lM_N74360_7_Children;

			lM_N74368.AddChild(lM_N74360, 1f);
			lState_N74382.motion = lM_N74368;

			UnityEditor.Animations.AnimatorState lState_N74380 = MotionControllerMotion.EditorFindState(lSSM_N74384, "Rifle BlendTree");
			if (lState_N74380 == null) { lState_N74380 = lSSM_N74384.AddState("Rifle BlendTree", new Vector3(340, 240, 0)); }
			lState_N74380.speed = 1f;
			lState_N74380.mirror = false;
			lState_N74380.tag = "";

			UnityEditor.Animations.BlendTree lM_N74366 = MotionControllerMotion.EditorCreateBlendTree("Move Blend Tree", lController, rLayerIndex);
			lM_N74366.blendType = UnityEditor.Animations.BlendTreeType.Simple1D;
			lM_N74366.blendParameter = "InputMagnitude";
			lM_N74366.blendParameterY = "InputX";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
			lM_N74366.useAutomaticThresholds = false;
#endif
			lM_N74366.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_Idle_Single_Frame"), 0f);

			UnityEditor.Animations.BlendTree lM_N74364 = MotionControllerMotion.EditorCreateBlendTree("WalkTree", lController, rLayerIndex);
			lM_N74364.blendType = UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;
			lM_N74364.blendParameter = "InputX";
			lM_N74364.blendParameterY = "InputY";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
			lM_N74364.useAutomaticThresholds = true;
#endif
			lM_N74364.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_WalkFwdLoop"), new Vector2(0f, 0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74364_0_Children = lM_N74364.children;
			lM_N74364_0_Children[lM_N74364_0_Children.Length - 1].mirror = false;
			lM_N74364_0_Children[lM_N74364_0_Children.Length - 1].timeScale = 1.1f;
			lM_N74364.children = lM_N74364_0_Children;

			lM_N74364.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro_Diagonals.fbx", "Rifle_StrafeRight45Loop"), new Vector2(0.35f, 0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74364_1_Children = lM_N74364.children;
			lM_N74364_1_Children[lM_N74364_1_Children.Length - 1].mirror = false;
			lM_N74364_1_Children[lM_N74364_1_Children.Length - 1].timeScale = 1.2f;
			lM_N74364.children = lM_N74364_1_Children;

			lM_N74364.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_StrafeLeft45Loop"), new Vector2(-0.35f, 0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74364_2_Children = lM_N74364.children;
			lM_N74364_2_Children[lM_N74364_2_Children.Length - 1].mirror = false;
			lM_N74364_2_Children[lM_N74364_2_Children.Length - 1].timeScale = 1.2f;
			lM_N74364.children = lM_N74364_2_Children;

			lM_N74364.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_StrafeLeftLoop"), new Vector2(-0.35f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74364_3_Children = lM_N74364.children;
			lM_N74364_3_Children[lM_N74364_3_Children.Length - 1].mirror = false;
			lM_N74364_3_Children[lM_N74364_3_Children.Length - 1].timeScale = 1.2f;
			lM_N74364.children = lM_N74364_3_Children;

			lM_N74364.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_StrafeRightLoop"), new Vector2(0.35f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74364_4_Children = lM_N74364.children;
			lM_N74364_4_Children[lM_N74364_4_Children.Length - 1].mirror = false;
			lM_N74364_4_Children[lM_N74364_4_Children.Length - 1].timeScale = 1.2f;
			lM_N74364.children = lM_N74364_4_Children;

			lM_N74364.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro_Diagonals.fbx", "Rifle_StrafeLeft135Loop"), new Vector2(-0.35f, -0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74364_5_Children = lM_N74364.children;
			lM_N74364_5_Children[lM_N74364_5_Children.Length - 1].mirror = false;
			lM_N74364_5_Children[lM_N74364_5_Children.Length - 1].timeScale = 1.1f;
			lM_N74364.children = lM_N74364_5_Children;

			lM_N74364.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_StrafeRight135Loop"), new Vector2(0.35f, -0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74364_6_Children = lM_N74364.children;
			lM_N74364_6_Children[lM_N74364_6_Children.Length - 1].mirror = false;
			lM_N74364_6_Children[lM_N74364_6_Children.Length - 1].timeScale = 1.1f;
			lM_N74364.children = lM_N74364_6_Children;

			lM_N74364.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_WalkBwdLoop"), new Vector2(0f, -0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74364_7_Children = lM_N74364.children;
			lM_N74364_7_Children[lM_N74364_7_Children.Length - 1].mirror = false;
			lM_N74364_7_Children[lM_N74364_7_Children.Length - 1].timeScale = 1f;
			lM_N74364.children = lM_N74364_7_Children;

			lM_N74366.AddChild(lM_N74364, 0.5f);

			UnityEditor.Animations.BlendTree lM_N74374 = MotionControllerMotion.EditorCreateBlendTree("RunTree", lController, rLayerIndex);
			lM_N74374.blendType = UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;
			lM_N74374.blendParameter = "InputX";
			lM_N74374.blendParameterY = "InputY";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
			lM_N74374.useAutomaticThresholds = true;
#endif
			lM_N74374.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro_Additionals.fbx", "Rifle_RunFwdLoop"), new Vector2(0f, 0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74374_0_Children = lM_N74374.children;
			lM_N74374_0_Children[lM_N74374_0_Children.Length - 1].mirror = false;
			lM_N74374_0_Children[lM_N74374_0_Children.Length - 1].timeScale = 1f;
			lM_N74374.children = lM_N74374_0_Children;

			lM_N74374.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro_Diagonals.fbx", "Rifle_StrafeRun45RightLoop"), new Vector2(0.7f, 0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74374_1_Children = lM_N74374.children;
			lM_N74374_1_Children[lM_N74374_1_Children.Length - 1].mirror = false;
			lM_N74374_1_Children[lM_N74374_1_Children.Length - 1].timeScale = 1.1f;
			lM_N74374.children = lM_N74374_1_Children;

			lM_N74374.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro_Additionals.fbx", "Rifle_StrafeRun45LeftLoop"), new Vector2(-0.7f, 0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74374_2_Children = lM_N74374.children;
			lM_N74374_2_Children[lM_N74374_2_Children.Length - 1].mirror = false;
			lM_N74374_2_Children[lM_N74374_2_Children.Length - 1].timeScale = 1.1f;
			lM_N74374.children = lM_N74374_2_Children;

			lM_N74374.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro_Additionals.fbx", "Rifle_StrafeRunLeftLoop"), new Vector2(-0.7f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74374_3_Children = lM_N74374.children;
			lM_N74374_3_Children[lM_N74374_3_Children.Length - 1].mirror = false;
			lM_N74374_3_Children[lM_N74374_3_Children.Length - 1].timeScale = 1f;
			lM_N74374.children = lM_N74374_3_Children;

			lM_N74374.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro_Additionals.fbx", "Rifle_StrafeRunRightLoop"), new Vector2(0.7f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74374_4_Children = lM_N74374.children;
			lM_N74374_4_Children[lM_N74374_4_Children.Length - 1].mirror = false;
			lM_N74374_4_Children[lM_N74374_4_Children.Length - 1].timeScale = 1f;
			lM_N74374.children = lM_N74374_4_Children;

			lM_N74374.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro_Diagonals.fbx", "Rifle_StrafeRun135LeftLoop"), new Vector2(-0.7f, -0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74374_5_Children = lM_N74374.children;
			lM_N74374_5_Children[lM_N74374_5_Children.Length - 1].mirror = false;
			lM_N74374_5_Children[lM_N74374_5_Children.Length - 1].timeScale = 1.1f;
			lM_N74374.children = lM_N74374_5_Children;

			lM_N74374.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro_Additionals.fbx", "Rifle_StrafeRun135LeftLoop"), new Vector2(0.7f, -0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74374_6_Children = lM_N74374.children;
			lM_N74374_6_Children[lM_N74374_6_Children.Length - 1].mirror = false;
			lM_N74374_6_Children[lM_N74374_6_Children.Length - 1].timeScale = 1.1f;
			lM_N74374.children = lM_N74374_6_Children;

			lM_N74374.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/RifleAnimsetPro/Animations/RifleAnimsetPro_Additionals.fbx", "Rifle_RunBwdLoop"), new Vector2(0f, -0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74374_7_Children = lM_N74374.children;
			lM_N74374_7_Children[lM_N74374_7_Children.Length - 1].mirror = false;
			lM_N74374_7_Children[lM_N74374_7_Children.Length - 1].timeScale = 1f;
			lM_N74374.children = lM_N74374_7_Children;

			lM_N74366.AddChild(lM_N74374, 1f);
			lState_N74380.motion = lM_N74366;

			UnityEditor.Animations.AnimatorState lState_N74378 = MotionControllerMotion.EditorFindState(lSSM_N74384, "Pistol BlendTree");
			if (lState_N74378 == null) { lState_N74378 = lSSM_N74384.AddState("Pistol BlendTree", new Vector3(340, 300, 0)); }
			lState_N74378.speed = 1f;
			lState_N74378.mirror = false;
			lState_N74378.tag = "";

			UnityEditor.Animations.BlendTree lM_N74372 = MotionControllerMotion.EditorCreateBlendTree("Move Blend Tree", lController, rLayerIndex);
			lM_N74372.blendType = UnityEditor.Animations.BlendTreeType.Simple1D;
			lM_N74372.blendParameter = "InputMagnitude";
			lM_N74372.blendParameterY = "InputX";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
			lM_N74372.useAutomaticThresholds = false;
#endif
			lM_N74372.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolIdlePose.anim", "PistolIdlePose"), 0f);

			UnityEditor.Animations.BlendTree lM_N74376 = MotionControllerMotion.EditorCreateBlendTree("WalkTree", lController, rLayerIndex);
			lM_N74376.blendType = UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;
			lM_N74376.blendParameter = "InputX";
			lM_N74376.blendParameterY = "InputY";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
			lM_N74376.useAutomaticThresholds = true;
#endif
			lM_N74376.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolWalk.anim", "PistolWalk"), new Vector2(0f, 0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74376_0_Children = lM_N74376.children;
			lM_N74376_0_Children[lM_N74376_0_Children.Length - 1].mirror = false;
			lM_N74376_0_Children[lM_N74376_0_Children.Length - 1].timeScale = 1f;
			lM_N74376.children = lM_N74376_0_Children;

			lM_N74376.AddChild(null, new Vector2(-0.35f, 0.35f));
			lM_N74376.AddChild(null, new Vector2(0.35f, 0.35f));
			lM_N74376.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolWalkLeft.anim", "PistolWalkLeft"), new Vector2(-0.35f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74376_3_Children = lM_N74376.children;
			lM_N74376_3_Children[lM_N74376_3_Children.Length - 1].mirror = false;
			lM_N74376_3_Children[lM_N74376_3_Children.Length - 1].timeScale = 1.1f;
			lM_N74376.children = lM_N74376_3_Children;

			lM_N74376.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolWalkRight.anim", "PistolWalkRight"), new Vector2(0.35f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74376_4_Children = lM_N74376.children;
			lM_N74376_4_Children[lM_N74376_4_Children.Length - 1].mirror = false;
			lM_N74376_4_Children[lM_N74376_4_Children.Length - 1].timeScale = 1.1f;
			lM_N74376.children = lM_N74376_4_Children;

			lM_N74376.AddChild(null, new Vector2(-0.35f, -0.35f));
			lM_N74376.AddChild(null, new Vector2(0.35f, -0.35f));
			lM_N74376.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolWalkBackwards.anim", "PistolWalkBackwards"), new Vector2(0f, -0.35f));
			UnityEditor.Animations.ChildMotion[] lM_N74376_7_Children = lM_N74376.children;
			lM_N74376_7_Children[lM_N74376_7_Children.Length - 1].mirror = false;
			lM_N74376_7_Children[lM_N74376_7_Children.Length - 1].timeScale = 1f;
			lM_N74376.children = lM_N74376_7_Children;

			lM_N74372.AddChild(lM_N74376, 0.5f);

			UnityEditor.Animations.BlendTree lM_N74370 = MotionControllerMotion.EditorCreateBlendTree("RunTree", lController, rLayerIndex);
			lM_N74370.blendType = UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;
			lM_N74370.blendParameter = "InputX";
			lM_N74370.blendParameterY = "InputY";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
			lM_N74370.useAutomaticThresholds = true;
#endif
			lM_N74370.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolRun.anim", "PistolRun"), new Vector2(0f, 0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74370_0_Children = lM_N74370.children;
			lM_N74370_0_Children[lM_N74370_0_Children.Length - 1].mirror = false;
			lM_N74370_0_Children[lM_N74370_0_Children.Length - 1].timeScale = 0.9f;
			lM_N74370.children = lM_N74370_0_Children;

			lM_N74370.AddChild(null, new Vector2(-0.7f, 0.7f));
			lM_N74370.AddChild(null, new Vector2(0.7f, 0.7f));
			lM_N74370.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolRunLeft.anim", "PistolRunLeft"), new Vector2(-0.7f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74370_3_Children = lM_N74370.children;
			lM_N74370_3_Children[lM_N74370_3_Children.Length - 1].mirror = false;
			lM_N74370_3_Children[lM_N74370_3_Children.Length - 1].timeScale = 1f;
			lM_N74370.children = lM_N74370_3_Children;

			lM_N74370.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolRunRight.anim", "PistolRunRight"), new Vector2(0.7f, 0f));
			UnityEditor.Animations.ChildMotion[] lM_N74370_4_Children = lM_N74370.children;
			lM_N74370_4_Children[lM_N74370_4_Children.Length - 1].mirror = false;
			lM_N74370_4_Children[lM_N74370_4_Children.Length - 1].timeScale = 1f;
			lM_N74370.children = lM_N74370_4_Children;

			lM_N74370.AddChild(null, new Vector2(-0.7f, -0.7f));
			lM_N74370.AddChild(null, new Vector2(0.7f, -0.7f));
			lM_N74370.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolRunBackwards.anim", "PistolRunBackwards"), new Vector2(0f, -0.7f));
			UnityEditor.Animations.ChildMotion[] lM_N74370_7_Children = lM_N74370.children;
			lM_N74370_7_Children[lM_N74370_7_Children.Length - 1].mirror = false;
			lM_N74370_7_Children[lM_N74370_7_Children.Length - 1].timeScale = 1f;
			lM_N74370.children = lM_N74370_7_Children;

			lM_N74372.AddChild(lM_N74370, 1f);
			lState_N74378.motion = lM_N74372;

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N79224 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N74380, 0);
			if (lAnyTransition_N79224 == null) { lAnyTransition_N79224 = lLayerStateMachine.AddAnyStateTransition(lState_N74380); }
			lAnyTransition_N79224.isExit = false;
			lAnyTransition_N79224.hasExitTime = false;
			lAnyTransition_N79224.hasFixedDuration = true;
			lAnyTransition_N79224.exitTime = 0.75f;
			lAnyTransition_N79224.duration = 0.25f;
			lAnyTransition_N79224.offset = 0f;
			lAnyTransition_N79224.mute = false;
			lAnyTransition_N79224.solo = false;
			lAnyTransition_N79224.canTransitionToSelf = true;
			lAnyTransition_N79224.orderedInterruption = true;
			lAnyTransition_N79224.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_N79224.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N79224.RemoveCondition(lAnyTransition_N79224.conditions[i]); }
			lAnyTransition_N79224.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73100f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_N79224.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 500f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N79256 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N74378, 0);
			if (lAnyTransition_N79256 == null) { lAnyTransition_N79256 = lLayerStateMachine.AddAnyStateTransition(lState_N74378); }
			lAnyTransition_N79256.isExit = false;
			lAnyTransition_N79256.hasExitTime = false;
			lAnyTransition_N79256.hasFixedDuration = true;
			lAnyTransition_N79256.exitTime = 0.75f;
			lAnyTransition_N79256.duration = 0.25f;
			lAnyTransition_N79256.offset = 0f;
			lAnyTransition_N79256.mute = false;
			lAnyTransition_N79256.solo = false;
			lAnyTransition_N79256.canTransitionToSelf = true;
			lAnyTransition_N79256.orderedInterruption = true;
			lAnyTransition_N79256.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_N79256.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N79256.RemoveCondition(lAnyTransition_N79256.conditions[i]); }
			lAnyTransition_N79256.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73100f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_N79256.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 550f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N79892 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N74382, 0);
			if (lAnyTransition_N79892 == null) { lAnyTransition_N79892 = lLayerStateMachine.AddAnyStateTransition(lState_N74382); }
			lAnyTransition_N79892.isExit = false;
			lAnyTransition_N79892.hasExitTime = false;
			lAnyTransition_N79892.hasFixedDuration = true;
			lAnyTransition_N79892.exitTime = 0.9f;
			lAnyTransition_N79892.duration = 0.2f;
			lAnyTransition_N79892.offset = 0f;
			lAnyTransition_N79892.mute = false;
			lAnyTransition_N79892.solo = false;
			lAnyTransition_N79892.canTransitionToSelf = true;
			lAnyTransition_N79892.orderedInterruption = true;
			lAnyTransition_N79892.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_N79892.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N79892.RemoveCondition(lAnyTransition_N79892.conditions[i]); }
			lAnyTransition_N79892.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73100f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_N79892.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");

		}

		#endregion Definition

	}
}
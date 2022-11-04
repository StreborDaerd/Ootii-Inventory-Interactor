using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Combat;
using com.ootii.Actors.Inventory;
using com.ootii.Cameras;
using com.ootii.Game;
using com.ootii.Geometry;
using com.ootii.Helpers;
using com.ootii.Items;
using com.ootii.Messages;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WildWalrus.Items;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace WildWalrus.Actors.AnimationControllers
{
	[MotionName("Basic Shooter Attack 1")]
	[MotionDescription("Test. Basic attacks that use guns instead of projectiles.")]
	public class BasicShooterAttack1 : BasicShooterMotion1
	{
		protected string TAG_FIRE = "Fire";
		protected string EVENT_FIRE = "fire";


		#region MotionPhases
		
		public int PHASE_UNKNOWN = 0;
		public int PHASE_START = 3500;
		public int PHASE_START_AUTO = 3510;
		public int PHASE_STOP = 3515;
		public int PHASE_RESTART = 3516;

		#endregion MotionPhases


		#region Properties
		
		public string _WeaponSlotID = "RIGHT_HAND";
		public string WeaponSlotID
		{
			get { return _WeaponSlotID; }
			set { _WeaponSlotID = value; }
		}
		

		public bool _AllowWhenRunning = false;
		public bool AllowWhenRunning
		{
			get { return _AllowWhenRunning; }
			set { _AllowWhenRunning = value; }
		}
		
		
		public string _AimActionAlias = "Camera Aim";
		public string AimActionAlias
		{
			get { return _AimActionAlias; }
			set { _AimActionAlias = value; }
		}
		
		
		public string _AltAimActionAlias = "Alt Camera Aim";
		public string AltAimActionAlias
		{
			get { return _AltAimActionAlias; }
			set { _AltAimActionAlias = value; }
		}
		
		
		public bool _ManageCrosshair = false;
		public bool ManageCrosshair
		{
			get { return _ManageCrosshair; }
			set { _ManageCrosshair = value; }
		}
		
		
		public int _RightAimCameraRigMode = 5;
		public int RightAimCameraRigMode
		{
			get { return _RightAimCameraRigMode; }
			set { _RightAimCameraRigMode = value; }
		}
		
		
		public int _LeftAimCameraRigMode = 4;
		public int LeftAimCameraRigMode
		{
			get { return _LeftAimCameraRigMode; }
			set { _LeftAimCameraRigMode = value; }
		}

		#endregion Properties


		#region Members
		
		protected bool mIsFiring = false;
		
		protected bool mIsAiming = true;
		
		protected bool mIsAltAiming = false;
		protected bool mWasAltAiming = false;
		
		protected bool mIsIKActivated = false;
		
		protected bool mIsLinked = false;
		
		protected bool mLinkRotation = false;
		
		protected Vector3 mStoredInputForward = Vector3.forward;
		
		protected bool mUseCameraUpdate = false;
		
		protected float mYaw = 0f;
		protected float mYawTarget = 0f;
		protected float mYawVelocity = 0f;
		
		protected Vector3 mTargetForward = Vector3.zero;
		
		protected bool mIsRotationLocked = false;
		
		protected bool mWasReticleVisible = false;
		
		protected Vector3 mCoverPosition = Vector3.zero;
		
		protected Quaternion mCoverRotation = Quaternion.identity;
		
		protected ICoverMotion mCoverMotion = null;

		#endregion Members


		#region Constructors
		
		public BasicShooterAttack1() : base()
		{
			_Pack = ShooterPackDefinition.PackName;
			_Category = EnumMotionCategories.COMBAT_SHOOTING;
			
			_Priority = 15;
			_ActionAlias = "Combat Attack";
			
			if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicShooterAttack-SM"; }
		}
		
		
		public BasicShooterAttack1(MotionController rController) : base(rController)
		{
			_Pack = ShooterPackDefinition.PackName;
			_Category = EnumMotionCategories.COMBAT_SHOOTING;
			
			_Priority = 15;
			_ActionAlias = "Combat Attack";
			
			if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicShooterAttack-SM"; }
		}

		#endregion Constructors
		
		
		public override void Awake()
		{
			base.Awake();
			
			// Object that will provide access to attributes
			if (_InventorySourceOwner != null)
			{
				mInventorySource = InterfaceHelper.GetComponent<IInventorySource>(_InventorySourceOwner);
			}
			
			// If the input source is still null, see if we can grab a local input source
			if (mInventorySource == null && mMotionController != null)
			{
				mInventorySource = InterfaceHelper.GetComponent<IInventorySource>(mMotionController.gameObject);
				if (mInventorySource != null) { _InventorySourceOwner = mMotionController.gameObject; }
			}
		}


		#region Tests
		
		public override bool TestActivate()
		{
			if (!mIsStartable)
			{
				return false;
			}
			
			if (!mActorController.IsGrounded)
			{
				return false;
			}
			
			// This is only valid if we're in combat mode
			if (mMotionController.Stance != EnumControllerStance.COMBAT_SHOOTING)
			{
				return false;
			}
			
			// Don't activate if we're currently equipping or storing
			if (mMotionLayer.ActiveMotion is IEquipStoreMotion)
			{
				return false;
			}
			
			// Determine if we can aim and shoot while running
			if (!_AllowWhenRunning && mMotionController.MotionLayers[0].ActiveMotion is IWalkRunMotion)
			{
				if (((IWalkRunMotion)mMotionController.MotionLayers[0].ActiveMotion).IsRunActive)
				{
					return false;
				}
			}
			
			// Check if we've been activated
			if (mMotionController._InputSource != null)
			{
				mIsAiming = (_AimActionAlias.Length > 0 && mMotionController._InputSource.IsPressed(_AimActionAlias));
				mIsFiring = (_ActionAlias.Length > 0 && mMotionController._InputSource.IsJustPressed(_ActionAlias));
				
				if (mIsAiming)
				{
					return true;
				}
				
				if (mIsFiring)
				{
					MCGunCore lGunCore = FindWeapon(WeaponSlotID);
					if (lGunCore != null)
					{
						if (lGunCore.CanFire)
						{
							return true;
						}
						else
						{
							lGunCore.FailFiring();
						}
					}
				}
			}
			
			// Return the final result
			return false;
		}
		
		
		public override bool TestUpdate()
		{
			if (mIsActivatedFrame) { return true; }
			
			// This is only valid if we're in combat mode
			if (mMotionController.Stance != EnumControllerStance.COMBAT_SHOOTING)
			{
				return false;
			}
			
			// Determine if we can aim and shoot while running
			if (!_AllowWhenRunning && mMotionController.MotionLayers[0].ActiveMotion is IWalkRunMotion)
			{
				if (((IWalkRunMotion)mMotionController.MotionLayers[0].ActiveMotion).IsRunActive)
				{
					return false;
				}
			}
			
			// If we've reached the exit state, leave. The delay is to ensure that we're not in an old motion's exit state
			if (mMotionController.State.AnimatorStates[mMotionLayer._AnimatorLayerIndex].StateInfo.IsTag("Exit"))
			{
				//if (mIsDeactivating && (mLookIKEasingFunction == null || mLookIKWeight == 0f))
				//if (!(mMotionController.ActiveMotion is ICoverMotion))
				{
					return false;
				}
			}
			
			return true;
		}
		
		
		public override bool TestInterruption(MotionControllerMotion rMotion)
		{
			//Debug.Log("BSA.TestInterrupt(" + rMotion.GetType().Name + ")");
			
			if (mLookIKWeight > 0f && !(rMotion is BasicShooterEmpty))
			{
				EaseOutIK(_LookIKOutSpeed);
			}
			
			return true;
		}

		#endregion Tests


		#region MotionFunctions
		
		public override bool Activate(MotionControllerMotion rPrevMotion)
		{
			//mIsFiring = false;
			mIsAltAiming = false;
			mWasAltAiming = false;
			mLinkRotation = false;
			mIsIKActivated = false;
			mCoverMotion = null;
			
			// Determine if the reticle is already visible
			if (TargetingReticle.Instance != null)
			{
				mWasReticleVisible = TargetingReticle.Instance.IsVisible;
			}
			
			// First, see if we can find a bow in hand
			mGunCore = FindWeapon(WeaponSlotID);
			if (mGunCore != null)
			{
				mGunSupport = mGunCore.gameObject.transform.Find("Support");
			}
			
			// Clear out any old look IK
			if (mLookIKEasingFunction != null)
			{
				mMotionController.StopCoroutine(mLookIKEasingFunction);
				mLookIKEasingFunction = null;
			}
			
			// Use the weight of the attack motion as we may already be using IK
			if (rPrevMotion is BasicShooterEmpty1)
			{
				mLookIKWeight = ((BasicShooterEmpty1)rPrevMotion).LookIKWeight;
			}
			
			// Reset the yaw info for smoothing
			mYaw = 0f;
			mYawTarget = 0f;
			mYawVelocity = 0f;
			
			// Activate the cursor
			if (mIsAiming && _ManageCrosshair && TargetingReticle.Instance != null)
			{
				TargetingReticle.Instance.IsVisible = true;
				TargetingReticle.Instance.FillPercent = 0f;
			}
			
			// Store the target forward based on input
			mStoredInputForward = mMotionController.State.InputForward;
			
			// Register this motion with the camera
			if (mMotionController.CameraRig is BaseCameraRig)
			{
				((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
				((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate += OnCameraUpdated;
			}
			
			// Check if a fire action has been activated
			//if (mIsAiming && _ActionAlias.Length > 0 && mMotionController._InputSource != null)
			//{
			//    if (mMotionController._InputSource.IsPressed(_ActionAlias))
			//    {
			//        mIsFiring = true;
			//    }
			//}
			
			// Tell the animator to start your animations
			int lParameter = (mIsAiming ? 1 : 0);
			int lForm = (_Form >= 0 ? _Form : mMotionController.CurrentForm);
			mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_START, lForm, lParameter, true);
			
			// Track if we were under cover
			if (mMotionController.MotionLayers[0].ActiveMotion is ICoverMotion)
			{
				mLookIKWeight = 0f;
				
				mCoverPosition = mMotionController._Transform.position;
				mCoverRotation = mMotionController._Transform.rotation;
				mCoverMotion = mMotionController.MotionLayers[0].ActiveMotion as ICoverMotion;
				
				mCoverMotion.ExitCover(true, true);
			}
			
			// Return
			return base.Activate(rPrevMotion);
		}
		
		
		public override void Deactivate()
		{
			// Ensure we're not firing and clear the gun so we
			// can grab a new one later
			if (mGunCore != null)
			{
				mGunCore.StopFiring();
				mGunCore = null;
				mGunSupport = null;
			}
			
			// Deactivate the cursor
			if (_ManageCrosshair && TargetingReticle.Instance != null)
			{
				TargetingReticle.Instance.IsVisible = mWasReticleVisible;
			}
			
			// Unregister this motion with the camera
			if (mMotionController.CameraRig is BaseCameraRig)
			{
				((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
			}
			
			// Reset the parameter
			mMotionController.SetAnimatorMotionParameter(mMotionLayer.AnimatorLayerIndex, 0);
			
			// Finish the deactivation process
			base.Deactivate();
		}
		
		
		public override void UpdateRootMotion(float rDeltaTime, int rUpdateIndex, ref Vector3 rMovement, ref Quaternion rRotation)
		{
			rMovement = Vector3.zero;
			rRotation = Quaternion.identity;
		}
		
		
		public override void Update(float rDeltaTime, int rUpdateIndex)
		{
			mRotation = Quaternion.identity;
			mUseCameraUpdate = false;
			
			// Store the target forward based on input
			mStoredInputForward = mMotionController.State.InputForward;
			
			// Clear the firing flag. We'll reset it if we're still holding down
			//mIsFiring = mGunCore.IsActive;
			
			// We need to find out if we're in a transition that would prevent us from firing
			bool lIsTransitioning = (mCoverMotion != null && mMotionController.ActiveMotion == mCoverMotion);
			if (!lIsTransitioning) { lIsTransitioning = (mCoverMotion != null && mMotionController.State.AnimatorStates[0].StateInfo.IsTag("Exit")); }
			
			// If we're in a cover motion, we need to wait for it to exit before we try to use IK
			if (!mIsIKActivated)
			{
				Vector3 lTargetForward = mMotionController._Transform.forward;
				if (mMotionController._CameraTransform != null) { lTargetForward = mMotionController._CameraTransform.forward; }
				
				float lAngle = Vector3Ext.HorizontalAngleTo(mMotionController._Transform.forward, lTargetForward, mMotionController._Transform.up);
				if (Mathf.Abs(lAngle) < 65f)
				{
					EaseInIK(mIsFiring ? 0.1f : _LookIKInSpeed);
					mIsIKActivated = true;
				}
			}
			
			// Check if we're still aiming and firing
			if (mMotionController._InputSource != null)
			{
				if (_ActionAlias.Length > 0)
				{
					if (mGunCore.IsAutomatic)
					{
						if (mMotionController._InputSource.IsPressed(_ActionAlias)) { mIsFiring = true; }
					}
					else
					{
						if (mMotionController._InputSource.IsJustPressed(_ActionAlias)) { mIsFiring = true; }
					}
				}
				else
				{
					mIsFiring = true;
				}
				
				if (mGunCore.FireCount > 0 && _ActionAlias.Length > 0)
				{
					if (mMotionController._InputSource.IsReleased(_ActionAlias)) { mIsFiring = false; }
				}
				
				if (_AimActionAlias.Length > 0)
				{
					mIsAiming = mMotionController._InputSource.IsPressed(_AimActionAlias);
				}
				
				if (mIsAiming && _AltAimActionAlias.Length > 0)
				{
					if (mMotionController._InputSource.IsJustPressed(_AltAimActionAlias))
					{
						mIsAltAiming = !mIsAltAiming;
					}
				}
			}
			// For NPCs, we automatically fire
			else if(mLookIKWeight >= 1f)
			{
				if (mMotionLayer._AnimatorTransitionID == 0)
				{
					mIsFiring = true;
				}
			}
			
			// If we're transitioning (say from cover), we can't fire yet
			if (mCoverMotion != null && mCoverMotion.IsExiting)
			{
				//Debug.Log("BSA.Update() IsExitingCover");
			}
			// If we are firing, we may need to start firing
			else if (mIsFiring)
			{
				//Debug.Log("BSA.Update() Firing");
				if (!mGunCore.IsActive)
				{
					if (mGunCore.CanFire)
					{
						if (mMotionController.State.AnimatorStates[mMotionLayer._AnimatorLayerIndex].MotionPhase == PHASE_STOP)
						{
							mMotionController.State.AnimatorStates[mMotionLayer._AnimatorLayerIndex].MotionPhase = 0;
						}
						
						mIsFiring = mGunCore.StartFiring();
						mMotionController.SetAnimatorMotionParameter(mMotionLayer._AnimatorLayerIndex, 0);
					}
					else
					{
						mIsFiring = false;
						
						if (_ActionAlias.Length > 0 && mMotionController._InputSource != null && mMotionController._InputSource.IsJustPressed(_ActionAlias))
						{
							mGunCore.FailFiring();
						}
					}
				}
			}
			// Since we're not firing, check if we're aiming
			else
			{
				if (mGunCore.IsActive)
				{
					mGunCore.StopFiring();
				}
				
				// Start aiming
				if (mIsAiming)
				{
					mMotionController.SetAnimatorMotionParameter(mMotionLayer._AnimatorLayerIndex, 1);
				}
				else
				{
					// When doing a single shot, we may need to wait until we
					// IK to the right direction
					if (mLookIKEasingFunction == null)
					{
						// We don't want to transition yet if we're just coming into the animator state
						mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_STOP, 0, 0, true);
					}
				}
			}
			
			// Determine if there was a change in our aiming pattern
			if (mIsAltAiming != mWasAltAiming && mMotionController.CameraRig != null)
			{
				if (mIsAltAiming)
				{
					mMotionController.CameraRig.Mode = LeftAimCameraRigMode;
				}
				else
				{
					mMotionController.CameraRig.Mode = RightAimCameraRigMode;
				}
			}
			
			// Update for future checking
			mWasAltAiming = mIsAltAiming;
			
			// Aiming, we want to allow the user to control the rotation
			if (mCoverMotion == null || !mCoverMotion.IsExiting)
			{
				if (mTargetForward.sqrMagnitude > 0f)
				{
					RotateToDirection(mTargetForward, _RotationSpeed * 5f, rDeltaTime, ref mRotation);
				}
				else // if (mIsAiming)
				{
					// If set, rotate most of the way to the camera direction
					if (_RotateWithCamera && mMotionController._CameraTransform != null)
					{
						RotateToDirection(mMotionController._CameraTransform.forward, _RotationSpeed, rDeltaTime, ref mRotation);
					}
					// Otherwise, rotate using input
					else if (!_RotateWithCamera)
					{
						if (_RotateWithInput) { RotateUsingInput(_RotationSpeed, rDeltaTime, ref mRotation); }
					}
				}
			}
			
			// Update the firing window if we're not doing it later
			if (mGunCore != null && !(mMotionController.CameraRig is BaseCameraRig))
			{
				if (_IsLookIKEnabled)
				{
					Quaternion lTargetRotation = mMotionController._Transform.rotation;
					if (mMotionController._CameraTransform != null) { lTargetRotation = mMotionController._CameraTransform.rotation; }
					
					mLookIKRotation = mGunCore.transform.rotation;
					mLookIKRotation = mMotionController._Transform.rotation.RotationTo(mLookIKRotation);
					
					RotateSpineToDirection(mLookIKRotation, lTargetRotation, mLookIKWeight);
				}
				
				if (mGunCore.IsActive)
				{
					mIsFiring = mGunCore.UpdateFireWindow(mMotionController._CameraTransform);
				}
			}
		}

		#endregion MotionFunctions


		#region Functions
		
		public override bool DeactivatedUpdate(float rDeltaTime, int rUpdateIndex)
		{
			if (!_IsLookIKEnabled) { return false; }
			if (mLookIKWeight == 0f) { return false; }
			if (QuaternionExt.IsIdentity(mLookIKRotation)) { return false; }
			if (mLookIKWeight > 0f && mMotionLayer.ActiveMotion is BasicShooterEmpty) { return false; }
			
			Quaternion lTargetRotation = mMotionController._Transform.rotation;
			if (mMotionController._CameraTransform != null) { lTargetRotation = mMotionController._CameraTransform.rotation; }
			
			RotateSpineToDirection(mLookIKRotation, lTargetRotation, mLookIKWeight);
			
			return true;
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
				lYawDelta = mMotionController._InputSource.ViewX * (_RotationSpeed / 60f);
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

		#endregion Functions


		#region Events
		
		protected void OnCameraUpdated(float rDeltaTime, int rUpdateIndex, BaseCameraRig rCameraRig)
		{
			if (mMotionController._CameraTransform == null) { return; }
			
			if (_RotateWithCamera && (mCoverMotion == null || !mCoverMotion.IsExiting))
			{
				// We do the inverse tilt so we calculate the rotation in "natural up" space vs. "actor up" space.
				Quaternion lInvTilt = QuaternionExt.FromToRotation(mMotionController._Transform.up, Vector3.up);
				
				// Forward direction of the actor in "natural up"
				Vector3 lActorForward = lInvTilt * mMotionController._Transform.forward;
				
				// Camera forward in "natural up"
				Vector3 lCameraForward = lInvTilt * mMotionController._CameraTransform.forward;
				
				// Get the rotation angle to the camera
				float lActorToCameraAngle = NumberHelper.GetHorizontalAngle(lActorForward, lCameraForward);
				
				// Clear the link if we're out of rotation range
				if (_RotationSpeed > 0f && Mathf.Abs(lActorToCameraAngle) > _RotationSpeed * rDeltaTime * 5f) { mIsRotationLocked = false; }
				
				// We only want to do this is we're very very close to the desired angle. This will remove any stuttering
				if (_RotationSpeed == 0f || mIsRotationLocked || Mathf.Abs(lActorToCameraAngle) < _RotationSpeed * rDeltaTime * 1f)
				{
					mIsRotationLocked = true;
					
					// Since we're after the camera update, we have to force the rotation outside the normal flow
					Quaternion lRotation = Quaternion.AngleAxis(lActorToCameraAngle, Vector3.up);
					mActorController.Yaw = mActorController.Yaw * lRotation;
					mActorController._Transform.rotation = mActorController.Tilt * mActorController.Yaw;
					
					// If we've gotten here we are in sync with the camera and don't need to head to any
					// target that may have been set due to recoil or something.
					rCameraRig.ClearTargetYawPitch();
				}
			}
			
			// If we're aiming, use IK
			if (_IsLookIKEnabled)
			{
				mLookIKRotation = mGunCore.transform.rotation;
				mLookIKRotation = mMotionController._Transform.rotation.RotationTo(mLookIKRotation);
				
				RotateSpineToDirection(mLookIKRotation, mMotionController._CameraTransform.rotation, mLookIKWeight);
			}
			
			// Now that the camera has updated, we can update the gun to fire
			if (mGunCore.IsActive)
			{
				mIsFiring = mGunCore.UpdateFireWindow(mMotionController._CameraTransform, rCameraRig);
			}
		}
		
		
		public override void OnMessageReceived(IMessage rMessage)
		{
			if (rMessage == null) { return; }
			if (mMotionController.Stance != EnumControllerStance.COMBAT_SHOOTING) { return; }
			
			CombatMessage lCombatMessage = rMessage as CombatMessage;
			if (lCombatMessage != null)
			{
				// Attack messages
				if (lCombatMessage.Attacker == mMotionController.gameObject)
				{
					// Call for an attack
					if (rMessage.ID == CombatMessage.MSG_COMBATANT_ATTACK)
					{
						if (!mIsActive && mMotionLayer._AnimatorTransitionID == 0)
						{
							if (lCombatMessage.Defender != null)
							{
								mTargetForward = (lCombatMessage.Defender.transform.position - mMotionController._Transform.position).normalized;
							}
							else if (lCombatMessage.HitDirection.sqrMagnitude > 0f)
							{
								mTargetForward = lCombatMessage.HitDirection;
							}
							else
							{
								mTargetForward = mMotionController._Transform.forward;
							}
							
							lCombatMessage.IsHandled = true;
							lCombatMessage.Recipient = this;
							mMotionController.ActivateMotion(this);
						}
					}
					// Stop shooting if we are
					else if (rMessage.ID == CombatMessage.MSG_COMBATANT_CANCEL)
					{
						if (mIsActive)
						{
							Deactivate();
						}
					}
					// Gives us a chance to modify the attack message
					else if (rMessage.ID == CombatMessage.MSG_ATTACKER_ATTACKED)
					{
					}
				}
			}
		}

		#endregion Events


		#region EditorGUI
		
#if UNITY_EDITOR
		
		public override bool OnInspectorGUI()
		{
			bool lIsDirty = false;
			
			if (EditorHelper.TextField("Aim Alias",
				"Action alias that has us aiming.",
				AimActionAlias, mMotionController))
			{
				lIsDirty = true;
				AimActionAlias = EditorHelper.FieldStringValue;
			}
			
			if (EditorHelper.TextField("Alt Aim Alias",
				"Toggles the alternate aiming view.",
				AltAimActionAlias, mMotionController))
			{
				lIsDirty = true;
				AltAimActionAlias = EditorHelper.FieldStringValue;
			}
			
			// Alt aim
			if (AltAimActionAlias.Length > 0)
			{
				GUILayout.BeginHorizontal();
				
				EditorGUILayout.LabelField(
					new GUIContent("Alt Aim Modes",
					"Mode (or motor indexes) to use when entering and leaving the alt aim position."),
					GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

				if (EditorHelper.IntField(RightAimCameraRigMode, "Right Aim", mMotionController, 0f, 20f))
				{
					lIsDirty = true;
					RightAimCameraRigMode = EditorHelper.FieldIntValue;
				}

				if (EditorHelper.IntField(LeftAimCameraRigMode, "Left Aim", mMotionController, 0f, 20f))
				{
					lIsDirty = true;
					LeftAimCameraRigMode = EditorHelper.FieldIntValue;
				}

				GUILayout.EndHorizontal();
			}

			if (EditorHelper.BoolField("Manage Reticle",
				"Determines if this motion will enable/disable the reticle.",
				ManageCrosshair, mMotionController))
			{
				lIsDirty = true;
				ManageCrosshair = EditorHelper.FieldBoolValue;
			}

			GUILayout.Space(5f);

			if (EditorHelper.TextField("Attack Alias",
				"Action alias that is required to trigger the attack.",
				ActionAlias, mMotionController))
			{
				lIsDirty = true;
				ActionAlias = EditorHelper.FieldStringValue;
			}

			if (EditorHelper.BoolField("Allow When Running",
				"Determines if we can shoot an aim when running.",
				AllowWhenRunning, mMotionController))
			{
				lIsDirty = true;
				AllowWhenRunning = EditorHelper.FieldBoolValue;
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
				"Degrees per second to rotate the actor to the camera's forward.",
				RotationSpeed, mMotionController))
			{
				lIsDirty = true;
				RotationSpeed = EditorHelper.FieldFloatValue;
			}

			GUILayout.Space(5f);

			EditorHelper.DrawInspectorDescription("IK properties for aiming the gun", MessageType.None);

			if (EditorHelper.BoolField("Enable Look IK",
				"Determines if we'll use the camera to rotate the spine forward.",
				IsLookIKEnabled, mMotionController))
			{
				lIsDirty = true;
				IsLookIKEnabled = EditorHelper.FieldBoolValue;
			}

			if (IsLookIKEnabled)
			{
				// IK Angles
				GUILayout.BeginHorizontal();

				EditorGUILayout.LabelField(new GUIContent("IK Time",
					"Time in seconds to transition the IK in and out."),
					GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

				if (EditorHelper.FloatField(LookIKInSpeed, "IK In", mMotionController, 0f, 20f))
				{
					lIsDirty = true;
					LookIKInSpeed = EditorHelper.FieldFloatValue;
				}

				if (EditorHelper.FloatField(LookIKOutSpeed, "IK Out", mMotionController, 0f, 20f))
				{
					lIsDirty = true;
					LookIKOutSpeed = EditorHelper.FieldFloatValue;
				}

				GUILayout.EndHorizontal();

				// IK Angles
				GUILayout.BeginHorizontal();

				EditorGUILayout.LabelField(new GUIContent("IK Angles",
					"Additional IK angles used when aiming."),
					GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

				if (EditorHelper.FloatField(LookIKHorizontalAngle, "Horizonal Aim", mMotionController, 0f, 20f))
				{
					lIsDirty = true;
					LookIKHorizontalAngle = EditorHelper.FieldFloatValue;
				}

				if (EditorHelper.FloatField(LookIKVerticalAngle, "Vertical Aim", mMotionController, 0f, 20f))
				{
					lIsDirty = true;
					LookIKVerticalAngle = EditorHelper.FieldFloatValue;
				}

				GUILayout.EndHorizontal();

				// IK Angles
				GUILayout.BeginHorizontal();

				EditorGUILayout.LabelField(new GUIContent("Quick IK Angles",
					"Additional IK angles used when fast shooting."),
					GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

				if (EditorHelper.FloatField(FastHorizontalAimAngle, "Fast Horizonal Aim", mMotionController, 0f, 20f))
				{
					lIsDirty = true;
					FastHorizontalAimAngle = EditorHelper.FieldFloatValue;
				}

				if (EditorHelper.FloatField(FastVerticalAimAngle, "Fast Vertical Aim", mMotionController, 0f, 20f))
				{
					lIsDirty = true;
					FastVerticalAimAngle = EditorHelper.FieldFloatValue;
				}

				GUILayout.EndHorizontal();

				GUILayout.Space(5f);
			}

			if (EditorHelper.BoolField("Enable Arm IK",
				"Determines if we use the 'Support' transform for arm IK.",
				IsSupportIKEnabled, mMotionController))
			{
				lIsDirty = true;
				IsSupportIKEnabled = EditorHelper.FieldBoolValue;
			}

			GUILayout.Space(5f);

			EditorHelper.DrawInspectorDescription("Inventory information about the weapon.", MessageType.None);

			GameObject lNewAttributeSourceOwner = EditorHelper.InterfaceOwnerField<IInventorySource>(
				new GUIContent("Inventory Source",
				"Inventory source we'll use for accessing items and slots."),
				InventorySourceOwner, true);
			if (lNewAttributeSourceOwner != InventorySourceOwner)
			{
				lIsDirty = true;
				InventorySourceOwner = lNewAttributeSourceOwner;
			}

			if (EditorHelper.TextField("Weapon Slot ID",
				"Inventory slot ID holding the weapon.",
				WeaponSlotID, mMotionController))
			{
				lIsDirty = true;
				WeaponSlotID = EditorHelper.FieldStringValue;
			}

			return lIsDirty;
		}

#endif

		#endregion EditorGUI


		#region Auto-Generated
		// ************************************ START AUTO GENERATED ************************************

		/// <summary>
		/// These declarations go inside the class so you can test for which state
		/// and transitions are active. Testing hash values is much faster than strings.
		/// </summary>
		public int STATE_Empty = -1;
		public int STATE_RifleIdlePoseExit = -1;
		public int STATE_RifleShoot = -1;
		public int STATE_RifleAimIdlePose = -1;
		public int STATE_PistolIdlePoseExit = -1;
		public int STATE_PistolShoot = -1;
		public int STATE_PistolAimIdlePose = -1;
		public int TRANS_AnyState_RifleShoot = -1;
		public int TRANS_EntryState_RifleShoot = -1;
		public int TRANS_AnyState_RifleAimIdlePose = -1;
		public int TRANS_EntryState_RifleAimIdlePose = -1;
		public int TRANS_AnyState_PistolShoot = -1;
		public int TRANS_EntryState_PistolShoot = -1;
		public int TRANS_AnyState_PistolAimIdlePose = -1;
		public int TRANS_EntryState_PistolAimIdlePose = -1;
		public int TRANS_RifleIdlePoseExit_RifleShoot = -1;
		public int TRANS_RifleIdlePoseExit_RifleAimIdlePose = -1;
		public int TRANS_RifleShoot_RifleIdlePoseExit = -1;
		public int TRANS_RifleShoot_RifleAimIdlePose = -1;
		public int TRANS_RifleAimIdlePose_RifleShoot = -1;
		public int TRANS_RifleAimIdlePose_RifleIdlePoseExit = -1;
		public int TRANS_PistolIdlePoseExit_PistolShoot = -1;
		public int TRANS_PistolIdlePoseExit_PistolAimIdlePose = -1;
		public int TRANS_PistolShoot_PistolIdlePoseExit = -1;
		public int TRANS_PistolShoot_PistolAimIdlePose = -1;
		public int TRANS_PistolAimIdlePose_PistolShoot = -1;
		public int TRANS_PistolAimIdlePose_PistolIdlePoseExit = -1;

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
					if (lStateID == STATE_Empty) { return true; }
					if (lStateID == STATE_RifleIdlePoseExit) { return true; }
					if (lStateID == STATE_RifleShoot) { return true; }
					if (lStateID == STATE_RifleAimIdlePose) { return true; }
					if (lStateID == STATE_PistolIdlePoseExit) { return true; }
					if (lStateID == STATE_PistolShoot) { return true; }
					if (lStateID == STATE_PistolAimIdlePose) { return true; }
				}

				if (lTransitionID == TRANS_AnyState_RifleShoot) { return true; }
				if (lTransitionID == TRANS_EntryState_RifleShoot) { return true; }
				if (lTransitionID == TRANS_AnyState_RifleAimIdlePose) { return true; }
				if (lTransitionID == TRANS_EntryState_RifleAimIdlePose) { return true; }
				if (lTransitionID == TRANS_AnyState_PistolShoot) { return true; }
				if (lTransitionID == TRANS_EntryState_PistolShoot) { return true; }
				if (lTransitionID == TRANS_AnyState_PistolAimIdlePose) { return true; }
				if (lTransitionID == TRANS_EntryState_PistolAimIdlePose) { return true; }
				if (lTransitionID == TRANS_RifleIdlePoseExit_RifleShoot) { return true; }
				if (lTransitionID == TRANS_RifleIdlePoseExit_RifleAimIdlePose) { return true; }
				if (lTransitionID == TRANS_RifleShoot_RifleIdlePoseExit) { return true; }
				if (lTransitionID == TRANS_RifleShoot_RifleAimIdlePose) { return true; }
				if (lTransitionID == TRANS_RifleAimIdlePose_RifleShoot) { return true; }
				if (lTransitionID == TRANS_RifleAimIdlePose_RifleIdlePoseExit) { return true; }
				if (lTransitionID == TRANS_PistolIdlePoseExit_PistolShoot) { return true; }
				if (lTransitionID == TRANS_PistolIdlePoseExit_PistolAimIdlePose) { return true; }
				if (lTransitionID == TRANS_PistolShoot_PistolIdlePoseExit) { return true; }
				if (lTransitionID == TRANS_PistolShoot_PistolAimIdlePose) { return true; }
				if (lTransitionID == TRANS_PistolAimIdlePose_PistolShoot) { return true; }
				if (lTransitionID == TRANS_PistolAimIdlePose_PistolIdlePoseExit) { return true; }
				return false;
			}
		}

		/// <summary>
		/// Used to determine if the actor is in one of the states for this motion
		/// </summary>
		/// <returns></returns>
		public override bool IsMotionState(int rStateID)
		{
			if (rStateID == STATE_Empty) { return true; }
			if (rStateID == STATE_RifleIdlePoseExit) { return true; }
			if (rStateID == STATE_RifleShoot) { return true; }
			if (rStateID == STATE_RifleAimIdlePose) { return true; }
			if (rStateID == STATE_PistolIdlePoseExit) { return true; }
			if (rStateID == STATE_PistolShoot) { return true; }
			if (rStateID == STATE_PistolAimIdlePose) { return true; }
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
				if (rStateID == STATE_Empty) { return true; }
				if (rStateID == STATE_RifleIdlePoseExit) { return true; }
				if (rStateID == STATE_RifleShoot) { return true; }
				if (rStateID == STATE_RifleAimIdlePose) { return true; }
				if (rStateID == STATE_PistolIdlePoseExit) { return true; }
				if (rStateID == STATE_PistolShoot) { return true; }
				if (rStateID == STATE_PistolAimIdlePose) { return true; }
			}

			if (rTransitionID == TRANS_AnyState_RifleShoot) { return true; }
			if (rTransitionID == TRANS_EntryState_RifleShoot) { return true; }
			if (rTransitionID == TRANS_AnyState_RifleAimIdlePose) { return true; }
			if (rTransitionID == TRANS_EntryState_RifleAimIdlePose) { return true; }
			if (rTransitionID == TRANS_AnyState_PistolShoot) { return true; }
			if (rTransitionID == TRANS_EntryState_PistolShoot) { return true; }
			if (rTransitionID == TRANS_AnyState_PistolAimIdlePose) { return true; }
			if (rTransitionID == TRANS_EntryState_PistolAimIdlePose) { return true; }
			if (rTransitionID == TRANS_RifleIdlePoseExit_RifleShoot) { return true; }
			if (rTransitionID == TRANS_RifleIdlePoseExit_RifleAimIdlePose) { return true; }
			if (rTransitionID == TRANS_RifleShoot_RifleIdlePoseExit) { return true; }
			if (rTransitionID == TRANS_RifleShoot_RifleAimIdlePose) { return true; }
			if (rTransitionID == TRANS_RifleAimIdlePose_RifleShoot) { return true; }
			if (rTransitionID == TRANS_RifleAimIdlePose_RifleIdlePoseExit) { return true; }
			if (rTransitionID == TRANS_PistolIdlePoseExit_PistolShoot) { return true; }
			if (rTransitionID == TRANS_PistolIdlePoseExit_PistolAimIdlePose) { return true; }
			if (rTransitionID == TRANS_PistolShoot_PistolIdlePoseExit) { return true; }
			if (rTransitionID == TRANS_PistolShoot_PistolAimIdlePose) { return true; }
			if (rTransitionID == TRANS_PistolAimIdlePose_PistolShoot) { return true; }
			if (rTransitionID == TRANS_PistolAimIdlePose_PistolIdlePoseExit) { return true; }
			return false;
		}

		/// <summary>
		/// Preprocess any animator data so the motion can use it later
		/// </summary>
		public override void LoadAnimatorData()
		{
			string lLayer = mMotionController.Animator.GetLayerName(mMotionLayer._AnimatorLayerIndex);
			TRANS_AnyState_RifleShoot = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicShooterAttack-SM.Rifle Shoot");
			TRANS_EntryState_RifleShoot = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicShooterAttack-SM.Rifle Shoot");
			TRANS_AnyState_RifleAimIdlePose = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicShooterAttack-SM.Rifle Aim Idle Pose");
			TRANS_EntryState_RifleAimIdlePose = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicShooterAttack-SM.Rifle Aim Idle Pose");
			TRANS_AnyState_PistolShoot = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicShooterAttack-SM.Pistol Shoot");
			TRANS_EntryState_PistolShoot = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicShooterAttack-SM.Pistol Shoot");
			TRANS_AnyState_PistolAimIdlePose = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicShooterAttack-SM.Pistol Aim Idle Pose");
			TRANS_EntryState_PistolAimIdlePose = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicShooterAttack-SM.Pistol Aim Idle Pose");
			STATE_Empty = mMotionController.AddAnimatorName("" + lLayer + ".Empty");
			STATE_RifleIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".BasicShooterAttack-SM.Rifle Idle Pose Exit");
			TRANS_RifleIdlePoseExit_RifleShoot = mMotionController.AddAnimatorName("" + lLayer + ".BasicShooterAttack-SM.Rifle Idle Pose Exit -> " + lLayer + ".BasicShooterAttack-SM.Rifle Shoot");
			TRANS_RifleIdlePoseExit_RifleAimIdlePose = mMotionController.AddAnimatorName("" + lLayer + ".BasicShooterAttack-SM.Rifle Idle Pose Exit -> " + lLayer + ".BasicShooterAttack-SM.Rifle Aim Idle Pose");
			STATE_RifleShoot = mMotionController.AddAnimatorName("" + lLayer + ".BasicShooterAttack-SM.Rifle Shoot");
			TRANS_RifleShoot_RifleIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".BasicShooterAttack-SM.Rifle Shoot -> " + lLayer + ".BasicShooterAttack-SM.Rifle Idle Pose Exit");
			TRANS_RifleShoot_RifleAimIdlePose = mMotionController.AddAnimatorName("" + lLayer + ".BasicShooterAttack-SM.Rifle Shoot -> " + lLayer + ".BasicShooterAttack-SM.Rifle Aim Idle Pose");
			STATE_RifleAimIdlePose = mMotionController.AddAnimatorName("" + lLayer + ".BasicShooterAttack-SM.Rifle Aim Idle Pose");
			TRANS_RifleAimIdlePose_RifleShoot = mMotionController.AddAnimatorName("" + lLayer + ".BasicShooterAttack-SM.Rifle Aim Idle Pose -> " + lLayer + ".BasicShooterAttack-SM.Rifle Shoot");
			TRANS_RifleAimIdlePose_RifleIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".BasicShooterAttack-SM.Rifle Aim Idle Pose -> " + lLayer + ".BasicShooterAttack-SM.Rifle Idle Pose Exit");
			STATE_PistolIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".BasicShooterAttack-SM.Pistol Idle Pose Exit");
			TRANS_PistolIdlePoseExit_PistolShoot = mMotionController.AddAnimatorName("" + lLayer + ".BasicShooterAttack-SM.Pistol Idle Pose Exit -> " + lLayer + ".BasicShooterAttack-SM.Pistol Shoot");
			TRANS_PistolIdlePoseExit_PistolAimIdlePose = mMotionController.AddAnimatorName("" + lLayer + ".BasicShooterAttack-SM.Pistol Idle Pose Exit -> " + lLayer + ".BasicShooterAttack-SM.Pistol Aim Idle Pose");
			STATE_PistolShoot = mMotionController.AddAnimatorName("" + lLayer + ".BasicShooterAttack-SM.Pistol Shoot");
			TRANS_PistolShoot_PistolIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".BasicShooterAttack-SM.Pistol Shoot -> " + lLayer + ".BasicShooterAttack-SM.Pistol Idle Pose Exit");
			TRANS_PistolShoot_PistolAimIdlePose = mMotionController.AddAnimatorName("" + lLayer + ".BasicShooterAttack-SM.Pistol Shoot -> " + lLayer + ".BasicShooterAttack-SM.Pistol Aim Idle Pose");
			STATE_PistolAimIdlePose = mMotionController.AddAnimatorName("" + lLayer + ".BasicShooterAttack-SM.Pistol Aim Idle Pose");
			TRANS_PistolAimIdlePose_PistolShoot = mMotionController.AddAnimatorName("" + lLayer + ".BasicShooterAttack-SM.Pistol Aim Idle Pose -> " + lLayer + ".BasicShooterAttack-SM.Pistol Shoot");
			TRANS_PistolAimIdlePose_PistolIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".BasicShooterAttack-SM.Pistol Aim Idle Pose -> " + lLayer + ".BasicShooterAttack-SM.Pistol Idle Pose Exit");
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

			UnityEditor.Animations.AnimatorStateMachine lSSM_34996 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "BasicShooterAttack-SM");
			if (lSSM_34996 == null) { lSSM_34996 = lLayerStateMachine.AddStateMachine("BasicShooterAttack-SM", new Vector3(620, -910, 0)); }

			UnityEditor.Animations.AnimatorState lState_35102 = MotionControllerMotion.EditorFindState(lSSM_34996, "Rifle Idle Pose Exit");
			if (lState_35102 == null) { lState_35102 = lSSM_34996.AddState("Rifle Idle Pose Exit", new Vector3(564, 0, 0)); }
			lState_35102.speed = 1f;
			lState_35102.mirror = false;
			lState_35102.tag = "Exit";
			lState_35102.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/RifleIdlePose.anim", "RifleIdlePose");

			UnityEditor.Animations.AnimatorState lState_35040 = MotionControllerMotion.EditorFindState(lSSM_34996, "Rifle Shoot");
			if (lState_35040 == null) { lState_35040 = lSSM_34996.AddState("Rifle Shoot", new Vector3(310, -30, 0)); }
			lState_35040.speed = 1f;
			lState_35040.mirror = false;
			lState_35040.tag = "";
			lState_35040.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/RifleFire.anim", "RifleFire");

			UnityEditor.Animations.AnimatorState lState_35042 = MotionControllerMotion.EditorFindState(lSSM_34996, "Rifle Aim Idle Pose");
			if (lState_35042 == null) { lState_35042 = lSSM_34996.AddState("Rifle Aim Idle Pose", new Vector3(310, 40, 0)); }
			lState_35042.speed = 1f;
			lState_35042.mirror = false;
			lState_35042.tag = "";
			lState_35042.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/RifleAimIdlePose.anim", "RifleAimIdlePose");

			UnityEditor.Animations.AnimatorState lState_35104 = MotionControllerMotion.EditorFindState(lSSM_34996, "Pistol Idle Pose Exit");
			if (lState_35104 == null) { lState_35104 = lSSM_34996.AddState("Pistol Idle Pose Exit", new Vector3(564, 156, 0)); }
			lState_35104.speed = 1f;
			lState_35104.mirror = false;
			lState_35104.tag = "Exit";
			lState_35104.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolIdlePose.anim", "PistolIdlePose");

			UnityEditor.Animations.AnimatorState lState_35054 = MotionControllerMotion.EditorFindState(lSSM_34996, "Pistol Shoot");
			if (lState_35054 == null) { lState_35054 = lSSM_34996.AddState("Pistol Shoot", new Vector3(312, 132, 0)); }
			lState_35054.speed = 1f;
			lState_35054.mirror = false;
			lState_35054.tag = "";
			lState_35054.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolFire.anim", "PistolFire");

			UnityEditor.Animations.AnimatorState lState_35056 = MotionControllerMotion.EditorFindState(lSSM_34996, "Pistol Aim Idle Pose");
			if (lState_35056 == null) { lState_35056 = lSSM_34996.AddState("Pistol Aim Idle Pose", new Vector3(312, 204, 0)); }
			lState_35056.speed = 1f;
			lState_35056.mirror = false;
			lState_35056.tag = "";

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_35006 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_35040, 0);
			if (lAnyTransition_35006 == null) { lAnyTransition_35006 = lLayerStateMachine.AddAnyStateTransition(lState_35040); }
			lAnyTransition_35006.isExit = false;
			lAnyTransition_35006.hasExitTime = false;
			lAnyTransition_35006.hasFixedDuration = true;
			lAnyTransition_35006.exitTime = 0.75f;
			lAnyTransition_35006.duration = 0.1f;
			lAnyTransition_35006.offset = 0.7810742f;
			lAnyTransition_35006.mute = false;
			lAnyTransition_35006.solo = false;
			lAnyTransition_35006.canTransitionToSelf = true;
			lAnyTransition_35006.orderedInterruption = true;
			lAnyTransition_35006.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_35006.conditions.Length - 1; i >= 0; i--) { lAnyTransition_35006.RemoveCondition(lAnyTransition_35006.conditions[i]); }
			lAnyTransition_35006.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3500f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_35006.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 500f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_35006.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_35008 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_35042, 0);
			if (lAnyTransition_35008 == null) { lAnyTransition_35008 = lLayerStateMachine.AddAnyStateTransition(lState_35042); }
			lAnyTransition_35008.isExit = false;
			lAnyTransition_35008.hasExitTime = false;
			lAnyTransition_35008.hasFixedDuration = true;
			lAnyTransition_35008.exitTime = 0.75f;
			lAnyTransition_35008.duration = 0.15f;
			lAnyTransition_35008.offset = 0f;
			lAnyTransition_35008.mute = false;
			lAnyTransition_35008.solo = false;
			lAnyTransition_35008.canTransitionToSelf = true;
			lAnyTransition_35008.orderedInterruption = true;
			lAnyTransition_35008.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_35008.conditions.Length - 1; i >= 0; i--) { lAnyTransition_35008.RemoveCondition(lAnyTransition_35008.conditions[i]); }
			lAnyTransition_35008.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3500f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_35008.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 500f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_35008.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_35020 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_35054, 0);
			if (lAnyTransition_35020 == null) { lAnyTransition_35020 = lLayerStateMachine.AddAnyStateTransition(lState_35054); }
			lAnyTransition_35020.isExit = false;
			lAnyTransition_35020.hasExitTime = false;
			lAnyTransition_35020.hasFixedDuration = true;
			lAnyTransition_35020.exitTime = 0.7500004f;
			lAnyTransition_35020.duration = 0.1f;
			lAnyTransition_35020.offset = 0.2146628f;
			lAnyTransition_35020.mute = false;
			lAnyTransition_35020.solo = false;
			lAnyTransition_35020.canTransitionToSelf = true;
			lAnyTransition_35020.orderedInterruption = true;
			lAnyTransition_35020.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_35020.conditions.Length - 1; i >= 0; i--) { lAnyTransition_35020.RemoveCondition(lAnyTransition_35020.conditions[i]); }
			lAnyTransition_35020.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3500f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_35020.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 550f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_35020.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_35022 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_35056, 0);
			if (lAnyTransition_35022 == null) { lAnyTransition_35022 = lLayerStateMachine.AddAnyStateTransition(lState_35056); }
			lAnyTransition_35022.isExit = false;
			lAnyTransition_35022.hasExitTime = false;
			lAnyTransition_35022.hasFixedDuration = true;
			lAnyTransition_35022.exitTime = 0.75f;
			lAnyTransition_35022.duration = 0.15f;
			lAnyTransition_35022.offset = 0f;
			lAnyTransition_35022.mute = false;
			lAnyTransition_35022.solo = false;
			lAnyTransition_35022.canTransitionToSelf = true;
			lAnyTransition_35022.orderedInterruption = true;
			lAnyTransition_35022.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_35022.conditions.Length - 1; i >= 0; i--) { lAnyTransition_35022.RemoveCondition(lAnyTransition_35022.conditions[i]); }
			lAnyTransition_35022.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3500f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_35022.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 550f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_35022.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35106 = MotionControllerMotion.EditorFindTransition(lState_35102, lState_35040, 0);
			if (lTransition_35106 == null) { lTransition_35106 = lState_35102.AddTransition(lState_35040); }
			lTransition_35106.isExit = false;
			lTransition_35106.hasExitTime = false;
			lTransition_35106.hasFixedDuration = true;
			lTransition_35106.exitTime = 0f;
			lTransition_35106.duration = 0.1f;
			lTransition_35106.offset = 0f;
			lTransition_35106.mute = false;
			lTransition_35106.solo = false;
			lTransition_35106.canTransitionToSelf = true;
			lTransition_35106.orderedInterruption = true;
			lTransition_35106.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35106.conditions.Length - 1; i >= 0; i--) { lTransition_35106.RemoveCondition(lTransition_35106.conditions[i]); }
			lTransition_35106.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3516f, "L" + rLayerIndex + "MotionPhase");
			lTransition_35106.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35108 = MotionControllerMotion.EditorFindTransition(lState_35102, lState_35042, 0);
			if (lTransition_35108 == null) { lTransition_35108 = lState_35102.AddTransition(lState_35042); }
			lTransition_35108.isExit = false;
			lTransition_35108.hasExitTime = false;
			lTransition_35108.hasFixedDuration = true;
			lTransition_35108.exitTime = 0f;
			lTransition_35108.duration = 0.1f;
			lTransition_35108.offset = 0f;
			lTransition_35108.mute = false;
			lTransition_35108.solo = false;
			lTransition_35108.canTransitionToSelf = true;
			lTransition_35108.orderedInterruption = true;
			lTransition_35108.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35108.conditions.Length - 1; i >= 0; i--) { lTransition_35108.RemoveCondition(lTransition_35108.conditions[i]); }
			lTransition_35108.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3516f, "L" + rLayerIndex + "MotionPhase");
			lTransition_35108.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35110 = MotionControllerMotion.EditorFindTransition(lState_35040, lState_35102, 0);
			if (lTransition_35110 == null) { lTransition_35110 = lState_35040.AddTransition(lState_35102); }
			lTransition_35110.isExit = false;
			lTransition_35110.hasExitTime = false;
			lTransition_35110.hasFixedDuration = true;
			lTransition_35110.exitTime = 0.3373314f;
			lTransition_35110.duration = 0.2f;
			lTransition_35110.offset = 0f;
			lTransition_35110.mute = false;
			lTransition_35110.solo = false;
			lTransition_35110.canTransitionToSelf = true;
			lTransition_35110.orderedInterruption = true;
			lTransition_35110.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35110.conditions.Length - 1; i >= 0; i--) { lTransition_35110.RemoveCondition(lTransition_35110.conditions[i]); }
			lTransition_35110.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3515f, "L" + rLayerIndex + "MotionPhase");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35112 = MotionControllerMotion.EditorFindTransition(lState_35040, lState_35042, 0);
			if (lTransition_35112 == null) { lTransition_35112 = lState_35040.AddTransition(lState_35042); }
			lTransition_35112.isExit = false;
			lTransition_35112.hasExitTime = false;
			lTransition_35112.hasFixedDuration = true;
			lTransition_35112.exitTime = 0.06250006f;
			lTransition_35112.duration = 0.1f;
			lTransition_35112.offset = 0f;
			lTransition_35112.mute = false;
			lTransition_35112.solo = false;
			lTransition_35112.canTransitionToSelf = true;
			lTransition_35112.orderedInterruption = true;
			lTransition_35112.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35112.conditions.Length - 1; i >= 0; i--) { lTransition_35112.RemoveCondition(lTransition_35112.conditions[i]); }
			lTransition_35112.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionPhase");
			lTransition_35112.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35116 = MotionControllerMotion.EditorFindTransition(lState_35042, lState_35040, 0);
			if (lTransition_35116 == null) { lTransition_35116 = lState_35042.AddTransition(lState_35040); }
			lTransition_35116.isExit = false;
			lTransition_35116.hasExitTime = false;
			lTransition_35116.hasFixedDuration = true;
			lTransition_35116.exitTime = 0.8809521f;
			lTransition_35116.duration = 0.1f;
			lTransition_35116.offset = 0.7522035f;
			lTransition_35116.mute = false;
			lTransition_35116.solo = false;
			lTransition_35116.canTransitionToSelf = true;
			lTransition_35116.orderedInterruption = true;
			lTransition_35116.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35116.conditions.Length - 1; i >= 0; i--) { lTransition_35116.RemoveCondition(lTransition_35116.conditions[i]); }
			lTransition_35116.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionPhase");
			lTransition_35116.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35118 = MotionControllerMotion.EditorFindTransition(lState_35042, lState_35102, 0);
			if (lTransition_35118 == null) { lTransition_35118 = lState_35042.AddTransition(lState_35102); }
			lTransition_35118.isExit = false;
			lTransition_35118.hasExitTime = false;
			lTransition_35118.hasFixedDuration = true;
			lTransition_35118.exitTime = 0f;
			lTransition_35118.duration = 0.2f;
			lTransition_35118.offset = 0f;
			lTransition_35118.mute = false;
			lTransition_35118.solo = false;
			lTransition_35118.canTransitionToSelf = true;
			lTransition_35118.orderedInterruption = true;
			lTransition_35118.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35118.conditions.Length - 1; i >= 0; i--) { lTransition_35118.RemoveCondition(lTransition_35118.conditions[i]); }
			lTransition_35118.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3515f, "L" + rLayerIndex + "MotionPhase");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35122 = MotionControllerMotion.EditorFindTransition(lState_35104, lState_35054, 0);
			if (lTransition_35122 == null) { lTransition_35122 = lState_35104.AddTransition(lState_35054); }
			lTransition_35122.isExit = false;
			lTransition_35122.hasExitTime = false;
			lTransition_35122.hasFixedDuration = true;
			lTransition_35122.exitTime = 0f;
			lTransition_35122.duration = 0.1f;
			lTransition_35122.offset = 0.07351331f;
			lTransition_35122.mute = false;
			lTransition_35122.solo = false;
			lTransition_35122.canTransitionToSelf = true;
			lTransition_35122.orderedInterruption = true;
			lTransition_35122.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35122.conditions.Length - 1; i >= 0; i--) { lTransition_35122.RemoveCondition(lTransition_35122.conditions[i]); }
			lTransition_35122.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3516f, "L" + rLayerIndex + "MotionPhase");
			lTransition_35122.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35124 = MotionControllerMotion.EditorFindTransition(lState_35104, lState_35056, 0);
			if (lTransition_35124 == null) { lTransition_35124 = lState_35104.AddTransition(lState_35056); }
			lTransition_35124.isExit = false;
			lTransition_35124.hasExitTime = false;
			lTransition_35124.hasFixedDuration = true;
			lTransition_35124.exitTime = 0f;
			lTransition_35124.duration = 0.1f;
			lTransition_35124.offset = 0f;
			lTransition_35124.mute = false;
			lTransition_35124.solo = false;
			lTransition_35124.canTransitionToSelf = true;
			lTransition_35124.orderedInterruption = true;
			lTransition_35124.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35124.conditions.Length - 1; i >= 0; i--) { lTransition_35124.RemoveCondition(lTransition_35124.conditions[i]); }
			lTransition_35124.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3516f, "L" + rLayerIndex + "MotionPhase");
			lTransition_35124.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35126 = MotionControllerMotion.EditorFindTransition(lState_35054, lState_35104, 0);
			if (lTransition_35126 == null) { lTransition_35126 = lState_35054.AddTransition(lState_35104); }
			lTransition_35126.isExit = false;
			lTransition_35126.hasExitTime = false;
			lTransition_35126.hasFixedDuration = true;
			lTransition_35126.exitTime = 0.5722023f;
			lTransition_35126.duration = 0.09999996f;
			lTransition_35126.offset = 82.20575f;
			lTransition_35126.mute = false;
			lTransition_35126.solo = false;
			lTransition_35126.canTransitionToSelf = true;
			lTransition_35126.orderedInterruption = true;
			lTransition_35126.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35126.conditions.Length - 1; i >= 0; i--) { lTransition_35126.RemoveCondition(lTransition_35126.conditions[i]); }
			lTransition_35126.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3515f, "L" + rLayerIndex + "MotionPhase");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35128 = MotionControllerMotion.EditorFindTransition(lState_35054, lState_35056, 0);
			if (lTransition_35128 == null) { lTransition_35128 = lState_35054.AddTransition(lState_35056); }
			lTransition_35128.isExit = false;
			lTransition_35128.hasExitTime = false;
			lTransition_35128.hasFixedDuration = true;
			lTransition_35128.exitTime = 0.611675f;
			lTransition_35128.duration = 0.0999999f;
			lTransition_35128.offset = 192.21f;
			lTransition_35128.mute = false;
			lTransition_35128.solo = false;
			lTransition_35128.canTransitionToSelf = true;
			lTransition_35128.orderedInterruption = true;
			lTransition_35128.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35128.conditions.Length - 1; i >= 0; i--) { lTransition_35128.RemoveCondition(lTransition_35128.conditions[i]); }
			lTransition_35128.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionPhase");
			lTransition_35128.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35132 = MotionControllerMotion.EditorFindTransition(lState_35056, lState_35054, 0);
			if (lTransition_35132 == null) { lTransition_35132 = lState_35056.AddTransition(lState_35054); }
			lTransition_35132.isExit = false;
			lTransition_35132.hasExitTime = false;
			lTransition_35132.hasFixedDuration = true;
			lTransition_35132.exitTime = 0.8809586f;
			lTransition_35132.duration = 0.1f;
			lTransition_35132.offset = 0.2146628f;
			lTransition_35132.mute = false;
			lTransition_35132.solo = false;
			lTransition_35132.canTransitionToSelf = true;
			lTransition_35132.orderedInterruption = true;
			lTransition_35132.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35132.conditions.Length - 1; i >= 0; i--) { lTransition_35132.RemoveCondition(lTransition_35132.conditions[i]); }
			lTransition_35132.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionPhase");
			lTransition_35132.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35134 = MotionControllerMotion.EditorFindTransition(lState_35056, lState_35104, 0);
			if (lTransition_35134 == null) { lTransition_35134 = lState_35056.AddTransition(lState_35104); }
			lTransition_35134.isExit = false;
			lTransition_35134.hasExitTime = false;
			lTransition_35134.hasFixedDuration = true;
			lTransition_35134.exitTime = 0f;
			lTransition_35134.duration = 0.2f;
			lTransition_35134.offset = 0f;
			lTransition_35134.mute = false;
			lTransition_35134.solo = false;
			lTransition_35134.canTransitionToSelf = true;
			lTransition_35134.orderedInterruption = true;
			lTransition_35134.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35134.conditions.Length - 1; i >= 0; i--) { lTransition_35134.RemoveCondition(lTransition_35134.conditions[i]); }
			lTransition_35134.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3515f, "L" + rLayerIndex + "MotionPhase");


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

			UnityEditor.Animations.AnimatorStateMachine lSSM_34996 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "BasicShooterAttack-SM");
			if (lSSM_34996 == null) { lSSM_34996 = lLayerStateMachine.AddStateMachine("BasicShooterAttack-SM", new Vector3(620, -910, 0)); }

			UnityEditor.Animations.AnimatorState lState_35102 = MotionControllerMotion.EditorFindState(lSSM_34996, "Rifle Idle Pose Exit");
			if (lState_35102 == null) { lState_35102 = lSSM_34996.AddState("Rifle Idle Pose Exit", new Vector3(564, 0, 0)); }
			lState_35102.speed = 1f;
			lState_35102.mirror = false;
			lState_35102.tag = "Exit";
			lState_35102.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/RifleIdlePose.anim", "RifleIdlePose");

			UnityEditor.Animations.AnimatorState lState_35040 = MotionControllerMotion.EditorFindState(lSSM_34996, "Rifle Shoot");
			if (lState_35040 == null) { lState_35040 = lSSM_34996.AddState("Rifle Shoot", new Vector3(310, -30, 0)); }
			lState_35040.speed = 1f;
			lState_35040.mirror = false;
			lState_35040.tag = "";
			lState_35040.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/RifleFire.anim", "RifleFire");

			UnityEditor.Animations.AnimatorState lState_35042 = MotionControllerMotion.EditorFindState(lSSM_34996, "Rifle Aim Idle Pose");
			if (lState_35042 == null) { lState_35042 = lSSM_34996.AddState("Rifle Aim Idle Pose", new Vector3(310, 40, 0)); }
			lState_35042.speed = 1f;
			lState_35042.mirror = false;
			lState_35042.tag = "";
			lState_35042.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/RifleAimIdlePose.anim", "RifleAimIdlePose");

			UnityEditor.Animations.AnimatorState lState_35104 = MotionControllerMotion.EditorFindState(lSSM_34996, "Pistol Idle Pose Exit");
			if (lState_35104 == null) { lState_35104 = lSSM_34996.AddState("Pistol Idle Pose Exit", new Vector3(564, 156, 0)); }
			lState_35104.speed = 1f;
			lState_35104.mirror = false;
			lState_35104.tag = "Exit";
			lState_35104.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolIdlePose.anim", "PistolIdlePose");

			UnityEditor.Animations.AnimatorState lState_35054 = MotionControllerMotion.EditorFindState(lSSM_34996, "Pistol Shoot");
			if (lState_35054 == null) { lState_35054 = lSSM_34996.AddState("Pistol Shoot", new Vector3(312, 132, 0)); }
			lState_35054.speed = 1f;
			lState_35054.mirror = false;
			lState_35054.tag = "";
			lState_35054.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolFire.anim", "PistolFire");

			UnityEditor.Animations.AnimatorState lState_35056 = MotionControllerMotion.EditorFindState(lSSM_34996, "Pistol Aim Idle Pose");
			if (lState_35056 == null) { lState_35056 = lSSM_34996.AddState("Pistol Aim Idle Pose", new Vector3(312, 204, 0)); }
			lState_35056.speed = 1f;
			lState_35056.mirror = false;
			lState_35056.tag = "";

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_35006 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_35040, 0);
			if (lAnyTransition_35006 == null) { lAnyTransition_35006 = lLayerStateMachine.AddAnyStateTransition(lState_35040); }
			lAnyTransition_35006.isExit = false;
			lAnyTransition_35006.hasExitTime = false;
			lAnyTransition_35006.hasFixedDuration = true;
			lAnyTransition_35006.exitTime = 0.75f;
			lAnyTransition_35006.duration = 0.1f;
			lAnyTransition_35006.offset = 0.7810742f;
			lAnyTransition_35006.mute = false;
			lAnyTransition_35006.solo = false;
			lAnyTransition_35006.canTransitionToSelf = true;
			lAnyTransition_35006.orderedInterruption = true;
			lAnyTransition_35006.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_35006.conditions.Length - 1; i >= 0; i--) { lAnyTransition_35006.RemoveCondition(lAnyTransition_35006.conditions[i]); }
			lAnyTransition_35006.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3500f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_35006.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 500f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_35006.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_35008 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_35042, 0);
			if (lAnyTransition_35008 == null) { lAnyTransition_35008 = lLayerStateMachine.AddAnyStateTransition(lState_35042); }
			lAnyTransition_35008.isExit = false;
			lAnyTransition_35008.hasExitTime = false;
			lAnyTransition_35008.hasFixedDuration = true;
			lAnyTransition_35008.exitTime = 0.75f;
			lAnyTransition_35008.duration = 0.15f;
			lAnyTransition_35008.offset = 0f;
			lAnyTransition_35008.mute = false;
			lAnyTransition_35008.solo = false;
			lAnyTransition_35008.canTransitionToSelf = true;
			lAnyTransition_35008.orderedInterruption = true;
			lAnyTransition_35008.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_35008.conditions.Length - 1; i >= 0; i--) { lAnyTransition_35008.RemoveCondition(lAnyTransition_35008.conditions[i]); }
			lAnyTransition_35008.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3500f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_35008.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 500f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_35008.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_35020 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_35054, 0);
			if (lAnyTransition_35020 == null) { lAnyTransition_35020 = lLayerStateMachine.AddAnyStateTransition(lState_35054); }
			lAnyTransition_35020.isExit = false;
			lAnyTransition_35020.hasExitTime = false;
			lAnyTransition_35020.hasFixedDuration = true;
			lAnyTransition_35020.exitTime = 0.7500004f;
			lAnyTransition_35020.duration = 0.1f;
			lAnyTransition_35020.offset = 0.2146628f;
			lAnyTransition_35020.mute = false;
			lAnyTransition_35020.solo = false;
			lAnyTransition_35020.canTransitionToSelf = true;
			lAnyTransition_35020.orderedInterruption = true;
			lAnyTransition_35020.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_35020.conditions.Length - 1; i >= 0; i--) { lAnyTransition_35020.RemoveCondition(lAnyTransition_35020.conditions[i]); }
			lAnyTransition_35020.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3500f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_35020.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 550f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_35020.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_35022 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_35056, 0);
			if (lAnyTransition_35022 == null) { lAnyTransition_35022 = lLayerStateMachine.AddAnyStateTransition(lState_35056); }
			lAnyTransition_35022.isExit = false;
			lAnyTransition_35022.hasExitTime = false;
			lAnyTransition_35022.hasFixedDuration = true;
			lAnyTransition_35022.exitTime = 0.75f;
			lAnyTransition_35022.duration = 0.15f;
			lAnyTransition_35022.offset = 0f;
			lAnyTransition_35022.mute = false;
			lAnyTransition_35022.solo = false;
			lAnyTransition_35022.canTransitionToSelf = true;
			lAnyTransition_35022.orderedInterruption = true;
			lAnyTransition_35022.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_35022.conditions.Length - 1; i >= 0; i--) { lAnyTransition_35022.RemoveCondition(lAnyTransition_35022.conditions[i]); }
			lAnyTransition_35022.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3500f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_35022.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 550f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_35022.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35106 = MotionControllerMotion.EditorFindTransition(lState_35102, lState_35040, 0);
			if (lTransition_35106 == null) { lTransition_35106 = lState_35102.AddTransition(lState_35040); }
			lTransition_35106.isExit = false;
			lTransition_35106.hasExitTime = false;
			lTransition_35106.hasFixedDuration = true;
			lTransition_35106.exitTime = 0f;
			lTransition_35106.duration = 0.1f;
			lTransition_35106.offset = 0f;
			lTransition_35106.mute = false;
			lTransition_35106.solo = false;
			lTransition_35106.canTransitionToSelf = true;
			lTransition_35106.orderedInterruption = true;
			lTransition_35106.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35106.conditions.Length - 1; i >= 0; i--) { lTransition_35106.RemoveCondition(lTransition_35106.conditions[i]); }
			lTransition_35106.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3516f, "L" + rLayerIndex + "MotionPhase");
			lTransition_35106.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35108 = MotionControllerMotion.EditorFindTransition(lState_35102, lState_35042, 0);
			if (lTransition_35108 == null) { lTransition_35108 = lState_35102.AddTransition(lState_35042); }
			lTransition_35108.isExit = false;
			lTransition_35108.hasExitTime = false;
			lTransition_35108.hasFixedDuration = true;
			lTransition_35108.exitTime = 0f;
			lTransition_35108.duration = 0.1f;
			lTransition_35108.offset = 0f;
			lTransition_35108.mute = false;
			lTransition_35108.solo = false;
			lTransition_35108.canTransitionToSelf = true;
			lTransition_35108.orderedInterruption = true;
			lTransition_35108.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35108.conditions.Length - 1; i >= 0; i--) { lTransition_35108.RemoveCondition(lTransition_35108.conditions[i]); }
			lTransition_35108.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3516f, "L" + rLayerIndex + "MotionPhase");
			lTransition_35108.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35110 = MotionControllerMotion.EditorFindTransition(lState_35040, lState_35102, 0);
			if (lTransition_35110 == null) { lTransition_35110 = lState_35040.AddTransition(lState_35102); }
			lTransition_35110.isExit = false;
			lTransition_35110.hasExitTime = false;
			lTransition_35110.hasFixedDuration = true;
			lTransition_35110.exitTime = 0.3373314f;
			lTransition_35110.duration = 0.2f;
			lTransition_35110.offset = 0f;
			lTransition_35110.mute = false;
			lTransition_35110.solo = false;
			lTransition_35110.canTransitionToSelf = true;
			lTransition_35110.orderedInterruption = true;
			lTransition_35110.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35110.conditions.Length - 1; i >= 0; i--) { lTransition_35110.RemoveCondition(lTransition_35110.conditions[i]); }
			lTransition_35110.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3515f, "L" + rLayerIndex + "MotionPhase");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35112 = MotionControllerMotion.EditorFindTransition(lState_35040, lState_35042, 0);
			if (lTransition_35112 == null) { lTransition_35112 = lState_35040.AddTransition(lState_35042); }
			lTransition_35112.isExit = false;
			lTransition_35112.hasExitTime = false;
			lTransition_35112.hasFixedDuration = true;
			lTransition_35112.exitTime = 0.06250006f;
			lTransition_35112.duration = 0.1f;
			lTransition_35112.offset = 0f;
			lTransition_35112.mute = false;
			lTransition_35112.solo = false;
			lTransition_35112.canTransitionToSelf = true;
			lTransition_35112.orderedInterruption = true;
			lTransition_35112.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35112.conditions.Length - 1; i >= 0; i--) { lTransition_35112.RemoveCondition(lTransition_35112.conditions[i]); }
			lTransition_35112.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionPhase");
			lTransition_35112.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35116 = MotionControllerMotion.EditorFindTransition(lState_35042, lState_35040, 0);
			if (lTransition_35116 == null) { lTransition_35116 = lState_35042.AddTransition(lState_35040); }
			lTransition_35116.isExit = false;
			lTransition_35116.hasExitTime = false;
			lTransition_35116.hasFixedDuration = true;
			lTransition_35116.exitTime = 0.8809521f;
			lTransition_35116.duration = 0.1f;
			lTransition_35116.offset = 0.7522035f;
			lTransition_35116.mute = false;
			lTransition_35116.solo = false;
			lTransition_35116.canTransitionToSelf = true;
			lTransition_35116.orderedInterruption = true;
			lTransition_35116.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35116.conditions.Length - 1; i >= 0; i--) { lTransition_35116.RemoveCondition(lTransition_35116.conditions[i]); }
			lTransition_35116.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionPhase");
			lTransition_35116.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35118 = MotionControllerMotion.EditorFindTransition(lState_35042, lState_35102, 0);
			if (lTransition_35118 == null) { lTransition_35118 = lState_35042.AddTransition(lState_35102); }
			lTransition_35118.isExit = false;
			lTransition_35118.hasExitTime = false;
			lTransition_35118.hasFixedDuration = true;
			lTransition_35118.exitTime = 0f;
			lTransition_35118.duration = 0.2f;
			lTransition_35118.offset = 0f;
			lTransition_35118.mute = false;
			lTransition_35118.solo = false;
			lTransition_35118.canTransitionToSelf = true;
			lTransition_35118.orderedInterruption = true;
			lTransition_35118.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35118.conditions.Length - 1; i >= 0; i--) { lTransition_35118.RemoveCondition(lTransition_35118.conditions[i]); }
			lTransition_35118.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3515f, "L" + rLayerIndex + "MotionPhase");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35122 = MotionControllerMotion.EditorFindTransition(lState_35104, lState_35054, 0);
			if (lTransition_35122 == null) { lTransition_35122 = lState_35104.AddTransition(lState_35054); }
			lTransition_35122.isExit = false;
			lTransition_35122.hasExitTime = false;
			lTransition_35122.hasFixedDuration = true;
			lTransition_35122.exitTime = 0f;
			lTransition_35122.duration = 0.1f;
			lTransition_35122.offset = 0.07351331f;
			lTransition_35122.mute = false;
			lTransition_35122.solo = false;
			lTransition_35122.canTransitionToSelf = true;
			lTransition_35122.orderedInterruption = true;
			lTransition_35122.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35122.conditions.Length - 1; i >= 0; i--) { lTransition_35122.RemoveCondition(lTransition_35122.conditions[i]); }
			lTransition_35122.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3516f, "L" + rLayerIndex + "MotionPhase");
			lTransition_35122.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35124 = MotionControllerMotion.EditorFindTransition(lState_35104, lState_35056, 0);
			if (lTransition_35124 == null) { lTransition_35124 = lState_35104.AddTransition(lState_35056); }
			lTransition_35124.isExit = false;
			lTransition_35124.hasExitTime = false;
			lTransition_35124.hasFixedDuration = true;
			lTransition_35124.exitTime = 0f;
			lTransition_35124.duration = 0.1f;
			lTransition_35124.offset = 0f;
			lTransition_35124.mute = false;
			lTransition_35124.solo = false;
			lTransition_35124.canTransitionToSelf = true;
			lTransition_35124.orderedInterruption = true;
			lTransition_35124.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35124.conditions.Length - 1; i >= 0; i--) { lTransition_35124.RemoveCondition(lTransition_35124.conditions[i]); }
			lTransition_35124.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3516f, "L" + rLayerIndex + "MotionPhase");
			lTransition_35124.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35126 = MotionControllerMotion.EditorFindTransition(lState_35054, lState_35104, 0);
			if (lTransition_35126 == null) { lTransition_35126 = lState_35054.AddTransition(lState_35104); }
			lTransition_35126.isExit = false;
			lTransition_35126.hasExitTime = false;
			lTransition_35126.hasFixedDuration = true;
			lTransition_35126.exitTime = 0.5722023f;
			lTransition_35126.duration = 0.09999996f;
			lTransition_35126.offset = 82.20575f;
			lTransition_35126.mute = false;
			lTransition_35126.solo = false;
			lTransition_35126.canTransitionToSelf = true;
			lTransition_35126.orderedInterruption = true;
			lTransition_35126.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35126.conditions.Length - 1; i >= 0; i--) { lTransition_35126.RemoveCondition(lTransition_35126.conditions[i]); }
			lTransition_35126.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3515f, "L" + rLayerIndex + "MotionPhase");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35128 = MotionControllerMotion.EditorFindTransition(lState_35054, lState_35056, 0);
			if (lTransition_35128 == null) { lTransition_35128 = lState_35054.AddTransition(lState_35056); }
			lTransition_35128.isExit = false;
			lTransition_35128.hasExitTime = false;
			lTransition_35128.hasFixedDuration = true;
			lTransition_35128.exitTime = 0.611675f;
			lTransition_35128.duration = 0.0999999f;
			lTransition_35128.offset = 192.21f;
			lTransition_35128.mute = false;
			lTransition_35128.solo = false;
			lTransition_35128.canTransitionToSelf = true;
			lTransition_35128.orderedInterruption = true;
			lTransition_35128.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35128.conditions.Length - 1; i >= 0; i--) { lTransition_35128.RemoveCondition(lTransition_35128.conditions[i]); }
			lTransition_35128.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionPhase");
			lTransition_35128.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35132 = MotionControllerMotion.EditorFindTransition(lState_35056, lState_35054, 0);
			if (lTransition_35132 == null) { lTransition_35132 = lState_35056.AddTransition(lState_35054); }
			lTransition_35132.isExit = false;
			lTransition_35132.hasExitTime = false;
			lTransition_35132.hasFixedDuration = true;
			lTransition_35132.exitTime = 0.8809586f;
			lTransition_35132.duration = 0.1f;
			lTransition_35132.offset = 0.2146628f;
			lTransition_35132.mute = false;
			lTransition_35132.solo = false;
			lTransition_35132.canTransitionToSelf = true;
			lTransition_35132.orderedInterruption = true;
			lTransition_35132.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35132.conditions.Length - 1; i >= 0; i--) { lTransition_35132.RemoveCondition(lTransition_35132.conditions[i]); }
			lTransition_35132.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionPhase");
			lTransition_35132.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_35134 = MotionControllerMotion.EditorFindTransition(lState_35056, lState_35104, 0);
			if (lTransition_35134 == null) { lTransition_35134 = lState_35056.AddTransition(lState_35104); }
			lTransition_35134.isExit = false;
			lTransition_35134.hasExitTime = false;
			lTransition_35134.hasFixedDuration = true;
			lTransition_35134.exitTime = 0f;
			lTransition_35134.duration = 0.2f;
			lTransition_35134.offset = 0f;
			lTransition_35134.mute = false;
			lTransition_35134.solo = false;
			lTransition_35134.canTransitionToSelf = true;
			lTransition_35134.orderedInterruption = true;
			lTransition_35134.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_35134.conditions.Length - 1; i >= 0; i--) { lTransition_35134.RemoveCondition(lTransition_35134.conditions[i]); }
			lTransition_35134.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3515f, "L" + rLayerIndex + "MotionPhase");

		}
		
		#endregion Definition

	}
}
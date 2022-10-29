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
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace WildWalrus.Actors.AnimationControllers
{
	[MotionName("Rifle AP Basic Shooter Attack")]
	[MotionDescription("Kubold's RIfle Anim Set Pro. Basic attacks that use guns instead of projectiles.")]
	public class RifleAP_BasicShooterAttack : BasicShooterMotion
	{
		protected string TAG_FIRE = "Fire";
		protected string EVENT_FIRE = "fire";

		// Enum values for the motion
		public int PHASE_UNKNOWN = 0;
		public int PHASE_START = 73500;
		public int PHASE_START_AUTO = 73510;
		public int PHASE_STOP = 73515;
		public int PHASE_RESTART = 73516;

		/// <summary>
		/// ID of the slot that holds the weapon
		/// </summary>
		public string _WeaponSlotID = "RIGHT_HAND";
		public string WeaponSlotID
		{
			get { return _WeaponSlotID; }
			set { _WeaponSlotID = value; }
		}

		/// <summary>
		/// Determines if we can shoot when running
		/// </summary>
		public bool _AllowWhenRunning = false;
		public bool AllowWhenRunning
		{
			get { return _AllowWhenRunning; }
			set { _AllowWhenRunning = value; }
		}

		/// <summary>
		/// Alias used to aim and target
		/// </summary>
		public string _AimActionAlias = "Camera Aim";
		public string AimActionAlias
		{
			get { return _AimActionAlias; }
			set { _AimActionAlias = value; }
		}

		/// <summary>
		/// Alias used for the alternate aim and target
		/// </summary>
		public string _AltAimActionAlias = "Alt Camera Aim";
		public string AltAimActionAlias
		{
			get { return _AltAimActionAlias; }
			set { _AltAimActionAlias = value; }
		}

		/// <summary>
		/// Determines if we use the aiming to manage the crosshairs
		/// </summary>
		public bool _ManageCrosshair = false;
		public bool ManageCrosshair
		{
			get { return _ManageCrosshair; }
			set { _ManageCrosshair = value; }
		}

		/// <summary>
		/// Camera rig mode/motor to swap to for right aim
		/// </summary>
		public int _RightAimCameraRigMode = 5;
		public int RightAimCameraRigMode
		{
			get { return _RightAimCameraRigMode; }
			set { _RightAimCameraRigMode = value; }
		}

		/// <summary>
		/// Camera rig mode/motor to swap to for left aim
		/// </summary>
		public int _LeftAimCameraRigMode = 4;
		public int LeftAimCameraRigMode
		{
			get { return _LeftAimCameraRigMode; }
			set { _LeftAimCameraRigMode = value; }
		}

		// Determines if the fire button is down (used to help with overdraw)
		protected bool mIsFiring = false;

		// Determine if we're doing a quick shot or holding for aim
		protected bool mIsAiming = false;

		// Determines if we're using the alternate aiming view
		protected bool mIsAltAiming = false;
		protected bool mWasAltAiming = false;

		// Determine if we've activated the IK for the first time
		protected bool mIsIKActivated = false;

		/// <summary>
		/// Determines if we link the actor rotation to the camera rotation
		/// </summary>
		protected bool mIsLinked = false;

		/// <summary>
		/// Determines if the actor rotation should be linked to the camera
		/// </summary>
		protected bool mLinkRotation = false;

		/// <summary>
		/// Track the angle we have from the input
		/// </summary>
		protected Vector3 mStoredInputForward = Vector3.forward;

		/// <summary>
		/// Determines if we will use the camera update to rotate while not pivoting
		/// </summary>
		protected bool mUseCameraUpdate = false;

		/// <summary>
		/// Fields to help smooth out the mouse rotation
		/// </summary>
		protected float mYaw = 0f;
		protected float mYawTarget = 0f;
		protected float mYawVelocity = 0f;

		/// <summary>
		/// Rotation target we're heading to
		/// </summary>
		protected Vector3 mTargetForward = Vector3.zero;

		// Determine if the rotation is locked to the camera
		protected bool mIsRotationLocked = false;

		// Determine if the reticle was visible before the motion
		protected bool mWasReticleVisible = false;

		// Track the cover position we were at when this motion was activated
		protected Vector3 mCoverPosition = Vector3.zero;

		// Track the cover rotation we were at when this motion was activated
		protected Quaternion mCoverRotation = Quaternion.identity;

		// Motion that was being used for cover
		protected ICoverMotion mCoverMotion = null;

		/// <summary>
		/// Default constructor
		/// </summary>
		public RifleAP_BasicShooterAttack()
			 : base()
		{
			_Pack = ShooterPackDefinition.PackName;
			_Category = EnumMotionCategories.COMBAT_SHOOTING;

			_Priority = 15;
			_ActionAlias = "Combat Attack";

			if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "RifleAP_BasicShooterAttack-SM"; }
		}

		/// <summary>
		/// Controller constructor
		/// </summary>
		/// <param name="rController">Controller the motion belongs to</param>
		public RifleAP_BasicShooterAttack(MotionController rController)
			 : base(rController)
		{
			_Pack = ShooterPackDefinition.PackName;
			_Category = EnumMotionCategories.COMBAT_SHOOTING;

			_Priority = 15;
			_ActionAlias = "Combat Attack";

			if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "RifleAP_BasicShooterAttack-SM"; }
		}

		/// <summary>
		/// Allows for any processing after the motion has been deserialized
		/// </summary>
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

		/// <summary>
		/// Tests if this motion should be started. However, the motion
		/// isn't actually started.
		/// </summary>
		/// <returns></returns>
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
					GunCore lGunCore = FindWeapon(WeaponSlotID);
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

		/// <summary>
		/// Tests if the motion should continue. If it shouldn't, the motion
		/// is typically disabled
		/// </summary>
		/// <returns>Boolean that determines if the motion continues</returns>
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

		/// <summary>
		/// Raised when a motion is being interrupted by another motion
		/// </summary>
		/// <param name="rMotion">Motion doing the interruption</param>
		/// <returns>Boolean determining if it can be interrupted</returns>
		public override bool TestInterruption(MotionControllerMotion rMotion)
		{
			//Debug.Log("BSA.TestInterrupt(" + rMotion.GetType().Name + ")");

			if (mLookIKWeight > 0f && !(rMotion is BasicShooterEmpty))
			{
				EaseOutIK(_LookIKOutSpeed);
			}

			return true;
		}

		/// <summary>
		/// Called to start the specific motion. If the motion
		/// were something like 'jump', this would start the jumping process
		/// </summary>
		/// <param name="rPrevMotion">Motion that this motion is taking over from</param>
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
			if (rPrevMotion is BasicShooterEmpty)
			{
				mLookIKWeight = ((BasicShooterEmpty)rPrevMotion).LookIKWeight;
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

		/// <summary>
		/// Called to stop the motion. If the motion is stopable. Some motions
		/// like jump cannot be stopped early
		/// </summary>
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

		/// <summary>
		/// Allows the motion to modify the root-motion velocities before they are applied. 
		/// 
		/// NOTE:
		/// Be careful when removing rotations as some transitions will want rotations even 
		/// if the state they are transitioning from don't.
		/// </summary>
		/// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
		/// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
		/// <param name="rVelocityDelta">Root-motion linear velocity relative to the actor's forward</param>
		/// <param name="rRotationDelta">Root-motion rotational velocity</param>
		/// <returns></returns>
		public override void UpdateRootMotion(float rDeltaTime, int rUpdateIndex, ref Vector3 rMovement, ref Quaternion rRotation)
		{
			rMovement = Vector3.zero;
			rRotation = Quaternion.identity;
		}

		/// <summary>
		/// Updates the motion over time. This is called by the controller
		/// every update cycle so animations and stages can be updated.
		/// </summary>
		/// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
		/// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
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
			else
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

		/// <summary>
		/// When a motion is deactivated, it may need to live to do some clean up. In this case,
		/// this function will run. However, the motion should not expect to be in sync with the animator.
		/// Once the motion returns 'false', the updates will stop.
		/// </summary>
		/// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
		/// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
		/// <returns>Return true to continue the deactivated updates or false to stop them</returns>
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

		/// <summary>
		/// Create a rotation velocity that rotates the character based on input
		/// </summary>
		/// <param name="rDeltaTime"></param>
		/// <param name="rAngularVelocity"></param>
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

		/// <summary>
		/// Create a rotation velocity that rotates the character based on input
		/// </summary>
		/// <param name="rInputFromAvatarAngle"></param>
		/// <param name="rDeltaTime"></param>
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

		/// <summary>
		/// When we want to rotate based on the camera direction (which input does), we need to tweak the actor
		/// rotation AFTER we process the camera. Otherwise, we can get small stutters during camera rotation. 
		/// 
		/// This is the only way to keep them totally in sync. It also means we can't run any of our AC processing
		/// as the AC already ran. So, we do minimal work here
		/// </summary>
		/// <param name="rDeltaTime"></param>
		/// <param name="rUpdateCount"></param>
		/// <param name="rCamera"></param>
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

		/// <summary>
		/// Raised by the controller when a message is received
		/// </summary>
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

		// **************************************************************************************************
		// Following properties and function only valid while editing
		// **************************************************************************************************

#if UNITY_EDITOR

		/// <summary>
		/// Allow the constraint to render it's own GUI
		/// </summary>
		/// <returns>Reports if the object's value was changed</returns>
		public override bool OnInspectorGUI()
		{
			bool lIsDirty = false;

			if (EditorHelper.TextField("Aim Alias", "Action alias that has us aiming.", AimActionAlias, mMotionController))
			{
				lIsDirty = true;
				AimActionAlias = EditorHelper.FieldStringValue;
			}

			if (EditorHelper.TextField("Alt Aim Alias", "Toggles the alternate aiming view.", AltAimActionAlias, mMotionController))
			{
				lIsDirty = true;
				AltAimActionAlias = EditorHelper.FieldStringValue;
			}

			// Alt aim
			if (AltAimActionAlias.Length > 0)
			{
				GUILayout.BeginHorizontal();

				EditorGUILayout.LabelField(new GUIContent("Alt Aim Modes", "Mode (or motor indexes) to use when entering and leaving the alt aim position."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

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

			if (EditorHelper.BoolField("Manage Reticle", "Determines if this motion will enable/disable the reticle.", ManageCrosshair, mMotionController))
			{
				lIsDirty = true;
				ManageCrosshair = EditorHelper.FieldBoolValue;
			}

			GUILayout.Space(5f);

			if (EditorHelper.TextField("Attack Alias", "Action alias that is required to trigger the attack.", ActionAlias, mMotionController))
			{
				lIsDirty = true;
				ActionAlias = EditorHelper.FieldStringValue;
			}

			if (EditorHelper.BoolField("Allow When Running", "Determines if we can shoot an aim when running.", AllowWhenRunning, mMotionController))
			{
				lIsDirty = true;
				AllowWhenRunning = EditorHelper.FieldBoolValue;
			}

			GUILayout.Space(5f);

			if (EditorHelper.BoolField("Rotate With Input", "Determines if we rotate based on user input.", RotateWithInput, mMotionController))
			{
				lIsDirty = true;
				RotateWithInput = EditorHelper.FieldBoolValue;
			}

			if (EditorHelper.BoolField("Rotate With Camera", "Determines if we rotate to match the camera.", RotateWithCamera, mMotionController))
			{
				lIsDirty = true;
				RotateWithCamera = EditorHelper.FieldBoolValue;
			}

			if (EditorHelper.FloatField("Rotation Speed", "Degrees per second to rotate the actor to the camera's forward.", RotationSpeed, mMotionController))
			{
				lIsDirty = true;
				RotationSpeed = EditorHelper.FieldFloatValue;
			}

			GUILayout.Space(5f);

			EditorHelper.DrawInspectorDescription("IK properties for aiming the gun", MessageType.None);

			if (EditorHelper.BoolField("Enable Look IK", "Determines if we'll use the camera to rotate the spine forward.", IsLookIKEnabled, mMotionController))
			{
				lIsDirty = true;
				IsLookIKEnabled = EditorHelper.FieldBoolValue;
			}

			if (IsLookIKEnabled)
			{
				// IK Angles
				GUILayout.BeginHorizontal();

				EditorGUILayout.LabelField(new GUIContent("IK Time", "Time in seconds to transition the IK in and out."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

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

				EditorGUILayout.LabelField(new GUIContent("IK Angles", "Additional IK angles used when aiming."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

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

				EditorGUILayout.LabelField(new GUIContent("Quick IK Angles", "Additional IK angles used when fast shooting."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

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

			if (EditorHelper.BoolField("Enable Arm IK", "Determines if we use the 'Support' transform for arm IK.", IsSupportIKEnabled, mMotionController))
			{
				lIsDirty = true;
				IsSupportIKEnabled = EditorHelper.FieldBoolValue;
			}

			GUILayout.Space(5f);

			EditorHelper.DrawInspectorDescription("Inventory information about the weapon.", MessageType.None);

			GameObject lNewAttributeSourceOwner = EditorHelper.InterfaceOwnerField<IInventorySource>(new GUIContent("Inventory Source", "Inventory source we'll use for accessing items and slots."), InventorySourceOwner, true);
			if (lNewAttributeSourceOwner != InventorySourceOwner)
			{
				lIsDirty = true;
				InventorySourceOwner = lNewAttributeSourceOwner;
			}

			if (EditorHelper.TextField("Weapon Slot ID", "Inventory slot ID holding the weapon.", WeaponSlotID, mMotionController))
			{
				lIsDirty = true;
				WeaponSlotID = EditorHelper.FieldStringValue;
			}

			return lIsDirty;
		}

#endif

		#region Auto-Generated
		// ************************************ START AUTO GENERATED ************************************

		/// <summary>
		/// These declarations go inside the class so you can test for which state
		/// and transitions are active. Testing hash values is much faster than strings.
		/// </summary>
		public int STATE_Empty = -1;
		public int STATE_RifleIdlePoseExit = -1;
		public int STATE_RifleShootOnce = -1;
		public int STATE_RifleIdlePose = -1;
		public int STATE_PistolIdlePoseExit = -1;
		public int STATE_PistolShootOnce = -1;
		public int STATE_PistolIdlePose = -1;
		public int TRANS_AnyState_RifleShootOnce = -1;
		public int TRANS_EntryState_RifleShootOnce = -1;
		public int TRANS_AnyState_RifleIdlePose = -1;
		public int TRANS_EntryState_RifleIdlePose = -1;
		public int TRANS_AnyState_PistolShootOnce = -1;
		public int TRANS_EntryState_PistolShootOnce = -1;
		public int TRANS_AnyState_PistolIdlePose = -1;
		public int TRANS_EntryState_PistolIdlePose = -1;
		public int TRANS_RifleIdlePoseExit_RifleShootOnce = -1;
		public int TRANS_RifleIdlePoseExit_RifleIdlePose = -1;
		public int TRANS_RifleShootOnce_RifleIdlePoseExit = -1;
		public int TRANS_RifleShootOnce_RifleIdlePose = -1;
		public int TRANS_RifleIdlePose_RifleShootOnce = -1;
		public int TRANS_RifleIdlePose_RifleIdlePoseExit = -1;
		public int TRANS_PistolIdlePoseExit_PistolShootOnce = -1;
		public int TRANS_PistolIdlePoseExit_PistolIdlePose = -1;
		public int TRANS_PistolShootOnce_PistolIdlePoseExit = -1;
		public int TRANS_PistolShootOnce_PistolIdlePose = -1;
		public int TRANS_PistolIdlePose_PistolShootOnce = -1;
		public int TRANS_PistolIdlePose_PistolIdlePoseExit = -1;

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
					if (lStateID == STATE_RifleShootOnce) { return true; }
					if (lStateID == STATE_RifleIdlePose) { return true; }
					if (lStateID == STATE_PistolIdlePoseExit) { return true; }
					if (lStateID == STATE_PistolShootOnce) { return true; }
					if (lStateID == STATE_PistolIdlePose) { return true; }
				}

				if (lTransitionID == TRANS_AnyState_RifleShootOnce) { return true; }
				if (lTransitionID == TRANS_EntryState_RifleShootOnce) { return true; }
				if (lTransitionID == TRANS_AnyState_RifleIdlePose) { return true; }
				if (lTransitionID == TRANS_EntryState_RifleIdlePose) { return true; }
				if (lTransitionID == TRANS_AnyState_PistolShootOnce) { return true; }
				if (lTransitionID == TRANS_EntryState_PistolShootOnce) { return true; }
				if (lTransitionID == TRANS_AnyState_PistolIdlePose) { return true; }
				if (lTransitionID == TRANS_EntryState_PistolIdlePose) { return true; }
				if (lTransitionID == TRANS_RifleIdlePoseExit_RifleShootOnce) { return true; }
				if (lTransitionID == TRANS_RifleIdlePoseExit_RifleIdlePose) { return true; }
				if (lTransitionID == TRANS_RifleShootOnce_RifleIdlePoseExit) { return true; }
				if (lTransitionID == TRANS_RifleShootOnce_RifleIdlePose) { return true; }
				if (lTransitionID == TRANS_RifleIdlePose_RifleShootOnce) { return true; }
				if (lTransitionID == TRANS_RifleIdlePose_RifleIdlePoseExit) { return true; }
				if (lTransitionID == TRANS_PistolIdlePoseExit_PistolShootOnce) { return true; }
				if (lTransitionID == TRANS_PistolIdlePoseExit_PistolIdlePose) { return true; }
				if (lTransitionID == TRANS_PistolShootOnce_PistolIdlePoseExit) { return true; }
				if (lTransitionID == TRANS_PistolShootOnce_PistolIdlePose) { return true; }
				if (lTransitionID == TRANS_PistolIdlePose_PistolShootOnce) { return true; }
				if (lTransitionID == TRANS_PistolIdlePose_PistolIdlePoseExit) { return true; }
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
			if (rStateID == STATE_RifleShootOnce) { return true; }
			if (rStateID == STATE_RifleIdlePose) { return true; }
			if (rStateID == STATE_PistolIdlePoseExit) { return true; }
			if (rStateID == STATE_PistolShootOnce) { return true; }
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
				if (rStateID == STATE_Empty) { return true; }
				if (rStateID == STATE_RifleIdlePoseExit) { return true; }
				if (rStateID == STATE_RifleShootOnce) { return true; }
				if (rStateID == STATE_RifleIdlePose) { return true; }
				if (rStateID == STATE_PistolIdlePoseExit) { return true; }
				if (rStateID == STATE_PistolShootOnce) { return true; }
				if (rStateID == STATE_PistolIdlePose) { return true; }
			}

			if (rTransitionID == TRANS_AnyState_RifleShootOnce) { return true; }
			if (rTransitionID == TRANS_EntryState_RifleShootOnce) { return true; }
			if (rTransitionID == TRANS_AnyState_RifleIdlePose) { return true; }
			if (rTransitionID == TRANS_EntryState_RifleIdlePose) { return true; }
			if (rTransitionID == TRANS_AnyState_PistolShootOnce) { return true; }
			if (rTransitionID == TRANS_EntryState_PistolShootOnce) { return true; }
			if (rTransitionID == TRANS_AnyState_PistolIdlePose) { return true; }
			if (rTransitionID == TRANS_EntryState_PistolIdlePose) { return true; }
			if (rTransitionID == TRANS_RifleIdlePoseExit_RifleShootOnce) { return true; }
			if (rTransitionID == TRANS_RifleIdlePoseExit_RifleIdlePose) { return true; }
			if (rTransitionID == TRANS_RifleShootOnce_RifleIdlePoseExit) { return true; }
			if (rTransitionID == TRANS_RifleShootOnce_RifleIdlePose) { return true; }
			if (rTransitionID == TRANS_RifleIdlePose_RifleShootOnce) { return true; }
			if (rTransitionID == TRANS_RifleIdlePose_RifleIdlePoseExit) { return true; }
			if (rTransitionID == TRANS_PistolIdlePoseExit_PistolShootOnce) { return true; }
			if (rTransitionID == TRANS_PistolIdlePoseExit_PistolIdlePose) { return true; }
			if (rTransitionID == TRANS_PistolShootOnce_PistolIdlePoseExit) { return true; }
			if (rTransitionID == TRANS_PistolShootOnce_PistolIdlePose) { return true; }
			if (rTransitionID == TRANS_PistolIdlePose_PistolShootOnce) { return true; }
			if (rTransitionID == TRANS_PistolIdlePose_PistolIdlePoseExit) { return true; }
			return false;
		}

		/// <summary>
		/// Preprocess any animator data so the motion can use it later
		/// </summary>
		public override void LoadAnimatorData()
		{
			string lLayer = mMotionController.Animator.GetLayerName(mMotionLayer._AnimatorLayerIndex);
			TRANS_AnyState_RifleShootOnce = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".RifleAP_BasicShooterAttack-SM.Rifle ShootOnce");
			TRANS_EntryState_RifleShootOnce = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".RifleAP_BasicShooterAttack-SM.Rifle ShootOnce");
			TRANS_AnyState_RifleIdlePose = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".RifleAP_BasicShooterAttack-SM.Rifle Idle Pose");
			TRANS_EntryState_RifleIdlePose = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".RifleAP_BasicShooterAttack-SM.Rifle Idle Pose");
			TRANS_AnyState_PistolShootOnce = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".RifleAP_BasicShooterAttack-SM.Pistol ShootOnce");
			TRANS_EntryState_PistolShootOnce = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".RifleAP_BasicShooterAttack-SM.Pistol ShootOnce");
			TRANS_AnyState_PistolIdlePose = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".RifleAP_BasicShooterAttack-SM.Pistol Idle Pose");
			TRANS_EntryState_PistolIdlePose = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".RifleAP_BasicShooterAttack-SM.Pistol Idle Pose");
			STATE_Empty = mMotionController.AddAnimatorName("" + lLayer + ".Empty");
			STATE_RifleIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicShooterAttack-SM.Rifle Idle Pose Exit");
			TRANS_RifleIdlePoseExit_RifleShootOnce = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicShooterAttack-SM.Rifle Idle Pose Exit -> " + lLayer + ".RifleAP_BasicShooterAttack-SM.Rifle ShootOnce");
			TRANS_RifleIdlePoseExit_RifleIdlePose = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicShooterAttack-SM.Rifle Idle Pose Exit -> " + lLayer + ".RifleAP_BasicShooterAttack-SM.Rifle Idle Pose");
			STATE_RifleShootOnce = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicShooterAttack-SM.Rifle ShootOnce");
			TRANS_RifleShootOnce_RifleIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicShooterAttack-SM.Rifle ShootOnce -> " + lLayer + ".RifleAP_BasicShooterAttack-SM.Rifle Idle Pose Exit");
			TRANS_RifleShootOnce_RifleIdlePose = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicShooterAttack-SM.Rifle ShootOnce -> " + lLayer + ".RifleAP_BasicShooterAttack-SM.Rifle Idle Pose");
			STATE_RifleIdlePose = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicShooterAttack-SM.Rifle Idle Pose");
			TRANS_RifleIdlePose_RifleShootOnce = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicShooterAttack-SM.Rifle Idle Pose -> " + lLayer + ".RifleAP_BasicShooterAttack-SM.Rifle ShootOnce");
			TRANS_RifleIdlePose_RifleIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicShooterAttack-SM.Rifle Idle Pose -> " + lLayer + ".RifleAP_BasicShooterAttack-SM.Rifle Idle Pose Exit");
			STATE_PistolIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicShooterAttack-SM.Pistol Idle Pose Exit");
			TRANS_PistolIdlePoseExit_PistolShootOnce = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicShooterAttack-SM.Pistol Idle Pose Exit -> " + lLayer + ".RifleAP_BasicShooterAttack-SM.Pistol ShootOnce");
			TRANS_PistolIdlePoseExit_PistolIdlePose = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicShooterAttack-SM.Pistol Idle Pose Exit -> " + lLayer + ".RifleAP_BasicShooterAttack-SM.Pistol Idle Pose");
			STATE_PistolShootOnce = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicShooterAttack-SM.Pistol ShootOnce");
			TRANS_PistolShootOnce_PistolIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicShooterAttack-SM.Pistol ShootOnce -> " + lLayer + ".RifleAP_BasicShooterAttack-SM.Pistol Idle Pose Exit");
			TRANS_PistolShootOnce_PistolIdlePose = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicShooterAttack-SM.Pistol ShootOnce -> " + lLayer + ".RifleAP_BasicShooterAttack-SM.Pistol Idle Pose");
			STATE_PistolIdlePose = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicShooterAttack-SM.Pistol Idle Pose");
			TRANS_PistolIdlePose_PistolShootOnce = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicShooterAttack-SM.Pistol Idle Pose -> " + lLayer + ".RifleAP_BasicShooterAttack-SM.Pistol ShootOnce");
			TRANS_PistolIdlePose_PistolIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicShooterAttack-SM.Pistol Idle Pose -> " + lLayer + ".RifleAP_BasicShooterAttack-SM.Pistol Idle Pose Exit");
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

			UnityEditor.Animations.AnimatorStateMachine lSSM_31650 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "RifleAP_BasicShooterAttack-SM");
			if (lSSM_31650 == null) { lSSM_31650 = lLayerStateMachine.AddStateMachine("RifleAP_BasicShooterAttack-SM", new Vector3(240, -910, 0)); }

			UnityEditor.Animations.AnimatorState lState_32850 = MotionControllerMotion.EditorFindState(lSSM_31650, "Rifle Idle Pose Exit");
			if (lState_32850 == null) { lState_32850 = lSSM_31650.AddState("Rifle Idle Pose Exit", new Vector3(564, 0, 0)); }
			lState_32850.speed = 1f;
			lState_32850.mirror = false;
			lState_32850.tag = "Exit";
			lState_32850.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_Idle_Pose");

			UnityEditor.Animations.AnimatorState lState_32852 = MotionControllerMotion.EditorFindState(lSSM_31650, "Rifle ShootOnce");
			if (lState_32852 == null) { lState_32852 = lSSM_31650.AddState("Rifle ShootOnce", new Vector3(310, -30, 0)); }
			lState_32852.speed = 1f;
			lState_32852.mirror = false;
			lState_32852.tag = "";
			lState_32852.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_ShootOnce");

			UnityEditor.Animations.AnimatorState lState_32854 = MotionControllerMotion.EditorFindState(lSSM_31650, "Rifle Idle Pose");
			if (lState_32854 == null) { lState_32854 = lSSM_31650.AddState("Rifle Idle Pose", new Vector3(310, 40, 0)); }
			lState_32854.speed = 1f;
			lState_32854.mirror = false;
			lState_32854.tag = "";
			lState_32854.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_Idle_Pose");

			UnityEditor.Animations.AnimatorState lState_32856 = MotionControllerMotion.EditorFindState(lSSM_31650, "Pistol Idle Pose Exit");
			if (lState_32856 == null) { lState_32856 = lSSM_31650.AddState("Pistol Idle Pose Exit", new Vector3(560, 160, 0)); }
			lState_32856.speed = 1f;
			lState_32856.mirror = false;
			lState_32856.tag = "Exit";
			lState_32856.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/PistolAnimsetPro/Animations/PistolAnimsetPro.fbx", "Pistol_Idle_Pose");

			UnityEditor.Animations.AnimatorState lState_32858 = MotionControllerMotion.EditorFindState(lSSM_31650, "Pistol ShootOnce");
			if (lState_32858 == null) { lState_32858 = lSSM_31650.AddState("Pistol ShootOnce", new Vector3(312, 132, 0)); }
			lState_32858.speed = 1f;
			lState_32858.mirror = false;
			lState_32858.tag = "";
			lState_32858.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/PistolAnimsetPro/Animations/PistolAnimsetPro.fbx", "Pistol_ShootOnce");

			UnityEditor.Animations.AnimatorState lState_32860 = MotionControllerMotion.EditorFindState(lSSM_31650, "Pistol Idle Pose");
			if (lState_32860 == null) { lState_32860 = lSSM_31650.AddState("Pistol Idle Pose", new Vector3(312, 204, 0)); }
			lState_32860.speed = 1f;
			lState_32860.mirror = false;
			lState_32860.tag = "";
			lState_32860.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/PistolAnimsetPro/Animations/PistolAnimsetPro.fbx", "Pistol_Idle_Pose");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33194 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_32852, 0);
			if (lAnyTransition_33194 == null) { lAnyTransition_33194 = lLayerStateMachine.AddAnyStateTransition(lState_32852); }
			lAnyTransition_33194.isExit = false;
			lAnyTransition_33194.hasExitTime = false;
			lAnyTransition_33194.hasFixedDuration = true;
			lAnyTransition_33194.exitTime = 0.75f;
			lAnyTransition_33194.duration = 0.1f;
			lAnyTransition_33194.offset = 0.7810742f;
			lAnyTransition_33194.mute = false;
			lAnyTransition_33194.solo = false;
			lAnyTransition_33194.canTransitionToSelf = true;
			lAnyTransition_33194.orderedInterruption = true;
			lAnyTransition_33194.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_33194.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33194.RemoveCondition(lAnyTransition_33194.conditions[i]); }
			lAnyTransition_33194.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73500f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33194.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 500f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_33194.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33196 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_32854, 0);
			if (lAnyTransition_33196 == null) { lAnyTransition_33196 = lLayerStateMachine.AddAnyStateTransition(lState_32854); }
			lAnyTransition_33196.isExit = false;
			lAnyTransition_33196.hasExitTime = false;
			lAnyTransition_33196.hasFixedDuration = true;
			lAnyTransition_33196.exitTime = 0.75f;
			lAnyTransition_33196.duration = 0.15f;
			lAnyTransition_33196.offset = 0f;
			lAnyTransition_33196.mute = false;
			lAnyTransition_33196.solo = false;
			lAnyTransition_33196.canTransitionToSelf = true;
			lAnyTransition_33196.orderedInterruption = true;
			lAnyTransition_33196.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_33196.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33196.RemoveCondition(lAnyTransition_33196.conditions[i]); }
			lAnyTransition_33196.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73500f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33196.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 500f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_33196.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33198 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_32858, 0);
			if (lAnyTransition_33198 == null) { lAnyTransition_33198 = lLayerStateMachine.AddAnyStateTransition(lState_32858); }
			lAnyTransition_33198.isExit = false;
			lAnyTransition_33198.hasExitTime = false;
			lAnyTransition_33198.hasFixedDuration = true;
			lAnyTransition_33198.exitTime = 0.75f;
			lAnyTransition_33198.duration = 0.1f;
			lAnyTransition_33198.offset = 0.2146628f;
			lAnyTransition_33198.mute = false;
			lAnyTransition_33198.solo = false;
			lAnyTransition_33198.canTransitionToSelf = true;
			lAnyTransition_33198.orderedInterruption = true;
			lAnyTransition_33198.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_33198.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33198.RemoveCondition(lAnyTransition_33198.conditions[i]); }
			lAnyTransition_33198.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73500f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33198.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 550f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_33198.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33200 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_32860, 0);
			if (lAnyTransition_33200 == null) { lAnyTransition_33200 = lLayerStateMachine.AddAnyStateTransition(lState_32860); }
			lAnyTransition_33200.isExit = false;
			lAnyTransition_33200.hasExitTime = false;
			lAnyTransition_33200.hasFixedDuration = true;
			lAnyTransition_33200.exitTime = 0.75f;
			lAnyTransition_33200.duration = 0.15f;
			lAnyTransition_33200.offset = 0f;
			lAnyTransition_33200.mute = false;
			lAnyTransition_33200.solo = false;
			lAnyTransition_33200.canTransitionToSelf = true;
			lAnyTransition_33200.orderedInterruption = true;
			lAnyTransition_33200.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_33200.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33200.RemoveCondition(lAnyTransition_33200.conditions[i]); }
			lAnyTransition_33200.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73500f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33200.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 550f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_33200.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39284 = MotionControllerMotion.EditorFindTransition(lState_32850, lState_32852, 0);
			if (lTransition_39284 == null) { lTransition_39284 = lState_32850.AddTransition(lState_32852); }
			lTransition_39284.isExit = false;
			lTransition_39284.hasExitTime = false;
			lTransition_39284.hasFixedDuration = true;
			lTransition_39284.exitTime = 0f;
			lTransition_39284.duration = 0.1f;
			lTransition_39284.offset = 0f;
			lTransition_39284.mute = false;
			lTransition_39284.solo = false;
			lTransition_39284.canTransitionToSelf = true;
			lTransition_39284.orderedInterruption = true;
			lTransition_39284.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39284.conditions.Length - 1; i >= 0; i--) { lTransition_39284.RemoveCondition(lTransition_39284.conditions[i]); }
			lTransition_39284.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73516f, "L" + rLayerIndex + "MotionPhase");
			lTransition_39284.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39286 = MotionControllerMotion.EditorFindTransition(lState_32850, lState_32854, 0);
			if (lTransition_39286 == null) { lTransition_39286 = lState_32850.AddTransition(lState_32854); }
			lTransition_39286.isExit = false;
			lTransition_39286.hasExitTime = false;
			lTransition_39286.hasFixedDuration = true;
			lTransition_39286.exitTime = 0f;
			lTransition_39286.duration = 0.1f;
			lTransition_39286.offset = 0f;
			lTransition_39286.mute = false;
			lTransition_39286.solo = false;
			lTransition_39286.canTransitionToSelf = true;
			lTransition_39286.orderedInterruption = true;
			lTransition_39286.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39286.conditions.Length - 1; i >= 0; i--) { lTransition_39286.RemoveCondition(lTransition_39286.conditions[i]); }
			lTransition_39286.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73516f, "L" + rLayerIndex + "MotionPhase");
			lTransition_39286.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39288 = MotionControllerMotion.EditorFindTransition(lState_32852, lState_32850, 0);
			if (lTransition_39288 == null) { lTransition_39288 = lState_32852.AddTransition(lState_32850); }
			lTransition_39288.isExit = false;
			lTransition_39288.hasExitTime = false;
			lTransition_39288.hasFixedDuration = true;
			lTransition_39288.exitTime = 0.3373314f;
			lTransition_39288.duration = 0.2f;
			lTransition_39288.offset = 0f;
			lTransition_39288.mute = false;
			lTransition_39288.solo = false;
			lTransition_39288.canTransitionToSelf = true;
			lTransition_39288.orderedInterruption = true;
			lTransition_39288.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39288.conditions.Length - 1; i >= 0; i--) { lTransition_39288.RemoveCondition(lTransition_39288.conditions[i]); }
			lTransition_39288.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73515f, "L" + rLayerIndex + "MotionPhase");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39290 = MotionControllerMotion.EditorFindTransition(lState_32852, lState_32854, 0);
			if (lTransition_39290 == null) { lTransition_39290 = lState_32852.AddTransition(lState_32854); }
			lTransition_39290.isExit = false;
			lTransition_39290.hasExitTime = false;
			lTransition_39290.hasFixedDuration = true;
			lTransition_39290.exitTime = 0.06250006f;
			lTransition_39290.duration = 0.1f;
			lTransition_39290.offset = 0f;
			lTransition_39290.mute = false;
			lTransition_39290.solo = false;
			lTransition_39290.canTransitionToSelf = true;
			lTransition_39290.orderedInterruption = true;
			lTransition_39290.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39290.conditions.Length - 1; i >= 0; i--) { lTransition_39290.RemoveCondition(lTransition_39290.conditions[i]); }
			lTransition_39290.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionPhase");
			lTransition_39290.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39292 = MotionControllerMotion.EditorFindTransition(lState_32854, lState_32852, 0);
			if (lTransition_39292 == null) { lTransition_39292 = lState_32854.AddTransition(lState_32852); }
			lTransition_39292.isExit = false;
			lTransition_39292.hasExitTime = false;
			lTransition_39292.hasFixedDuration = true;
			lTransition_39292.exitTime = 0.8809521f;
			lTransition_39292.duration = 0.1f;
			lTransition_39292.offset = 0.7522035f;
			lTransition_39292.mute = false;
			lTransition_39292.solo = false;
			lTransition_39292.canTransitionToSelf = true;
			lTransition_39292.orderedInterruption = true;
			lTransition_39292.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39292.conditions.Length - 1; i >= 0; i--) { lTransition_39292.RemoveCondition(lTransition_39292.conditions[i]); }
			lTransition_39292.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionPhase");
			lTransition_39292.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39294 = MotionControllerMotion.EditorFindTransition(lState_32854, lState_32850, 0);
			if (lTransition_39294 == null) { lTransition_39294 = lState_32854.AddTransition(lState_32850); }
			lTransition_39294.isExit = false;
			lTransition_39294.hasExitTime = false;
			lTransition_39294.hasFixedDuration = true;
			lTransition_39294.exitTime = 0f;
			lTransition_39294.duration = 0.2f;
			lTransition_39294.offset = 0f;
			lTransition_39294.mute = false;
			lTransition_39294.solo = false;
			lTransition_39294.canTransitionToSelf = true;
			lTransition_39294.orderedInterruption = true;
			lTransition_39294.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39294.conditions.Length - 1; i >= 0; i--) { lTransition_39294.RemoveCondition(lTransition_39294.conditions[i]); }
			lTransition_39294.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73515f, "L" + rLayerIndex + "MotionPhase");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39296 = MotionControllerMotion.EditorFindTransition(lState_32856, lState_32858, 0);
			if (lTransition_39296 == null) { lTransition_39296 = lState_32856.AddTransition(lState_32858); }
			lTransition_39296.isExit = false;
			lTransition_39296.hasExitTime = false;
			lTransition_39296.hasFixedDuration = true;
			lTransition_39296.exitTime = 0f;
			lTransition_39296.duration = 0.1f;
			lTransition_39296.offset = 0.07351331f;
			lTransition_39296.mute = false;
			lTransition_39296.solo = false;
			lTransition_39296.canTransitionToSelf = true;
			lTransition_39296.orderedInterruption = true;
			lTransition_39296.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39296.conditions.Length - 1; i >= 0; i--) { lTransition_39296.RemoveCondition(lTransition_39296.conditions[i]); }
			lTransition_39296.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73516f, "L" + rLayerIndex + "MotionPhase");
			lTransition_39296.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39298 = MotionControllerMotion.EditorFindTransition(lState_32856, lState_32860, 0);
			if (lTransition_39298 == null) { lTransition_39298 = lState_32856.AddTransition(lState_32860); }
			lTransition_39298.isExit = false;
			lTransition_39298.hasExitTime = false;
			lTransition_39298.hasFixedDuration = true;
			lTransition_39298.exitTime = 0f;
			lTransition_39298.duration = 0.1f;
			lTransition_39298.offset = 0f;
			lTransition_39298.mute = false;
			lTransition_39298.solo = false;
			lTransition_39298.canTransitionToSelf = true;
			lTransition_39298.orderedInterruption = true;
			lTransition_39298.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39298.conditions.Length - 1; i >= 0; i--) { lTransition_39298.RemoveCondition(lTransition_39298.conditions[i]); }
			lTransition_39298.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73516f, "L" + rLayerIndex + "MotionPhase");
			lTransition_39298.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39300 = MotionControllerMotion.EditorFindTransition(lState_32858, lState_32856, 0);
			if (lTransition_39300 == null) { lTransition_39300 = lState_32858.AddTransition(lState_32856); }
			lTransition_39300.isExit = false;
			lTransition_39300.hasExitTime = false;
			lTransition_39300.hasFixedDuration = true;
			lTransition_39300.exitTime = 0.5722023f;
			lTransition_39300.duration = 0.09999996f;
			lTransition_39300.offset = 82.20575f;
			lTransition_39300.mute = false;
			lTransition_39300.solo = false;
			lTransition_39300.canTransitionToSelf = true;
			lTransition_39300.orderedInterruption = true;
			lTransition_39300.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39300.conditions.Length - 1; i >= 0; i--) { lTransition_39300.RemoveCondition(lTransition_39300.conditions[i]); }
			lTransition_39300.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73515f, "L" + rLayerIndex + "MotionPhase");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39302 = MotionControllerMotion.EditorFindTransition(lState_32858, lState_32860, 0);
			if (lTransition_39302 == null) { lTransition_39302 = lState_32858.AddTransition(lState_32860); }
			lTransition_39302.isExit = false;
			lTransition_39302.hasExitTime = false;
			lTransition_39302.hasFixedDuration = true;
			lTransition_39302.exitTime = 0.611675f;
			lTransition_39302.duration = 0.0999999f;
			lTransition_39302.offset = 192.21f;
			lTransition_39302.mute = false;
			lTransition_39302.solo = false;
			lTransition_39302.canTransitionToSelf = true;
			lTransition_39302.orderedInterruption = true;
			lTransition_39302.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39302.conditions.Length - 1; i >= 0; i--) { lTransition_39302.RemoveCondition(lTransition_39302.conditions[i]); }
			lTransition_39302.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionPhase");
			lTransition_39302.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39306 = MotionControllerMotion.EditorFindTransition(lState_32860, lState_32858, 0);
			if (lTransition_39306 == null) { lTransition_39306 = lState_32860.AddTransition(lState_32858); }
			lTransition_39306.isExit = false;
			lTransition_39306.hasExitTime = false;
			lTransition_39306.hasFixedDuration = true;
			lTransition_39306.exitTime = 0.8809586f;
			lTransition_39306.duration = 0.1f;
			lTransition_39306.offset = 0.2146628f;
			lTransition_39306.mute = false;
			lTransition_39306.solo = false;
			lTransition_39306.canTransitionToSelf = true;
			lTransition_39306.orderedInterruption = true;
			lTransition_39306.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39306.conditions.Length - 1; i >= 0; i--) { lTransition_39306.RemoveCondition(lTransition_39306.conditions[i]); }
			lTransition_39306.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionPhase");
			lTransition_39306.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39308 = MotionControllerMotion.EditorFindTransition(lState_32860, lState_32856, 0);
			if (lTransition_39308 == null) { lTransition_39308 = lState_32860.AddTransition(lState_32856); }
			lTransition_39308.isExit = false;
			lTransition_39308.hasExitTime = false;
			lTransition_39308.hasFixedDuration = true;
			lTransition_39308.exitTime = 0f;
			lTransition_39308.duration = 0.2f;
			lTransition_39308.offset = 0f;
			lTransition_39308.mute = false;
			lTransition_39308.solo = false;
			lTransition_39308.canTransitionToSelf = true;
			lTransition_39308.orderedInterruption = true;
			lTransition_39308.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39308.conditions.Length - 1; i >= 0; i--) { lTransition_39308.RemoveCondition(lTransition_39308.conditions[i]); }
			lTransition_39308.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73515f, "L" + rLayerIndex + "MotionPhase");


			// Run any post processing after creating the state machine
			OnStateMachineCreated();
		}

#endif

		// ************************************ END AUTO GENERATED ************************************
		#endregion


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

			UnityEditor.Animations.AnimatorStateMachine lSSM_31650 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "RifleAP_BasicShooterAttack-SM");
			if (lSSM_31650 == null) { lSSM_31650 = lLayerStateMachine.AddStateMachine("RifleAP_BasicShooterAttack-SM", new Vector3(240, -910, 0)); }

			UnityEditor.Animations.AnimatorState lState_32850 = MotionControllerMotion.EditorFindState(lSSM_31650, "Rifle Idle Pose Exit");
			if (lState_32850 == null) { lState_32850 = lSSM_31650.AddState("Rifle Idle Pose Exit", new Vector3(564, 0, 0)); }
			lState_32850.speed = 1f;
			lState_32850.mirror = false;
			lState_32850.tag = "Exit";
			lState_32850.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_Idle_Pose");

			UnityEditor.Animations.AnimatorState lState_32852 = MotionControllerMotion.EditorFindState(lSSM_31650, "Rifle ShootOnce");
			if (lState_32852 == null) { lState_32852 = lSSM_31650.AddState("Rifle ShootOnce", new Vector3(310, -30, 0)); }
			lState_32852.speed = 1f;
			lState_32852.mirror = false;
			lState_32852.tag = "";
			lState_32852.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_ShootOnce");

			UnityEditor.Animations.AnimatorState lState_32854 = MotionControllerMotion.EditorFindState(lSSM_31650, "Rifle Idle Pose");
			if (lState_32854 == null) { lState_32854 = lSSM_31650.AddState("Rifle Idle Pose", new Vector3(310, 40, 0)); }
			lState_32854.speed = 1f;
			lState_32854.mirror = false;
			lState_32854.tag = "";
			lState_32854.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_Idle_Pose");

			UnityEditor.Animations.AnimatorState lState_32856 = MotionControllerMotion.EditorFindState(lSSM_31650, "Pistol Idle Pose Exit");
			if (lState_32856 == null) { lState_32856 = lSSM_31650.AddState("Pistol Idle Pose Exit", new Vector3(560, 160, 0)); }
			lState_32856.speed = 1f;
			lState_32856.mirror = false;
			lState_32856.tag = "Exit";
			lState_32856.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/PistolAnimsetPro/Animations/PistolAnimsetPro.fbx", "Pistol_Idle_Pose");

			UnityEditor.Animations.AnimatorState lState_32858 = MotionControllerMotion.EditorFindState(lSSM_31650, "Pistol ShootOnce");
			if (lState_32858 == null) { lState_32858 = lSSM_31650.AddState("Pistol ShootOnce", new Vector3(312, 132, 0)); }
			lState_32858.speed = 1f;
			lState_32858.mirror = false;
			lState_32858.tag = "";
			lState_32858.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/PistolAnimsetPro/Animations/PistolAnimsetPro.fbx", "Pistol_ShootOnce");

			UnityEditor.Animations.AnimatorState lState_32860 = MotionControllerMotion.EditorFindState(lSSM_31650, "Pistol Idle Pose");
			if (lState_32860 == null) { lState_32860 = lSSM_31650.AddState("Pistol Idle Pose", new Vector3(312, 204, 0)); }
			lState_32860.speed = 1f;
			lState_32860.mirror = false;
			lState_32860.tag = "";
			lState_32860.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/PistolAnimsetPro/Animations/PistolAnimsetPro.fbx", "Pistol_Idle_Pose");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33194 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_32852, 0);
			if (lAnyTransition_33194 == null) { lAnyTransition_33194 = lLayerStateMachine.AddAnyStateTransition(lState_32852); }
			lAnyTransition_33194.isExit = false;
			lAnyTransition_33194.hasExitTime = false;
			lAnyTransition_33194.hasFixedDuration = true;
			lAnyTransition_33194.exitTime = 0.75f;
			lAnyTransition_33194.duration = 0.1f;
			lAnyTransition_33194.offset = 0.7810742f;
			lAnyTransition_33194.mute = false;
			lAnyTransition_33194.solo = false;
			lAnyTransition_33194.canTransitionToSelf = true;
			lAnyTransition_33194.orderedInterruption = true;
			lAnyTransition_33194.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_33194.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33194.RemoveCondition(lAnyTransition_33194.conditions[i]); }
			lAnyTransition_33194.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73500f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33194.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 500f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_33194.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33196 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_32854, 0);
			if (lAnyTransition_33196 == null) { lAnyTransition_33196 = lLayerStateMachine.AddAnyStateTransition(lState_32854); }
			lAnyTransition_33196.isExit = false;
			lAnyTransition_33196.hasExitTime = false;
			lAnyTransition_33196.hasFixedDuration = true;
			lAnyTransition_33196.exitTime = 0.75f;
			lAnyTransition_33196.duration = 0.15f;
			lAnyTransition_33196.offset = 0f;
			lAnyTransition_33196.mute = false;
			lAnyTransition_33196.solo = false;
			lAnyTransition_33196.canTransitionToSelf = true;
			lAnyTransition_33196.orderedInterruption = true;
			lAnyTransition_33196.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_33196.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33196.RemoveCondition(lAnyTransition_33196.conditions[i]); }
			lAnyTransition_33196.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73500f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33196.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 500f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_33196.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33198 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_32858, 0);
			if (lAnyTransition_33198 == null) { lAnyTransition_33198 = lLayerStateMachine.AddAnyStateTransition(lState_32858); }
			lAnyTransition_33198.isExit = false;
			lAnyTransition_33198.hasExitTime = false;
			lAnyTransition_33198.hasFixedDuration = true;
			lAnyTransition_33198.exitTime = 0.75f;
			lAnyTransition_33198.duration = 0.1f;
			lAnyTransition_33198.offset = 0.2146628f;
			lAnyTransition_33198.mute = false;
			lAnyTransition_33198.solo = false;
			lAnyTransition_33198.canTransitionToSelf = true;
			lAnyTransition_33198.orderedInterruption = true;
			lAnyTransition_33198.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_33198.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33198.RemoveCondition(lAnyTransition_33198.conditions[i]); }
			lAnyTransition_33198.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73500f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33198.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 550f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_33198.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33200 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_32860, 0);
			if (lAnyTransition_33200 == null) { lAnyTransition_33200 = lLayerStateMachine.AddAnyStateTransition(lState_32860); }
			lAnyTransition_33200.isExit = false;
			lAnyTransition_33200.hasExitTime = false;
			lAnyTransition_33200.hasFixedDuration = true;
			lAnyTransition_33200.exitTime = 0.75f;
			lAnyTransition_33200.duration = 0.15f;
			lAnyTransition_33200.offset = 0f;
			lAnyTransition_33200.mute = false;
			lAnyTransition_33200.solo = false;
			lAnyTransition_33200.canTransitionToSelf = true;
			lAnyTransition_33200.orderedInterruption = true;
			lAnyTransition_33200.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_33200.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33200.RemoveCondition(lAnyTransition_33200.conditions[i]); }
			lAnyTransition_33200.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73500f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33200.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 550f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_33200.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39284 = MotionControllerMotion.EditorFindTransition(lState_32850, lState_32852, 0);
			if (lTransition_39284 == null) { lTransition_39284 = lState_32850.AddTransition(lState_32852); }
			lTransition_39284.isExit = false;
			lTransition_39284.hasExitTime = false;
			lTransition_39284.hasFixedDuration = true;
			lTransition_39284.exitTime = 0f;
			lTransition_39284.duration = 0.1f;
			lTransition_39284.offset = 0f;
			lTransition_39284.mute = false;
			lTransition_39284.solo = false;
			lTransition_39284.canTransitionToSelf = true;
			lTransition_39284.orderedInterruption = true;
			lTransition_39284.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39284.conditions.Length - 1; i >= 0; i--) { lTransition_39284.RemoveCondition(lTransition_39284.conditions[i]); }
			lTransition_39284.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73516f, "L" + rLayerIndex + "MotionPhase");
			lTransition_39284.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39286 = MotionControllerMotion.EditorFindTransition(lState_32850, lState_32854, 0);
			if (lTransition_39286 == null) { lTransition_39286 = lState_32850.AddTransition(lState_32854); }
			lTransition_39286.isExit = false;
			lTransition_39286.hasExitTime = false;
			lTransition_39286.hasFixedDuration = true;
			lTransition_39286.exitTime = 0f;
			lTransition_39286.duration = 0.1f;
			lTransition_39286.offset = 0f;
			lTransition_39286.mute = false;
			lTransition_39286.solo = false;
			lTransition_39286.canTransitionToSelf = true;
			lTransition_39286.orderedInterruption = true;
			lTransition_39286.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39286.conditions.Length - 1; i >= 0; i--) { lTransition_39286.RemoveCondition(lTransition_39286.conditions[i]); }
			lTransition_39286.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73516f, "L" + rLayerIndex + "MotionPhase");
			lTransition_39286.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39288 = MotionControllerMotion.EditorFindTransition(lState_32852, lState_32850, 0);
			if (lTransition_39288 == null) { lTransition_39288 = lState_32852.AddTransition(lState_32850); }
			lTransition_39288.isExit = false;
			lTransition_39288.hasExitTime = false;
			lTransition_39288.hasFixedDuration = true;
			lTransition_39288.exitTime = 0.3373314f;
			lTransition_39288.duration = 0.2f;
			lTransition_39288.offset = 0f;
			lTransition_39288.mute = false;
			lTransition_39288.solo = false;
			lTransition_39288.canTransitionToSelf = true;
			lTransition_39288.orderedInterruption = true;
			lTransition_39288.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39288.conditions.Length - 1; i >= 0; i--) { lTransition_39288.RemoveCondition(lTransition_39288.conditions[i]); }
			lTransition_39288.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73515f, "L" + rLayerIndex + "MotionPhase");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39290 = MotionControllerMotion.EditorFindTransition(lState_32852, lState_32854, 0);
			if (lTransition_39290 == null) { lTransition_39290 = lState_32852.AddTransition(lState_32854); }
			lTransition_39290.isExit = false;
			lTransition_39290.hasExitTime = false;
			lTransition_39290.hasFixedDuration = true;
			lTransition_39290.exitTime = 0.06250006f;
			lTransition_39290.duration = 0.1f;
			lTransition_39290.offset = 0f;
			lTransition_39290.mute = false;
			lTransition_39290.solo = false;
			lTransition_39290.canTransitionToSelf = true;
			lTransition_39290.orderedInterruption = true;
			lTransition_39290.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39290.conditions.Length - 1; i >= 0; i--) { lTransition_39290.RemoveCondition(lTransition_39290.conditions[i]); }
			lTransition_39290.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionPhase");
			lTransition_39290.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39292 = MotionControllerMotion.EditorFindTransition(lState_32854, lState_32852, 0);
			if (lTransition_39292 == null) { lTransition_39292 = lState_32854.AddTransition(lState_32852); }
			lTransition_39292.isExit = false;
			lTransition_39292.hasExitTime = false;
			lTransition_39292.hasFixedDuration = true;
			lTransition_39292.exitTime = 0.8809521f;
			lTransition_39292.duration = 0.1f;
			lTransition_39292.offset = 0.7522035f;
			lTransition_39292.mute = false;
			lTransition_39292.solo = false;
			lTransition_39292.canTransitionToSelf = true;
			lTransition_39292.orderedInterruption = true;
			lTransition_39292.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39292.conditions.Length - 1; i >= 0; i--) { lTransition_39292.RemoveCondition(lTransition_39292.conditions[i]); }
			lTransition_39292.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionPhase");
			lTransition_39292.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39294 = MotionControllerMotion.EditorFindTransition(lState_32854, lState_32850, 0);
			if (lTransition_39294 == null) { lTransition_39294 = lState_32854.AddTransition(lState_32850); }
			lTransition_39294.isExit = false;
			lTransition_39294.hasExitTime = false;
			lTransition_39294.hasFixedDuration = true;
			lTransition_39294.exitTime = 0f;
			lTransition_39294.duration = 0.2f;
			lTransition_39294.offset = 0f;
			lTransition_39294.mute = false;
			lTransition_39294.solo = false;
			lTransition_39294.canTransitionToSelf = true;
			lTransition_39294.orderedInterruption = true;
			lTransition_39294.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39294.conditions.Length - 1; i >= 0; i--) { lTransition_39294.RemoveCondition(lTransition_39294.conditions[i]); }
			lTransition_39294.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73515f, "L" + rLayerIndex + "MotionPhase");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39296 = MotionControllerMotion.EditorFindTransition(lState_32856, lState_32858, 0);
			if (lTransition_39296 == null) { lTransition_39296 = lState_32856.AddTransition(lState_32858); }
			lTransition_39296.isExit = false;
			lTransition_39296.hasExitTime = false;
			lTransition_39296.hasFixedDuration = true;
			lTransition_39296.exitTime = 0f;
			lTransition_39296.duration = 0.1f;
			lTransition_39296.offset = 0.07351331f;
			lTransition_39296.mute = false;
			lTransition_39296.solo = false;
			lTransition_39296.canTransitionToSelf = true;
			lTransition_39296.orderedInterruption = true;
			lTransition_39296.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39296.conditions.Length - 1; i >= 0; i--) { lTransition_39296.RemoveCondition(lTransition_39296.conditions[i]); }
			lTransition_39296.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73516f, "L" + rLayerIndex + "MotionPhase");
			lTransition_39296.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39298 = MotionControllerMotion.EditorFindTransition(lState_32856, lState_32860, 0);
			if (lTransition_39298 == null) { lTransition_39298 = lState_32856.AddTransition(lState_32860); }
			lTransition_39298.isExit = false;
			lTransition_39298.hasExitTime = false;
			lTransition_39298.hasFixedDuration = true;
			lTransition_39298.exitTime = 0f;
			lTransition_39298.duration = 0.1f;
			lTransition_39298.offset = 0f;
			lTransition_39298.mute = false;
			lTransition_39298.solo = false;
			lTransition_39298.canTransitionToSelf = true;
			lTransition_39298.orderedInterruption = true;
			lTransition_39298.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39298.conditions.Length - 1; i >= 0; i--) { lTransition_39298.RemoveCondition(lTransition_39298.conditions[i]); }
			lTransition_39298.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73516f, "L" + rLayerIndex + "MotionPhase");
			lTransition_39298.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39300 = MotionControllerMotion.EditorFindTransition(lState_32858, lState_32856, 0);
			if (lTransition_39300 == null) { lTransition_39300 = lState_32858.AddTransition(lState_32856); }
			lTransition_39300.isExit = false;
			lTransition_39300.hasExitTime = false;
			lTransition_39300.hasFixedDuration = true;
			lTransition_39300.exitTime = 0.5722023f;
			lTransition_39300.duration = 0.09999996f;
			lTransition_39300.offset = 82.20575f;
			lTransition_39300.mute = false;
			lTransition_39300.solo = false;
			lTransition_39300.canTransitionToSelf = true;
			lTransition_39300.orderedInterruption = true;
			lTransition_39300.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39300.conditions.Length - 1; i >= 0; i--) { lTransition_39300.RemoveCondition(lTransition_39300.conditions[i]); }
			lTransition_39300.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73515f, "L" + rLayerIndex + "MotionPhase");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39302 = MotionControllerMotion.EditorFindTransition(lState_32858, lState_32860, 0);
			if (lTransition_39302 == null) { lTransition_39302 = lState_32858.AddTransition(lState_32860); }
			lTransition_39302.isExit = false;
			lTransition_39302.hasExitTime = false;
			lTransition_39302.hasFixedDuration = true;
			lTransition_39302.exitTime = 0.611675f;
			lTransition_39302.duration = 0.0999999f;
			lTransition_39302.offset = 192.21f;
			lTransition_39302.mute = false;
			lTransition_39302.solo = false;
			lTransition_39302.canTransitionToSelf = true;
			lTransition_39302.orderedInterruption = true;
			lTransition_39302.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39302.conditions.Length - 1; i >= 0; i--) { lTransition_39302.RemoveCondition(lTransition_39302.conditions[i]); }
			lTransition_39302.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionPhase");
			lTransition_39302.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39306 = MotionControllerMotion.EditorFindTransition(lState_32860, lState_32858, 0);
			if (lTransition_39306 == null) { lTransition_39306 = lState_32860.AddTransition(lState_32858); }
			lTransition_39306.isExit = false;
			lTransition_39306.hasExitTime = false;
			lTransition_39306.hasFixedDuration = true;
			lTransition_39306.exitTime = 0.8809586f;
			lTransition_39306.duration = 0.1f;
			lTransition_39306.offset = 0.2146628f;
			lTransition_39306.mute = false;
			lTransition_39306.solo = false;
			lTransition_39306.canTransitionToSelf = true;
			lTransition_39306.orderedInterruption = true;
			lTransition_39306.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39306.conditions.Length - 1; i >= 0; i--) { lTransition_39306.RemoveCondition(lTransition_39306.conditions[i]); }
			lTransition_39306.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionPhase");
			lTransition_39306.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39308 = MotionControllerMotion.EditorFindTransition(lState_32860, lState_32856, 0);
			if (lTransition_39308 == null) { lTransition_39308 = lState_32860.AddTransition(lState_32856); }
			lTransition_39308.isExit = false;
			lTransition_39308.hasExitTime = false;
			lTransition_39308.hasFixedDuration = true;
			lTransition_39308.exitTime = 0f;
			lTransition_39308.duration = 0.2f;
			lTransition_39308.offset = 0f;
			lTransition_39308.mute = false;
			lTransition_39308.solo = false;
			lTransition_39308.canTransitionToSelf = true;
			lTransition_39308.orderedInterruption = true;
			lTransition_39308.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39308.conditions.Length - 1; i >= 0; i--) { lTransition_39308.RemoveCondition(lTransition_39308.conditions[i]); }
			lTransition_39308.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73515f, "L" + rLayerIndex + "MotionPhase");

		}


	}
}
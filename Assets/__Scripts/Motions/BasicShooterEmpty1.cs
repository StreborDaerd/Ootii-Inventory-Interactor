using System.Collections;
using UnityEngine;
using com.ootii.Actors.Inventory;
using com.ootii.Cameras;
using com.ootii.Geometry;
using com.ootii.Helpers;
using com.ootii.Actors.AnimationControllers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WildWalrus.Actors.AnimationControllers
{
	[MotionName("Basic Shooter Empty 1")]
	[MotionDescription("Test. Used on additional layers as the 'empty' motion to clear out any animation so that the base layer can be full-body. This version includes IK support for looking.")]
	public class BasicShooterEmpty1 : BasicShooterMotion
	{
		/// <summary>
		/// Trigger values for the motion
		/// </summary>
		public int PHASE_UNKNOWN = 0;
		public int PHASE_START = 3010;

		/// <summary>
		/// Optional "Form" or "Style" required for this motion to activate
		/// </summary>
		public string _RequiredForms = "500,550";
		public string RequiredForms
		{
			get { return _RequiredForms; }

			set
			{
				_RequiredForms = value;

				if (Application.isPlaying)
				{
					mRequiredForms = null;

					if (_RequiredForms.Length > 0)
					{
						string[] lRequiredForms = _RequiredForms.Split(',');
						mRequiredForms = new int[lRequiredForms.Length];

						for (int i = 0; i < lRequiredForms.Length; i++)
						{
							mRequiredForms[i] = -1;
							int.TryParse(lRequiredForms[i], out mRequiredForms[i]);
						}
					}
				}
			}
		}

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
		/// ID of the slot that holds the weapon
		/// </summary>
		public string _AltWeaponSlotID = "LEFT_HAND";
		public string AltWeaponSlotID
		{
			get { return _AltWeaponSlotID; }
			set { _AltWeaponSlotID = value; }
		}

		// Strings representing the required forms
		protected int[] mRequiredForms = null;

		// Determines if we use look IK locally
		protected bool mIsLookIKEnabledLocally = true;

		/// <summary>
		/// Default constructor
		/// </summary>
		public BasicShooterEmpty1()
			 : base()
		{
			_Category = EnumMotionCategories.IDLE;

			_Priority = 1;
			_Form = 0;

			mLookIKMaxHorizontalAngle = 45f;

#if UNITY_EDITOR
			if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Empty-SM"; }
#endif
		}

		/// <summary>
		/// Controller constructor
		/// </summary>
		/// <param name="rController">Controller the motion belongs to</param>
		public BasicShooterEmpty1(MotionController rController)
			 : base(rController)
		{
			_Category = EnumMotionCategories.IDLE;

			_Priority = 1;
			_Form = 0;

			mLookIKMaxHorizontalAngle = 45f;

#if UNITY_EDITOR
			if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Empty-SM"; }
#endif
		}

		/// <summary>
		/// Awake is called after all objects are initialized so you can safely speak to other objects. This is where
		/// reference can be associated.
		/// </summary>
		public override void Awake()
		{
			base.Awake();
			RequiredForms = _RequiredForms;

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
			// Handle the disqualifiers
			if (!mIsStartable) { return false; }
			if (!mMotionController.IsGrounded) { return false; }

			// This is a catch all. If there are no motions found to match
			// the controller's state, we default to this motion.
			if (mMotionLayer.ActiveMotion != null && !(mMotionLayer.ActiveMotion is Empty)) { return false; }

			// Check if we're in the required form
			bool lFormValidated = (mRequiredForms == null);

			// Check if we're in the required form
			if (!lFormValidated)
			{
				int lForm = mMotionController.CurrentForm;
				for (int i = 0; i < mRequiredForms.Length; i++)
				{
					if (mRequiredForms[i] == lForm)
					{
						lFormValidated = true;
						break;
					}
				}
			}

			if (!lFormValidated) { return false; }

			// If we get here, we're good
			return true;
		}

		/// <summary>
		/// Tests if the motion should continue. If it shouldn't, the motion
		/// is typically disabled
		/// </summary>
		/// <returns></returns>
		public override bool TestUpdate()
		{
			if (mIsAnimatorActive && !IsInMotionState)
			{
				return false;
			}

			// Check if we're in the required form
			bool lFormValidated = (mRequiredForms == null);

			// Check if we're in the required form
			if (!lFormValidated)
			{
				int lForm = mMotionController.CurrentForm;
				for (int i = 0; i < mRequiredForms.Length; i++)
				{
					if (mRequiredForms[i] == lForm)
					{
						lFormValidated = true;
						break;
					}
				}
			}

			if (!lFormValidated) { return false; }

			// Stay in
			return true;
		}

		/// <summary>
		/// Raised when a motion is being interrupted by another motion
		/// </summary>
		/// <param name="rMotion">Motion doing the interruption</param>
		/// <returns>Boolean determining if it can be interrupted</returns>
		public override bool TestInterruption(MotionControllerMotion rMotion)
		{
			if (mLookIKWeight > 0f && !(rMotion is BasicShooterAttack))
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
			// First, see if we can find a weapon in hand
			mGunCore = FindWeapon(WeaponSlotID);
			if (mGunCore == null) { mGunCore = FindWeapon(AltWeaponSlotID); }

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
			if (rPrevMotion is BasicShooterAttack)
			{
				mLookIKWeight = ((BasicShooterAttack)rPrevMotion).LookIKWeight;
			}

			// Register this motion with the camera
			if (mMotionController.CameraRig is BaseCameraRig)
			{
				((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
				((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate += OnCameraUpdated;
			}

			// Move to the empty state
			mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, _Form, mParameter, true);

			// Finalize the activation
			return base.Activate(rPrevMotion);
		}

		/// <summary>
		/// Raised when we shut the motion down
		/// </summary>
		public override void Deactivate()
		{
			// Clear the gun so we can grab a new one later
			if (mGunCore != null)
			{
				mGunCore = null;
				mGunSupport = null;
			}

			// Unregister this motion with the camera
			if (mMotionController.CameraRig is BaseCameraRig)
			{
				((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
			}

			// Continue with the deactivation
			base.Deactivate();
		}

		/// <summary>
		/// Allows the motion to modify the velocity before it is applied.
		/// </summary>
		/// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
		/// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
		/// <param name="rMovement">Amount of movement caused by root motion this frame</param>
		/// <param name="rRotation">Amount of rotation caused by root motion this frame</param>
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
			mIsLookIKEnabledLocally = _IsLookIKEnabled;

			// If spine IK isn't meant to be active, shut it down
			if (mIsLookIKEnabledLocally && _LookIKMotionTag.Length > 0)
			{
				MotionControllerLayer lBaseLayer = mMotionController.MotionLayers[0];
				MotionControllerMotion lBaseMotion = lBaseLayer.ActiveMotion;

				if (lBaseMotion == null)
				{
					mLookIKWeight = 0f;
					mIsLookIKEnabledLocally = false;
				}
				else
				{
					// We don't want to enable IK if the tag doesn't exist
					if (!lBaseMotion.TagExists(_LookIKMotionTag))
					{
						mLookIKWeight = 0f;
						mIsLookIKEnabledLocally = false;
					}
					else
					{
						// We don't want to re-enable the IK if we're in a transition
						if (mLookIKWeight < 0.1f && (!lBaseMotion.IsAnimatorActive || lBaseLayer._AnimatorTransitionID != 0f))
						{
							mLookIKWeight = 0f;
							mIsLookIKEnabledLocally = false;
						}
					}
				}
			}

			// Only run IK if we're meant to
			if (_IsLookIKEnabled)
			{
				// Ease in spine IK
				if (mGunCore != null)
				{
					mLookIKRotation = mGunCore.transform.rotation;
					mLookIKRotation = mMotionController._Transform.rotation.RotationTo(mLookIKRotation);

					if (mIsLookIKEnabledLocally && mLookIKWeight < 1f && mLookIKEasingFunction == null)
					{
						EaseInIK(_LookIKInSpeed);
					}
				}

				// Run spine IK
				if (mGunCore != null && !(mMotionController.CameraRig is BaseCameraRig))
				{
					if (mLookIKWeight > 0f && mLookIKRotation == Quaternion.identity && mMotionController._CameraTransform != null)
					{
						if (!(mMotionController.ActiveMotion is ICoverMotion))
						{
							Quaternion lTargetRotation = mMotionController._Transform.rotation;
							if (mMotionController._CameraTransform != null) { lTargetRotation = mMotionController._CameraTransform.rotation; }

							mLookIKRotation = mGunCore.transform.rotation;
							mLookIKRotation = mMotionController._Transform.rotation.RotationTo(mLookIKRotation);

							RotateSpineToDirection(mLookIKRotation, lTargetRotation, mLookIKWeight);
							if (mGunSupport != null) { RotateArmToSupport(mGunSupport.position, mLookIKWeight); }
						}
					}
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
			if (!mIsLookIKEnabledLocally) { return false; }
			if (mLookIKWeight == 0f) { return false; }
			if (QuaternionExt.IsIdentity(mLookIKRotation)) { return false; }
			if (mLookIKEasingFunction == null) { return false; }
			if (mLookIKWeight > 0f && mMotionLayer.ActiveMotion is BasicShooterAttack) { return false; }
			if (mMotionController._CameraTransform == null) { return false; }

			Quaternion lTargetRotation = mMotionController._Transform.rotation;
			if (mMotionController._CameraTransform != null) { lTargetRotation = mMotionController._CameraTransform.rotation; }

			RotateSpineToDirection(mLookIKRotation, lTargetRotation, mLookIKWeight);
			if (mGunSupport != null) { RotateArmToSupport(mGunSupport.position, mLookIKWeight); }

			return true;
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
			// If we're aiming, use IK
			if (mIsLookIKEnabledLocally)
			{
				if (mLookIKWeight > 0f && mLookIKRotation != Quaternion.identity && mMotionController._CameraTransform != null)
				{
					if (!(mMotionController.ActiveMotion is ICoverMotion))
					{
						mLookIKRotation = mGunCore.transform.rotation;
						mLookIKRotation = mMotionController._Transform.rotation.RotationTo(mLookIKRotation);

						RotateSpineToDirection(mLookIKRotation, mMotionController._CameraTransform.rotation, mLookIKWeight);
						if (mGunSupport != null) { RotateArmToSupport(mGunSupport.position, mLookIKWeight); }
					}
				}
			}
		}

		/// <summary>
		/// Eases in the IK for aiming over the specified time
		/// </summary>
		/// <param name="rTime">Time to ease into aiming</param>
		/// <param name="rSmooth">Determines if we smooth out the easing</param>
		/// <returns></returns>
		//protected override IEnumerator EaseOutIKInternal(float rTime, bool rSmooth = true)
		//{
		//    float lStartTime = Time.time - (rTime * (1f - mLookIKWeight));

		//    while (_IsLookIKEnabled && mLookIKWeight > 0f)
		//    {
		//        mLookIKWeight = 1f - Mathf.Clamp01((Time.time - lStartTime) / rTime);
		//        if (rSmooth) { mLookIKWeight = NumberHelper.EaseInOutCubic(mLookIKWeight); }

		//        yield return null;
		//    }

		//    // Clear out IK support
		//    mLookIKRotation = Quaternion.identity;
		//    mLookIKEasingFunction = null;
		//}

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

			if (EditorHelper.TextField("Required Forms", "Comma delimited list of forms that this motion will activate for.", RequiredForms, mMotionController))
			{
				lIsDirty = true;
				RequiredForms = EditorHelper.FieldStringValue;
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
				if (EditorHelper.TextField("IK Tag", "Tag required by the first motion layer in order for us to enable spine IK", LookIKMotionTag, mMotionController))
				{
					lIsDirty = true;
					LookIKMotionTag = EditorHelper.FieldStringValue;
				}

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
	}
}
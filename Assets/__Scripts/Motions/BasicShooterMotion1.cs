using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Combat;
using com.ootii.Actors.Inventory;
using com.ootii.Actors.LifeCores;
using com.ootii.Geometry;
using com.ootii.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WildWalrus.Items;

namespace WildWalrus.Actors.AnimationControllers
{
	public abstract class BasicShooterMotion1 : MotionControllerMotion
	{
		#region Properties

		public GameObject _InventorySourceOwner = null;
		public GameObject InventorySourceOwner
		{
			get { return _InventorySourceOwner; }
			set { _InventorySourceOwner = value; }
		}
		

		[NonSerialized]
		protected IInventorySource mInventorySource = null;
		public IInventorySource InventorySource
		{
			get { return mInventorySource; }
			set { mInventorySource = value; }
		}
		
		
		public bool _RotateWithInput = false;
		public bool RotateWithInput
		{
			get { return _RotateWithInput; }
			set { _RotateWithInput = value; }
		}
		
		
		public bool _RotateWithCamera = true;
		public bool RotateWithCamera
		{
			get { return _RotateWithCamera; }
			set { _RotateWithCamera = value; }
		}
		
		
		public float _RotationSpeed = 270f;
		public float RotationSpeed
		{
			get { return _RotationSpeed; }
			set { _RotationSpeed = value; }
		}
		
		
		public bool _IsLookIKEnabled = true;
		public bool IsLookIKEnabled
		{
			get { return _IsLookIKEnabled; }
			set { _IsLookIKEnabled = value; }
		}
		
		
		public string _LookIKMotionTag = "AimSpineIK";
		public string LookIKMotionTag
		{
			get { return _LookIKMotionTag; }
			set { _LookIKMotionTag = value; }
		}
		
		
		public float _LookIKInSpeed = 0.2f;
		public float LookIKInSpeed
		{
			get { return _LookIKInSpeed; }
			set { _LookIKInSpeed = value; }
		}
		
		
		public float _LookIKOutSpeed = 0.2f;
		public float LookIKOutSpeed
		{
			get { return _LookIKOutSpeed; }
			set { _LookIKOutSpeed = value; }
		}
		
		
		protected float mLookIKWeight = 0f;
		public float LookIKWeight
		{
			get { return mLookIKWeight; }
			set { mLookIKWeight = value; }
		}
		
		
		public float _LookIKHorizontalAngle = 0f;
		public float LookIKHorizontalAngle
		{
			get { return _LookIKHorizontalAngle; }
			set { _LookIKHorizontalAngle = value; }
		}
		
		
		public float _LookIKVerticalAngle = 0f;
		public float LookIKVerticalAngle
		{
			get { return _LookIKVerticalAngle; }
			set { _LookIKVerticalAngle = value; }
		}
		
		
		public float _LookIKTwistAngle = 0f;
		public float LookIKTwistAngle
		{
			get { return _LookIKTwistAngle; }
			set { _LookIKTwistAngle = value; }
		}
		
		
		public float _FastLookIKHorizontalAngle = 0f;
		public float FastLookIKHorizontalAngle
		{
			get { return _FastLookIKHorizontalAngle; }
			set { _FastLookIKHorizontalAngle = value; }
		}
		
		
		public float _FastLookIKVerticalAngle = 0f;
		public float FastLookIKVerticalAngle
		{
			get { return _FastLookIKVerticalAngle; }
			set { _FastLookIKVerticalAngle = value; }
		}
		
		
		public bool _ForceHorizontalAimForward = true;
		public bool ForceHorizontalAimForward
		{
			get { return _ForceHorizontalAimForward; }
			set { _ForceHorizontalAimForward = value; }
		}
		
		
		public float _FastHorizontalAimAngle = 0f;
		public float FastHorizontalAimAngle
		{
			get { return _FastHorizontalAimAngle; }
			set { _FastHorizontalAimAngle = value; }
		}
		
		
		public float _FastVerticalAimAngle = 0f;
		public float FastVerticalAimAngle
		{
			get { return _FastVerticalAimAngle; }
			set { _FastVerticalAimAngle = value; }
		}
		
		
		public bool _IsSupportIKEnabled = true;
		public bool IsSupportIKEnabled
		{
			get { return _IsSupportIKEnabled; }
			set { _IsSupportIKEnabled = value; }
		}
		
		
		public override bool VerifyTransition
		{
			get { return false; }
		}
		
		#endregion Properties


		#region Members

		protected MCGunCore mGunCore = null;
		
		protected Transform mGunSupport = null;
		
		protected Quaternion mLookIKRotation = Quaternion.identity;
		
		protected IEnumerator mLookIKEasingFunction = null;
		
		protected float mLookIKMaxHorizontalAngle = 0f;

		#endregion Members


		#region Constructors
		
		public BasicShooterMotion1() : base()
		{
			_Pack = ShooterPackDefinition.PackName;
			_Category = EnumMotionCategories.COMBAT_SHOOTING;
		}
		
		
		public BasicShooterMotion1(MotionController rController) : base(rController)
		{
			_Pack = ShooterPackDefinition.PackName;
			_Category = EnumMotionCategories.COMBAT_SHOOTING;
		}

		#endregion Constructors


		#region IKFunctions

		#region Easing
		
		protected virtual void EaseInIK(float rTime, bool rSmooth = true)
		{
			if (mLookIKEasingFunction != null) { mMotionController.StopCoroutine(mLookIKEasingFunction); }
			
			mLookIKEasingFunction = EaseInIKInternal(rTime, rSmooth);
			mMotionController.StartCoroutine(mLookIKEasingFunction);
		}
		
		
		protected virtual IEnumerator EaseInIKInternal(float rTime, bool rSmooth = true)
		{
			float lStartTime = Time.time - (rTime * mLookIKWeight);
			
			while (_IsLookIKEnabled && mLookIKWeight < 1f)
			{
				mLookIKWeight = Mathf.Clamp01((Time.time - lStartTime) / rTime);
				if (rSmooth) { mLookIKWeight = NumberHelper.EaseInOutCubic(mLookIKWeight); }
				
				yield return null;
			}
			
			mLookIKEasingFunction = null;
		}
		
		
		protected virtual void EaseOutIK(float rTime, bool rSmooth = true)
		{
			if (mLookIKEasingFunction != null) { mMotionController.StopCoroutine(mLookIKEasingFunction); }
			
			mLookIKEasingFunction = EaseOutIKInternal(rTime, rSmooth);
			mMotionController.StartCoroutine(mLookIKEasingFunction);
		}
		
		
		protected virtual IEnumerator EaseOutIKInternal(float rTime, bool rSmooth = true)
		{
			float lStartTime = Time.time - (rTime * (1f - mLookIKWeight));
			
			while (_IsLookIKEnabled && mLookIKWeight > 0f)
			{
				mLookIKWeight = 1f - Mathf.Clamp01((Time.time - lStartTime) / rTime);
				if (rSmooth) { mLookIKWeight = NumberHelper.EaseInOutCubic(mLookIKWeight); }
				
				yield return null;
			}
			
			mLookIKEasingFunction = null;
		}

		#endregion Easing
		
		
		protected void RotateSpineToDirection(Quaternion rSource, Quaternion rTarget, float rWeight, bool rIsFast = false)
		{
			if (rWeight == 0f) { return; }
			
			// Move from local space to world space
			rSource = mMotionController._Transform.rotation * rSource;
			
			// Get the forward directions and bones
			Vector3 rTargetForward = rTarget * Vector3.forward;
			Vector3 rSourceForward = rSource * Vector3.forward;
			
			Transform lBody = mMotionController._Transform;
			Transform lSpine = mMotionController.Animator.GetBoneTransform(HumanBodyBones.Spine);
			Transform lChest = mMotionController.Animator.GetBoneTransform(HumanBodyBones.Chest);
			
			// If we're too far away from the target forward, just stop
			float lBodyAngle = Vector3Ext.HorizontalAngleTo(lBody.forward, rTargetForward, lBody.up);
			float lHorizontalAimAngle = Vector3Ext.HorizontalAngleTo(rSourceForward, rTargetForward, lBody.up);
			float lVerticalAimAngle = Vector3Ext.HorizontalAngleTo(rSourceForward, rTargetForward, lBody.right);
			float lTwistAimAngle = Vector3Ext.HorizontalAngleTo(rSource * Vector3.up, rTarget * Vector3.up, lBody.forward);
			
			float lFKPercent = Mathf.Clamp01(1f - (Mathf.Abs(lBodyAngle) / 60f));
			
			//Debug.Log("BSM.RotateSpineToDirection w:" + rWeight.ToString("f3") + " %:" + lFKPercent.ToString("f3") + " b:" + lBodyAngle.ToString("f3") + " h:" + lHorizontalAimAngle.ToString("f3") + " v:" + lVerticalAimAngle.ToString("f3"));
			
			if (lFKPercent == 0f) { return; }
			
			// In the case of extreme angles, get out
			//if (mLookIKMaxHorizontalAngle > 0f && Mathf.Abs(lHorizontalAimAngle) > mLookIKMaxHorizontalAngle) { return; }
			//if (Mathf.Abs(lHorizontalAimAngle) > 70f) { lHorizontalAimAngle = 0f; }
			//if (Mathf.Abs(lVerticalAimAngle) > 70f) { lVerticalAimAngle = 0f; }
			
			// Add any additional angles per the inspector
			float lHAngle = (lHorizontalAimAngle + (rIsFast ? _FastHorizontalAimAngle : _LookIKHorizontalAngle)) * rWeight * lFKPercent;
			float lVAngle = (lVerticalAimAngle + (rIsFast ? _FastVerticalAimAngle : _LookIKVerticalAngle)) * rWeight * lFKPercent;
			float lTAngle = (lTwistAimAngle + (rIsFast ? _FastVerticalAimAngle : _LookIKTwistAngle)) * rWeight * lFKPercent;
			//lVAngle = Mathf.Clamp(lVAngle, -65f, 65f);
			
			//Debug.Log("BSM.RotateSpineToDirection  h:" + lHAngle.ToString("f3") + " v:" + lVAngle.ToString("f3") + " t:" + lTAngle.ToString("f3"));
			
			// Check how many bones we'll rotate with and apply it
			if (lSpine != null && lChest != null) { lHAngle = lHAngle * 0.5f; }
			if (lSpine != null && lChest != null) { lVAngle = lVAngle * 0.5f; }
			if (lSpine != null && lChest != null) { lTAngle = lTAngle * 0.5f; }
			
			if (lSpine != null)
			{
				lSpine.rotation = Quaternion.AngleAxis(lTAngle, lBody.forward) * Quaternion.AngleAxis(lHAngle, lBody.up) * Quaternion.AngleAxis(lVAngle, lBody.right) * lSpine.rotation;
			}
			
			if (lChest != null)
			{
				lChest.rotation = Quaternion.AngleAxis(lTAngle, lBody.forward) * Quaternion.AngleAxis(lHAngle, lBody.up) * Quaternion.AngleAxis(lVAngle, lBody.right) * lChest.rotation;
			}
		}
		
		
		protected void RotateArmToSupport(Vector3 rTargetPosition, float rWeight, bool rIsFast = false)
		{
			if (!_IsSupportIKEnabled) { return; }
			if (rWeight == 0f) { return; }
			
			//Transform lUpperArm = mMotionController.Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
			Transform lLowerArm = mMotionController.Animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
			Transform lLeftHand = mMotionController.Animator.GetBoneTransform(HumanBodyBones.LeftHand);
			Vector3 lBoneVector = lLeftHand.position - lLowerArm.position;
			Vector3 lBoneDirection = lBoneVector.normalized;
			
			Vector3 lTargetDirection = (rTargetPosition - lLowerArm.position).normalized;
			
			Quaternion lLowerArmRotation = lLowerArm.rotation;
			if (lBoneDirection == lLowerArm.forward)
			{
				Quaternion lAdjust = Quaternion.FromToRotation(lLowerArm.forward, lTargetDirection);
				lLowerArmRotation = lAdjust * lLowerArmRotation;
			}
			else if (lBoneDirection == lLowerArm.right)
			{
				Quaternion lAdjust = Quaternion.FromToRotation(lLowerArm.right, lTargetDirection);
				lLowerArmRotation = lAdjust * lLowerArmRotation;
			}
			else if (lBoneDirection == lLowerArm.up)
			{
				Quaternion lAdjust = Quaternion.FromToRotation(lLowerArm.up, lTargetDirection);
				lLowerArmRotation = lAdjust * lLowerArmRotation;
			}
			
			lLowerArm.rotation = lLowerArmRotation;
		}

		#endregion IKFunctions
		
		
		protected MCGunCore FindWeapon(string rWeaponSlotID)
		{
			GameObject lWeapon = null;
			
			if (mInventorySource == null)
			{
				mInventorySource = mMotionController.gameObject.GetComponent<IInventorySource>();
				if (mInventorySource == null) { return null; }
			}
			
			ICombatant lCombatant = mMotionController.gameObject.GetComponent<ICombatant>();
			if (lCombatant != null)
			{
				IWeaponCore lWeaponCore = lCombatant.PrimaryWeapon;
				if (lWeaponCore != null) { lWeapon = lWeaponCore.gameObject; }
			}

			if (lWeapon == null)
			{
				string lEquipSlotID = rWeaponSlotID;
				if (lEquipSlotID.Length == 0) { lEquipSlotID = "RIGHT_HAND"; }

				string lItemID = mInventorySource.GetItemID(lEquipSlotID);
				lWeapon = mInventorySource.GetItemPropertyValue<GameObject>(lItemID, "Instance");
			}

			if (lWeapon != null)
			{
				MCGunCore lCore = lWeapon.GetComponent<MCGunCore>();
				if (lCore != null)
				{
					return lCore;
				}
			}

			return null;
		}

	}
}
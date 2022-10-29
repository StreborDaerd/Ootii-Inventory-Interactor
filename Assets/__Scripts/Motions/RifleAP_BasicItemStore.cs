using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Combat;
using com.ootii.Actors.Inventory;
using com.ootii.Data.Serializers;
using com.ootii.Geometry;
using com.ootii.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WildWalrus.Actors.AnimationControllers
{
	[MotionName("Rifle AP Basic Item Store")]
	[MotionDescription("Kubold's RIfle Anim Set Pro. Store the item based on the specified animation style.")]
	public class RifleAP_BasicItemStore : MotionControllerMotion, IStoreMotion
	{
		public static string EVENT_STORE = "store";
		
		
		#region MotionPhases

		public int PHASE_UNKNOWN = 0;
		public int PHASE_START = 73155;

		#endregion MotionPhases


		#region MotionProperties

		public override bool VerifyTransition
		{
			get { return false; }
		}

		#endregion MotionProperties


		#region IStoreMotion
		
		protected bool mIsEquipped = false;
		public bool IsEquipped
		{
			get { return mIsEquipped; }
			set { mIsEquipped = value; }
		}
		
		
		[NonSerialized]
		public string _OverrideItemID = null;
		[SerializationIgnore]
		public string OverrideItemID
		{
			get { return _OverrideItemID; }
			set { _OverrideItemID = value; }
		}
		
		
		[NonSerialized]
		public string _OverrideSlotID = null;
		[SerializationIgnore]
		public string OverrideSlotID
		{
			get { return _OverrideSlotID; }
			set { _OverrideSlotID = value; }
		}
		
		#endregion IStoreMotion


		#region Properties
		
		public string _SlotID = "RIGHT_HAND";
		public string SlotID
		{
			get { return _SlotID; }
			set { _SlotID = value; }
		}
		
		#endregion Properties


		#region Members
		
		[NonSerialized]
		protected IInventorySource mInventorySource = null;
		public IInventorySource InventorySource
		{
			get { return mInventorySource; }
			set { mInventorySource = value; }
		}
		
		
		protected GameObject mEquippedItem = null;
		public GameObject EquippedItem
		{
			get { return mEquippedItem; }
			set { mEquippedItem = value; }
		}
		
		#endregion Members


		#region Constructors
		
		public RifleAP_BasicItemStore() : base()
		{
			_Pack = BasicIdle.GroupName();
			_Category = EnumMotionCategories.UNKNOWN;
			
			_Priority = 8f;
			_ActionAlias = "";
			
#if UNITY_EDITOR
			if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "RifleAP_BasicEquipStore-SM"; }
#endif
		}
		
		
		public RifleAP_BasicItemStore(MotionController rController) : base(rController)
		{
			_Pack = BasicIdle.GroupName();
			_Category = EnumMotionCategories.UNKNOWN;
			
			_Priority = 8f;
			_ActionAlias = "";
			
#if UNITY_EDITOR
			if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "RifleAP_BasicEquipStore-SM"; }
#endif
		}
		
		#endregion Constructors


		#region MotionFunctions
		
		public override void Awake()
		{
			base.Awake();
			
			// If the input source is still null, see if we can grab a local input source
			if (mInventorySource == null && mMotionController != null)
			{
				mInventorySource = mMotionController.gameObject.GetComponent<IInventorySource>();
			}
		}
		

		#region Tests
		
		public override bool TestActivate()
		{
			if (!mIsStartable) { return false; }
			if (!mActorController.IsGrounded) { return false; }
			if (mMotionController._InputSource == null) { return false; }
			if (mMotionLayer._AnimatorTransitionID != 0) { return false; }
			
			// Since we're using BasicInventory, it can
			if (mInventorySource != null && !mInventorySource.AllowMotionSelfActivation) { return false; }
			
			// Determine if we should activate the motion
			if (_ActionAlias.Length > 0 && mMotionController._InputSource.IsJustPressed(_ActionAlias))
			{
				return true;
			}
			return false;
		}


		public override bool TestUpdate()
		{
			// If we've reached the exit state, leave. The delay is to ensure that we're not in an old motion's exit state
			if (mAge > 0.2f && mMotionController.State.AnimatorStates[mMotionLayer._AnimatorLayerIndex].StateInfo.IsTag("Exit"))
			{
				return false;
			}
			
			return true;
		}
		
		
		public override bool TestInterruption(MotionControllerMotion rMotion)
		{
			if (rMotion.Category != EnumMotionCategories.DEATH)
			{
				if (mIsEquipped)
				{
					mIsEquipped = false;
					StoreItem();
				}
			}
			
			return base.TestInterruption(rMotion);
		}
		
		#endregion Tests
		
		
		public override bool Activate(MotionControllerMotion rPrevMotion)
		{
			mIsEquipped = true;
			
			// Trigger the animation
			mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, _Form, 0, true);
			return base.Activate(rPrevMotion);
		}
		
		
		public override void Deactivate()
		{
			// Clear for the next activation
			_OverrideSlotID = "";
			_OverrideItemID = "";
			
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
		}
		
		
		public override void OnAnimationEvent(AnimationEvent rEvent)
		{
			if (rEvent == null) { return; }
			
			if (rEvent.stringParameter.Length == 0 || StringHelper.CleanString(rEvent.stringParameter) == EVENT_STORE)
			{
				if (mIsEquipped)
				{
					mIsEquipped = false;
					StoreItem();
				}
			}
		}

		#endregion MotionFunctions


		#region Functions
		
		protected virtual void StoreItem()
		{
			string lSlotID = "";
			if (OverrideSlotID.Length > 0)
			{
				lSlotID = OverrideSlotID;
			}
			else
			{
				lSlotID = SlotID;
			}
			
			GameObject lWeapon = mEquippedItem;
			
			ICombatant lCombatant = mMotionController.gameObject.GetComponent<ICombatant>();
			if (lCombatant != null)
			{
				if (lCombatant.PrimaryWeapon != null)
				{
					lWeapon = lCombatant.PrimaryWeapon.gameObject;
					lCombatant.PrimaryWeapon.Owner = null;
				}
				
				lCombatant.PrimaryWeapon = null;
			}
			
			if (mInventorySource != null)
			{
				mInventorySource.StoreItem(lSlotID);
			}
			else
			{
				if (lWeapon == null)
				{
					Transform lParentBone = FindTransform(mMotionController._Transform, _SlotID);
					Transform lWeaponTransform = lParentBone.GetChild(lParentBone.childCount - 1);
					if (lWeaponTransform != null) { lWeapon = lWeaponTransform.gameObject; }
				}
				
				if (lWeapon != null)
				{
					GameObject.Destroy(lWeapon);
					mEquippedItem = null;
				}
			}
			
			// Remove a body sphere we may have added
			mMotionController._ActorController.RemoveBodyShape("Combatant Shape");
		}
		
		
		protected Transform FindTransform(Transform rParent, string rName)
		{
			Transform lTransform = null;
			
			// Check by HumanBone name
			Animator lAnimator = rParent.GetComponentInChildren<Animator>();
			if (lAnimator != null)
			{
				if (BasicInventory.UnityBones == null)
				{
					BasicInventory.UnityBones = System.Enum.GetNames(typeof(HumanBodyBones));
					for (int i = 0; i < BasicInventory.UnityBones.Length; i++)
					{
						BasicInventory.UnityBones[i] = StringHelper.CleanString(BasicInventory.UnityBones[i]);
					}
				}
				
				string lCleanName = StringHelper.CleanString(rName);
				for (int i = 0; i < BasicInventory.UnityBones.Length; i++)
				{
					if (BasicInventory.UnityBones[i] == lCleanName)
					{
						lTransform = lAnimator.GetBoneTransform((HumanBodyBones)i);
						break;
					}
				}
			}
			
			// Check if by exact name
			if (lTransform == null)
			{
				lTransform = rParent.transform.FindTransform(rName);
			}
			
			// Default to the root
			if (lTransform == null)
			{
				lTransform = rParent.transform;
			}
			
			return lTransform;
		}
		
		#endregion Functions


		#region EditorFunctions

#if UNITY_EDITOR

		public override bool OnInspectorGUI()
		{
			bool lIsDirty = false;
			
			if (EditorHelper.IntField("Form",
				"Within the animator state, defines which animator flow will run. This is used to control animations.",
				Form, mMotionController))
			{
				lIsDirty = true;
				Form = EditorHelper.FieldIntValue;
			}
			
			if (EditorHelper.TextField("Action Alias",
				"Action alias that is used to store the item.",
				ActionAlias, mMotionController))
			{
				lIsDirty = true;
				ActionAlias = EditorHelper.FieldStringValue;
			}
			
			GUILayout.Space(5f);
			
			if (EditorHelper.TextField("Slot ID",
				"ID of the slot the item should be held in.",
				SlotID, mMotionController))
			{
				lIsDirty = true;
				SlotID = EditorHelper.FieldStringValue;
			}
			
			return lIsDirty;
		}

#endif

		#endregion EditorFunctions


		#region Auto-Generated
		
		/// <summary>
		/// These declarations go inside the class so you can test for which state
		/// and transitions are active. Testing hash values is much faster than strings.
		/// </summary>
		public int STATE_Empty = -1;
		public int STATE_EquipRifle = -1;
		public int STATE_StoreRifle = -1;
		public int STATE_StoreRifleIdlePoseExit = -1;
		public int STATE_EquipRifleIdlePoseExit = -1;
		public int STATE_EquipPistol = -1;
		public int STATE_StorePistol = -1;
		public int STATE_StorePistolIdlePoseExit = -1;
		public int STATE_EquipPistolIdlePoseExit = -1;
		public int STATE_Equip_Bow = -1;
		public int STATE_Disarm_Bow = -1;
		public int STATE_EquipBowIdlePoseExit = -1;
		public int STATE_StoreBowIdlePoseExit = -1;
		public int TRANS_AnyState_EquipRifle = -1;
		public int TRANS_EntryState_EquipRifle = -1;
		public int TRANS_AnyState_StoreRifle = -1;
		public int TRANS_EntryState_StoreRifle = -1;
		public int TRANS_AnyState_EquipPistol = -1;
		public int TRANS_EntryState_EquipPistol = -1;
		public int TRANS_AnyState_StorePistol = -1;
		public int TRANS_EntryState_StorePistol = -1;
		public int TRANS_AnyState_Disarm_Bow = -1;
		public int TRANS_EntryState_Disarm_Bow = -1;
		public int TRANS_AnyState_Equip_Bow = -1;
		public int TRANS_EntryState_Equip_Bow = -1;
		public int TRANS_EquipRifle_EquipRifleIdlePoseExit = -1;
		public int TRANS_StoreRifle_StoreRifleIdlePoseExit = -1;
		public int TRANS_EquipPistol_EquipPistolIdlePoseExit = -1;
		public int TRANS_StorePistol_StorePistolIdlePoseExit = -1;
		public int TRANS_Equip_Bow_EquipBowIdlePoseExit = -1;
		public int TRANS_Disarm_Bow_StoreBowIdlePoseExit = -1;

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
					if (lStateID == STATE_EquipRifle) { return true; }
					if (lStateID == STATE_StoreRifle) { return true; }
					if (lStateID == STATE_StoreRifleIdlePoseExit) { return true; }
					if (lStateID == STATE_EquipRifleIdlePoseExit) { return true; }
					if (lStateID == STATE_EquipPistol) { return true; }
					if (lStateID == STATE_StorePistol) { return true; }
					if (lStateID == STATE_StorePistolIdlePoseExit) { return true; }
					if (lStateID == STATE_EquipPistolIdlePoseExit) { return true; }
					if (lStateID == STATE_Equip_Bow) { return true; }
					if (lStateID == STATE_Disarm_Bow) { return true; }
					if (lStateID == STATE_EquipBowIdlePoseExit) { return true; }
					if (lStateID == STATE_StoreBowIdlePoseExit) { return true; }
				}

				if (lTransitionID == TRANS_AnyState_EquipRifle) { return true; }
				if (lTransitionID == TRANS_EntryState_EquipRifle) { return true; }
				if (lTransitionID == TRANS_AnyState_StoreRifle) { return true; }
				if (lTransitionID == TRANS_EntryState_StoreRifle) { return true; }
				if (lTransitionID == TRANS_AnyState_EquipPistol) { return true; }
				if (lTransitionID == TRANS_EntryState_EquipPistol) { return true; }
				if (lTransitionID == TRANS_AnyState_StorePistol) { return true; }
				if (lTransitionID == TRANS_EntryState_StorePistol) { return true; }
				if (lTransitionID == TRANS_AnyState_Disarm_Bow) { return true; }
				if (lTransitionID == TRANS_EntryState_Disarm_Bow) { return true; }
				if (lTransitionID == TRANS_AnyState_Equip_Bow) { return true; }
				if (lTransitionID == TRANS_EntryState_Equip_Bow) { return true; }
				if (lTransitionID == TRANS_EquipRifle_EquipRifleIdlePoseExit) { return true; }
				if (lTransitionID == TRANS_StoreRifle_StoreRifleIdlePoseExit) { return true; }
				if (lTransitionID == TRANS_EquipPistol_EquipPistolIdlePoseExit) { return true; }
				if (lTransitionID == TRANS_StorePistol_StorePistolIdlePoseExit) { return true; }
				if (lTransitionID == TRANS_Equip_Bow_EquipBowIdlePoseExit) { return true; }
				if (lTransitionID == TRANS_Disarm_Bow_StoreBowIdlePoseExit) { return true; }
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
			if (rStateID == STATE_EquipRifle) { return true; }
			if (rStateID == STATE_StoreRifle) { return true; }
			if (rStateID == STATE_StoreRifleIdlePoseExit) { return true; }
			if (rStateID == STATE_EquipRifleIdlePoseExit) { return true; }
			if (rStateID == STATE_EquipPistol) { return true; }
			if (rStateID == STATE_StorePistol) { return true; }
			if (rStateID == STATE_StorePistolIdlePoseExit) { return true; }
			if (rStateID == STATE_EquipPistolIdlePoseExit) { return true; }
			if (rStateID == STATE_Equip_Bow) { return true; }
			if (rStateID == STATE_Disarm_Bow) { return true; }
			if (rStateID == STATE_EquipBowIdlePoseExit) { return true; }
			if (rStateID == STATE_StoreBowIdlePoseExit) { return true; }
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
				if (rStateID == STATE_EquipRifle) { return true; }
				if (rStateID == STATE_StoreRifle) { return true; }
				if (rStateID == STATE_StoreRifleIdlePoseExit) { return true; }
				if (rStateID == STATE_EquipRifleIdlePoseExit) { return true; }
				if (rStateID == STATE_EquipPistol) { return true; }
				if (rStateID == STATE_StorePistol) { return true; }
				if (rStateID == STATE_StorePistolIdlePoseExit) { return true; }
				if (rStateID == STATE_EquipPistolIdlePoseExit) { return true; }
				if (rStateID == STATE_Equip_Bow) { return true; }
				if (rStateID == STATE_Disarm_Bow) { return true; }
				if (rStateID == STATE_EquipBowIdlePoseExit) { return true; }
				if (rStateID == STATE_StoreBowIdlePoseExit) { return true; }
			}

			if (rTransitionID == TRANS_AnyState_EquipRifle) { return true; }
			if (rTransitionID == TRANS_EntryState_EquipRifle) { return true; }
			if (rTransitionID == TRANS_AnyState_StoreRifle) { return true; }
			if (rTransitionID == TRANS_EntryState_StoreRifle) { return true; }
			if (rTransitionID == TRANS_AnyState_EquipPistol) { return true; }
			if (rTransitionID == TRANS_EntryState_EquipPistol) { return true; }
			if (rTransitionID == TRANS_AnyState_StorePistol) { return true; }
			if (rTransitionID == TRANS_EntryState_StorePistol) { return true; }
			if (rTransitionID == TRANS_AnyState_Disarm_Bow) { return true; }
			if (rTransitionID == TRANS_EntryState_Disarm_Bow) { return true; }
			if (rTransitionID == TRANS_AnyState_Equip_Bow) { return true; }
			if (rTransitionID == TRANS_EntryState_Equip_Bow) { return true; }
			if (rTransitionID == TRANS_EquipRifle_EquipRifleIdlePoseExit) { return true; }
			if (rTransitionID == TRANS_StoreRifle_StoreRifleIdlePoseExit) { return true; }
			if (rTransitionID == TRANS_EquipPistol_EquipPistolIdlePoseExit) { return true; }
			if (rTransitionID == TRANS_StorePistol_StorePistolIdlePoseExit) { return true; }
			if (rTransitionID == TRANS_Equip_Bow_EquipBowIdlePoseExit) { return true; }
			if (rTransitionID == TRANS_Disarm_Bow_StoreBowIdlePoseExit) { return true; }
			return false;
		}

		/// <summary>
		/// Preprocess any animator data so the motion can use it later
		/// </summary>
		public override void LoadAnimatorData()
		{
			string lLayer = mMotionController.Animator.GetLayerName(mMotionLayer._AnimatorLayerIndex);
			TRANS_AnyState_EquipRifle = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".RifleAP_BasicEquipStore-SM.Equip Rifle");
			TRANS_EntryState_EquipRifle = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".RifleAP_BasicEquipStore-SM.Equip Rifle");
			TRANS_AnyState_StoreRifle = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".RifleAP_BasicEquipStore-SM.Store Rifle");
			TRANS_EntryState_StoreRifle = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".RifleAP_BasicEquipStore-SM.Store Rifle");
			TRANS_AnyState_EquipPistol = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".RifleAP_BasicEquipStore-SM.Equip Pistol");
			TRANS_EntryState_EquipPistol = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".RifleAP_BasicEquipStore-SM.Equip Pistol");
			TRANS_AnyState_StorePistol = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".RifleAP_BasicEquipStore-SM.Store Pistol");
			TRANS_EntryState_StorePistol = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".RifleAP_BasicEquipStore-SM.Store Pistol");
			TRANS_AnyState_Disarm_Bow = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".RifleAP_BasicEquipStore-SM.Disarm_Bow");
			TRANS_EntryState_Disarm_Bow = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".RifleAP_BasicEquipStore-SM.Disarm_Bow");
			TRANS_AnyState_Equip_Bow = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".RifleAP_BasicEquipStore-SM.Equip_Bow");
			TRANS_EntryState_Equip_Bow = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".RifleAP_BasicEquipStore-SM.Equip_Bow");
			STATE_Empty = mMotionController.AddAnimatorName("" + lLayer + ".Empty");
			STATE_EquipRifle = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicEquipStore-SM.Equip Rifle");
			TRANS_EquipRifle_EquipRifleIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicEquipStore-SM.Equip Rifle -> " + lLayer + ".RifleAP_BasicEquipStore-SM.Equip Rifle Idle Pose Exit");
			STATE_StoreRifle = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicEquipStore-SM.Store Rifle");
			TRANS_StoreRifle_StoreRifleIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicEquipStore-SM.Store Rifle -> " + lLayer + ".RifleAP_BasicEquipStore-SM.Store Rifle Idle Pose Exit");
			STATE_StoreRifleIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicEquipStore-SM.Store Rifle Idle Pose Exit");
			STATE_EquipRifleIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicEquipStore-SM.Equip Rifle Idle Pose Exit");
			STATE_EquipPistol = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicEquipStore-SM.Equip Pistol");
			TRANS_EquipPistol_EquipPistolIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicEquipStore-SM.Equip Pistol -> " + lLayer + ".RifleAP_BasicEquipStore-SM.Equip Pistol Idle Pose Exit");
			STATE_StorePistol = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicEquipStore-SM.Store Pistol");
			TRANS_StorePistol_StorePistolIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicEquipStore-SM.Store Pistol -> " + lLayer + ".RifleAP_BasicEquipStore-SM.Store Pistol Idle Pose Exit");
			STATE_StorePistolIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicEquipStore-SM.Store Pistol Idle Pose Exit");
			STATE_EquipPistolIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicEquipStore-SM.Equip Pistol Idle Pose Exit");
			STATE_Equip_Bow = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicEquipStore-SM.Equip_Bow");
			TRANS_Equip_Bow_EquipBowIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicEquipStore-SM.Equip_Bow -> " + lLayer + ".RifleAP_BasicEquipStore-SM.EquipBowIdlePoseExit");
			STATE_Disarm_Bow = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicEquipStore-SM.Disarm_Bow");
			TRANS_Disarm_Bow_StoreBowIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicEquipStore-SM.Disarm_Bow -> " + lLayer + ".RifleAP_BasicEquipStore-SM.StoreBowIdlePoseExit");
			STATE_EquipBowIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicEquipStore-SM.EquipBowIdlePoseExit");
			STATE_StoreBowIdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".RifleAP_BasicEquipStore-SM.StoreBowIdlePoseExit");
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

			UnityEditor.Animations.AnimatorStateMachine lSSM_31648 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "RifleAP_BasicEquipStore-SM");
			if (lSSM_31648 == null) { lSSM_31648 = lLayerStateMachine.AddStateMachine("RifleAP_BasicEquipStore-SM", new Vector3(240, -990, 0)); }

			UnityEditor.Animations.AnimatorState lState_33224 = MotionControllerMotion.EditorFindState(lSSM_31648, "Equip Rifle");
			if (lState_33224 == null) { lState_33224 = lSSM_31648.AddState("Equip Rifle", new Vector3(300, 520, 0)); }
			lState_33224.speed = 1f;
			lState_33224.mirror = false;
			lState_33224.tag = "";
			lState_33224.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/RifleAnimsetPro/Animations/RifleAnimsetPro_Equips.fbx", "EquipRifle");

			UnityEditor.Animations.AnimatorState lState_33226 = MotionControllerMotion.EditorFindState(lSSM_31648, "Store Rifle");
			if (lState_33226 == null) { lState_33226 = lSSM_31648.AddState("Store Rifle", new Vector3(300, 576, 0)); }
			lState_33226.speed = 1f;
			lState_33226.mirror = false;
			lState_33226.tag = "";
			lState_33226.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/RifleAnimsetPro/Animations/RifleAnimsetPro_Equips.fbx", "HolsterRifle");

			UnityEditor.Animations.AnimatorState lState_33228 = MotionControllerMotion.EditorFindState(lSSM_31648, "Store Rifle Idle Pose Exit");
			if (lState_33228 == null) { lState_33228 = lSSM_31648.AddState("Store Rifle Idle Pose Exit", new Vector3(552, 576, 0)); }
			lState_33228.speed = 1f;
			lState_33228.mirror = false;
			lState_33228.tag = "Exit";
			lState_33228.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/MovementAnimsetPro/Animations/MovementAnimsetPro.fbx", "Idle_Pose");

			UnityEditor.Animations.AnimatorState lState_33230 = MotionControllerMotion.EditorFindState(lSSM_31648, "Equip Rifle Idle Pose Exit");
			if (lState_33230 == null) { lState_33230 = lSSM_31648.AddState("Equip Rifle Idle Pose Exit", new Vector3(552, 528, 0)); }
			lState_33230.speed = 1f;
			lState_33230.mirror = false;
			lState_33230.tag = "Exit";
			lState_33230.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_Idle_Pose");

			UnityEditor.Animations.AnimatorState lState_33232 = MotionControllerMotion.EditorFindState(lSSM_31648, "Equip Pistol");
			if (lState_33232 == null) { lState_33232 = lSSM_31648.AddState("Equip Pistol", new Vector3(300, 648, 0)); }
			lState_33232.speed = 1f;
			lState_33232.mirror = false;
			lState_33232.tag = "";
			lState_33232.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/PistolAnimsetPro/Animations/PistolAnimsetPro.fbx", "Pistol_Equip");

			UnityEditor.Animations.AnimatorState lState_33234 = MotionControllerMotion.EditorFindState(lSSM_31648, "Store Pistol");
			if (lState_33234 == null) { lState_33234 = lSSM_31648.AddState("Store Pistol", new Vector3(300, 696, 0)); }
			lState_33234.speed = 1f;
			lState_33234.mirror = false;
			lState_33234.tag = "";
			lState_33234.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/PistolAnimsetPro/Animations/PistolAnimsetPro.fbx", "Pistol_UnEquip");

			UnityEditor.Animations.AnimatorState lState_33236 = MotionControllerMotion.EditorFindState(lSSM_31648, "Store Pistol Idle Pose Exit");
			if (lState_33236 == null) { lState_33236 = lSSM_31648.AddState("Store Pistol Idle Pose Exit", new Vector3(552, 696, 0)); }
			lState_33236.speed = 1f;
			lState_33236.mirror = false;
			lState_33236.tag = "Exit";
			lState_33236.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/MovementAnimsetPro/Animations/MovementAnimsetPro.fbx", "Idle_Pose");

			UnityEditor.Animations.AnimatorState lState_33238 = MotionControllerMotion.EditorFindState(lSSM_31648, "Equip Pistol Idle Pose Exit");
			if (lState_33238 == null) { lState_33238 = lSSM_31648.AddState("Equip Pistol Idle Pose Exit", new Vector3(552, 648, 0)); }
			lState_33238.speed = 1f;
			lState_33238.mirror = false;
			lState_33238.tag = "Exit";
			lState_33238.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/PistolAnimsetPro/Animations/PistolAnimsetPro.fbx", "Pistol_Idle_Pose");

			UnityEditor.Animations.AnimatorState lState_33240 = MotionControllerMotion.EditorFindState(lSSM_31648, "Equip_Bow");
			if (lState_33240 == null) { lState_33240 = lSSM_31648.AddState("Equip_Bow", new Vector3(300, 288, 0)); }
			lState_33240.speed = 1f;
			lState_33240.mirror = false;
			lState_33240.tag = "";
			lState_33240.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Archery/Content/Animations/Mixamo/standing equip bow.fbx", "standing equip bow");

			UnityEditor.Animations.AnimatorState lState_33242 = MotionControllerMotion.EditorFindState(lSSM_31648, "Disarm_Bow");
			if (lState_33242 == null) { lState_33242 = lSSM_31648.AddState("Disarm_Bow", new Vector3(300, 340, 0)); }
			lState_33242.speed = 1f;
			lState_33242.mirror = false;
			lState_33242.tag = "";
			lState_33242.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Archery/Content/Animations/Mixamo/standing disarm bow.fbx", "standing disarm bow");

			UnityEditor.Animations.AnimatorState lState_33244 = MotionControllerMotion.EditorFindState(lSSM_31648, "EquipBowIdlePoseExit");
			if (lState_33244 == null) { lState_33244 = lSSM_31648.AddState("EquipBowIdlePoseExit", new Vector3(550, 290, 0)); }
			lState_33244.speed = 1f;
			lState_33244.mirror = false;
			lState_33244.tag = "Exit";
			lState_33244.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Archery/Content/Animations/Mixamo/standing idle 01.fbx", "IdlePose");

			UnityEditor.Animations.AnimatorState lState_33246 = MotionControllerMotion.EditorFindState(lSSM_31648, "StoreBowIdlePoseExit");
			if (lState_33246 == null) { lState_33246 = lSSM_31648.AddState("StoreBowIdlePoseExit", new Vector3(550, 340, 0)); }
			lState_33246.speed = 1f;
			lState_33246.mirror = false;
			lState_33246.tag = "Exit";
			lState_33246.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/MovementAnimsetPro/Animations/MovementAnimsetPro.fbx", "Idle_Pose");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33202 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_33224, 0);
			if (lAnyTransition_33202 == null) { lAnyTransition_33202 = lLayerStateMachine.AddAnyStateTransition(lState_33224); }
			lAnyTransition_33202.isExit = false;
			lAnyTransition_33202.hasExitTime = false;
			lAnyTransition_33202.hasFixedDuration = true;
			lAnyTransition_33202.exitTime = 0.03445775f;
			lAnyTransition_33202.duration = 0.1000001f;
			lAnyTransition_33202.offset = 0f;
			lAnyTransition_33202.mute = false;
			lAnyTransition_33202.solo = false;
			lAnyTransition_33202.canTransitionToSelf = true;
			lAnyTransition_33202.orderedInterruption = true;
			lAnyTransition_33202.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_33202.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33202.RemoveCondition(lAnyTransition_33202.conditions[i]); }
			lAnyTransition_33202.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 73150f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33202.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 500f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33204 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_33226, 0);
			if (lAnyTransition_33204 == null) { lAnyTransition_33204 = lLayerStateMachine.AddAnyStateTransition(lState_33226); }
			lAnyTransition_33204.isExit = false;
			lAnyTransition_33204.hasExitTime = false;
			lAnyTransition_33204.hasFixedDuration = true;
			lAnyTransition_33204.exitTime = 0.04618796f;
			lAnyTransition_33204.duration = 0.1000001f;
			lAnyTransition_33204.offset = 0f;
			lAnyTransition_33204.mute = false;
			lAnyTransition_33204.solo = false;
			lAnyTransition_33204.canTransitionToSelf = true;
			lAnyTransition_33204.orderedInterruption = true;
			lAnyTransition_33204.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_33204.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33204.RemoveCondition(lAnyTransition_33204.conditions[i]); }
			lAnyTransition_33204.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 73155f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33204.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 550f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33206 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_33232, 0);
			if (lAnyTransition_33206 == null) { lAnyTransition_33206 = lLayerStateMachine.AddAnyStateTransition(lState_33232); }
			lAnyTransition_33206.isExit = false;
			lAnyTransition_33206.hasExitTime = false;
			lAnyTransition_33206.hasFixedDuration = true;
			lAnyTransition_33206.exitTime = 0.01803551f;
			lAnyTransition_33206.duration = 0.05695996f;
			lAnyTransition_33206.offset = 0f;
			lAnyTransition_33206.mute = false;
			lAnyTransition_33206.solo = false;
			lAnyTransition_33206.canTransitionToSelf = true;
			lAnyTransition_33206.orderedInterruption = true;
			lAnyTransition_33206.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_33206.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33206.RemoveCondition(lAnyTransition_33206.conditions[i]); }
			lAnyTransition_33206.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 73150f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33206.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 550f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33208 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_33234, 0);
			if (lAnyTransition_33208 == null) { lAnyTransition_33208 = lLayerStateMachine.AddAnyStateTransition(lState_33234); }
			lAnyTransition_33208.isExit = false;
			lAnyTransition_33208.hasExitTime = false;
			lAnyTransition_33208.hasFixedDuration = true;
			lAnyTransition_33208.exitTime = 0.009677414f;
			lAnyTransition_33208.duration = 0.1f;
			lAnyTransition_33208.offset = 0.007258054f;
			lAnyTransition_33208.mute = false;
			lAnyTransition_33208.solo = false;
			lAnyTransition_33208.canTransitionToSelf = true;
			lAnyTransition_33208.orderedInterruption = true;
			lAnyTransition_33208.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_33208.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33208.RemoveCondition(lAnyTransition_33208.conditions[i]); }
			lAnyTransition_33208.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 73155f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33208.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 550f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33210 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_33242, 0);
			if (lAnyTransition_33210 == null) { lAnyTransition_33210 = lLayerStateMachine.AddAnyStateTransition(lState_33242); }
			lAnyTransition_33210.isExit = false;
			lAnyTransition_33210.hasExitTime = false;
			lAnyTransition_33210.hasFixedDuration = true;
			lAnyTransition_33210.exitTime = 0.75f;
			lAnyTransition_33210.duration = 0.25f;
			lAnyTransition_33210.offset = 0f;
			lAnyTransition_33210.mute = false;
			lAnyTransition_33210.solo = false;
			lAnyTransition_33210.canTransitionToSelf = true;
			lAnyTransition_33210.orderedInterruption = true;
			lAnyTransition_33210.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_33210.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33210.RemoveCondition(lAnyTransition_33210.conditions[i]); }
			lAnyTransition_33210.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 73155f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33210.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 200f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33212 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_33240, 0);
			if (lAnyTransition_33212 == null) { lAnyTransition_33212 = lLayerStateMachine.AddAnyStateTransition(lState_33240); }
			lAnyTransition_33212.isExit = false;
			lAnyTransition_33212.hasExitTime = false;
			lAnyTransition_33212.hasFixedDuration = true;
			lAnyTransition_33212.exitTime = 0.75f;
			lAnyTransition_33212.duration = 0.25f;
			lAnyTransition_33212.offset = 0f;
			lAnyTransition_33212.mute = false;
			lAnyTransition_33212.solo = false;
			lAnyTransition_33212.canTransitionToSelf = true;
			lAnyTransition_33212.orderedInterruption = true;
			lAnyTransition_33212.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_33212.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33212.RemoveCondition(lAnyTransition_33212.conditions[i]); }
			lAnyTransition_33212.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73150f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33212.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 200f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39336 = MotionControllerMotion.EditorFindTransition(lState_33224, lState_33230, 0);
			if (lTransition_39336 == null) { lTransition_39336 = lState_33224.AddTransition(lState_33230); }
			lTransition_39336.isExit = false;
			lTransition_39336.hasExitTime = true;
			lTransition_39336.hasFixedDuration = true;
			lTransition_39336.exitTime = 0.5167463f;
			lTransition_39336.duration = 0.2f;
			lTransition_39336.offset = 0f;
			lTransition_39336.mute = false;
			lTransition_39336.solo = false;
			lTransition_39336.canTransitionToSelf = true;
			lTransition_39336.orderedInterruption = true;
			lTransition_39336.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39336.conditions.Length - 1; i >= 0; i--) { lTransition_39336.RemoveCondition(lTransition_39336.conditions[i]); }

			UnityEditor.Animations.AnimatorStateTransition lTransition_39340 = MotionControllerMotion.EditorFindTransition(lState_33226, lState_33228, 0);
			if (lTransition_39340 == null) { lTransition_39340 = lState_33226.AddTransition(lState_33228); }
			lTransition_39340.isExit = false;
			lTransition_39340.hasExitTime = true;
			lTransition_39340.hasFixedDuration = true;
			lTransition_39340.exitTime = 0.4055317f;
			lTransition_39340.duration = 0.2f;
			lTransition_39340.offset = 0f;
			lTransition_39340.mute = false;
			lTransition_39340.solo = false;
			lTransition_39340.canTransitionToSelf = true;
			lTransition_39340.orderedInterruption = true;
			lTransition_39340.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39340.conditions.Length - 1; i >= 0; i--) { lTransition_39340.RemoveCondition(lTransition_39340.conditions[i]); }

			UnityEditor.Animations.AnimatorStateTransition lTransition_39344 = MotionControllerMotion.EditorFindTransition(lState_33232, lState_33238, 0);
			if (lTransition_39344 == null) { lTransition_39344 = lState_33232.AddTransition(lState_33238); }
			lTransition_39344.isExit = false;
			lTransition_39344.hasExitTime = true;
			lTransition_39344.hasFixedDuration = true;
			lTransition_39344.exitTime = 0.5760845f;
			lTransition_39344.duration = 0.1999999f;
			lTransition_39344.offset = 0f;
			lTransition_39344.mute = false;
			lTransition_39344.solo = false;
			lTransition_39344.canTransitionToSelf = true;
			lTransition_39344.orderedInterruption = true;
			lTransition_39344.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39344.conditions.Length - 1; i >= 0; i--) { lTransition_39344.RemoveCondition(lTransition_39344.conditions[i]); }

			UnityEditor.Animations.AnimatorStateTransition lTransition_39348 = MotionControllerMotion.EditorFindTransition(lState_33234, lState_33236, 0);
			if (lTransition_39348 == null) { lTransition_39348 = lState_33234.AddTransition(lState_33236); }
			lTransition_39348.isExit = false;
			lTransition_39348.hasExitTime = true;
			lTransition_39348.hasFixedDuration = true;
			lTransition_39348.exitTime = 0.6415698f;
			lTransition_39348.duration = 0.2f;
			lTransition_39348.offset = 0f;
			lTransition_39348.mute = false;
			lTransition_39348.solo = false;
			lTransition_39348.canTransitionToSelf = true;
			lTransition_39348.orderedInterruption = true;
			lTransition_39348.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39348.conditions.Length - 1; i >= 0; i--) { lTransition_39348.RemoveCondition(lTransition_39348.conditions[i]); }

			UnityEditor.Animations.AnimatorStateTransition lTransition_39352 = MotionControllerMotion.EditorFindTransition(lState_33240, lState_33244, 0);
			if (lTransition_39352 == null) { lTransition_39352 = lState_33240.AddTransition(lState_33244); }
			lTransition_39352.isExit = false;
			lTransition_39352.hasExitTime = true;
			lTransition_39352.hasFixedDuration = true;
			lTransition_39352.exitTime = 0.9f;
			lTransition_39352.duration = 0.1f;
			lTransition_39352.offset = 0f;
			lTransition_39352.mute = false;
			lTransition_39352.solo = false;
			lTransition_39352.canTransitionToSelf = true;
			lTransition_39352.orderedInterruption = true;
			lTransition_39352.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39352.conditions.Length - 1; i >= 0; i--) { lTransition_39352.RemoveCondition(lTransition_39352.conditions[i]); }

			UnityEditor.Animations.AnimatorStateTransition lTransition_39354 = MotionControllerMotion.EditorFindTransition(lState_33242, lState_33246, 0);
			if (lTransition_39354 == null) { lTransition_39354 = lState_33242.AddTransition(lState_33246); }
			lTransition_39354.isExit = false;
			lTransition_39354.hasExitTime = true;
			lTransition_39354.hasFixedDuration = true;
			lTransition_39354.exitTime = 0.9f;
			lTransition_39354.duration = 0.1f;
			lTransition_39354.offset = 0f;
			lTransition_39354.mute = false;
			lTransition_39354.solo = false;
			lTransition_39354.canTransitionToSelf = true;
			lTransition_39354.orderedInterruption = true;
			lTransition_39354.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39354.conditions.Length - 1; i >= 0; i--) { lTransition_39354.RemoveCondition(lTransition_39354.conditions[i]); }


			// Run any post processing after creating the state machine
			OnStateMachineCreated();
		}

#endif

		#endregion Auto-Generated


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

			UnityEditor.Animations.AnimatorStateMachine lSSM_31648 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "RifleAP_BasicEquipStore-SM");
			if (lSSM_31648 == null) { lSSM_31648 = lLayerStateMachine.AddStateMachine("RifleAP_BasicEquipStore-SM", new Vector3(240, -990, 0)); }

			UnityEditor.Animations.AnimatorState lState_33224 = MotionControllerMotion.EditorFindState(lSSM_31648, "Equip Rifle");
			if (lState_33224 == null) { lState_33224 = lSSM_31648.AddState("Equip Rifle", new Vector3(300, 520, 0)); }
			lState_33224.speed = 1f;
			lState_33224.mirror = false;
			lState_33224.tag = "";
			lState_33224.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/RifleAnimsetPro/Animations/RifleAnimsetPro_Equips.fbx", "EquipRifle");

			UnityEditor.Animations.AnimatorState lState_33226 = MotionControllerMotion.EditorFindState(lSSM_31648, "Store Rifle");
			if (lState_33226 == null) { lState_33226 = lSSM_31648.AddState("Store Rifle", new Vector3(300, 576, 0)); }
			lState_33226.speed = 1f;
			lState_33226.mirror = false;
			lState_33226.tag = "";
			lState_33226.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/RifleAnimsetPro/Animations/RifleAnimsetPro_Equips.fbx", "HolsterRifle");

			UnityEditor.Animations.AnimatorState lState_33228 = MotionControllerMotion.EditorFindState(lSSM_31648, "Store Rifle Idle Pose Exit");
			if (lState_33228 == null) { lState_33228 = lSSM_31648.AddState("Store Rifle Idle Pose Exit", new Vector3(552, 576, 0)); }
			lState_33228.speed = 1f;
			lState_33228.mirror = false;
			lState_33228.tag = "Exit";
			lState_33228.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/MovementAnimsetPro/Animations/MovementAnimsetPro.fbx", "Idle_Pose");

			UnityEditor.Animations.AnimatorState lState_33230 = MotionControllerMotion.EditorFindState(lSSM_31648, "Equip Rifle Idle Pose Exit");
			if (lState_33230 == null) { lState_33230 = lSSM_31648.AddState("Equip Rifle Idle Pose Exit", new Vector3(552, 528, 0)); }
			lState_33230.speed = 1f;
			lState_33230.mirror = false;
			lState_33230.tag = "Exit";
			lState_33230.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_Idle_Pose");

			UnityEditor.Animations.AnimatorState lState_33232 = MotionControllerMotion.EditorFindState(lSSM_31648, "Equip Pistol");
			if (lState_33232 == null) { lState_33232 = lSSM_31648.AddState("Equip Pistol", new Vector3(300, 648, 0)); }
			lState_33232.speed = 1f;
			lState_33232.mirror = false;
			lState_33232.tag = "";
			lState_33232.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/PistolAnimsetPro/Animations/PistolAnimsetPro.fbx", "Pistol_Equip");

			UnityEditor.Animations.AnimatorState lState_33234 = MotionControllerMotion.EditorFindState(lSSM_31648, "Store Pistol");
			if (lState_33234 == null) { lState_33234 = lSSM_31648.AddState("Store Pistol", new Vector3(300, 696, 0)); }
			lState_33234.speed = 1f;
			lState_33234.mirror = false;
			lState_33234.tag = "";
			lState_33234.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/PistolAnimsetPro/Animations/PistolAnimsetPro.fbx", "Pistol_UnEquip");

			UnityEditor.Animations.AnimatorState lState_33236 = MotionControllerMotion.EditorFindState(lSSM_31648, "Store Pistol Idle Pose Exit");
			if (lState_33236 == null) { lState_33236 = lSSM_31648.AddState("Store Pistol Idle Pose Exit", new Vector3(552, 696, 0)); }
			lState_33236.speed = 1f;
			lState_33236.mirror = false;
			lState_33236.tag = "Exit";
			lState_33236.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/MovementAnimsetPro/Animations/MovementAnimsetPro.fbx", "Idle_Pose");

			UnityEditor.Animations.AnimatorState lState_33238 = MotionControllerMotion.EditorFindState(lSSM_31648, "Equip Pistol Idle Pose Exit");
			if (lState_33238 == null) { lState_33238 = lSSM_31648.AddState("Equip Pistol Idle Pose Exit", new Vector3(552, 648, 0)); }
			lState_33238.speed = 1f;
			lState_33238.mirror = false;
			lState_33238.tag = "Exit";
			lState_33238.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/PistolAnimsetPro/Animations/PistolAnimsetPro.fbx", "Pistol_Idle_Pose");

			UnityEditor.Animations.AnimatorState lState_33240 = MotionControllerMotion.EditorFindState(lSSM_31648, "Equip_Bow");
			if (lState_33240 == null) { lState_33240 = lSSM_31648.AddState("Equip_Bow", new Vector3(300, 288, 0)); }
			lState_33240.speed = 1f;
			lState_33240.mirror = false;
			lState_33240.tag = "";
			lState_33240.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Archery/Content/Animations/Mixamo/standing equip bow.fbx", "standing equip bow");

			UnityEditor.Animations.AnimatorState lState_33242 = MotionControllerMotion.EditorFindState(lSSM_31648, "Disarm_Bow");
			if (lState_33242 == null) { lState_33242 = lSSM_31648.AddState("Disarm_Bow", new Vector3(300, 340, 0)); }
			lState_33242.speed = 1f;
			lState_33242.mirror = false;
			lState_33242.tag = "";
			lState_33242.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Archery/Content/Animations/Mixamo/standing disarm bow.fbx", "standing disarm bow");

			UnityEditor.Animations.AnimatorState lState_33244 = MotionControllerMotion.EditorFindState(lSSM_31648, "EquipBowIdlePoseExit");
			if (lState_33244 == null) { lState_33244 = lSSM_31648.AddState("EquipBowIdlePoseExit", new Vector3(550, 290, 0)); }
			lState_33244.speed = 1f;
			lState_33244.mirror = false;
			lState_33244.tag = "Exit";
			lState_33244.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Archery/Content/Animations/Mixamo/standing idle 01.fbx", "IdlePose");

			UnityEditor.Animations.AnimatorState lState_33246 = MotionControllerMotion.EditorFindState(lSSM_31648, "StoreBowIdlePoseExit");
			if (lState_33246 == null) { lState_33246 = lSSM_31648.AddState("StoreBowIdlePoseExit", new Vector3(550, 340, 0)); }
			lState_33246.speed = 1f;
			lState_33246.mirror = false;
			lState_33246.tag = "Exit";
			lState_33246.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/MovementAnimsetPro/Animations/MovementAnimsetPro.fbx", "Idle_Pose");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33202 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_33224, 0);
			if (lAnyTransition_33202 == null) { lAnyTransition_33202 = lLayerStateMachine.AddAnyStateTransition(lState_33224); }
			lAnyTransition_33202.isExit = false;
			lAnyTransition_33202.hasExitTime = false;
			lAnyTransition_33202.hasFixedDuration = true;
			lAnyTransition_33202.exitTime = 0.03445775f;
			lAnyTransition_33202.duration = 0.1000001f;
			lAnyTransition_33202.offset = 0f;
			lAnyTransition_33202.mute = false;
			lAnyTransition_33202.solo = false;
			lAnyTransition_33202.canTransitionToSelf = true;
			lAnyTransition_33202.orderedInterruption = true;
			lAnyTransition_33202.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_33202.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33202.RemoveCondition(lAnyTransition_33202.conditions[i]); }
			lAnyTransition_33202.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 73150f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33202.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 500f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33204 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_33226, 0);
			if (lAnyTransition_33204 == null) { lAnyTransition_33204 = lLayerStateMachine.AddAnyStateTransition(lState_33226); }
			lAnyTransition_33204.isExit = false;
			lAnyTransition_33204.hasExitTime = false;
			lAnyTransition_33204.hasFixedDuration = true;
			lAnyTransition_33204.exitTime = 0.04618796f;
			lAnyTransition_33204.duration = 0.1000001f;
			lAnyTransition_33204.offset = 0f;
			lAnyTransition_33204.mute = false;
			lAnyTransition_33204.solo = false;
			lAnyTransition_33204.canTransitionToSelf = true;
			lAnyTransition_33204.orderedInterruption = true;
			lAnyTransition_33204.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_33204.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33204.RemoveCondition(lAnyTransition_33204.conditions[i]); }
			lAnyTransition_33204.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 73155f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33204.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 550f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33206 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_33232, 0);
			if (lAnyTransition_33206 == null) { lAnyTransition_33206 = lLayerStateMachine.AddAnyStateTransition(lState_33232); }
			lAnyTransition_33206.isExit = false;
			lAnyTransition_33206.hasExitTime = false;
			lAnyTransition_33206.hasFixedDuration = true;
			lAnyTransition_33206.exitTime = 0.01803551f;
			lAnyTransition_33206.duration = 0.05695996f;
			lAnyTransition_33206.offset = 0f;
			lAnyTransition_33206.mute = false;
			lAnyTransition_33206.solo = false;
			lAnyTransition_33206.canTransitionToSelf = true;
			lAnyTransition_33206.orderedInterruption = true;
			lAnyTransition_33206.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_33206.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33206.RemoveCondition(lAnyTransition_33206.conditions[i]); }
			lAnyTransition_33206.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 73150f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33206.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 550f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33208 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_33234, 0);
			if (lAnyTransition_33208 == null) { lAnyTransition_33208 = lLayerStateMachine.AddAnyStateTransition(lState_33234); }
			lAnyTransition_33208.isExit = false;
			lAnyTransition_33208.hasExitTime = false;
			lAnyTransition_33208.hasFixedDuration = true;
			lAnyTransition_33208.exitTime = 0.009677414f;
			lAnyTransition_33208.duration = 0.1f;
			lAnyTransition_33208.offset = 0.007258054f;
			lAnyTransition_33208.mute = false;
			lAnyTransition_33208.solo = false;
			lAnyTransition_33208.canTransitionToSelf = true;
			lAnyTransition_33208.orderedInterruption = true;
			lAnyTransition_33208.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_33208.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33208.RemoveCondition(lAnyTransition_33208.conditions[i]); }
			lAnyTransition_33208.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 73155f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33208.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 550f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33210 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_33242, 0);
			if (lAnyTransition_33210 == null) { lAnyTransition_33210 = lLayerStateMachine.AddAnyStateTransition(lState_33242); }
			lAnyTransition_33210.isExit = false;
			lAnyTransition_33210.hasExitTime = false;
			lAnyTransition_33210.hasFixedDuration = true;
			lAnyTransition_33210.exitTime = 0.75f;
			lAnyTransition_33210.duration = 0.25f;
			lAnyTransition_33210.offset = 0f;
			lAnyTransition_33210.mute = false;
			lAnyTransition_33210.solo = false;
			lAnyTransition_33210.canTransitionToSelf = true;
			lAnyTransition_33210.orderedInterruption = true;
			lAnyTransition_33210.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_33210.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33210.RemoveCondition(lAnyTransition_33210.conditions[i]); }
			lAnyTransition_33210.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 73155f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33210.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 200f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33212 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_33240, 0);
			if (lAnyTransition_33212 == null) { lAnyTransition_33212 = lLayerStateMachine.AddAnyStateTransition(lState_33240); }
			lAnyTransition_33212.isExit = false;
			lAnyTransition_33212.hasExitTime = false;
			lAnyTransition_33212.hasFixedDuration = true;
			lAnyTransition_33212.exitTime = 0.75f;
			lAnyTransition_33212.duration = 0.25f;
			lAnyTransition_33212.offset = 0f;
			lAnyTransition_33212.mute = false;
			lAnyTransition_33212.solo = false;
			lAnyTransition_33212.canTransitionToSelf = true;
			lAnyTransition_33212.orderedInterruption = true;
			lAnyTransition_33212.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_33212.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33212.RemoveCondition(lAnyTransition_33212.conditions[i]); }
			lAnyTransition_33212.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73150f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33212.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 200f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lTransition_39336 = MotionControllerMotion.EditorFindTransition(lState_33224, lState_33230, 0);
			if (lTransition_39336 == null) { lTransition_39336 = lState_33224.AddTransition(lState_33230); }
			lTransition_39336.isExit = false;
			lTransition_39336.hasExitTime = true;
			lTransition_39336.hasFixedDuration = true;
			lTransition_39336.exitTime = 0.5167463f;
			lTransition_39336.duration = 0.2f;
			lTransition_39336.offset = 0f;
			lTransition_39336.mute = false;
			lTransition_39336.solo = false;
			lTransition_39336.canTransitionToSelf = true;
			lTransition_39336.orderedInterruption = true;
			lTransition_39336.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39336.conditions.Length - 1; i >= 0; i--) { lTransition_39336.RemoveCondition(lTransition_39336.conditions[i]); }

			UnityEditor.Animations.AnimatorStateTransition lTransition_39340 = MotionControllerMotion.EditorFindTransition(lState_33226, lState_33228, 0);
			if (lTransition_39340 == null) { lTransition_39340 = lState_33226.AddTransition(lState_33228); }
			lTransition_39340.isExit = false;
			lTransition_39340.hasExitTime = true;
			lTransition_39340.hasFixedDuration = true;
			lTransition_39340.exitTime = 0.4055317f;
			lTransition_39340.duration = 0.2f;
			lTransition_39340.offset = 0f;
			lTransition_39340.mute = false;
			lTransition_39340.solo = false;
			lTransition_39340.canTransitionToSelf = true;
			lTransition_39340.orderedInterruption = true;
			lTransition_39340.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39340.conditions.Length - 1; i >= 0; i--) { lTransition_39340.RemoveCondition(lTransition_39340.conditions[i]); }

			UnityEditor.Animations.AnimatorStateTransition lTransition_39344 = MotionControllerMotion.EditorFindTransition(lState_33232, lState_33238, 0);
			if (lTransition_39344 == null) { lTransition_39344 = lState_33232.AddTransition(lState_33238); }
			lTransition_39344.isExit = false;
			lTransition_39344.hasExitTime = true;
			lTransition_39344.hasFixedDuration = true;
			lTransition_39344.exitTime = 0.5760845f;
			lTransition_39344.duration = 0.1999999f;
			lTransition_39344.offset = 0f;
			lTransition_39344.mute = false;
			lTransition_39344.solo = false;
			lTransition_39344.canTransitionToSelf = true;
			lTransition_39344.orderedInterruption = true;
			lTransition_39344.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39344.conditions.Length - 1; i >= 0; i--) { lTransition_39344.RemoveCondition(lTransition_39344.conditions[i]); }

			UnityEditor.Animations.AnimatorStateTransition lTransition_39348 = MotionControllerMotion.EditorFindTransition(lState_33234, lState_33236, 0);
			if (lTransition_39348 == null) { lTransition_39348 = lState_33234.AddTransition(lState_33236); }
			lTransition_39348.isExit = false;
			lTransition_39348.hasExitTime = true;
			lTransition_39348.hasFixedDuration = true;
			lTransition_39348.exitTime = 0.6415698f;
			lTransition_39348.duration = 0.2f;
			lTransition_39348.offset = 0f;
			lTransition_39348.mute = false;
			lTransition_39348.solo = false;
			lTransition_39348.canTransitionToSelf = true;
			lTransition_39348.orderedInterruption = true;
			lTransition_39348.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39348.conditions.Length - 1; i >= 0; i--) { lTransition_39348.RemoveCondition(lTransition_39348.conditions[i]); }

			UnityEditor.Animations.AnimatorStateTransition lTransition_39352 = MotionControllerMotion.EditorFindTransition(lState_33240, lState_33244, 0);
			if (lTransition_39352 == null) { lTransition_39352 = lState_33240.AddTransition(lState_33244); }
			lTransition_39352.isExit = false;
			lTransition_39352.hasExitTime = true;
			lTransition_39352.hasFixedDuration = true;
			lTransition_39352.exitTime = 0.9f;
			lTransition_39352.duration = 0.1f;
			lTransition_39352.offset = 0f;
			lTransition_39352.mute = false;
			lTransition_39352.solo = false;
			lTransition_39352.canTransitionToSelf = true;
			lTransition_39352.orderedInterruption = true;
			lTransition_39352.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39352.conditions.Length - 1; i >= 0; i--) { lTransition_39352.RemoveCondition(lTransition_39352.conditions[i]); }

			UnityEditor.Animations.AnimatorStateTransition lTransition_39354 = MotionControllerMotion.EditorFindTransition(lState_33242, lState_33246, 0);
			if (lTransition_39354 == null) { lTransition_39354 = lState_33242.AddTransition(lState_33246); }
			lTransition_39354.isExit = false;
			lTransition_39354.hasExitTime = true;
			lTransition_39354.hasFixedDuration = true;
			lTransition_39354.exitTime = 0.9f;
			lTransition_39354.duration = 0.1f;
			lTransition_39354.offset = 0f;
			lTransition_39354.mute = false;
			lTransition_39354.solo = false;
			lTransition_39354.canTransitionToSelf = true;
			lTransition_39354.orderedInterruption = true;
			lTransition_39354.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_39354.conditions.Length - 1; i >= 0; i--) { lTransition_39354.RemoveCondition(lTransition_39354.conditions[i]); }

		}

		#endregion Definition

	}
}
using com.ootii.Actors;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Combat;
using com.ootii.Actors.Inventory;
using com.ootii.Actors.LifeCores;
using com.ootii.Data.Serializers;
using com.ootii.Geometry;
using com.ootii.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WildWalrus.Actors.AnimationControllers
{
	[MotionName("Rifle AP Basic Item Equip")]
	[MotionDescription("Kubold's RIfle Anim Set Pro. Equip the item based on the specified animation style.")]
	public class RifleAP_BasicItemEquip : MotionControllerMotion, IEquipMotion
	{
		public static string EVENT_EQUIP = "equip";


		#region MotionPhases

		public int PHASE_UNKNOWN = 0;
		public int PHASE_START = 73150;

		#endregion MotionPhases


		#region MotionProperties

		public override bool VerifyTransition
		{
			get { return false; }
		}

		#endregion MotionProperties


		#region IEquipMotion
		
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

		#endregion IEquipMotion


		#region Properties
		
		public string _SlotID = "RIGHT_HAND";
		public string SlotID
		{
			get { return _SlotID; }
			set { _SlotID = value; }
		}
		
		
		public string _ItemID = "Sword_01";
		public string ItemID
		{
			get { return _ItemID; }
			set { _ItemID = value; }
		}
		
		
		public string _ResourcePath = "";
		public string ResourcePath
		{
			get { return _ResourcePath; }
			set { _ResourcePath = value; }
		}
		
		
		public bool _AddCombatantBodyShape = true;
		public bool AddCombatantBodyShape
		{
			get { return _AddCombatantBodyShape; }
			set { _AddCombatantBodyShape = value; }
		}
		
		
		public float _CombatantBodyShapeRadius = 0.8f;
		public float CombatantBodyShapeRadius
		{
			get { return _CombatantBodyShapeRadius; }
			set { _CombatantBodyShapeRadius = value; }
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

		#endregion Members


		#region Constructors
		
		public RifleAP_BasicItemEquip() : base()
		{
			_Pack = BasicIdle.GroupName();
			_Category = EnumMotionCategories.UNKNOWN;
			
			_Priority = 20f;
			_ActionAlias = "";
			
#if UNITY_EDITOR
			if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "RifleAP_BasicEquipStore-SM"; }
#endif
		}
		
		
		public RifleAP_BasicItemEquip(MotionController rController) : base(rController)
		{
			_Pack = BasicIdle.GroupName();
			_Category = EnumMotionCategories.UNKNOWN;
			
			_Priority = 20f;
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
			
			// Test if we should activate
			if (_ActionAlias.Length > 0 && mMotionController._InputSource.IsJustPressed(_ActionAlias))
			{
				return true;
			}
			
			return false;
		}
		
		
		public override bool TestUpdate()
		{
			if (mIsActivatedFrame) { return true; }
			
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
				if (!mIsEquipped)
				{
					GameObject lItem = CreateItem();
					if (lItem != null)
					{
						mIsEquipped = true;
					}
				}
			}
			
			return base.TestInterruption(rMotion);
		}

		#endregion Tests
		
		
		public override bool Activate(MotionControllerMotion rPrevMotion)
		{
			mIsEquipped = false;
			
			// If we already have equipment in hand, we don't need to run this motion
			string lItemID = (_OverrideItemID != null && _OverrideItemID.Length > 0 ? _OverrideItemID : _ItemID);
			string lSlotID = (_OverrideSlotID != null && _OverrideSlotID.Length > 0 ? _OverrideSlotID : _SlotID);
			
			if (mInventorySource != null)
			{
				string lEquippedItemID = mInventorySource.GetItemID(lSlotID);
				if (lEquippedItemID != null && lEquippedItemID.Length > 0)
				{
					if (lItemID != null && lItemID.Length > 0 && lItemID == lEquippedItemID)
					{
						return false;
					}
				}
			}
			
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
			
			if (rEvent.stringParameter.Length == 0 || StringHelper.CleanString(rEvent.stringParameter) == EVENT_EQUIP)
			{
				if (!mIsEquipped)
				{
					GameObject lItem = CreateItem();
					if (lItem != null)
					{
						mIsEquipped = true;
					}
				}
			}
		}

		#endregion MotionFunctions


		#region Functions
		
		protected virtual GameObject CreateItem()
		{
			string lResourcePath = "";
			
			string lItemID = "";
			if (OverrideItemID != null && OverrideItemID.Length > 0)
			{
				lItemID = OverrideItemID;
			}
			else if (ResourcePath.Length > 0)
			{
				lResourcePath = ResourcePath;
			}
			else if (ItemID.Length > 0)
			{
				lItemID = ItemID;
			}
			
			string lSlotID = "";
			if (OverrideSlotID != null && OverrideSlotID.Length > 0)
			{
				lSlotID = OverrideSlotID;
			}
			else
			{
				lSlotID = SlotID;
			}
			
			ICombatant lCombatant = mMotionController.gameObject.GetComponent<ICombatant>();
			
			GameObject lItem = null;
			if (mInventorySource != null)
			{
				lItem = mInventorySource.EquipItem(lItemID, lSlotID, lResourcePath);
			}
			else
			{
				lItem = EquipItem(lItemID, lSlotID, lResourcePath);
			}
			
			if (lCombatant != null)
			{
				try
				{
					lCombatant.PrimaryWeapon = lItem.GetComponent<IWeaponCore>();
					if (lCombatant.PrimaryWeapon != null)
					{
						lCombatant.PrimaryWeapon.Owner = mMotionController.gameObject;
					}
				}
				catch { }
				
				// Add another body shape in order to compensate for the pose
				if (_AddCombatantBodyShape)
				{
					BodyCapsule lShape = new BodyCapsule();
					lShape.Name = "Combatant Shape";
					lShape.Radius = _CombatantBodyShapeRadius;
					lShape.Offset = new Vector3(0f, 1.0f, 0f);
					lShape.EndOffset = new Vector3(0f, 1.2f, 0f);
					lShape.IsEnabledOnGround = true;
					lShape.IsEnabledOnSlope = true;
					lShape.IsEnabledAboveGround = true;
					mActorController.AddBodyShape(lShape);
				}
			}
			
			return lItem;
		}
		
		
		public virtual GameObject EquipItem(string rItemID, string rSlotID, string rResourcePath = "")
		{
			string lResourcePath = rResourcePath;
			
			Vector3 lLocalPosition = Vector3.zero;
			Quaternion lLocalRotation = Quaternion.identity;
			
			GameObject lGameObject = CreateAndMountItem(mMotionController.gameObject, lResourcePath, lLocalPosition, lLocalRotation, rSlotID);
			
			if (lGameObject != null)
			{
				IItemCore lItemCore = lGameObject.GetComponent<IItemCore>();
				if (lItemCore != null) { lItemCore.OnEquipped(); }
			}
			
			return lGameObject;
		}
		
		
		protected virtual GameObject CreateAndMountItem(GameObject rParent,
			string rResourcePath, Vector3 rLocalPosition, Quaternion rLocalRotation,
			string rParentMountPoint = "Left Hand", string rItemMountPoint = "Handle")
		{
			GameObject lItem = null;
			
			if (rResourcePath.Length > 0)
			{
				// Create and mount if we need to
				Animator lAnimator = rParent.GetComponentInChildren<Animator>();
				if (lAnimator != null)
				{
					UnityEngine.Object lResource = Resources.Load(rResourcePath);
					if (lResource != null)
					{
						lItem = GameObject.Instantiate(lResource) as GameObject;
						MountItem(rParent, lItem, rLocalPosition, rLocalRotation, rParentMountPoint);
					}
					else
					{
						Debug.LogWarning("Resource not found. Resource Path: " + rResourcePath);
					}
				}
			}
			
			return lItem;
		}
		
		
		protected virtual void MountItem(GameObject rParent, GameObject rItem,
			Vector3 rLocalPosition, Quaternion rLocalRotation,
			string rParentMountPoint, string rItemMountPoint = "Handle")
		{
			if (rParent == null || rItem == null) { return; }
			
			//bool lIsConnected = false;
			
			Transform lParentBone = FindTransform(rParent.transform, rParentMountPoint);
			rItem.transform.parent = lParentBone;
			
			//IItemCore lItemCore = InterfaceHelper.GetComponent<IItemCore>(rItem);
			IItemCore lItemCore = rItem.GetComponent<IItemCore>();
			if (lItemCore != null)
			{
				lItemCore.Owner = mMotionController.gameObject;
				
				if (rLocalPosition.sqrMagnitude == 0f && QuaternionExt.IsIdentity(rLocalRotation))
				{
					rItem.transform.localPosition = (lItemCore.LocalPosition);
					rItem.transform.localRotation = (lItemCore.LocalRotation);
				}
				else
				{
					rItem.transform.localPosition = rLocalPosition;
					rItem.transform.localRotation = rLocalRotation;
				}
			}
			else
			{
				rItem.transform.localPosition = rLocalPosition;
				rItem.transform.localRotation = rLocalRotation;
			}
			
			// Reenable the item as needed
			rItem.SetActive(true);
			rItem.hideFlags = HideFlags.None;
			
			// Inform the combatant of the change
			ICombatant lCombatant = mMotionController.gameObject.GetComponent<ICombatant>();
			if (lCombatant != null)
			{
				IWeaponCore lWeaponCore = rItem.GetComponent<IWeaponCore>();
				if (lWeaponCore != null)
				{
					string lCleanParentMountPoint = StringHelper.CleanString(rParentMountPoint);
					if (lCleanParentMountPoint == "righthand")
					{
						lCombatant.PrimaryWeapon = lWeaponCore;
					}
					else if (lCleanParentMountPoint == "lefthand" || lCleanParentMountPoint == "leftlowerarm")
					{
						lCombatant.SecondaryWeapon = lWeaponCore;
					}
				}
			}
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
				"Action alias that is used to trigger the equipping of the item.",
				ActionAlias, mMotionController))
			{
				lIsDirty = true;
				ActionAlias = EditorHelper.FieldStringValue;
			}
			
			GUILayout.Space(5f);
			
			if (EditorHelper.TextField("Slot ID",
				"Slot ID of the slot the item should be held in.",
				SlotID, mMotionController))
			{
				lIsDirty = true;
				SlotID = EditorHelper.FieldStringValue;
			}
			
			if (EditorHelper.TextField("Item ID",
				"Item ID that defines the item that will be equipped.",
				ItemID, mMotionController))
			{
				lIsDirty = true;
				ItemID = EditorHelper.FieldStringValue;
			}
			
			string lNewResourcePath = EditorHelper.FileSelect(new GUIContent("Resource Path",
				"Override path to the prefab resource that is the item."),
				ResourcePath, "fbx,prefab");
			if (lNewResourcePath != ResourcePath)
			{
				lIsDirty = true;
				ResourcePath = lNewResourcePath;
			}
			
			GUILayout.Space(5f);
			
			if (EditorHelper.BoolField("Add Body Shape",
				"Determines if we'll add an extra body shape to account for the stance.",
				AddCombatantBodyShape, mMotionController))
			{
				lIsDirty = true;
				AddCombatantBodyShape = EditorHelper.FieldBoolValue;
			}
			
			if (AddCombatantBodyShape)
			{
				if (EditorHelper.FloatField("Body Shape Radius",
					"Radius to make the body shape.",
					CombatantBodyShapeRadius, mMotionController))
				{
					lIsDirty = true;
					CombatantBodyShapeRadius = EditorHelper.FieldFloatValue;
				}
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

			UnityEditor.Animations.AnimatorStateMachine lSSM_32520 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "RifleAP_BasicEquipStore-SM");
			if (lSSM_32520 == null) { lSSM_32520 = lLayerStateMachine.AddStateMachine("RifleAP_BasicEquipStore-SM", new Vector3(240, -990, 0)); }

			UnityEditor.Animations.AnimatorState lState_32630 = MotionControllerMotion.EditorFindState(lSSM_32520, "Equip Rifle");
			if (lState_32630 == null) { lState_32630 = lSSM_32520.AddState("Equip Rifle", new Vector3(300, 520, 0)); }
			lState_32630.speed = 1f;
			lState_32630.mirror = false;
			lState_32630.tag = "";
			lState_32630.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/RifleAnimsetPro/Animations/EquipRifle.anim", "EquipRifle");

			UnityEditor.Animations.AnimatorState lState_32632 = MotionControllerMotion.EditorFindState(lSSM_32520, "Store Rifle");
			if (lState_32632 == null) { lState_32632 = lSSM_32520.AddState("Store Rifle", new Vector3(300, 580, 0)); }
			lState_32632.speed = 1f;
			lState_32632.mirror = false;
			lState_32632.tag = "";
			lState_32632.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/RifleAnimsetPro/Animations/HolsterRifle.anim", "HolsterRifle");

			UnityEditor.Animations.AnimatorState lState_32802 = MotionControllerMotion.EditorFindState(lSSM_32520, "Store Rifle Idle Pose Exit");
			if (lState_32802 == null) { lState_32802 = lSSM_32520.AddState("Store Rifle Idle Pose Exit", new Vector3(552, 576, 0)); }
			lState_32802.speed = 1f;
			lState_32802.mirror = false;
			lState_32802.tag = "Exit";
			lState_32802.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/MovementAnimsetPro/Animations/MovementAnimsetPro.fbx", "Idle_Pose");

			UnityEditor.Animations.AnimatorState lState_32804 = MotionControllerMotion.EditorFindState(lSSM_32520, "Equip Rifle Idle Pose Exit");
			if (lState_32804 == null) { lState_32804 = lSSM_32520.AddState("Equip Rifle Idle Pose Exit", new Vector3(552, 528, 0)); }
			lState_32804.speed = 1f;
			lState_32804.mirror = false;
			lState_32804.tag = "Exit";
			lState_32804.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_Idle_Pose");

			UnityEditor.Animations.AnimatorState lState_32634 = MotionControllerMotion.EditorFindState(lSSM_32520, "Equip Pistol");
			if (lState_32634 == null) { lState_32634 = lSSM_32520.AddState("Equip Pistol", new Vector3(300, 648, 0)); }
			lState_32634.speed = 1f;
			lState_32634.mirror = false;
			lState_32634.tag = "";
			lState_32634.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/PistolAnimsetPro/Animations/Pistol_Equip.anim", "Pistol_Equip");

			UnityEditor.Animations.AnimatorState lState_32636 = MotionControllerMotion.EditorFindState(lSSM_32520, "Store Pistol");
			if (lState_32636 == null) { lState_32636 = lSSM_32520.AddState("Store Pistol", new Vector3(300, 696, 0)); }
			lState_32636.speed = 1f;
			lState_32636.mirror = false;
			lState_32636.tag = "";
			lState_32636.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/PistolAnimsetPro/Animations/Pistol_UnEquip.anim", "Pistol_UnEquip");

			UnityEditor.Animations.AnimatorState lState_32806 = MotionControllerMotion.EditorFindState(lSSM_32520, "Store Pistol Idle Pose Exit");
			if (lState_32806 == null) { lState_32806 = lSSM_32520.AddState("Store Pistol Idle Pose Exit", new Vector3(552, 696, 0)); }
			lState_32806.speed = 1f;
			lState_32806.mirror = false;
			lState_32806.tag = "Exit";
			lState_32806.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/MovementAnimsetPro/Animations/MovementAnimsetPro.fbx", "Idle_Pose");

			UnityEditor.Animations.AnimatorState lState_32808 = MotionControllerMotion.EditorFindState(lSSM_32520, "Equip Pistol Idle Pose Exit");
			if (lState_32808 == null) { lState_32808 = lSSM_32520.AddState("Equip Pistol Idle Pose Exit", new Vector3(552, 648, 0)); }
			lState_32808.speed = 1f;
			lState_32808.mirror = false;
			lState_32808.tag = "Exit";
			lState_32808.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/PistolAnimsetPro/Animations/PistolAnimsetPro.fbx", "Pistol_Idle_Pose");

			UnityEditor.Animations.AnimatorState lState_32640 = MotionControllerMotion.EditorFindState(lSSM_32520, "Equip_Bow");
			if (lState_32640 == null) { lState_32640 = lSSM_32520.AddState("Equip_Bow", new Vector3(300, 288, 0)); }
			lState_32640.speed = 1f;
			lState_32640.mirror = false;
			lState_32640.tag = "";
			lState_32640.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Archery/Content/Animations/Mixamo/standing equip bow.fbx", "standing equip bow");

			UnityEditor.Animations.AnimatorState lState_32638 = MotionControllerMotion.EditorFindState(lSSM_32520, "Disarm_Bow");
			if (lState_32638 == null) { lState_32638 = lSSM_32520.AddState("Disarm_Bow", new Vector3(300, 340, 0)); }
			lState_32638.speed = 1f;
			lState_32638.mirror = false;
			lState_32638.tag = "";
			lState_32638.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Archery/Content/Animations/Mixamo/standing disarm bow.fbx", "standing disarm bow");

			UnityEditor.Animations.AnimatorState lState_32810 = MotionControllerMotion.EditorFindState(lSSM_32520, "EquipBowIdlePoseExit");
			if (lState_32810 == null) { lState_32810 = lSSM_32520.AddState("EquipBowIdlePoseExit", new Vector3(550, 290, 0)); }
			lState_32810.speed = 1f;
			lState_32810.mirror = false;
			lState_32810.tag = "Exit";
			lState_32810.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Archery/Content/Animations/Mixamo/standing idle 01.fbx", "IdlePose");

			UnityEditor.Animations.AnimatorState lState_32812 = MotionControllerMotion.EditorFindState(lSSM_32520, "StoreBowIdlePoseExit");
			if (lState_32812 == null) { lState_32812 = lSSM_32520.AddState("StoreBowIdlePoseExit", new Vector3(550, 340, 0)); }
			lState_32812.speed = 1f;
			lState_32812.mirror = false;
			lState_32812.tag = "Exit";
			lState_32812.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/MovementAnimsetPro/Animations/MovementAnimsetPro.fbx", "Idle_Pose");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_32566 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_32630, 0);
			if (lAnyTransition_32566 == null) { lAnyTransition_32566 = lLayerStateMachine.AddAnyStateTransition(lState_32630); }
			lAnyTransition_32566.isExit = false;
			lAnyTransition_32566.hasExitTime = false;
			lAnyTransition_32566.hasFixedDuration = true;
			lAnyTransition_32566.exitTime = 0.03445775f;
			lAnyTransition_32566.duration = 0.1000001f;
			lAnyTransition_32566.offset = 0f;
			lAnyTransition_32566.mute = false;
			lAnyTransition_32566.solo = false;
			lAnyTransition_32566.canTransitionToSelf = true;
			lAnyTransition_32566.orderedInterruption = true;
			lAnyTransition_32566.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_32566.conditions.Length - 1; i >= 0; i--) { lAnyTransition_32566.RemoveCondition(lAnyTransition_32566.conditions[i]); }
			lAnyTransition_32566.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73150f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_32566.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 500f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_32568 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_32632, 0);
			if (lAnyTransition_32568 == null) { lAnyTransition_32568 = lLayerStateMachine.AddAnyStateTransition(lState_32632); }
			lAnyTransition_32568.isExit = false;
			lAnyTransition_32568.hasExitTime = false;
			lAnyTransition_32568.hasFixedDuration = true;
			lAnyTransition_32568.exitTime = 0.04618796f;
			lAnyTransition_32568.duration = 0.1000001f;
			lAnyTransition_32568.offset = 0f;
			lAnyTransition_32568.mute = false;
			lAnyTransition_32568.solo = false;
			lAnyTransition_32568.canTransitionToSelf = true;
			lAnyTransition_32568.orderedInterruption = true;
			lAnyTransition_32568.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_32568.conditions.Length - 1; i >= 0; i--) { lAnyTransition_32568.RemoveCondition(lAnyTransition_32568.conditions[i]); }
			lAnyTransition_32568.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73155f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_32568.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 500f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_32570 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_32634, 0);
			if (lAnyTransition_32570 == null) { lAnyTransition_32570 = lLayerStateMachine.AddAnyStateTransition(lState_32634); }
			lAnyTransition_32570.isExit = false;
			lAnyTransition_32570.hasExitTime = false;
			lAnyTransition_32570.hasFixedDuration = true;
			lAnyTransition_32570.exitTime = 0.01803551f;
			lAnyTransition_32570.duration = 0.002518231f;
			lAnyTransition_32570.offset = 0f;
			lAnyTransition_32570.mute = false;
			lAnyTransition_32570.solo = false;
			lAnyTransition_32570.canTransitionToSelf = true;
			lAnyTransition_32570.orderedInterruption = true;
			lAnyTransition_32570.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_32570.conditions.Length - 1; i >= 0; i--) { lAnyTransition_32570.RemoveCondition(lAnyTransition_32570.conditions[i]); }
			lAnyTransition_32570.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73150f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_32570.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 550f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_32572 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_32636, 0);
			if (lAnyTransition_32572 == null) { lAnyTransition_32572 = lLayerStateMachine.AddAnyStateTransition(lState_32636); }
			lAnyTransition_32572.isExit = false;
			lAnyTransition_32572.hasExitTime = false;
			lAnyTransition_32572.hasFixedDuration = true;
			lAnyTransition_32572.exitTime = 0.009677414f;
			lAnyTransition_32572.duration = 0.1f;
			lAnyTransition_32572.offset = 0.007258054f;
			lAnyTransition_32572.mute = false;
			lAnyTransition_32572.solo = false;
			lAnyTransition_32572.canTransitionToSelf = true;
			lAnyTransition_32572.orderedInterruption = true;
			lAnyTransition_32572.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_32572.conditions.Length - 1; i >= 0; i--) { lAnyTransition_32572.RemoveCondition(lAnyTransition_32572.conditions[i]); }
			lAnyTransition_32572.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73155f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_32572.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 550f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_32574 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_32638, 0);
			if (lAnyTransition_32574 == null) { lAnyTransition_32574 = lLayerStateMachine.AddAnyStateTransition(lState_32638); }
			lAnyTransition_32574.isExit = false;
			lAnyTransition_32574.hasExitTime = false;
			lAnyTransition_32574.hasFixedDuration = true;
			lAnyTransition_32574.exitTime = 0.75f;
			lAnyTransition_32574.duration = 0.25f;
			lAnyTransition_32574.offset = 0f;
			lAnyTransition_32574.mute = false;
			lAnyTransition_32574.solo = false;
			lAnyTransition_32574.canTransitionToSelf = true;
			lAnyTransition_32574.orderedInterruption = true;
			lAnyTransition_32574.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_32574.conditions.Length - 1; i >= 0; i--) { lAnyTransition_32574.RemoveCondition(lAnyTransition_32574.conditions[i]); }
			lAnyTransition_32574.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73155f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_32574.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 200f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_32576 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_32640, 0);
			if (lAnyTransition_32576 == null) { lAnyTransition_32576 = lLayerStateMachine.AddAnyStateTransition(lState_32640); }
			lAnyTransition_32576.isExit = false;
			lAnyTransition_32576.hasExitTime = false;
			lAnyTransition_32576.hasFixedDuration = true;
			lAnyTransition_32576.exitTime = 0.75f;
			lAnyTransition_32576.duration = 0.25f;
			lAnyTransition_32576.offset = 0f;
			lAnyTransition_32576.mute = false;
			lAnyTransition_32576.solo = false;
			lAnyTransition_32576.canTransitionToSelf = true;
			lAnyTransition_32576.orderedInterruption = true;
			lAnyTransition_32576.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_32576.conditions.Length - 1; i >= 0; i--) { lAnyTransition_32576.RemoveCondition(lAnyTransition_32576.conditions[i]); }
			lAnyTransition_32576.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73150f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_32576.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 200f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lTransition_32814 = MotionControllerMotion.EditorFindTransition(lState_32630, lState_32804, 0);
			if (lTransition_32814 == null) { lTransition_32814 = lState_32630.AddTransition(lState_32804); }
			lTransition_32814.isExit = false;
			lTransition_32814.hasExitTime = true;
			lTransition_32814.hasFixedDuration = true;
			lTransition_32814.exitTime = 0.5167463f;
			lTransition_32814.duration = 0.2f;
			lTransition_32814.offset = 0f;
			lTransition_32814.mute = false;
			lTransition_32814.solo = false;
			lTransition_32814.canTransitionToSelf = true;
			lTransition_32814.orderedInterruption = true;
			lTransition_32814.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_32814.conditions.Length - 1; i >= 0; i--) { lTransition_32814.RemoveCondition(lTransition_32814.conditions[i]); }

			UnityEditor.Animations.AnimatorStateTransition lTransition_32816 = MotionControllerMotion.EditorFindTransition(lState_32632, lState_32802, 0);
			if (lTransition_32816 == null) { lTransition_32816 = lState_32632.AddTransition(lState_32802); }
			lTransition_32816.isExit = false;
			lTransition_32816.hasExitTime = true;
			lTransition_32816.hasFixedDuration = true;
			lTransition_32816.exitTime = 0.7877609f;
			lTransition_32816.duration = 0.1999999f;
			lTransition_32816.offset = 10.32021f;
			lTransition_32816.mute = false;
			lTransition_32816.solo = false;
			lTransition_32816.canTransitionToSelf = true;
			lTransition_32816.orderedInterruption = true;
			lTransition_32816.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_32816.conditions.Length - 1; i >= 0; i--) { lTransition_32816.RemoveCondition(lTransition_32816.conditions[i]); }

			UnityEditor.Animations.AnimatorStateTransition lTransition_32818 = MotionControllerMotion.EditorFindTransition(lState_32634, lState_32808, 0);
			if (lTransition_32818 == null) { lTransition_32818 = lState_32634.AddTransition(lState_32808); }
			lTransition_32818.isExit = false;
			lTransition_32818.hasExitTime = true;
			lTransition_32818.hasFixedDuration = true;
			lTransition_32818.exitTime = 0.5760845f;
			lTransition_32818.duration = 0.1999999f;
			lTransition_32818.offset = 0f;
			lTransition_32818.mute = false;
			lTransition_32818.solo = false;
			lTransition_32818.canTransitionToSelf = true;
			lTransition_32818.orderedInterruption = true;
			lTransition_32818.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_32818.conditions.Length - 1; i >= 0; i--) { lTransition_32818.RemoveCondition(lTransition_32818.conditions[i]); }

			UnityEditor.Animations.AnimatorStateTransition lTransition_32822 = MotionControllerMotion.EditorFindTransition(lState_32636, lState_32806, 0);
			if (lTransition_32822 == null) { lTransition_32822 = lState_32636.AddTransition(lState_32806); }
			lTransition_32822.isExit = false;
			lTransition_32822.hasExitTime = true;
			lTransition_32822.hasFixedDuration = true;
			lTransition_32822.exitTime = 0.6415698f;
			lTransition_32822.duration = 0.2f;
			lTransition_32822.offset = 0f;
			lTransition_32822.mute = false;
			lTransition_32822.solo = false;
			lTransition_32822.canTransitionToSelf = true;
			lTransition_32822.orderedInterruption = true;
			lTransition_32822.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_32822.conditions.Length - 1; i >= 0; i--) { lTransition_32822.RemoveCondition(lTransition_32822.conditions[i]); }

			UnityEditor.Animations.AnimatorStateTransition lTransition_32826 = MotionControllerMotion.EditorFindTransition(lState_32640, lState_32810, 0);
			if (lTransition_32826 == null) { lTransition_32826 = lState_32640.AddTransition(lState_32810); }
			lTransition_32826.isExit = false;
			lTransition_32826.hasExitTime = true;
			lTransition_32826.hasFixedDuration = true;
			lTransition_32826.exitTime = 0.9f;
			lTransition_32826.duration = 0.1f;
			lTransition_32826.offset = 0f;
			lTransition_32826.mute = false;
			lTransition_32826.solo = false;
			lTransition_32826.canTransitionToSelf = true;
			lTransition_32826.orderedInterruption = true;
			lTransition_32826.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_32826.conditions.Length - 1; i >= 0; i--) { lTransition_32826.RemoveCondition(lTransition_32826.conditions[i]); }

			UnityEditor.Animations.AnimatorStateTransition lTransition_32828 = MotionControllerMotion.EditorFindTransition(lState_32638, lState_32812, 0);
			if (lTransition_32828 == null) { lTransition_32828 = lState_32638.AddTransition(lState_32812); }
			lTransition_32828.isExit = false;
			lTransition_32828.hasExitTime = true;
			lTransition_32828.hasFixedDuration = true;
			lTransition_32828.exitTime = 0.9f;
			lTransition_32828.duration = 0.1f;
			lTransition_32828.offset = 0f;
			lTransition_32828.mute = false;
			lTransition_32828.solo = false;
			lTransition_32828.canTransitionToSelf = true;
			lTransition_32828.orderedInterruption = true;
			lTransition_32828.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_32828.conditions.Length - 1; i >= 0; i--) { lTransition_32828.RemoveCondition(lTransition_32828.conditions[i]); }


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
			lAnyTransition_33202.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73150f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33202.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 500f, "L" + rLayerIndex + "MotionForm");

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
			lAnyTransition_33204.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73155f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33204.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 500f, "L" + rLayerIndex + "MotionForm");

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
			lAnyTransition_33206.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73150f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33206.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 550f, "L" + rLayerIndex + "MotionForm");

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
			lAnyTransition_33208.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73155f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33208.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 550f, "L" + rLayerIndex + "MotionForm");

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
			lAnyTransition_33210.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73155f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_33210.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 200f, "L" + rLayerIndex + "MotionForm");

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
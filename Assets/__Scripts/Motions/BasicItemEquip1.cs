using System;
using UnityEngine;
using com.ootii.Actors;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Combat;
using com.ootii.Actors.Inventory;
using com.ootii.Actors.LifeCores;
using com.ootii.Data.Serializers;
using com.ootii.Geometry;
using com.ootii.Helpers;

namespace WildWalrus.Actors.AnimationControllers
{
	[MotionName("Basic Item Equip 1")]
	[MotionDescription("Equip the item based on the specified animation style.")]
	public class BasicItemEquip1 : MotionControllerMotion, IEquipMotion
	{
		/// <summary>
		/// Preallocates string for the event tests
		/// </summary>
		public static string EVENT_EQUIP = "equip";

		/// <summary>
		/// Determines if we're using the IsInMotion() function to verify that
		/// the transition in the animator has occurred for this motion.
		/// </summary>
		public override bool VerifyTransition
		{
			get { return false; }
		}

		/// <summary>
		/// Trigger values for th emotion
		/// </summary>
		public int PHASE_UNKNOWN = 0;
		public int PHASE_START = 73150;

		/// <summary>
		/// Slot ID of the 'left hand' that the sword will be held in
		/// </summary>
		public string _SlotID = "RIGHT_HAND";
		public string SlotID
		{
			get { return _SlotID; }
			set { _SlotID = value; }
		}

		/// <summary>
		/// Item ID in the inventory to load
		/// </summary>
		public string _ItemID = "Sword_01";
		public string ItemID
		{
			get { return _ItemID; }
			set { _ItemID = value; }
		}

		/// <summary>
		/// Resource path to the sword that we'll instanciated
		/// </summary>
		public string _ResourcePath = "";
		public string ResourcePath
		{
			get { return _ResourcePath; }
			set { _ResourcePath = value; }
		}

		/// <summary>
		/// Determines if we'll add a weapon body shape to ensure 
		/// combatants don't get too close
		/// </summary>
		public bool _AddCombatantBodyShape = true;
		public bool AddCombatantBodyShape
		{
			get { return _AddCombatantBodyShape; }
			set { _AddCombatantBodyShape = value; }
		}

		/// <summary>
		/// Radius of the weapon body shape
		/// </summary>
		public float _CombatantBodyShapeRadius = 0.8f;
		public float CombatantBodyShapeRadius
		{
			get { return _CombatantBodyShapeRadius; }
			set { _CombatantBodyShapeRadius = value; }
		}

		/// <summary>
		/// Slot ID that is will hold the item. 
		/// This overrides any properties for one activation.
		/// </summary>
		[NonSerialized]
		public string _OverrideSlotID = null;

		[SerializationIgnore]
		public string OverrideSlotID
		{
			get { return _OverrideSlotID; }
			set { _OverrideSlotID = value; }
		}

		/// <summary>
		/// Item ID that is going to be unsheathed. 
		/// This overrides any properties for one activation.
		/// </summary>
		[NonSerialized]
		public string _OverrideItemID = null;

		[SerializationIgnore]
		public string OverrideItemID
		{
			get { return _OverrideItemID; }
			set { _OverrideItemID = value; }
		}

		/// <summary>
		/// Defines the source of our inventory items.
		/// </summary>
		[NonSerialized]
		protected IInventorySource mInventorySource = null;
		public IInventorySource InventorySource
		{
			get { return mInventorySource; }
			set { mInventorySource = value; }
		}

		/// <summary>
		/// Determines if the weapon is currently equipped
		/// </summary>
		protected bool mIsEquipped = false;
		public bool IsEquipped
		{
			get { return mIsEquipped; }
			set { mIsEquipped = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public BasicItemEquip1()
			 : base()
		{
			_Pack = BasicIdle.GroupName();
			_Category = EnumMotionCategories.UNKNOWN;

			_Priority = 20f;
			_ActionAlias = "";

#if UNITY_EDITOR
			if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "RifleAP_BasicEquipStore-SM"; }
#endif
		}

		/// <summary>
		/// Controller constructor
		/// </summary>
		/// <param name="rController">Controller the motion belongs to</param>
		public BasicItemEquip1(MotionController rController)
			 : base(rController)
		{
			_Pack = BasicIdle.GroupName();
			_Category = EnumMotionCategories.UNKNOWN;

			_Priority = 20f;
			_ActionAlias = "";

#if UNITY_EDITOR
			if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "RifleAP_BasicEquipStore-SM"; }
#endif
		}

		/// <summary>
		/// Awake is called after all objects are initialized so you can safely speak to other objects. This is where
		/// reference can be associated.
		/// </summary>
		public override void Awake()
		{
			base.Awake();

			// If the input source is still null, see if we can grab a local input source
			if (mInventorySource == null && mMotionController != null)
			{
				mInventorySource = mMotionController.gameObject.GetComponent<IInventorySource>();
			}
		}

		/// <summary>
		/// Tests if this motion should be started. However, the motion
		/// isn't actually started.
		/// </summary>
		/// <returns></returns>
		public override bool TestActivate()
		{
			if (!mIsStartable) { return false; }
			if (!mActorController.IsGrounded) { return false; }
			if (mMotionController._InputSource == null) { return false; }
			if (mMotionLayer._AnimatorTransitionID != 0) { return false; }
			//if (mInventorySource == null) { return false; }

			// Since we're using BasicInventory, it can 
			if (mInventorySource != null && !mInventorySource.AllowMotionSelfActivation) { return false; }

			// Test if we should activate
			if (_ActionAlias.Length > 0 && mMotionController._InputSource.IsJustPressed(_ActionAlias))
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Tests if the motion should continue. If it shouldn't, the motion
		/// is typically disabled
		/// </summary>
		/// <returns></returns>
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

		/// <summary>
		/// Raised when a motion is being interrupted by another motion
		/// </summary>
		/// <param name="rMotion">Motion doing the interruption</param>
		/// <returns>Boolean determining if it can be interrupted</returns>
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

		/// <summary>
		/// Called to start the specific motion. If the motion
		/// were something like 'jump', this would start the jumping process
		/// </summary>
		/// <param name="rPrevMotion">Motion that this motion is taking over from</param>
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

		/// <summary>
		/// Raised when we shut the motion down
		/// </summary>
		public override void Deactivate()
		{
			// Clear for the next activation
			_OverrideSlotID = "";
			_OverrideItemID = "";

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
		}

		/// <summary>
		/// Raised by the animation when an event occurs
		/// </summary>
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

		/// <summary>
		/// Create the item to unsheathe
		/// </summary>
		/// <returns></returns>
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

		/// <summary>
		/// Instantiates the specified item and equips it. We return the instantiated item.
		/// </summary>
		/// <param name="rItemID">String representing the name or ID of the item to equip</param>
		/// <param name="rSlotID">String representing the name or ID of the slot to equip</param>
		/// <param name="rResourcePath">Alternate resource path to override the ItemID's</param>
		/// <returns>GameObject that is the instance or null if it could not be created</returns>
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

		/// <summary>
		/// Creates the item and attaches it to the parent mount point
		/// </summary>
		/// <param name="rParent">GameObject that is the parent (typically a character)</param>
		/// <param name="rResourcePath">String that is the resource path to the item</param>
		/// <param name="rLocalPosition">Position the item will have relative to the parent mount point</param>
		/// <param name="rLocalRotation">Rotation the item will have relative to the parent mount pont</param>
		/// <returns></returns>
		protected virtual GameObject CreateAndMountItem(GameObject rParent, string rResourcePath, Vector3 rLocalPosition, Quaternion rLocalRotation, string rParentMountPoint = "Left Hand", string rItemMountPoint = "Handle")
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

		/// <summary>
		/// Mounts the item to the specified position based on the ItemCore
		/// </summary>
		/// <param name="rParent">Parent GameObject</param>
		/// <param name="rItem">Child GameObject that is this item</param>
		/// <param name="rLocalPosition">Vector3 that is the local position to set when the item is parented.</param>
		/// <param name="rLocalRotation">Quaternion that is the local rotation to set when the item is parented.</param>
		/// <param name="rParentMountPoint">Name of the parent mount point we're tying the item to</param>
		/// <param name="rItemMountPoint">Name of the child mount point we're tying the item to</param>
		protected virtual void MountItem(GameObject rParent, GameObject rItem, Vector3 rLocalPosition, Quaternion rLocalRotation, string rParentMountPoint, string rItemMountPoint = "Handle")
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

		/// <summary>
		/// Attempts to find a matching transform
		/// </summary>
		/// <param name="rParent">Parent transform where we'll start the search</param>
		/// <param name="rName">Name or identifier of the transform we want</param>
		/// <returns>Transform matching the name or the parent if not found</returns>
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

#if UNITY_EDITOR

		/// <summary>
		/// Allow the constraint to render it's own GUI
		/// </summary>
		/// <returns>Reports if the object's value was changed</returns>
		public override bool OnInspectorGUI()
		{
			bool lIsDirty = false;

			if (EditorHelper.IntField("Form", "Within the animator state, defines which animator flow will run. This is used to control animations.", Form, mMotionController))
			{
				lIsDirty = true;
				Form = EditorHelper.FieldIntValue;
			}

			if (EditorHelper.TextField("Action Alias", "Action alias that is used to trigger the equipping of the item.", ActionAlias, mMotionController))
			{
				lIsDirty = true;
				ActionAlias = EditorHelper.FieldStringValue;
			}

			GUILayout.Space(5f);

			if (EditorHelper.TextField("Slot ID", "Slot ID of the slot the item should be held in.", SlotID, mMotionController))
			{
				lIsDirty = true;
				SlotID = EditorHelper.FieldStringValue;
			}

			if (EditorHelper.TextField("Item ID", "Item ID that defines the item that will be equipped.", ItemID, mMotionController))
			{
				lIsDirty = true;
				ItemID = EditorHelper.FieldStringValue;
			}

			string lNewResourcePath = EditorHelper.FileSelect(new GUIContent("Resource Path", "Override path to the prefab resource that is the item."), ResourcePath, "fbx,prefab");
			if (lNewResourcePath != ResourcePath)
			{
				lIsDirty = true;
				ResourcePath = lNewResourcePath;
			}

			GUILayout.Space(5f);

			if (EditorHelper.BoolField("Add Body Shape", "Determines if we'll add an extra body shape to account for the stance.", AddCombatantBodyShape, mMotionController))
			{
				lIsDirty = true;
				AddCombatantBodyShape = EditorHelper.FieldBoolValue;
			}

			if (AddCombatantBodyShape)
			{
				if (EditorHelper.FloatField("Body Shape Radius", "Radius to make the body shape.", CombatantBodyShapeRadius, mMotionController))
				{
					lIsDirty = true;
					CombatantBodyShapeRadius = EditorHelper.FieldFloatValue;
				}
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
		public int TRANS_AnyState_Equip_Bow = -1;
		public int TRANS_EntryState_Equip_Bow = -1;
		public int TRANS_AnyState_Disarm_Bow = -1;
		public int TRANS_EntryState_Disarm_Bow = -1;
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
				if (lTransitionID == TRANS_AnyState_Equip_Bow) { return true; }
				if (lTransitionID == TRANS_EntryState_Equip_Bow) { return true; }
				if (lTransitionID == TRANS_AnyState_Disarm_Bow) { return true; }
				if (lTransitionID == TRANS_EntryState_Disarm_Bow) { return true; }
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
			if (rTransitionID == TRANS_AnyState_Equip_Bow) { return true; }
			if (rTransitionID == TRANS_EntryState_Equip_Bow) { return true; }
			if (rTransitionID == TRANS_AnyState_Disarm_Bow) { return true; }
			if (rTransitionID == TRANS_EntryState_Disarm_Bow) { return true; }
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
			TRANS_AnyState_Equip_Bow = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".RifleAP_BasicEquipStore-SM.Equip_Bow");
			TRANS_EntryState_Equip_Bow = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".RifleAP_BasicEquipStore-SM.Equip_Bow");
			TRANS_AnyState_Disarm_Bow = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".RifleAP_BasicEquipStore-SM.Disarm_Bow");
			TRANS_EntryState_Disarm_Bow = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".RifleAP_BasicEquipStore-SM.Disarm_Bow");
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

			UnityEditor.Animations.AnimatorStateMachine lSSM_82200 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "RifleAP_BasicEquipStore-SM");
			if (lSSM_82200 == null) { lSSM_82200 = lLayerStateMachine.AddStateMachine("RifleAP_BasicEquipStore-SM", new Vector3(190, -1010, 0)); }

			UnityEditor.Animations.AnimatorState lState_82034 = MotionControllerMotion.EditorFindState(lSSM_82200, "Equip Rifle");
			if (lState_82034 == null) { lState_82034 = lSSM_82200.AddState("Equip Rifle", new Vector3(300, 528, 0)); }
			lState_82034.speed = 2.5f;
			lState_82034.mirror = false;
			lState_82034.tag = "";
			lState_82034.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/RifleEquip.anim", "RifleEquip");

			UnityEditor.Animations.AnimatorState lState_82110 = MotionControllerMotion.EditorFindState(lSSM_82200, "Store Rifle");
			if (lState_82110 == null) { lState_82110 = lSSM_82200.AddState("Store Rifle", new Vector3(300, 576, 0)); }
			lState_82110.speed = 2f;
			lState_82110.mirror = false;
			lState_82110.tag = "";
			lState_82110.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/RifleStore.anim", "RifleStore");

			UnityEditor.Animations.AnimatorState lState_82102 = MotionControllerMotion.EditorFindState(lSSM_82200, "Store Rifle Idle Pose Exit");
			if (lState_82102 == null) { lState_82102 = lSSM_82200.AddState("Store Rifle Idle Pose Exit", new Vector3(552, 576, 0)); }
			lState_82102.speed = 1f;
			lState_82102.mirror = false;
			lState_82102.tag = "Exit";
			lState_82102.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdlePose");

			UnityEditor.Animations.AnimatorState lState_82088 = MotionControllerMotion.EditorFindState(lSSM_82200, "Equip Rifle Idle Pose Exit");
			if (lState_82088 == null) { lState_82088 = lSSM_82200.AddState("Equip Rifle Idle Pose Exit", new Vector3(552, 528, 0)); }
			lState_82088.speed = 1f;
			lState_82088.mirror = false;
			lState_82088.tag = "Exit";
			lState_82088.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/RifleIdlePose.anim", "RifleIdlePose");

			UnityEditor.Animations.AnimatorState lState_82164 = MotionControllerMotion.EditorFindState(lSSM_82200, "Equip Pistol");
			if (lState_82164 == null) { lState_82164 = lSSM_82200.AddState("Equip Pistol", new Vector3(300, 648, 0)); }
			lState_82164.speed = 1f;
			lState_82164.mirror = false;
			lState_82164.tag = "";
			lState_82164.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolEquip.anim", "PistolEquip");

			UnityEditor.Animations.AnimatorState lState_82062 = MotionControllerMotion.EditorFindState(lSSM_82200, "Store Pistol");
			if (lState_82062 == null) { lState_82062 = lSSM_82200.AddState("Store Pistol", new Vector3(300, 696, 0)); }
			lState_82062.speed = 0.5f;
			lState_82062.mirror = false;
			lState_82062.tag = "";
			lState_82062.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolStore.anim", "PistolStore");

			UnityEditor.Animations.AnimatorState lState_82144 = MotionControllerMotion.EditorFindState(lSSM_82200, "Store Pistol Idle Pose Exit");
			if (lState_82144 == null) { lState_82144 = lSSM_82200.AddState("Store Pistol Idle Pose Exit", new Vector3(552, 696, 0)); }
			lState_82144.speed = 1f;
			lState_82144.mirror = false;
			lState_82144.tag = "Exit";
			lState_82144.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdlePose");

			UnityEditor.Animations.AnimatorState lState_82158 = MotionControllerMotion.EditorFindState(lSSM_82200, "Equip Pistol Idle Pose Exit");
			if (lState_82158 == null) { lState_82158 = lSSM_82200.AddState("Equip Pistol Idle Pose Exit", new Vector3(552, 648, 0)); }
			lState_82158.speed = 1f;
			lState_82158.mirror = false;
			lState_82158.tag = "Exit";
			lState_82158.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionControllerPacks/Shooter/Content/Animations/Mixamo/PistolIdlePose.anim", "PistolIdlePose");

			UnityEditor.Animations.AnimatorState lState_82086 = MotionControllerMotion.EditorFindState(lSSM_82200, "Equip_Bow");
			if (lState_82086 == null) { lState_82086 = lSSM_82200.AddState("Equip_Bow", new Vector3(300, 288, 0)); }
			lState_82086.speed = 1f;
			lState_82086.mirror = false;
			lState_82086.tag = "";

			UnityEditor.Animations.AnimatorState lState_82066 = MotionControllerMotion.EditorFindState(lSSM_82200, "Disarm_Bow");
			if (lState_82066 == null) { lState_82066 = lSSM_82200.AddState("Disarm_Bow", new Vector3(300, 336, 0)); }
			lState_82066.speed = 1f;
			lState_82066.mirror = false;
			lState_82066.tag = "";

			UnityEditor.Animations.AnimatorState lState_82112 = MotionControllerMotion.EditorFindState(lSSM_82200, "EquipBowIdlePoseExit");
			if (lState_82112 == null) { lState_82112 = lSSM_82200.AddState("EquipBowIdlePoseExit", new Vector3(552, 288, 0)); }
			lState_82112.speed = 1f;
			lState_82112.mirror = false;
			lState_82112.tag = "Exit";

			UnityEditor.Animations.AnimatorState lState_82074 = MotionControllerMotion.EditorFindState(lSSM_82200, "StoreBowIdlePoseExit");
			if (lState_82074 == null) { lState_82074 = lSSM_82200.AddState("StoreBowIdlePoseExit", new Vector3(552, 336, 0)); }
			lState_82074.speed = 1f;
			lState_82074.mirror = false;
			lState_82074.tag = "Exit";
			lState_82074.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/Assets/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdlePose");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_81822 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_82034, 0);
			if (lAnyTransition_81822 == null) { lAnyTransition_81822 = lLayerStateMachine.AddAnyStateTransition(lState_82034); }
			lAnyTransition_81822.isExit = false;
			lAnyTransition_81822.hasExitTime = false;
			lAnyTransition_81822.hasFixedDuration = true;
			lAnyTransition_81822.exitTime = 0.75f;
			lAnyTransition_81822.duration = 0.1f;
			lAnyTransition_81822.offset = 0.08977433f;
			lAnyTransition_81822.mute = false;
			lAnyTransition_81822.solo = false;
			lAnyTransition_81822.canTransitionToSelf = true;
			lAnyTransition_81822.orderedInterruption = true;
			lAnyTransition_81822.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_81822.conditions.Length - 1; i >= 0; i--) { lAnyTransition_81822.RemoveCondition(lAnyTransition_81822.conditions[i]); }
			lAnyTransition_81822.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73150f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_81822.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 500f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_81948 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_82110, 0);
			if (lAnyTransition_81948 == null) { lAnyTransition_81948 = lLayerStateMachine.AddAnyStateTransition(lState_82110); }
			lAnyTransition_81948.isExit = false;
			lAnyTransition_81948.hasExitTime = false;
			lAnyTransition_81948.hasFixedDuration = true;
			lAnyTransition_81948.exitTime = 0.75f;
			lAnyTransition_81948.duration = 0.1f;
			lAnyTransition_81948.offset = 0f;
			lAnyTransition_81948.mute = false;
			lAnyTransition_81948.solo = false;
			lAnyTransition_81948.canTransitionToSelf = true;
			lAnyTransition_81948.orderedInterruption = true;
			lAnyTransition_81948.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_81948.conditions.Length - 1; i >= 0; i--) { lAnyTransition_81948.RemoveCondition(lAnyTransition_81948.conditions[i]); }
			lAnyTransition_81948.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73155f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_81948.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 500f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_81876 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_82164, 0);
			if (lAnyTransition_81876 == null) { lAnyTransition_81876 = lLayerStateMachine.AddAnyStateTransition(lState_82164); }
			lAnyTransition_81876.isExit = false;
			lAnyTransition_81876.hasExitTime = false;
			lAnyTransition_81876.hasFixedDuration = true;
			lAnyTransition_81876.exitTime = 0.7499999f;
			lAnyTransition_81876.duration = 0.09096396f;
			lAnyTransition_81876.offset = 0.1180723f;
			lAnyTransition_81876.mute = false;
			lAnyTransition_81876.solo = false;
			lAnyTransition_81876.canTransitionToSelf = true;
			lAnyTransition_81876.orderedInterruption = true;
			lAnyTransition_81876.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_81876.conditions.Length - 1; i >= 0; i--) { lAnyTransition_81876.RemoveCondition(lAnyTransition_81876.conditions[i]); }
			lAnyTransition_81876.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73150f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_81876.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 550f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_81970 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_82062, 0);
			if (lAnyTransition_81970 == null) { lAnyTransition_81970 = lLayerStateMachine.AddAnyStateTransition(lState_82062); }
			lAnyTransition_81970.isExit = false;
			lAnyTransition_81970.hasExitTime = false;
			lAnyTransition_81970.hasFixedDuration = true;
			lAnyTransition_81970.exitTime = 0.75f;
			lAnyTransition_81970.duration = 0.1f;
			lAnyTransition_81970.offset = 0.3219408f;
			lAnyTransition_81970.mute = false;
			lAnyTransition_81970.solo = false;
			lAnyTransition_81970.canTransitionToSelf = true;
			lAnyTransition_81970.orderedInterruption = true;
			lAnyTransition_81970.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_81970.conditions.Length - 1; i >= 0; i--) { lAnyTransition_81970.RemoveCondition(lAnyTransition_81970.conditions[i]); }
			lAnyTransition_81970.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73155f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_81970.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 550f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_81762 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_82086, 0);
			if (lAnyTransition_81762 == null) { lAnyTransition_81762 = lLayerStateMachine.AddAnyStateTransition(lState_82086); }
			lAnyTransition_81762.isExit = false;
			lAnyTransition_81762.hasExitTime = false;
			lAnyTransition_81762.hasFixedDuration = true;
			lAnyTransition_81762.exitTime = 0.75f;
			lAnyTransition_81762.duration = 0.25f;
			lAnyTransition_81762.offset = 0f;
			lAnyTransition_81762.mute = false;
			lAnyTransition_81762.solo = false;
			lAnyTransition_81762.canTransitionToSelf = true;
			lAnyTransition_81762.orderedInterruption = true;
			lAnyTransition_81762.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_81762.conditions.Length - 1; i >= 0; i--) { lAnyTransition_81762.RemoveCondition(lAnyTransition_81762.conditions[i]); }
			lAnyTransition_81762.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73150f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_81762.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 200f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_81768 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_82066, 0);
			if (lAnyTransition_81768 == null) { lAnyTransition_81768 = lLayerStateMachine.AddAnyStateTransition(lState_82066); }
			lAnyTransition_81768.isExit = false;
			lAnyTransition_81768.hasExitTime = false;
			lAnyTransition_81768.hasFixedDuration = true;
			lAnyTransition_81768.exitTime = 0.75f;
			lAnyTransition_81768.duration = 0.25f;
			lAnyTransition_81768.offset = 0f;
			lAnyTransition_81768.mute = false;
			lAnyTransition_81768.solo = false;
			lAnyTransition_81768.canTransitionToSelf = true;
			lAnyTransition_81768.orderedInterruption = true;
			lAnyTransition_81768.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_81768.conditions.Length - 1; i >= 0; i--) { lAnyTransition_81768.RemoveCondition(lAnyTransition_81768.conditions[i]); }
			lAnyTransition_81768.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 73155f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_81768.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 200f, "L" + rLayerIndex + "MotionForm");

			UnityEditor.Animations.AnimatorStateTransition lTransition_81946 = MotionControllerMotion.EditorFindTransition(lState_82034, lState_82088, 0);
			if (lTransition_81946 == null) { lTransition_81946 = lState_82034.AddTransition(lState_82088); }
			lTransition_81946.isExit = false;
			lTransition_81946.hasExitTime = true;
			lTransition_81946.hasFixedDuration = true;
			lTransition_81946.exitTime = 0.5167463f;
			lTransition_81946.duration = 0.2f;
			lTransition_81946.offset = 0f;
			lTransition_81946.mute = false;
			lTransition_81946.solo = false;
			lTransition_81946.canTransitionToSelf = true;
			lTransition_81946.orderedInterruption = true;
			lTransition_81946.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_81946.conditions.Length - 1; i >= 0; i--) { lTransition_81946.RemoveCondition(lTransition_81946.conditions[i]); }

			UnityEditor.Animations.AnimatorStateTransition lTransition_81972 = MotionControllerMotion.EditorFindTransition(lState_82110, lState_82102, 0);
			if (lTransition_81972 == null) { lTransition_81972 = lState_82110.AddTransition(lState_82102); }
			lTransition_81972.isExit = false;
			lTransition_81972.hasExitTime = true;
			lTransition_81972.hasFixedDuration = true;
			lTransition_81972.exitTime = 0.4055317f;
			lTransition_81972.duration = 0.2f;
			lTransition_81972.offset = 0f;
			lTransition_81972.mute = false;
			lTransition_81972.solo = false;
			lTransition_81972.canTransitionToSelf = true;
			lTransition_81972.orderedInterruption = true;
			lTransition_81972.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_81972.conditions.Length - 1; i >= 0; i--) { lTransition_81972.RemoveCondition(lTransition_81972.conditions[i]); }

			UnityEditor.Animations.AnimatorStateTransition lTransition_81864 = MotionControllerMotion.EditorFindTransition(lState_82164, lState_82158, 0);
			if (lTransition_81864 == null) { lTransition_81864 = lState_82164.AddTransition(lState_82158); }
			lTransition_81864.isExit = false;
			lTransition_81864.hasExitTime = true;
			lTransition_81864.hasFixedDuration = true;
			lTransition_81864.exitTime = 0.5760845f;
			lTransition_81864.duration = 0.1999999f;
			lTransition_81864.offset = 0f;
			lTransition_81864.mute = false;
			lTransition_81864.solo = false;
			lTransition_81864.canTransitionToSelf = true;
			lTransition_81864.orderedInterruption = true;
			lTransition_81864.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_81864.conditions.Length - 1; i >= 0; i--) { lTransition_81864.RemoveCondition(lTransition_81864.conditions[i]); }

			UnityEditor.Animations.AnimatorStateTransition lTransition_81906 = MotionControllerMotion.EditorFindTransition(lState_82062, lState_82144, 0);
			if (lTransition_81906 == null) { lTransition_81906 = lState_82062.AddTransition(lState_82144); }
			lTransition_81906.isExit = false;
			lTransition_81906.hasExitTime = true;
			lTransition_81906.hasFixedDuration = true;
			lTransition_81906.exitTime = 0.6415698f;
			lTransition_81906.duration = 0.2f;
			lTransition_81906.offset = 0f;
			lTransition_81906.mute = false;
			lTransition_81906.solo = false;
			lTransition_81906.canTransitionToSelf = true;
			lTransition_81906.orderedInterruption = true;
			lTransition_81906.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_81906.conditions.Length - 1; i >= 0; i--) { lTransition_81906.RemoveCondition(lTransition_81906.conditions[i]); }

			UnityEditor.Animations.AnimatorStateTransition lTransition_81848 = MotionControllerMotion.EditorFindTransition(lState_82086, lState_82112, 0);
			if (lTransition_81848 == null) { lTransition_81848 = lState_82086.AddTransition(lState_82112); }
			lTransition_81848.isExit = false;
			lTransition_81848.hasExitTime = true;
			lTransition_81848.hasFixedDuration = true;
			lTransition_81848.exitTime = 0.9f;
			lTransition_81848.duration = 0.1f;
			lTransition_81848.offset = 0f;
			lTransition_81848.mute = false;
			lTransition_81848.solo = false;
			lTransition_81848.canTransitionToSelf = true;
			lTransition_81848.orderedInterruption = true;
			lTransition_81848.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_81848.conditions.Length - 1; i >= 0; i--) { lTransition_81848.RemoveCondition(lTransition_81848.conditions[i]); }

			UnityEditor.Animations.AnimatorStateTransition lTransition_81794 = MotionControllerMotion.EditorFindTransition(lState_82066, lState_82074, 0);
			if (lTransition_81794 == null) { lTransition_81794 = lState_82066.AddTransition(lState_82074); }
			lTransition_81794.isExit = false;
			lTransition_81794.hasExitTime = true;
			lTransition_81794.hasFixedDuration = true;
			lTransition_81794.exitTime = 0.9f;
			lTransition_81794.duration = 0.1f;
			lTransition_81794.offset = 0f;
			lTransition_81794.mute = false;
			lTransition_81794.solo = false;
			lTransition_81794.canTransitionToSelf = true;
			lTransition_81794.orderedInterruption = true;
			lTransition_81794.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lTransition_81794.conditions.Length - 1; i >= 0; i--) { lTransition_81794.RemoveCondition(lTransition_81794.conditions[i]); }


			// Run any post processing after creating the state machine
			OnStateMachineCreated();
		}

#endif

		// ************************************ END AUTO GENERATED ************************************
		#endregion



	}
}
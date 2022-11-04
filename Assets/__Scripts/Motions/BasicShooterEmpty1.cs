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
	public class BasicShooterEmpty1 : BasicShooterMotion1
	{
		#region MotionPhases
		
		public int PHASE_UNKNOWN = 0;
		public int PHASE_START = 3010;

		#endregion MotionPhases


		#region Properties

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
		

		public string _WeaponSlotID = "RIGHT_HAND";
		public string WeaponSlotID
		{
			get { return _WeaponSlotID; }
			set { _WeaponSlotID = value; }
		}
		
		
		public string _AltWeaponSlotID = "LEFT_HAND";
		public string AltWeaponSlotID
		{
			get { return _AltWeaponSlotID; }
			set { _AltWeaponSlotID = value; }
		}
		
		#endregion Properties


		#region Members
		
		protected int[] mRequiredForms = null;
		
		protected bool mIsLookIKEnabledLocally = true;

		#endregion Members


		#region Constructors
		
		public BasicShooterEmpty1() : base()
		{
			_Category = EnumMotionCategories.IDLE;
			
			_Priority = 1;
			_Form = 0;
			
			mLookIKMaxHorizontalAngle = 45f;
			
#if UNITY_EDITOR
			if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Empty-SM"; }
#endif
		}
		
		
		public BasicShooterEmpty1(MotionController rController) : base(rController)
		{
			_Category = EnumMotionCategories.IDLE;
			
			_Priority = 1;
			_Form = 0;
			
			mLookIKMaxHorizontalAngle = 45f;
			
#if UNITY_EDITOR
			if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Empty-SM"; }
#endif
		}

		#endregion Constructors


		#region MotionFunctions
		
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

		#region Tests
		
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
		
		
		public override bool TestInterruption(MotionControllerMotion rMotion)
		{
			if (mLookIKWeight > 0f && !(rMotion is BasicShooterAttack))
			{
				EaseOutIK(_LookIKOutSpeed);
			}
			
			return true;
		}
		
		#endregion Tests
		

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
		
		
		public override void UpdateRootMotion(float rDeltaTime, int rUpdateIndex, ref Vector3 rMovement, ref Quaternion rRotation)
		{
			rMovement = Vector3.zero;
			rRotation = Quaternion.identity;
		}
		
		
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

		#endregion Motion


		#region Events
		
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

		#endregion Events


		#region EditorGUI

#if UNITY_EDITOR
		
		public override bool OnInspectorGUI()
		{
			bool lIsDirty = false;
			
			if (EditorHelper.TextField("Required Forms",
				"Comma delimited list of forms that this motion will activate for.",
				RequiredForms, mMotionController))
			{
				lIsDirty = true;
				RequiredForms = EditorHelper.FieldStringValue;
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
				if (EditorHelper.TextField("IK Tag",
					"Tag required by the first motion layer in order for us to enable spine IK",
					LookIKMotionTag, mMotionController))
				{
					lIsDirty = true;
					LookIKMotionTag = EditorHelper.FieldStringValue;
				}
				
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
			
			GameObject lNewAttributeSourceOwner =
				EditorHelper.InterfaceOwnerField<IInventorySource>(
					new GUIContent("Inventory Source", "Inventory source we'll use for accessing items and slots."),
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
		public int STATE_EmptyPose = -1;
		public int TRANS_AnyState_EmptyPose = -1;
		public int TRANS_EntryState_EmptyPose = -1;

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
					if (lStateID == STATE_EmptyPose) { return true; }
				}

				if (lTransitionID == TRANS_AnyState_EmptyPose) { return true; }
				if (lTransitionID == TRANS_EntryState_EmptyPose) { return true; }
				if (lTransitionID == TRANS_AnyState_EmptyPose) { return true; }
				if (lTransitionID == TRANS_EntryState_EmptyPose) { return true; }
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
			if (rStateID == STATE_EmptyPose) { return true; }
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
				if (rStateID == STATE_EmptyPose) { return true; }
			}

			if (rTransitionID == TRANS_AnyState_EmptyPose) { return true; }
			if (rTransitionID == TRANS_EntryState_EmptyPose) { return true; }
			if (rTransitionID == TRANS_AnyState_EmptyPose) { return true; }
			if (rTransitionID == TRANS_EntryState_EmptyPose) { return true; }
			return false;
		}

		/// <summary>
		/// Preprocess any animator data so the motion can use it later
		/// </summary>
		public override void LoadAnimatorData()
		{
			string lLayer = mMotionController.Animator.GetLayerName(mMotionLayer._AnimatorLayerIndex);
			TRANS_AnyState_EmptyPose = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".Empty-SM.EmptyPose");
			TRANS_EntryState_EmptyPose = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".Empty-SM.EmptyPose");
			TRANS_AnyState_EmptyPose = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".Empty-SM.EmptyPose");
			TRANS_EntryState_EmptyPose = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".Empty-SM.EmptyPose");
			STATE_Empty = mMotionController.AddAnimatorName("" + lLayer + ".Empty");
			STATE_EmptyPose = mMotionController.AddAnimatorName("" + lLayer + ".Empty-SM.EmptyPose");
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

			UnityEditor.Animations.AnimatorStateMachine lSSM_98366 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "Empty-SM");
			if (lSSM_98366 == null) { lSSM_98366 = lLayerStateMachine.AddStateMachine("Empty-SM", new Vector3(192, -480, 0)); }

			UnityEditor.Animations.AnimatorState lState_98274 = MotionControllerMotion.EditorFindState(lSSM_98366, "EmptyPose");
			if (lState_98274 == null) { lState_98274 = lSSM_98366.AddState("EmptyPose", new Vector3(312, 84, 0)); }
			lState_98274.speed = 1f;
			lState_98274.mirror = false;
			lState_98274.tag = "";

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_98098 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_98274, 0);
			if (lAnyTransition_98098 == null) { lAnyTransition_98098 = lLayerStateMachine.AddAnyStateTransition(lState_98274); }
			lAnyTransition_98098.isExit = false;
			lAnyTransition_98098.hasExitTime = false;
			lAnyTransition_98098.hasFixedDuration = true;
			lAnyTransition_98098.exitTime = 0.75f;
			lAnyTransition_98098.duration = 0.15f;
			lAnyTransition_98098.offset = 0f;
			lAnyTransition_98098.mute = false;
			lAnyTransition_98098.solo = false;
			lAnyTransition_98098.canTransitionToSelf = false;
			lAnyTransition_98098.orderedInterruption = false;
			lAnyTransition_98098.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)2;
			for (int i = lAnyTransition_98098.conditions.Length - 1; i >= 0; i--) { lAnyTransition_98098.RemoveCondition(lAnyTransition_98098.conditions[i]); }
			lAnyTransition_98098.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3010f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_98098.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_98098.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_98062 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_98274, 1);
			if (lAnyTransition_98062 == null) { lAnyTransition_98062 = lLayerStateMachine.AddAnyStateTransition(lState_98274); }
			lAnyTransition_98062.isExit = false;
			lAnyTransition_98062.hasExitTime = false;
			lAnyTransition_98062.hasFixedDuration = true;
			lAnyTransition_98062.exitTime = 0.75f;
			lAnyTransition_98062.duration = 0f;
			lAnyTransition_98062.offset = 0f;
			lAnyTransition_98062.mute = false;
			lAnyTransition_98062.solo = false;
			lAnyTransition_98062.canTransitionToSelf = false;
			lAnyTransition_98062.orderedInterruption = false;
			lAnyTransition_98062.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)2;
			for (int i = lAnyTransition_98062.conditions.Length - 1; i >= 0; i--) { lAnyTransition_98062.RemoveCondition(lAnyTransition_98062.conditions[i]); }
			lAnyTransition_98062.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3010f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_98062.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_98062.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");


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

			UnityEditor.Animations.AnimatorStateMachine lSSM_98366 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "Empty-SM");
			if (lSSM_98366 == null) { lSSM_98366 = lLayerStateMachine.AddStateMachine("Empty-SM", new Vector3(192, -480, 0)); }

			UnityEditor.Animations.AnimatorState lState_98274 = MotionControllerMotion.EditorFindState(lSSM_98366, "EmptyPose");
			if (lState_98274 == null) { lState_98274 = lSSM_98366.AddState("EmptyPose", new Vector3(312, 84, 0)); }
			lState_98274.speed = 1f;
			lState_98274.mirror = false;
			lState_98274.tag = "";

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_98098 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_98274, 0);
			if (lAnyTransition_98098 == null) { lAnyTransition_98098 = lLayerStateMachine.AddAnyStateTransition(lState_98274); }
			lAnyTransition_98098.isExit = false;
			lAnyTransition_98098.hasExitTime = false;
			lAnyTransition_98098.hasFixedDuration = true;
			lAnyTransition_98098.exitTime = 0.75f;
			lAnyTransition_98098.duration = 0.15f;
			lAnyTransition_98098.offset = 0f;
			lAnyTransition_98098.mute = false;
			lAnyTransition_98098.solo = false;
			lAnyTransition_98098.canTransitionToSelf = false;
			lAnyTransition_98098.orderedInterruption = false;
			lAnyTransition_98098.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)2;
			for (int i = lAnyTransition_98098.conditions.Length - 1; i >= 0; i--) { lAnyTransition_98098.RemoveCondition(lAnyTransition_98098.conditions[i]); }
			lAnyTransition_98098.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3010f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_98098.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_98098.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_98062 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_98274, 1);
			if (lAnyTransition_98062 == null) { lAnyTransition_98062 = lLayerStateMachine.AddAnyStateTransition(lState_98274); }
			lAnyTransition_98062.isExit = false;
			lAnyTransition_98062.hasExitTime = false;
			lAnyTransition_98062.hasFixedDuration = true;
			lAnyTransition_98062.exitTime = 0.75f;
			lAnyTransition_98062.duration = 0f;
			lAnyTransition_98062.offset = 0f;
			lAnyTransition_98062.mute = false;
			lAnyTransition_98062.solo = false;
			lAnyTransition_98062.canTransitionToSelf = false;
			lAnyTransition_98062.orderedInterruption = false;
			lAnyTransition_98062.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)2;
			for (int i = lAnyTransition_98062.conditions.Length - 1; i >= 0; i--) { lAnyTransition_98062.RemoveCondition(lAnyTransition_98062.conditions[i]); }
			lAnyTransition_98062.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3010f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_98062.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_98062.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

		}


		#endregion Definition

	}
}
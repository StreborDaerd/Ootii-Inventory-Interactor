using UnityEngine;
using com.ootii.Cameras;
using com.ootii.Geometry;
using com.ootii.Helpers;
using com.ootii.Actors.AnimationControllers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WildWalrus.Actors.AnimationControllers
{
	[MotionName("Basic Idle 1")]
	[MotionDescription("Test. Simple idle motion to be used as a default motion. It can also rotate the actor with the camera view.")]
	public class BasicIdle1 : MotionControllerMotion
	{
		/// <summary>
		/// Trigger values for the motion
		/// </summary>
		public int PHASE_UNKNOWN = 0;
		public int PHASE_START = 3000;

		/// <summary>
		/// Determines if we're using the IsInMotion() function to verify that
		/// the transition in the animator has occurred for this motion.
		/// </summary>
		public override bool VerifyTransition
		{
			get { return false; }
		}

		/// <summary>
		/// Determines if we rotate to match the camera
		/// </summary>
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

		/// <summary>
		/// Desired degrees of rotation per second
		/// </summary>
		public float _RotationToCameraSpeed = 360f;
		public float RotationToCameraSpeed
		{
			get { return _RotationToCameraSpeed; }
			set { _RotationToCameraSpeed = value; }
		}

		/// <summary>
		/// Determines if we rotate by ourselves
		/// </summary>
		public bool _RotateWithInput = false;
		public bool RotateWithInput
		{
			get { return _RotateWithInput; }
			set { _RotateWithInput = value; }
		}

		/// <summary>
		/// Desired degrees of rotation per second
		/// </summary>
		public float _RotationSpeed = 120f;
		public float RotationSpeed
		{
			get { return _RotationSpeed; }
			set { _RotationSpeed = value; }
		}

		/// <summary>
		/// Used to apply some smoothing to the mouse movement
		/// </summary>
		public float _RotationSmoothing = 0.1f;
		public virtual float RotationSmoothing
		{
			get { return _RotationSmoothing; }
			set { _RotationSmoothing = value; }
		}

		/// <summary>
		/// Determines if the actor rotation should be linked to the camera
		/// </summary>
		protected bool mLinkRotation = false;

		/// <summary>
		/// Fields to help smooth out the mouse rotation
		/// </summary>
		protected float mYaw = 0f;
		protected float mYawTarget = 0f;
		protected float mYawVelocity = 0f;

		// Used to force a change if neede
		protected int mActiveForm = 0;

		/// <summary>
		/// Default constructor
		/// </summary>
		public BasicIdle1()
			 : base()
		{
			_Category = EnumMotionCategories.IDLE;

			_Priority = 0;

#if UNITY_EDITOR
			if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicIdle-SM"; }
#endif
		}

		/// <summary>
		/// Controller constructor
		/// </summary>
		/// <param name="rController">Controller the motion belongs to</param>
		public BasicIdle1(MotionController rController)
			 : base(rController)
		{
			_Category = EnumMotionCategories.IDLE;

			_Priority = 0;

#if UNITY_EDITOR
			if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicIdle-SM"; }
#endif
		}

		/// <summary>
		/// Awake is called after all objects are initialized so you can safely speak to other objects. This is where
		/// reference can be associated.
		/// </summary>
		public override void Awake()
		{
			base.Awake();
		}

		/// <summary>
		/// Tests if this motion should be started. However, the motion
		/// isn't actually started.
		/// </summary>
		/// <returns></returns>
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

		/// <summary>
		/// Tests if the motion should continue. If it shouldn't, the motion
		/// is typically disabled
		/// </summary>
		/// <returns></returns>
		public override bool TestUpdate()
		{
			//if (mIsAnimatorActive && !IsInMotionState)
			//{
			//    return false;
			//}

			//if (mMotionController.Stance != EnumControllerStance.TRAVERSAL)
			//{
			//    return false;
			//}

			// Exit if we hit an exit node
			if (mMotionLayer.AnimatorTransitionID == 0 &&
				 mMotionController.State.AnimatorStates[mMotionLayer._AnimatorLayerIndex].StateInfo.IsTag("Exit"))
			{
				return false;
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

		/// <summary>
		/// Raised when we shut the motion down
		/// </summary>
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

		/// <summary>
		/// Create a rotation velocity that rotates the character based on input
		/// </summary>
		/// <param name="rDeltaTime"></param>
		/// <param name="rAngularVelocity"></param>
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

		/// <summary>
		/// When we want to rotate based on the camera direction, we need to tweak the actor
		/// rotation AFTER we process the camera. Otherwise, we can get small stutters during camera rotation. 
		/// 
		/// This is the only way to keep them totally in sync. It also means we can't run any of our AC processing
		/// as the AC already ran. So, we do minimal work here
		/// </summary>
		/// <param name="rDeltaTime"></param>
		/// <param name="rUpdateCount"></param>
		/// <param name="rCamera"></param>
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

			if (EditorHelper.IntField("Form", "Sets the LXMotionForm animator property to determine the animation for the motion. If value is < 0, we use the Actor Core's 'Default Form' state.", Form, mMotionController))
			{
				lIsDirty = true;
				Form = EditorHelper.FieldIntValue;
			}

			GUILayout.Space(5f);

			if (EditorHelper.BoolField("Rotate With Camera", "Determines if we rotate to match the camera.", RotateWithCamera, mMotionController))
			{
				lIsDirty = true;
				RotateWithCamera = EditorHelper.FieldBoolValue;
			}

			if (EditorHelper.TextField("Rotate Action Alias", "Action alias determines if rotation is activated. This typically matches the input source's View Activator.", ActionAlias, mMotionController))
			{
				lIsDirty = true;
				ActionAlias = EditorHelper.FieldStringValue;
			}

			if (EditorHelper.FloatField("Rotation Speed", "Degrees per second to rotate to the camera's direction.", RotationToCameraSpeed, mMotionController))
			{
				lIsDirty = true;
				RotationToCameraSpeed = EditorHelper.FieldFloatValue;
			}

			GUILayout.Space(5f);

			if (EditorHelper.BoolField("Rotate With View Input", "Determines if we rotate based on user input (view x).", RotateWithInput, mMotionController))
			{
				lIsDirty = true;
				RotateWithInput = EditorHelper.FieldBoolValue;
			}

			if (EditorHelper.FloatField("Rotation Speed", "Degrees per second to rotate the actor.", RotationSpeed, mMotionController))
			{
				lIsDirty = true;
				RotationSpeed = EditorHelper.FieldFloatValue;
			}

			if (EditorHelper.FloatField("Rotation Smoothing", "Smoothing factor applied to rotation (0 disables).", RotationSmoothing, mMotionController))
			{
				lIsDirty = true;
				RotationSmoothing = EditorHelper.FieldFloatValue;
			}

			return lIsDirty;
		}

#endif

		#region Pack Methods

		/// <summary>
		/// Name of the group these motions belong to
		/// </summary>
		public static string GroupName()
		{
			return "Basic";
		}

		#endregion

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
				if (lTransitionID == TRANS_AnyState_UnarmedIdlePose) { return true; }
				if (lTransitionID == TRANS_EntryState_UnarmedIdlePose) { return true; }
				if (lTransitionID == TRANS_AnyState_RifleIdlePose) { return true; }
				if (lTransitionID == TRANS_EntryState_RifleIdlePose) { return true; }
				if (lTransitionID == TRANS_AnyState_RifleIdlePose) { return true; }
				if (lTransitionID == TRANS_EntryState_RifleIdlePose) { return true; }
				if (lTransitionID == TRANS_AnyState_PistolIdlePose) { return true; }
				if (lTransitionID == TRANS_EntryState_PistolIdlePose) { return true; }
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
			if (rTransitionID == TRANS_AnyState_UnarmedIdlePose) { return true; }
			if (rTransitionID == TRANS_EntryState_UnarmedIdlePose) { return true; }
			if (rTransitionID == TRANS_AnyState_RifleIdlePose) { return true; }
			if (rTransitionID == TRANS_EntryState_RifleIdlePose) { return true; }
			if (rTransitionID == TRANS_AnyState_RifleIdlePose) { return true; }
			if (rTransitionID == TRANS_EntryState_RifleIdlePose) { return true; }
			if (rTransitionID == TRANS_AnyState_PistolIdlePose) { return true; }
			if (rTransitionID == TRANS_EntryState_PistolIdlePose) { return true; }
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
			TRANS_AnyState_UnarmedIdlePose = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicIdle-SM.Unarmed Idle Pose");
			TRANS_EntryState_UnarmedIdlePose = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicIdle-SM.Unarmed Idle Pose");
			TRANS_AnyState_UnarmedIdlePose = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicIdle-SM.Unarmed Idle Pose");
			TRANS_EntryState_UnarmedIdlePose = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicIdle-SM.Unarmed Idle Pose");
			TRANS_AnyState_RifleIdlePose = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicIdle-SM.Rifle Idle Pose");
			TRANS_EntryState_RifleIdlePose = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicIdle-SM.Rifle Idle Pose");
			TRANS_AnyState_RifleIdlePose = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicIdle-SM.Rifle Idle Pose");
			TRANS_EntryState_RifleIdlePose = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicIdle-SM.Rifle Idle Pose");
			TRANS_AnyState_PistolIdlePose = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicIdle-SM.PistolIdlePose");
			TRANS_EntryState_PistolIdlePose = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicIdle-SM.PistolIdlePose");
			TRANS_AnyState_PistolIdlePose = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicIdle-SM.PistolIdlePose");
			TRANS_EntryState_PistolIdlePose = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicIdle-SM.PistolIdlePose");
			STATE_Start = mMotionController.AddAnimatorName("" + lLayer + ".Start");
			STATE_UnarmedIdlePose = mMotionController.AddAnimatorName("" + lLayer + ".BasicIdle-SM.Unarmed Idle Pose");
			STATE_RifleIdlePose = mMotionController.AddAnimatorName("" + lLayer + ".BasicIdle-SM.Rifle Idle Pose");
			STATE_PistolIdlePose = mMotionController.AddAnimatorName("" + lLayer + ".BasicIdle-SM.PistolIdlePose");
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

			UnityEditor.Animations.AnimatorStateMachine lSSM_25970 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "BasicIdle-SM");
			if (lSSM_25970 == null) { lSSM_25970 = lLayerStateMachine.AddStateMachine("BasicIdle-SM", new Vector3(192, -1056, 0)); }

			UnityEditor.Animations.AnimatorState lState_26748 = MotionControllerMotion.EditorFindState(lSSM_25970, "Unarmed Idle Pose");
			if (lState_26748 == null) { lState_26748 = lSSM_25970.AddState("Unarmed Idle Pose", new Vector3(310, 80, 0)); }
			lState_26748.speed = 1f;
			lState_26748.mirror = false;
			lState_26748.tag = "";
			lState_26748.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/MovementAnimsetPro/Animations/MovementAnimsetPro.fbx", "Idle_Pose");

			UnityEditor.Animations.AnimatorState lState_26750 = MotionControllerMotion.EditorFindState(lSSM_25970, "Rifle Idle Pose");
			if (lState_26750 == null) { lState_26750 = lSSM_25970.AddState("Rifle Idle Pose", new Vector3(360, 270, 0)); }
			lState_26750.speed = 1f;
			lState_26750.mirror = false;
			lState_26750.tag = "";
			lState_26750.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/RifleAnimsetPro/Animations/RifleAnimsetPro.fbx", "Rifle_Idle_Pose");

			UnityEditor.Animations.AnimatorState lState_26752 = MotionControllerMotion.EditorFindState(lSSM_25970, "PistolIdlePose");
			if (lState_26752 == null) { lState_26752 = lSSM_25970.AddState("PistolIdlePose", new Vector3(312, 396, 0)); }
			lState_26752.speed = 1f;
			lState_26752.mirror = false;
			lState_26752.tag = "";
			lState_26752.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/Animation/Kubold/PistolAnimsetPro/Animations/PistolAnimsetPro.fbx", "Pistol_Idle_Pose");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_26612 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_26748, 0);
			if (lAnyTransition_26612 == null) { lAnyTransition_26612 = lLayerStateMachine.AddAnyStateTransition(lState_26748); }
			lAnyTransition_26612.isExit = false;
			lAnyTransition_26612.hasExitTime = false;
			lAnyTransition_26612.hasFixedDuration = true;
			lAnyTransition_26612.exitTime = 0.75f;
			lAnyTransition_26612.duration = 0.1f;
			lAnyTransition_26612.offset = 0f;
			lAnyTransition_26612.mute = false;
			lAnyTransition_26612.solo = false;
			lAnyTransition_26612.canTransitionToSelf = true;
			lAnyTransition_26612.orderedInterruption = true;
			lAnyTransition_26612.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_26612.conditions.Length - 1; i >= 0; i--) { lAnyTransition_26612.RemoveCondition(lAnyTransition_26612.conditions[i]); }
			lAnyTransition_26612.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3000f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_26612.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_26612.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_26614 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_26748, 1);
			if (lAnyTransition_26614 == null) { lAnyTransition_26614 = lLayerStateMachine.AddAnyStateTransition(lState_26748); }
			lAnyTransition_26614.isExit = false;
			lAnyTransition_26614.hasExitTime = false;
			lAnyTransition_26614.hasFixedDuration = true;
			lAnyTransition_26614.exitTime = 0.75f;
			lAnyTransition_26614.duration = 0f;
			lAnyTransition_26614.offset = 0f;
			lAnyTransition_26614.mute = false;
			lAnyTransition_26614.solo = false;
			lAnyTransition_26614.canTransitionToSelf = true;
			lAnyTransition_26614.orderedInterruption = true;
			lAnyTransition_26614.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_26614.conditions.Length - 1; i >= 0; i--) { lAnyTransition_26614.RemoveCondition(lAnyTransition_26614.conditions[i]); }
			lAnyTransition_26614.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3000f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_26614.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_26614.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_26630 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_26750, 0);
			if (lAnyTransition_26630 == null) { lAnyTransition_26630 = lLayerStateMachine.AddAnyStateTransition(lState_26750); }
			lAnyTransition_26630.isExit = false;
			lAnyTransition_26630.hasExitTime = false;
			lAnyTransition_26630.hasFixedDuration = true;
			lAnyTransition_26630.exitTime = 0.75f;
			lAnyTransition_26630.duration = 0f;
			lAnyTransition_26630.offset = 0f;
			lAnyTransition_26630.mute = false;
			lAnyTransition_26630.solo = false;
			lAnyTransition_26630.canTransitionToSelf = false;
			lAnyTransition_26630.orderedInterruption = true;
			lAnyTransition_26630.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_26630.conditions.Length - 1; i >= 0; i--) { lAnyTransition_26630.RemoveCondition(lAnyTransition_26630.conditions[i]); }
			lAnyTransition_26630.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3000f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_26630.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 500f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_26630.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_26632 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_26750, 1);
			if (lAnyTransition_26632 == null) { lAnyTransition_26632 = lLayerStateMachine.AddAnyStateTransition(lState_26750); }
			lAnyTransition_26632.isExit = false;
			lAnyTransition_26632.hasExitTime = false;
			lAnyTransition_26632.hasFixedDuration = true;
			lAnyTransition_26632.exitTime = 0.75f;
			lAnyTransition_26632.duration = 0.25f;
			lAnyTransition_26632.offset = 0f;
			lAnyTransition_26632.mute = false;
			lAnyTransition_26632.solo = false;
			lAnyTransition_26632.canTransitionToSelf = true;
			lAnyTransition_26632.orderedInterruption = true;
			lAnyTransition_26632.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_26632.conditions.Length - 1; i >= 0; i--) { lAnyTransition_26632.RemoveCondition(lAnyTransition_26632.conditions[i]); }
			lAnyTransition_26632.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3000f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_26632.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 500f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_26632.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_26656 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_26752, 0);
			if (lAnyTransition_26656 == null) { lAnyTransition_26656 = lLayerStateMachine.AddAnyStateTransition(lState_26752); }
			lAnyTransition_26656.isExit = false;
			lAnyTransition_26656.hasExitTime = false;
			lAnyTransition_26656.hasFixedDuration = true;
			lAnyTransition_26656.exitTime = 0.75f;
			lAnyTransition_26656.duration = 0f;
			lAnyTransition_26656.offset = 0f;
			lAnyTransition_26656.mute = false;
			lAnyTransition_26656.solo = false;
			lAnyTransition_26656.canTransitionToSelf = true;
			lAnyTransition_26656.orderedInterruption = true;
			lAnyTransition_26656.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_26656.conditions.Length - 1; i >= 0; i--) { lAnyTransition_26656.RemoveCondition(lAnyTransition_26656.conditions[i]); }
			lAnyTransition_26656.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3000f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_26656.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 550f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_26656.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

			UnityEditor.Animations.AnimatorStateTransition lAnyTransition_26658 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_26752, 1);
			if (lAnyTransition_26658 == null) { lAnyTransition_26658 = lLayerStateMachine.AddAnyStateTransition(lState_26752); }
			lAnyTransition_26658.isExit = false;
			lAnyTransition_26658.hasExitTime = false;
			lAnyTransition_26658.hasFixedDuration = true;
			lAnyTransition_26658.exitTime = 0.75f;
			lAnyTransition_26658.duration = 0.25f;
			lAnyTransition_26658.offset = 0f;
			lAnyTransition_26658.mute = false;
			lAnyTransition_26658.solo = false;
			lAnyTransition_26658.canTransitionToSelf = true;
			lAnyTransition_26658.orderedInterruption = true;
			lAnyTransition_26658.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
			for (int i = lAnyTransition_26658.conditions.Length - 1; i >= 0; i--) { lAnyTransition_26658.RemoveCondition(lAnyTransition_26658.conditions[i]); }
			lAnyTransition_26658.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3000f, "L" + rLayerIndex + "MotionPhase");
			lAnyTransition_26658.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 550f, "L" + rLayerIndex + "MotionForm");
			lAnyTransition_26658.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");


			// Run any post processing after creating the state machine
			OnStateMachineCreated();
		}

#endif

		// ************************************ END AUTO GENERATED ************************************
		#endregion



	}
}
using com.ootii.Actors;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Combat;
using com.ootii.Actors.LifeCores;
using com.ootii.Cameras;
using com.ootii.Geometry;
using com.ootii.Graphics;
using com.ootii.Helpers;
using com.ootii.Input;
using com.ootii.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WildWalrus.Actors.Combat
{
	public delegate void CombatantTransformEvent(MCCombatant rCombatant, Transform rTransform);
	public delegate bool CombatantMotionEvent(MCCombatant rCombatant, MotionControllerMotion rMotion);
	public delegate void CombatantMessageEvent(MCCombatant rCombatant, CombatMessage rMessage);

	public class MCCombatant : MonoBehaviour, ICombatant
	{
		#region ICombatant
		
		[NonSerialized]
		public Transform _Transform = null;
		public Transform Transform
		{
			get { return _Transform; }
		}


		#region CombatOrigin

		public Vector3 CombatOrigin
		{
			get
			{
				Vector3 lOffset = _CombatOffset;
				Transform lTransform = transform;
				
				if (_CombatTransform != null)
				{
					if (_CombatTransformHeightOnly)
					{
						Vector3 lLocalPosition = lTransform.InverseTransformPoint(_CombatTransform.position);
						lOffset.y = lOffset.y + lLocalPosition.y;
					}
					else
					{
						lTransform = _CombatTransform;
					}
				}
				
				return lTransform.position + (lTransform.rotation * lOffset);
			}
		}
		
		
		public Vector3 _CombatOffset = new Vector3(0f, 0f, 0f);
		public Vector3 CombatOffset
		{
			get { return _CombatOffset; }
			set { _CombatOffset = value; }
		}
		
		
		public Transform _CombatTransform = null;
		public Transform CombatTransform
		{
			get { return _CombatTransform; }
			set { _CombatTransform = value; }
		}
		
		
		public bool _CombatTransformHeightOnly = true;
		public bool CombatTransformHeightOnly
		{
			get { return _CombatTransformHeightOnly; }
			set { _CombatTransformHeightOnly = value; }
		}

		#endregion CombatOrigin
		
		
		public float _MinMeleeReach = 0.1f;
		public float MinMeleeReach
		{
			get { return _MinMeleeReach; }
			set { _MinMeleeReach = value; }
		}
		
		
		public float _MaxMeleeReach = 0.75f;
		public float MaxMeleeReach
		{
			get { return _MaxMeleeReach; }
			set { _MaxMeleeReach = value; }
		}


		#region Target
		
		public Transform _Target = null;
		public Transform Target
		{
			get { return _Target; }
			
			set
			{
				if (_Target != value)
				{
					if (value == null)
					{
						if (_Target != null) { OnTargetUnlocked(_Target); }
						
						_Target = null;
						IsTargetLocked = false;
					}
					else
					{
						_Target = value;
						IsTargetLocked = true;
						
						OnTargetLocked(_Target);
					}
				}
			}
		}


		public bool _IsTargetLocked = false;
		public bool IsTargetLocked
		{
			get { return (_IsLockingEnabled && _IsTargetLocked && _Target != null); }
			set { _IsTargetLocked = value; }
		}
		
		
		public bool _ForceActorRotation = false;
		public bool ForceActorRotation
		{
			get { return _ForceActorRotation; }
			set { _ForceActorRotation = value; }
		}


		#region TargetLocking
		
		
		public bool _IsLockingEnabled = false;
		public bool IsLockingEnabled
		{
			get { return _IsLockingEnabled; }
			set { _IsLockingEnabled = value; }
		}
		
		
		public Texture _TargetLockedIcon = null;
		public Texture TargetLockedIcon
		{
			get { return _TargetLockedIcon; }
			set { _TargetLockedIcon = value; }
		}
		
		
		public string _ToggleCombatantLockAlias = "Combat Lock";
		public string ToggleCombatantLockAlias
		{
			get { return _ToggleCombatantLockAlias; }
			set { _ToggleCombatantLockAlias = value; }
		}
		
		
		public float _MaxLockDistance = 10f;
		public float MaxLockDistance
		{
			get { return _MaxLockDistance; }
			set { _MaxLockDistance = value; }
		}
		
		
		public bool _LockRequiresCombatant = true;
		public bool LockRequiresCombatant
		{
			get { return _LockRequiresCombatant; }
			set { _LockRequiresCombatant = value; }
		}
		
		
		public int _LockCameraMode = -1;
		public int LockCameraMode
		{
			get { return _LockCameraMode; }
			set { _LockCameraMode = value; }
		}
		
		
		public int _UnlockCameraMode = -1;
		public int UnlockCameraMode
		{
			get { return _UnlockCameraMode; }
			set { _UnlockCameraMode = value; }
		}
		
		
		public bool _ForceCameraRotation = false;
		public bool ForceCameraRotation
		{
			get { return _ForceCameraRotation; }
			set { _ForceCameraRotation = value; }
		}

		#endregion TargetLocking

		#endregion Target


		#region CombatInterfaces
		
		protected IWeaponCore mPrimaryWeapon = null;
		public IWeaponCore PrimaryWeapon
		{
			get { return mPrimaryWeapon; }
			set { mPrimaryWeapon = value; }
		}
		
		
		protected IWeaponCore mSecondaryWeapon = null;
		public IWeaponCore SecondaryWeapon
		{
			get { return mSecondaryWeapon; }
			set { mSecondaryWeapon = value; }
		}
		

		protected ICombatStyle mCombatStyle = null;
		public ICombatStyle CombatStyle
		{
			get { return mCombatStyle; }
			set { mCombatStyle = value; }
		}

		#endregion CombatInterfaces

		#endregion ICombatant


		#region Properties

		public string _ActorStances = "11,1,2,8";
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
					
					int lState = 0;
					string[] lStates = _ActorStances.Split(',');
					for (int i = 0; i < lStates.Length; i++)
					{
						if (int.TryParse(lStates[i], out lState))
						{
							if (!mActorStances.Contains(lState))
							{
								mActorStances.Add(lState);
							}
						}
					}
				}
			}
		}
		
		
		public bool _ShowDebug = false;
		public bool ShowDebug
		{
			get { return _ShowDebug; }
			set { _ShowDebug = value; }
		}

		#endregion Properties


		#region EventProperties

		public MessageEvent TargetLockedEvent = null;
		
		public MessageEvent TargetUnlockedEvent = null;
		
		[NonSerialized]
		public CombatantMotionEvent AttackActivated = null;
		
		[NonSerialized]
		public CombatantMessageEvent PreAttack = null;
		
		[NonSerialized]
		public CombatantMessageEvent PostAttack = null;
		
		[NonSerialized]
		public CombatantMessageEvent Attacked = null;
		
		[NonSerialized]
		public CombatantTransformEvent TargetLocked = null;
		
		[NonSerialized]
		public CombatantTransformEvent TargetUnlocked = null;

		#endregion EventProperties


		#region Members
		
		protected ActorController mActorController = null;
		
		protected MotionController mMotionController = null;
		
		protected IInputSource mInputSource = null;
		
		protected List<GameObject> mMeleeProspects = new List<GameObject>();
		
		protected float mMeleeProspectDelay = 0.25f;
		
		protected float mMeleeProspectTime = 0f;
		
		protected List<int> mActorStances = null;
		
		protected bool mIsRotationLocked = false;

		#endregion Members


		#region Monobehaviour
		
		protected void Start()
		{
			_Transform = transform;
			
			ActorStances = _ActorStances;
			
			mActorController = gameObject.GetComponent<ActorController>();
			
			mMotionController = gameObject.GetComponent<MotionController>();
			if (mMotionController != null) { mInputSource = mMotionController._InputSource; }
			
			// Register this combatant with the camera
			if (mMotionController != null && mMotionController.CameraRig is BaseCameraRig)
			{
				((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
				((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate += OnCameraUpdated;
			}
			
			if (ShowDebug) { CombatManager.ShowDebug = true; }
		}
		
		
		protected void OnDisable()
		{
			// Unregister this combatant with the camera
			if (mMotionController != null && mMotionController.CameraRig is BaseCameraRig)
			{
				((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
			}
		}
		
		
		protected virtual void Update()
		{
			if (mMotionController == null) { return; }
			
			if (mInputSource != null)
			{
				if (_IsLockingEnabled && _ToggleCombatantLockAlias.Length > 0)
				{
					if (mInputSource.IsJustPressed(_ToggleCombatantLockAlias))
					{
						if (IsTargetLocked)
						{
							Target = null;
						}
						else
						{
							if (mActorStances == null || mActorStances.Count == 0 || mActorStances.Contains(mMotionController.Stance))
							{
								Target = FindTarget();
							}
						}
					}
				}
			}
			
			// Unlock the target if our stance isn't valid
			if (_IsLockingEnabled && IsTargetLocked)
			{
				// Ensure our target is alive and able to be targeted
				if (_Target != null)
				{
					ActorCore lTargetActorCore = _Target.GetComponent<ActorCore>();
					if (lTargetActorCore != null && !lTargetActorCore.IsAlive)
					{
						IsTargetLocked = false;
						OnTargetUnlocked(_Target);
					}
				}
				
				// Ensure we're in a stance where targeting is valid
				if (mActorStances != null && mActorStances.Count > 0 && !mActorStances.Contains(mMotionController.Stance))
				{
					IsTargetLocked = false;
					OnTargetUnlocked(_Target);
				}
				
				// Finally, force the rotations as needed
				if (IsTargetLocked)
				{
					if (_ForceActorRotation) { RotateActorToTarget(_Target, 360f); }
					if (_ForceCameraRotation && mMotionController.CameraRig == null) { RotateCameraToTarget(_Target, 360f); }
				}
			}
		}

		#endregion Monobehaviour


		#region TargetFunctions
		
		public virtual Transform FindTarget(string rTag = null)
		{
			if (mMotionController == null) { return null; }
			
			float lMaxRadius = 8f;
			float lMaxDistance = 20f;
			float lRevolutions = 2f;
			float lDegreesPerStep = 27f;
			float lSteps = lRevolutions * (360f / lDegreesPerStep);
			float lRadiusPerStep = lMaxRadius / lSteps;
			
			float lAngle = 0f;
			float lRadius = 0f;
			Vector3 lPosition = Vector3.zero;
			
			Transform lTarget = null;
			
			// We want our final revolution to be max radius. So, increase the steps
			lSteps = lSteps + (360f / lDegreesPerStep) - 1f;
			
			// Start at the center and spiral out
			int lCount = 0;
			for (lCount = 0; lCount < lSteps; lCount++)
			{
				lPosition.x = lRadius * Mathf.Cos(lAngle * Mathf.Deg2Rad);
				lPosition.y = lRadius * Mathf.Sin(lAngle * Mathf.Deg2Rad);
				lPosition.z = lMaxDistance;
				
				//GraphicsManager.DrawLine(mMotionController.CameraTransform.position, mMotionController.CameraTransform.TransformPoint(lPosition), (lCount == 0 ? Color.red : lColor), null, 5f);
				
				RaycastHit lHitInfo;
				Vector3 lDirection = (mMotionController.CameraTransform.TransformPoint(lPosition) - mMotionController.CameraTransform.position).normalized;
				if (RaycastExt.SafeRaycast(mMotionController.CameraTransform.position, lDirection, out lHitInfo, _MaxLockDistance, -1, _Transform))
				{
					// Grab the gameobject this collider belongs to
					GameObject lGameObject = lHitInfo.collider.gameObject;
					
					// Don't count the ignore
					if (lGameObject.transform == mMotionController.CameraTransform) { continue; }
					if (lHitInfo.collider is TerrainCollider) { continue; }
					
					// Determine if the combatant has the appropriate tag
					if (rTag != null && rTag.Length > 0)
					{
						if (lGameObject.CompareTag(rTag))
						{
							lTarget = lGameObject.transform;
							break;
						}
					}
					
					// We only care about combatants we'll enage with
					ICombatant lCombatant = lGameObject.GetComponent<ICombatant>();
					if (lCombatant != null)
					{
						lTarget = lGameObject.transform;
						break;
					}
					
					// We can do a catch-all if a combatant isn't required
					if (lTarget == null && !_LockRequiresCombatant)
					{
						lTarget = lGameObject.transform;
					}
				}
				
				// Increment the spiral
				lAngle += lDegreesPerStep;
				lRadius = Mathf.Min(lRadius + lRadiusPerStep, lMaxRadius);
			}
			
			// Return the target hit
			return lTarget;
		}
		

		public virtual int QueryCombatTargets(AttackStyle rAttackStyle, IWeaponCore rWeapon, List<CombatTarget> rCombatTargets, Transform rIgnore)
		{
			IWeaponCore lWeapon = (rWeapon != null ? rWeapon : mPrimaryWeapon);
			
			CombatFilter lFilter = new CombatFilter(rAttackStyle);
			lFilter.MinDistance = (rAttackStyle.MinRange > 0f ? rAttackStyle.MinRange : _MinMeleeReach + lWeapon.MinRange);
			lFilter.MaxDistance = (rAttackStyle.MaxRange > 0f ? rAttackStyle.MaxRange : _MaxMeleeReach + lWeapon.MaxRange);
			
			rCombatTargets.Clear();
			int lTargetCount = CombatManager.QueryCombatTargets(_Transform, CombatOrigin, lFilter, rCombatTargets, rIgnore);
			
			return lTargetCount;
		}
		
		
		public virtual int QueryCombatTargets(AttackStyle rStyle, List<CombatTarget> rCombatTargets)
		{
			return QueryCombatTargets(rStyle, null, rCombatTargets, _Transform);
		}
		
		
		protected virtual void RotateActorToTarget(Transform rTarget, float rSpeed)
		{
			// Get the forward looking direction
			Vector3 lForward = (rTarget.position - mActorController._Transform.position).normalized;
			
			// We do the inverse tilt so we calculate the rotation in "natural up" space vs. "actor up" space.
			Quaternion lInvTilt = QuaternionExt.FromToRotation(mActorController._Transform.up, Vector3.up);
			
			// Character's forward direction of the actor in "natural up"
			Vector3 lActorForward = lInvTilt * mActorController._Transform.forward;
			
			// Target forward in "natural up"
			Vector3 lTargetForward = lInvTilt * lForward;
			
			// Ensure we don't exceed our rotation speed
			float lActorToTargetAngle = NumberHelper.GetHorizontalAngle(lActorForward, lTargetForward);
			if (rSpeed > 0f && Mathf.Abs(lActorToTargetAngle) > rSpeed * Time.deltaTime)
			{
				lActorToTargetAngle = Mathf.Sign(lActorToTargetAngle) * rSpeed * Time.deltaTime;
			}
			
			// Add the rotation to our character
			Quaternion lRotation = Quaternion.AngleAxis(lActorToTargetAngle, Vector3.up);
			
			if (mActorController._UseTransformPosition && mActorController._UseTransformRotation)
			{
				_Transform.rotation = _Transform.rotation * lRotation;
			}
			else
			{
				mActorController.Rotate(lRotation, Quaternion.identity);
			}
		}
		
		
		protected void RotateCameraToTarget(Transform rTarget, float rSpeed = 0)
		{
			if (rTarget == null) { return; }
			if (mMotionController == null) { return; }
			
			float lSpeed = (rSpeed > 0f ? rSpeed : 360f);
			
			if (mMotionController.CameraRig != null)
			{
				Vector3 lTargetPosition = rTarget.position;
				
				Combatant lTargetCombatant = rTarget.GetComponent<Combatant>();
				if (lTargetCombatant != null) { lTargetPosition = lTargetCombatant.CombatOrigin; }
				
				Vector3 lForward = (lTargetPosition - mMotionController.CameraRig.Transform.position).normalized;
				mMotionController.CameraRig.SetTargetForward(lForward, lSpeed);
			}
			else if (mMotionController._CameraTransform != null)
			{
				Vector3 lNewPosition = mMotionController._Transform.position + (mMotionController._Transform.rotation * mMotionController.RootMotionMovement);
				Vector3 lForward = (rTarget.position - lNewPosition).normalized;
				mMotionController._CameraTransform.rotation = Quaternion.LookRotation(lForward, mMotionController._CameraTransform.up);
			}
		}
		
		#endregion TargetFunctions


		#region Events
		
		public virtual bool OnAttackActivated(MotionControllerMotion rMotion)
		{
			//com.ootii.Utilities.Debug.Log.FileWrite(_Transform.name + ".OnAttackActivated(" + rMotion.GetType().Name + ")");
			
			if (AttackActivated != null) { return AttackActivated(this, rMotion); }
			
			return true;
		}
		
		
		public virtual void OnPreAttack(CombatMessage rMessage)
		{
			//com.ootii.Utilities.Debug.Log.FileWrite(_Transform.name + ".OnPreAttack()");
			
			if (PreAttack != null) { PreAttack(this, rMessage); }
			
			if (mMotionController != null) { mMotionController.SendMessage(rMessage); }
		}
		
		
		public virtual void OnPostAttack(CombatMessage rMessage)
		{
			//com.ootii.Utilities.Debug.Log.FileWrite(_Transform.name + ".OnPostAttack()");
			
			if (PostAttack != null) { PostAttack(this, rMessage); }
			
			if (mMotionController != null) { mMotionController.SendMessage(rMessage); }
			
			// If the defender was killed, release the target
			if (_Target != null && rMessage.ID == CombatMessage.MSG_DEFENDER_KILLED)
			{
				IsTargetLocked = false;
				OnTargetUnlocked(_Target);
			}
		}
		
		
		public virtual void OnAttacked(CombatMessage rMessage)
		{
			//com.ootii.Utilities.Debug.Log.FileWrite(_Transform.name + ".OnAttacked()");
			
			if (Attacked != null) { Attacked(this, rMessage); }
			
			if (rMessage.ID != CombatMessage.MSG_DEFENDER_ATTACKED) { return; }
			
			ActorCore lDefenderCore = gameObject.GetComponent<ActorCore>();
			if (lDefenderCore != null)
			{
				lDefenderCore.SendMessage(rMessage);
			}
			else
			{
				// Check if this defender is blocking, parrying, etc
				if (mMotionController != null) { mMotionController.SendMessage(rMessage); }
				
				// Determine if we're continuing with the attack and apply damage
				if (rMessage.ID == CombatMessage.MSG_DEFENDER_ATTACKED)
				{
					IDamageable lDamageable = gameObject.GetComponent<IDamageable>();
					if (lDamageable != null)
					{
						lDamageable.OnDamaged(rMessage);
					}
					else
					{
						rMessage.ID = CombatMessage.MSG_DEFENDER_DAMAGED;
						if (mMotionController != null) { mMotionController.SendMessage(rMessage); }
					}
				}
				
				// Disable this combatant
				if (rMessage.ID == CombatMessage.MSG_DEFENDER_KILLED)
				{
					this.enabled = false;
				}
			}
		}
		
		
		protected virtual void OnTargetLocked(Transform rTransform)
		{
			if (_LockCameraMode >= 0)
			{
				if (mMotionController != null && mMotionController.CameraRig != null)
				{
					mMotionController.CameraRig.Mode = _LockCameraMode;
				}
			}
			
			if (TargetLocked != null) { TargetLocked(this, rTransform); }
			
			// Send the message
			CombatMessage lMessage = CombatMessage.Allocate();
			lMessage.ID = EnumMessageID.MSG_COMBAT_ATTACKER_TARGET_LOCKED;
			lMessage.Attacker = gameObject;
			lMessage.Defender = _Target.gameObject;
			
			if (TargetLockedEvent != null)
			{
				TargetLockedEvent.Invoke(lMessage);
			}
			
			CombatMessage.Release(lMessage);
		}
		
		
		protected virtual void OnTargetUnlocked(Transform rTransform)
		{
			// Ensure we release the camera
			if (_ForceCameraRotation && mMotionController.CameraRig != null)
			{
				mMotionController.CameraRig.ClearTargetForward();
			}
			
			// Change the mode if needed
			if (_UnlockCameraMode >= 0)
			{
				if (mMotionController != null && mMotionController.CameraRig != null)
				{
					mMotionController.CameraRig.Mode = _UnlockCameraMode;
				}
			}
			
			// Report the unlock
			if (TargetUnlocked != null) { TargetUnlocked(this, rTransform); }
			
			// Send the message
			CombatMessage lMessage = CombatMessage.Allocate();
			lMessage.ID = EnumMessageID.MSG_COMBAT_ATTACKER_TARGET_UNLOCKED;
			lMessage.Attacker = gameObject;
			lMessage.Defender = rTransform.gameObject;
			
			if (TargetUnlockedEvent != null)
			{
				TargetUnlockedEvent.Invoke(lMessage);
			}
			
			CombatMessage.Release(lMessage);
		}
		
		
		private void OnCameraUpdated(float rDeltaTime, int rUpdateIndex, BaseCameraRig rCamera)
		{
			if (!_ForceCameraRotation) { return; }
			if (!IsTargetLocked) { return; }
			
			float lSpeed = 360f;
			Vector3 lTargetPosition = _Target.position;
			
			Combatant lTargetCombatant = _Target.GetComponent<Combatant>();
			if (lTargetCombatant != null) { lTargetPosition = lTargetCombatant.CombatOrigin; }
			
			Vector3 lForward = (lTargetPosition - mMotionController.CameraRig.Transform.position).normalized;
			mMotionController.CameraRig.SetTargetForward(lForward, lSpeed);
		}
		
		#endregion Events


		#region GUIFunctions
		
		protected virtual void OnGUI()
		{
			if (_IsLockingEnabled && IsTargetLocked)
			{
				DrawTargetIcon();
			}
		}
		
		
		private void DrawTargetIcon()
		{
			if (!_IsLockingEnabled || !IsTargetLocked) { return; }
			if (_TargetLockedIcon == null) { return; }
			
			Vector3 lPosition = Vector3.zero;
			
			Combatant lCombatant = _Target.GetComponent<Combatant>();
			if (lCombatant != null)
			{
				lPosition = lCombatant.CombatOrigin;
			}
			else
			{
				lPosition = _Target.position + (_Target.up * 1.6f);
			}
			
			GraphicsManager.DrawTexture(_TargetLockedIcon, lPosition, 32f, 32f);
		}

		#endregion GUIFunctions


		#region EditorFunctions
		
#if UNITY_EDITOR
		
		public bool EditorShowEvents = false;
		
		
		void Reset()
		{
			if (_CombatTransform == null)
			{
				Animator lAnimator = gameObject.GetComponent<Animator>();
				if (lAnimator != null)
				{
					_CombatTransform = lAnimator.GetBoneTransform(HumanBodyBones.Chest);
					_CombatOffset = new Vector3(0f, 0.25f, 0f);
				}
			}
			
			if (_CombatTransform == null)
			{
				_CombatOffset = new Vector3(0f, 1.4f, 0f);
			}
		}
		
#endif
		
		#endregion EditorFunctions
	
	}
}
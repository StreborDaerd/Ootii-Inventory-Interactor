using com.ootii.Actors.Combat;
using com.ootii.Actors.LifeCores;
using com.ootii.Cameras;
using com.ootii.Geometry;
using com.ootii.Graphics;
using com.ootii.Items;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WildWalrus.Items
{
	public class MCGunCore : ItemCore, IWeaponCore, IItem
	{
		#region ItemCore
		
		public override GameObject Owner
		{
			get { return mOwner; }
			set { mOwner = value; }
		}

		#endregion ItemCore


		#region Variables

		public Transform _MuzzleTransform = null;
		public Transform MuzzleTransform
		{
			get { return _MuzzleTransform; }
			set { _MuzzleTransform = value; }
		}
		

		public AudioClip _FailAudio = null;
		public AudioClip FailAudio
		{
			get { return _FailAudio; }
			set { _FailAudio = value; }
		}
		
		
		protected AudioSource mAudioSource = null;
		public AudioSource AudioSource
		{
			get { return mAudioSource; }
			set { mAudioSource = value; }
		}
		
		
		public bool _ShowDebug = false;
		public bool ShowDebug
		{
			get { return _ShowDebug; }
			set { _ShowDebug = value; }
		}
		
		
		protected bool mIsActive = false;
		public bool IsActive
		{
			get { return mIsActive; }
			set { mIsActive = value; }
		}
		
		
		public bool HasColliders
		{
			get { return false; }
		}
		
		
		public virtual bool CanFire
		{
			get
			{
				if (mMagazine != null && mMagazine.Quantity <= 0) { return false; }
				return true;
			}
		}
		
		
		public virtual int AmmoCount
		{
			get
			{
				if (mMagazine != null) { return mMagazine.Quantity; }
				return 0;
			}
		}
		
		
		protected float mLastFired = 0f;
		public float LastFired
		{
			get { return mLastFired; }
		}
		
		
		protected int mFireCount = 0;
		public int FireCount
		{
			get { return mFireCount; }
		}
		
		
		protected float mFireDelay = 0f;
		
		
		protected float mLastSpreadDiameter = 0.5f;
		
		
		protected CombatHit mLastImpact = CombatHit.EMPTY;

		#endregion Variables


		#region Statistics

		public float _MinRange = 0f;
		public virtual float MinRange
		{
			get { return _MinRange; }
			set { _MinRange = value; }
		}
		
		
		public float _MaxRange = 1f;
		public virtual float MaxRange
		{
			get { return _MaxRange; }
			set { _MaxRange = value; }
		}
		

		public bool _IsAutomatic = true;
		public virtual bool IsAutomatic
		{
			get { return _IsAutomatic; }
			set { _IsAutomatic = value; }
		}
		
		
		public int _RoundsPerMinute = 700;
		public virtual int RoundsPerMinute
		{
			get { return _RoundsPerMinute; }
			
			set
			{
				_RoundsPerMinute = value;
				mFireDelay = (_RoundsPerMinute == 0 ? 0f : 1f / (_RoundsPerMinute / 60f));
			}
		}
		
		
		public float _MinImpactPower = 1f;
		public virtual float MinImpactPower
		{
			get { return (mBullet != null ? mBullet.MinImpactPower : _MinImpactPower); }
			set { _MinImpactPower = value; }
		}
		
		
		public float _MaxImpactPower = 1f;
		public virtual float MaxImpactPower
		{
			get { return (mBullet != null ? mBullet.MaxImpactPower : _MaxImpactPower); }
			set { _MaxImpactPower = value; }
		}
		
		
		public float _MinSpread = 0.2f;
		public float MinSpread
		{
			get { return _MinSpread; }
			set { _MinSpread = value; }
		}
		
		
		public float _MaxSpread = 3f;
		public float MaxSpread
		{
			get { return _MaxSpread; }
			set { _MaxSpread = value; }
		}
		
		
		public float _RecoilYaw = 0.12f;
		public float RecoilYaw
		{
			get { return _RecoilYaw; }
			set { _RecoilYaw = value; }
		}
		
		
		public float _RecoilPitch = -0.32f;
		public float RecoilPitch
		{
			get { return _RecoilPitch; }
			set { _RecoilPitch = value; }
		}
		
		
		public float _RecoilFirstMultiplier = 2.2f;
		public float RecoilFirstMultiplier
		{
			get { return _RecoilFirstMultiplier; }
			set { _RecoilFirstMultiplier = value; }
		}

		#endregion Statistics


		#region WeaponComponents
		
		[SerializeField]
		protected IGunHandle mHandle = null;
		public IGunHandle Handle
		{
			get { return mHandle; }
			set { mHandle = value; }
		}
		
		
		[SerializeField]
		protected IGunScope mScope = null;
		public IGunScope Scope
		{
			get { return mScope; }
			set { mScope = value; }
		}
		
		
		[SerializeField]
		protected IGunMagazine mMagazine = null;
		public IGunMagazine Magazine
		{
			get { return mMagazine; }
			set { mMagazine = value; }
		}
		
		
		[SerializeField]
		protected IGunMuzzle mMuzzle = null;
		public IGunMuzzle Muzzle
		{
			get { return mMuzzle; }
			set { mMuzzle = value; }
		}
		
		
		[SerializeField]
		protected IBullet mBullet = null;
		public IBullet Bullet
		{
			get { return mBullet; }
			set { mBullet = value; }
		}

		#endregion WeaponComponents


		protected virtual void Start()
		{
			// Grab the audio source
			mAudioSource = gameObject.GetComponent<AudioSource>();
			
			// Grab any core components
			IItemComponent[] lComponents = gameObject.GetComponents<IItemComponent>();
			for (int i = 0; i < lComponents.Length; i++)
			{
				// Find the handle
				if (mHandle == null && lComponents[i].IsEnabled && lComponents[i] is IGunHandle)
				{
					mHandle = lComponents[i] as IGunHandle;
				}
				
				// Find the scope
				if (mScope == null && lComponents[i].IsEnabled && lComponents[i] is IGunScope)
				{
					mScope = lComponents[i] as IGunScope;
				}
				
				// Find the magazine
				if (mMagazine == null && lComponents[i].IsEnabled && lComponents[i] is IGunMagazine)
				{
					mMagazine = lComponents[i] as IGunMagazine;
				}
				
				// Find the muzzle
				if (mMuzzle == null && lComponents[i].IsEnabled && lComponents[i] is IGunMuzzle)
				{
					mMuzzle = lComponents[i] as IGunMuzzle;
				}
				
				// Find the bullets
				if (mBullet == null && lComponents[i].IsEnabled && lComponents[i] is IBullet)
				{
					mBullet = lComponents[i] as IBullet;
				}
			}
			
			// Gather some stats
			mFireDelay = (_RoundsPerMinute == 0 ? 0f : 1f / (_RoundsPerMinute / 60f));
		}
		
		
		protected override void OnEnable()
		{
			base.OnEnable();
			
			mLastSpreadDiameter = GetFinalSpread(mFireCount);
		}
		

		public virtual float GetAttackDamage(float rPercent = 1f, float rMultiplier = 1f)
		{
			if (mBullet != null) { return (mBullet.MinDamage + ((mBullet.MaxDamage - mBullet.MinDamage) * rPercent)) * rMultiplier; }
			return 0f;
		}
		
		
		public virtual float GetAttackImpactPower(float rPercent = 1f, float rMultiplier = 1f)
		{
			if (mBullet != null) { return (mBullet.MinImpactPower + ((mBullet.MaxImpactPower - mBullet.MinImpactPower) * rPercent)) * rMultiplier; }
			return 0f;
		}
		
		
		public int Reload(int rQuantity)
		{
			int lTake = 0;
			
			if (mMagazine != null)
			{
				lTake = Mathf.Min(mMagazine.Capacity - mMagazine.Quantity, rQuantity);
				mMagazine.Quantity = mMagazine.Quantity + lTake;
			}
			
			return lTake;
		}
		
		
		public void FailFiring()
		{
			if (mAudioSource != null && _FailAudio != null)
			{
				mAudioSource.PlayOneShot(_FailAudio);
			}
		}
		
		
		public bool StartFiring()
		{
			//Debug.Log("GunCore.StartFiring()");
			
			mFireCount = 0;
			mIsActive = true;
			
			// Conditions that stop us from firing
			if (!CanFire) { mIsActive = false; }
			
			// Return the result
			return mIsActive;
		}
		
		
		public void StopFiring()
		{
			//Debug.Log("GunCore.StopFiring()");
			
			mFireCount = 0;
			mIsActive = false;
			
			mLastSpreadDiameter = GetFinalSpread(0);
		}
		
		
		public virtual bool UpdateFireWindow(Transform rOrigin = null, IBaseCameraRig rCameraRig = null)
		{
			if (rOrigin == null) { rOrigin = _MuzzleTransform; }
			if (rOrigin == null) { rOrigin = gameObject.transform; }
			
			// Update each of the components
			if (mScope != null && mScope.IsEnabled) { mScope.UpdateComponent(this); }
			if (mHandle != null && mHandle.IsEnabled) { mHandle.UpdateComponent(this); }
			if (mMagazine != null && mMagazine.IsEnabled) { mMagazine.UpdateComponent(this); }
			if (mMuzzle != null && mMuzzle.IsEnabled) { mMuzzle.UpdateComponent(this); }
			
			// Determine if when to fire the weapon
			if (mIsActive)
			{
				if (mLastFired + mFireDelay + (mFireCount == 1 ? 0.05f : 0f) < Time.time)
				{
					Fire(rOrigin, rCameraRig);
					
					if (!IsAutomatic)
					{
						StopFiring();
					}
				}
			}
			
			if (_ShowDebug)
			{
				GraphicsManager.DrawSolidCone(rOrigin.position, rOrigin.forward, _MaxRange, mLastSpreadDiameter * 0.5f, Color.cyan, rDuration : 0.5f);
			}
			
			return mIsActive;
		}
		
		
		public virtual void Fire(Transform rOrigin, IBaseCameraRig rCameraRig = null)
		{
			//Debug.Log("GunCore.Fire()");
			
			// Update the stats
			mFireCount = mFireCount + 1;
			mLastFired = Time.time;
			
			// Allow the muzzle to do it's work
			if (mMuzzle != null)
			{
				mMuzzle.OnFired(mAudioSource);
			}
			
			// Determine the shot center. We'll expand from there
			Vector3 lShotCenter = rOrigin.position + (rOrigin.forward * MaxRange);
			if (_ShowDebug)
			{
				GraphicsManager.DrawLine(rOrigin.position, rOrigin.forward * MaxRange, Color.red, null, 2f);
			}

			// Determine the resulting spread
			mLastSpreadDiameter = GetFinalSpread(mFireCount);
			
			// Using uniformly distributed spreading.
			float lSpreadRadius = mLastSpreadDiameter * 0.5f;
			lSpreadRadius = Mathf.Sqrt(Random.Range(0f, lSpreadRadius * lSpreadRadius));
			
			// Using the spread, determine the ray target the bullet will follow
			float lAngle = Random.Range(0f, 1f) * Mathf.PI * 2f;
			Vector3 lOffset = new Vector3(Mathf.Cos(lAngle) * lSpreadRadius, Mathf.Sin(lAngle) * lSpreadRadius, 0f);
			Vector3 lRayEnd = lShotCenter + rOrigin.TransformDirection(lOffset);
			
			// Fire the actual bullets and look for a hit
			RaycastHit lHitInfo;
			
			int lLayers = -1;
			if (mBullet != null) { lLayers = mBullet.CollisionLayers; }
			
			// Fire from the camera first
			Vector3 lFireDirection = (lRayEnd - rOrigin.position).normalized;
			if (RaycastExt.SafeRaycast(rOrigin.position, lRayEnd - rOrigin.position, out lHitInfo, MaxRange, lLayers, mOwner.transform))
			{
				// Camera based impact
				mLastImpact.Index = mFireCount;
				mLastImpact.Collider = lHitInfo.collider;
				mLastImpact.Point = lHitInfo.point;
				mLastImpact.Normal = lHitInfo.normal;
				mLastImpact.Vector = rOrigin.forward;
				mLastImpact.Distance = lHitInfo.distance;
				
				// With the hit point doe to the camera ray, we now need to test the ray from our muzzle
				Vector3 lHitDirection = (lHitInfo.point - _MuzzleTransform.position).normalized;
				
				// Ensure the impact didn't happen behind the muzzle due to camera placement
				float lHitDirectionDot = Vector3.Dot(lFireDirection, lHitDirection);
				if (lHitDirectionDot > 0f)
				{
					if (RaycastExt.SafeRaycast(_MuzzleTransform.position, lHitDirection, out lHitInfo, lHitDirection.magnitude * 1.05f, lLayers, mOwner.transform))
					{
						mLastImpact.Collider = lHitInfo.collider;
						mLastImpact.Point = lHitInfo.point;
						mLastImpact.Normal = lHitInfo.normal;
						mLastImpact.Vector = _MuzzleTransform.forward;
						mLastImpact.Distance = lHitInfo.distance;
					}
					
					if (_ShowDebug)
					{
						GraphicsManager.DrawLine(_MuzzleTransform.position, lHitInfo.point, Color.yellow, null, 2f);
					}
					
					// Report the impact
					OnImpact(mLastImpact);
				}
			}
			// There was no hit from the camera, but we still need to check a short distance from the muzzle
			else
			{
				// With the hit point doe to the camera ray, we now need to test the ray from our muzzle
				Vector3 lHitDirection = _MuzzleTransform.forward;
				if (RaycastExt.SafeRaycast(_MuzzleTransform.position, lHitDirection.normalized, out lHitInfo, 0.5f, lLayers, mOwner.transform))
				{
					mLastImpact.Collider = lHitInfo.collider;
					mLastImpact.Point = lHitInfo.point;
					mLastImpact.Normal = lHitInfo.normal;
					mLastImpact.Vector = _MuzzleTransform.forward;
					mLastImpact.Distance = lHitInfo.distance;
					
					// Report the impact
					OnImpact(mLastImpact);
				}
				
				if (_ShowDebug)
				{
					GraphicsManager.DrawLine(_MuzzleTransform.position, _MuzzleTransform.position + (_MuzzleTransform.forward * 0.5f), Color.yellow, null, 2f);
				}
			}
			
			// Apply an recoil
			ApplyRecoil(rOrigin, rCameraRig);
			
			// Reduce the ammo count
			if (mMagazine != null) { mMagazine.Quantity = Mathf.Max(mMagazine.Quantity - 1, 0); }
			if (mMagazine == null || mMagazine.Quantity <= 0)
			{
				mIsActive = false;
			}
		}
		
		
		protected virtual void OnImpact(CombatHit rHitInfo, ICombatStyle rAttackStyle = null)
		{
			// Extract out information about the hit
			Transform lHitTransform = GetClosestTransform(rHitInfo.Point, rHitInfo.Collider.transform);
			Vector3 lHitDirection = Quaternion.Inverse(lHitTransform.rotation) * (rHitInfo.Point - lHitTransform.position).normalized;
			
			// Put together the combat info. This will will be modified over time
			CombatMessage lMessage = CombatMessage.Allocate();
			lMessage.Attacker = mOwner;
			lMessage.Defender = rHitInfo.Collider.gameObject;
			lMessage.Weapon = this;
			lMessage.Damage = GetAttackDamage(1f, (rAttackStyle != null ? rAttackStyle.DamageModifier : 1f));
			lMessage.ImpactPower = GetAttackImpactPower();
			lMessage.HitPoint = rHitInfo.Point;
			lMessage.HitNormal = rHitInfo.Normal;
			lMessage.HitDirection = lHitDirection;
			lMessage.HitVector = rHitInfo.Vector;
			lMessage.HitTransform = lHitTransform;
			
			// Allow the bullet to show its impact effects and sounds
			if (mBullet != null)
			{
				mBullet.OnImpact(lMessage);
			}
			
			// Grab cores for processing
			ActorCore lAttackerCore = (mOwner != null ? mOwner.GetComponent<ActorCore>() : null);
			ActorCore lDefenderCore = rHitInfo.Collider.gameObject.GetComponent<ActorCore>();
			
			// Pre-Attack
			lMessage.ID = CombatMessage.MSG_ATTACKER_ATTACKED;
			
			if (lAttackerCore != null)
			{
				lAttackerCore.SendMessage(lMessage);
			}
			
			// Attack Defender
			lMessage.ID = CombatMessage.MSG_DEFENDER_ATTACKED;
			
			if (lDefenderCore != null)
			{
				ICombatant lDefenderCombatant = rHitInfo.Collider.gameObject.GetComponent<ICombatant>();
				if (lDefenderCombatant != null)
				{
					lMessage.HitDirection = Quaternion.Inverse(lDefenderCore.Transform.rotation) * (rHitInfo.Point - lDefenderCombatant.CombatOrigin).normalized;
				}
				
				lDefenderCore.SendMessage(lMessage);
			}
			else
			{
				lMessage.HitDirection = Quaternion.Inverse(lHitTransform.rotation) * (rHitInfo.Point - lHitTransform.position).normalized;
				
				IDamageable lDefenderDamageable = rHitInfo.Collider.gameObject.GetComponent<IDamageable>();
				if (lDefenderDamageable != null)
				{
					lDefenderDamageable.OnDamaged(lMessage);
				}
				
				Rigidbody lRigidBody = rHitInfo.Collider.gameObject.GetComponent<Rigidbody>();
				if (lRigidBody != null)
				{
					lRigidBody.AddForceAtPosition(rHitInfo.Vector * lMessage.ImpactPower, rHitInfo.Point, ForceMode.Impulse);
				}
			}
			
			// Attacker response
			if (lAttackerCore != null)
			{
				lAttackerCore.SendMessage(lMessage);
			}
			
			// Release the combatant to the pool
			CombatMessage.Release(lMessage);
		}
		
		
		public override void OnEquipped()
		{
			if (mScope == null) { mScope = gameObject.GetComponent<IGunScope>(); }
			if (mScope != null) { mScope.OnEquipped(this); }
			
			if (mHandle == null) { mHandle = gameObject.GetComponent<IGunHandle>(); }
			if (mHandle != null) { mHandle.OnEquipped(this); }
			
			if (mMagazine == null) { mMagazine = gameObject.GetComponent<IGunMagazine>(); }
			if (mMagazine != null) { mMagazine.OnEquipped(this); }
			
			if (mMuzzle == null) { mMuzzle = gameObject.GetComponent<IGunMuzzle>(); }
			if (mMuzzle != null) { mMuzzle.OnEquipped(this); }
		}
		
		
		public override void OnStored()
		{
			if (mScope != null) { mScope.OnStored(this); }
		}
		
		
		protected virtual float GetFinalSpread(float rFireCount)
		{
			float lAccuracy = 0f;
			if (mHandle != null) { lAccuracy = lAccuracy + mHandle.Accuracy; }
			if (mMuzzle != null) { lAccuracy = lAccuracy + mMuzzle.Accuracy; }
			if (mScope != null) { lAccuracy = lAccuracy + mScope.Accuracy; }
			
			float lRandomAccuracy = Random.Range(0.75f, 1f);
			
			// Determine the spread based on constant fire
			float lFireSpread = 0.01f;
			float lCountSpread = 0.01f;
			if (rFireCount > 1)
			{
				float lRoundsPerSecond = (_RoundsPerMinute / 60f);
				lCountSpread = rFireCount / lRoundsPerSecond;
				
				lFireSpread = Mathf.Clamp(lCountSpread, 0.01f, 1.0f);
			}
			
			// Accuracy determines how much the spread changes over time
			float lAccuracySpread = lFireSpread * lRandomAccuracy * (1f - Mathf.Clamp01(lAccuracy));
			
			// Apply that to our spread limits
			float lFinalSpread = Mathf.Lerp(MinSpread, MaxSpread, lAccuracySpread);
			
			return lFinalSpread;
		}
		
		
		protected virtual float GetFinalRecoil(float rFireCount)
		{
			float lStability = 0f;
			if (mHandle != null) { lStability = lStability + mHandle.Stability; }
			if (mMuzzle != null) { lStability = lStability + mMuzzle.Stability; }
			if (mScope != null) { lStability = lStability + mScope.Stability; }
			
			lStability = (1f - Mathf.Clamp01(lStability)) * (rFireCount == 1 ? RecoilFirstMultiplier : 1f);
			
			return lStability;
		}
		
		
		protected virtual void ApplyRecoil(Transform rOrigin, IBaseCameraRig rCameraRig = null)
		{
			float lRecoil = GetFinalRecoil(mFireCount);
			
			float lYaw = RecoilYaw * lRecoil;
			float lPitch = RecoilPitch * lRecoil;
			
			if (rCameraRig != null && rCameraRig.Anchor != null)
			{
				Quaternion lLocalRotation = Quaternion.Inverse(rCameraRig.Anchor.rotation) * rCameraRig.Transform.rotation;
				
				float lLocalYaw = lLocalRotation.eulerAngles.y;
				if (lLocalYaw > 180f) { lLocalYaw = lLocalYaw - 360f; }
				else if (lLocalYaw < -180f) { lLocalYaw = lLocalYaw + 360f; }
				
				float lLocalPitch = lLocalRotation.eulerAngles.x;
				if (lLocalPitch > 180f) { lLocalPitch = lLocalPitch - 360f; }
				else if (lLocalPitch < -180f) { lLocalPitch = lLocalPitch + 360f; }
				
				rCameraRig.SetTargetYawPitch(lLocalYaw + lYaw, lLocalPitch + lPitch, 720f, true);
			}
			else
			{
				float lLocalYaw = rOrigin.rotation.eulerAngles.y;
				if (lLocalYaw > 180f) { lLocalYaw = lLocalYaw - 360f; }
				else if (lLocalYaw < -180f) { lLocalYaw = lLocalYaw + 360f; }
				
				float lLocalPitch = rOrigin.rotation.eulerAngles.x;
				if (lLocalPitch > 180f) { lLocalPitch = lLocalPitch - 360f; }
				else if (lLocalPitch < -180f) { lLocalPitch = lLocalPitch + 360f; }
				
				if (lLocalPitch < -65f || lLocalPitch > 65f) { lPitch = 0f; }
				if (lLocalYaw < -65f || lLocalYaw > 65f) { lYaw = 0f; }
				
				rOrigin.rotation = rOrigin.rotation * Quaternion.Euler(lPitch, lYaw, 0f);
			}
		}
		
		
		public virtual Transform GetClosestTransform(Vector3 rPosition, Transform rCollider)
		{
			// Find the anchor's root transform
			Transform lActorTransform = rCollider;
			
			// Grab the closest body transform
			float lMinDistance = float.MaxValue;
			Transform lMinTransform = lActorTransform;
			GetClosestTransform(rPosition, lActorTransform, ref lMinDistance, ref lMinTransform);
			
			// Return it
			return lMinTransform;
		}
		
		
		protected virtual void GetClosestTransform(Vector3 rPosition, Transform rTransform, ref float rMinDistance, ref Transform rMinTransform)
		{
			// Limit what we'll connect to
			if (rTransform.childCount > 20) { return; }
			if (!rTransform.gameObject.activeInHierarchy) { return; }
			if (rTransform.name.Contains("connector")) { return; }
			if (rTransform.gameObject.GetComponent<IWeaponCore>() != null) { return; }
			
			// If this transform is closer to the hit position, use it
			float lDistance = Vector3.Distance(rPosition, rTransform.position);
			if (lDistance < rMinDistance)
			{
				rMinDistance = lDistance;
				rMinTransform = rTransform;
			}
			
			// Check if any child transform is closer to the hit position
			for (int i = 0; i < rTransform.childCount; i++)
			{
				GetClosestTransform(rPosition, rTransform.GetChild(i), ref rMinDistance, ref rMinTransform);
			}
		}
		
		
		protected bool IsDescendant(Transform rParent, Transform rDescendant)
		{
			if (rParent == null) { return false; }
			
			Transform lDescendantParent = rDescendant;
			while (lDescendantParent != null)
			{
				if (lDescendantParent == rParent) { return true; }
				lDescendantParent = lDescendantParent.parent;
			}
			
			return false;
		}

	}
}
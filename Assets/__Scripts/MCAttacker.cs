using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Combat;
using com.ootii.Actors.Inventory;
using com.ootii.Actors.LifeCores;
using com.ootii.Helpers;
using com.ootii.Reactors;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WildWalrus.Actors.AnimationControllers;
using WildWalrus.Input;

namespace WildWalrus
{
	public class MCAttacker : MonoBehaviour
	{

		#region States

		public const int IDLE = 0;
		public const int MOVING = 1;
		public const int EQUIPPING = 2;
		public const int ATTACKING = 3;
		public const int BLOCKING = 4;
		public const int REACTING = 5;
		public const int KILLED = 10;

		#endregion NPCStates


		#region Components

		[SerializeField] ActorCore mTargetActorCore;

		[SerializeField] ActorCore mActorCore;

		[SerializeField] MotionController mMotionController;

		[SerializeField] BasicInventory mInventory;

		[SerializeField] MCNavMeshInputSource MCNavMeshInputSource;

		#endregion Components


		#region Properties
		
		public bool Move = true;

		//public float MovementSpeed = 1.9f;

		public bool Rotate = true;

		//public float RotationSpeed = 360f;

		public float AttackRangeMax = 100f;

		public bool UseGun = true;
		
		public GameObject _Target = null;
		public GameObject Target
		{
			get { return _Target; }
			
			set
			{
				_Target = value;
				
				if (_Target == null)
				{
					mTargetActorCore = null;
				}
				else
				{
					mTargetActorCore = _Target.GetComponent<ActorCore>();
				}
			}
		}

		#endregion Properties


		#region MonoBehaviour

		void Awake()
		{
			if (mActorCore == null) { mActorCore = gameObject.GetComponent<ActorCore>(); }
			//if (mActorCore != null) { mActorCore.SetStateValue("State", 0); }
			if (mMotionController == null) { mMotionController = gameObject.GetComponent<MotionController>(); }
			if (mInventory == null) { mInventory = gameObject.GetComponent<BasicInventory>(); }
			if (_Target != null) { Target = _Target; }
		}


		public void OnStart()
		{
			//MCNavMeshInputSource.TargetPosition = TargetPosition.Value;
			if (mActorCore != null) { mActorCore.SetStateValue("State", 0); }
			MCNavMeshInputSource.Target = Target.transform;

			MCNavMeshInputSource.OnStart();
		}
		
		
		public bool OnUpdate()
		{
			if (!mActorCore.IsAlive) { return true; }
			
			DetermineTarget();
			
			int lState = mActorCore.GetStateValue("State");
			if (lState == IDLE || lState == MOVING)
			{
				DetermineWeapon();
			}
			
			lState = mActorCore.GetStateValue("State");
			if (lState == IDLE || lState == BLOCKING || lState == MOVING)
			{
			}
			
			lState = mActorCore.GetStateValue("State");
			if (lState == IDLE || lState == MOVING)
			{
				DetermineAttack(2f, 5f, 10f);
			}
			
			lState = mActorCore.GetStateValue("State");
			if (lState == IDLE || lState == MOVING || lState == ATTACKING)
			{
				if (Rotate)
				{
					RotateToTarget();
				}
				
				if (Move)
				{
					MoveToTarget(1f * (lState == MOVING ? 1f : 1.5f));
				}
			}

			return false;
		}

		#endregion MonoBehaviour


		#region Functions

		public void DetermineTarget()
		{
			if (mTargetActorCore != null && !mTargetActorCore.IsAlive)
			{
				BasicShooterAttack1 lAttack = mMotionController.GetActiveMotion(1) as BasicShooterAttack1;
				if (lAttack != null)
				{
					lAttack.Deactivate();
					mActorCore.SetStateValue("State", IDLE);
				}
				
				Target = null;
			}
		}
		
		
		public void DetermineWeapon()
		{
			if (_Target == null)
			{
				if (mInventory.ActiveWeaponSet != 0)
				{
					mInventory.ToggleWeaponSet(0);
					mActorCore.SetStateValue("State", EQUIPPING);
				}
			}
			else
			{
				Vector3 lToTarget = _Target.transform.position - transform.position;
				float lToTargetDistance = lToTarget.magnitude;

				if (UseGun && lToTargetDistance > 2f)
				{
					if (mInventory.ActiveWeaponSet != 3)
					{
						mInventory.ToggleWeaponSet(3);
						mActorCore.SetStateValue("State", EQUIPPING);
					}
				}
			}
		}
		
		
		public void DetermineAttack(float rMeleeMax, float rRangedMin, float rRangedMax)
		{
			if (_Target == null) { return; }
			
			Vector3 lToTarget = _Target.transform.position - transform.position;
			float lToTargetDistance = lToTarget.magnitude;
			
			int lStance = mActorCore.GetStateValue("Stance");
			if (UseGun && lStance == EnumControllerStance.COMBAT_SHOOTING)
			{
				if (lToTargetDistance > rRangedMin && lToTargetDistance < rRangedMax)
				{
					BasicShooterAttack1 lAttack = mMotionController.GetMotion<BasicShooterAttack1>();
					if (lAttack != null && !lAttack.IsActive)
					{
						mMotionController.ActivateMotion(lAttack);
						mActorCore.SetStateValue("State", ATTACKING);
					}
				}
			}
		}
		
		
		public void RotateToTarget()
		{
			if (_Target == null) { return; }

			//Vector3 lToTarget = _Target.transform.position - transform.position;
			//Vector3 lToTargetDirection = lToTarget.normalized;

			//float lToTargetAngle = NumberHelper.GetHorizontalAngle(transform.forward, lToTargetDirection, transform.up);
			//if (lToTargetAngle != 0f)
			//{
			//	float lRotationSpeed = Mathf.Sign(lToTargetAngle) * Mathf.Min(RotationSpeed * Time.deltaTime, Mathf.Abs(lToTargetAngle));
			//	transform.rotation = transform.rotation * Quaternion.AngleAxis(lRotationSpeed, transform.up);
			//}

			MCNavMeshInputSource.RotateToTarget();
		}
		
		
		public void MoveToTarget(float rDistance)
		{
			if (_Target == null) { return; }
			
			//Vector3 lToTarget = _Target.transform.position - transform.position;
			//Vector3 lToTargetDirection = lToTarget.normalized;
			//float lToTargetDistance = lToTarget.magnitude;
			
			//if (lToTargetDistance > rDistance)
			if(MCNavMeshInputSource.OnUpdate())
			{
				if (mActorCore.GetStateValue("State") == IDLE) { mActorCore.SetStateValue("State", MOVING); }
				
				//float lSpeed = Mathf.Min(MovementSpeed * Time.deltaTime, lToTargetDistance);
				//transform.position = transform.position + (lToTargetDirection * lSpeed);

			}
			else
			{
				mActorCore.SetStateValue("State", IDLE);
			}
		}

		#endregion Functions


		#region Events
		
		public void OnWeaponSetEquipped(ReactorAction rAction)
		{
			mActorCore.SetStateValue("State", IDLE);
		}
		
		
		public void OnPreAttack(ReactorAction rAction)
		{
			mActorCore.SetStateValue("State", ATTACKING);
			
			if (rAction == null || rAction.Message == null) { return; }
		}
		
		
		public void OnPostAttack(ReactorAction rAction)
		{
			mActorCore.SetStateValue("State", IDLE);
			
			if (rAction == null || rAction.Message == null) { return; }
		}
		
		
		public void OnDamaged(ReactorAction rAction)
		{
			// This is primarily done to clear any block that is up
			mActorCore.SetStateValue("State", IDLE);
		}
		
		
		public void OnKilled(ReactorAction rAction)
		{
			if (rAction.Message is CombatMessage)
			{
				if (((CombatMessage)rAction.Message).Defender == gameObject)
				{
					mActorCore.SetStateValue("State", KILLED);
				}
			}
		}
		
		#endregion Events

	}
}
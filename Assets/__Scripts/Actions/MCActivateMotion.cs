using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using com.ootii.Actors.AnimationControllers;
using UnityEngine;
using UnityEngine.AI;

namespace WildWalrus.BehaviorDesigner.Actions
{
	[TaskName("MC Activate Motion")]
	[TaskCategory("ootii/Motion Controller")]
	[TaskDescription("Forces a specific motion to activate.")]
	public class MCActivateMotion : Action
	{
		private static string[] CompletionList = new string[] { "Immediate", "Activated", "Deactivated", "Motion Time", "State Time" };


		#region Properties

		public SharedString Motion = "";
		
		public SharedInt LayerIndex = 0;

		public SharedInt MotionParameter = 0;

		public int CompletionIndex = 2;
		
		public string ExitState = "";
		
		public float ExitTime = 0f;
		
		public bool TestActivateRequired = false;
		
		public bool AllowMotionMovement = true;

		#endregion Properties


		#region Members
		
		private int mExitStateID = 0;
		
		private MotionControllerMotion mMotion = null;
		
		private bool mIsActive = false;
		
		private MotionController mMotionController = null;

		#endregion Members


		#region ActionFunctions
		
		public override void OnAwake()
		{
			base.OnAwake();
			
			GameObject lGameObject = GetDefaultGameObject(null);
			mMotionController = lGameObject.GetComponentInParent<MotionController>();
			
			mMotion = mMotionController.GetMotion(LayerIndex.Value, Motion.Value);
		}
		
		
		public override void OnStart()
		{
			base.OnStart();
			
			// Grab the state as needed
			if (ExitState.Length > 0)
			{
				mExitStateID = mMotionController.AddAnimatorName(ExitState);
			}
			
			if (mMotionController != null && mMotion != null)
			{
				mIsActive = mMotion.IsActive;
				if (!mIsActive && !mMotion.QueueActivation)
				{
					bool lIsValid = true;
					
					if (TestActivateRequired) { lIsValid = mMotion.TestActivate(); }
					
					if (lIsValid)
					{
						if (AllowMotionMovement) { EnableMotionMovement(true); }
						mIsActive = false;
						//mMotion.Parameter = MotionParameter.Value;
						// = MotionParameter.Value;
						//Debug.Log("OnStart " + mMotion.Parameter + " " + mMotion.Phase);

						mMotionController.ActivateMotion(mMotion);
						mMotionController.SetAnimatorMotionParameter(LayerIndex.Value, MotionParameter.Value);
					}
				}
			}
		}
		
		
		public override TaskStatus OnUpdate()
		{
			//mMotion.Parameter = MotionParameter.Value;
			mMotionController.SetAnimatorMotionParameter(LayerIndex.Value, MotionParameter.Value);

			// Determine if we're active yet. This allows us to test if the
			// motion has even started
			if (!mIsActive && mMotion.IsActive)
			{
				mIsActive = true;
			}
			
			// If the motion is done, flag the action as done
			if (mMotion == null)
			{
				if (AllowMotionMovement) { EnableMotionMovement(false); }
				
				return TaskStatus.Failure;
			}
			// If we're not waiting or we've completed, call it done
			else if (CompletionIndex == 0 || (mIsActive && !mMotion.IsActive))
			{
				if (AllowMotionMovement) { EnableMotionMovement(false); }
				
				mIsActive = false;
				return TaskStatus.Success;
			}
			// Exit on activate
			else if (CompletionIndex == 1)
			{
				if (mIsActive && mMotion.IsActive)
				{
					if (AllowMotionMovement) { EnableMotionMovement(false); }
					
					mIsActive = false;
					return TaskStatus.Success;
				}
			}
			// Exit on deactivate
			else if (CompletionIndex == 2)
			{
				if (mIsActive && !mMotion.IsActive)
				{
					if (AllowMotionMovement) { EnableMotionMovement(false); }
					
					mIsActive = false;
					return TaskStatus.Success;
				}
			}
			// Exit on age
			else if (CompletionIndex == 3)
			{
				if (mIsActive && mMotion.Age > ExitTime)
				{
					if (AllowMotionMovement) { EnableMotionMovement(false); }
					
					mIsActive = false;
					return TaskStatus.Success;
				}
			}
			// Exit on state
			else if (CompletionIndex == 4)
			{
				if (mIsActive && (mExitStateID == 0 || mMotion.MotionLayer._AnimatorStateID == mExitStateID))
				{
					if (mMotion.MotionLayer._AnimatorStateNormalizedTime > ExitTime)
					{
						if (AllowMotionMovement) { EnableMotionMovement(false); }
						
						mIsActive = false;
						return TaskStatus.Success;
					}
				}
			}
			Debug.Log("OnUpdate Parameter " + mMotion.Parameter + " Phase " + mMotion.Phase);

			return TaskStatus.Running;
		}

		#endregion ActionFunctions
		
		
		protected void EnableMotionMovement(bool rEnable)
		{
			GameObject currentGameObject = GetDefaultGameObject(null);
			NavMeshAgent lNavMeshAgent = currentGameObject.GetComponentInParent<NavMeshAgent>();
			
			if (lNavMeshAgent != null)
			{
				lNavMeshAgent.updatePosition = !rEnable;
				lNavMeshAgent.updateRotation = !rEnable;
				
				if (!rEnable) { lNavMeshAgent.Warp(mMotionController._Transform.position); }
			}
			
			mMotionController.ActorController.UseTransformPosition = !rEnable;
		}
	
	}
}
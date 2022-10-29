using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Navigation;
using com.ootii.Helpers;
using Utilities = com.ootii.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace WildWalrus.BehaviorDesigner.Actions
{
	[TaskName("MC Navigate To")]
	[TaskCategory("ootii/Motion Controller")]
	[TaskDescription("Uses a nav mesh agent to navigate the actor to the specified target or position. This action supports walking, running, jumping, climbing, and dropping.")]
	public class MCNavigateTo : Action
	{
		#region Variables

		private static float EPSILON = 0.005f;
		
		public bool UseNavMeshAgentPosition = true;
		
		public SharedTransform Target = null;
		
		public SharedVector3 TargetPosition;
		
		public bool IsFocusEnabled = false;
		
		public SharedTransform FocusTarget;
		
		public SharedFloat StopDistance = 0.25f;
		
		public SharedFloat DefaultNormalizedSpeed = 1f;
		
		public SharedFloat SlowDistance = 2.0f;
		
		public SharedFloat SlowNormalizedSpeed = 0.5f;
		
		public bool ForceRotation = true;
		
		public SharedFloat VoxelSize = 0.166f;
		
		
		protected NavMeshAgent mNavMeshAgent = null;
		
		protected MotionController mMotionController = null;
		
		protected bool mHasArrived = true;
		
		protected Vector3 mDestination = Vector3.zero;
		
		protected Vector3 mFocusVector = Vector3.zero;
		
		protected float mFocusAngle = 0f;
		
		protected Vector3 mWaypoint = Vector3.zero;
		
		protected Vector3 mWaypointDirection = Vector3.zero;
		
		protected float mWaypointDistance = 0f;
		
		protected float mWaypointNormalizedSpeed = 1f;
		
		protected bool mFirstPathSet = false;
		protected bool mFirstPathValid = false;
		
		private OffMeshLinkDriver mOffMeshLinkDriver = null;

		#endregion Variables

		
		#region Functions
		
		public override void OnAwake()
		{
			base.OnAwake();
			
			GameObject lGameObject = GetDefaultGameObject(null);
			mMotionController = lGameObject.GetComponentInParent<MotionController>();
			
			mNavMeshAgent = lGameObject.GetComponent<NavMeshAgent>();
		}
		
		
		public override void OnStart()
		{
			mHasArrived = false;
			mFirstPathSet = false;
			mFirstPathValid = false;
			mDestination = Vector3.zero;
			
			// Determine where we are moving to
			Vector3 lTargetPosition = TargetPosition.Value;
			if (Target.Value != null) { lTargetPosition = Target.Value.position; }
			
			Vector3 lTargetDirection = lTargetPosition - mMotionController._Transform.position;
			float lTargetDistance = lTargetDirection.magnitude;
			
			// Determine what we're looking at
			bool lIsFocusEnabled = IsFocusEnabled && !UseNavMeshAgentPosition;
			if (lIsFocusEnabled && FocusTarget != null && FocusTarget.Value != null)
			{
				mFocusVector = FocusTarget.Value.position - mMotionController._Transform.position;
			}
			else
			{
				lIsFocusEnabled = false;
				mFocusVector = lTargetDirection.normalized;
			}
			
			// If we're using the focus target, check out status
			if (lIsFocusEnabled)
			{
				mFocusAngle = NumberHelper.GetHorizontalAngle(mMotionController._Transform.forward, mFocusVector.normalized, mMotionController._Transform.up);
				mFocusAngle = Mathf.Sign(mFocusAngle) * Mathf.Min(Mathf.Abs(mFocusAngle), mNavMeshAgent.angularSpeed * Time.deltaTime);
				
				if (lTargetDistance < StopDistance.Value && Mathf.Abs(mFocusAngle) < 0.5f)
				{
					ClearTarget();
				}
			}
			else if (!UseNavMeshAgentPosition)
			{
				if (lTargetDistance < StopDistance.Value)
				{
					ClearTarget();
				}
			}
			else if (lTargetDistance < mNavMeshAgent.stoppingDistance)
			{
				ClearTarget();
			}
		}
		
		public override TaskStatus OnUpdate()
		{
			bool lIsDone = false;
			
			if (UseNavMeshAgentPosition)
			{
				lIsDone = OnUpdate_NMA();
			}
			else
			{
				lIsDone = OnUpdate_MC();
			}
			
			return (lIsDone ? TaskStatus.Success : TaskStatus.Running);
		}
		
		
		protected virtual bool OnUpdate_NMA()
		{
			// Determine the destination
			Vector3 lDestination = mDestination;
			
			if (Target.Value != null)
			{
				lDestination = Target.Value.position;
			}
			else
			{
				lDestination = TargetPosition.Value;
			}
			
			// Determine if we're at the destination
			Vector3 lToTarget = mDestination - mNavMeshAgent.transform.position;
			
			// Check if we've arrived
			float lDistance = lToTarget.magnitude - EPSILON;
			if (lDistance <= mNavMeshAgent.stoppingDistance)
			{
				mHasArrived = true;
				mDestination.x = float.MinValue;
				
				return true;
			}
			// Process the movement
			else
			{
				// If we're on a link with no driver, create one
				if (mNavMeshAgent.isOnOffMeshLink)
				{
					if (mOffMeshLinkDriver == null)
					{
						mOffMeshLinkDriver = mNavMeshAgent.gameObject.AddComponent<OffMeshLinkDriver>();
					}
				}
				
				if (mOffMeshLinkDriver != null)
				{
					if (mOffMeshLinkDriver.HasCompleted)
					{
						Component.Destroy(mOffMeshLinkDriver);
						mOffMeshLinkDriver = null;
					}
				}
			}
			
			// Set the new target destination. We do it at the end so that
			// we can process the current path before changing it
			SetDestination(lDestination);
			
			return false;
		}
		
		
		protected virtual bool OnUpdate_MC()
		{
			Utilities.Debug.Log.FileWrite("");
			
			// If we're on a link with no driver, create one
			if (mNavMeshAgent.isOnOffMeshLink)
			{
				if (mOffMeshLinkDriver == null)
				{
					mMotionController.ClearTarget();
					mMotionController._ActorController.UseTransformPosition = true;
					
					mOffMeshLinkDriver = mNavMeshAgent.gameObject.AddComponent<OffMeshLinkDriver>();
				}
			}
			
			// Determine if the driver has finished
			if (mOffMeshLinkDriver != null)
			{
				if (mOffMeshLinkDriver.HasCompleted)
				{
					mNavMeshAgent.updatePosition = false;
					mNavMeshAgent.updateRotation = false;
					mMotionController._ActorController.UseTransformPosition = false;
					
					Component.Destroy(mOffMeshLinkDriver);
					mOffMeshLinkDriver = null;
				}
			}
			
			// If we don't have a driver, continue normally
			if (mOffMeshLinkDriver == null)
			{
				// Get the latest destination. This can change, so we grab each update
				Vector3 lDestination = TargetPosition.Value;
				if (Target != null && Target.Value != null) { lDestination = Target.Value.position; }
				
				// Determine if we're at the destination
				Vector3 lTargetDirection = lDestination - mMotionController._Transform.position;
				float lTargetDistance = lTargetDirection.magnitude;
				
				// Determine what we're looking at
				if (IsFocusEnabled && FocusTarget != null && FocusTarget.Value != null)
				{
					mFocusVector = (FocusTarget.Value.position - mMotionController._Transform.position).normalized;
				}
				else
				{
					if (mWaypointDirection.sqrMagnitude > 0f)
					{
						mFocusVector = mWaypointDirection;
					}
					else
					{
						mFocusVector = lTargetDirection.normalized;
					}
				}
				
				// If we're close enough and facing the focus... we can stop
				mFocusAngle = NumberHelper.GetHorizontalAngle(mMotionController._Transform.forward, mFocusVector.normalized, mMotionController._Transform.up);
				mFocusAngle = Mathf.Sign(mFocusAngle) * Mathf.Min(Mathf.Abs(mFocusAngle), mNavMeshAgent.angularSpeed * Time.deltaTime);
				
				// If we've arrived, stop
				if (mHasArrived || (lTargetDistance < StopDistance.Value && (!IsFocusEnabled || Mathf.Abs(mFocusAngle) < 0.5f)))
				{
					ClearTarget();
					return true;
				}
				
				// Check if our first path is set and done
				if (mFirstPathSet && mNavMeshAgent.hasPath && !mNavMeshAgent.pathPending)
				{
					mFirstPathValid = true;
				}
				
				// Determine the next move
				if (!mHasArrived && mFirstPathValid)
				{
					// Update the way point to our next position if we have a path
					if (mNavMeshAgent.hasPath && !mNavMeshAgent.pathPending)
					{
						mWaypoint = mNavMeshAgent.steeringTarget - (mNavMeshAgent.transform.up * (VoxelSize.Value * 0.5f));
						
						Vector3 lVerticalDelta = Vector3.Project(mWaypoint - mMotionController._Transform.position, mMotionController._Transform.up);
						if (lVerticalDelta.sqrMagnitude < 0.1f) { mWaypoint = mWaypoint - lVerticalDelta; }
					}
					
					// Move towards the current waypoint
					Vector3 lWaypointVector = mWaypoint - mMotionController._Transform.position;
					mWaypointDistance = lWaypointVector.magnitude;
					mWaypointDirection = lWaypointVector.normalized;
					
					mWaypointNormalizedSpeed = (SlowDistance.Value > 0f
						&& lTargetDistance < SlowDistance.Value ? SlowNormalizedSpeed.Value : DefaultNormalizedSpeed.Value);
					mMotionController.SetTargetPosition(mWaypoint, mWaypointNormalizedSpeed);
					
					Utilities.Debug.Log.FileWrite("SetTargetPosition spd:" + mWaypointNormalizedSpeed.ToString("f3"));
					
					if (IsFocusEnabled)
					{
						Quaternion lRotation = mMotionController._Transform.rotation * Quaternion.AngleAxis(mFocusAngle, Vector3.up);
						mMotionController.SetTargetRotation(lRotation);
					}
					else if (ForceRotation && mWaypointDirection.sqrMagnitude > 0f)
					{
						float lAngle = NumberHelper.GetHorizontalAngle(mMotionController._Transform.forward, mWaypointDirection, mMotionController._Transform.up);
						lAngle = Mathf.Sign(lAngle) * Mathf.Min(Mathf.Abs(lAngle), mNavMeshAgent.angularSpeed * Time.deltaTime);
						
						Quaternion lRotation = mMotionController._Transform.rotation * Quaternion.AngleAxis(lAngle, Vector3.up);
						mMotionController.SetTargetRotation(lRotation);
					}
				}
				
				// Force the agent to stay with our actor. This way, the path is
				// alway relative to our current position. Then, we can use the AC
				// to move to a valid position.
				mNavMeshAgent.nextPosition = mMotionController._Transform.position;
				
				// Set the new target destination. We do it at the end so that
				// we can process the current path before changing it
				SetDestination(lDestination);
			}
			
			return false;
		}
		
		
		protected virtual void SetDestination(Vector3 rDestination)
		{
			if (mHasArrived) { return; }
			
			// Don't re-run the path finding if we're fine
			if (mNavMeshAgent.hasPath || mNavMeshAgent.pathPending)
			{
				if (mDestination == rDestination) { return; }
			}
			
			// Recalculate the path
			if (!mNavMeshAgent.pathPending)
			{
				// Ensure we've shut down the NMA (if needed)
				if (!UseNavMeshAgentPosition)
				{
					mNavMeshAgent.updatePosition = false;
					mNavMeshAgent.updateRotation = false;
					mNavMeshAgent.stoppingDistance = StopDistance.Value;
				}
				
				// Set the next destination
				mDestination = rDestination;
				
				mNavMeshAgent.ResetPath();
				mNavMeshAgent.SetDestination(mDestination);
				
				mFirstPathSet = true;
			}
		}
		
		
		protected void ClearTarget()
		{
			Utilities.Debug.Log.FileWrite("ClearTarget()");
			
			mHasArrived = true;
			mFirstPathSet = false;
			mFirstPathValid = false;
			
			mMotionController.ClearTarget();
			mNavMeshAgent.isStopped = true;
		}
		
		#endregion Functions

	}
}
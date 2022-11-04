using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Navigation;
using Utilities = com.ootii.Utilities;
using UnityEngine;
using UnityEngine.AI;
using com.ootii.Helpers;

namespace WildWalrus.Input
{
	public class MCNavMeshInputSource : MonoBehaviour
	{

		[SerializeField]
		public NavMeshAgent mNavMeshAgent;// = null;

		[SerializeField]
		protected MotionController mMotionController;// = null;

		private OffMeshLinkDriver mOffMeshLinkDriver = null;

		public Transform Target = null;

		public Transform FocusTarget;

		public Vector3 TargetPosition;

		public bool IsFocusEnabled = false;

		public float StopDistance = 0.25f;

		public float DefaultNormalizedSpeed = 1f;

		public float SlowDistance = 2.0f;

		public float SlowNormalizedSpeed = 0.5f;

		public bool ForceRotation = true;

		public float VoxelSize = 0.166f;



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

		[SerializeField] public Transform TargetVisualisation;
		//bool mNavMeshAgentNextPositionSet;

		public void OnStart()
		{
			mHasArrived = false;
			mFirstPathSet = false;
			mFirstPathValid = false;
			mDestination = Vector3.zero;


			mNavMeshAgent.nextPosition = mMotionController._Transform.position;

			// Determine where we are moving to
			Vector3 lTargetPosition = TargetPosition;
			if (Target != null) { lTargetPosition = Target.position; }

			Vector3 lTargetDirection = lTargetPosition - mMotionController._Transform.position;
			float lTargetDistance = lTargetDirection.magnitude;

			// Determine what we're looking at
			bool lIsFocusEnabled = IsFocusEnabled;
			if (lIsFocusEnabled && FocusTarget != null && FocusTarget != null)
			{
				mFocusVector = FocusTarget.position - mMotionController._Transform.position;
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

				if (lTargetDistance < StopDistance && Mathf.Abs(mFocusAngle) < 0.5f)
				{
					ClearTarget();
				}
			}
			else
			{
				if (lTargetDistance < StopDistance)
				{
					ClearTarget();
				}
			}

		}

		private void LateUpdate()
		{
			//if(!mNavMeshAgentNextPositionSet)
			{
				mNavMeshAgent.nextPosition = mMotionController._Transform.position;
			}
		}

		public bool OnUpdate()
		{
			//Debug.Log("MCNavMeshInputSource OnUpdate");
			Utilities.Debug.Log.FileWrite("");
			//mNavMeshAgentNextPositionSet = false;
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
				Vector3 lDestination = TargetPosition;
				if (Target != null && Target != null) { lDestination = Target.position; }

				// Determine if we're at the destination
				Vector3 lTargetDirection = lDestination - mMotionController._Transform.position;
				float lTargetDistance = lTargetDirection.magnitude;

				// Determine what we're looking at
				if (IsFocusEnabled && FocusTarget != null && FocusTarget!= null)
				{
					mFocusVector = (FocusTarget.position - mMotionController._Transform.position).normalized;
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
				if (mHasArrived || (lTargetDistance < StopDistance && (!IsFocusEnabled || Mathf.Abs(mFocusAngle) < 0.5f)))
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
						mWaypoint = mNavMeshAgent.steeringTarget - (mNavMeshAgent.transform.up * (VoxelSize * 0.5f));

						Vector3 lVerticalDelta = Vector3.Project(mWaypoint - mMotionController._Transform.position, mMotionController._Transform.up);
						if (lVerticalDelta.sqrMagnitude < 0.1f) { mWaypoint = mWaypoint - lVerticalDelta; }
					}

					// Move towards the current waypoint
					Vector3 lWaypointVector = mWaypoint - mMotionController._Transform.position;
					mWaypointDistance = lWaypointVector.magnitude;
					mWaypointDirection = lWaypointVector.normalized;

					mWaypointNormalizedSpeed = (SlowDistance > 0f
						&& lTargetDistance < SlowDistance ? SlowNormalizedSpeed : DefaultNormalizedSpeed);
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
				//mNavMeshAgentNextPositionSet = true;
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
				//if (!UseNavMeshAgentPosition)
				{
					mNavMeshAgent.updatePosition = false;
					mNavMeshAgent.updateRotation = false;
					mNavMeshAgent.stoppingDistance = StopDistance;
				}

				// Set the next destination
				mDestination = rDestination;

				mNavMeshAgent.ResetPath();
				mNavMeshAgent.SetDestination(mDestination);

				mFirstPathSet = true;
			}
		}


		public void ClearTarget()
		{
			//Utilities.Debug.Log.FileWrite("ClearTarget()");
			//Debug.Log("MCNavMeshInputSource ClearTarget()");
			mHasArrived = true;
			mFirstPathSet = false;
			mFirstPathValid = false;

			mMotionController.ClearTarget();
			mNavMeshAgent.isStopped = true;
			mNavMeshAgent.nextPosition = mMotionController._Transform.position;
			//mNavMeshAgent.speed = 0f;
			//mNavMeshAgent.
			//mMotionController.GetMotion()
		}


		public void RotateToTarget()
		{
			Vector3 lDestination = TargetPosition;
			if (Target != null && Target != null) { lDestination = Target.position; }

			// Determine if we're at the destination
			Vector3 lTargetDirection = lDestination - mMotionController._Transform.position;

			if (lTargetDirection.sqrMagnitude < 0.005f)
			{
				return;
			}

			lTargetDirection = lTargetDirection.normalized;

			float lAngle = NumberHelper.GetHorizontalAngle(mMotionController._Transform.forward, lTargetDirection, mMotionController._Transform.up);
			//should actually use the rotation speed of the active motion but dificult to do
			lAngle = Mathf.Sign(lAngle) * Mathf.Min(Mathf.Abs(lAngle), mNavMeshAgent.angularSpeed * Time.deltaTime);

			Quaternion lRotation = mMotionController._Transform.rotation * Quaternion.AngleAxis(lAngle, Vector3.up);
			mMotionController.SetTargetRotation(lRotation);
		}
	}
}
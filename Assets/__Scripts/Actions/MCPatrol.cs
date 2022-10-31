using BehaviorDesigner.Runtime.Tasks;
using BehaviorDesigner.Runtime;
using Tooltip = BehaviorDesigner.Runtime.Tasks.TooltipAttribute;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WildWalrus.Input;

namespace WildWalrus.BehaviorDesigner.Actions
{
	[TaskName("MC Patrol")]
	[TaskDescription("Patrol around the specified waypoints using the Unity NavMesh and Motion Controller.")]
	[TaskCategory("ootii/Motion Controller")]
	[TaskIcon("Assets/Behavior Designer Movement/Editor/Icons/{SkinColor}PatrolIcon.png")]
	public class MCPatrol : Action
	{
		[SerializeField] Transform[] PatrolWaypoints;
		[SerializeField] MCNavMeshInputSource MCNavMeshInputSource;
		[SerializeField] bool randomWaypointOrder = false;
		int currentWaypoint = -1;

		public override void OnStart()
		{
			SetNextWaypoint();
			MCNavMeshInputSource.OnStart();
		}

		public override TaskStatus OnUpdate()
		{
			if (MCNavMeshInputSource.OnUpdate())
			{
				SetNextWaypoint();
			}

			return TaskStatus.Running;

		}

		void SetNextWaypoint()
		{
			if (randomWaypointOrder)
			{
				int random;
				do
				{
					random = Random.Range(0, PatrolWaypoints.Length);
				}
				while (currentWaypoint == random);

				currentWaypoint = random;

			}
			else
			{
				currentWaypoint = (currentWaypoint + 1) % PatrolWaypoints.Length;
			}

			MCNavMeshInputSource.TargetPosition = PatrolWaypoints[currentWaypoint].position;
			MCNavMeshInputSource.Target = null;
			MCNavMeshInputSource.OnStart();
		}

			/*
			[Tooltip("Should the agent patrol the waypoints randomly?")]
			public SharedBool randomPatrol = false;
			[Tooltip("The length of time that the agent should pause when arriving at a waypoint")]
			public SharedFloat waypointPauseDuration = 0;
			[Tooltip("The waypoints to move to")]
			public SharedGameObjectList waypoints;

			// The current index that we are heading towards within the waypoints array
			private int waypointIndex;
			private float waypointReachedTime;

			public override void OnStart()
			{

				base.OnStart();
				// initially move towards the closest waypoint
				float distance = Mathf.Infinity;
				float localDistance;
				for (int i = 0; i < waypoints.Value.Count; ++i)
				{
					if ((localDistance = Vector3.Magnitude(transform.position - waypoints.Value[i].transform.position)) < distance)
					{
						distance = localDistance;
						waypointIndex = i;
					}
				}

				waypointReachedTime = -1;
				SetDestination(WayPoint());
			}


			// Patrol around the different waypoints specified in the waypoint array. Always return a task status of running.
			public override TaskStatus OnUpdate()
			{
				if (waypoints.Value.Count == 0)
				{
					return TaskStatus.Failure;
				}

				if (MCNavMeshInputSource.OnUpdate())
				{
					if (waypointReachedTime == -1)
					{
						waypointReachedTime = Time.time;
					}

					// wait the required duration before switching waypoints.
					if (waypointReachedTime + waypointPauseDuration.Value <= Time.time)
					{
						if (randomPatrol.Value)
						{
							if (waypoints.Value.Count == 1)
							{
								waypointIndex = 0;
							}
							else
							{
								// prevent the same waypoint from being selected
								var newWaypointIndex = waypointIndex;
								while (newWaypointIndex == waypointIndex)
								{
									newWaypointIndex = Random.Range(0, waypoints.Value.Count);
								}
								waypointIndex = newWaypointIndex;
							}
						}
						else
						{
							waypointIndex = (waypointIndex + 1) % waypoints.Value.Count;
						}
						SetDestination(WayPoint());
						waypointReachedTime = -1;
					}
				}

				return TaskStatus.Running;
			}


			protected void SetDestination(Transform destination)
			{
				MCNavMeshInputSource.mNavMeshAgent.isStopped = false;
				MCNavMeshInputSource.Target = destination;
			}


			// Return the current waypoint index position
			private Transform WayPoint()
			{
				if (waypointIndex >= waypoints.Value.Count)
				{
					return transform;
				}
				return waypoints.Value[waypointIndex].transform;
			}


			// Reset the public variables
			public override void OnReset()
			{
				base.OnReset();

				randomPatrol = false;
				waypointPauseDuration = 0;
				waypoints = null;
			}


			// Draw a gizmo indicating a patrol
			public override void OnDrawGizmos()
			{
	#if UNITY_EDITOR
				if (waypoints == null || waypoints.Value == null)
				{
					return;
				}
				var oldColor = UnityEditor.Handles.color;
				UnityEditor.Handles.color = Color.yellow;
				for (int i = 0; i < waypoints.Value.Count; ++i)
				{
					if (waypoints.Value[i] != null)
					{
						UnityEditor.Handles.SphereHandleCap(0, waypoints.Value[i].transform.position, waypoints.Value[i].transform.rotation, 1, EventType.Repaint);
					}
				}
				UnityEditor.Handles.color = oldColor;
	#endif
			}*/
		}
}
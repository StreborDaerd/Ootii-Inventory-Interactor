using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BehaviorDesigner.Runtime.Tasks.Movement;
using Tooltip = BehaviorDesigner.Runtime.Tasks.TooltipAttribute;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace WildWalrus.BehaviorDesigner.Actions
{
	[TaskName("MC Wander")]
	[TaskDescription("Wander using the Unity NavMesh.")]
	[TaskCategory("ootii/Motion Controller")]
	[TaskIcon("Assets/Behavior Designer Movement/Editor/Icons/{SkinColor}WanderIcon.png")]
	public class MCWander : MCSeek
	{
		[Tooltip("Minimum distance ahead of the current position to look ahead for a destination")]
		public SharedFloat minWanderDistance = 20;
		[Tooltip("Maximum distance ahead of the current position to look ahead for a destination")]
		public SharedFloat maxWanderDistance = 20;
		[Tooltip("The amount that the agent rotates direction")]
		public SharedFloat wanderRate = 2;
		[Tooltip("The minimum length of time that the agent should pause at each destination")]
		public SharedFloat minPauseDuration = 0;
		[Tooltip("The maximum length of time that the agent should pause at each destination (zero to disable)")]
		public SharedFloat maxPauseDuration = 0;
		[Tooltip("The maximum number of retries per tick (set higher if using a slow tick time)")]
		public SharedInt targetRetries = 1;
		
		private float pauseTime;
		private float destinationReachTime;

		// There is no success or fail state with wander - the agent will just keep wandering

		private bool targetSet = false;

		public override void OnStart()
		{
			targetSet = TrySetTarget();

			base.OnStart();
		}

		public override TaskStatus OnUpdate()
		{
			bool lIsDone = false;

			if(targetSet)
			{
				lIsDone = MCNavMeshInputSource.OnUpdate();
				if (lIsDone) { return TaskStatus.Success; }
			}

			//return (lIsDone ? TaskStatus.Success : TaskStatus.Running);
			Debug.Log("OnUpdate 0");
			if (lIsDone || ! targetSet)
			{
				Debug.Log("OnUpdate 1");

				// The agent should pause at the destination only if the max pause duration is greater than 0
				if (maxPauseDuration.Value > 0)
				{
					Debug.Log("OnUpdate 2");

					if (destinationReachTime == -1)
					{
						Debug.Log("OnUpdate 3");
						destinationReachTime = Time.time;
						pauseTime = Random.Range(minPauseDuration.Value, maxPauseDuration.Value);
					}
					if (destinationReachTime + pauseTime <= Time.time)
					{
						Debug.Log("OnUpdate 4");
						// Only reset the time if a destination has been set.
						if (TrySetTarget())
						{
							Debug.Log("OnUpdate 5");
							destinationReachTime = -1;
						}
					}
				}
				else
				{
					Debug.Log("OnUpdate 6");
					TrySetTarget();
				}
			}
			return TaskStatus.Running;
		}
		
		
		private bool TrySetTarget()
		{
			var direction = transform.forward;
			var validDestination = false;
			var attempts = targetRetries.Value;
			var destination = transform.position;
			while (!validDestination && attempts > 0)
			{
				direction = direction + Random.insideUnitSphere * wanderRate.Value;
				destination = transform.position + direction.normalized * Random.Range(minWanderDistance.Value, maxWanderDistance.Value);
				validDestination = SamplePosition(destination);
				attempts--;
			}
			if (validDestination)
			{
				TargetPosition = destination;
				MCNavMeshInputSource.TargetPosition = TargetPosition.Value;
				MCNavMeshInputSource.Target = null;
				MCNavMeshInputSource.OnStart();
				MCNavMeshInputSource.TargetVisualisation.position = TargetPosition.Value;
			}

			targetSet = validDestination;

			return validDestination;
		}
		
		
		// Reset the public variables
		public override void OnReset()
		{
			minWanderDistance = 20;
			maxWanderDistance = 20;
			wanderRate = 2;
			minPauseDuration = 0;
			maxPauseDuration = 0;
			targetRetries = 1;
		}

		protected bool SamplePosition(Vector3 position)
		{
			NavMeshHit hit;
			return NavMesh.SamplePosition(position, out hit, MCNavMeshInputSource.mNavMeshAgent.height * 0.5f, NavMesh.AllAreas);
		}
	}
}
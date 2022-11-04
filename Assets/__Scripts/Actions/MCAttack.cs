using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WildWalrus.BehaviorDesigner.Actions
{
	[TaskName("MC Attack")]
	[TaskCategory("ootii/Motion Controller")]
	[TaskDescription("Uses a MCAttacker to navigate the actor to the specified target or position and attack a target. This action supports walking, running, jumping, climbing, and dropping.")]
	public class MCAttack : Action
	{
		[SerializeField] MCAttacker MCAttacker;
		[SerializeField] GameObject Target;

		public override void OnStart()
		{
			//SetNextWaypoint();
			//MCNavMeshInputSource.OnStart();
			MCAttacker.Target = Target;
			MCAttacker.OnStart();
		}

		public override TaskStatus OnUpdate()
		{
			if (MCAttacker.OnUpdate()) { return TaskStatus.Success; }
			return TaskStatus.Running;
		}

	}
}
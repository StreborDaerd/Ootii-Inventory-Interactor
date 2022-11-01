using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WildWalrus.Input;

namespace WildWalrus.BehaviorDesigner.Actions
{
	[TaskName("MC Rotate Towards")]
	[TaskCategory("ootii/Motion Controller")]
	[TaskDescription("Uses Motion Controller to rotate towards the specified rotation. The rotation can either be specified by a transform or position. If the transform " +
							"is used then the position will not be used.")]
	[TaskIcon("Assets/Behavior Designer Movement/Editor/Icons/{SkinColor}RotateTowardsIcon.png")]
	public class MCRotateTowards : Action
	{
		public SharedTransform Target = null;

		public SharedVector3 TargetPosition = null;

		[SerializeField] protected MCNavMeshInputSource MCNavMeshInputSource;

		public override void OnStart()
		{
			MCNavMeshInputSource.TargetPosition = TargetPosition.Value;
			MCNavMeshInputSource.Target = Target.Value;

			MCNavMeshInputSource.OnStart();
		}

		public override TaskStatus OnUpdate()
		{
			MCNavMeshInputSource.RotateToTarget();
			return TaskStatus.Running;
		}
	}
}
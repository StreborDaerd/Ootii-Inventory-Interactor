using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WildWalrus.BehaviorDesigner.Actions
{
	[TaskName("MC Seek")]
	[TaskCategory("ootii/Motion Controller")]
	[TaskDescription("Uses a MCNavMeshInputSource to navigate the actor to the specified target or position. This action supports walking, running, jumping, climbing, and dropping.")]
	public class MCSeek : Action
	{
		public SharedTransform Target = null;

		public SharedVector3 TargetPosition;

		public bool IsFocusEnabled = false;

		public SharedTransform FocusTarget;

		//public bool UseInputSourceDefaults = false;

		public SharedFloat StopDistance = 0.25f;

		public SharedFloat DefaultNormalizedSpeed = 1f;

		public SharedFloat SlowDistance = 2.0f;

		public SharedFloat SlowNormalizedSpeed = 0.5f;

		public bool ForceRotation = true;

		public SharedFloat VoxelSize = 0.166f;

		[SerializeField] MCNavMeshInputSource MCNavMeshInputSource;

		public override void OnStart()
		{
			MCNavMeshInputSource.TargetPosition = TargetPosition.Value;
			MCNavMeshInputSource.Target = Target.Value;

			MCNavMeshInputSource.IsFocusEnabled = IsFocusEnabled;
			MCNavMeshInputSource.FocusTarget = FocusTarget.Value;

			MCNavMeshInputSource.StopDistance = StopDistance.Value;
			MCNavMeshInputSource.DefaultNormalizedSpeed = DefaultNormalizedSpeed.Value;
			MCNavMeshInputSource.SlowDistance = SlowDistance.Value;
			MCNavMeshInputSource.SlowNormalizedSpeed = SlowNormalizedSpeed.Value;
			MCNavMeshInputSource.ForceRotation = ForceRotation;
			MCNavMeshInputSource.VoxelSize = VoxelSize.Value;


			MCNavMeshInputSource.OnStart();
		}

		public override TaskStatus OnUpdate()
		{
			return (MCNavMeshInputSource.OnUpdate() ? TaskStatus.Success : TaskStatus.Running);
		}
	}
}
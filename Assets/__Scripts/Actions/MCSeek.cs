using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using WildWalrus.Input;

namespace WildWalrus.BehaviorDesigner.Actions
{
	[TaskName("MC Seek")]
	[TaskCategory("ootii/Motion Controller")]
	[TaskDescription("Uses a MCNavMeshInputSource to navigate the actor to the specified target or position. This action supports walking, running, jumping, climbing, and dropping.")]
	public class MCSeek : Action
	{
		public SharedTransform Target = null;

		public SharedVector3 TargetPosition = null;

		public bool IsFocusEnabled = false;

		public SharedTransform FocusTarget = null;

		//public bool UseInputSourceDefaults = false;

		public SharedFloat StopDistance = 0.25f;

		public SharedFloat DefaultNormalizedSpeed = 1f;

		public SharedFloat SlowDistance = 2.0f;

		public SharedFloat SlowNormalizedSpeed = 0.5f;

		public bool ForceRotation = true;

		public SharedFloat VoxelSize = 0.1666f;

		[SerializeField] protected MCNavMeshInputSource MCNavMeshInputSource;

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

			MCNavMeshInputSource.mNavMeshAgent.isStopped = false;
			MCNavMeshInputSource.mNavMeshAgent.speed = 10f;

			MCNavMeshInputSource.OnStart();

		}

		public override TaskStatus OnUpdate()
		{
			if(MCNavMeshInputSource.OnUpdate())
			{
				Stop();
				return TaskStatus.Success;
			}
			else
			{
				return TaskStatus.Running;
			}

			//return (MCNavMeshInputSource.OnUpdate() ? TaskStatus.Success : TaskStatus.Running);
		}

		public override void OnReset()
		{
			base.OnReset();
			Target = null;
			TargetPosition = Vector3.zero;

			MCNavMeshInputSource.TargetPosition = TargetPosition.Value;
			MCNavMeshInputSource.Target = Target.Value;
		}

		protected void Stop()
		{
			Debug.Log("MCNavMeshInputSource Stop");
			//if (MCNavMeshInputSource.mNavMeshAgent.hasPath)
			//{
			//	MCNavMeshInputSource.mNavMeshAgent.isStopped = true;
			//}
			MCNavMeshInputSource.mNavMeshAgent.speed = 0f;

			MCNavMeshInputSource.ClearTarget();
		}

		public override void OnBehaviorComplete()
		{
			Stop();
		}

		public override void OnEnd()
		{
			Stop();
		}
	}
}
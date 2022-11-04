using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using com.ootii.Actors.AnimationControllers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WildWalrus.BehaviorDesigner.Actions
{
	[TaskName("MC Fire Gun")]
	[TaskCategory("ootii/Motion Controller")]
	[TaskDescription("Uses a MCNavMeshInputSource to navigate the actor to the specified target or position. This action supports walking, running, jumping, climbing, and dropping.")]
	public class MCSetMotion : Action
	{
		[SerializeField] MotionController MotionController;

		public SharedInt LayerIndex = 0;
		public SharedInt MotionPhase = 0;
		public SharedInt MotionForm = 0;
		public SharedInt MotionParameter = 0;


		public override void OnAwake()
		{
			base.OnAwake();

			if (MotionController == null)
			{
				GameObject lGameObject = GetDefaultGameObject(null);
				MotionController = lGameObject.GetComponentInParent<MotionController>();
			}
		}

		void Update()
		{
			
		}
	}
}
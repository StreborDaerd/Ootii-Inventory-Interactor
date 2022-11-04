using BehaviorDesigner.Runtime.Tasks;
using com.ootii.Actors.AnimationControllers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WildWalrus.Actors.AnimationControllers;

namespace WildWalrus.BehaviorDesigner.Actions
{
	[TaskName("MC Fire Gun")]
	[TaskCategory("ootii/Motion Controller")]
	[TaskDescription("Uses a MCNavMeshInputSource to navigate the actor to the specified target or position. This action supports walking, running, jumping, climbing, and dropping.")]
	public class MCFireGun : Action
	{
		[SerializeField] MotionController MotionController;
		BasicShooterAttack1 BasicShooterAttack;

		void Update()
		{
			
		}
	}
}
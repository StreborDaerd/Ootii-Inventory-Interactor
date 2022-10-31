using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Inventory;
using com.ootii.Actors.LifeCores;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WildWalrus
{
	public class MCAttacker : MonoBehaviour
	{
		public const int IDLE = 0;
		public const int MOVING = 1;
		public const int EQUIPPING = 2;
		public const int ATTACKING = 3;
		public const int BLOCKING = 4;
		public const int REACTING = 5;
		public const int KILLED = 10;
		
		[SerializeField] ActorCore mTargetActorCore;

		[SerializeField] ActorCore mActorCore;

		[SerializeField] MotionController mMotionController;

		[SerializeField] BasicInventory mInventory;

		private void Awake()
		{
			mActorCore.SetStateValue("State", 0);
		}



	}
}
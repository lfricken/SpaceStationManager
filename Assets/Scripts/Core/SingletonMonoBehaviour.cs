using System;
using UnityEngine;

namespace SSM
{
	public abstract class SingletonMonoBehavior<T> : MonoBehaviour where T : class
	{
		public static T Instance
		{
			get;
			private set;
		}

		protected SingletonMonoBehavior()
		{
			if(Instance != null)
				throw new Exception($"You can't make two {nameof(SingletonMonoBehavior<T>)}'s !");

			Instance = this as T;
		}
	}
}

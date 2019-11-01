using System.Collections;
using UnityEngine;

namespace Game
{
	public class GasflowManager : SingletonMonoBehavior<GasflowManager>
	{
		public GasFlowGpu gas;

		public Vector3Int Resolution
		{
			get;
			private set;
		}

		private void Start()
		{
			Resolution = new Vector3Int(256, 256, 32);
			gas = new GasFlowGpu(Resolution);
			gas.AddRemoveMass.AddDelta(new Vector2Int(100, 100), 10);
			gas.AddRemoveMass.SendUpdatesToGpu();
			StartCoroutine(Tick());
		}

		private void Update()
		{

		}

		private void OnDestroy()
		{
			gas.Dispose();
		}

		IEnumerator Tick()
		{
			//gas.Init();
			for (int i = 0; i < 10000; i++)
			{
				yield return new WaitForSeconds(0.0166f);
				//for (int n = 0; n < 5; n++)
				gas.Tick();
			}
		}
	}
}

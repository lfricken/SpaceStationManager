using System.Collections;
using UnityEngine;

namespace SSM
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
			Resolution = new Vector3Int(32, 32, 32);
			gas = new GasFlowGpu(Resolution);
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
			for (int i = 0; i < 10000; i++)
			{
				yield return new WaitForSeconds(0.0166f);
				//for (int n = 0; n < 5; n++)
				gas.Tick();
			}
		}
	}
}

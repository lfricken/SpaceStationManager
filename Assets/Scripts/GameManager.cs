using Assets.Scripts;
using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	GasFlowGpu gas;

	void InitCanvas()
	{
		// this resolution must match that in gas.compute.index
		Vector3Int resolution = new Vector3Int(256, 256, 32);
		gas = new GasFlowGpu(resolution);

		var outputImage = GameObject.Find("canvas/image").GetComponent<UnityEngine.UI.Image>();
		outputImage.material.mainTexture = gas.RenderTexture;
	}

	void Start()
	{
		InitCanvas();
		StartCoroutine(Tick());
	}

	IEnumerator Tick()
	{
		for (int i = 0; i < 1000; i++)
		{
			yield return new WaitForSeconds(0.00f);
			gas.Tick();
		}
	}
}

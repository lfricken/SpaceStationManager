﻿using Assets.Scripts;
//using GasFlow;
using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	GasFlowGpu gas;
	//GasWorld<Tile> gas2;

	void InitCanvas()
	{
		// this resolution must match that in gas.compute.index
		Vector3Int resolution = new Vector3Int(64, 64, 32);
		gas = new GasFlowGpu(resolution);
		//gas2 = new GasWorld<Tile>(new Vector2Int(resolution.x, resolution.y));

		{
			var outputImage = GameObject.Find("canvas/image").GetComponent<UnityEngine.UI.Image>();
			outputImage.material.SetTexture("_MainTex", gas.RenderTexture);
		}
		{
			var velocity = GameObject.Find("velocity/image2").GetComponent<UnityEngine.UI.Image>();
			velocity.material.SetTexture("_MainTex", gas.VelocityMap);
		}
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
			yield return new WaitForSeconds(0.01f);
			gas.Tick();
		}
	}
}

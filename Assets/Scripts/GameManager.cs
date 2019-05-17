using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	GpuComputer gasMover;

	void initCanvas()
	{
		Vector2Int res = new Vector2Int(64, 64);
		Texture2D texture = new Texture2D(res.x, res.y);
		texture.filterMode = FilterMode.Point;

		for (int x = 0; x < res.x; x++)
			for (int y = 0; y < res.y; y++)
			{
				texture.SetPixel(x, y, Color.black);
			}
		texture.SetPixel(52, 51, Color.green);

		texture.SetPixel(51, 50, Color.clear);
		texture.SetPixel(52, 50, Color.clear);
		texture.SetPixel(53, 50, Color.clear);
		texture.Apply();

		gasMover = new GpuComputer("move", texture);
		gasMover.output = gasMover.input;

		var outputImage = GameObject.Find("canvas/image").GetComponent<UnityEngine.UI.Image>();
		outputImage.material.mainTexture = gasMover.output;

	}

	void Start()
	{
		initCanvas();
		StartCoroutine(Example());
	}

	void Update()
	{

	}

	IEnumerator Example()
	{
		for (int i = 0; i < 10; i++)
		{
			yield return new WaitForSeconds(0.70f);
			gasMover.Tick();
		}
	}
}

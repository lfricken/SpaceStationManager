using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	Texture2D texture;

	Vector3Int resolution;

	//RenderTexture pressureTexture;
	//ComputeShader pressureShader;
	//int pressureHandle;

	RenderTexture moveTexture;
	ComputeShader moveShader;
	int moveHandle;

	void initCanvas()
	{
		resolution = new Vector3Int(64, 64, 24);

		moveTexture = new RenderTexture(resolution.x, resolution.y, resolution.z);
		moveTexture.enableRandomWrite = true;
		moveTexture.Create();
		moveTexture.filterMode = FilterMode.Point;

		//inputTexture = new RenderTexture(resolution.x, resolution.y, resolution.z);
		//inputTexture.enableRandomWrite = true;
		//inputTexture.Create();
		//inputTexture.filterMode = FilterMode.Point;


		texture = new Texture2D(resolution.y, resolution.y);
		texture.filterMode = FilterMode.Point;

		var outputImage = GameObject.Find("canvas/image").GetComponent<UnityEngine.UI.Image>();
		outputImage.material.mainTexture = moveTexture;

		
		for (int x = 0; x < resolution.x; x++)
			for (int y = 0; y < resolution.y; y++)
			{
				texture.SetPixel(x, y, Color.black);
			}
		texture.SetPixel(52, 51, Color.green);

		texture.SetPixel(51, 50, Color.clear);
		texture.SetPixel(52, 50, Color.clear);
		texture.SetPixel(53, 50, Color.clear);
		texture.Apply();

		RenderTexture.active = moveTexture;
		Graphics.Blit(texture, moveTexture);
		RenderTexture.active = null;

		moveShader = Resources.Load<ComputeShader>("move");
		moveHandle = moveShader.FindKernel(nameof(moveHandle));

		moveShader.SetTexture(moveHandle, nameof(moveTexture), moveTexture);
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
			moveShader.Dispatch(moveHandle, resolution.x / 8, resolution.y / 8, 1);
		}
	}
}

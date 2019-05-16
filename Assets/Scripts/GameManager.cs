using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	//ComputeShader shader;
	//RenderTexture read;

	GameObject mainCanvas;
	UnityEngine.UI.Image outputImage;
	RenderTexture outputTexture;

	Texture2D texture;

	//Renderer rend;

	Vector3Int resolution;

	private void Awake()
	{

	}

	void initCanvas()
	{
		resolution = new Vector3Int(4, 4, 32);

		outputTexture = new RenderTexture(resolution.x, resolution.y, resolution.z);
		outputTexture.enableRandomWrite = true;
		outputTexture.Create();
		outputTexture.filterMode = FilterMode.Point;

		mainCanvas = GameObject.Find("canvas");


		texture = new Texture2D(outputTexture.width, outputTexture.height);
		texture.filterMode = FilterMode.Point;

		outputImage = GameObject.Find("canvas/image").GetComponent<UnityEngine.UI.Image>();
		outputImage.material.mainTexture = texture;



		Color color = Color.red;

		RenderTexture.active = outputTexture;
		for (int x = 0; x < outputTexture.width; x++)
			for (int y = 0; y < outputTexture.height; y++)
			{
				if (x % 2 == 0)
				{
					color.r = 0;
					color.g = 1;
				}
				else
				{
					color.r = 1;
					color.g = 0;
				}
				texture.SetPixel(x, y, color);
			}
		texture.Apply();
		RenderTexture.active = null;
	}

	// Start is called before the first frame update
	void Start()
	{
		initCanvas();

	}

	// Update is called once per frame
	void Update()
	{

	}
}

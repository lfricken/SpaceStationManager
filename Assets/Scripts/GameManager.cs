using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	Texture2D texture;
	RenderTexture outputTexture;

	Vector3Int resolution;

	ComputeShader shader;
	int shaderHandle;

	void initCanvas()
	{
		resolution = new Vector3Int(256, 256, 24);

		outputTexture = new RenderTexture(resolution.x, resolution.y, resolution.z);
		outputTexture.enableRandomWrite = true;
		outputTexture.Create();
		outputTexture.filterMode = FilterMode.Point;


		texture = new Texture2D(resolution.y, resolution.y);
		texture.filterMode = FilterMode.Point;

		var outputImage = GameObject.Find("canvas/image").GetComponent<UnityEngine.UI.Image>();
		outputImage.material.mainTexture = outputTexture;


		Color color = Color.red;

		for (int x = 0; x < resolution.x; x++)
			for (int y = 0; y < resolution.y; y++)
			{
				if (x % 4 == 0)
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

		RenderTexture.active = outputTexture;
		Graphics.Blit(texture, outputTexture);
		RenderTexture.active = null;

		shader = Resources.Load<ComputeShader>("blur");            // here we link computer shader code file to the shader class
		shaderHandle = shader.FindKernel(nameof(shaderHandle));
		shader.SetTexture(shaderHandle, nameof(outputTexture), outputTexture);

		shader.Dispatch(shaderHandle, resolution.x / 8, resolution.y / 8, 1);
	}

	void Start()
	{
		initCanvas();
	}

	void Update()
	{
		shader.Dispatch(shaderHandle, resolution.x / 8, resolution.y / 8, 1);
	}
}

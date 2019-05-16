using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	Texture2D texture;
	RenderTexture outputTexture;
	RenderTexture inputTexture;

	Vector3Int resolution;

	ComputeShader shader;
	int shaderHandle;

	void initCanvas()
	{
		resolution = new Vector3Int(8, 8, 24);

		outputTexture = new RenderTexture(resolution.x, resolution.y, resolution.z);
		outputTexture.enableRandomWrite = true;
		outputTexture.Create();
		outputTexture.filterMode = FilterMode.Point;

		inputTexture = new RenderTexture(resolution.x, resolution.y, resolution.z);
		inputTexture.enableRandomWrite = true;
		inputTexture.Create();
		inputTexture.filterMode = FilterMode.Point;


		texture = new Texture2D(resolution.y, resolution.y);
		texture.filterMode = FilterMode.Point;

		var outputImage = GameObject.Find("canvas/image").GetComponent<UnityEngine.UI.Image>();
		outputImage.material.mainTexture = outputTexture;


		Color color = Color.black;

		for (int x = 0; x < resolution.x; x++)
			for (int y = 0; y < resolution.y; y++)
			{
				if (x % 4 == 0)
				{
					color.r = 1;
				}
				else
				{
					color.r = 0;
				}
				texture.SetPixel(x, y, color);
			}
		texture.Apply();

		RenderTexture.active = outputTexture;
		Graphics.Blit(texture, outputTexture);
		RenderTexture.active = null;

		RenderTexture.active = inputTexture;
		Graphics.Blit(texture, inputTexture);
		RenderTexture.active = null;

		shader = Resources.Load<ComputeShader>("blur");            // here we link computer shader code file to the shader class
		shaderHandle = shader.FindKernel(nameof(shaderHandle));

	}

	void Start()
	{
		initCanvas();
	}

	void Update()
	{
		shader.SetTexture(shaderHandle, nameof(outputTexture), outputTexture);
		shader.SetTexture(shaderHandle, nameof(inputTexture), inputTexture);
		shader.Dispatch(shaderHandle, resolution.x / 8, resolution.y / 8, 1);
	}
}

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
		resolution = new Vector3Int(256, 256, 32);

		outputTexture = new RenderTexture(resolution.x, resolution.y, resolution.z);
		outputTexture.enableRandomWrite = true;
		outputTexture.Create();
		outputTexture.filterMode = FilterMode.Point;

		mainCanvas = GameObject.Find("canvas");
		mainCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
		mainCanvas.GetComponent<Canvas>().worldCamera = Camera.main;
		mainCanvas.GetComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
		mainCanvas.GetComponent<UnityEngine.UI.CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
		mainCanvas.GetComponent<UnityEngine.UI.CanvasScaler>().matchWidthOrHeight = 1.0f;


		texture = new Texture2D(outputTexture.width, outputTexture.height);

		outputImage = GameObject.Find("canvas/image").GetComponent<UnityEngine.UI.Image>();
		outputImage.material.mainTexture = texture;
		outputImage.type = UnityEngine.UI.Image.Type.Simple;
		outputImage.GetComponent<RectTransform>().sizeDelta = new Vector2(1080, 1080);



		Color color = Color.red;

		RenderTexture.active = outputTexture;
		for (int x = 0; x < outputTexture.width; x++)
			for (int y = 0; y < outputTexture.height; y++)
				texture.SetPixel(x, y, color);
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

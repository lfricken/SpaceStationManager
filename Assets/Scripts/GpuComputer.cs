using UnityEngine;

namespace Assets.Scripts
{
	public class GpuComputer
	{
		public GpuComputer(string _shaderName, Texture2D initialValues)
		{
			shaderName = _shaderName;
			read = initialValues;

			input = new RenderTexture(read.width, read.height, 24);
			input.enableRandomWrite = true;
			input.Create();
			input.filterMode = FilterMode.Point;

			output = new RenderTexture(read.width, read.height, 24);
			output.enableRandomWrite = true;
			output.Create();
			output.filterMode = FilterMode.Point;

			RenderTexture.active = input;
			Graphics.Blit(read, input);
			RenderTexture.active = null;

			shader = Resources.Load<ComputeShader>(shaderName);
			handle = shader.FindKernel(shaderName);

			shader.SetTexture(handle, nameof(input), input);
			shader.SetTexture(handle, nameof(output), output);
		}

		public RenderTexture input;
		public RenderTexture output;
		public Texture2D read { get; private set; }

		private ComputeShader shader;
		private string shaderName;
		private int handle;

		public float Read(Vector2Int coordinate)
		{
			return 0f;
		}

		public void Tick()
		{
			int groupDivider = 8;
			shader.Dispatch(handle, input.width / groupDivider, input.height / groupDivider, 1);
			//RenderTexture.active = output;
			//read.ReadPixels(new Rect(0, 0, output.width / groupDivider, output.height / groupDivider), 0, 0);
			//read.Apply();
			//RenderTexture.active = null;
		}
	}
}

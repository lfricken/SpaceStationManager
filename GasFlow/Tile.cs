using System.Collections.Generic;

namespace GasFlow
{
	public class Tile
	{
		/// <summary>
		/// 
		/// </summary>
		public bool Blocked { get; set; }

		public float TotalPressure { get; set; }

		public virtual void Update(List<Tile> readTiles)
		{
			if (Blocked) { return; }

			float average = 0;
			foreach(var tile in readTiles)
			{
				average += tile.TotalPressure;
			}
			average /= 9f;

			TotalPressure = average;
		}
	}
}

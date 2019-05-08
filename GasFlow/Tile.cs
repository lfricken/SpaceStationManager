using System.Collections.Generic;

namespace GasFlow
{
	public class Tile
	{
		/// <summary>
		/// 
		/// </summary>
		public bool Blocked { get; internal set; }

		public float TotalPressure { get; set; }

		public virtual void Update(List<Tile> readTiles)
		{
			if (Blocked) { return; }

			float average = 0;
			int averageCount = 0;
			foreach (var tile in readTiles)
			{
				if (tile.Blocked)
				{
					continue;
				}
				average += tile.TotalPressure;
				averageCount += 1;
			}
			average /= averageCount;

			TotalPressure = average;
		}
	}
}

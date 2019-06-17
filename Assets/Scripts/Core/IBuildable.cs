using System;
using System.Collections.Generic;

namespace Game
{
	/// <summary>
	/// Anything that can be built from a menu implements this.
	/// </summary>
	public interface IBuildable
	{
		//string Name { get; set; }
		//string Description { get; set; }
		//string Cost { get; set; }
	}

	public static class IBuildableFinder
	{
		public static List<Type> GetTypes()
		{
			var types = new List<Type>();

			var baseType = typeof(IBuildable);
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				foreach (var type in assembly.GetTypes())
					if (baseType.IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
						types.Add(type);

			return types;
		}
	}
}

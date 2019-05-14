using System;
using System.Runtime.InteropServices;

namespace TestGround
{
	class Program
	{
		public class Cpp
		{
			[DllImport("FastGas.dll", EntryPoint = "add", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
			public static extern int add(int a, int b);
		}

		static void Main(string[] args)
		{
			Console.WriteLine("a ");
			Console.WriteLine(Cpp.add(3,5));

			Console.ReadKey();
		}
	}
}

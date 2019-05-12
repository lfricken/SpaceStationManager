using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TestGround
{
	class Worker
	{
		public Worker(int _range, Program _data)
		{
			range = _range;
			data = _data;
		}

		readonly int range;
		readonly Program data;

		public void Work()
		{
			Thread.Sleep(1000);
			Console.WriteLine($"Do Work {range}");
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Start");
			var p = new Program();
			var threads = p.StartWork();

			Console.WriteLine("Do other stuff");
			threads.Wait();
			Console.WriteLine("Exit");
			Console.ReadLine();
		}

		public async Task StartWork()
		{
			Console.WriteLine("Start Work");

			var tasks = StartThreads();
			await Task.WhenAll(tasks);

			Console.WriteLine("Finish Work");
		}

		public List<Task> StartThreads()
		{
			List<Task> tasks = new List<Task>();
			for (int i = 0; i < 8; ++i)
			{
				Worker worker = new Worker(i, this);
				Task t = new Task(() => StartThread(worker));
				t.Start();
				tasks.Add(t);
			}
			return tasks;
		}

		public static void StartThread(Worker worker)
		{
			worker.Work();
		}

	}
}

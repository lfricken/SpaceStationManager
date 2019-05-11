using System;
using System.Threading;

namespace TestGround
{
	class Worker
	{
		public Worker(int _range, Program _data, CountdownEvent _threadCounter)
		{
			threadCounter = _threadCounter;
			range = _range;
			data = _data;

			threadCounter.AddCount(1);
		}

		int range;
		Program data;
		CountdownEvent threadCounter;

		public void Work()
		{
			Thread.Sleep(1000);
			Console.WriteLine($"Hello from {range}");
			threadCounter.Signal();
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			var p = new Program();
			var threads = p.StartWork(10);

			Console.WriteLine("Middle.");

			threads.Wait();

			Console.WriteLine("Exit.");
			Console.ReadLine();
		}

		public CountdownEvent StartWork(int work)
		{
			CountdownEvent threadCounter = new CountdownEvent(1);
			Console.WriteLine("Start.");
			for (int i = 0; i < work; ++i)
			{
				Worker worker = new Worker(i, this, threadCounter);
				ThreadPool.QueueUserWorkItem(StartThread, worker);
			}

			threadCounter.Signal();
			return threadCounter;
		}

		public void StartThread(object data)
		{
			Worker w = (Worker)data;
			w.Work();
		}

	}
}

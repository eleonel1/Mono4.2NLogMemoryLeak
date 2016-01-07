using System;

namespace MemoryLeakTest
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			var appHost = new MemoryLeakTestHost("MemoryLeakTest",20,typeof(MemoryLeakTestService).Assembly);
			appHost.Init();
			var listeningOn = args.Length == 0 ? "http://*:9080/" : args[0];
			appHost.Start(listeningOn);
			Console.WriteLine("MemoryLeakTest Service created on {0}, listening at {1}", DateTime.Now, listeningOn);
			Console.WriteLine("Pres Ctrl + c to end execution.");
			while (true)
			{
				System.Threading.Thread.Sleep(100);
			}
		}
	}
}

using System;

namespace Scheduler
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Schedule Generator v0.1");

			var config = new Config(args);

			if (config.IsSet("?") || config.Files.Count == 0 || !config.IsSet("out"))
			{
				Console.WriteLine("Usage: Scheduler.exe [file(s) to process] [parameters]");
				Console.WriteLine();
				Console.WriteLine("Parameters:");
				Console.WriteLine("    /out:type(s) - Specifies the output type(s) to generate.");
				Console.WriteLine("    /wait        - Waits for a keypress once the program is finished.");
				Console.WriteLine();
				Console.WriteLine("Output types (comma-separated):");
				Console.WriteLine("    html");
				Console.WriteLine("    png");
				Console.WriteLine("    text");
			}
			else
			{
				string[] outputs = config["out"].Split(',');
				foreach (string file in config.Files)
				{
					ScheduleGen.ProcessFile(file, outputs);
				}
			}

			if (config.IsSet("wait"))
			{
				Console.WriteLine("Press any key...");
				Console.ReadKey();
			}
		}
	}
}

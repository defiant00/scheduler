using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Scheduler
{
	class ScheduleGen
	{
		static Regex taskRegex = new Regex(@";([a-zA-Z0-9-_]*)[ \t]*(?:\(([0-9.]*)\))?[ \t]*(?:([0-9.]*)([dhm]))?[ \t]*(.*)");

		public static void ProcessFile(string file, string[] outputs)
		{
			var sch = new Schedule();
			string[] lines = File.ReadAllLines(file);
			foreach (string l in lines)
			{
				string line = l.Trim();
				if (line.StartsWith("/start") && line.Length > 7)
				{
					sch.Start = DateTime.Parse(line.Substring(7));
				}
				else if (line.StartsWith(";"))
				{
					var match = taskRegex.Match(line);
					if (match.Success)
					{
						string lbl = match.Groups[1].Value;
						string percent = match.Groups[2].Value;
						string time = match.Groups[3].Value;
						string timeType = match.Groups[4].Value;
						string desc = match.Groups[5].Value;

						var t = new Schedule.Task { Label = lbl, Description = desc };
						if (!string.IsNullOrEmpty(percent)) { t.Percent = Convert.ToDouble(percent); }
						if (!string.IsNullOrEmpty(time))
						{
							int seconds = 0;
							double timeAmt = Convert.ToDouble(time);
							if (timeType == "d") { seconds = (int)(timeAmt * 24 * 60 * 60); }
							else if (timeType == "h") { seconds = (int)(timeAmt * 60 * 60); }
							else if (timeType == "m") { seconds = (int)(timeAmt * 60); }
							t.Time = new TimeSpan(0, 0, seconds);
						}
						sch.Tasks.Add(t);
					}
				}
				else if (line.StartsWith(">"))
				{

				}
				else if (line.StartsWith("["))
				{

				}
				else if (!string.IsNullOrWhiteSpace(line))
				{
					sch.Tasks[sch.Tasks.Count - 1].Description += Environment.NewLine + line.Trim();
				}
			}
			Console.WriteLine(sch);
		}
	}
}

using Scheduler.Output;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Scheduler
{
	class ScheduleGen
	{
		enum ParseMode
		{
			Settings,
			Tasks,
			Links,
		}

		class Link { public string Parent, Child; }

		static Regex settingRegex = new Regex(@"([a-zA-Z0-9-_]*)[ \t]*:(.*)");
		static Regex taskStartRegex = new Regex(@"^[ \t>]*;");
		static Regex taskRegex = new Regex(@";([a-zA-Z0-9-_]*)[ \t]*(?:([0-9.]*)%)?[ \t]*(?:([0-9.]*)([dhm]))?[ \t]*(.*)");

		public static void ProcessFile(string file, string[] outputs)
		{
			var sch = new Schedule();
			var tasks = new List<TaskDepth>();
			tasks.Add(new TaskDepth { Task = sch, Depth = 0 });
			var mode = ParseMode.Settings;
			var links = new List<Link>();
			string[] lines = File.ReadAllLines(file);
			foreach (string l in lines)
			{
				string line = l.Trim();
				if (line.StartsWith("/"))
				{
					switch (line.ToLower())
					{
						case "/settings":
							mode = ParseMode.Settings;
							break;
						case "/tasks":
							mode = ParseMode.Tasks;
							break;
						case "/links":
							mode = ParseMode.Links;
							break;
						default:
							throw new Exception("Unknown mode " + line);
					}
				}
				else if (mode == ParseMode.Settings)
				{
					var match = settingRegex.Match(line);
					if (match.Success)
					{
						string lbl = match.Groups[1].Value;
						string val = match.Groups[2].Value.Trim();

						switch (lbl.ToLower())
						{
							case "start":
								sch.Start = DateTime.Parse(val);
								break;
						}
					}
				}
				else if (mode == ParseMode.Tasks)
				{
					if (taskStartRegex.IsMatch(line))
					{
						int lineDepth = 0;
						while (line.StartsWith(">"))
						{
							line = line.Substring(1).Trim();
							lineDepth++;
						}

						if (lineDepth > tasks[tasks.Count - 1].Depth && tasks[tasks.Count - 1].Task.Tasks.Count > 0)
						{
							var t = tasks[tasks.Count - 1].Task;
							tasks.Add(new TaskDepth { Task = t.Tasks[t.Tasks.Count - 1], Depth = lineDepth });
						}
						else
						{
							while (lineDepth < tasks[tasks.Count - 1].Depth)
							{
								tasks.RemoveAt(tasks.Count - 1);
							}
						}

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
							tasks[tasks.Count - 1].Task.Tasks.Add(t);
						}
					}
					else if (tasks[tasks.Count - 1].Task.Tasks.Count > 0)
					{
						var t = tasks[tasks.Count - 1].Task;
						t.Tasks[t.Tasks.Count - 1].Description += Environment.NewLine + line.Trim();
					}
				}
				else
				{
					// Links
					string[] items = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
					for (int i = 1; i < items.Length; i++)
					{
						links.Add(new Link { Parent = items[i - 1], Child = items[i] });
					}
				}
			}

			// Parse and trim all the descriptions since they might have metadata, or empty lines at the end.
			sch.ParseDescriptions();

			// Process and add all task links.
			foreach (var l in links) { sch.AddLink(l.Parent, l.Child); }

			// Calculate the start and end times of all tasks.
			sch.CalculateTimes();

			var outputFuncs = new Dictionary<string, Action<string, Schedule>> {
				{"text", Text.CreateOutput},
				{"csv", CSV.CreateOutput},
			};

			// Do the output functions.
			foreach (string o in outputs)
			{
				if (outputFuncs.ContainsKey(o)) { outputFuncs[o](file, sch); }
				else { throw new Exception("Unknown output method " + o); }
			}
		}

		class TaskDepth
		{
			public TaskCollection Task;
			public int Depth;
		}
	}
}

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Scheduler.Output
{
	class CSV
	{
		public static void CreateOutput(string input, Schedule schedule)
		{
			using (var writer = new StreamWriter(input + ".csv"))
			{
				writer.Write("Parents,Label,Description,Start,End,Time,Percent");
				foreach (string k in schedule.Metadata.Keys)
				{
					writer.Write(",");
					writer.Write(schedule.Metadata[k]);
				}
				writer.WriteLine();
				foreach (var t in schedule.Tasks) { WriteTask(schedule, writer, t, ""); }
			}
		}

		private static void WriteTask(Schedule schedule, StreamWriter writer, Schedule.Task task, string group)
		{
			string lbl = string.IsNullOrEmpty(group) ? task.Label : (task.Label + " in " + group);
			writer.Write($"{GetParentsString(task)},{FormatVal(lbl)},{FormatVal(task.Description)},{FormatVal(task.Start.ToString())},{FormatVal((task.Start + task.Time).ToString())},{FormatVal(task.Time.ToString())},{FormatVal(task.Percent + "%")}");
			if (schedule.Metadata.Count > 0)
			{
				writer.Write(",");
				var vals = new List<string>();
				foreach (string k in schedule.Metadata.Keys)
				{
					string val = "";
					if (task.Metadata.ContainsKey(k)) { val = task.Metadata[k]; }
					vals.Add(FormatVal(val));
				}
				writer.Write(string.Join(",", vals));
			}
			writer.WriteLine();

			foreach (var t in task.Tasks) { WriteTask(schedule, writer, t, lbl); }
		}

		private static string GetParentsString(Schedule.Task task)
		{
			return FormatVal(string.Join(",", task.Parents.Select(p => p.Label)));
		}

		private static string FormatVal(string val)
		{
			return '"' + val.Replace("\"", "\"\"") + '"';
		}
	}
}

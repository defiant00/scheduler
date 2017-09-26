using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Scheduler.Output
{
	class CSV
	{
		public static void CreateOutput(string input, Schedule schedule)
		{
			var allMetaKeys = new List<string>();
			foreach (var t in schedule.Tasks) { GetMetadata(t, allMetaKeys); }

			using (var writer = new StreamWriter(input + ".csv"))
			{
				writer.Write("Parents,Label,Description,Start,End,Time,Percent");
				if (allMetaKeys.Count > 0)
				{
					writer.Write(",");
					writer.Write(string.Join(",", allMetaKeys));
				}
				writer.WriteLine();
				foreach (var t in schedule.Tasks) { WriteTask(writer, t, "", allMetaKeys); }
			}
		}

		private static void GetMetadata(Schedule.Task task, List<string> metaKeys)
		{
			foreach (string k in task.Metadata.Keys)
			{
				if (!metaKeys.Contains(k)) { metaKeys.Add(k); }
			}

			foreach (var t in task.Tasks) { GetMetadata(t, metaKeys); }
		}

		private static void WriteTask(StreamWriter writer, Schedule.Task task, string group, List<string> allMetaKeys)
		{
			string lbl = string.IsNullOrEmpty(group) ? task.Label : (task.Label + " in " + group);
			writer.Write($"{GetParentsString(task)},{FormatVal(lbl)},{FormatVal(task.Description)},{FormatVal(task.Start.ToString())},{FormatVal((task.Start + task.Time).ToString())},{FormatVal(task.Time.ToString())},{FormatVal(task.Percent + "%")}");
			if (allMetaKeys.Count > 0)
			{
				writer.Write(",");
				var vals = new List<string>();
				foreach (string k in allMetaKeys)
				{
					string val = "";
					if (task.Metadata.ContainsKey(k)) { val = task.Metadata[k]; }
					vals.Add(FormatVal(val));
				}
				writer.Write(string.Join(",", vals));
			}
			writer.WriteLine();

			foreach (var t in task.Tasks) { WriteTask(writer, t, lbl, allMetaKeys); }
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

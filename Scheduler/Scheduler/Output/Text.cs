using System;
using System.IO;
using System.Text;

namespace Scheduler.Output
{
	class Text
	{
		public static void CreateOutput(string input, Schedule schedule)
		{
			using (var writer = new StreamWriter(input + ".txt"))
			{
				foreach (var t in schedule.Tasks) { WriteTask(writer, 0, t); }
			}
		}

		private static void WriteTask(StreamWriter writer, int indent, Schedule.Task task)
		{
			Indent(writer, indent);
			writer.WriteLine($"{task.Label} - {task.Percent}% ({task.Start} to {task.Start + task.Time})");

			if (task.Metadata.Count > 0)
			{
				var sb = new StringBuilder();
				foreach (string key in task.Metadata.Keys)
				{
					sb.Append("[");
					sb.Append(key);
					sb.Append(":");
					sb.Append(task.Metadata[key]);
					sb.Append("] ");
				}

				Indent(writer, indent);
				writer.WriteLine(sb.ToString().Trim());
			}

			writer.WriteLine(IndentMultiLine(task.Description, indent));
			writer.WriteLine("--------");
			foreach (var t in task.Tasks) { WriteTask(writer, indent + 1, t); }
		}

		private static void Indent(StreamWriter writer, int indent)
		{
			for (int i = 0; i < indent; i++) { writer.Write("\t"); }
		}

		private static string IndentMultiLine(string line, int indent)
		{
			string tabs = "".PadLeft(indent, '\t');
			return tabs + line.Replace(Environment.NewLine, Environment.NewLine + tabs);
		}
	}
}

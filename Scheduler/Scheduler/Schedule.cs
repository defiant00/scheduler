using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Scheduler
{
	public class TaskCollection
	{
		public List<Schedule.Task> Tasks = new List<Schedule.Task>();
	}

	public class Schedule : TaskCollection
	{
		public DateTime Start;

		public List<Link> TaskLinks = new List<Link>();

		public Dictionary<string, string> Metadata = new Dictionary<string, string>();

		// All metadata, key is lowercase trimmed, value is display value.
		public Dictionary<string, string> AllTaskMetadata = new Dictionary<string, string>();

		public void ParseDescriptions()
		{
			foreach (var t in Tasks) { t.ParseDescriptions(this); }
		}

		public void AddLink(string parent, string child)
		{
			var pTask = GetTask(parent);
			var cTask = GetTask(child);
			if (pTask != null && cTask != null)
			{
				TaskLinks.Add(new Link { Parent = pTask, Child = cTask });
				pTask.Children.Add(cTask);
				cTask.Parents.Add(pTask);
			}
			else if (pTask == null)
			{
				throw new Exception("Could not find task '" + parent + "'");
			}
			else
			{
				throw new Exception("Could not find task '" + child + "'");
			}
		}

		public void CalculateTimes()
		{
			var priorSC = new SetCount();
			var sc = GetSetCount();
			while (sc.Unset != priorSC.Unset)
			{
				priorSC = sc;

				foreach (var t in Tasks) { t.CalculateTime(Start); }

				sc = GetSetCount();
			}
			if (sc.Unset > 0)
			{
				throw new Exception("Unable to set all task times.");
			}
		}

		private Task GetTask(string label)
		{
			foreach (var t in Tasks)
			{
				var rTask = t.GetTask(label);
				if (rTask != null) { return rTask; }
			}
			return null;
		}

		private SetCount GetSetCount()
		{
			var sc = new SetCount();
			foreach (var t in Tasks) { t.GetSetCount(sc); }
			return sc;
		}

		public class Task : TaskCollection
		{
			static DateTime UNSET { get { return DateTime.MinValue; } }
			static Regex metadataRegex = new Regex(@"\[([^:]+):([^]]*)]");

			public string Label;
			public string Description;
			public Dictionary<string, string> Metadata = new Dictionary<string, string>();
			public List<Task> Parents = new List<Task>();
			public List<Task> Children = new List<Task>();

			DateTime _Start = UNSET;
			public DateTime Start
			{
				get { return Tasks.Count == 0 ? _Start : Tasks.Min(t => t.Start); }
			}

			TimeSpan _Time;
			public TimeSpan Time
			{
				get
				{
					if (Tasks.Count == 0) { return _Time; }

					DateTime end = Start;
					foreach (var t in Tasks)
					{
						var tEnd = t.Start + t.Time;
						if (tEnd > end) { end = tEnd; }
					}
					return end - Start;
				}
				set { _Time = value; }
			}

			double _Percent;
			public double Percent
			{
				get
				{
					if (Tasks.Count == 0) { return _Percent; }

					var totalTime = new TimeSpan();
					var completeTime = new TimeSpan();
					double totalPercent = Tasks.Count * 100;
					double completePercent = 0;

					foreach (var t in Tasks)
					{
						double percent = t.Percent;
						totalTime = totalTime.Add(t.Time);
						completeTime = completeTime.Add(new TimeSpan((long)(t.Time.Ticks * (percent / 100))));
						completePercent += percent;
					}

					return totalTime.TotalMilliseconds > 0 ? (completeTime.TotalMilliseconds / totalTime.TotalMilliseconds) * 100 : (completePercent / totalPercent);
				}
				set { _Percent = value; }
			}

			public void ParseDescriptions(Schedule schedule)
			{
				foreach (Match m in metadataRegex.Matches(Description))
				{
					SetMetadata(schedule, m.Groups[1].Value.Trim(), m.Groups[2].Value.Trim());
				}
				Description = metadataRegex.Replace(Description, "").Trim();
				foreach (var t in Tasks) { t.ParseDescriptions(schedule); }
			}

			public Task GetTask(string label)
			{
				if (Label == label) { return this; }
				foreach (var t in Tasks)
				{
					var rTask = t.GetTask(label);
					if (rTask != null) { return rTask; }
				}
				return null;
			}

			public void GetSetCount(SetCount sc)
			{
				if (_Start == UNSET) { sc.Unset++; }
				else { sc.Set++; }
				foreach (var t in Tasks) { t.GetSetCount(sc); }
			}

			public void CalculateTime(DateTime baseStart)
			{
				if (_Start == UNSET)
				{
					if (Parents.Count == 0) { _Start = baseStart; }
					else
					{
						bool allSet = true;
						DateTime latest = baseStart;
						foreach (var p in Parents)
						{
							if (p.Start == UNSET)
							{
								allSet = false;
								break;
							}
							else
							{
								var d = p.Start + p.Time;
								if (d > latest) { latest = d; }
							}
						}
						if (allSet) { _Start = latest; }
					}
				}

				// Not an else since we might have just set _Start.
				if (_Start != UNSET)
				{
					foreach (var t in Tasks) { t.CalculateTime(_Start); }
				}
			}

			private void SetMetadata(Schedule schedule, string key, string val)
			{
				switch (key.ToLower())
				{
					case "start":
						_Start = DateTime.Parse(val);
						break;
					default:
						string lKey = key.ToLower();
						if (!string.IsNullOrEmpty(val)) { Metadata[lKey] = val; }
						if (!schedule.AllTaskMetadata.ContainsKey(lKey)) { schedule.AllTaskMetadata[lKey] = key; }
						break;
				}
			}

			public override string ToString()
			{
				return $"{Label} : {_Start} to {_Start + Time}";
			}
		}

		public class Link
		{
			public Task Parent, Child;
			public override string ToString() { return $"{Parent.Label} -> {Child.Label}"; }
		}

		public class SetCount
		{
			public int Set, Unset;
			public override string ToString() { return $"Set: {Set}, Unset: {Unset}"; }
		}
	}
}

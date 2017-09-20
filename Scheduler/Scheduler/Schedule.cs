using System;
using System.Collections.Generic;

namespace Scheduler
{
	public class TaskCollection
	{
		public List<Schedule.Task> Tasks = new List<Schedule.Task>();
	}

	public class Schedule : TaskCollection
	{
		public DateTime Start;

		public void TrimDescriptions()
		{
			foreach (var t in Tasks) { t.TrimDescriptions(); }
		}

		public class Task : TaskCollection
		{
			public string Label;
			public string Description;
			public DateTime Start;

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

					var total = new TimeSpan();
					var complete = new TimeSpan();

					foreach (var t in Tasks)
					{
						total.Add(t.Time);
						complete.Add(new TimeSpan((long)(t.Time.Ticks * (t.Percent / 100))));
					}

					return (complete.TotalMilliseconds / total.TotalMilliseconds) * 100;
				}
				set { _Percent = value; }
			}

			public void TrimDescriptions()
			{
				Description = Description.Trim();
				foreach (var t in Tasks) { t.TrimDescriptions(); }
			}
		}
	}
}

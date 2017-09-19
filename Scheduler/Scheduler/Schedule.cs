using System;
using System.Collections.Generic;

namespace Scheduler
{
	public class Schedule
	{
		public DateTime Start;
		public List<Task> Tasks = new List<Task>();

		public class Task
		{
			List<Task> Tasks = new List<Task>();

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
		}
	}
}

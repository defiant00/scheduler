using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace Scheduler.Output
{
	class PngGantt
	{
		class Area
		{
			public Rectangle Rect;
			public Schedule.Task Task;
			public string DisplayText;
			public Rectangle TextRect;
		}

		class Connection { public Area Parent, Child; }

		public static void CreateOutput(string input, Schedule schedule)
		{
			int dayWidth = schedule.Metadata.ContainsKey("daywidth") ? Convert.ToInt32(schedule.Metadata["daywidth"]) : 400;
			int padding = schedule.Metadata.ContainsKey("padding") ? Convert.ToInt32(schedule.Metadata["padding"]) : 20;

			var areas = new List<Area>();
			var connections = new List<Connection>();

			var start = schedule.Tasks.Min(t => t.Start);
			if (schedule.Start < start) { start = schedule.Start; }

			var end = schedule.Tasks.Max(t => t.Start + t.Time);
			if (schedule.Start > end) { end = schedule.Start; }

			var font = new Font("Arial", 8);
			var fontBrush = new SolidBrush(Color.Black);
			var gridPen = new Pen(Color.FromArgb(128, 192, 255));
			var dateBrush = new SolidBrush(Color.Blue);

			var tImg = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
			var tG = Graphics.FromImage(tImg);
			CreateAreas(schedule.Tasks, areas, start, end, dayWidth, tG, font);
			CreateConnections(areas, connections);

			int width = areas.Max(a => Math.Max(a.Rect.Right, a.TextRect.Right));
			int height = areas.Max(a => Math.Max(a.Rect.Bottom, a.TextRect.Bottom));

			var img = new Bitmap(width + 2 * padding, height + 2 * padding, PixelFormat.Format32bppArgb);
			var g = Graphics.FromImage(img);
			g.Clear(Color.White);
			DrawDateGrid(img, g, font, dateBrush, gridPen, start, end, dayWidth, padding);

			var c_incomplete = Color.Red;
			var c_complete = Color.Blue;

			var connPen = new Pen(Color.DarkGray);
			var groupIncPen = new Pen(c_incomplete, 4);
			var groupComPen = new Pen(c_complete, 4);
			var incompletePen = new Pen(c_incomplete);
			var completePen = new Pen(c_complete);
			var areaBrush = new SolidBrush(Color.FromArgb(150, 255, 255, 255));
			var progressBrush = new SolidBrush(Color.FromArgb(150, 128, 128, 255));
			var completeBrush = new SolidBrush(c_complete);
			var incompleteBrush = new SolidBrush(c_incomplete);

			foreach (var c in connections)
			{
				g.DrawLine(connPen, c.Parent.Rect.Right + padding + +1, c.Parent.Rect.Y + padding + c.Parent.Rect.Height / 2, c.Child.Rect.X + padding - 1, c.Child.Rect.Y + padding + c.Child.Rect.Height / 2);
			}

			foreach (var a in areas)
			{
				if (a.Task.Tasks.Count > 0)
				{
					g.DrawLine(a.Task.Percent == 100 ? groupComPen : groupIncPen, a.Rect.X + padding, a.Rect.Y + padding + a.Rect.Height / 2, a.Rect.Right + padding, a.Rect.Y + padding + a.Rect.Height / 2);
				}
				else if (a.Rect.Width > 0)
				{
					var r = new Rectangle(a.Rect.X + padding, a.Rect.Y + padding, a.Rect.Width, a.Rect.Height);
					int perSplit = (int)(r.Width * a.Task.Percent / 100);
					g.FillRectangle(progressBrush, r.X, r.Y, perSplit, r.Height);
					g.FillRectangle(areaBrush, r.X + perSplit, r.Y, r.Width - perSplit, r.Height);
					g.DrawRectangle(a.Task.Percent == 100 ? completePen : incompletePen, r);
				}
				else
				{
					g.FillRectangle(a.Task.Percent == 100 ? completeBrush : incompleteBrush, a.Rect.X + padding - 4, a.Rect.Y + padding + a.Rect.Height / 2 - 4, 8, 8);
				}

				g.DrawString(a.DisplayText, font, fontBrush, a.TextRect.X + padding, a.TextRect.Y + padding);
			}

			img.Save(input + ".gantt.png", ImageFormat.Png);
		}

		static void DrawDateGrid(Image img, Graphics g, Font font, Brush fontBrush, Pen gridPen, DateTime start, DateTime end, int dayWidth, int padding)
		{
			int count = 0;
			int x = 0;
			int offset = (int)((1 - start.TimeOfDay.TotalDays) * dayWidth);
			DateTime curr = start.Date.AddDays(1);
			while (x < img.Width)
			{
				x = count * dayWidth + padding + offset;
				g.DrawLine(gridPen, x, 0, x, img.Height);
				g.DrawString($"{curr.ToShortDateString()} ({curr.DayOfWeek})", font, fontBrush, x + 1, 2);
				count++;
				curr = curr.AddDays(1);
			}
		}

		static void CreateAreas(List<Schedule.Task> tasks, List<Area> areas, DateTime start, DateTime end, int dayWidth, Graphics g, Font f)
		{
			foreach (var t in tasks)
			{
				int x = (int)((t.Start - start).TotalDays * dayWidth);
				int y = areas.Count > 0 ? areas[areas.Count - 1].Rect.Bottom + 2 : 1;
				int w = (int)(t.Time.TotalDays * dayWidth);

				var a = new Area
				{
					Task = t,
					DisplayText = $"({t.Label}) {t.Description}",
				};
				var size = g.MeasureString(a.DisplayText, f);
				a.TextRect = new Rectangle(x + 2, y + 2, (int)size.Width, (int)size.Height);
				a.Rect = new Rectangle(x, y, w, a.TextRect.Height + 4);
				if (t.Tasks.Count > 0)
				{
					a.TextRect.X = x;
					a.TextRect.Y = y;
					a.Rect.Height *= 2;
					if (a.Rect.Width == 0) { a.Rect.Width = a.TextRect.Width + 4; }
				}
				else if (w == 0)
				{
					a.TextRect.X = x + 6;
					a.TextRect.Y = y + a.Rect.Height / 2 - 8;
				}
				else if (a.TextRect.Width + 4 > a.Rect.Width)
				{
					a.TextRect.X = a.Rect.Right + 2;
				}

				areas.Add(a);
				CreateAreas(t.Tasks, areas, start, end, dayWidth, g, f);
			}
		}

		static void CreateConnections(List<Area> areas, List<Connection> connections)
		{
			foreach (var a in areas)
			{
				foreach (var t in a.Task.Children)
				{
					connections.Add(new Connection
					{
						Parent = a,
						Child = areas.First(ar => ar.Task == t)
					});
				}
			}
		}
	}
}

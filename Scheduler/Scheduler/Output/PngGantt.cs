using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
			bool hourGrid = schedule.Metadata.ContainsKey("hourgrid") ? Convert.ToBoolean(schedule.Metadata["hourgrid"]) : true;

			var areas = new List<Area>();
			var connections = new List<Connection>();

			var start = schedule.Tasks.Min(t => t.Start);
			if (schedule.Start < start) { start = schedule.Start; }

			var end = schedule.Tasks.Max(t => t.Start + t.Time);
			if (schedule.Start > end) { end = schedule.Start; }

			var font = new Font("Arial", 8);
			var fontBrush = new SolidBrush(Color.Black);
			var gridPen = new Pen(Color.FromArgb(128, 192, 255), 2);
			var quarterPen = new Pen(Color.FromArgb(128, 192, 255));
			var subPen = new Pen(Color.FromArgb(224, 224, 255));
			var dateBrush = new SolidBrush(Color.Blue);

			var tImg = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
			var tG = Graphics.FromImage(tImg);
			CreateAreas(schedule.Tasks, areas, start, end, dayWidth, tG, font);
			CreateConnections(areas, connections);

			int width = areas.Max(a => Math.Max(a.Rect.Right, a.TextRect.Right));
			int height = areas.Max(a => Math.Max(a.Rect.Bottom, a.TextRect.Bottom));

			var img = new Bitmap(width + 2 * padding, height + 2 * padding, PixelFormat.Format32bppArgb);
			var g = Graphics.FromImage(img);
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.Clear(Color.White);
			DrawDateGrid(img, g, font, dateBrush, gridPen, quarterPen, subPen, start, end, dayWidth, padding, hourGrid);

			var c_incomplete = Color.Red;
			var c_complete = Color.Blue;

			var connPen = new Pen(Color.FromArgb(96, 96, 96));
			var incompletePen = new Pen(c_incomplete);
			var completePen = new Pen(c_complete);
			var areaBrush = new SolidBrush(Color.FromArgb(150, 255, 255, 255));
			var progressBrush = new SolidBrush(Color.FromArgb(150, 128, 128, 255));
			var completeBrush = new SolidBrush(c_complete);
			var incompleteBrush = new SolidBrush(c_incomplete);

			foreach (var c in connections)
			{
				var pp = new Rectangle(c.Parent.Rect.X + padding, c.Parent.Rect.Y + padding, c.Parent.Rect.Width, c.Parent.Rect.Height);
				var pc = new Rectangle(c.Child.Rect.X + padding, c.Child.Rect.Y + padding, c.Child.Rect.Width, c.Child.Rect.Height);
				DrawConnection(g, pp, pc, connPen);
			}

			foreach (var a in areas)
			{
				if (a.Task.Tasks.Count > 0)
				{
					const int groupThickness = 3;

					var pr = new Rectangle(a.Rect.X + padding, a.Rect.Y + padding, a.Rect.Width, a.Rect.Height);
					g.FillPolygon(a.Task.Percent == 100 ? completeBrush : incompleteBrush, new[] { new Point(pr.X, pr.Bottom), new Point(pr.X, pr.Y), new Point(pr.Right, pr.Y), new Point(pr.Right, pr.Bottom), new Point(pr.Right - groupThickness, pr.Y + groupThickness), new Point(pr.X + groupThickness, pr.Y + groupThickness) });
				}
				else if (a.Task.Time > TimeSpan.Zero)
				{
					var r = new Rectangle(a.Rect.X + padding, a.Rect.Y + padding, a.Rect.Width, a.Rect.Height);
					int perSplit = (int)(r.Width * a.Task.Percent / 100);
					g.FillRectangle(progressBrush, r.X, r.Y, perSplit, r.Height);
					g.FillRectangle(areaBrush, r.X + perSplit, r.Y, r.Width - perSplit, r.Height);
					g.DrawRectangle(a.Task.Percent == 100 ? completePen : incompletePen, r);
				}
				else
				{
					int px = a.Rect.X + padding;
					int py = a.Rect.Y + padding;
					int hw = a.Rect.Width / 2;
					int hh = a.Rect.Height / 2;
					g.FillPolygon(a.Task.Percent == 100 ? completeBrush : incompleteBrush, new[] { new Point(px + hw, py), new Point(px, py + hh), new Point(px + hw, py + a.Rect.Height), new Point(px + a.Rect.Width, py + hh) });
				}

				g.DrawString(a.DisplayText, font, fontBrush, a.TextRect.X + padding, a.TextRect.Y + padding);
			}

			img.Save(input + ".gantt.png", ImageFormat.Png);
		}

		static void DrawConnection(Graphics g, Rectangle parent, Rectangle child, Pen pen)
		{
			var points = new List<Point>();
			var startPoint = new Point(parent.Right + 1, parent.Y + parent.Height / 2);
			points.Add(startPoint);
			var offsetPoint = new Point(startPoint.X + 4, startPoint.Y);
			points.Add(offsetPoint);
			var endPoint = new Point(child.X + 6, (parent.Y < child.Y ? child.Y - 1 : child.Bottom + 1));

			if (offsetPoint.X < endPoint.X)
			{
				points.Add(new Point(endPoint.X, offsetPoint.Y));
			}
			else
			{
				int y = offsetPoint.Y > endPoint.Y ? parent.Y - 2 : parent.Bottom + 2;
				points.Add(new Point(offsetPoint.X, y));
				points.Add(new Point(endPoint.X, y));
			}

			points.Add(endPoint);

			g.DrawLines(pen, points.ToArray());

			const int arrowSize = 4;
			if (parent.Y < child.Y)
			{
				g.FillPolygon(pen.Brush, new[] { endPoint, new Point(endPoint.X - arrowSize, endPoint.Y - arrowSize), new Point(endPoint.X + arrowSize, endPoint.Y - arrowSize) });
			}
			else
			{
				g.FillPolygon(pen.Brush, new[] { endPoint, new Point(endPoint.X + arrowSize, endPoint.Y + arrowSize), new Point(endPoint.X - arrowSize, endPoint.Y + arrowSize) });
			}
		}

		static void DrawDateGrid(Image img, Graphics g, Font font, Brush fontBrush, Pen gridPen, Pen quarterPen, Pen subPen, DateTime start, DateTime end, int dayWidth, int padding, bool hours)
		{
			DateTime curr = start.Date;
			int x = 0;
			while (x < img.Width)
			{
				x = (int)((curr - start).TotalDays * dayWidth) + padding;
				if (x >= 0 && x < img.Width)
				{
					bool main = curr.Hour == 0;
					bool quarter = curr.Hour % 6 == 0;
					g.DrawLine((main ? gridPen : (quarter ? quarterPen : subPen)), x, (quarter ? 0 : 16), x, img.Height);
					if (main)
					{
						g.DrawString($"{curr.ToShortDateString()} ({curr.DayOfWeek})", font, fontBrush, x + 1, 2);
					}
					else if (hours && quarter)
					{
						g.DrawString(curr.Hour.ToString(), font, fontBrush, x + 1, 2);
					}
				}
				curr = hours ? curr.AddHours(1) : curr.AddDays(1);
			}
		}

		static void CreateAreas(List<Schedule.Task> tasks, List<Area> areas, DateTime start, DateTime end, int dayWidth, Graphics g, Font f)
		{
			foreach (var t in tasks)
			{
				int x = (int)((t.Start - start).TotalDays * dayWidth);
				int y = areas.Count > 0 ? areas[areas.Count - 1].Rect.Bottom + 6 : 0;
				int w = (int)(t.Time.TotalDays * dayWidth);

				var a = new Area
				{
					Task = t,
					DisplayText = $"({t.Label}) {t.Description}",
				};
				var size = g.MeasureString(a.DisplayText, f);
				a.TextRect = new Rectangle(x + 2, y + 2, (int)size.Width, (int)size.Height);
				a.Rect = new Rectangle(x, y, w, a.TextRect.Height + 4);

				int minWidth = t.Tasks.Count > 0 ? 8 : 2;

				if (t.Time == TimeSpan.Zero)
				{
					a.Rect.X -= 6;
					a.Rect.Width = 12;
				}
				else if (a.Rect.Width < minWidth) { a.Rect.Width = minWidth; }
				if (a.TextRect.Width + 4 > a.Rect.Width) { a.TextRect.X = a.Rect.Right + 2; }

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

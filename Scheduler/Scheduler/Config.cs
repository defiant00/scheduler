using System.Collections.Generic;

namespace Scheduler
{
	public class Config
	{
		public Dictionary<string, string> Flags = new Dictionary<string, string>();
		public List<string> Files = new List<string>();

		public bool IsSet(string flag)
		{
			return Flags.ContainsKey(flag);
		}

		public string this[string flag]
		{
			get { return Flags[flag]; }
			set { Flags[flag] = value; }
		}

		public Config(string[] args)
		{
			if (args.Length > 0)
			{
				foreach (string a in args)
				{
					if (a[0] == '/')
					{
						int ind = a.IndexOf(':');
						string key = a;
						string val = null;
						if (ind > -1)
						{
							key = a.Substring(0, ind);
							val = a.Substring(ind + 1);
						}
						Flags[key.Substring(1)] = val;
					}
					else { Files.Add(a); }
				}
			}
		}
	}
}

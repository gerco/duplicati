﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LocalizationTool2
{
	class MainClass
	{
		public static int Main(string[] args)
		{
			var sourcefolder = Environment.CurrentDirectory;
			if (args == null || args.Length != 0)
				sourcefolder = args[0];

			sourcefolder = Path.GetFullPath(sourcefolder.Replace("~", Environment.GetEnvironmentVariable("HOME")));


			if (!Directory.Exists(sourcefolder))
			{
				Console.WriteLine("No such directory: {0}", sourcefolder);
				return 1;
			}

			sourcefolder = Path.GetFullPath(sourcefolder);
			Console.WriteLine("Using directory {0}, scanning ....", sourcefolder);


			var searchlist = new Dictionary<string, Regex>();
			searchlist.Add("html", new Regex("((\\{\\{)|(ng-bind-html\\s*=\\s*\"))\\s*\\'(?<sourcestring>(\\\\\\'|[^\\'])+)\\'\\s*\\|\\s*localize(\\s|\\:|\\})", RegexOptions.Multiline | RegexOptions.IgnoreCase));
			searchlist.Add("js", new Regex("Localization\\.localize\\(\\s*((\\'(?<sourcestring>(\\\\\\'|[^\\'])+)\\')|(\\\"(?<sourcestring>(\\\\\\\"|[^\\\"])+)\\\"))", RegexOptions.Multiline | RegexOptions.IgnoreCase));
			searchlist.Add("cs", new Regex("LC.L\\s*\\(((@\\s*\"(?<sourcestring>(\"\"|[^\"])+))|(\"(?<sourcestring>(\\\\\"|[^\"])+)))\"\\s*\\)", RegexOptions.Multiline | RegexOptions.IgnoreCase));

			var map = new Dictionary<string, LocalizationEntry>();

			foreach (var ext in searchlist.Keys)
			{
				var re = searchlist[ext];
				foreach (var f in Directory.GetFiles(sourcefolder, "*." + ext, SearchOption.AllDirectories))
				{
					var txt = File.ReadAllText(f);
					foreach (Match match in re.Matches(txt))
					{
						var linepos = txt.Substring(match.Index).Count(x => x == '\n');
						var str = match.Groups["sourcestring"].Value;
						LocalizationEntry le;
						if (!map.TryGetValue(str, out le))
							map[str] = new LocalizationEntry(str, Path.GetFileName(f), linepos);
						else
							le.AddSource(Path.GetFileName(f), linepos);						
					}
				}
			}

			File.WriteAllText(Path.Combine(sourcefolder, "translations.json"), Newtonsoft.Json.JsonConvert.SerializeObject(map.Values.OrderBy(x => x.SourceLocations.FirstOrDefault()).ToArray(), Newtonsoft.Json.Formatting.Indented));
			File.WriteAllText(Path.Combine(sourcefolder, "translations-list.json"), Newtonsoft.Json.JsonConvert.SerializeObject(map.Select(x => x.Key).OrderBy(x => x).ToArray(), Newtonsoft.Json.Formatting.Indented));

			return 0;

		}
	}
}
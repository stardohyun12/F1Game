Application.runInBackground = true;
TownPathBot.Only = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/tmp/onlyfail.txt")).Trim();
TownPathBot.Begin("cover", 3f);
return "only " + TownPathBot.Only.Split(',').Length;

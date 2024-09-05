using Maid.Docs.Scraper.CSharp;

using Newtonsoft.Json;

string configPath = "D:\\docs.config.json";
var config = JsonConvert.DeserializeObject<DocsConfig>(File.ReadAllText(configPath));
if(config is null) throw new Exception("Config is null");
//await CodeScraperCSharp.ScrapeAllTypes(config);
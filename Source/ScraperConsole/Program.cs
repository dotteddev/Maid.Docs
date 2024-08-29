using Maid.Docs.Scraper;

using Newtonsoft.Json;

string configPath = "D:\\docs.config.json";
var config = JsonConvert.DeserializeObject<DocConfig>(File.ReadAllText(configPath));
if(config is null) throw new Exception("Config is null");
await CodeScraper.ScrapeCodeAsync(config);
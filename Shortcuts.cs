// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
using Newtonsoft.Json;
using System.Collections.Generic;

public class ShortcutsJsonRoot
{
    [JsonProperty("Apps")]
    public List<App> Apps { get; set; }
}

public class App
{
    [JsonProperty("Name")]
    public string Name { get; set; }

    [JsonProperty("FriendlyName")]
    public string FriendlyName { get; set; }

    [JsonProperty("Shortcuts")]
    public List<Shortcut> Shortcuts { get; set; }
}

public class Shortcut
{
    [JsonProperty("Modifier")]
    public string Modifier { get; set; }

    [JsonProperty("Key")]
    public string Key { get; set; }

    [JsonProperty("Description")]
    public string Description { get; set; }

    [JsonProperty("Extended")]
    public string Extended { get; set; }
}


// ShortcutsJsonRoot myDeserializedClass = JsonConvert.DeserializeObject<ShortcutsJsonRoot>(myJsonResponse);
using Newtonsoft.Json;
using System.Collections.Generic;

public class App
{
    [JsonProperty("Name")]
    public string Name { get; set; }

    [JsonProperty("FriendlyName")]
    public string FriendlyName { get; set; }

    [JsonProperty("ShortcutGroups")]
    public List<ShortcutGroup> ShortcutGroups { get; set; }
}

public class ShortcutsJsonRoot
{
    [JsonProperty("Apps")]
    public List<App> Apps { get; set; }
}

public class Shortcut
{
    [JsonProperty("Keys")]
    public List<List<string>> Keys { get; set; }

    [JsonProperty("Description")]
    public string Description { get; set; }

    [JsonProperty("Extended")]
    public string Extended { get; set; }
}

public class ShortcutGroup
{
    [JsonProperty("Name")]
    public string Name { get; set; }

    [JsonProperty("Shortcuts")]
    public List<Shortcut> Shortcuts { get; set; }
}


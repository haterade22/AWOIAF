using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace DOTS.Tests.Features.FactionMap;

[TestClass]
public class FactionMapDataTests
{
    private static readonly string ModuleDataPath = Path.GetFullPath(
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..",
            "Main", "_Module", "ModuleData"));

    private static readonly Regex KeyTokenRegex = new(@"^\{=([^}]+)\}", RegexOptions.Compiled);

    private static JObject LoadFactions()
    {
        var path = Path.Combine(ModuleDataPath, "factionmap", "factions.json");
        Assert.IsTrue(File.Exists(path), $"factions.json missing at {path}");
        return JObject.Parse(File.ReadAllText(path));
    }

    [TestMethod]
    public void FactionsJson_Parses_AsValidJsonObject()
    {
        // Bootstrap: factions.json starts empty for DOTS (GoT factions authored later).
        var root = LoadFactions();
        Assert.IsNotNull(root, "factions.json must parse as a valid JSON object");
    }

    [TestMethod]
    public void FactionsJson_PlayableFactions_HaveMinimumContentSections()
    {
        // Structural check: any playable faction that IS present must have the required arrays.
        // Bootstrap state: 0 playable factions is acceptable.
        var root = LoadFactions();
        foreach (var prop in root.Properties())
        {
            if (prop.Value is not JObject faction) continue;
            if (faction.Value<bool?>("playable") != true) continue;

            var key = prop.Name;
            AssertArrayLength(faction, "perks", min: 2, key);
            AssertArrayLength(faction, "bonuses", min: 3, key);
            AssertArrayLength(faction, "special_units", min: 1, key);
            AssertArrayLength(faction, "strengths", min: 3, key);
            AssertArrayLength(faction, "weaknesses", min: 2, key);
            AssertArrayLength(faction, "traits", min: 3, key);
        }
    }

    private static void AssertArrayLength(JObject faction, string field, int min, string factionKey)
    {
        var arr = faction[field] as JArray;
        Assert.IsNotNull(arr, $"'{factionKey}'.{field} must be an array");
        Assert.IsTrue(arr.Count >= min, $"'{factionKey}'.{field} must have ≥{min} entries (got {arr.Count})");
    }

    [TestMethod]
    public void EveryFactionStringField_StartsWithLocalizationKey()
    {
        var root = LoadFactions();
        var violations = new List<string>();

        foreach (var factionProp in root.Properties())
        {
            if (factionProp.Value is not JObject faction) continue;

            void Check(string fieldPath, string value)
            {
                if (string.IsNullOrEmpty(value)) return;
                if (!KeyTokenRegex.IsMatch(value))
                    violations.Add($"{factionProp.Name}.{fieldPath}: '{value}' (missing {{=KEY}} prefix)");
            }

            Check("name", faction.Value<string>("name") ?? "");
            Check("description", faction.Value<string>("description") ?? "");

            if (faction["traits"] is JArray traits)
                for (int i = 0; i < traits.Count; i++)
                    Check($"traits[{i}]", traits[i]?.Value<string>() ?? "");

            CheckObjectArray(faction, "perks", new[] { "name", "description" }, Check);
            CheckObjectArray(faction, "special_units", new[] { "name", "description" }, Check);

            if (faction["bonuses"] is JArray bonuses)
                for (int i = 0; i < bonuses.Count; i++)
                    if (bonuses[i] is JObject b)
                        Check($"bonuses[{i}].text", b.Value<string>("text") ?? "");

            foreach (var arrName in new[] { "strengths", "weaknesses" })
                if (faction[arrName] is JArray arr)
                    for (int i = 0; i < arr.Count; i++)
                        Check($"{arrName}[{i}]", arr[i]?.Value<string>() ?? "");
        }

        if (violations.Count > 0)
            Assert.Fail("factions.json has un-localized strings:\n" + string.Join("\n", violations));
    }

    private static void CheckObjectArray(JObject faction, string arrName, string[] fields, Action<string, string> check)
    {
        if (faction[arrName] is not JArray list) return;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] is not JObject obj) continue;
            foreach (var field in fields)
                check($"{arrName}[{i}].{field}", obj.Value<string>(field) ?? "");
        }
    }

    [TestMethod]
    public void EveryFactionMapKey_HasMatchingStringInDotsModuleStrings()
    {
        var root = LoadFactions();
        var jsonKeys = new HashSet<string>();
        foreach (var s in EnumerateStrings(root))
        {
            var m = KeyTokenRegex.Match(s);
            if (m.Success && m.Groups[1].Value.StartsWith("dots_faction_"))
                jsonKeys.Add(m.Groups[1].Value);
        }

        var stringsXml = File.ReadAllText(Path.Combine(ModuleDataPath, "dots_module_strings.xml"));
        var xmlKeys = new HashSet<string>(
            Regex.Matches(stringsXml, @"<string id=""([^""]+)""")
                 .Cast<Match>()
                 .Select(m => m.Groups[1].Value));

        var missing = jsonKeys.Where(k => !xmlKeys.Contains(k)).OrderBy(k => k).ToList();
        if (missing.Count > 0)
        {
            Assert.Fail(
                $"{missing.Count} factionmap keys referenced in factions.json have no matching " +
                $"<string id=\"...\"> in dots_module_strings.xml. Re-run " +
                $"`python tools/harvest_factionmap_strings.py`. First 10 missing:\n" +
                string.Join("\n", missing.Take(10)));
        }
    }

    private static IEnumerable<string> EnumerateStrings(JToken node)
    {
        switch (node)
        {
            case JValue v when v.Type == JTokenType.String:
                yield return v.Value<string>() ?? "";
                break;
            case JObject obj:
                foreach (var p in obj.Properties())
                    foreach (var s in EnumerateStrings(p.Value))
                        yield return s;
                break;
            case JArray arr:
                foreach (var item in arr)
                    foreach (var s in EnumerateStrings(item))
                        yield return s;
                break;
        }
    }
}

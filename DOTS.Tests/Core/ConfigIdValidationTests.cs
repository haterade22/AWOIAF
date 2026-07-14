using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DOTS.Tests.Core;

[TestClass]
public class ConfigIdValidationTests
{
    private static readonly HashSet<string> ValidCultureIds = new HashSet<string>
    {
        // Custom cultures (LOTR names as StringIds)
        "gondor", "mordor", "erebor", "rivendell", "lothlorien",
        "mirkwood", "isengard", "gundabad", "dolguldur", "umbar",
        // New orc cultures (Misty Mountains expansion)
        "goblin", "mistymountainorcs",
        // XSLT cultures (vanilla engine StringIds)
        "vlandia", "empire", "aserai", "khuzait", "sturgia", "battania"
    };

    private static readonly HashSet<string> ValidKingdomIds = new HashSet<string>
    {
        "empire_w", "empire_s", "empire", "vlandia", "battania",
        "aserai", "khuzait", "sturgia", "erebor", "rivendell",
        "lothlorien", "mirkwood", "isengard", "gundabad", "dolguldur",
        "umbar", "shaghana", "abanissa",
        // New kingdoms (Misty Mountains expansion): goblin + mistymountainorcs = new cultures;
        // lindon reuses Culture.rivendell; bluecraig reuses Culture.goblin.
        "goblin", "mistymountainorcs", "lindon", "bluecraig"
    };

    private static string FindModuleDataPath()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "Main", "_Module", "ModuleData");
            if (Directory.Exists(candidate))
                return candidate;
            dir = Directory.GetParent(dir)?.FullName;
        }

        return null;
    }

    private static HashSet<string> CulturesWithFlaggedRoster(string filePath, string flagAttribute)
    {
        var cultures = new HashSet<string>();
        if (!File.Exists(filePath))
            return cultures;
        foreach (var roster in XDocument.Load(filePath).Descendants("EquipmentRoster"))
        {
            if (roster.Element("Flags")?.Attribute(flagAttribute)?.Value != "true")
                continue;
            var culture = roster.Attribute("culture")?.Value?.Replace("Culture.", "");
            if (!string.IsNullOrEmpty(culture))
                cultures.Add(culture);
        }
        return cultures;
    }

    // --- Child-generation equipment templates (RCA 2026-06-02; re-pinned 2026-07-14) ---
    // A custom culture whose clans get lords triggers InitialChildGeneration -> HeroCreator.CreateChild
    // -> EquipmentSelectionModel.GetEquipmentForInitialChildrenGeneration. That model searches for a
    // culture-matching equipment roster flagged IsChildEquipmentTemplate (young) or
    // IsTeenagerEquipmentTemplate (teen), both also IsLordTemplate; if NONE exists it returns null and
    // vanilla NREs on new-game in EquipmentHelper.AssignHeroEquipmentFromEquipment. Originally pinned
    // to the LOTR-era goblin + mistymountainorcs regression; now data-driven over the GoT faction
    // layer: every custom culture that any clan uses (characters/clans.xml + the spclans.xslt
    // vanilla-clan retargets) must ship child/teen/lord rosters, or new games crash.
    [TestMethod]
    public void ChildGenerationCultures_HaveChildTeenAndLordEquipmentTemplates()
    {
        var md = FindModuleDataPath();
        if (md == null) { Assert.Inconclusive("ModuleData path not found — run from repo root"); return; }

        var childFile = Path.Combine(md, "equipmentsets", "dots_child_equipment_templates.xml");
        var lordFile = Path.Combine(md, "equipmentsets", "dots_lord_template_equipment.xml");

        var childCultures = CulturesWithFlaggedRoster(childFile, "IsChildEquipmentTemplate");
        var teenCultures = CulturesWithFlaggedRoster(lordFile, "IsTeenagerEquipmentTemplate");
        var lordCultures = CulturesWithFlaggedRoster(lordFile, "IsLordTemplate");

        // Vanilla cultures are covered by vanilla's own sandbox_equipment_sets.xml rosters.
        var vanillaCovered = new HashSet<string>(StringComparer.Ordinal)
            { "empire", "sturgia", "aserai", "vlandia", "battania", "khuzait" };

        var requiredCultures = new SortedSet<string>(StringComparer.Ordinal);
        var clansFile = Path.Combine(md, "characters", "clans.xml");
        if (File.Exists(clansFile))
            foreach (var clan in XDocument.Load(clansFile).Descendants("Faction"))
            {
                var c = clan.Attribute("culture")?.Value?.Replace("Culture.", "");
                if (!string.IsNullOrEmpty(c) && !vanillaCovered.Contains(c))
                    requiredCultures.Add(c);
            }
        // spclans.xslt retargets vanilla clans onto custom cultures (e.g. Greyjoy -> Ironborn).
        var spclansXslt = Path.Combine(md, "spclans.xslt");
        if (File.Exists(spclansXslt))
        {
            XNamespace xsl = "http://www.w3.org/1999/XSL/Transform";
            foreach (var attr in XDocument.Load(spclansXslt).Descendants(xsl + "attribute"))
                if (attr.Attribute("name")?.Value == "culture")
                {
                    var c = attr.Value.Replace("Culture.", "");
                    if (!string.IsNullOrEmpty(c) && !vanillaCovered.Contains(c))
                        requiredCultures.Add(c);
                }
        }

        Assert.AreNotEqual(0, requiredCultures.Count,
            "No custom clan cultures found — clans.xml/spclans.xslt missing? The guard would be vacuous.");
        foreach (var c in requiredCultures)
            Assert.IsTrue(childCultures.Contains(c),
                $"Culture '{c}' has clans/lords but no IsChildEquipmentTemplate roster in dots_child_equipment_templates.xml — " +
                "InitialChildGeneration NREs in HeroCreator.CreateChild for this culture's lords.");

        // Consistency: every culture with child templates must also have teenager + adult lord
        // templates (a child grows into a teen then adult; child-gen draws teen equipment by age).
        var missingTeen = childCultures.Where(c => !teenCultures.Contains(c)).OrderBy(c => c).ToList();
        var missingLord = childCultures.Where(c => !lordCultures.Contains(c)).OrderBy(c => c).ToList();
        Assert.AreEqual(0, missingTeen.Count,
            $"Cultures with child templates but no IsTeenagerEquipmentTemplate lord roster: {string.Join(", ", missingTeen)}");
        Assert.AreEqual(0, missingLord.Count,
            $"Cultures with child templates but no IsLordTemplate roster: {string.Join(", ", missingLord)}");
    }

    [TestMethod]
    public void NewOrcCultures_HaveChildEducationEquipmentRosters()
    {
        var md = FindModuleDataPath();
        if (md == null) { Assert.Inconclusive("ModuleData path not found"); return; }

        var eduFile = Path.Combine(md, "equipmentsets", "dots_education_equipment_templates.xml");
        Assert.IsTrue(File.Exists(eduFile), "dots_education_equipment_templates.xml missing");
        var ids = XDocument.Load(eduFile).Descendants("EquipmentRoster")
            .Select(r => r.Attribute("id")?.Value ?? "").ToList();
        foreach (var c in new[] { "goblin", "mistymountainorcs" })
            Assert.IsTrue(ids.Any(id => id.StartsWith("child_education_equipments") && id.EndsWith("_" + c)),
                $"Culture '{c}' has no child_education_equipments_*_{c} rosters — childhood education events NRE.");
    }

    // --- Settlement Guards: Spear culture IDs ---

    [TestMethod]
    public void SettlementGuards_SpearCultureIds_AllValid()
    {
        var moduleDataPath = FindModuleDataPath();
        if (moduleDataPath == null)
            Assert.Inconclusive("ModuleData path not found — run from repo root");

        var path = Path.Combine(moduleDataPath, "settlement_guards", "settlement_guards_config.xml");
        if (!File.Exists(path))
            Assert.Inconclusive($"Config file not found: {path}");

        var doc = XDocument.Load(path);
        var spearsEl = doc.Root.Element("Spears");
        if (spearsEl == null)
            Assert.Fail("No <Spears> element in settlement_guards_config.xml");

        var invalid = new List<string>();
        foreach (var el in spearsEl.Elements("Spear"))
        {
            var culture = el.Attribute("culture")?.Value;
            if (!string.IsNullOrEmpty(culture) && !ValidCultureIds.Contains(culture))
                invalid.Add(culture);
        }

        Assert.AreEqual(0, invalid.Count,
            $"Invalid culture IDs in Spears config: {string.Join(", ", invalid)}. " +
            "XSLT cultures must use engine IDs (vlandia, empire, aserai, khuzait, sturgia, battania), not lore names.");
    }

    // --- Settlement Guards: Culture fallback IDs ---

    [TestMethod]
    public void SettlementGuards_CultureFallbackIds_AllValid()
    {
        var moduleDataPath = FindModuleDataPath();
        if (moduleDataPath == null)
            Assert.Inconclusive("ModuleData path not found");

        var path = Path.Combine(moduleDataPath, "settlement_guards", "settlement_guards_config.xml");
        if (!File.Exists(path))
            Assert.Inconclusive($"Config file not found: {path}");

        var doc = XDocument.Load(path);
        var invalid = new List<string>();

        foreach (var el in doc.Root.Elements("Culture"))
        {
            var id = el.Attribute("id")?.Value;
            if (!string.IsNullOrEmpty(id) && !ValidCultureIds.Contains(id))
                invalid.Add(id);
        }

        Assert.AreEqual(0, invalid.Count,
            $"Invalid culture IDs in Culture fallback config: {string.Join(", ", invalid)}");
    }

    // --- Lore name guard: catch common mistakes ---

    [TestMethod]
    [DataRow("rohan", "vlandia")]
    [DataRow("dunland", "empire")]
    [DataRow("harad", "aserai")]
    [DataRow("rhun", "khuzait")]
    [DataRow("dale", "sturgia")]
    [DataRow("khand", "battania")]
    [DataRow("dol_guldur", "dolguldur")]
    public void LoreNames_AreNotValidCultureIds(string loreName, string correctId)
    {
        Assert.IsFalse(ValidCultureIds.Contains(loreName),
            $"'{loreName}' should NOT be a valid culture ID. Use '{correctId}' instead.");
        Assert.IsTrue(ValidCultureIds.Contains(correctId),
            $"'{correctId}' should be a valid culture ID.");
    }

    // --- ValidCultureIds set is complete ---

    [TestMethod]
    public void ValidCultureIds_Contains18Cultures()
    {
        Assert.AreEqual(18, ValidCultureIds.Count,
            "Expected 18 valid culture IDs (12 custom incl. goblin + mistymountainorcs, + 6 XSLT)");
    }

    [TestMethod]
    public void ValidKingdomIds_Contains22Kingdoms()
    {
        Assert.AreEqual(22, ValidKingdomIds.Count,
            "Expected 22 valid kingdom IDs (incl. goblin, mistymountainorcs, lindon, bluecraig)");
    }

    // --- Character-creation coverage (RCA 2026-06-02) ---
    // NarrativeMenuBuilder filters CC narrative entries by culture_id == selectedCulture.StringId.
    // A selectable culture with no entries in the culture-keyed menus renders the Family/Youth/
    // Adulthood/Education stages BLANK. goblin + mistymountainorcs shipped playable but with no CC
    // content (and no cc_body_properties body for the preview). Childhood is culture-independent.
    [TestMethod]
    public void NewCultures_HaveCharacterCreationMenuEntries()
    {
        var md = FindModuleDataPath();
        if (md == null) { Assert.Inconclusive("ModuleData path not found — run from repo root"); return; }
        var cc = Path.Combine(md, "charactercreation");
        string[] cultureKeyedMenus = { "parents", "youth", "adulthood", "education" };

        // Bootstrap: no GoT-specific playable cultures defined yet.
        // Add GoT house culture IDs here (e.g. "stark", "lannister") when CC content is authored.
        var newCultures = new string[] { };
        foreach (var culture in newCultures)
        {
            foreach (var menu in cultureKeyedMenus)
            {
                var text = File.ReadAllText(Path.Combine(cc, menu + "_menu.json"));
                Assert.IsTrue(text.Contains($"\"culture_id\": \"{culture}\""),
                    $"{menu}_menu.json has no entry for culture '{culture}' — the CC {menu} stage renders blank.");
            }
            var body = XDocument.Load(Path.Combine(cc, "cc_body_properties.xml"));
            Assert.IsTrue(body.Descendants("Culture").Any(c => c.Attribute("id")?.Value == culture),
                $"cc_body_properties.xml has no <Culture id=\"{culture}\"> — CC body preview falls back to a default body.");
        }
    }
}

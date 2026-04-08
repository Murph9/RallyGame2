using murph9.RallyGame2.godot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace murph9.RallyGame2.godot.Cars.Init.Parts;

/// <summary>
/// Loads parts_registry.json and provides canonical metadata (color, icon, levelCost)
/// that individual car data files can omit. Car files may still override levelCost
/// by providing their own value — the registry acts as a fallback.
/// </summary>
public class PartRegistry {
    private static PartRegistry _instance;
    public static PartRegistry Instance => _instance ??= Load();

    /// <summary>Entries keyed by part code.</summary>
    private readonly Dictionary<string, PartRegistryEntry> _entries;

    private PartRegistry(IEnumerable<PartRegistryEntry> entries) {
        _entries = entries.ToDictionary(e => e.Code, StringComparer.OrdinalIgnoreCase);
    }

    private static PartRegistry Load() {
        var raw = FileLoader.ReadJsonFile<PartRegistryFile>("Cars", "Init", "Data", "parts_registry.json");
        return new PartRegistry(raw.Parts);
    }

    /// <summary>
    /// Fills in any missing metadata on <paramref name="part"/> from the registry entry
    /// that matches by code. Throws if the part code is not in the registry.
    /// Car-provided values (non-null / non-empty) always take precedence over registry defaults.
    /// </summary>
    public void Merge(PartDetails part) {
        if (!_entries.TryGetValue(part.Code, out var entry))
            throw new Exception($"Part code '{part.Code}' not found in parts_registry.json. Add it to the registry or fix the code.");

        part.Name ??= entry.Name;
        part.Color ??= entry.Color;
        part.Icon ??= entry.Icon;
        part.LevelCost ??= entry.LevelCost;
    }

    /// <summary>Returns the registry entry for a given part code, or null if not found.</summary>
    public PartRegistryEntry GetEntry(string code) =>
        _entries.TryGetValue(code, out var e) ? e : null;
}

public class PartRegistryEntry {
    public string Category { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public string Color { get; set; }
    public double[] LevelCost { get; set; }
}

/// <summary>Top-level wrapper matching the parts_registry.json shape.</summary>
file class PartRegistryFile {
    public List<PartRegistryEntry> Parts { get; set; } = [];
}

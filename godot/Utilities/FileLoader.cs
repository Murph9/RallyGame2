using System;
using System.IO;
using System.Text.Json;

namespace murph9.RallyGame2.godot.Utilities;

public class FileLoader {
    private readonly static JsonSerializerOptions DEFAULT_OPTIONS = new () {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        IncludeFields = true
    };

    public static T ReadJsonFile<T>(params string[] filePaths) =>
        ReadJsonFile<T>(Path.Combine(filePaths));

    public static T ReadJsonFile<T>(string filePath, JsonSerializerOptions options = null) {
        var absolutefilePath = Path.Combine(AppContext.BaseDirectory, filePath);
        var content = File.ReadAllText(absolutefilePath);

        return JsonSerializer.Deserialize<T>(content, options ?? DEFAULT_OPTIONS);
    }
}

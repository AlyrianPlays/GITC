using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GITC.Core
{
    // Loads JSON content from Content/Data/. All path logic lives here so it can be swapped for release builds (T-16.5).
    internal static class DataLoader
    {
        public const string DATAFOLDER = "Data";

        // Shared by loading and saving: tolerant of comments/trailing commas, enums as lower_snake_case strings
        public static readonly JsonSerializerOptions options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        // Folder that holds Content/ at runtime.
        // Repo convention first (bin/<Config>/<tfm>/ back to the project dir), then the build output folder,
        // where the csproj copies Content/Data/**.json. Only the second works from `dotnet run` at the repo root.
        public static string ContentRoot()
        {
            DirectoryInfo projectDir = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent;
            if (projectDir != null)
            {
                string conventional = Path.Combine(projectDir.FullName, "Content");
                if (Directory.Exists(Path.Combine(conventional, DATAFOLDER)))
                    return conventional;
            }
            return Path.Combine(AppContext.BaseDirectory, "Content");
        }

        // relativePath is relative to Content/Data/, e.g. "companies.json" or "companies/swiftail.json"
        public static string DataPath(string relativePath)
        {
            return Path.Combine(ContentRoot(), DATAFOLDER, relativePath);
        }

        // Errors are logged and return null (default); callers decide what to do about missing content
        public static T Load<T>(string relativePath) where T : class
        {
            string path = DataPath(relativePath);
            try
            {
                return JsonSerializer.Deserialize<T>(File.ReadAllText(path), options);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("DataLoader: could not load " + path + ": " + e.Message);
                return null;
            }
        }
    }
}

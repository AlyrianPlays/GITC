using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace GITC.Core
{
    // Loads and saves SaveData as JSON in Content/Saves/save.json.
    //TODO: release builds should use Environment.SpecialFolder.ApplicationData (T-16.5)
    internal class SaveManager
    {
        public const string SAVEFOLDER = "Saves";
        public const string SAVEFILE = "save.json";

        public SaveData current { get; set; }
        public string savePath { get; set; }

        public SaveManager()
        {
            current = new SaveData();
            savePath = Path.Combine(DataLoader.ContentRoot(), SAVEFOLDER, SAVEFILE);
        }

        // Reads the save file; a missing or unreadable file starts a fresh save (a bad file is kept as save.json.bad)
        public void Load()
        {
            if (!File.Exists(savePath))
            {
                current = new SaveData();
                return;
            }
            try
            {
                SaveData loaded = JsonSerializer.Deserialize<SaveData>(File.ReadAllText(savePath), DataLoader.options);
                current = loaded ?? new SaveData();
                Migrate();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("SaveManager: could not read " + savePath + ": " + e.Message);
                try { File.Copy(savePath, savePath + ".bad", true); } catch (Exception) { }
                current = new SaveData();
            }
        }

        // Writes to a temp file first so a crash mid-write cannot corrupt the existing save
        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                string tempPath = savePath + ".tmp";
                File.WriteAllText(tempPath, JsonSerializer.Serialize(current, DataLoader.options));
                File.Move(tempPath, savePath, true);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("SaveManager: could not write " + savePath + ": " + e.Message);
            }
        }

        // Upgrade older save versions here as the shape changes
        private void Migrate()
        {
            if (current.version < SaveData.CURRENTVERSION)
                current.version = SaveData.CURRENTVERSION;
        }

        public bool IsMissionUnlocked(string missionId)
        {
            return current.unlockedMissions.Contains(missionId);
        }

        public bool IsMissionCompleted(string missionId)
        {
            return current.completedMissions.Contains(missionId);
        }

        public void UnlockMission(string missionId)
        {
            if (!current.unlockedMissions.Contains(missionId))
                current.unlockedMissions.Add(missionId);
        }

        public bool HasFlag(string flag)
        {
            return current.flags.Contains(flag);
        }

        public void SetFlag(string flag)
        {
            if (!current.flags.Contains(flag))
                current.flags.Add(flag);
        }

        public void AddSecret(string secret)
        {
            if (!current.discoveredSecrets.Contains(secret))
                current.discoveredSecrets.Add(secret);
        }

        // Autosave hook for the end of a mission (wired in from Mission in T-4.1/T-4.3)
        public void OnMissionEnded(string missionId, bool won, int opsRemaining, List<string> unlocks)
        {
            if (won)
            {
                if (!current.completedMissions.Contains(missionId))
                    current.completedMissions.Add(missionId);
                if (!current.bestScores.ContainsKey(missionId) || opsRemaining > current.bestScores[missionId])
                    current.bestScores[missionId] = opsRemaining;
                if (unlocks != null)
                    foreach (string id in unlocks)
                        UnlockMission(id);
            }
            Save();
        }

        // Copies the saved settings into UserInfo (by enum index, ignoring size mismatches from older saves)
        public void ApplySettings(UserInfo info)
        {
            for (int i = 0; i < Math.Min(current.bSettings.Count, info.bSettings.Count); i++)
                info.bSettings[i] = current.bSettings[i];
            for (int i = 0; i < Math.Min(current.iSettings.Count, info.iSettings.Count); i++)
                info.iSettings[i] = current.iSettings[i];
        }

        public void CaptureSettings(UserInfo info)
        {
            current.bSettings = new List<bool>(info.bSettings);
            current.iSettings = new List<int>(info.iSettings);
        }
    }
}

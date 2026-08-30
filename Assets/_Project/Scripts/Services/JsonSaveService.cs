using System;
using System.IO;
using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// JSON file save at Application.persistentDataPath/save.json. Corruption or a
    /// missing file falls back to a fresh profile rather than throwing.
    /// </summary>
    public sealed class JsonSaveService : ISaveService
    {
        public static string Path => System.IO.Path.Combine(Application.persistentDataPath, "save.json");

        public MetaState Load()
        {
            try
            {
                if (!File.Exists(Path)) return MetaState.CreateDefault();
                var json = File.ReadAllText(Path);
                var state = JsonUtility.FromJson<MetaState>(json);
                return state ?? MetaState.CreateDefault();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] load failed, using defaults: {e.Message}");
                return MetaState.CreateDefault();
            }
        }

        public void Save(MetaState state)
        {
            try
            {
                File.WriteAllText(Path, JsonUtility.ToJson(state, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] write failed: {e.Message}");
            }
        }

        public void Delete()
        {
            try
            {
                if (File.Exists(Path)) File.Delete(Path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] delete failed: {e.Message}");
            }
        }
    }
}

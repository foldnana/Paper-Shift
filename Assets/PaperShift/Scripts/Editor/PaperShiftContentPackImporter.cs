using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PaperShift.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PaperShift.Editor
{
    public static class PaperShiftContentPackImporter
    {
        private const string DataFolder = "Assets/PaperShift/Data";
        private const string DefaultDatabasePath = DataFolder + "/PaperShiftImportedDatabase.asset";
        private const string DefaultContentPackPath = "Tools/PaperShiftContentTool/output/PaperShiftContentPack.json";

        [MenuItem("Paper Shift/Content Tool/Import Content Pack JSON...")]
        public static void ImportContentPackJson()
        {
            var path = EditorUtility.OpenFilePanel("Import Paper Shift Content Pack", Directory.GetCurrentDirectory(), "json");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            ImportContentPackJsonAtPath(path, DefaultDatabasePath, true);
        }

        [MenuItem("Paper Shift/Content Tool/Import Default Tool Output")]
        public static void ImportDefaultToolOutput()
        {
            var absolutePath = Path.GetFullPath(DefaultContentPackPath);
            if (!File.Exists(absolutePath))
            {
                Debug.LogWarning("Paper Shift content pack not found: " + absolutePath);
                return;
            }

            ImportContentPackJsonAtPath(absolutePath, DefaultDatabasePath, true);
        }

        [MenuItem("Paper Shift/Content Tool/Assign Imported Database To Open Scene")]
        public static void AssignImportedDatabaseToOpenScene()
        {
            var database = AssetDatabase.LoadAssetAtPath<PaperShiftDatabase>(DefaultDatabasePath);
            if (database == null)
            {
                Debug.LogWarning("Paper Shift imported database not found. Import a content pack first.");
                return;
            }

            var presenters = UnityEngine.Object.FindObjectsOfType<PaperShift.Presenter.PaperShiftGamePresenter>(true);
            if (presenters == null || presenters.Length == 0)
            {
                Debug.LogWarning("No PaperShiftGamePresenter found in the open scene.");
                return;
            }

            for (var i = 0; i < presenters.Length; i++)
            {
                presenters[i].Database = database;
                EditorUtility.SetDirty(presenters[i]);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Assigned imported database to " + presenters.Length + " presenter(s): " + DefaultDatabasePath);
        }

        public static PaperShiftDatabase ImportContentPackJsonAtPath(string path, string targetAssetPath, bool selectAsset)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Content pack path is empty.");
            }

            var absolutePath = Path.IsPathRooted(path) ? path : Path.GetFullPath(path);
            var json = File.ReadAllText(absolutePath, Encoding.UTF8);
            var pack = JsonUtility.FromJson<PaperShiftContentPack>(json);
            if (pack == null)
            {
                throw new InvalidDataException("Invalid Paper Shift content pack JSON: " + absolutePath);
            }

            if (!string.Equals(pack.Format, "PaperShiftContentPack", StringComparison.Ordinal))
            {
                Debug.LogWarning("Content pack Format is not PaperShiftContentPack. Import will still continue.");
            }

            EnsureFolder(DataFolder);
            var replace = string.Equals(pack.MergeMode, "replace", StringComparison.OrdinalIgnoreCase);
            var database = AssetDatabase.LoadAssetAtPath<PaperShiftDatabase>(targetAssetPath);
            if (database == null)
            {
                database = PaperShiftSeedData.CreateDefaultDatabase();
                AssetDatabase.CreateAsset(database, targetAssetPath);
            }
            else if (replace)
            {
                Clear(database);
            }
            else
            {
                PaperShiftSeedData.ApplyRuntimeDefaults(database);
            }

            ApplyPack(database, pack, replace);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (selectAsset)
            {
                Selection.activeObject = database;
            }

            Debug.Log("Imported Paper Shift content pack: " + absolutePath + " -> " + targetAssetPath);
            return database;
        }

        private static void ApplyPack(PaperShiftDatabase database, PaperShiftContentPack pack, bool replace)
        {
            database.Rarities = MergeById(database.Rarities, pack.Rarities, item => item == null ? string.Empty : item.Id, replace);
            database.Stats = MergeById(database.Stats, pack.Stats, item => item == null ? string.Empty : item.Id, replace);
            database.Tags = MergeById(database.Tags, pack.Tags, item => item == null ? string.Empty : item.Id, replace);
            database.WorkTags = MergeById(database.WorkTags, pack.WorkTags, item => item == null ? string.Empty : item.Id, replace);
            database.Companies = MergeById(database.Companies, pack.Companies, item => item == null ? string.Empty : item.Id, replace);
            database.Events = MergeById(database.Events, pack.Events, item => item == null ? string.Empty : item.Id, replace);
            database.LaterLifeRules = MergeById(database.LaterLifeRules, pack.LaterLifeRules, item => item == null ? string.Empty : item.Id, replace);
            database.LastNames = MergeStrings(database.LastNames, pack.LastNames, replace);
            database.MaleFirstNames = MergeStrings(database.MaleFirstNames, pack.MaleFirstNames, replace);
            database.FemaleFirstNames = MergeStrings(database.FemaleFirstNames, pack.FemaleFirstNames, replace);
        }

        private static void Clear(PaperShiftDatabase database)
        {
            database.Rarities = new RarityDefinition[0];
            database.Stats = new StatDefinition[0];
            database.Tags = new TagDefinition[0];
            database.WorkTags = new WorkTagDefinition[0];
            database.Companies = new CompanyDefinition[0];
            database.Events = new GameEventDefinition[0];
            database.LaterLifeRules = new LaterLifeRuleDefinition[0];
            database.LastNames = new string[0];
            database.MaleFirstNames = new string[0];
            database.FemaleFirstNames = new string[0];
        }

        private static T[] MergeById<T>(T[] current, T[] incoming, Func<T, string> idOf, bool replace) where T : class
        {
            if (incoming == null || incoming.Length == 0)
            {
                return replace ? new T[0] : (current ?? new T[0]);
            }

            if (replace)
            {
                return incoming;
            }

            var result = new List<T>();
            if (current != null)
            {
                for (var i = 0; i < current.Length; i++)
                {
                    if (current[i] != null)
                    {
                        result.Add(current[i]);
                    }
                }
            }

            for (var i = 0; i < incoming.Length; i++)
            {
                var item = incoming[i];
                if (item == null)
                {
                    continue;
                }

                var id = idOf(item);
                var existingIndex = FindIndex(result, idOf, id);
                if (existingIndex >= 0)
                {
                    result[existingIndex] = item;
                }
                else
                {
                    result.Add(item);
                }
            }

            return result.ToArray();
        }

        private static int FindIndex<T>(List<T> items, Func<T, string> idOf, string id) where T : class
        {
            if (string.IsNullOrEmpty(id))
            {
                return -1;
            }

            for (var i = 0; i < items.Count; i++)
            {
                if (string.Equals(idOf(items[i]), id, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private static string[] MergeStrings(string[] current, string[] incoming, bool replace)
        {
            if (incoming == null || incoming.Length == 0)
            {
                return replace ? new string[0] : (current ?? new string[0]);
            }

            if (replace)
            {
                return incoming;
            }

            var result = new List<string>();
            AddUnique(result, current);
            AddUnique(result, incoming);
            return result.ToArray();
        }

        private static void AddUnique(List<string> result, string[] values)
        {
            if (values == null)
            {
                return;
            }

            for (var i = 0; i < values.Length; i++)
            {
                var value = values[i];
                if (string.IsNullOrEmpty(value) || result.Contains(value))
                {
                    continue;
                }

                result.Add(value);
            }
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
            {
                return;
            }

            var parts = assetFolder.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}

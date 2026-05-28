using System.IO;
using PaperShift.Data;
using UnityEditor;
using UnityEngine;

namespace PaperShift.Editor
{
    public static class PaperShiftDatabaseAssetCreator
    {
        private const string DataFolder = "Assets/PaperShift/Data";
        private const string DefaultDatabasePath = DataFolder + "/PaperShiftDefaultDatabase.asset";

        [MenuItem("Paper Shift/Create Default Logic Database")]
        public static void CreateDefaultDatabaseAsset()
        {
            if (!Directory.Exists(DataFolder))
            {
                Directory.CreateDirectory(DataFolder);
            }

            var database = PaperShiftSeedData.CreateDefaultDatabase();
            var existing = AssetDatabase.LoadAssetAtPath<PaperShiftDatabase>(DefaultDatabasePath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(database, existing);
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(database);
            }
            else
            {
                AssetDatabase.CreateAsset(database, DefaultDatabasePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<PaperShiftDatabase>(DefaultDatabasePath);
            Debug.Log("Paper Shift default logic database created: " + DefaultDatabasePath);
        }
    }
}

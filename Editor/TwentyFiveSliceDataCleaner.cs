using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TwentyFiveSlicer.Runtime;

namespace TwentyFiveSlicer.TFSEditor.Editor
{
    public class TwentyFiveSliceDataCleaner : EditorWindow
    {
        [MenuItem("Tools/Twenty Five Slicer Tools/Slice Data Cleaner")]
        public static void ShowWindow()
        {
            GetWindow<TwentyFiveSliceDataCleaner>("25-Slice Data Cleaner");
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Clean Up Missing Sprites", GUILayout.Height(30)))
            {
                CleanUpMissingSprites();
            }
        }

        private void CleanUpMissingSprites()
        {
            // Check if the SliceDataMap exists
            if (!SliceDataManager.Instance.IsSliceDataMapExist())
            {
                Debug.LogError(
                    "SliceDataMap not found. Please create and place it in the Resources folder. Its name should be 'SliceDataMap'.");
                return;
            }

            // Get all entries from SliceDataManager
            var allEntries = SliceDataManager.Instance.GetAllEntries();

            // Get all Sprite objects in the project
            string[] guids = AssetDatabase.FindAssets("t:Sprite");
            HashSet<string> existingSpritePaths = new HashSet<string>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                existingSpritePaths.Add(path);
            }

            // Collect sprites to remove
            List<Sprite> spritesToRemove = new List<Sprite>();
            foreach (var entry in allEntries)
            {
                string spritePath = AssetDatabase.GetAssetPath(entry.Key);
                if (!existingSpritePaths.Contains(spritePath)) // Check if the sprite exists in the project
                {
                    Debug.LogWarning($"Sprite not found: {spritePath}");
                    spritesToRemove.Add(entry.Key);
                }
            }

            // Remove missing sprites
            foreach (var sprite in spritesToRemove)
            {
                SliceDataManager.Instance.RemoveSliceData(sprite);
            }

            // Save changes
            Debug.Log($"Removed {spritesToRemove.Count} missing sprites from SliceDataMap.");
        }
    }
}
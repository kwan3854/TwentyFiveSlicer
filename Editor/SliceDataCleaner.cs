using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using TwentyFiveSlicer.Runtime;

namespace TwentyFiveSlicer.EditorTools
{
    public class SliceDataCleanerWindow : EditorWindow
    {
        private readonly List<string> _orphanedFiles = new();
        private Vector2 _scrollPosition;

        [MenuItem("Tools/Twenty Five Slicer Tools/Slice Data Cleaner")]
        public static void ShowWindow()
        {
            GetWindow<SliceDataCleanerWindow>("Slice Data Cleaner");
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Check for Orphaned SliceData"))
            {
                CheckForOrphanedSliceData();
            }

            if (_orphanedFiles.Count > 0)
            {
                EditorGUILayout.LabelField($"Found {_orphanedFiles.Count} orphaned slice data files:", EditorStyles.boldLabel);
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));

                foreach (string file in _orphanedFiles)
                {
                    EditorGUILayout.LabelField(file);
                }

                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("Clean Orphaned SliceData"))
                {
                    CleanOrphanedSliceData();
                }
            }
            else
            {
                EditorGUILayout.LabelField("No orphaned slice data files found.");
            }
        }

        private void CheckForOrphanedSliceData()
        {
            _orphanedFiles.Clear();
            string directoryPath = $"{Application.dataPath}/Resources/TwentyFiveSliceData";

            if (!Directory.Exists(directoryPath))
            {
                Debug.Log("No slice data directory found.");
                return;
            }

            string[] sliceDataFiles = Directory.GetFiles(directoryPath, "*.json");

            foreach (string filePath in sliceDataFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                bool fileIsOrphaned = true;

                foreach (var guid in AssetDatabase.FindAssets("t:Sprite"))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

                    if (sprite != null)
                    {
                        string spriteHash = TfsHashGenerator.GenerateUniqueSpriteHash(sprite);
                        if (spriteHash == fileName)
                        {
                            fileIsOrphaned = false;
                            break;
                        }
                    }
                }

                if (fileIsOrphaned)
                {
                    _orphanedFiles.Add(filePath);
                }
            }

            Repaint();
        }

        private void CleanOrphanedSliceData()
        {
            foreach (string filePath in _orphanedFiles)
            {
                File.Delete(filePath);
                Debug.Log($"Deleted orphaned slice data: {filePath}");
            }

            _orphanedFiles.Clear();
            Repaint();
        }
    }
}
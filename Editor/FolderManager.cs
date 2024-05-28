    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

namespace Revamp.AudioTools.FolderCreator
{
    public class FolderManager
    {

        public static string SO_AssetPath = "Packages/com.revamp.audiofolders/Editor/SampleFolderConfig.asset";
        public static string csvPath = "Packages/com.revamp.audiofolders/Docs/FolderCreate.csv";
        public static Dictionary<string, List<string>> FolderRelations = new Dictionary<string, List<string>>();
        public static Dictionary<string, string> ButtonTexts = new Dictionary<string, string>();

        public static void LoadAndUpdateFolders()
        {
            // folderKeyCount gets its value from LoadFolderStructureFromCSV out
            // compared against the number of folders in SampleFolderConfig. config
            int folderKeyCount; 
            List<SampleFolder> updatedFolders = LoadFolderStructureFromCSV(out folderKeyCount);
            SampleFolderConfig config = AssetDatabase.LoadAssetAtPath<SampleFolderConfig>(SO_AssetPath);
            
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<SampleFolderConfig>();
                config.folders = new List<SampleFolder>(updatedFolders);
                AssetDatabase.CreateAsset(config, SO_AssetPath);
            }
            else
            {
                MergeFolders(config.folders, updatedFolders);
            }

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (folderKeyCount != config.folders.Count)
            {
                Debug.Log($"<color=orange>Config Folder Element # {config.folders.Count}, Expected Key # {folderKeyCount}</color>");
                // Debug.LogError("Run Excel macro / ID to Folder count.");
            }
        }

        // Element to ID && clip to localization key
        private static void MergeFolders(List<SampleFolder> existingFolders, List<SampleFolder> updatedFolders)
        {
            // ID for primary reference and folder structure
            var folderDict = new Dictionary<int, SampleFolder>();
            // Additional dictionary to maintain clip persistence based on localizationKey
            var clipsDict = existingFolders.ToDictionary(folder => folder.localizationKey, folder => folder.clips);

            foreach (var folder in existingFolders)
            {
                folderDict[folder.id] = folder;
            }

            foreach (var updatedFolder in updatedFolders)
            {
                if (folderDict.TryGetValue(updatedFolder.id, out var existingFolder))
                {
                    if (!AreFoldersEqual(existingFolder, updatedFolder))
                    {
                        Debug.Log($"Updating folder {updatedFolder.id} due to changes detected.");
                        existingFolder.parentId = updatedFolder.parentId;
                        existingFolder.folderName = updatedFolder.folderName;
                        existingFolder.localizationKey = updatedFolder.localizationKey;
                        existingFolder.hasChildren = updatedFolder.hasChildren;
                        // Restore clips from clipsDict if available, otherwise use updated folder's clips
                        existingFolder.clips = clipsDict.TryGetValue(updatedFolder.localizationKey, out var clips) ? clips : updatedFolder.clips;
                    }
                }
                else
                {
                    // If new folder, add it and use clips from updated folder if available in clipsDict
                    updatedFolder.clips = clipsDict.TryGetValue(updatedFolder.localizationKey, out var clips) ? clips : updatedFolder.clips;
                    folderDict.Add(updatedFolder.id, updatedFolder);
                    Debug.Log($"Added new folder {updatedFolder.id}.");
                }
            }

            existingFolders.Clear();
            existingFolders.AddRange(folderDict.Values);
        }


        public static List<SampleFolder> LoadFolderStructureFromCSV(out int folderKeyCount)
        {
            List<SampleFolder> folders = new List<SampleFolder>();
            folderKeyCount = 0;
            if (!File.Exists(csvPath))
            {
                Debug.LogError("File not found: " + csvPath);
                return folders;
            }

            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length < 1) return folders;

            FolderRelations.Clear();
            Dictionary<string, int> columnMap = new Dictionary<string, int>();
            string[] headers = lines[0].Split(',');
            for (int index = 0; index < headers.Length; index++)
            {
                columnMap[headers[index].Trim()] = index;
            }

            // Ensure 'Key' and 'English(en)' indices are correct
            if (!columnMap.TryGetValue("Key", out int keyIndex) || !columnMap.TryGetValue("English(en)", out int englishIndex))
            {
                Debug.LogError("CSV headers are incorrect or missing 'Key' or 'English(en)' fields");
                return folders;
            }

            // Ensure fresh load
            ButtonTexts.Clear();

            for (int i = 1; i < lines.Length; i++)
            {
                string[] tokens = lines[i].Split(',');
                if (tokens.Length <= keyIndex || tokens.Length <= englishIndex)
                    continue;  // Skip if key or English text is missing

                string key = tokens[keyIndex].Trim();
                string value = tokens[englishIndex].Trim();

                //Debug.Log($"Processing line {i}: Key={key}, Value={value}");

                if (key.StartsWith("BTN_"))
                {
                    ButtonTexts[key] = value;  // Use indexing to handle updating existing keys
                    //Debug.Log($"Added to ButtonTexts: {key} -> {value}");
                }

                // Populate FolderRelations
                if (key.StartsWith("FOLDER_"))
                {
                    folderKeyCount++;

                    string id = tokens[columnMap["Id"]].Trim();
                    string parentId = columnMap.ContainsKey("Parent ID") && tokens.Length > columnMap["Parent ID"] ? tokens[columnMap["Parent ID"]].Trim() : "";
                    string folderName = tokens[englishIndex].Trim();

                    if (!FolderRelations.ContainsKey(parentId))
                    {
                        FolderRelations[parentId] = new List<string>();
                    }
                    FolderRelations[parentId].Add(folderName);

                    folders.Add(new SampleFolder
                    {
                        id = int.Parse(id),
                        parentId = parentId,
                        folderName = folderName,
                        localizationKey = key,
                        clips = new List<AudioClip>(),
                        hasChildren = columnMap.ContainsKey("Has Child") && tokens.Length > columnMap["Has Child"] && bool.TryParse(tokens[columnMap["Has Child"]].Trim(), out bool hasChildren) && hasChildren
                    });
                }
            }

            // Debug print of the complete dictionary
            /* foreach (var entry in FolderRelations)
            {
            Debug.Log($"Parent: {entry.Key}, Children: {string.Join(", ", entry.Value)}");
            } */
            return folders;
        }


        private static bool AreFoldersEqual(SampleFolder a, SampleFolder b)
        {
            return a.id == b.id &&
                a.parentId == b.parentId &&
                a.folderName == b.folderName &&
                a.localizationKey == b.localizationKey &&
                a.hasChildren == b.hasChildren;
            // Note: Clips are not included in the comparison as they might be manually modified
        }

        // LOGS
        public static void LogButtonTexts()
        {
            if (ButtonTexts.Count == 0)
            {
                Debug.Log("ButtonTexts dictionary is empty.");
                return;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("Loaded Button Texts:");
            foreach (var pair in ButtonTexts)
            {
                sb.AppendLine($"{pair.Key}: {pair.Value}");
            }
            Debug.Log(sb.ToString());
        }

    }
}
    using UnityEditor;
    using UnityEngine;
    using System.IO;
    using System.Text;
    using System.Linq;
    using UnityEngine.UIElements;
    using System.Collections.Generic;

namespace Revamp.AudioTools.FolderCreator
{
    [CustomEditor(typeof(SampleFolderConfig))]
    public class SampleFolderConfigEditor : Editor
    {
        private string clipsPath;
        void OnEnable()
            {
                clipsPath = AudioFolderEditorWindow.baseClipFolder;
            }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI(); // Draw the default inspector

            SampleFolderConfig config = (SampleFolderConfig)target;


                    // UPDATE FOLDERS
            if (GUILayout.Button("Load and Update Folders"))
            {
                    //FolderManager.LoadFolderStructureFromCSV();  
                FolderManager.LoadAndUpdateFolders();
            }

            // Dictionary Folder Map
            if (GUILayout.Button("Build Folder Map"))
            {
                config.BuildDictionary();
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                Debug.Log("Folder Map built.");

                // Dictionary Printer
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (var pair in  config.clipToFolderMap)
                {
                    sb.AppendLine($"<color=yellow>{pair.Key}: {pair.Value}</color>");
                }
                Debug.Log(sb.ToString());
            }

        // Unassigned Clips Button
            if (GUILayout.Button("Unassigned Clips"))
            {
                FindUnassignedClips(config);
            }

        void FindUnassignedClips(SampleFolderConfig config)
        {
            string[] allClipFiles = Directory.GetFiles(clipsPath, "*.*", SearchOption.AllDirectories)
                                            .Where(file => file.EndsWith(".mp3") || file.EndsWith(".wav"))
                                            .Select(file => Path.GetFileNameWithoutExtension(file))
                                            .ToArray();

            HashSet<string> assignedClips = new HashSet<string>(config.folders.SelectMany(folder => folder.clips).Select(clip => clip.name));

            List<string> unassignedClips = allClipFiles.Where(clip => !assignedClips.Contains(clip)).ToList();

            if (unassignedClips.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Unassigned Clips:");
                foreach (var clip in unassignedClips)
                {
                    sb.AppendLine(clip);
                }
                Debug.Log(sb.ToString());
            }
            else
            {
                Debug.Log("All clips are assigned.");
            }
        }


        // TOGGLE TEST START
        if (GUILayout.Button("Select Stone Fall and Set Parent Toggle"))
        {
            SelectStoneFallAndSetParentToggle();
        }

        void SelectStoneFallAndSetParentToggle()
        {
            var audioFolderWindow = GetAudioFolderEditorWindow();
            if (audioFolderWindow == null)
            {
                Debug.LogWarning("<color=yellow>AudioFolderEditorWindow not found.</color>");
                return;
            }

            // Select the Stone Fall toggle
            string folderName = "Stone Fall";
            var toggle = FindSampleToggle(audioFolderWindow, folderName);

            if (toggle != null)
            {
                // Set the toggle to true
                toggle.value = true;
                Debug.Log($"<color=yellow>Selected folder: {folderName}</color>");

                // Look up the parent folder
                string parentFolderName = GetParentFolderName(folderName);

                if (!string.IsNullOrEmpty(parentFolderName))
                {
                    Debug.Log($"<color=yellow>Parent folder of {folderName} is {parentFolderName}</color>");

                    // Select the parent toggle
                    var parentToggle = FindSampleToggle(audioFolderWindow, parentFolderName);
                    if (parentToggle != null)
                    {
                        parentToggle.value = true;
                        Debug.Log($"<color=yellow>Parent toggle for {parentFolderName} set to true due to child {folderName} activation.</color>");
                    }
                    else
                    {
                        Debug.LogWarning($"<color=yellow>Parent toggle for {parentFolderName} not found.</color>");
                    }
                }
                else
                {
                    Debug.LogWarning($"<color=yellow>No parent found for {folderName}</color>");
                }
            }
            else
            {
                Debug.LogWarning($"<color=yellow>Toggle for {folderName} not found.</color>");
            }
        }

        Toggle FindSampleToggle(AudioFolderEditorWindow audioFolderWindow, string folderName)
        {
            var samplesContainer = audioFolderWindow.rootVisualElement.Q<VisualElement>("samples-contain");
            if (samplesContainer == null)
            {
                Debug.LogError("<color=yellow>samples-contain not found in the visual tree.</color>");
                return null;
            }
            return samplesContainer.Query<Toggle>(folderName.Replace(" ", "") + "Toggle").First();
        }

        string GetParentFolderName(string childFolderName)
        {
            foreach (var entry in FolderManager.FolderRelations)
            {
                if (entry.Value.Contains(childFolderName))
                {
                    return entry.Key;
                }
            }
            return null;
        }

        AudioFolderEditorWindow GetAudioFolderEditorWindow()
        {
            var windows = Resources.FindObjectsOfTypeAll<AudioFolderEditorWindow>();
            return windows.FirstOrDefault();
        }
        // TOGGLE TEST END



                    // Dictionary Folder Relations
            if (GUILayout.Button("Print Folder Relations"))
            {
                int folderKeyCount;
                FolderManager.LoadFolderStructureFromCSV(out folderKeyCount); // Ensure populated
                PrintFolderRelations();
            }

        void PrintFolderRelations()
            {
                if (FolderManager.FolderRelations.Count == 0)
                {
                    Debug.Log("FolderRelations dictionary is empty.");
                    return;
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Folder Relations:");
                foreach (var pair in FolderManager.FolderRelations)
                {
                    sb.AppendLine($"{pair.Key}: {string.Join(", ", pair.Value)}");
                }
                Debug.Log(sb.ToString());
            }
            
        // COUNT ASSIGNED CLIPS
        if (GUILayout.Button("Count Audio Clips"))
            {
                CountAudioClips(config);
            }

        void CountAudioClips(SampleFolderConfig config)
        {
            int totalClips = config.folders.Sum(folder => folder.clips.Count);
            Debug.Log($"<color=yellow>Total audio clips assigned: {totalClips}</color>");
        }
            




            if (GUILayout.Button("Save Changes"))
            {
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
            }

            // REMOVE ELEMENT
            for (int i = 0; i < config.folders.Count; i++)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Folder Name: " + config.folders[i].folderName + " ID: " + config.folders[i].id, EditorStyles.boldLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(80)))
                {
                    config.folders.RemoveAt(i);
                    EditorUtility.SetDirty(target);
                    AssetDatabase.SaveAssets();
                    break; // Break to prevent modification during iteration
                }
                GUILayout.EndHorizontal();
            }
        


        // FORCED MOVE
            if (GUILayout.Button("FORCED CLIP MOVE"))
            {
                MoveAllWavFiles();
            }
        

        }

        private void MoveAllWavFiles()
        {
            string sourceFolder = "Assets/Editor/Development/Audio";
            string targetFolder = "Assets/Editor/Development/Audio/Audio Folder Creator/Clips";

            // Ensure the target folder exists
            if (!AssetDatabase.IsValidFolder(targetFolder))
            {
                AssetDatabase.CreateFolder("Assets/Editor/Development/Audio/Audio Folder Creator", "Clips");
            }

            // Get all .wav files in the source folder
            string[] wavFiles = Directory.GetFiles(sourceFolder, "*.mp3", SearchOption.AllDirectories);
            foreach (string file in wavFiles)
            {
                string fileName = Path.GetFileName(file);
                string sourcePath = file.Replace("\\", "/");
                string targetPath = $"{targetFolder}/{fileName}";

                // Move the asset and log the result
                string result = AssetDatabase.MoveAsset(sourcePath, targetPath);
                if (!string.IsNullOrEmpty(result))
                {
                    Debug.LogError($"Failed to move asset {fileName} from {sourcePath} to {targetPath}: {result}");
                }
                else
                {
                    Debug.Log($"<color=teal>Moved {fileName} to {targetPath} successfully.</color>");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
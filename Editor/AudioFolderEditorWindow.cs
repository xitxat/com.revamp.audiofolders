    using UnityEngine;
    using UnityEditor;
    using UnityEngine.UIElements;
    using UnityEditor.UIElements;
    using System.IO;
    using System.Linq;
    using System.Collections.Generic;

namespace Revamp.AudioTools.FolderCreator
{
    #region *** EDITOR WINDOW   ***
    public class AudioFolderEditorWindow : EditorWindow
    {
        private SampleFolderConfig sampleFolderConfig;
        private CreditManager creditManager;
        public static string baseClipFolder = "Packages/com.revamp.audiofolders/AudioClips";
        private static string pdfPath =  "Packages/com.revamp.audiofolders/Docs/The License.pdf";  
        private bool isChildTriggered = false;
        private bool isUserAction = true;
        private static string assetPath = FolderManager.SO_AssetPath;
        private static string visualTreePath = "Packages/com.revamp.audiofolders/Docs/AudioFolderCreator.uxml";
        private static VisualTreeAsset visualTree;


        private void OnEnable()
        {
            AssetDatabase.Refresh();
            visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(visualTreePath);
            if (visualTree == null)
            {
                Debug.LogError("Failed to load VisualTreeAsset from: " + visualTreePath);
            }
        }

        [MenuItem("Tools/Revamp/Audio Folders")]
        public static void ShowWindow()
        {
            var window = GetWindow<AudioFolderEditorWindow>();
            window.titleContent = new GUIContent("Create Audio Folders");
            window.CreateDefaultGUI();
            window.LoadAssets();

        }

        void LoadAssets()
        {
            creditManager = new CreditManager();

            // ScriptableObject  audio folder configuration
            sampleFolderConfig = AssetDatabase.LoadAssetAtPath<SampleFolderConfig>(assetPath);

            SetupSamplesGUI();  
            CollapseAllFoldouts();

            int folderKeyCount;
            FolderManager.LoadFolderStructureFromCSV(out folderKeyCount);
            UpdateUIButtons("samples-contain");
        }

        public void CreateDefaultGUI()
        {
            // UXML
            if (visualTree != null)
            {
                visualTree.CloneTree(rootVisualElement);
            }
            else
            {
                Debug.LogError("VisualTreeAsset is not loaded.");
                return;
            }

            // Update texts immediately after cloning the tree
            creditManager?.UpdateCreditTexts(rootVisualElement);

            SetupButtonsAndEvents();
        }

        private void SetupButtonsAndEvents()
        {
            // Samples
            Button selectAllSamplesButton = rootVisualElement.Q<Button>("BTN_SELECT-ALL-SAMPLES");
            Button selectNoneSamplesButton = rootVisualElement.Q<Button>("BTN_SELECT-NONE-SAMPLES");
            Button creditButton = rootVisualElement.Q<Button>("BTN_CREDITS");
            Button licenseButton = rootVisualElement.Q<Button>("BTN_LICENSE");
            Button viewFoldersButton = rootVisualElement.Q<Button>("BTN_VIEW-FOLDERS");
            Button createSamplesButton = rootVisualElement.Q<Button>("BTN_CREATE-SAMPLES");
            // Credits
            Button urlSonnissButton = rootVisualElement.Q<Button>("btn_url_sonniss");
            Button urlDiscordButton = rootVisualElement.Q<Button>("btn_url_discord");
            Button creditCloseButton = rootVisualElement.Q<Button>("btn_close-credits");
            //Safety
            Toggle enableCreateToggle = rootVisualElement.Q<Toggle>("toggle-create");
            enableCreateToggle.value = false;
            // Btn Create Bdr
            UpdateButtonBorder(enableCreateToggle.value, createSamplesButton);
            // text fade
            UpdateTextFadeClass(enableCreateToggle.value, createSamplesButton);
            // Register event handler for the toggle
            enableCreateToggle.RegisterValueChangedCallback(evt => UpdateButtonBorder(evt.newValue, createSamplesButton));
            enableCreateToggle.RegisterValueChangedCallback(evt => UpdateTextFadeClass(evt.newValue, createSamplesButton));

            // Events
            creditButton.clickable.clicked += () => ToggleVisibility("credits-contain");        
            creditCloseButton.clickable.clicked += () => ToggleVisibility("samples-contain");
            selectAllSamplesButton.clickable.clicked += () => SetAllSamplesToggles(true);
            selectNoneSamplesButton.clickable.clicked += () => SetAllSamplesToggles(false);
            createSamplesButton.clickable.clicked += () => 
                { 
                    if (enableCreateToggle.value)
                    {
                        CreateSelectedSamplesFolders(); 
                    }
                    else
                    {
                        Debug.LogWarning("<color=yellow>Create action is disabled.</color>");
                    }
                };

            licenseButton.clickable.clicked += OpenLicensePDF;
            urlSonnissButton.clickable.clicked += () => Application.OpenURL("https://sonniss.com/gameaudiogdc");
            urlDiscordButton.clickable.clicked += () => Application.OpenURL("https://discord.com");

            SetupToggleRelationships();
            EnsureBaseFolderExists();
        }

        // CSS
        private void UpdateButtonBorder(bool isSecure, Button createButton)
        {
            if (isSecure)
            {
                createButton.RemoveFromClassList("btn-brdr-red");
                createButton.AddToClassList("btn-brdr-green");
            }
            else
            {
                createButton.RemoveFromClassList("btn-brdr-green");
                createButton.AddToClassList("btn-brdr-red");
            }
        }

    private void UpdateTextFadeClass(bool isEnabled, Button createButton)
{
    if (isEnabled)
    {
        createButton.RemoveFromClassList("btn-txt-fade");
    }
    else
    {
        createButton.AddToClassList("btn-txt-fade");
    }
}

        private void CollapseAllFoldouts()
        {
            var samplesContainer = rootVisualElement.Q<VisualElement>("samples-contain");
            if (samplesContainer != null)
            {
                var foldouts = samplesContainer.Query<Foldout>().ToList();
                foreach (var foldout in foldouts)
                {
                    foldout.value = false; // Collapse the foldout
                }
            }
        }

        public void UpdateUIButtons(string containerName)
        {
            var container = rootVisualElement.Q<VisualElement>(containerName);
            if (container == null)
            {
                Debug.LogError($"{containerName} not found");
                return;
            }

            UpdateUITexts(container, FolderManager.ButtonTexts);
        }

        public void UpdateUITexts(VisualElement container, Dictionary<string, string> uiTexts)
        {
            var buttons = container.Query<Button>().ToList();
            foreach (var button in buttons)
            {
                if (uiTexts.TryGetValue(button.name, out var newText))
                {
                    button.text = newText;
                    //Debug.Log($"Updated {button.name} to '{newText}'");  // Confirm each update
                }
                else
                {
                    Debug.LogWarning($"No text found for button {button.name} in dictionary"); 
                }
            }
        }

        #endregion

        #region *** SAMPLE FOLDERS  ***

        //  nested, clean
        private void SetupSamplesGUI() 
        {
            var foldersSamplesContainer = rootVisualElement.Q<VisualElement>("folders-samples");

            foldersSamplesContainer.Clear();
            var scrollView = new ScrollView();
            foldersSamplesContainer.Add(scrollView);

            Dictionary<string, Foldout> parentFoldouts = new Dictionary<string, Foldout>();

            // Track created foldouts to avoid duplicates
            HashSet<string> createdFoldouts = new HashSet<string>();

            // Initialize foldouts for all folders that need to act as a parent
            foreach (var folder in sampleFolderConfig.folders) {
                if (folder.hasChildren && !createdFoldouts.Contains(folder.folderName)) {
                    var foldout = new Foldout { text = folder.folderName };
                    scrollView.Add(foldout);
                    createdFoldouts.Add(folder.folderName);
                    parentFoldouts[folder.folderName] = foldout;
                    //Debug.Log($"Created parent foldout for: {folder.folderName}");
                }
            }

            // Add all folders to their respective parent foldout / scrollView
            foreach (var folder in sampleFolderConfig.folders) {
                if (!string.IsNullOrEmpty(folder.parentId) && parentFoldouts.TryGetValue(folder.parentId, out var parentFoldout)) {
                    CreateFolderUI(folder, parentFoldout);  // Add child folder to its parent foldout
                } else if (!folder.hasChildren && string.IsNullOrEmpty(folder.parentId)) {
                    CreateFolderUI(folder, scrollView);  // Add root folder with no children directly to scrollView
                }
            }
        }
    
        //  Attach Click Event Handlers to Parent Toggles
        private void CreateFolderUI(SampleFolder folder, VisualElement parentElement, int depth = 0)   
        {
            var horizontalContainer = new VisualElement
            {
                name = $"{folder.folderName.Replace(" ", "")}Container",
                style =
                {
                    flexDirection = FlexDirection.Column,
                    alignItems = Align.FlexStart,
                    marginLeft = depth * 5 
                }
            };

            var folderToggle = new Toggle
            {
                text = folder.folderName,
                name = $"{folder.folderName.Replace(" ", "")}Toggle"
            };
            folderToggle.AddToClassList("foldout-VE-parent"); //  styling

            // Attach click event handler to the parent toggle
            folderToggle.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (evt.button == (int)MouseButton.LeftMouse)
                {
                    isUserAction = true; 
                    UpdateRelatedToggles(folder.folderName, folderToggle.value);
                    isUserAction = false;
                }
            });

            // Create a foldout to act only as a container for child folders and clips
            var folderFoldout = new Foldout
            {
                name = $"{folder.folderName.Replace(" ", "")}Foldout"
            };

            // Register callback to control the visibility of the foldout and propagate toggle changes
            folderToggle.RegisterValueChangedCallback(evt =>
            {
                folderFoldout.value = evt.newValue;
                if (!isUserAction) // Only propagate to avoid loops
                {
                    UpdateRelatedToggles(folder.folderName, evt.newValue);
                }
            });

            horizontalContainer.Add(folderToggle);
            horizontalContainer.Add(folderFoldout);
            parentElement.Add(horizontalContainer);

            SetupClipsUI(folder, folderFoldout, folderToggle);

            // Recursively add child folders to the foldout, ensuring they are not added at the parent level
            var childFolders = sampleFolderConfig.folders.Where(f => f.parentId == folder.folderName).ToList();
            foreach (var childFolder in childFolders)
            {
                CreateFolderUI(childFolder, folderFoldout, depth + 1); // Ensure children are added to the foldout
            }
        }

        private void SetupClipsUI(SampleFolder folder, Foldout foldout, Toggle parentFolderToggle) 
        {
            foreach (var clip in folder.clips) {
                AddClipUI(clip, foldout, parentFolderToggle);
            }
        }

        // User Action Flag
        private void AddClipUI(AudioClip clip, Foldout foldout, Toggle parentFolderToggle)
        {
            var horizontalContainer = new VisualElement()
            {
                style = { flexDirection = FlexDirection.Row }
            };

            var clipToggle = new Toggle()
            {
                style = { flexGrow = 0 }
            };

            var objectField = new ObjectField
            {
                objectType = typeof(AudioClip),
                value = clip,
                style = { flexGrow = 1 }
            };
            objectField.AddToClassList("object-field-style");

            clipToggle.RegisterValueChangedCallback(evt =>
            {
                objectField.SetEnabled(evt.newValue);
                if (evt.newValue)
                {
                    isUserAction = false;
                    parentFolderToggle.value = true;  // Automatically turn on the parent folder's toggle
                    isUserAction = true;
                //  Debug.Log($"Clip toggle for {clip.name} set to {evt.newValue}. Triggering parent toggle for {parentFolderToggle.name.Replace("Toggle", "")}");
                    UpdateParentToggleState(parentFolderToggle.name.Replace("Toggle", "").Replace(" ", ""), true);
                }
                else
                {
                    UpdateParentToggleState(parentFolderToggle.name.Replace("Toggle", "").Replace(" ", ""), false);
                }
            });

            horizontalContainer.Add(clipToggle);
            horizontalContainer.Add(objectField);
            foldout.Add(horizontalContainer);
        }

        private string GetParentFolderName(string childFolderName)
        {
            //    Debug.Log($"<color=yellow>GetParentFolderName called for child: {childFolderName}</color>");
            foreach (var entry in FolderManager.FolderRelations)
            {
                if (entry.Value.Contains(childFolderName))
                {
                    //        Debug.Log($"<color=yellow>Identified relationship - Child: {childFolderName}, Parent: {entry.Key}</color>");
                    return entry.Key;
                }
            }
        // Debug.LogWarning($"<color=yellow>No parent found for child: {childFolderName}</color>");
            return null;
        }

        private Dictionary<string, List<string>> BuildFolderHierarchy(List<string> selectedFolders)
        {
            var hierarchy = new Dictionary<string, List<string>>();

            foreach (var selectedFolder in selectedFolders)
            {
                BuildHierarchyRecursively(selectedFolder, hierarchy, selectedFolders);
            }

            return hierarchy;
        }

        private void BuildHierarchyRecursively(string parentFolder, Dictionary<string, List<string>> hierarchy, List<string> selectedFolders)
        {
            if (!FolderManager.FolderRelations.ContainsKey(parentFolder))
            {
                return;
            }

            if (!hierarchy.ContainsKey(parentFolder))
            {
                hierarchy[parentFolder] = new List<string>();
            }

            foreach (var childFolder in FolderManager.FolderRelations[parentFolder])
            {
                if (selectedFolders.Contains(childFolder))
                {
                    hierarchy[parentFolder].Add(childFolder);
                    BuildHierarchyRecursively(childFolder, hierarchy, selectedFolders);
                }
            }
        }

        // TOGGLES
        //  Attach Click Event Handlers to Parent Toggles
        public void UpdateRelatedToggles(string folderName, bool isChecked)
        {
            //    Debug.Log($"Updating toggles related to: {folderName}, Set Checked: {isChecked}");

            var parentElement = rootVisualElement.Q<VisualElement>("samples-contain");
            var folderElement = parentElement?.Q<VisualElement>($"{folderName.Replace(" ", "")}Foldout");

            // Only propagate to child toggles  to avoid loops
            if (folderElement != null && !isUserAction)
            {
                var childToggles = folderElement.Query<Toggle>().ToList();
                foreach (var toggle in childToggles)
                {
                    toggle.value = isChecked;
                //   Debug.Log($"Updated {toggle.name} to {isChecked}");
                }
            }

            // Recursively update all nested child folders and their clips
            var childFolders = sampleFolderConfig.folders.Where(f => f.parentId == folderName).ToList();
            foreach (var childFolder in childFolders)
            {
                UpdateRelatedToggles(childFolder.folderName, isChecked);
            }

            // Prevent sibling selection
            if (!isChildTriggered)
            {
                // Update parent toggles using FolderRelations
                UpdateParentToggleState(folderName, isChecked);
            }
        }

        private void UpdateParentToggles(string childFolderName, bool isChecked)
        {
            foreach (var entry in FolderManager.FolderRelations)
            {
                if (entry.Value.Contains(childFolderName))
                {
                    var parentFolderName = entry.Key;
                    //  Debug.Log($"Child: {childFolderName}, Parent: {parentFolderName}");

                    if (!string.IsNullOrEmpty(parentFolderName))
                    {
                        var parentToggle = rootVisualElement.Q<Toggle>($"{parentFolderName.Replace(" ", "")}Toggle");
                        if (parentToggle != null)
                        {
                            //  Debug.Log($"Found parent toggle for {parentFolderName}. Current value: {parentToggle.value}, Setting to: {isChecked}");
                            if (parentToggle.value != isChecked)
                            {
                                parentToggle.value = isChecked; // Set parent toggle to the same value as the current toggle
                                //  Debug.Log($"Parent toggle for {parentFolderName} set to {isChecked} due to child {childFolderName} activation.");
                                UpdateParentToggles(parentFolderName, isChecked); // Recursively update further up the hierarchy
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Parent toggle for {parentFolderName} not found.");
                        }
                    }
                    break;
                }
            }
        }

        //  User Action Flag
        private void UpdateParentToggleState(string childFolderName, bool isChecked)
        {
            //Debug.Log($"<color=yellow>UpdateParentToggleState called for child: {childFolderName}, isChecked: {isChecked}</color>");

            var parentFolderName = GetParentFolderName(childFolderName);

            if (!string.IsNullOrEmpty(parentFolderName))
            {
            // Debug.Log($"<color=yellow>Identified relationship - Child: {childFolderName}, Parent: {parentFolderName}</color>");

                var parentElement = rootVisualElement.Q<VisualElement>("samples-contain");
                var parentToggle = parentElement?.Q<Toggle>($"{parentFolderName.Replace(" ", "")}Toggle");
                if (parentToggle != null)
                {
                    //Debug.Log($"<color=yellow>Found parent toggle for {parentFolderName}. Current value: {parentToggle.value}, Setting to: {isChecked}</color>");
                    if (parentToggle.value != isChecked)
                    {
                        isChildTriggered = true;
                        isUserAction = false;
                        parentToggle.value = isChecked; // Set parent toggle to the same value as the current toggle
                        isUserAction = true;
                        isChildTriggered = false;
                        //Debug.Log($"<color=yellow>Parent toggle for {parentFolderName} set to {isChecked} due to child {childFolderName} activation.</color>");

                        // Update GUI after setting parent toggle
                        parentToggle.MarkDirtyRepaint();

                        // Recursively update up the hierarchy
                        UpdateParentToggleState(parentFolderName, isChecked);
                    }
                }
                else
                {
                Debug.LogWarning($"<color=yellow>Parent toggle for {parentFolderName} not found.</color>");
                }
            }
            else
            {
        //        Debug.LogWarning($"<color=yellow>Parent folder name is empty for child: {childFolderName}</color>");
            }
        }

        private void ToggleVisibility(string activeContainerName)
        {
            var samplesContainer = rootVisualElement.Q<VisualElement>("samples-contain");
            var creditsContainer = rootVisualElement.Q<VisualElement>("credits-contain");

            if (samplesContainer == null || creditsContainer == null)
            {
                Debug.LogError("One or more containers were not found in the visual tree.");
                return;
            }

            if (activeContainerName == "credits-contain")
            {
                // Update texts right after making it visible
                creditManager.UpdateCreditTexts(rootVisualElement);
            }

            // Hide all containers initially
            //foldersContainer.style.display = DisplayStyle.None;
            samplesContainer.style.display = DisplayStyle.None;
            creditsContainer.style.display = DisplayStyle.None;

            // Only show the active container
            switch (activeContainerName)
            {
            /*  case "folders-contain":
                    foldersContainer.style.display = DisplayStyle.Flex;
                    break; */
                case "samples-contain":
                    samplesContainer.style.display = DisplayStyle.Flex;
                    break;
                case "credits-contain":
                    creditsContainer.style.display = DisplayStyle.Flex;
                    break;
                default:
                    Debug.LogError("Invalid container name provided: " + activeContainerName);
                    break;
            }
        }

        private void SetupToggleRelationships()
        {
            // Directly query all parent toggles marked with a specific class
            var parentToggles = rootVisualElement.Query<Toggle>().Where(toggle => toggle.ClassListContains("foldout-VE-parent")).ToList();

            foreach (var parentToggle in parentToggles)
            {
                var foldout = parentToggle.Q<Foldout>();
                var childrenContainer = foldout?.Query<VisualElement>().Where(ve => ve.ClassListContains("foldout-VE-children")).First();

                if (childrenContainer != null)
                {
                    foreach (var childToggle in childrenContainer.Query<Toggle>().Build().ToList())
                    {
                        childToggle.RegisterValueChangedCallback(evt =>
                        {
                            if (evt.newValue)
                            {
                                // Directly affect the grandparent Toggle's value
                                parentToggle.value = true;
                            }
                        });
                    }
                }
            }
        }

        private void SetAllSamplesToggles(bool state)
        {
            var samplesContainer = rootVisualElement.Q<VisualElement>("samples-contain");
            if (samplesContainer == null)
            {
                Debug.LogError("<color=yellow>samples-contain not found in the visual tree.</color>");
                return;
            }

            // Query and set 
            var toggles = samplesContainer.Query<Toggle>().ToList();
            foreach (var toggle in toggles)
            {
                toggle.value = state;
            }
        }

    #endregion

    #region *** CREATE *** 

        private void EnsureBaseFolderExists()
        {
            string basePath = Path.Combine(Application.dataPath, "Audio");

            string assetPath = "Assets/Audio";

            // Check if the folder exists
            if (!AssetDatabase.IsValidFolder(assetPath))
            {
                AssetDatabase.CreateFolder("Assets", "Audio");
                Debug.Log("<color=yellow>Base 'Audio' folder created in Assets.</color>");
            }
            else
            {
            //    Debug.Log("Base 'Audio' folder already exists.");
            }

            AssetDatabase.Refresh();
        }

        private string GetAudioFilePath(string baseClipFolder, string clipName)
    {
        string mp3Path = $"{baseClipFolder}/{clipName}.mp3";
        string wavPath = $"{baseClipFolder}/{clipName}.wav";

        if (System.IO.File.Exists(mp3Path))
        {
            return mp3Path;
        }
        else if (System.IO.File.Exists(wavPath))
        {
            return wavPath;
        }
        else
        {
            Debug.LogError($"No audio file found for {clipName} in {baseClipFolder}");
            return null;
        }
    }

        private void CreateSelectedSamplesFolders()
        {
            EnsureBaseFolderExists();

            var selectedFolders = GetSelectedFolders();

            var hierarchy = BuildFolderHierarchy(selectedFolders);

            CreateFoldersFromHierarchy(hierarchy);

            MoveClipsToFolders(hierarchy);

            AssetDatabase.Refresh();

            //  Debug.Log("Selected sample folders and their nested structure with clips have been created successfully.");
        }

        private List<string> GetSelectedFolders()
        {
            var selectedFolders = new List<string>();

            var samplesContainer = rootVisualElement.Q<VisualElement>("samples-contain");
            if (samplesContainer == null)
            {
                Debug.LogError("samples-contain not found in the visual tree.");
                return selectedFolders;
            }

            var sampleToggles = samplesContainer.Query<Toggle>().Where(toggle => toggle.value).ToList();
            foreach (var toggle in sampleToggles)
            {
                if (toggle != null && !string.IsNullOrEmpty(toggle.text))
                {
                    selectedFolders.Add(toggle.text.Trim());
                }
            }

            //  Debug.Log($"<color=yellow>Selected Folders: {string.Join(", ", selectedFolders)}</color>");
            return selectedFolders;
        }

        private void CreateFoldersFromHierarchy(Dictionary<string, List<string>> hierarchy)
        {
            string basePath = Path.Combine(Application.dataPath, "Audio");

            foreach (var parent in hierarchy)
            {
                // Parent folder first
                var parentPath = Path.Combine(basePath, parent.Key);
                Directory.CreateDirectory(parentPath);

                // Then nest each child folder
                foreach (var child in parent.Value)
                {
                    var childPath = Path.Combine(parentPath, child);
                    Directory.CreateDirectory(childPath);
                }
            }
            AssetDatabase.Refresh();
        }

        private void MoveClipsToFolders(Dictionary<string, List<string>> hierarchy)
        {
            sampleFolderConfig.BuildDictionary();

            if (hierarchy == null)
            {
                Debug.LogError("Hierarchy is null.");
                return;
            }

            if (sampleFolderConfig.clipToFolderMap.Count == 0)
            {
            // Debug.LogError("<color=teal>clipToFolderMap is empty. Check if BuildDictionary() is properly populating the map.</color>");
            }
            else
            {
                foreach (var item in sampleFolderConfig.clipToFolderMap)
                {
                //   Debug.Log($"<color=teal>Dictionary Entry: {item.Key} maps to {item.Value}</color>");
                }
            }

            foreach (var entry in hierarchy)
            {
                string folderName = entry.Key;
                List<string> childFolders = entry.Value;

                //  Debug.Log($"<color=teal>Processing folder: {folderName} with {childFolders.Count} children</color>");

                if (childFolders == null)
                {
                    Debug.LogError($"No child folders found for {folderName}.");
                    continue;
                }

                // Check for clips in parent folders
                if (sampleFolderConfig.clipToFolderMap.TryGetValue(folderName, out string clipName))
                {
                    //  Debug.Log($"<color=teal>Found clip mapping for {folderName}: {clipName}. Attempting to move...</color>");
                    CopyClip(folderName, clipName);
                }
                else
                {
                //  Debug.LogWarning($"No clip mapping found for folder {folderName}.");
                }

                // Check for clips in child folders
                foreach (var childFolder in childFolders)
                {
                    if (sampleFolderConfig.clipToFolderMap.TryGetValue(childFolder, out clipName))
                    {
                        CopyClip(childFolder, clipName, folderName);
                    }
                    else
                    {
                    //   Debug.LogWarning($"No clip mapping found for child folder {childFolder}.");
                    }
                }
            }
        }

        private void CopyClip(string folderName, string clipName, string parentFolder = null)
        {
            string targetFolder = parentFolder == null ? $"Assets/Audio/{folderName}" : $"Assets/Audio/{parentFolder}/{folderName}";
            string sourcePath = GetAudioFilePath(baseClipFolder, clipName);
            string targetPath = $"{targetFolder}/{Path.GetFileName(sourcePath)}";

            if (sourcePath == null)
            {
                return; 
            }

            if (!AssetDatabase.IsValidFolder(targetFolder))
            {
                string[] folderParts = targetFolder.Split('/');
                string currentPath = folderParts[0];
                for (int i = 1; i < folderParts.Length; i++)
                {
                    currentPath = Path.Combine(currentPath, folderParts[i]);
                    if (!AssetDatabase.IsValidFolder(currentPath))
                    {
                        AssetDatabase.CreateFolder(currentPath.Substring(0, currentPath.LastIndexOf('/')), folderParts[i]);
                    }
                }
            }

            // Copy asset
            if (System.IO.File.Exists(sourcePath))
            {
                bool success = AssetDatabase.CopyAsset(sourcePath, targetPath);
                if (!success)
                {
                Debug.LogError($"<color=teal>Failed to copy asset {clipName} from {sourcePath} to {targetPath}.</color>");
                }
                else
                {
                //  Debug.Log($"<color=teal>Copied {clipName} to {targetPath} successfully.</color>");
                }
            }
            else
            {
            Debug.LogError($"<color=teal>Source file does not exist: {sourcePath}</color>");
            }
        }

        private void OpenLicensePDF()
    {
        string fullPdfPath = Path.GetFullPath(pdfPath);
        string url = "file:///" + fullPdfPath.Replace("\\", "/");

        if (System.IO.File.Exists(fullPdfPath))
        {
            Application.OpenURL(url);
            Debug.Log("Opening: " + url);
        }
        else
        {
            Debug.LogError("File not found: " + fullPdfPath);
        }
    }
   #endregion
    }
}
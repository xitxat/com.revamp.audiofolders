    using UnityEngine;
    using System.Collections.Generic;
    using System.Linq;

namespace Revamp.AudioTools.FolderCreator
{
    [CreateAssetMenu(fileName = "SampleFolderConfig", menuName = "Audio Tool/Sample Folder Configuration")]
    public class SampleFolderConfig : ScriptableObject
    {
        public List<SampleFolder> folders;
        public Dictionary<string, string> clipToFolderMap; // Maps clip names to folder names

        public void BuildDictionary()
        {
            clipToFolderMap = new Dictionary<string, string>();
            foreach (var folder in folders)
            {
                foreach (var clip in folder.clips)
                {
                    //  one folder maps to one clip, 
                    if (!clipToFolderMap.ContainsKey(folder.folderName))
                    {
                        clipToFolderMap.Add(folder.folderName, clip.name);
                    }
                }
            }
        Debug.Log("<color=green>Dictionary built with " + clipToFolderMap.Count + " entries.</color>");
        }

    }

    [System.Serializable]
    public class SampleFolder
    {
        public int id;
        public string parentId; // Name of the parent folder
        public string folderName;
        public string localizationKey;
        public List<AudioClip> clips;
        public bool hasChildren;
    }
}
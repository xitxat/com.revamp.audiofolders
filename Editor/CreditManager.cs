    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;
    using UnityEngine.UIElements;

namespace Revamp.AudioTools.FolderCreator
{
    public class CreditManager
    {
        private Dictionary<string, string> creditInfo = new Dictionary<string, string>();
        private List<string> artistNames = new List<string>
        {
            "BluezoneCorp",
            "CB Sound Design",
            "Frederico Soler Fernandez",
            "InspectorJ",
            "Fabian Bentrup",
            "Justsoundeffects",
            "Marek Klemczak",
            "Pole Position Production",
            "Rogue Waves",
            "RYK-Sounds",
            "SanjoSounds",
            "Shapeforms Audio"
        };

        public CreditManager()
        {
            LoadCreditInfo(FolderManager.csvPath);
        }

        private void LoadCreditInfo(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError("File not found: " + filePath);
                return;
            }

            string[] lines = File.ReadAllLines(filePath);
            if (lines.Length <= 1) return; 

            // Parse headers to find column indices
            string[] headers = lines[0].Split(',');
            int keyIndex = Array.IndexOf(headers, "Key");
            int englishIndex = Array.IndexOf(headers, "English(en)");

            if (keyIndex == -1 || englishIndex == -1)
            {
                Debug.LogError("CSV headers are incorrect or missing 'Key' or 'English(en)' fields");
                return;
            }

            // Extract CREDIT_ rows and their English values
            for (int i = 1; i < lines.Length; i++)
            {
                var tokens = lines[i].Split(',');
                if (tokens.Length > keyIndex && tokens.Length > englishIndex)
                {
                    string key = tokens[keyIndex].Trim();
                    if (key.StartsWith("CREDIT_"))
                    {
                        creditInfo[key] = tokens[englishIndex].Trim();
                    }
                }
            }


            // Dictionary
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var pair in creditInfo)
            {
                sb.AppendLine($"{pair.Key}: {pair.Value}");
            }
           // Debug.Log(sb.ToString());
        }

        public void UpdateCreditTexts(VisualElement root)
        {
            //  parent element 
            var creditsContainer = root.Q<VisualElement>("credits-contain");
            if (creditsContainer == null) return;

            // Dynamically update via dictionary keys
            foreach (var entry in creditInfo)
            {
                var element = creditsContainer.Q<VisualElement>(entry.Key);
                if (element != null)
                {
                    if (element is Label label)
                    {
                        label.text = entry.Value;
                    }
                }
            }


            UpdateButtonText(creditsContainer, "btn_url_sonniss", "CREDIT_VISIT_SITE");
            UpdateArtistList(creditsContainer, "artists", artistNames);
        }

        private void UpdateButtonText(VisualElement container, string buttonName, string creditKey)
        {
            Button button = container?.Q<Button>(buttonName);
            if (button != null && creditInfo.TryGetValue(creditKey, out string text))
            {
                button.text = text;
            }
        }

        private void UpdateArtistList(VisualElement container, string listViewName, List<string> items)
        {
            var listView = container.Q<ListView>(listViewName);
            if (listView != null)
            {
                listView.makeItem = () => new Label();
                listView.bindItem = (element, i) => (element as Label).text = items[i];
                listView.itemsSource = items;
                listView.Rebuild();
            }
        }

    }
}
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using Bounce.Unmanaged;
using System.Collections.Generic;
using System.Linq;
using System;

namespace LordAshes
{
    [BepInPlugin(Guid, "Dice Selection Plug-In", Version)]
    [BepInDependency(FileAccessPlugin.Guid)]
    public partial class DiceSelectionPlugin : BaseUnityPlugin
    {
        // Plugin info
        public const string Guid = "org.lordashes.plugins.diceselection";
        public const string Version = "2.0.0.0";

        // Content directory
        private string dir = UnityEngine.Application.dataPath.Substring(0, UnityEngine.Application.dataPath.LastIndexOf("/")) + "/TaleSpire_CustomData/";

        private ConfigEntry<KeyboardShortcut> trigger;

        private int updateInterval = 5000;

        private bool showMenu = false;

        int perColumn = 11;

        private Dictionary<string, string[]> rollMacrosByCharaceter = new Dictionary<string, string[]>();

        private DateTime lastUpdateTime = DateTime.MinValue;

        /// <summary>
        /// Function for initializing plugin
        /// This function is called once by TaleSpire
        /// </summary>
        public void Awake()
        {
            UnityEngine.Debug.Log("Lord Ashes Dice Selection Plugin: Active.");

            trigger = Config.Bind("Hotkeys", "Open Roll Menu", new KeyboardShortcut(KeyCode.D, KeyCode.LeftControl));

            updateInterval = Config.Bind("Settings", "Update Interval In Milliseconds", 5000).Value;

            perColumn = Config.Bind("Settings", "Entries Per Column", 11).Value;

            LordAshes.DiceSelectionPlugin.Initialize(this.GetType());
        }

        /// <summary>
        /// Function for determining if view mode has been toggled and, if so, activating or deactivating Character View mode.
        /// This function is called periodically by TaleSpire.
        /// </summary>
        public void Update()
        {
            if(DateTime.UtcNow.Subtract(lastUpdateTime).TotalMilliseconds>updateInterval)
            {
                UnityEngine.Debug.Log("Lord Ashes Dice Selection Plugin: Updating Character Sheet Data.");
                UpdateCharacterSheets();
                lastUpdateTime = DateTime.UtcNow;
            }

            if (StrictKeyCheck(trigger.Value))
            {
                showMenu = !showMenu;
            }
        }

        public void OnGUI()
        {
            if (showMenu)
            {
                // Default macros
                string[] rollMacros = new string[]
                {
                    "1D4 [1D4]",
                    "1D6 [1D6]",
                    "1D8 [1D8]",
                    "1D10 [1D10]",
                    "1D12 [1D12]",
                    "1D20 [1D20]",
                    "2D4 [2D4]",
                    "2D6 [2D6]",
                    "2D10 [2D10]",
                    "3D4 [3D4]",
                    "3D6 [3D6]",
                    "4D4 [4D4]",
                };

                // Load character specific macros if available
                if (LocalClient.SelectedCreatureId != null)
                {
                    CreatureBoardAsset asset;
                    CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out asset);
                    if (asset != null)
                    {
                        UnityEngine.Debug.Log("Lord Ashes Dice Selection Plugin: Character Selected When Dice Menu IS Open");
                        if (rollMacrosByCharaceter.ContainsKey(GetCreatureName(asset)))
                        {
                            UnityEngine.Debug.Log("Lord Ashes Dice Selection Plugin: Character Data Obtained");
                            rollMacros = rollMacrosByCharaceter[GetCreatureName(asset)];
                        }
                    }
                }

                // Render macros
                GUI.backgroundColor = new UnityEngine.Color(0.2f, 0.2f, 0.2f, 0.9f);

                GUIStyle gs1 = new GUIStyle(GUI.skin.box);
                // gs1.normal.background = Texture2D.whiteTexture;
                gs1.normal.textColor = Color.yellow;
                gs1.fontSize = 24;
                gs1.border = new RectOffset() { top = 5, bottom = 5, left = 5, right = 5 };

                GUIStyle gs2 = new GUIStyle(GUI.skin.box);
                //gs2.normal.background = Texture2D.whiteTexture;
                gs2.normal.textColor = Color.yellow;
                gs2.fontSize = 14;
                gs2.border = new RectOffset() { top = 5, bottom = 5, left = 5, right = 5 };

                int xOffset = (1920-((int)Math.Ceiling(rollMacros.Count()/(decimal)perColumn)*310))/2;
                int yOffset = 0;
                for (int i = 0; i < rollMacros.Length; i++)
                {
                    if (rollMacros[i] != "")
                    {
                        if (GUI.Button(new Rect(5 + xOffset, 50 + yOffset, 300, 40), GetRollName(rollMacros[i]), gs1))
                        {
                            showMenu = false;
                            CreateDice(GetRollName(rollMacros[i]), GetFormula(rollMacros[i]));
                            break;
                        }
                        if (GUI.Button(new Rect(5 + xOffset, 50 + 40 + yOffset, 300, 25), GetFormula(rollMacros[i]), gs2))
                        {
                            showMenu = false;
                            CreateDice(GetRollName(rollMacros[i]), GetFormula(rollMacros[i]));
                            break;
                        }
                    }
                    yOffset = yOffset + 80;
                    if (yOffset > perColumn * 80) { yOffset = 0; xOffset = xOffset + 310; }
                }
            }
        }

        public void CreateDice(string name, string formula)
        {
            if (formula == "?")
            {
                SystemMessage.AskForTextInput("Roll...", "Roll Formula:", "OK", (manualFormula) => CreateDice(name, manualFormula),null,"Cancel",null,"");
                return;
            }

            Debug.Log("Dice Selection '"+name+"' Activated Which Rolls '"+formula+"'");
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo = new System.Diagnostics.ProcessStartInfo()
            {
                CreateNoWindow = true,
                FileName = "talespire://dice/"+name+":"+formula,
                Arguments = "",
                UseShellExecute = true,
                Verb = "Open"
            };
            p.Start();
        }

        public string GetCreatureName(CreatureBoardAsset creature)
        {
            string name = ((creature.Name != null) ? creature.Name : creature.CreatureId.ToString());
            if(name.IndexOf("<size=0")>-1)
            {
                name = name.Substring(0, name.IndexOf("<size=0"));
            }
            return name;
        }

        public string GetRollName(string txt)
        {
            return txt.Substring(0, txt.IndexOf("[")).Replace(" "," ").Trim();
        }

        public string GetFormula(string txt)
        {
            txt = txt.Substring(txt.IndexOf("[")+1);
            return txt.Substring(0, txt.IndexOf("]")).Trim();
        }

        public void UpdateCharacterSheets()
        {
            // Use temporary storage to ensure character rolls are available while performing update
            Dictionary<string, string[]> characeterSheetData = new Dictionary<string, string[]>();
            // Find each DSM file
            string[] characterSheetFiles = FileAccessPlugin.File.Find(".dsm");
            // Process each DSM file
            foreach (string characterSheetFile in characterSheetFiles)
            {
                // Get creature name from file name
                string creatureName = System.IO.Path.GetFileNameWithoutExtension(characterSheetFile);
                // Add contents of DSM file to character sheet dictionary
                UnityEngine.Debug.Log("Lord Ashes Dice Selection Plugin: Updating Character Sheet Data For '"+creatureName+"'.");
                if (!characeterSheetData.ContainsKey(creatureName))
                {
                    characeterSheetData.Add(creatureName, FileAccessPlugin.File.ReadAllLines(characterSheetFile));
                }
                else
                {
                    Debug.LogWarning("Lord Ashes Dice Selection Plugin: Multiple Chartacter Sheets For '" + creatureName + "'");
                }
            }
            // Switch over to the newly read DSM content
            rollMacrosByCharaceter = characeterSheetData;
        }

        /// <summary>
        /// Method to properly evaluate shortcut keys. 
        /// </summary>
        /// <param name="check"></param>
        /// <returns></returns>
        public bool StrictKeyCheck(KeyboardShortcut check)
        {
            if (!check.IsUp()) { return false; }
            foreach (KeyCode modifier in new KeyCode[] { KeyCode.LeftAlt, KeyCode.RightAlt, KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftShift, KeyCode.RightShift })
            {
                if (Input.GetKey(modifier) != check.Modifiers.Contains(modifier)) { return false; }
            }
            return true;
        }
    }
}

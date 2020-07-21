using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using System;
using System.Reflection;
using UnityEngine;

namespace Picker
{
    public class Picker : IUserMod
    {
        public string Name => "Picker";
        public string Description => "Eyedrop any object from the map, by Elektrix and Quboid";
        public const string settingsFileName = "Picker";

        private static int findItVersion = -1;
        internal static int FindItVersion
        {
            get
            {
                if (findItVersion == -1)
                {
                    findItVersion = GetFindItVersion();
                }
                return findItVersion;
            }
        }

        private static int moveItVersion = -1;
        internal static int MoveItVersion
        {
            get
            {
                if (moveItVersion == -1)
                {
                    moveItVersion = GetMoveItVersion();
                }
                return moveItVersion;
            }
        }

        public Picker()
        {
            try
            {
                // Creating setting file
                if (GameSettings.FindSettingsFileByName(settingsFileName) == null)
                {
                    GameSettings.AddSettingsFile(new SettingsFile[] { new SettingsFile() { fileName = settingsFileName } });
                }
            }
            catch (Exception e)
            {
                Debug.Log($"Could not load/create the setting file.\n{e}");
            }
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelperBase group = helper.AddGroup(Name);

            //Assembly assembly = null;
            //foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            //{
            //    if (a.FullName.Length >= 12 && a.FullName.Substring(0, 12) == "NetworkSkins")
            //    {
            //        assembly = a;
            //        break;
            //    }
            //}

            ((UIPanel)((UIHelper)group).self).gameObject.AddComponent<OptionsKeymappingMain>();
            UIPanel panel = ((UIHelper)group).self as UIPanel;

            group.AddSpace(20);

            UIButton button = (UIButton)group.AddButton("Reset Button Position", () =>
            {
                UIPickerButton.savedX.value = -1000;
                UIPickerButton.savedY.value = -1000;
                PickerTool.instance?.m_button?.ResetPosition();
            });

            group.AddSpace(20);

            panel = ((UIHelper)group).self as UIPanel;
            UILabel fitLabel = panel.AddUIComponent<UILabel>();
            fitLabel.name = "fitLabel";
            fitLabel.text = $"Find It: ";
            switch (GetFindItVersion())
            {
                case 0:
                    fitLabel.text += "Not found";
                    break;
                case 1:
                    fitLabel.text += "Found (v1)";
                    break;
                case 2:
                    fitLabel.text += "Found (v2)";
                    break;
                default:
                    fitLabel.text += "Unknown";
                    break;
            }

            UILabel mitLabel = panel.AddUIComponent<UILabel>();
            mitLabel.name = "mitLabel";
            mitLabel.text = $"Move It: ";
            switch (GetMoveItVersion())
            {
                case 0:
                    mitLabel.text += "Not found";
                    break;
                case 1:
                    mitLabel.text += "Found";
                    break;
                default:
                    mitLabel.text += "Unknown";
                    break;
            }

            UILabel ns2Label = panel.AddUIComponent<UILabel>();
            ns2Label.name = "ns2Label";
            ns2Label.text = $"Network Skins 2: ";
            switch (PickerTool.isNS2Installed())
            {
                case false:
                    ns2Label.text += "Not found";
                    break;
                case true:
                    ns2Label.text += "Found";
                    break;
            }

            group.AddSpace(20);
        }

        internal static Assembly GetAssembly(string name)
        {
            Assembly assembly = null;
            foreach (PluginManager.PluginInfo plugin in PluginManager.instance.GetPluginsInfo())
            {
                foreach (Assembly a in plugin.GetAssemblies())
                {
                    if (plugin.isEnabled && a.GetName().Name.ToLower() == name)
                    {
                        //Debug.Log($"Assembly {name} found");
                        assembly = a;
                        break;
                    }
                }
            }
            return assembly;
        }

        internal static int GetFindItVersion()
        {
            Assembly assembly = GetAssembly("findit");

            if (assembly == null)
            {
                return 0;
            }

            if (assembly.FullName.StartsWith("FindIt, Version=1.0"))
            {
                return 1;
            }

            return 2;
        }

        internal static int GetMoveItVersion()
        {
            Assembly assembly = GetAssembly("moveit");

            if (assembly == null)
            {
                return 0;
            }
            return 1;
        }
    }
}

using ColossalFramework;
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

            group.AddSpace(20);

            UIButton button = (UIButton)group.AddButton("Reset Button Position", () =>
            {
                UIPickerButton.savedX.value = -1000;
                UIPickerButton.savedY.value = -1000;
                PickerTool.instance?.m_button?.ResetPosition();
            });
        }
    }
}

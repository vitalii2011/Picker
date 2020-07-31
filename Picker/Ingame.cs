using ICities;
using ColossalFramework.Plugins;
using System.Reflection;
using UnityEngine;

namespace Picker
{
    public class IngameLoader : LoadingExtensionBase
    {
        private static GameObject GO_FindIt;

        public override void OnLevelLoaded(LoadMode mode)
        {
            if (!(mode == LoadMode.LoadGame || mode == LoadMode.NewGame || mode == LoadMode.NewGameFromScenario))
            {
                return;
            }

            InstallMod();
        }

        public override void OnLevelUnloading()
        {
            UninstallMod();
        }

        public static void InstallMod()
        {
            if (PickerTool.instance == null)
            {
                ToolController toolController = ToolsModifierControl.toolController;
                PickerTool.instance = toolController.gameObject.AddComponent<PickerTool>();
                PickerTool.instance.enabled = false;
            }
            else
            {
                Debug.Log($"Picker: InstallMod with existing instance!");
            }

            GO_FindIt = new GameObject();
            PickerTool.FindIt = GO_FindIt.AddComponent<FindItManager>();
        }

        public static void UninstallMod()
        {
            if (ToolsModifierControl.toolController.CurrentTool is PickerTool)
                ToolsModifierControl.SetTool<DefaultTool>();

            if (PickerTool.instance != null)
            {
                PickerTool.instance.enabled = false;
            }

            Object.Destroy(GO_FindIt);
            PickerTool.FindIt = null;
            Object.Destroy(PickerTool.instance.m_button);
            PickerTool.instance.m_button = null;
            Object.Destroy(PickerTool.instance);
            PickerTool.instance = null;
        }
    }
}

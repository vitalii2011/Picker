using ColossalFramework.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Picker
{
    public partial class PickerTool
    {
        internal bool ReflectIntoNS2()
        {
            Assembly ass = Picker.GetAssembly("networkskins");
            Type tPipette = ass.GetType("NetworkSkins.Tool.PipetteTool")
                ?? throw new Exception("NS2 failed: tPipette is null");
            object ns2 = ass.GetType("NetworkSkins.GUI.NetworkSkinPanelController").GetField("Instance").GetValue(null) ?? throw new Exception("NS2 failed: ns2 is null");
            object pipette = ns2.GetType().GetProperty("Tool").GetValue(ns2, null) ?? throw new Exception("NS2 failed: pipette is null");

            MethodInfo apply = tPipette.GetMethod("ApplyTool", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new Exception("NS2 failed: apply is null");
            FieldInfo segmentId = tPipette.GetField("HoveredSegmentId", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new Exception("NS2 failed: segmentId is null");

            //Debug.Log($"NS2: {ns2},{apply}\n{pipette} <{pipette.GetType()}>\nlmb:{segmentId} <{segmentId.GetType()}>");

            segmentId.SetValue(pipette, hoveredId.NetSegment);
            apply.Invoke(pipette, null);

            return true;
        }

        internal static bool isNS2Installed()
        {
            if (!PluginManager.instance.GetPluginsInfo().Any(mod => (
                    mod.publishedFileID.AsUInt64 == 1758376843uL ||
                    mod.name.Contains("NetworkSkins2") ||
                    mod.name.Contains("1758376843")
            ) && mod.isEnabled))
            {
                return false;
            }

            if (PluginManager.instance.GetPluginsInfo().Any(mod =>
                    mod.publishedFileID.AsUInt64 == 543722850uL ||
                    (mod.name.Contains("NetworkSkins") && !mod.name.Contains("NetworkSkins2")) ||
                    mod.name.Contains("543722850")
            ))
            {
                return false;
            }

            return true;
        }
    }
}

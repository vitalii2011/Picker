using System;
using System.Reflection;
using UnityEngine;

namespace Picker
{
    public partial class PickerTool
    {
        internal bool ReflectIntoMoveIt()
        {
            Assembly a = Picker.GetAssembly("moveit");
            Type tMoveIt = a.GetType("MoveIt.MoveItTool");
            Type tInstance = a.GetType("MoveIt.Instance");
            if (tMoveIt == null || tInstance == null)
            {
                Debug.Log($"Move It not found");
                return false;
            }
            object MoveItInstance = tMoveIt.GetField("instance").GetValue(null);

            if ((bool)tMoveIt.GetProperty("enabled").GetValue(MoveItInstance, null))
            {
                object hovered = tMoveIt.GetField("m_hoverInstance").GetValue(MoveItInstance);
                if (hovered != null)
                {
                    hoveredId = (InstanceID)tInstance.GetProperty("id").GetValue(hovered, null); // PO is stored in InstanceID.NetLane
                    object IInfo = tInstance.GetProperty("Info").GetValue(hovered, null);
                    object info = IInfo.GetType().GetProperty("Prefab").GetValue(IInfo, null);
                    Activate((PrefabInfo)info);
                    return true;
                }
                else
                {
                    hoveredId = InstanceID.Empty;
                }
            }
            return false;
        }
    }
}

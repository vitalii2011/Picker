using ColossalFramework.Globalization;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Picker
{
    public partial class PickerTool : ToolBase
    {
        public static PickerTool instance;

        internal UIPickerButton m_button;

        // Allow....?
        public bool allowSegments = true;
        public bool allowNodes = true;
        public bool allowProps = true;
        public bool allowTrees = true;
        public bool allowBuildings = true;

        public InstanceID hoveredId = InstanceID.Empty;

        public bool hasSteppedOver = false;
        public List<InstanceID> objectBuffer = new List<InstanceID>();
        public int stepOverCounter = 0;
        public Vector3 stepOverPosition = Vector3.zero;
        public Vector3 mouseCurrentPosition = Vector3.zero;

        protected override void Awake()
        {
            m_toolController = FindObjectOfType<ToolController>();
            enabled = false;

            m_button = UIView.GetAView().AddUIComponent(typeof(UIPickerButton)) as UIPickerButton;

            if (IsRicoEnabled())
            { // Will need changed if/when Find It 2 adds more filter types
                MenuIndex["Prop"] += Picker.FindItVersion;
                MenuIndex["Decal"] += Picker.FindItVersion;
            }
        }

        private Dictionary<string, UIComponent> _componentCache = new Dictionary<string, UIComponent>();

        private Color hoverColor = new Color32(0, 181, 255, 255);

        private T FindComponentCached<T>(string name) where T : UIComponent
        {
            if (!_componentCache.TryGetValue(name, out UIComponent component) || component == null)
            {
                component = UIView.Find<UIButton>(name);
                _componentCache[name] = component;
            }

            return component as T;
        }

        protected override void OnToolUpdate()
        {
            base.OnToolUpdate();

            // Raycast to all currently "allowed"
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastInput input = new RaycastInput(ray, Camera.main.farClipPlane)
            {
                m_ignoreTerrain = false,
                m_ignoreSegmentFlags = allowSegments ? NetSegment.Flags.Untouchable : NetSegment.Flags.All,
                m_ignoreNodeFlags = NetNode.Flags.All, //allowNodes ? NetNode.Flags.Untouchable : NetNode.Flags.All,
                m_ignorePropFlags = allowProps ? PropInstance.Flags.None : PropInstance.Flags.All,
                m_ignoreTreeFlags = allowTrees ? TreeInstance.Flags.None : TreeInstance.Flags.All,
                m_ignoreBuildingFlags = allowBuildings ? Building.Flags.None : Building.Flags.All,

                m_ignoreCitizenFlags = CitizenInstance.Flags.All,
                m_ignoreVehicleFlags = Vehicle.Flags.Created,
                m_ignoreDisasterFlags = DisasterData.Flags.All,
                m_ignoreTransportFlags = TransportLine.Flags.All,
                m_ignoreParkedVehicleFlags = VehicleParked.Flags.All,
                m_ignoreParkFlags = DistrictPark.Flags.All
            };

            RayCast(input, out RaycastOutput output);

            // Set the world mouse position (useful for my implementation of StepOver)
            mouseCurrentPosition = output.m_hitPos;

            objectBuffer.Clear();

            if (output.m_netSegment != 0) objectBuffer.Add(new InstanceID() { NetSegment = output.m_netSegment });
            //if (output.m_netNode != 0) objectBuffer.Add(new InstanceID() { NetNode = output.m_netNode });
            if (output.m_treeInstance != 0) objectBuffer.Add(new InstanceID() { Tree = output.m_treeInstance });
            if (output.m_propInstance != 0) objectBuffer.Add(new InstanceID() { Prop = output.m_propInstance });
            if (output.m_building != 0) objectBuffer.Add(new InstanceID() { Building = output.m_building });

            objectBuffer.Sort((a, b) => Vector3.Distance(a.Position(), mouseCurrentPosition).CompareTo(Vector3.Distance(b.Position(), mouseCurrentPosition)));
            if (objectBuffer.Count > 0)
            {
                hoveredId = objectBuffer[0];
            }
            else
            {
                hoveredId = InstanceID.Empty;
            }

            // A prefab has been selected. Find it in the UI and enable it.
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
            {
                if (hoveredId.Info() == null)
                {
                    enabled = false;
                    ToolsModifierControl.SetTool<DefaultTool>();
                    return;
                }
                ShowInPanelResolveGrowables(DefaultPrefab(hoveredId.Info()));
            }

            // Escape key or RMB hit = disable the tool
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                enabled = false;
                ToolsModifierControl.SetTool<DefaultTool>();
            }
        }

        private void ShowInPanelResolveGrowables(PrefabInfo pInfo)
        {
            Debug.Log($"Hovered: {pInfo.name} ({hoveredId.Type})\nB:{hoveredId.Building}, P/D:{hoveredId.Prop}, PO:{hoveredId.NetLane}, N:{hoveredId.NetNode}, S:{hoveredId.NetSegment}");
            if (!(pInfo is BuildingInfo || pInfo is PropInfo))
            {
                ShowInPanel(pInfo);
                return;
            }

            // Try to locate in Find It!
            if (FISearchbox == null)
            {
                return;
            }
            if (FIFilterDropdown == null)
            {
                return;
            }

            if (pInfo is PropInfo propInfo)
            {
                if (propInfo.m_isDecal)
                    SetFIDropdown("Decal");
                else
                    SetFIDropdown("Prop");

                UITextField TextField = FISearchbox.Find<UITextField>("UITextField");
                TextField.text = "";

                if (Picker.FindItVersion == 2 && !propInfo.m_isDecal)
                {
                    UIComponent UIFilterProp = FISearchbox.Find("UIFilterProp");
                    UIFilterProp.GetComponentInChildren<UIButton>().SimulateClick();
                }

                ReflectIntoFindIt(propInfo);
                return;
            }

            BuildingInfo info = pInfo as BuildingInfo;
            if (info != null && (
                (info.m_class.m_service == ItemClass.Service.Residential) ||
                (info.m_class.m_service == ItemClass.Service.Industrial) ||
                (info.m_class.m_service == ItemClass.Service.Commercial) ||
                (info.m_class.m_service == ItemClass.Service.Office) ||
                (info.m_class.m_service == ItemClass.Service.Tourism)
                ))
            {
                //Debug.Log("Info " + info.name + " is a growable (or RICO).");

                SetFIDropdown("Growable");

                UITextField TextField = FISearchbox.Find<UITextField>("UITextField");
                TextField.text = "";

                UIComponent UIFilterGrowable = FISearchbox.Find("UIFilterGrowable");
                UIFilterGrowable.GetComponentInChildren<UIButton>().SimulateClick();

                // Reflect into the scroll panel, starting with the growable panel:
                ReflectIntoFindIt(info);
            }
            else
            {
                //Debug.Log("Info " + info.name + " is not a growable.");
                ShowInPanel(pInfo);
            }
        }

        private void ShowInPanel(PrefabInfo info)
        {
            UIButton button = FindComponentCached<UIButton>(info.name);
            if (button != null)
            {
                if (hoveredId.NetSegment > 0 && isNS2Installed())
                {
                    try
                    {
                        ReflectIntoNS2();
                        return;
                    }
                    catch (Exception e)
                    {
                        Debug.Log($"NS2 failed:\n{e}");
                    }
                }

                UIView.Find("TSCloseButton").SimulateClick();
                UITabstrip subMenuTabstrip = null;
                UIScrollablePanel scrollablePanel = null;
                UIPanel filterPanel;
                UIComponent current = button, parent = button.parent;

                int subMenuTabstripIndex = -1, menuTabstripIndex = -1;
                while (parent != null)
                {
                    if (current.name == "ScrollablePanel")
                    {
                        subMenuTabstripIndex = parent.zOrder;
                        scrollablePanel = current as UIScrollablePanel;
                    }
                    if (current.name == "GTSContainer")
                    {
                        menuTabstripIndex = parent.zOrder;
                        subMenuTabstrip = parent.Find<UITabstrip>("GroupToolstrip");
                    }
                    current = parent;
                    parent = parent.parent;
                }
                UITabstrip menuTabstrip = current.Find<UITabstrip>("MainToolstrip");
                if (scrollablePanel == null || subMenuTabstrip == null || menuTabstrip == null || menuTabstripIndex == -1 || subMenuTabstripIndex == -1)
                {
                    Debug.Log($"UI Panel not found");
                    return;
                }

                scrollablePanel.parent.GetComponentInChildren<UIPanel>();
                menuTabstrip.selectedIndex = menuTabstripIndex;
                menuTabstrip.ShowTab(menuTabstrip.tabs[menuTabstripIndex].name);
                subMenuTabstrip.selectedIndex = subMenuTabstripIndex;
                subMenuTabstrip.ShowTab(subMenuTabstrip.tabs[subMenuTabstripIndex].name);

                filterPanel = scrollablePanel.parent.Find<UIPanel>("FilterPanel");
                if (filterPanel != null)
                {
                    foreach (UIMultiStateButton c in filterPanel.GetComponentsInChildren<UIMultiStateButton>())
                    {
                        if (c.isVisible && c.activeStateIndex == 1)
                        {
                            c.activeStateIndex = 0;
                        }
                    }
                }

                StartCoroutine(ShowInPanelProcess(scrollablePanel, button));
            }
            else
            {
                Debug.Log($"Button not found, falling back to FindIt/All");
                SetFIDropdown("All");
                ReflectIntoFindIt(info);
            }
        }

        private IEnumerator<object> ShowInPanelProcess(UIScrollablePanel scrollablePanel, UIButton button)
        {
            yield return new WaitForSeconds(0.10f);

            button.SimulateClick();
            scrollablePanel.ScrollIntoView(button);
        }

        private static bool IsRicoEnabled()
        {
            foreach (PluginManager.PluginInfo plugin in PluginManager.instance.GetPluginsInfo())
            {
                foreach (Assembly assembly in plugin.GetAssemblies())
                {
                    if (assembly.GetName().Name.ToLower() == "ploppablerico")
                    {
                        Debug.Log("pRICO found");
                        return plugin.isEnabled;
                    }
                }
            }

            return false;
        }

        public PrefabInfo DefaultPrefab(PrefabInfo resolve)
        {
            if (!(resolve is NetInfo)) return resolve;

            NetInfo info = resolve as NetInfo;
            for (uint i = 0; i < PrefabCollection<NetInfo>.LoadedCount(); i++)
            {
                NetInfo prefab = PrefabCollection<NetInfo>.GetLoaded(i);
                if ((AssetEditorRoadUtils.TryGetBridge(prefab) != null && AssetEditorRoadUtils.TryGetBridge(prefab).name == info.name) ||
                   (AssetEditorRoadUtils.TryGetElevated(prefab) != null && AssetEditorRoadUtils.TryGetElevated(prefab).name == info.name) ||
                   (AssetEditorRoadUtils.TryGetSlope(prefab) != null && AssetEditorRoadUtils.TryGetSlope(prefab).name == info.name) ||
                   (AssetEditorRoadUtils.TryGetTunnel(prefab) != null && AssetEditorRoadUtils.TryGetTunnel(prefab).name == info.name))
                {
                    if (AssetEditorRoadUtils.TryGetBridge(prefab) != null && AssetEditorRoadUtils.TryGetBridge(prefab).name == info.name)
                    {
                        FRTSet("BridgeMode");
                    }
                    else if (AssetEditorRoadUtils.TryGetElevated(prefab) != null && AssetEditorRoadUtils.TryGetElevated(prefab).name == info.name)
                    {
                        FRTSet("ElevatedMode");
                    }
                    else if (AssetEditorRoadUtils.TryGetTunnel(prefab) != null && AssetEditorRoadUtils.TryGetTunnel(prefab).name == info.name)
                    {
                        FRTSet("TunnelMode");
                    }
                    else
                    {
                        FRTSet("GroundMode");
                    }

                    return prefab;
                }
                else if (prefab == info)
                {
                    FRTSet("GroundMode");
                }
            }
            return info;
        }

        private void FRTSet(string buttonName)
        {
            UIButton button = FindComponentCached<UIButton>("FRT_" + buttonName);
            //Debug.Log($"AAA {button.name} vis:{button.isVisible}, en:{button.enabled}");
            if (button is UIComponent)
            {
                SimulateClick(button);
            }
        }

        // From MoreShortcuts by Boogieman Sam
        private static void SimulateClick(UIComponent component)
        {
            Camera camera = component.GetCamera();
            Vector3 vector = camera.WorldToScreenPoint(component.center);
            Ray ray = camera.ScreenPointToRay(vector);
            UIMouseEventParameter p = new UIMouseEventParameter(component, UIMouseButton.Left, 1, ray, vector, Vector2.zero, 0f);

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            if (component.isEnabled)
            {
                component.GetType().GetMethod("OnMouseDown", flags).Invoke(component, new object[] { p });
                component.GetType().GetMethod("OnClick", flags).Invoke(component, new object[] { p });
                component.GetType().GetMethod("OnMouseUp", flags).Invoke(component, new object[] { p });
            }
            else
                component.GetType().GetMethod("OnDisabledClick", flags).Invoke(component, new object[] { p });

        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            base.RenderOverlay(cameraInfo);
            if (!enabled) return;

            if (hoveredId.NetSegment != 0)
            {
                NetSegment hoveredSegment = hoveredId.NetSegment.S();
                NetTool.RenderOverlay(cameraInfo, ref hoveredSegment, hoverColor, hoverColor);
            }
            //else if (hoveredObject.NetNode != 0 && hoveredObject.NetNode < 32768)
            //{
            //    NetNode hoveredNode = hoveredObject.NetNode.N();
            //    RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, hoverColor, hoveredNode.m_position, Mathf.Max(6f, hoveredNode.Info.m_halfWidth * 2f), -1f, 1280f, false, true);
            //}
            else if (hoveredId.Building != 0)
            {
                Building hoveredBuilding = hoveredId.Building.B();
                BuildingTool.RenderOverlay(cameraInfo, ref hoveredBuilding, hoverColor, hoverColor);
                
                while (hoveredBuilding.m_subBuilding > 0)
                {
                    hoveredBuilding = BuildingManager.instance.m_buildings.m_buffer[hoveredBuilding.m_subBuilding];
                    BuildingTool.RenderOverlay(cameraInfo, ref hoveredBuilding, hoverColor, hoverColor);
                }
            }
            else if (hoveredId.Tree != 0)
            {
                TreeInstance hoveredTree = hoveredId.Tree.T();
                TreeTool.RenderOverlay(cameraInfo, hoveredTree.Info, hoveredTree.Position, hoveredTree.Info.m_minScale, hoverColor);
            }
            else if (hoveredId.Prop != 0)
            {
                PropInstance hoveredProp = hoveredId.Prop.P();
                PropTool.RenderOverlay(cameraInfo, hoveredProp.Info, hoveredProp.Position, hoveredProp.Info.m_minScale, hoveredProp.Angle, hoverColor);
            }
        }
    }
}

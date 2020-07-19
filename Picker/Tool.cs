using ColossalFramework.Globalization;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Picker
{
    public class PickerTool : ToolBase
    {
        public static PickerTool instance;

        internal UIPickerButton m_button;

        // Allow....?
        public bool allowSegments = true;
        public bool allowNodes = true;
        public bool allowProps = true;
        public bool allowTrees = true;
        public bool allowBuildings = true;

        public InstanceID hoveredObject = InstanceID.Empty;

        public bool hasSteppedOver = false;
        public List<InstanceID> objectBuffer = new List<InstanceID>();
        public int stepOverCounter = 0;
        public Vector3 stepOverPosition = Vector3.zero;
        public Vector3 mouseCurrentPosition = Vector3.zero;

        private static Dictionary<String, int> MenuIndex = new Dictionary<string, int>
        {
            ["Ploppable"] = 2,
            ["Growable"] = 3,
            ["RICO"] = 4,
            ["Prop"] = 4,
            ["Decal"] = 5
        };

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

        internal bool ReflectIntoMoveIt()
        {
            Type tMoveIt = Picker.GetAssembly("moveit").GetType("MoveIt.MoveItTool");
            object MoveItInstance = tMoveIt.GetField("instance").GetValue(null);

            if ((bool)tMoveIt.GetProperty("enabled").GetValue(MoveItInstance, null))
            {
                object hovered = tMoveIt.GetField("m_hoverInstance").GetValue(MoveItInstance);
                if (hovered != null)
                {
                    InstanceID id = (InstanceID)hovered.GetType().GetProperty("id").GetValue(hovered, null);
                    ShowInPanelResolveGrowables(DefaultPrefab(id.Info()));
                    return true;
                }
            }
            return false;
        }

        private void ReflectIntoFindIt(PrefabInfo info)
        {
            Type FindItType = Type.GetType("FindIt.FindIt, FindIt");
            object FindItInstance = FindItType.GetField("instance").GetValue(null);
            Type SearchBoxType = Type.GetType("FindIt.GUI.UISearchBox, FindIt");
            object UISearchbox = FindItType.GetField("searchBox").GetValue(FindItInstance);

            FieldInfo checkBoxType = SearchBoxType.GetField(info.m_isCustomContent ? "workshopFilter" : "vanillaFilter");
            if (checkBoxType != null)
            {
                UICheckBox checkBox = (UICheckBox)checkBoxType.GetValue(UISearchbox);
                checkBox.isChecked = true;
            }

            StartCoroutine(ReflectIntoFindItProcess(info, false));
        }

        private IEnumerator<object> ReflectIntoFindItProcess(PrefabInfo info, bool tryingPRICO)
        {
            yield return new WaitForSeconds(0.05f);

            bool found = false;

            Type FindItType = Type.GetType("FindIt.FindIt, FindIt");
            object FindItInstance = FindItType.GetField("instance").GetValue(null);
            object UIScrollPanel = FindItType.GetField("scrollPanel").GetValue(FindItInstance);
            Type ScrollPanelType = Type.GetType("FindIt.GUI.UIScrollPanel, FindIt");
            
            // Get all the item data...
            PropertyInfo iData = ScrollPanelType.GetProperty("itemsData");
            object itemsData = iData.GetValue(UIScrollPanel, null);
            object[] itemDataBuffer = itemsData.GetType().GetMethod("ToArray").Invoke(itemsData, null) as object[];
            
            for (int i = 0; i < itemDataBuffer.Length; i++)
            {
                object itemData = itemDataBuffer[i];

                // Get the actual asset data of this prefab instance in the Find It scrollable panel
                Type UIScrollPanelItemData = itemData.GetType();
                object itemData_currentData_asset = UIScrollPanelItemData.GetField("asset").GetValue(itemData);
                PrefabInfo itemData_currentData_asset_info = itemData_currentData_asset.GetType().GetProperty("prefab").GetValue(itemData_currentData_asset, null) as PrefabInfo;
                
                // Display data at this position. Return.
                if (itemData_currentData_asset_info != null && itemData_currentData_asset_info.name == info.name)
                {
                    Debug.Log("Found data at position " + i + " in Find it ScrollablePanel");
                    ScrollPanelType.GetMethod("DisplayAt").Invoke(UIScrollPanel, new object[] { i });

                    string itemDataName = UIScrollPanelItemData.GetField("name").GetValue(itemData) as string;
                    UIComponent test = UIScrollPanel as UIComponent;
                    UIButton[] fYou = test.GetComponentsInChildren<UIButton>();
                    foreach (UIButton mhmBaby in fYou)
                    {
                        if (mhmBaby.name == itemDataName)
                        {
                            mhmBaby.SimulateClick();
                            found = true;
                        }
                    }
                    if (found)
                    {
                        break;
                    }
                }
            }

            if (!found)
            {
                if (info is BuildingInfo && !tryingPRICO)
                {
                    // If it's a building and it hasn't been found, try again for Ploppable RICO
                    UIDropDown FilterDropdown = UIView.Find("UISearchBox").Find<UIDropDown>("UIDropDown");
                    FilterDropdown.selectedIndex = MenuIndex["RICO"];
                    StartCoroutine(ReflectIntoFindItProcess(info, true));
                }
                else
                {
                    Debug.Log($"Object {info.name} not found");
                }
            }
        }

        private void ShowInPanelResolveGrowables(PrefabInfo pInfo)
        {
            if (!(pInfo is BuildingInfo || pInfo is PropInfo))
            {
                ShowInPanel(pInfo);
                return;
            }

            // Try to locate in Find It!
            UIComponent SearchBoxPanel = UIView.Find("UISearchBox");
            if (SearchBoxPanel != null && SearchBoxPanel.isVisible == false)
            {
                UIButton FIButton = UIView.Find<UIButton>("FindItMainButton");
                if (FIButton == null) return;
                FIButton.SimulateClick();
            }
            if (SearchBoxPanel == null)
            {
                Debug.Log($"Find It not found");
                return;
            }

            UIDropDown FilterDropdown = SearchBoxPanel.Find<UIDropDown>("UIDropDown");
            if (FilterDropdown == null)
            {
                Debug.Log($"Find It filters not found");
                return;
            }

            if (pInfo is PropInfo propInfo)
            {
                if (propInfo.m_isDecal)
                    FilterDropdown.selectedIndex = MenuIndex["Decal"];
                else
                    FilterDropdown.selectedIndex = MenuIndex["Prop"];

                UITextField TextField = SearchBoxPanel.Find<UITextField>("UITextField");
                TextField.text = "";

                if (Picker.FindItVersion == 2 && !propInfo.m_isDecal)
                {
                    UIComponent UIFilterProp = SearchBoxPanel.Find("UIFilterProp");
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
                Debug.Log("Info " + info.name + " is a growable (or RICO).");

                FilterDropdown.selectedIndex = MenuIndex["Growable"];

                UITextField TextField = SearchBoxPanel.Find<UITextField>("UITextField");
                TextField.text = "";

                UIComponent UIFilterGrowable = SearchBoxPanel.Find("UIFilterGrowable");
                UIFilterGrowable.GetComponentInChildren<UIButton>().SimulateClick();

                // Reflect into the scroll panel, starting with the growable panel:
                ReflectIntoFindIt(info);
            }
            else
            {
                Debug.Log("Info " + info.name + " is not a growable.");
                ShowInPanel(pInfo);
            }
        }

        private void ShowInPanel(PrefabInfo info)
        {
            UIButton button = FindComponentCached<UIButton>(info.name);
            if (button != null)
            {
                UIView.Find("TSCloseButton").SimulateClick();
                UITabstrip subMenuTabstrip = null;
                UIScrollablePanel scrollablePanel = null;
                UIPanel filterPanel = null;
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

        }

        private IEnumerator<object> ShowInPanelProcess(UIScrollablePanel scrollablePanel, UIButton button)
        {
            yield return new WaitForSeconds(0.05f);

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
                        FRTSet("NormalMode");
                    }

                    return prefab;
                }
                else if (prefab == info)
                {
                    FRTSet("NormalMode");
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

        protected override void OnToolUpdate()
        {
            base.OnToolUpdate();

            // Raycast to all currently "allowed"
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastInput input = new RaycastInput(ray, Camera.main.farClipPlane)
            {
                m_ignoreTerrain = false,
                m_ignoreSegmentFlags = allowSegments ? NetSegment.Flags.Untouchable : NetSegment.Flags.All,
                m_ignoreNodeFlags = allowNodes ? NetNode.Flags.Untouchable : NetNode.Flags.All,
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

            if (output.m_netSegment   != 0) objectBuffer.Add(new InstanceID() { NetSegment = output.m_netSegment   });
            if (output.m_netNode      != 0) objectBuffer.Add(new InstanceID() { NetNode    = output.m_netNode      });
            if (output.m_treeInstance != 0) objectBuffer.Add(new InstanceID() { Tree       = output.m_treeInstance });
            if (output.m_propInstance != 0) objectBuffer.Add(new InstanceID() { Prop       = output.m_propInstance });
            if (output.m_building     != 0) objectBuffer.Add(new InstanceID() { Building   = output.m_building     });

            objectBuffer.Sort((a, b) => Vector3.Distance(a.Position(), mouseCurrentPosition).CompareTo(Vector3.Distance(b.Position(), mouseCurrentPosition)));
            if (objectBuffer.Count > 0)
            {
                hoveredObject = objectBuffer[0];
            }
            else
            {
                hoveredObject = InstanceID.Empty;
            }

            // A prefab has been selected. Find it in the UI and enable it.
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
            {
                if (hoveredObject.Info() == null)
                {
                    enabled = false;
                    ToolsModifierControl.SetTool<DefaultTool>();
                    return;
                }
                ShowInPanelResolveGrowables(DefaultPrefab(hoveredObject.Info()));
            }

            // Escape key or RMB hit = disable the tool
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                enabled = false;
                ToolsModifierControl.SetTool<DefaultTool>();
            }
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            base.RenderOverlay(cameraInfo);
            if (!enabled) return;

            if (hoveredObject.NetSegment != 0)
            {
                NetSegment hoveredSegment = hoveredObject.NetSegment.S();
                NetTool.RenderOverlay(cameraInfo, ref hoveredSegment, hoverColor, new Color(1f, 0f, 0f, 1f));
            }
            else if (hoveredObject.NetNode != 0 && hoveredObject.NetNode < 32768)
            {
                NetNode hoveredNode = hoveredObject.NetNode.N();
                RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, hoverColor, hoveredNode.m_position, Mathf.Max(6f, hoveredNode.Info.m_halfWidth * 2f), -1f, 1280f, false, true);
            }
            else if (hoveredObject.Building != 0)
            {
                Building hoveredBuilding = hoveredObject.Building.B();
                BuildingTool.RenderOverlay(cameraInfo, ref hoveredBuilding, hoverColor, hoverColor);
                
                while (hoveredBuilding.m_subBuilding > 0)
                {
                    hoveredBuilding = BuildingManager.instance.m_buildings.m_buffer[hoveredBuilding.m_subBuilding];
                    BuildingTool.RenderOverlay(cameraInfo, ref hoveredBuilding, hoverColor, hoverColor);
                }
            }
            else if (hoveredObject.Tree != 0)
            {
                TreeInstance hoveredTree = hoveredObject.Tree.T();
                TreeTool.RenderOverlay(cameraInfo, hoveredTree.Info, hoveredTree.Position, hoveredTree.Info.m_minScale, hoverColor);
            }
            else if (hoveredObject.Prop != 0)
            {
                PropInstance hoveredProp = hoveredObject.Prop.P();
                PropTool.RenderOverlay(cameraInfo, hoveredProp.Info, hoveredProp.Position, hoveredProp.Info.m_minScale, hoveredProp.Angle, hoverColor);
            }
        }
    }
}

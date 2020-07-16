using ColossalFramework.Globalization;
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
        public List<InstanceID> stepOverBuffer = new List<InstanceID>();
        public int stepOverCounter = 0;
        public Vector3 stepOverPosition = Vector3.zero;
        public Vector3 mouseCurrentPosition = Vector3.zero;

        protected override void Awake()
        {
            m_toolController = FindObjectOfType<ToolController>();
            enabled = false;

            m_button = UIView.GetAView().AddUIComponent(typeof(UIPickerButton)) as UIPickerButton;
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
        
        private bool ReflectIntoFindIt(PrefabInfo info)
        {
            Type FindItType = Type.GetType("FindIt.FindIt, FindIt");
            object FindItInstance = FindItType.GetField("instance").GetValue(null);
            Type SearchBoxType = Type.GetType("FindIt.GUI.UISearchBox, FindIt");
            object UISearchbox = FindItType.GetField("searchBox").GetValue(FindItInstance);
            object UIScrollPanel = FindItType.GetField("scrollPanel").GetValue(FindItInstance);
            Type ScrollPanelType = Type.GetType("FindIt.GUI.UIScrollPanel, FindIt");
            Debug.Log(ScrollPanelType);

            // Get all the item data...
            PropertyInfo iData = ScrollPanelType.GetProperty("itemsData");
            object itemsData = iData.GetValue(UIScrollPanel, null);
            object[] itemDataBuffer = itemsData.GetType().GetMethod("ToArray").Invoke(itemsData, null) as object[];
            Debug.Log(itemDataBuffer);

            FieldInfo checkBoxType = SearchBoxType.GetField(info.m_isCustomContent ? "workshopFilter" : "vanillaFilter");
            if (checkBoxType != null)
            {
                UICheckBox checkBox = (UICheckBox)checkBoxType.GetValue(UISearchbox);
                checkBox.isChecked = true;
            }
            
            for (int i = 0; i < itemDataBuffer.Length; i++)
            {
                object itemData = itemDataBuffer[i];

                // Get the actual asset data of this prefab instance in the Find It scrollable panel
                Type UIScrollPanelItemData = itemData.GetType();
                Debug.Log(UIScrollPanelItemData);
                object itemData_currentData_asset = UIScrollPanelItemData.GetField("asset").GetValue(itemData);
                PrefabInfo itemData_currentData_asset_info = itemData_currentData_asset.GetType().GetProperty("prefab").GetValue(itemData_currentData_asset, null) as PrefabInfo;
                //string itemDataName = itemData_currentData_asset.GetType().GetField("name").GetValue(itemData_currentData_asset) as string;

                // Display data at this position. Return.
                if (itemData_currentData_asset_info != null && itemData_currentData_asset_info.name == info.name)
                {
                    Debug.Log("Found data at position " + i + " in Find it ScrollablePanel");
                    ScrollPanelType.GetMethod("DisplayAt").Invoke(UIScrollPanel, new object[] { i });

                    string itemDataName = UIScrollPanelItemData.GetField("name").GetValue(itemData) as string;
                    Debug.Log(itemDataName);
                    UIComponent test = UIScrollPanel as UIComponent;
                    UIButton[] fYou = test.GetComponentsInChildren<UIButton>();
                    foreach (UIButton mhmBaby in fYou)
                    {
                        if (mhmBaby.name == itemDataName)
                        {
                            mhmBaby.SimulateClick();
                            return true;
                        }
                    }
                    break;
                }
            }

            return false;
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

            if (pInfo is PropInfo propInfo)
            {
                UIDropDown FilterDropdown = SearchBoxPanel.Find<UIDropDown>("UIDropDown");
                if (propInfo.m_isDecal)
                    FilterDropdown.selectedValue = "Decal";
                else
                    FilterDropdown.selectedValue = "Prop";

                UITextField TextField = SearchBoxPanel.Find<UITextField>("UITextField");
                TextField.text = "";

                if (!propInfo.m_isDecal)
                {
                    UIComponent UIFilterGrowable = SearchBoxPanel.Find("UIFilterProp");
                    UIFilterGrowable.GetComponentInChildren<UIButton>().SimulateClick();
                }

                if (!ReflectIntoFindIt(propInfo))
                {
                    Debug.Log("Could not be found in Prop or Decal menus.");
                }
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

                UIDropDown FilterDropdown = SearchBoxPanel.Find<UIDropDown>("UIDropDown");
                FilterDropdown.selectedValue = "Growable";

                UITextField TextField = SearchBoxPanel.Find<UITextField>("UITextField");
                TextField.text = "";

                UIComponent UIFilterGrowable = SearchBoxPanel.Find("UIFilterGrowable");
                UIFilterGrowable.GetComponentInChildren<UIButton>().SimulateClick();

                // Reflect into the scroll panel, starting with the growable panel:
                if (!ReflectIntoFindIt(info))
                {
                    // And then if that fails, RICO:
                    FilterDropdown.selectedValue = "Rico";
                    if (!ReflectIntoFindIt(info))
                    {
                        // And then if that fails, give up and get a drink
                        Debug.Log("Could not be found in Growable or Rico menus.");
                    }
                }
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
                menuTabstrip.selectedIndex = menuTabstripIndex;
                menuTabstrip.ShowTab(menuTabstrip.tabs[menuTabstripIndex].name);
                subMenuTabstrip.selectedIndex = subMenuTabstripIndex;
                subMenuTabstrip.ShowTab(subMenuTabstrip.tabs[subMenuTabstripIndex].name);
                button.SimulateClick();
                scrollablePanel.ScrollIntoView(button);
            }
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
                    return prefab;
                }
            }
            return info;
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
            //Debug.Log($"Pos: {mouseCurrentPosition}");

            // Step Over Block.
            //if(Input.GetKeyDown(KeyCode.O)) // @TODO allow user to customize this.
            //{
            //    Debug.Log("Attempting to step over");
            //    stepOverCounter++;

            //    if(stepOverCounter == 1 && hasSteppedOver == false) // Only will be executed the first time we reach 1.
            //    {
            //        // @TODO populate step over buffer
            //    }

            //    hasSteppedOver = true;

            //    if (stepOverCounter >= stepOverBuffer.Count) stepOverCounter = 0;
            //    hoveredObject = stepOverBuffer[stepOverCounter];
            //}

            // This code is used when the step over function is not active. It will choose the closest of any given object and set it as the hovered object.
            if (!hasSteppedOver)
            {
                stepOverBuffer.Clear();

                if (output.m_netSegment   != 0) stepOverBuffer.Add(new InstanceID() { NetSegment = output.m_netSegment   });
                if (output.m_netNode      != 0) stepOverBuffer.Add(new InstanceID() { NetNode    = output.m_netNode      });
                if (output.m_treeInstance != 0) stepOverBuffer.Add(new InstanceID() { Tree       = output.m_treeInstance });
                if (output.m_propInstance != 0) stepOverBuffer.Add(new InstanceID() { Prop       = output.m_propInstance });
                if (output.m_building     != 0) stepOverBuffer.Add(new InstanceID() { Building   = output.m_building     });

                stepOverBuffer.Sort((a, b) => Vector3.Distance(a.Position(), mouseCurrentPosition).CompareTo(Vector3.Distance(b.Position(), mouseCurrentPosition)));
                if (stepOverBuffer.Count > 0) hoveredObject = stepOverBuffer[0];
                else hoveredObject = InstanceID.Empty;
            }
            else
            {
                // This function resets the step over function.
                if (mouseCurrentPosition != stepOverPosition)
                {
                    hasSteppedOver = false;
                }
            }
            
            // A prefab has been selected. Find it in the UI and enable it.
            if(Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
            {
                ShowInPanelResolveGrowables(DefaultPrefab(hoveredObject.Info()));
            }

            // Escape key hit = disable the tool
            if (Input.GetKeyDown(KeyCode.Escape))
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

using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Picker
{
    public class FindItManager : MonoBehaviour
    {
        Type FindItType, ScrollPanelType;
        object FindItInstance, ScrollPanel;

        private bool useRICO;

        // Presently dependant on FindIt version
        public static Dictionary<String, int> MenuIndex = new Dictionary<string, int>
        {
            ["All"] = 0,
            ["Growable"] = 3,
            ["RICO"] = 4,
            ["Prop"] = 4,
            ["Decal"] = 5
        };

        private UIComponent _Searchbox = null;
        internal UIComponent Searchbox
        {
            get
            {
                if (_Searchbox == null)
                {
                    UIComponent searchBoxPanel = UIView.Find("UISearchBox");
                    if (searchBoxPanel == null)
                    {
                        Debug.Log($"Find It not found");
                        return null;
                    }
                    _Searchbox = searchBoxPanel;
                }
                return _Searchbox;
            }
        }

        private UIDropDown _filterDropdown = null;
        internal UIDropDown FilterDropdown
        {
            get
            {
                if (_filterDropdown == null)
                {
                    UIDropDown filterDropdown = Searchbox.Find<UIDropDown>("UIDropDown");
                    if (filterDropdown == null)
                    {
                        Debug.Log($"Find It filters not found");
                        return null;
                    }
                    _filterDropdown = filterDropdown;
                }
                return _filterDropdown;
            }
        }

        public void Awake()
        {
            FindItType = Type.GetType("FindIt.FindIt, FindIt");
            FindItInstance = FindItType.GetField("instance").GetValue(null);
            //SearchBoxType = Type.GetType("FindIt.GUI.UISearchBox, FindIt");
            //UISearchbox = FindItType.GetField("searchBox").GetValue(FindItInstance);

            useRICO = Picker.IsRicoEnabled();

            if (useRICO)
            { // Will need changed if/when Find It 2 adds more filter types
                MenuIndex["Prop"] += Picker.FindItVersion;
                MenuIndex["Decal"] += Picker.FindItVersion;
            }
        }

        internal void SetDropdown(string entry)
        {
            //Debug.Log($"Dropdown: {entry}");
            FilterDropdown.selectedIndex = MenuIndex[entry];

            StartCoroutine(ClearFilters(entry));
        }

        private IEnumerator<object> ClearFilters(string entry)
        {
            yield return new WaitForSeconds(0.05f);
            if (Picker.FindItVersion == 1)
            {
                if (entry == "Growable")
                {
                    UIComponent UIFilterGrowable = Searchbox.Find("UIFilterGrowable");
                    UIFilterGrowable.GetComponentInChildren<UIButton>().SimulateClick();
                }
            }
            else if (Picker.FindItVersion == 2)
            {
                MethodInfo resetFilters = Searchbox.GetType().GetMethod("ResetFilters");
                resetFilters.Invoke(Searchbox, null);
            }
            else
            {
                throw new Exception($"Find It called but not available (version:{Picker.FindItVersion})!");
            }
        }

        internal void Find(PrefabInfo info)
        {
            // Turn on workshop or vanilla filter (Find It 2 only)
            //FieldInfo checkBoxType = SearchBoxType.GetField(info.m_isCustomContent ? "workshopFilter" : "vanillaFilter");
            //if (checkBoxType != null)
            //{
            //    UICheckBox checkBox = (UICheckBox)checkBoxType.GetValue(UISearchbox);
            //    checkBox.isChecked = true;
            //}

            if (Searchbox == null)
                return;

            // Open Find It
            if (!Searchbox.isVisible)
            {
                UIButton FIButton = UIView.Find<UIButton>("FindItMainButton");
                if (FIButton == null) return;
                FIButton.SimulateClick();
            }

            StartCoroutine(FindProcess(info, false));
        }

        private IEnumerator<object> FindProcess(PrefabInfo info, bool tryingPRICO)
        {
            yield return new WaitForSeconds(0.05f);

            bool found = false;
            ScrollPanelType = Type.GetType("FindIt.GUI.UIScrollPanel, FindIt");
            ScrollPanel = FindItType.GetField("scrollPanel").GetValue(FindItInstance);

            // Get all the item data...
            PropertyInfo iData = ScrollPanelType.GetProperty("itemsData");
            object itemsData = iData.GetValue(ScrollPanel, null);
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
                    //Debug.Log("Found data at position " + i + " in Find it ScrollablePanel");
                    ScrollPanelType.GetMethod("DisplayAt").Invoke(ScrollPanel, new object[] { i });

                    string itemDataName = UIScrollPanelItemData.GetField("name").GetValue(itemData) as string;
                    UIComponent test = ScrollPanel as UIComponent;
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
                    SetDropdown("RICO");
                    StartCoroutine(FindProcess(info, true));
                }
                else
                {
                    Debug.Log($"Object {info.name} not found");
                }
            }
        }
    }
}

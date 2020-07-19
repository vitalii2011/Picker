using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using System;
using System.Reflection;
using UnityEngine;

// Based on Move It's code
namespace Picker
{
    public class UIPickerButton : UIButton
    {
        public static readonly SavedInt savedX = new SavedInt("savedX", Picker.settingsFileName, -1000, true);
        public static readonly SavedInt savedY = new SavedInt("savedY", Picker.settingsFileName, -1000, true);
        
        private UIComponent BulldoserButton
        {
            get
            {
                UIComponent bulldoserButton = GetUIView().FindUIComponent<UIComponent>("MarqueeBulldozer");

                if (bulldoserButton == null)
                {
                    bulldoserButton = GetUIView().FindUIComponent<UIComponent>("BulldozerButton");
                }
                return bulldoserButton;
            }
        }

        public override void Start()
        {
            LoadResources();

            name = "Picker";

            normalFgSprite = "Picker";
            hoveredFgSprite = "Picker_hover";

            playAudioEvents = true;

            size = new Vector2(43, 49);

            if (savedX.value == -1000)
            {
                absolutePosition = new Vector2(BulldoserButton.absolutePosition.x - (width * 2) - 6, BulldoserButton.parent.absolutePosition.y);
            }
            else
            {
                absolutePosition = new Vector2(savedX.value, savedY.value);
            }
        }

        protected override void OnClick(UIMouseEventParameter p)
        {
            //string msg;
            //msg = $"Assemblies:";
            //foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            //{
            //    msg += $"\n{assembly.GetName().Name.ToLower()}\n - {assembly.FullName.Substring(0, 50)}";
            //}
            //Debug.Log(msg);

            //msg = "Plugins:";
            //foreach (PluginManager.PluginInfo pi in PluginManager.instance.GetPluginsInfo())
            //{
            //    msg += $"\n{pi.publishedFileID.AsUInt64} - {pi.name} ({pi.isEnabled})" +
            //        $"\n - {pi.modPath}";
            //}
            //Debug.Log(msg);

            Debug.Log($"Find It: v{Picker.FindItVersion}");

            if (p.buttons.IsFlagSet(UIMouseButton.Left) && PickerTool.instance != null)
            {
                PickerTool.instance.enabled = !PickerTool.instance.enabled;
            }
        }

        public void ResetPosition()
        {
            absolutePosition = new Vector2(BulldoserButton.absolutePosition.x - (width * 2) - 6, BulldoserButton.parent.absolutePosition.y);
        }

        private Vector3 m_deltaPos;
        protected override void OnMouseDown(UIMouseEventParameter p)
        {
            if (p.buttons.IsFlagSet(UIMouseButton.Right))
            {
                Vector3 mousePos = Input.mousePosition;
                mousePos.y = m_OwnerView.fixedHeight - mousePos.y;

                m_deltaPos = absolutePosition - mousePos;
                BringToFront();
            }
        }

        protected override void OnMouseMove(UIMouseEventParameter p)
        {
            if (p.buttons.IsFlagSet(UIMouseButton.Right))
            {
                Vector3 mousePos = Input.mousePosition;
                mousePos.y = m_OwnerView.fixedHeight - mousePos.y;

                absolutePosition = mousePos + m_deltaPos;
                savedX.value = (int)absolutePosition.x;
                savedY.value = (int)absolutePosition.y;
            }
        }

        public override void Update()
        {
            if (PickerTool.instance != null && PickerTool.instance.enabled)
            {
                normalFgSprite = "Picker_focused";
            }
            else
            {
                normalFgSprite = "Picker";
            }
        }

        public void OnGUI()
        {
            if (!UIView.HasModalInput() && !UIView.HasInputFocus() && OptionsKeymapping.toggleTool.IsPressed(Event.current))
            {
                bool useMoveIt = false;
                // Check for Move It hovered item
                if (Picker.MoveItVersion > 0)
                {
                    useMoveIt = PickerTool.instance.ReflectIntoMoveIt();
                }
                if (!useMoveIt)
                {
                    SimulateClick();
                }
            }
        }

        private void LoadResources()
        {
            string[] spriteNames = new string[]
            {
                "Picker",
                "Picker_focused",
                "Picker_hover"
            };

            atlas = GetAtlas(spriteNames);
        }

        internal static UITextureAtlas GetAtlas(string[] spriteNames)
        {
            return ResourceLoader.CreateTextureAtlas("Picker", spriteNames, "Picker.Icons.");
        }
    }
}

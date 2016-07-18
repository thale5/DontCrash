using System;
using System.IO;
using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Packaging;
using ColossalFramework.Steamworks;
using ColossalFramework.UI;
using UnityEngine;

namespace DontCrash
{
    public sealed class LazyEntry : UIPanel
    {
        public const int HEIGHT = 132;
        const float C0 = 10f, C1 = 222f, C2 = 500f, C3 = 720f, C4 = 810f, C5 = 904f;
        const float R0 = 8f, R1 = 13f, R2 = 106f;
        public UILabel title, updated, tags, count;
        public UICheckBox active;
        public UIButton share, delete, view;
        public UITextureSprite image;
        public UICheckboxDropDown styles;
        public LazyData data;

        public override void Awake()
        {
            base.Awake();
            size = new Vector2(946f, HEIGHT);
            backgroundSprite = "ContentManagerItemBackground";
            CreateTitle();
            CreateActive();
            CreateShare();
            CreateDelete();
            CreateView();
            CreateUpdated();
            CreateTags();
            CreateImage();
            CreateStyles();
            CreateCount();
        }

        public void ResetData()
        {
            styles.ClosePopup();

            if (data != null)
            {
                if (data.lacksAD || data.lacksSF)
                {
                    active.isEnabled = true;
                    active.text = Locale.Get("CONTENT_ONOFF");
                }

                data.entry = null;
                data = null;
            }
        }

        public void CreateTitle()
        {
            title = AddUIComponent<UILabel>();
            title.relativePosition = new Vector3(C1, R1, 0f);
            title.size = new Vector2(C2 - C1, 35f);
        }

        public void CreateActive()
        {
            active = AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsCheckBoxTemplate")) as UICheckBox;
            active.relativePosition = new Vector3(C1, R1 + 41f, 0f);
            active.size = new Vector2(94f, 21f);
            active.label.textScale = 1f;
            active.text = Locale.Get("CONTENT_ONOFF");
            ((UISprite) active.checkedBoxObject).spriteName = "ToggleBaseFocused";

            for (int i = 0; i < active.components.Count; i++)
                if (active.components[i].cachedName == "Unchecked")
                    ((UISprite) active.components[i]).spriteName = "ToggleBase";

            active.canFocus = false;
            active.eventCheckChanged += OnCheckChanged;
        }

        public void CreateShare()
        {
            share = AddUIComponent<UIButton>();
            share.canFocus = false;
            share.relativePosition = new Vector3(C1, R1 + 75f, 0f);
            share.size = new Vector2(123f, 32f);
            share.hoveredTextColor = new Color32(7, 132, 255, 255);
            share.pressedTextColor = new Color32(30, 30, 44, 255);
            share.eventMouseEnter += OnButtonHovered;
            share.eventClick += OnShare;
        }

        public void CreateDelete()
        {
            delete = AddUIComponent<UIButton>();
            delete.canFocus = false;
            delete.relativePosition = new Vector3(C5, 47f, 0f);
            delete.size = new Vector2(32f, 32f);
            delete.normalFgSprite = "buttonclose";
            delete.hoveredFgSprite = "buttonclosehover";
            delete.pressedFgSprite = "buttonclosepressed";
            delete.eventClick += OnDelete;
        }

        public void CreateView()
        {
            view = AddUIComponent<UIButton>();
            view.canFocus = false;
            view.relativePosition = new Vector3(C4, 37f, 0f);
            view.size = new Vector2(52f, 52f);
            view.normalBgSprite = "WorkshopButton";
            view.hoveredBgSprite = "WorkshopButtonHovered";
            view.pressedBgSprite = "WorkshopButtonPressed";
            view.eventClick += OnView;
        }

        public void CreateUpdated()
        {
            updated = AddUIComponent<UILabel>();
            updated.relativePosition = new Vector3(C3, R2, 0f);
            updated.size = new Vector2(220f, 18f);
        }

        public void CreateTags()
        {
            tags = AddUIComponent<UILabel>();
            tags.relativePosition = new Vector3(C2, R2, 0f);
            tags.size = new Vector2(200f, 18f);
            tags.textColor = new Color32(80, 134, 154, 255);
        }

        public void CreateImage()
        {
            image = AddUIComponent<UITextureSprite>();
            image.relativePosition = new Vector3(C0, R0, 0f);
            image.size = new Vector2(200f, 112f);
        }

        public void CreateStyles()
        {
            styles = AddUIComponent<UICheckboxDropDown>();
            styles.canFocus = false;
            styles.listBackground = "OptionsDropboxListbox";
            styles.relativePosition = new Vector3(C2, R1 + 31f, 0f);
            styles.size = new Vector2(200f, 27f);
            styles.popupColor = new Color32(255, 255, 255, 255);
            styles.itemHover = "ListItemHover";
            styles.uncheckedSprite = "AchievementCheckedFalse";
            styles.checkedSprite = "AchievementCheckedTrue";
            styles.builtinKeyNavigation = false;
            styles.listHeight = 800;
            styles.itemHeight = 22;
            styles.textFieldPadding = new RectOffset(10, 0, 4, 0);
            styles.itemPadding = new RectOffset(2, 2, 2, 2);
            styles.listPadding = new RectOffset(4, 4, 4, 4);
            styles.eventDropdownOpen += OnDropDownOpened;

            UIButton trigger = styles.AddUIComponent<UIButton>();
            styles.triggerButton = trigger;
            trigger.canFocus = false;
            trigger.relativePosition = Vector3.zero;
            trigger.size = styles.size;
            trigger.textColor = new Color32(192, 176, 96, 255);
            trigger.normalBgSprite = "CMStylesDropbox";
            trigger.hoveredBgSprite = "CMStylesDropboxHovered";
            trigger.localeID = "CONTENTMANAGER_ADDTOSTYLE";
        }

        public void CreateCount()
        {
            count = AddUIComponent<UILabel>();
            count.relativePosition = new Vector3(C2, R1, 0f);
            count.size = new Vector2(200f, 23f);
            count.textColor = new Color32(192, 176, 96, 255);
        }

        void OnCheckChanged(UIComponent c, bool isChecked)
        {
            if (data != null && data.asset.isEnabled != isChecked)
            {
                data.asset.isEnabled = isChecked;
                LazyContainer.instance.redraw = true;
            }
        }

        void OnButtonHovered(UIComponent c, UIMouseEventParameter param)
        {
            data?.OnHovered();
        }

        void OnShare(UIComponent component, UIMouseEventParameter p)
        {
            if (data != null && data.owned == OWNED.YES)
            {
                WorkshopAssetUploadPanel sharePanel = UIView.library.ShowModal<WorkshopAssetUploadPanel>("WorkshopAssetUploadPanel");
                sharePanel?.SetAsset(data.asset, data.PackageId);
            }
        }

        void OnDelete(UIComponent component, UIMouseEventParameter param)
        {
            if (data != null)
                if (data.IsWorkshop)
                    ConfirmPanel.ShowModal("CONTENT_CONFIRM_WORKSHOPDELETE", delegate (UIComponent comp, int ret)
                    {
                        if (ret == 1)
                            Steam.workshop.Unsubscribe(data.PackageId);
                    });
                else
                    ConfirmPanel.ShowModal("CONTENT_CONFIRM_DELETE", delegate (UIComponent comp, int ret)
                    {
                        if (ret == 1)
                        {
                            try
                            {
                                File.Delete(data.asset.package.packagePath);
                                LazyContainer.instance.OnDeleted(data);
                            }
                            catch (Exception ex)
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "An error occurred while deleting asset" + ex.ToString());
                            }
                        }
                    });
        }

        void OnView(UIComponent c, UIMouseEventParameter param)
        {
            if (data != null && data.IsWorkshop && Steam.IsOverlayEnabled())
                Steam.ActivateGameOverlayToWorkshopItem(data.PackageId);
        }

        void OnDropDownOpened(UICheckboxDropDown dropdown, UIScrollablePanel popup, ref bool overridden)
        {
            styles.eventDropdownClose -= OnDropDownClosed;
            styles.eventDropdownClose += OnDropDownClosed;
        }

        void OnDropDownClosed(UICheckboxDropDown dropdown, UIScrollablePanel popup, ref bool overridden)
        {
            bool changes = false;
            styles.eventDropdownClose -= OnDropDownClosed;

            if (data == null)
                return;

            try
            {
                PackageManager.DisableEvents();
                string a = data.asset.package.packageName + "." + data.asset.name;

                for (int i = 0; i < styles.items.Length; i++)
                {
                    Package.Asset style = (Package.Asset) styles.GetItemUserData(i);
                    DistrictStyleMetaData styleMetaData = style.Instantiate<DistrictStyleMetaData>();

                    if (styleMetaData.assets == null)
                        styleMetaData.assets = new string[0];

                    int j = Array.IndexOf(styleMetaData.assets, a);

                    if (styles.GetChecked(i))
                    {
                        if (j < 0)
                        {
                            string[] array = new string[styleMetaData.assets.Length + 1];
                            styleMetaData.assets.CopyTo(array, 0);
                            array[array.Length - 1] = a;
                            styleMetaData.assets = array;
                            StylesHelper.SaveStyle(styleMetaData, style.name, style.isWorkshopAsset, null);
                            changes = true;
                        }
                    }
                    else if (j >= 0)
                    {
                        styleMetaData.assets = styleMetaData.assets.RemoveAt(j);
                        StylesHelper.SaveStyle(styleMetaData, style.name, style.isWorkshopAsset, null);
                        changes = true;
                    }
                }
            }
            catch (Exception ex)
            {
                CODebugBase<LogChannel>.Error(LogChannel.Modding, string.Concat("Style package saving failed: ", ex.GetType(), " ", ex.Message, " ", ex.StackTrace));
            }
            finally
            {
                PackageManager.EnabledEvents();

                if (changes)
                    PackageManager.ForcePackagesChanged();
            }
        }

        void Remove<T>(ref T c) where T : UIComponent
        {
            if (c != null)
            {
                RemoveUIComponent(c);
                UnityEngine.Object.Destroy(c.gameObject);
            }

            c = null;
        }
    }
}

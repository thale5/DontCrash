using System;
using System.Collections.Generic;
using System.IO;
using ColossalFramework.Globalization;
using ColossalFramework.Packaging;
using ColossalFramework.Steamworks;
using ColossalFramework.UI;
using UnityEngine;

namespace DontCrash
{
    public sealed class LazyData
    {
        public Package.Asset asset;
        string name, author = string.Empty, tags = string.Empty;
        PublishedFileId packageId = new PublishedFileId(0);
        public int num;
        public OWNED owned = OWNED.NOT_KNOWN;
        uint timeUpdated, timeSubscribed;
        public LazyEntry entry;
        public Texture texture;
        public bool hasMetaData, hasStyles, lacksAD, lacksSF;
        public string Name => string.IsNullOrEmpty(name) ? asset.name : name;
        public bool IsWorkshop => PackageId != PublishedFileId.invalid;

        public PublishedFileId PackageId
        {
            get
            {
                if (packageId.AsUInt64 == 0uL)
                {
                    if (asset.package.packagePath.Contains("255710"))
                        packageId = Package.GetPublishedFileID(asset.package);
                    else
                        packageId = PublishedFileId.invalid;
                }

                return packageId;
            }
        }

        public uint TimeUpdated
        {
            get
            {
                if (timeUpdated == 0)
                    timeUpdated = LocalTimeUpdated();

                return timeUpdated;
            }
        }

        public uint TimeSubscribed
        {
            get
            {
                if (timeSubscribed == 0)
                    timeSubscribed = LocalTimeCreated();

                return timeSubscribed;
            }
        }

        public LazyData(Package.Asset asset, int num)
        {
            this.asset = asset;
            this.num = num;
        }

        public void SetVisible(LazyEntry e)
        {
            entry = e;
            e.data = this;
            SetTitle();
            SetActive();
            SetShare();
            SetDelete();
            SetView();
            SetUpdated();
            SetTags();
            SetImage();
            SetStyles();
            e.Show();

            if (owned == OWNED.NOT_KNOWN && UIInput.hoveredComponent == e.share)
                OnHovered();
        }

        public void SetTitle()
        {
            entry.title.text = string.IsNullOrEmpty(author) ? Name : string.Concat(Name, "\nby ", author);
            entry.title.tooltip = asset.package.packagePath;
        }

        public void SetActive()
        {
            if (lacksAD || lacksSF)
            {
                entry.active.isEnabled = false;
                entry.active.text = lacksAD ? Locale.Get("CONTENT_AD_REQUIRED") : Locale.Get("CONTENT_SF_REQUIRED");
            }

            entry.active.isChecked = asset.isEnabled;
        }

        public void SetShare()
        {
            if (owned == OWNED.NOT_KNOWN || owned == OWNED.QUERIED)
            {
                entry.share.normalBgSprite = string.Empty;
                entry.share.localeID = string.Empty;
                entry.share.text = "?";
                entry.share.Show();
            }
            else if (owned == OWNED.NO)
                entry.share.Hide();
            else
            {
                entry.share.normalBgSprite = "ButtonMenu";
                entry.share.localeID = IsWorkshop ? "CONTENT_UPDATE" : "CONTENT_SHARE";
                entry.share.Show();
            }
        }

        public void SetDelete()
        {
            if (owned == OWNED.NOT_KNOWN || owned == OWNED.QUERIED)
                entry.delete.Hide();
            else
                entry.delete.Show();
        }

        public void SetView()
        {
            if (owned == OWNED.NOT_KNOWN || owned == OWNED.QUERIED || !IsWorkshop)
                entry.view.Hide();
            else
                entry.view.Show();
        }

        public void SetUpdated()
        {
            if (timeUpdated == 0)
                entry.updated.Hide();
            else
            {
                entry.updated.text = SmartDateString(timeUpdated);
                entry.updated.Show();
            }
        }

        public void SetTags()
        {
            if (string.IsNullOrEmpty(tags))
                entry.tags.Hide();
            else
            {
                entry.tags.text = tags;
                entry.tags.Show();
            }
        }

        public void SetImage()
        {
            if (texture == null)
                entry.image.Hide();
            else
            {
                entry.image.texture = texture;
                entry.image.texture.wrapMode = TextureWrapMode.Clamp;
                entry.image.Show();
            }
        }

        public void SetStyles()
        {
            if (!hasStyles)
            {
                entry.styles.Hide();
                entry.count.Hide();
            }
            else
            {
                entry.styles.Clear();
                string a = asset.package.packageName + "." + asset.name;
                int cnt = 0;
                StyleData[] data = LazyContainer.instance.GetStyleData();

                for (int i = 0; i < data.Length; i++)
                {
                    bool isChecked = data[i].assets.Contains(a);

                    if (isChecked)
                        cnt++;

                    entry.styles.AddItem(data[i].name, isChecked, data[i].style);
                }

                entry.styles.Show();

                if (cnt == 0)
                    entry.count.Hide();
                else
                {
                    entry.count.text = string.Format(Locale.Get("CONTENTMANAGER_ASSET_STYLECOUNT"), cnt);
                    entry.count.Show();
                }
            }
        }

        public void OnHovered()
        {
            if (owned == OWNED.NOT_KNOWN || owned == OWNED.QUERIED)
                if (IsWorkshop)
                    LazyContainer.instance.SteamQuery(packageId, this);
                else
                {
                    timeUpdated = TimeUpdated;
                    owned = OWNED.YES;
                    SetSteamData();
                }
        }

        public void OnSteamQueryCompleted(string author, uint timeUpdated, bool owned)
        {
            this.author = author;
            this.timeUpdated = timeUpdated;
            this.owned = owned ? OWNED.YES : OWNED.NO;
            SetSteamData();
        }

        public void SetSteamData()
        {
            if (entry != null)
            {
                SetTitle();
                SetUpdated();
                SetDelete();
                SetView();
                SetShare();
            }
        }

        public void SetVisibleExtended()
        {
            if (!hasMetaData && entry != null)
            {
                InstantiateMetaData();
                SetTitle();
                SetTags();
                SetImage();
                SetStyles();

                if (lacksAD || lacksSF)
                    SetActive();
            }
        }

        public void InstantiateMetaData()
        {
            if (!hasMetaData)
            {
                hasMetaData = true;
                CustomAssetMetaData metadata = asset.Instantiate<CustomAssetMetaData>();

                if (metadata != null)
                {
                    if (metadata.steamTags != null && metadata.steamTags.Length > 0)
                    {
                        string[] s = metadata.steamTags;
                        string s0 = s[0];

                        if (s.Length >= 3 && s[2].StartsWith(s[1]))
                            s = s.RemoveAt(1);

                        if (s.Length >= 2 && s0 == "Building")
                            s = s.RemoveAt(0);

                        tags = string.Join(", ", s);
                        int w = metadata.width, d = metadata.length;

                        if (w > 0 && d > 0)
                        {
                            int l = (int) metadata.level + 1;

                            if (s0 == "Building" && l > 0)
                                tags += " L" + l;

                            if (s0 == "Park" || s0 == "Building")
                                tags += " " + w + "x" + d;
                        }
                    }

                    name = metadata.name;

                    if (metadata.imageRef != null)
                        texture = metadata.imageRef.Instantiate<Texture2D>();
                    else
                    {
                        WorkshopAssetUploadPanel sharePanel = UIView.library.Get<WorkshopAssetUploadPanel>("WorkshopAssetUploadPanel");
                        texture = UnityEngine.Object.Instantiate<Texture>(sharePanel.m_DefaultAssetPreviewTexture);
                    }

                    SteamHelper.DLC_BitMask assetMask = AssetImporterAssetTemplate.GetAssetDLCMask(metadata);
                    lacksAD = (assetMask & SteamHelper.DLC_BitMask.AfterDarkDLC) > (LazyContainer.instance.ownedMask & SteamHelper.DLC_BitMask.AfterDarkDLC);
                    lacksSF = (assetMask & SteamHelper.DLC_BitMask.SnowFallDLC) > (LazyContainer.instance.ownedMask & SteamHelper.DLC_BitMask.SnowFallDLC);

                    // if (IsWorkshop)
                    foreach (string tag in metadata.steamTags)
                        if (tag.Equals("Residential") || tag.Equals("Commercial") || tag.Equals("Industrial") || tag.Equals("Office"))
                        {
                            hasStyles = true;
                            break;
                        }
                }
            }
        }

        public bool IsMatch(string text)
        {
            return string.IsNullOrEmpty(text) || Name.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) != -1;
        }

        uint LocalTimeUpdated()
        {
            try
            {
                string path = asset.package.packagePath;

                if (IsWorkshop)
                    path = Path.GetDirectoryName(path);

                return (uint) (File.GetLastWriteTimeUtc(path) - LazyContainer.instance.epoch).TotalSeconds;
            }
            catch
            {
                return 1;
            }
        }

        uint LocalTimeCreated()
        {
            try
            {
                string path = asset.package.packagePath;

                if (IsWorkshop)
                    path = Path.GetDirectoryName(path);

                return (uint) (File.GetCreationTimeUtc(path) - LazyContainer.instance.epoch).TotalSeconds;
            }
            catch
            {
                return 1;
            }
        }

        public static string FormatTimeSpan(TimeSpan ts)
        {
            double secs = Math.Max(ts.TotalSeconds, 0.0);

            if (secs <= 3600.0)
                return LocaleFormatter.FormatGeneric("CONTENTMANAGER_LASTUPDATEDMINUTES", ts.Minutes.ToString());

            if (secs <= 86400.0)
                return LocaleFormatter.FormatGeneric("CONTENTMANAGER_LASTUPDATEDHOURS", ts.Hours.ToString());

            if (secs <= 2678400.0)
                return LocaleFormatter.FormatGeneric("CONTENTMANAGER_LASTUPDATEDDAYS", ts.Days.ToString());

            int months = (int) (ts.Days / 30.4);
            return LocaleFormatter.FormatGeneric("CONTENTMANAGER_LASTUPDATEDMONTHS", months.ToString());
        }

        public static string SmartDateString(uint secondsUTC)
        {
            DateTime dateTime = LazyContainer.instance.epoch.AddSeconds(secondsUTC).ToLocalTime();
            TimeSpan ts = new TimeSpan(DateTime.Now.Ticks - dateTime.Ticks);
            return LocaleFormatter.FormatGeneric("CONTENTMANAGER_LASTUPDATED", FormatTimeSpan(ts));
        }
    }

    public enum OWNED
    {
        NOT_KNOWN = -2,
        QUERIED = -1,
        NO = 0,
        YES = 1
    }

    public struct StyleData
    {
        public string name;
        public Package.Asset style;
        public HashSet<string> assets;
    }
}

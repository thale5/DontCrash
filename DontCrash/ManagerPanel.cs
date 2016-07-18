using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ColossalFramework.Packaging;
using ColossalFramework.Steamworks;
using ColossalFramework.UI;
using UnityEngine;

namespace DontCrash
{
    public sealed class ManagerPanel : DetourUtility
    {
        public static ManagerPanel instance;
        static readonly Stopwatch stopWatch = new Stopwatch();
        internal static int Millis => (int) stopWatch.ElapsedMilliseconds;
        public const int SCROLLBAR_WIDTH = 24;

        public ManagerPanel()
        {
            instance = this;
            init(typeof(ContentManagerPanel), "RefreshType");
            init(typeof(ContentManagerPanel), "RefreshWorkshopItems");
            stopWatch.Reset(); stopWatch.Start();
        }

        static internal void Setup()
        {
            new PackageManagerFix().Deploy();
            new ManagerPanel().Deploy();

            try
            {
                // Enabled on startup or enabled in Content Manager?
                ContentManagerPanel[] objects = Resources.FindObjectsOfTypeAll<ContentManagerPanel>();

                foreach (var cm in objects)
                    if (cm.isActiveAndEnabled)
                    {
                        CreateContainer(cm, false); // we are in Content Manager
                        break;
                    }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        internal override void Dispose()
        {
            Revert();
            base.Dispose();
            stopWatch.Stop();
            instance = null;
        }

        static void OnSearch(UIComponent component, string text)
        {
            LazyContainer.instance.RefreshSearch(text);
        }

        public static bool IsInitialized(UIComponent container)
        {
            int index = LazyContainer.instance.categoryContainer.components.IndexOf(container.parent.parent);
            return LazyContainer.instance.initialized.IsBitSet(index);
        }

        public static void SetInitialized(UIComponent container)
        {
            int index = LazyContainer.instance.categoryContainer.components.IndexOf(container.parent.parent);
            LazyContainer.instance.initialized = LazyContainer.instance.initialized.SetBit(index);
        }

        static void CreateContainer(ContentManagerPanel cm, bool settab)
        {
            UITabContainer categoryContainer = cm.Find<UITabContainer>("CategoryContainer");
            UIComponent assets = categoryContainer.Find("Assets");
            UIComponent moar = assets.Find("MoarGroup");

            if (moar != null)
            {
                moar.enabled = false; moar.Hide(); moar.gameObject.SetActive(false);
                assets.RemoveUIComponent(moar);
            }

            UIComponent container = assets.Find("Content");
            UIComponent parent = container.parent;

            while (parent.childCount > 0)
            {
                UIComponent c = parent.components[0];
                c.enabled = false; c.Hide(); c.gameObject.SetActive(false);
                parent.RemoveUIComponent(c);
            }

            LazyContainer lazy = parent.AddUIComponent<LazyContainer>();
            lazy.canFocus = true;
            lazy.name = container.name;
            lazy.size = parent.size - new Vector2(SCROLLBAR_WIDTH, 0f);
            lazy.AlignTo(parent, UIAlignAnchor.TopLeft);
            lazy.categoryContainer = categoryContainer;

            lazy.bar = parent.AddUIComponent<UIScrollbar>();
            lazy.bar.orientation = UIOrientation.Vertical;
            lazy.bar.size = new Vector2(SCROLLBAR_WIDTH, parent.height);
            lazy.bar.AlignTo(parent, UIAlignAnchor.TopRight);
            lazy.bar.autoHide = true;
            lazy.bar.canFocus = false;
            lazy.bar.value = 0f;

            UISlicedSprite track = lazy.bar.AddUIComponent<UISlicedSprite>();
            track.fillDirection = UIFillDirection.Vertical;
            track.size = lazy.bar.size;
            track.relativePosition = Vector2.zero;
            track.spriteName = "ScrollbarTrack";

            UISlicedSprite thumb = track.AddUIComponent<UISlicedSprite>();
            thumb.fillDirection = UIFillDirection.Vertical;
            thumb.minimumSize = new Vector2(SCROLLBAR_WIDTH - 4, 16f);
            thumb.width = SCROLLBAR_WIDTH - 4;
            thumb.spriteName = "ScrollbarThumb";

            lazy.bar.trackObject = track;
            lazy.bar.thumbObject = thumb;

            lazy.bar.eventValueChanged += lazy.OnScollChanged;
            parent.eventSizeChanged += lazy.OnParentSizeChanged;
            categoryContainer.eventSelectedIndexChanged += OnTabChanged;
            lazy.OnParentSizeChanged(parent, parent.size);

            if (settab)
                cm.SetCategory(2);

            AddButton(assets, 0f, 102f, "Sort by Name").eventClick += lazy.SortByName;
            AddButton(assets, 106f, 176f, "Sort by Subscription Time").eventClick += lazy.SortBySubscribed;
            AddButton(assets, 286f, 144f, "Sort by Update Time").eventClick += lazy.SortByUpdated;
            AddButton(assets, 434f, 76f, "Query All").eventClick += lazy.QueryAll;

            UITextField search = cm.Find<UITextField>("SearchField");
            search.eventTextChanged += OnSearch;

            assets.Find<UIButton>("EnableAll").eventMouseDown += lazy.OnEnableAll;
            assets.Find<UIButton>("DisableAll").eventMouseDown += lazy.OnDisableAll;
        }

        static UIButton AddButton(UIComponent assets, float x, float dx, string text)
        {
            UIButton sort = assets.AddUIComponent<UIButton>();
            sort.relativePosition = new Vector3(x, 11f, 0f);
            sort.size = new Vector2(dx, 23f);
            sort.textScale = 0.875f;
            sort.pivot = UIPivotPoint.MiddleCenter;
            sort.normalBgSprite = "ButtonMenu";
            sort.hoveredTextColor = new Color32(7, 132, 255, 255);
            sort.pressedTextColor = new Color32(30, 30, 44, 255);
            sort.text = text;
            sort.canFocus = false;
            return sort;
        }

        internal static List<LazyData> CreateData()
        {
            List<LazyData> list = new List<LazyData>(64);

            foreach (Package.Asset asset in PackageManager.FilterAssets(UserAssetType.CustomAssetMetaData))
                if (asset.isMainAsset)
                    list.Add(new LazyData(asset, LazyContainer.instance.NUM++));

            return list;
        }

        internal static void OnTabChanged(UIComponent c, int index)
        {
            UIComponent container = c.components[index].Find("Content");

            if (container != null && !IsInitialized(container))
                Init(container, index);

            if (index == 2)
                LazyContainer.instance.Refresh();
        }

        static void Init(UIComponent container, int index)
        {
            Package.AssetType assetType;
            string template;

            switch (index)
            {
                case 0:
                    assetType = UserAssetType.MapMetaData;
                    template = "MapEntryTemplate";
                    break;
                case 1:
                    assetType = UserAssetType.SaveGameMetaData;
                    template = "SaveEntryTemplate";
                    break;
                case 2:
                    SetInitialized(container);
                    return;
                case 3:
                    assetType = UserAssetType.ColorCorrection;
                    template = "AssetEntryTemplate";
                    break;
                case 4:
                    SetInitialized(container);
                    return;
                case 5:
                    assetType = UserAssetType.DistrictStyleMetaData;
                    template = "StyleEntryTemplate";
                    break;
                case 6:
                    assetType = UserAssetType.MapThemeMetaData;
                    template = "MapThemeEntryTemplate";
                    break;
                case 8:
                    RefreshWorkshopImpl(container);
                    return;
                default:
                    return;
            }

            RefreshTypeImpl(assetType, container, template, false);
        }

        static void RefreshType(ContentManagerPanel cm, Package.AssetType assetType, UIComponent container, string template, bool onlyMain)
        {
            if (LazyContainer.instance == null)
                CreateContainer(cm, true);

            if (assetType == UserAssetType.CustomAssetMetaData)
                LazyContainer.instance.Refresh();
            else if (container.isVisible || IsInitialized(container))
                RefreshTypeImpl(assetType, container, template, onlyMain);
        }

        public static void RefreshTypeImpl(Package.AssetType assetType, UIComponent container, string template, bool onlyMain)
        {
            SetInitialized(container);
            UIScrollablePanel sp = container as UIScrollablePanel;
            bool auto = sp.autoLayout;
            sp.autoLayout = false;
            int num = 0, skip = 0;

            foreach (Package.Asset current in PackageManager.FilterAssets(assetType))
            {
                if (!onlyMain || current.isMainAsset)
                {
                    PackageEntry packageEntry;

                    if (num >= container.components.Count)
                    {
                        packageEntry = UITemplateManager.Get<PackageEntry>(template);
                        container.AttachUIComponent(packageEntry.gameObject);
                    }
                    else
                    {
                        packageEntry = container.components[num].GetComponent<PackageEntry>();

                        if (current == packageEntry.asset && current.package == packageEntry.package)
                        {
                            num++;
                            skip++;
                            continue;
                        }

                        packageEntry.Reset();
                    }

                    packageEntry.entryActive = current.isEnabled;
                    packageEntry.package = current.package;
                    packageEntry.asset = current;
                    packageEntry.entryName = string.Concat(current.package.packageName, ".", current.name, "\t(", current.type, ")");
                    packageEntry.publishedFileId = current.package.GetPublishedFileID();
                    packageEntry.RequestDetails();
                    num++;
                }
            }

            while (container.components.Count > num)
            {
                UIComponent uIComponent = container.components[num];
                container.RemoveUIComponent(uIComponent);
                UnityEngine.Object.Destroy(uIComponent.gameObject);
            }

            sp.autoLayout = auto;
        }

        static void RefreshWorkshopItems(ContentManagerPanel cm)
        {
            if (LazyContainer.instance == null)
                CreateContainer(cm, true);

            UIComponent container = cm.Find<UITabContainer>("CategoryContainer").Find("SteamWorkshop").Find("Content");

            if (container.isVisible || IsInitialized(container))
                RefreshWorkshopImpl(container);
        }

        public static void RefreshWorkshopImpl(UIComponent container)
        {
            SetInitialized(container);
            UIScrollablePanel sp = container as UIScrollablePanel;
            bool auto = sp.autoLayout;
            sp.autoLayout = false;
            PublishedFileId[] subscribedItems = Steam.workshop.GetSubscribedItems();
            int num = 0, skip = 0;

            for (int i = 0; i < subscribedItems.Length; i++)
            {
                PublishedFileId publishedFileId = subscribedItems[i];
                PackageEntry packageEntry;

                if (num >= container.components.Count)
                {
                    packageEntry = UITemplateManager.Get<PackageEntry>("WorkshopEntryTemplate");
                    container.AttachUIComponent(packageEntry.gameObject);
                }
                else
                {
                    packageEntry = container.components[num].GetComponent<PackageEntry>();

                    if (publishedFileId == packageEntry.publishedFileId)
                    {
                        num++;
                        skip++;
                        continue;
                    }

                    packageEntry.Reset();
                }

                packageEntry.entryName = publishedFileId.AsUInt64.ToString();
                packageEntry.entryActive = true;
                packageEntry.publishedFileId = publishedFileId;
                packageEntry.RequestDetails();
                num++;
            }

            while (container.components.Count > num)
            {
                UIComponent uIComponent = container.components[num];
                container.RemoveUIComponent(uIComponent);
                UnityEngine.Object.Destroy(uIComponent.gameObject);
            }

            sp.autoLayout = auto;
        }
    }

    public class LazyContainer : UIComponent
    {
        const int QUERIES = 5;
        public static LazyContainer instance;
        public UITabContainer categoryContainer;
        public UIScrollbar bar;
        public int initialized, NUM;
        Dictionary<PublishedFileId, object> requests = new Dictionary<PublishedFileId, object>();
        HashSet<PublishedFileId> subscribed = new HashSet<PublishedFileId>();
        List<LazyData> dataList;
        LazyData[] lazyData;
        StyleData[] styleData;
        int stableCount, sort;
        string searchString;
        public readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        public SteamHelper.DLC_BitMask ownedMask = SteamHelper.GetOwnedDLCMask();
        public bool redraw, query;
        public int PartialEntries => Mathf.Max(((int) height + LazyEntry.HEIGHT - 1) / LazyEntry.HEIGHT, 1);
        public int FullEntries => Mathf.Max((int) height / LazyEntry.HEIGHT, 1);

        public override void Awake()
        {
            base.Awake();
            instance = this;
            Steam.workshop.eventUGCRequestUGCDetailsCompleted -= OnSteamQueryCompleted;
            Steam.workshop.eventUGCRequestUGCDetailsCompleted += OnSteamQueryCompleted;
            Steam.workshop.eventWorkshopSubscriptionChanged -= OnWorkshopSubscriptionChanged;
            Steam.workshop.eventWorkshopSubscriptionChanged += OnWorkshopSubscriptionChanged;
        }

        public override void Update()
        {
            base.Update();

            if (isVisible)
            {
                if (redraw)
                {
                    redraw = false;
                    stableCount = 4;
                    SetEntries();
                }
                else if (--stableCount == 0)
                    SetStable();

                if (query)
                    Query();
            }
        }

        public void SteamQuery(PublishedFileId id, LazyData d)
        {
            object o;

            if (requests.TryGetValue(id, out o))
            {
                if (o is LazyData)
                    requests[id] = new List<LazyData>(2) { (LazyData) o, d };
                else
                    ((List<LazyData>) o).Add(d);
            }
            else
                requests[id] = d;

            Steam.workshop.RequestItemDetails(id);
            d.owned = OWNED.QUERIED;
        }

        void OnSteamQueryCompleted(UGCDetails result, bool ioError)
        {
            object o;

            if (requests.TryGetValue(result.publishedFileId, out o))
            {
                requests.Remove(result.publishedFileId);
                string author = new Friend(result.creatorID).personaName;
                bool owned = Steam.steamID == result.creatorID;

                if (o is LazyData)
                    ((LazyData) o).OnSteamQueryCompleted(author, result.timeUpdated, owned);
                else
                    foreach (LazyData d in (List<LazyData>) o)
                        d.OnSteamQueryCompleted(author, result.timeUpdated, owned);
            }
        }

        void Query()
        {
            if (requests.Count < 10 * QUERIES)
            {
                int n = 0;

                foreach (LazyData d in GetData())
                {
                    if (d.owned == OWNED.NOT_KNOWN)
                    {
                        d.OnHovered();

                        if (++n == QUERIES)
                            return;
                    }
                }

                query = n > 0;
            }
        }

        void CreateEntries()
        {
            int n = PartialEntries;

            for (int i = childCount; i < n; i++)
            {
                LazyEntry entry = AddUIComponent<LazyEntry>();
                entry.relativePosition = new Vector3(0f, i * LazyEntry.HEIGHT, 0f);
            }

            while (childCount > n)
            {
                LazyEntry c = (LazyEntry) components[n];
                c.ResetData();
                RemoveUIComponent(c);
                UnityEngine.Object.Destroy(c.gameObject);
            }
        }

        void SetEntries()
        {
            if (PartialEntries != childCount)
                CreateEntries();

            if (!hasFocus && !UIView.HasInputFocus())
                Focus();

            for (int i = 0; i < childCount; i++)
                ((LazyEntry) components[i]).ResetData();

            LazyData[] data = GetData();
            bar.maxValue = data.Length;
            int start = Mathf.RoundToInt(bar.value);
            int n = Mathf.Min(childCount, data.Length - start);

            for (int i = 0; i < childCount; i++)
            {
                LazyEntry entry = (LazyEntry) components[i];

                if (i < n)
                    data[start + i].SetVisible(entry);
                else
                    entry.Hide();
            }
        }

        void SetStable()
        {
            LazyData[] data = GetData();
            int start = Mathf.RoundToInt(bar.value);
            int n = Mathf.Min(childCount, data.Length - start);

            for (int i = 0; i < n; i++)
                data[start + i].SetVisibleExtended();
        }

        LazyData[] GetData()
        {
            if (lazyData == null)
            {
                if (dataList == null)
                    dataList = ManagerPanel.CreateData();

                if (string.IsNullOrEmpty(searchString))
                    lazyData = dataList.ToArray();
                else
                    lazyData = dataList.Where(d => d.IsMatch(searchString)).ToArray();
            }

            return lazyData;
        }

        public StyleData[] GetStyleData()
        {
            if (styleData == null)
            {
                List<StyleData> list = new List<StyleData>();

                foreach (Package.Asset style in PackageManager.FilterAssets(UserAssetType.DistrictStyleMetaData))
                {
                    DistrictStyleMetaData meta = style.Instantiate<DistrictStyleMetaData>();

                    if (meta != null && !meta.builtin)
                    {
                        StyleData item;
                        item.name = meta.name;
                        item.style = style;
                        item.assets = new HashSet<string>(meta.assets);
                        list.Add(item);
                    }
                }

                list.Sort((StyleData s1, StyleData s2) => s1.name.CompareTo(s2.name));
                styleData = list.ToArray();
            }

            return styleData;
        }

        public void Refresh()
        {
            if (subscribed.Count > 0)
            {
                bool added = false;

                // Tricky because local copies of workshop packages are possible. PackageManager does not handle such duplicates correctly.
                foreach (Package.Asset asset in PackageManager.FilterAssets(UserAssetType.CustomAssetMetaData))
                    if (asset.isMainAsset && subscribed.Contains(asset.package.GetPublishedFileID()))
                    {
                        bool found = false;

                        foreach (LazyData d in dataList)
                            if (asset == d.asset && asset.package == d.asset.package)
                            {
                                found = true;
                                break;
                            }

                        if (!found)
                        {
                            dataList.Add(new LazyData(asset, NUM++));
                            added = true;
                        }
                    }

                if (added)
                    lazyData = null;
                else
                    subscribed.Clear();
            }

            styleData = null;
            redraw = true;
        }

        public void RefreshSearch(string text)
        {
            searchString = text;
            lazyData = null;
            redraw = true;
        }

        protected override void OnKeyDown(UIKeyEventParameter p)
        {
            if (!p.used)
            {
                float v;

                switch (p.keycode)
                {
                    case KeyCode.UpArrow:
                        v = bar.value - 1;
                        break;
                    case KeyCode.DownArrow:
                        v = bar.value + 1;
                        break;
                    case KeyCode.Home:
                        v = bar.minValue;
                        break;
                    case KeyCode.End:
                        v = bar.maxValue;
                        break;
                    case KeyCode.PageUp:
                        v = bar.value - FullEntries;
                        break;
                    case KeyCode.PageDown:
                        v = bar.value + FullEntries;
                        break;
                    default:
                        base.OnKeyDown(p);
                        return;
                }

                bar.value = v;
                p.Use();
            }

            base.OnKeyDown(p);
        }

        protected override void OnMouseWheel(UIMouseEventParameter p)
        {
            if (p.used)
                return;

            bar.value -= Mathf.Round(p.wheelDelta);
            p.Use();
            base.Invoke("OnMouseWheel", p);
            base.OnMouseWheel(p);
        }

        public void OnScollChanged(UIComponent component, float value) => redraw = true;

        public void OnParentSizeChanged(UIComponent p, Vector2 value)
        {
            size = value - new Vector2(ManagerPanel.SCROLLBAR_WIDTH, 0f);
            bar.height = bar.trackObject.height = value.y;
            bar.AlignTo(p, UIAlignAnchor.TopRight);
            bar.scrollSize = FullEntries;

            if (PartialEntries != childCount)
                redraw = true;
        }

        protected override void OnVisibilityChanged()
        {
            base.OnVisibilityChanged();

            if (!hasFocus && isVisible)
                Focus();
        }

        public void OnEnableAll(UIComponent component, UIMouseEventParameter p)
        {
            foreach (LazyData d in GetData())
            {
                d.InstantiateMetaData();
                d.asset.isEnabled = !(d.lacksAD || d.lacksSF);
            }

            redraw = true;
        }

        public void OnDisableAll(UIComponent component, UIMouseEventParameter p)
        {
            foreach (LazyData d in GetData())
                d.asset.isEnabled = false;

            redraw = true;
        }

        public void SortByName(UIComponent component, UIMouseEventParameter p) => Sort((LazyData a, LazyData b) => a.Name.CompareTo(b.Name), 1);
        public void SortBySubscribed(UIComponent component, UIMouseEventParameter p) => Sort((LazyData a, LazyData b) => (int) b.TimeSubscribed - (int) a.TimeSubscribed, 2);
        public void SortByUpdated(UIComponent component, UIMouseEventParameter p) => Sort((LazyData a, LazyData b) => (int) b.TimeUpdated - (int) a.TimeUpdated, 3);
        public void QueryAll(UIComponent component, UIMouseEventParameter p) { query = true; Focus(); }

        void Sort(Comparison<LazyData> cmp, int s)
        {
            if (dataList != null)
                if (sort != s)
                {
                    dataList.Sort(cmp);
                    sort = s;
                }
                else
                {
                    dataList.Sort((LazyData a, LazyData b) => a.num - b.num);
                    sort = 0;
                }

            lazyData = null;
            redraw = true;
        }

        void OnWorkshopSubscriptionChanged(PublishedFileId fileID, bool subscribed)
        {
            if (dataList != null)
            {
                if (subscribed)
                    this.subscribed.Add(fileID);
                else
                    for (int i = 0; i < dataList.Count;)
                    {
                        if (dataList[i].PackageId == fileID)
                            dataList.RemoveAt(i);
                        else
                            i++;
                    }

                lazyData = null;
                redraw = true;
            }
        }

        internal void OnDeleted(LazyData d)
        {
            if (dataList != null)
            {
                dataList.Remove(d);
                lazyData = null;
                redraw = true;
            }
        }

        public override void OnDestroy()
        {
            Steam.workshop.eventUGCRequestUGCDetailsCompleted -= OnSteamQueryCompleted;
            Steam.workshop.eventWorkshopSubscriptionChanged -= OnWorkshopSubscriptionChanged;
            requests.Clear();

            if (Mod.disposed)
            {
                ManagerPanel.instance?.Dispose();
                PackageManagerFix.instance?.Dispose();
                Mod.created = Mod.disposed = false;
            }

            instance = null;
        }
    }
}

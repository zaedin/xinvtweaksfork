using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using XInvTweaksFork.Patches;

namespace XInvTweaksFork;

public class XInvTweaksForkModSystem : ModSystem
{
    private static Harmony harmony;
    private ICoreAPI api;

    private ICoreClientAPI capi;
    private ChestSortDialog chestSortDialog;
    private string path;
    public static InvTweaksConfig Config { get; private set; }

    public override double ExecuteOrder()
    {
        return 1.0;
    }

    private static void DoHarmonyPatch()
    {
        harmony = new Harmony("XInvTweakPatch");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    public override void StartPre(ICoreAPI coreApi)
    {
        api = coreApi;
        LoadConfig();
    }

    public override void StartServerSide(ICoreServerAPI coreServerApi)
    {
        coreServerApi.Event.SaveGameLoaded += OnWorldLoaded;
    }

    public override void StartClientSide(ICoreClientAPI coreClientApi)
    {
        capi = coreClientApi;
        api = coreClientApi;

        PatchClient();
        RegisterKeys();

        if (!Config.ExtendChestUi) return;
        chestSortDialog = new ChestSortDialog(capi);

        GuiDialog? invDialog = capi.Gui.LoadedGuis.OfType<GuiDialogInventory>().FirstOrDefault();

        if (invDialog == null) return;
        invDialog.OnOpened += () => chestSortDialog.OnInventoryOpened(invDialog.Composers["maininventory"].Bounds);
        invDialog.OnClosed += () => { chestSortDialog.OnInventoryClosed(); };
    }

    public override void Dispose()
    {
        base.Dispose();
        harmony.UnpatchAll("XInvTweakPatch");
    }

    private void LoadConfig()
    {
        path = "xinvtweaksfork.json";

        try
        {
            Config = api.LoadModConfig<InvTweaksConfig>(path);
        }
        catch (Exception)
        {
            Config = new InvTweaksConfig();
        }

        Config ??= new InvTweaksConfig();
        Config.SortBlacklist.Add("cart");

        if (api.Side == EnumAppSide.Client)
        {
            if (Config.SortOrder.Count == 0)
            {
                Config.SortOrder.Add("priority");
                Config.SortOrder.Add("lightinvert");
                Config.SortOrder.Add("tool");
                Config.SortOrder.Add("tooltier");
                Config.SortOrder.Add("block");
                Config.SortOrder.Add("storageflags");
                Config.SortOrder.Add("id");
                Config.SortOrder.Add("name");
                Config.SortOrder.Add("durability");
                Config.SortOrder.Add("attackpower");
                Config.SortOrder.Add("satiety");
                Config.SortOrder.Add("health");
                Config.SortOrder.Add("intoxication");
                Config.SortOrder.Add("stacksize");
                Config.SortOrder.Add("density");
                Config.SortOrder.Add("state");
            }

            if (Config.StackOrder.Count == 0)
            {
                Config.StackOrder.Add("stacksize");
                Config.StackOrder.Add("durability");
                Config.StackOrder.Add("transition");
            }

            InventoryUtil.SortOrder = Config.SortOrder;
            InventoryUtil.StackOrder = Config.StackOrder;
            InventoryUtil.Priorities = Config.Priorities;
            InventoryUtil.StorageFlagsOrder = Config.StorageFlagsOrder;
        }

        api.StoreModConfig(Config, path);
    }

    private void OnWorldLoaded()
    {
        var toAdd = new Dictionary<string, int>();
        var toRemove = new List<string>();

        foreach (var collectible in api.World.Collectibles)
        {
            var storageProps = collectible.GetBehavior<CollectibleBehaviorGroundStorable>()?.StorageProps;
            if (storageProps == null) continue;
            var pair = Config.BulkQuanties.GetEnumerator();

            var found = false;
            while (pair.MoveNext() && !found)
                if (collectible.WildCardMatch(new AssetLocation(pair.Current.Key)))
                {
                    var maxTransfer = Math.Min(collectible.MaxStackSize, storageProps.StackingCapacity);
                    var value = Math.Max(Math.Min(pair.Current.Value, maxTransfer), storageProps.BulkTransferQuantity);
                    storageProps.BulkTransferQuantity = value;
                    found = true;

                    if (pair.Current.Value == value) continue;
                    if (toAdd.TryAdd(pair.Current.Key, value))
                    {
                        toRemove.Add(pair.Current.Key);
                    }
                }

            if (found) continue;

            if (collectible.MaxStackSize <= storageProps.BulkTransferQuantity) continue;
            if (storageProps.StackingCapacity <= storageProps.BulkTransferQuantity) continue;

            var firstCodePart = collectible.FirstCodePart();
            var dashCount = collectible.Code.Path.Count(x => x == '-');
            for (var ii = 0; ii < dashCount; ++ii) firstCodePart += "-*";

            toAdd.TryAdd(firstCodePart, storageProps.BulkTransferQuantity);
        }

        foreach (var key in toRemove) Config.BulkQuanties.Remove(key);

        if (toAdd.Count > 0)
        {
            var iterator = toAdd.GetEnumerator();
            while (iterator.MoveNext())
                if (!Config.BulkQuanties.ContainsKey(iterator.Current.Key))
                    Config.BulkQuanties.Add(iterator.Current.Key, iterator.Current.Value);
        }

        api.StoreModConfig(Config, path);
    }

    private void PatchClient()
    {
        DoHarmonyPatch();
        if (Config.Tools)
            ManualPatch.PatchMethod(harmony, typeof(CollectibleObject), typeof(CollectibleObjectPatch), "DamageItem");

        //if (Config.groundStorage) ManualPatch.PatchMethod(harmony, typeof(CollectibleBehaviorGroundStorable), typeof(CollectibleObjectPatch), "Interact");
        if (Config.GroundStorage)
            ManualPatch.PatchMethod(harmony, typeof(BlockEntityGroundStorage), typeof(BlockEntityGroundStoragePatch),
                "OnPlayerInteractStart");

        if (Config.Blocks)
            ManualPatch.PatchMethod(harmony, typeof(Block), typeof(CollectibleObjectPatch), "TryPlaceBlock");

        if (Config.Stairs)
            ManualPatch.PatchMethod(harmony, typeof(BlockStairs), typeof(CollectibleObjectPatch), "TryPlaceBlock");

        //if (Config.piles) ManualPatch.PatchMethod(harmony, typeof(BlockEntityItemPile), typeof(CollectibleObjectPatch), "OnPlayerInteract");
        if (Config.Piles)
            ManualPatch.PatchMethod(harmony, typeof(BlockEntityItemPile), typeof(BlockEntityItemPilePatch),
                "OnPlayerInteract");

        if (Config.ExtendChestUi)
            ManualPatch.PatchMethod(harmony, typeof(GuiDialogBlockEntityInventory),
                typeof(GuiDialogBlockEntityInventoryPatch), "OnGuiOpened");

        if (Config.ExtendChestUi)
            ManualPatch.PatchMethod(harmony, typeof(GuiDialogBlockEntityInventory),
                typeof(GuiDialogBlockEntityInventoryPatch), "OnGuiClosed");

        if (Config.CrateSwitch)
            ManualPatch.PatchMethod(harmony, typeof(BlockEntityCrate), typeof(BlockEntityCratePatch),
                "OnBlockInteractStart");

        if (Config.Seeds)
        {
            ManualPatch.PatchMethod(harmony, typeof(ItemPlantableSeed), typeof(CollectibleObjectPatch),
                "OnHeldInteractStart");

            var assembly = capi.ModLoader.GetModSystem("XSkills.XSkills")?.GetType().Assembly;
            var type = assembly?.GetType("XSkills.XSkillsItemPlantableSeed");
            if (type != null)
                ManualPatch.PatchMethod(harmony, type, typeof(CollectibleObjectPatch), "OnHeldInteractStart");
        }

        if (Config.StrgClick)
            ManualPatch.PatchMethod(harmony, typeof(InventoryBase), typeof(InventoryBasePatch), "ActivateSlot");

        if (Config.PushPullWheel)
            ManualPatch.PatchMethod(harmony, typeof(GuiElementItemSlotGridBase),
                typeof(GuiElementItemSlotGridBasePatch), "OnMouseWheel");
        //if (Config.survivalPick) ManualPatch.PatchMethod(harmony, typeof(SystemMouseInWorldInteractions), typeof(SystemMouseInWorldInteractionsPatch), "HandleMouseInteractionsBlockSelected");

        capi.Event.LevelFinalize += OnWorldLoaded;
    }

    private void RegisterKeys()
    {
        capi.Input.RegisterHotKey("pushinventory", Lang.Get("xinvtweaksfork:pushinventory"), GlKeys.Z,
            HotkeyType.InventoryHotkeys);
        capi.Input.RegisterHotKey("sortinventories", Lang.Get("xinvtweaksfork:sortinventories"), GlKeys.Z,
            HotkeyType.InventoryHotkeys, false, false, true);
        capi.Input.RegisterHotKey("pullinventory", Lang.Get("xinvtweaksfork:pullinventory"), GlKeys.Z,
            HotkeyType.InventoryHotkeys, false, true);
        capi.Input.RegisterHotKey("sortbackpack", Lang.Get("xinvtweaksfork:sortbackpack"), GlKeys.Z,
            HotkeyType.InventoryHotkeys, true);
        capi.Input.RegisterHotKey("fillbackpack", Lang.Get("xinvtweaksfork:fillbackpack"), GlKeys.Z,
            HotkeyType.InventoryHotkeys, true, true);
        capi.Input.RegisterHotKey("clearhandslot", Lang.Get("xinvtweaksfork:clearhandslot"), GlKeys.V,
            HotkeyType.InventoryHotkeys);

        capi.Input.SetHotKeyHandler("pushinventory", _ => InventoryUtil.PushInventory(capi));
        capi.Input.SetHotKeyHandler("sortinventories", _ => InventoryUtil.SortInventories(capi));
        capi.Input.SetHotKeyHandler("pullinventory", _ => InventoryUtil.PullInventory(capi));
        capi.Input.SetHotKeyHandler("sortbackpack", _ => InventoryUtil.SortBackpack(capi));
        capi.Input.SetHotKeyHandler("fillbackpack", _ => InventoryUtil.FillBackpack(capi));
        capi.Input.SetHotKeyHandler("clearhandslot", _ => InventoryUtil.ClearHandSlot(capi));
    }

    public void OnInventoryOpened(ElementBounds parent)
    {
        chestSortDialog.OnInventoryOpened(parent);
    }

    public void OnInventoryClosed()
    {
        chestSortDialog.OnInventoryClosed();
    }
}
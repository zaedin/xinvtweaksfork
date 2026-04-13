using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace XInvTweaksFork.Patches;

internal class CollectibleObjectPatch
{
    public static void TryPlaceBlockPostfix(Block __instance, bool __result, IPlayer byPlayer)
    {
        if (!__result) return;
        byPlayer?.Entity?.World.RegisterCallback(
            _ => Callback(__instance, byPlayer.InventoryManager?.ActiveHotbarSlot, byPlayer),
            XInvTweaksForkModSystem.Config.delay);
    }

    public static void DamageItemPostfix(CollectibleObject __instance, Entity byEntity, ItemSlot itemSlot)
    {
        var player = byEntity as EntityPlayer;
        if (player?.Api.Side != EnumAppSide.Client) return;
        if (__instance.Tool == null || itemSlot == null) return;
        if (!player.Player.InventoryManager.Inventories.ContainsValue(itemSlot.Inventory)) return;

        var durability = itemSlot.Itemstack?.Attributes.GetInt("durability", 99999) ?? 0;
        if (XInvTweaksForkModSystem.Config.toolSwitchDurability == 0 && itemSlot.Itemstack != null) return;
        if (durability > XInvTweaksForkModSystem.Config.toolSwitchDurability || durability == 0) return;

        ItemSlot bestResult = null;
        player.Player.InventoryManager.Find(slot2 =>
        {
            if (slot2 == itemSlot) return false;
            if (slot2.Itemstack?.Collectible?.Tool == __instance.Tool)
            {
                durability = slot2.Itemstack?.Attributes.GetInt("durability", 99999) ?? 0;
                if (durability <= XInvTweaksForkModSystem.Config.toolSwitchDurability) return false;

                if (slot2.Itemstack.Collectible == __instance)
                {
                    bestResult = slot2;
                    return true;
                }

                if (bestResult == null) bestResult = slot2;
                //example: prefer pickaxes over pro pickaxes
                else if (bestResult.Itemstack.Collectible.GetType() != __instance.GetType() &&
                         slot2.Itemstack.Collectible.GetType() == __instance.GetType())
                    bestResult = slot2;
            }

            return false;
        });

        if (bestResult != null)
            player.World.RegisterCallback(_ =>
            {
                var slotID = -1;
                for (var ii = 0; ii <= bestResult.Inventory.Count; ++ii)
                    if (bestResult.Inventory[ii] == bestResult)
                    {
                        slotID = ii;
                        break;
                    }

                var packet = bestResult.Inventory.TryFlipItems(slotID, itemSlot);
                (player.Api as ClientCoreAPI)?.Network.SendPacketClient(packet);
            }, 0);
    }

    public static void InteractPrefix(CollectibleBehaviorGroundStorable __instance, out CollectibleObject __state,
        ItemSlot itemslot)
    {
        __state = __instance?.collObj ?? itemslot.Itemstack?.Collectible;
    }

    public static void InteractPostfix(CollectibleObject __state, ItemSlot itemslot, EntityAgent byEntity)
    {
        OnHeldInteractStart(__state, itemslot, byEntity);
    }

    public static void OnHeldInteractStartPostfix(CollectibleObject __instance, ItemSlot itemslot, EntityAgent byEntity)
    {
        OnHeldInteractStart(__instance, itemslot, byEntity);
    }

    public static void OnPlayerInteractPrefix(BlockEntityItemPile __instance, out CollectibleObject __state)
    {
        __state = __instance.inventory?[0]?.Itemstack?.Collectible;
    }

    public static void OnPlayerInteractPostfix(BlockEntityItemPile __instance, CollectibleObject __state, bool __result,
        IPlayer byPlayer)
    {
        if (__result) OnHeldInteractStart(__state, byPlayer.InventoryManager.ActiveHotbarSlot, byPlayer.Entity);
    }

    public static void OnHeldInteractStart(CollectibleObject __instance, ItemSlot slot, EntityAgent byEntity)
    {
        var player = (byEntity as EntityPlayer)?.Player;
        if (player == null) return;
        byEntity?.World.RegisterCallback(_ => Callback(__instance, slot, player), 100);
    }

    private static void Callback(CollectibleObject collectible, ItemSlot slot, IPlayer player)
    {
        if (player?.Entity?.Api.Side != EnumAppSide.Client || slot == null) return;
        if (!player.InventoryManager.Inventories.ContainsValue(slot.Inventory)) return;
        if (slot.Itemstack != null) return;
        if (slot is ItemSlotBackpack) return;

        ItemSlot bestResult = null;
        player.InventoryManager.Find(slot2 =>
        {
            if (!(slot2.Inventory is InventoryBasePlayer)) return false;
            if (slot2.Itemstack?.Collectible == collectible
                && !(slot2 is ItemSlotCraftingOutput)
                && !(slot2 is ItemSlotOffhand))
            {
                bestResult = slot2;
                return true;
            }

            return false;
        });

        if (bestResult != null)
        {
            var op = new ItemStackMoveOperation(player.Entity.World, EnumMouseButton.Left, 0,
                EnumMergePriority.AutoMerge, bestResult.StackSize);
            var packet = player.InventoryManager.TryTransferTo(bestResult, slot, ref op);
            (player.Entity.Api as ClientCoreAPI)?.Network.SendPacketClient(packet);
        }
    }
}
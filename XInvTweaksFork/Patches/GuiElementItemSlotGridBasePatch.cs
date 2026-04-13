using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace XInvTweaksFork.Patches;

internal class GuiElementItemSlotGridBasePatch
{
    public static void OnMouseWheelPostfix(GuiElementItemSlotGridBase __instance, ElementBounds[] ___SlotBounds,
        Vintagestory.API.Datastructures.OrderedDictionary<int, ItemSlot> ___renderedSlots, IInventory ___inventory,
        Action<object> ___SendPacketHandler, ICoreClientAPI api, MouseWheelEventArgs args)
    {
        var ctrl = api.Input.KeyboardKeyState[(int)GlKeys.ShiftLeft] ||
                   api.Input.KeyboardKeyState[(int)GlKeys.ShiftRight];
        if (!ctrl || !__instance.KeyboardControlEnabled ||
            !__instance.IsPositionInside(api.Input.MouseX, api.Input.MouseY)) return;
        for (var i = 0; i < __instance.SlotBounds.Length; i++)
        {
            if (i >= ___renderedSlots.Count) break;

            if (__instance.SlotBounds[i].PointInside(api.Input.MouseX, api.Input.MouseY))
            {
                OnMouseWheel(api, ___inventory[___renderedSlots.GetKeyAtIndex(i)], args.delta, ___SendPacketHandler);
                args.SetHandled();
            }
        }
    }

    internal static void OnMouseWheel(ICoreClientAPI api, ItemSlot source, int wheelDelta,
        Action<object> SendPacketHandler)
    {
        object packet;
        if (api.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative) return;
        packet = wheelDelta > 0 ? PushItem(api, source) : PullItem(api, source);

        {
            SendPacketHandler?.Invoke(packet);
        }
    }

    internal static object PushItem(ICoreClientAPI api, ItemSlot source)
    {
        var op = new ItemStackMoveOperation(api.World, EnumMouseButton.Wheel, 0, EnumMergePriority.AutoMerge, 1)
        {
            ActingPlayer = api.World.Player
        };
        var target = api.World.Player.InventoryManager.GetBestSuitedSlot(source, false);
        return api.World.Player.InventoryManager.TryTransferTo(source, target, ref op);
    }

    internal static object PullItem(ICoreClientAPI api, ItemSlot target)
    {
        if (target?.Itemstack == null) return null;
        List<IInventory> inventories = api.World.Player.InventoryManager.OpenedInventories;
        ItemSlot bestSource = null;

        foreach (var inventory in inventories.Where(inventory => inventory != target.Inventory || bestSource == null)
                     .Where(_ => target.Inventory is not InventoryBasePlayer ||
                                         bestSource?.Inventory is not InventoryBasePlayer))
        {
            foreach (var slot in inventory)
            {
                if (slot == target) continue;
                if (slot.Itemstack?.Collectible != target.Itemstack.Collectible) continue;
                bestSource = slot;
                break;
            }

            if (bestSource?.Inventory is InventoryBasePlayer && bestSource?.Inventory != target.Inventory) break;
        }

        if (bestSource == null) return null;
        var op = new ItemStackMoveOperation(api.World, EnumMouseButton.Wheel, 0, EnumMergePriority.AutoMerge, 1);
        op.ActingPlayer = api.World.Player;
        return api.World.Player.InventoryManager.TryTransferTo(bestSource, target, ref op);
    }
}
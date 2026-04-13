using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace XInvTweaksFork.Patches;

internal class InventoryBasePatch
{
    private static bool ActivateSlotPrefix(InventoryBase __instance, ref object __result, int slotId,
        ref ItemStackMoveOperation op)
    {
        if (op.ShiftDown || !op.CtrlDown) return true;
        if (__instance is InventoryPlayerCreative) return true;

        var capi = __instance.Api as ICoreClientAPI;
        var slot = __instance[slotId];
        if (slot?.Itemstack == null || capi == null) return true;
        if (slot.Itemstack.Collectible.IsLiquid()) return true;
        __result = null;

        for (var ii = 0; ii <= slotId; ii++)
        {
            if (__instance[ii].Itemstack == null) continue;
            if (__instance[ii].Itemstack.Collectible != slot.Itemstack.Collectible) continue;
            var slotOp = new ItemStackMoveOperation(op.World, op.MouseButton, EnumModifierKey.SHIFT,
                EnumMergePriority.AutoMerge);
            slotOp.ActingPlayer = op.ActingPlayer;
            slotOp.RequestedQuantity = __instance[ii].Itemstack.StackSize;

            object[] objcs = slotOp.ActingPlayer.InventoryManager.TryTransferAway(__instance[ii], ref slotOp, false);
            if (objcs == null) continue;
            foreach (var obj in objcs) capi.Network.SendPacketClient(obj);
        }

        return false;
    }
}
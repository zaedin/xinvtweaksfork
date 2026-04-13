using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace XInvTweaksFork.Patches;

internal class BlockEntityCratePatch
{
    public static void OnBlockInteractStartPrefix(BlockEntityCrate __instance, IPlayer byPlayer)
    {
        if (__instance.Api.Side == EnumAppSide.Server) return;
        InventoryUtil.FindPushableCollectible(__instance.Inventory, byPlayer);
    }
}
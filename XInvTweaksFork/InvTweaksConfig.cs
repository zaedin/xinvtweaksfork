using System.Collections.Generic;
using Vintagestory.API.Common;

namespace XInvTweaksFork;

public class InvTweaksConfig
{
    public readonly bool Blocks = true;

    public readonly Dictionary<string, int> BulkQuanties = new();
    public readonly bool CrateSwitch = true;
    public readonly int Delay = 200;
    public readonly bool ExtendChestUi = true;
    public readonly bool GroundStorage = true;
    public readonly SortedSet<int> LockedSlots = new();
    public readonly bool Piles = true;

    public readonly Dictionary<string, int> Priorities = new();

    public readonly bool PushPullWheel = true;
    public readonly bool Seeds = true;
    public readonly SortedSet<string> SortBlacklist = new();

    public readonly List<string> SortOrder = new();
    public readonly List<string> StackOrder = new();
    public readonly bool Stairs = true;
    public readonly List<EnumItemStorageFlags> StorageFlagsOrder = new();
    public readonly bool StrgClick = true;
    public readonly bool Tools = true;
    public readonly int ToolSwitchDurability = 0;
}
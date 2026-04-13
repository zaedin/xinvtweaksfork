using System.Collections.Generic;
using Vintagestory.API.Common;

namespace XInvTweaksFork;

public class InvTweaksConfig
{
    public bool blocks = true;

    public Dictionary<string, int> BulkQuanties = new();
    public bool crateSwitch = true;
    public int delay = 200;
    public bool extendChestUi = true;
    public bool groundStorage = true;
    public SortedSet<int> LockedSlots = new();
    public bool piles = true;

    public Dictionary<string, int> Priorities = new();

    //public bool survivalPick = true;
    public bool pushPullWheel = true;
    public bool seeds = true;
    public SortedSet<string> SortBlacklist = new();

    public List<string> SortOrder = new();
    public List<string> StackOrder = new();
    public bool stairs = true;
    public List<EnumItemStorageFlags> StorageFlagsOrder = new();
    public bool strgClick = true;
    public bool tools = true;
    public int toolSwitchDurability = 0;
}
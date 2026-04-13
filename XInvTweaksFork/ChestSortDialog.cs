using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace XInvTweaksFork;

public class ChestSortDialog : HudElement
{
    protected int openedContainers;

    public ChestSortDialog(ICoreClientAPI capi) : base(capi)
    {
        Setup();
    }

    protected void Setup()
    {
        var dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftTop);
        var buttonSortBounds = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight, 100, 40);
        var buttonPushBounds = ElementBounds.Fixed(105, GuiStyle.TitleBarHeight, 40, 40);
        var buttonPullBounds = ElementBounds.Fixed(150, GuiStyle.TitleBarHeight, 40, 40);
        var buttonFillBounds = ElementBounds.Fixed(195, GuiStyle.TitleBarHeight, 40, 40);
        var inputBounds = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight + 40 + GuiStyle.HalfPadding, 190, 40);
        var clearBounds = ElementBounds.Fixed(195, GuiStyle.TitleBarHeight + 40 + GuiStyle.HalfPadding, 40, 40);

        var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.HalfPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;
        bgBounds.WithChildren(buttonSortBounds, buttonPushBounds, buttonPullBounds, buttonFillBounds, inputBounds,
            clearBounds);

        SingleComposer = capi.Gui.CreateCompo("ChestSortDialog", dialogBounds)
            .AddDialogBG(bgBounds)
            .AddDialogTitleBar("")
            .AddSmallButton(Lang.Get("xinvtweaksfork:sort"), OnSort, buttonSortBounds)
            .AddSmallButton(Lang.Get("xinvtweaksfork:Λ"), OnPush, buttonPushBounds)
            .AddSmallButton(Lang.Get("xinvtweaksfork:V"), OnPull, buttonPullBounds)
            .AddSmallButton(Lang.Get("xinvtweaksfork:V\n¯"), OnFill, buttonFillBounds)
            .AddTextInput(inputBounds, OnTextChanged, null, "input")
            .AddSmallButton(Lang.Get("xinvtweaksfork:X"), OnClearText, clearBounds)
            .Compose();

        SingleComposer.GetTextInput("input")?.SetPlaceHolderText(Lang.Get("xinvtweaksfork:priority"));
    }

    protected void OnTextChanged(string text)
    {
        if (text.Length == 0) InventoryUtil.Priority = null;
        else InventoryUtil.Priority = text;
    }

    protected bool OnlyPlayerInventoriesOpen()
    {
        List<IInventory> inventories = capi.World.Player.InventoryManager.OpenedInventories;
        foreach (var inventory in inventories)
            if (!(inventory is InventoryBasePlayer))
                return false;

        return true;
    }

    public bool OnSort()
    {
        if (OnlyPlayerInventoriesOpen())
            InventoryUtil.SortBackpack(capi);
        else
            InventoryUtil.SortOpenInventories(capi);
        return true;
    }

    public bool OnPush()
    {
        InventoryUtil.SortIntoInventory(capi);
        return true;
    }

    public bool OnPull()
    {
        InventoryUtil.PullInventories(capi);
        return true;
    }

    public bool OnFill()
    {
        InventoryUtil.FillBackpack(capi);
        return true;
    }

    public bool OnClearText()
    {
        SingleComposer.GetTextInput("input")?.SetValue("");
        return true;
    }

    public void OpenDialog(ElementBounds parent)
    {
        var title = SingleComposer["element-2"] as GuiElementDialogTitleBar;
        if (title?.Movable ?? false) parent = null;
        if (parent != null)
        {
            SingleComposer.Bounds.WithAlignment(EnumDialogArea.CenterBottom).WithParent(parent);
            SingleComposer.Bounds.WithFixedPosition(0, SingleComposer.Bounds.OuterHeightInt + 10);
        }
        else
        {
            var bounds = SingleComposer.Bounds;
            parent = capi.Gui.WindowBounds;
            if (bounds.absX + bounds.OuterWidth > parent.InnerWidth || bounds.absX < 0.0 ||
                bounds.absY + bounds.OuterHeight > parent.InnerHeight || bounds.absY < 0.0)
            {
                bounds.WithFixedPosition(
                    bounds.absX + bounds.OuterWidth > parent.InnerWidth || bounds.absX < 0.0 ? 0.0 : bounds.absX,
                    bounds.absY + bounds.OuterHeight > parent.InnerHeight || bounds.absY < 0.0 ? 0.0 : bounds.absY);
                bounds.WithAlignment(EnumDialogArea.LeftTop).WithParent(parent);
            }
        }

        SingleComposer.ReCompose();
        TryOpen();
    }

    public void OnInventoryOpened(ElementBounds parent)
    {
        openedContainers++;
        if (openedContainers == 1) OpenDialog(parent);
    }

    public void OnInventoryClosed()
    {
        openedContainers--;
        if (openedContainers <= 0) TryClose();
    }
}
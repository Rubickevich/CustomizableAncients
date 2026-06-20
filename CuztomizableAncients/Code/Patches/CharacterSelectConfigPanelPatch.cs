using CuztomizableAncients.Configuration;
using CuztomizableAncients.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace CuztomizableAncients.Patches;

[HarmonyPatch(typeof(NCharacterSelectScreen), "AfterInitialized")]
public static class CharacterSelectConfigPanelPatch
{
    public static void Postfix(NCharacterSelectScreen __instance)
    {
        if (__instance.Lobby.NetService.Type == NetGameType.Client)
        {
            return;
        }

        AddLauncher(__instance);
    }

    private static void AddLauncher(NCharacterSelectScreen screen)
    {
        if (screen.HasNode("CuztomizableAncientsLauncher"))
        {
            return;
        }

        Control menu = BuildMenu(screen);
        screen.AddChild(menu);

        Button launcher = new()
        {
            Name = "CuztomizableAncientsLauncher",
            Text = "⚙",
            CustomMinimumSize = new Vector2(108f, 108f),
            IconAlignment = HorizontalAlignment.Center,
            VerticalIconAlignment = VerticalAlignment.Center,
            ExpandIcon = true,
            AnchorLeft = 0f,
            AnchorRight = 0f,
            OffsetLeft = 24f,
            OffsetRight = 132f,
            OffsetTop = 24f,
            OffsetBottom = 132f
        };
        launcher.Icon = TryGetAncientChatIcon(ModelDb.AncientEvent<Neow>());
        launcher.Text = string.Empty;
        launcher.Modulate = new Color(1f, 1f, 1f, 0.58f);
        launcher.TooltipText = "Configure ancient relics";

        Button closeButton = (Button)menu.FindChild("CuztomizableAncientsCloseButton", true, false);
        Tween? menuTween = null;

        void CloseMenu()
        {
            menuTween?.Kill();
            menuTween = menu.CreateTween();
            menuTween.SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Cubic);
            menuTween.TweenProperty(
                menu,
                "position",
                new Vector2(0f, -screen.GetViewportRect().Size.Y),
                0.32);
            menuTween.TweenCallback(Callable.From(() =>
            {
                menu.Visible = false;
                menu.Position = Vector2.Zero;
                launcher.Visible = true;
            }));
        }

        launcher.Pressed += () =>
        {
            menuTween?.Kill();
            launcher.Visible = false;
            menu.Visible = true;
            menu.Position = new Vector2(0f, -screen.GetViewportRect().Size.Y);
            menuTween = menu.CreateTween();
            menuTween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            menuTween.TweenProperty(menu, "position", Vector2.Zero, 0.38);
        };
        closeButton.Pressed += CloseMenu;
        screen.AddChild(launcher);
    }

    private static Control BuildMenu(NCharacterSelectScreen screen)
    {
        Control overlay = new()
        {
            Name = "CuztomizableAncientsMenu",
            Visible = false,
            AnchorRight = 1f,
            AnchorBottom = 1f
        };

        ColorRect backdrop = new()
        {
            Color = new Color(0f, 0.04f, 0.07f, 0.94f),
            AnchorRight = 1f,
            AnchorBottom = 1f
        };
        overlay.AddChild(backdrop);

        PanelContainer menu = new()
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            OffsetLeft = 44f,
            OffsetRight = -44f,
            OffsetTop = 44f,
            OffsetBottom = -44f
        };
        overlay.AddChild(menu);

        VBoxContainer outer = new();
        menu.AddChild(outer);

        ScrollContainer ancientTabScroll = new()
        {
            CustomMinimumSize = new Vector2(0f, 76f),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        outer.AddChild(ancientTabScroll);

        HBoxContainer ancientTabs = new();
        ancientTabScroll.AddChild(ancientTabs);

        HBoxContainer content = new()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        outer.AddChild(content);

        ScrollContainer choiceScroll = new()
        {
            CustomMinimumSize = new Vector2(820f, 600f),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto
        };
        content.AddChild(choiceScroll);

        VBoxContainer choiceColumn = new()
        {
            CustomMinimumSize = new Vector2(800f, 0f),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        choiceScroll.AddChild(choiceColumn);

        VSeparator separator = new();
        content.AddChild(separator);

        VBoxContainer slotEditor = new()
        {
            CustomMinimumSize = new Vector2(330f, 600f),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        content.AddChild(slotEditor);

        VSeparator poolSeparator = new();
        content.AddChild(poolSeparator);

        PanelContainer poolRelicPanel = BuildPoolRelicPanel(screen);
        content.AddChild(poolRelicPanel);

        Button closeButton = new()
        {
            Name = "CuztomizableAncientsCloseButton",
            TooltipText = "Close configuration",
            CustomMinimumSize = new Vector2(0f, 54f),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        Control triangleHost = new()
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        Polygon2D triangleFill = new()
        {
            Polygon =
            [
                new Vector2(-40f, 14f),
                new Vector2(40f, 14f),
                new Vector2(0f, -17f)
            ],
            Color = Colors.White
        };
        triangleHost.AddChild(triangleFill);
        closeButton.AddChild(triangleHost);

        void CenterTriangle()
        {
            Vector2 center = triangleHost.Size * 0.5f;
            triangleFill.Position = center;
        }

        triangleHost.Resized += CenterTriangle;
        triangleHost.TreeEntered += () => Callable.From(CenterTriangle).CallDeferred();
        outer.AddChild(closeButton);

        AddSlotControls(screen, choiceColumn, slotEditor, poolRelicPanel, ancientTabs);

        return overlay;
    }

    private static void AddSlotControls(
        NCharacterSelectScreen screen,
        VBoxContainer choiceColumn,
        VBoxContainer slotEditor,
        PanelContainer poolRelicPanel,
        HBoxContainer ancientTabs)
    {
        IReadOnlyList<AncientEventModel> ancients = AncientRelicPools.AllAncients;
        AncientEventModel selectedAncient = ancients.FirstOrDefault(ancient => ancient is Neow)
            ?? ancients.First();
        bool showNonNativePools = false;
        bool showModdedPools = false;
        Action<AncientRelicPoolDefinition?> showPoolConfiguration = _ => { };
        ButtonGroup ancientTabGroup = new();
        List<(AncientEventModel Ancient, Button Button)> ancientTabButtons = [];

        foreach (AncientEventModel ancient in ancients)
        {
            Button tab = new()
            {
                CustomMinimumSize = new Vector2(68f, 64f),
                Icon = TryGetAncientChatIcon(ancient),
                ExpandIcon = false,
                IconAlignment = HorizontalAlignment.Center,
                VerticalIconAlignment = VerticalAlignment.Center,
                ToggleMode = true,
                ButtonGroup = ancientTabGroup,
                TooltipText = ancient.Title.GetFormattedText()
            };
            tab.Pressed += () => SelectAncient(ancient);
            ancientTabs.AddChild(tab);
            ancientTabButtons.Add((ancient, tab));
        }

        void Clear(Container container)
        {
            foreach (Node child in container.GetChildren())
            {
                container.RemoveChild(child);
                child.QueueFree();
            }
        }

        List<AncientRelicPoolDefinition> SelectedPools(AncientOptionConfig slot)
        {
            return slot.PoolIds
                .Select(AncientRelicPools.Get)
                .OfType<AncientRelicPoolDefinition>()
                .ToList();
        }

        AncientRelicConfig CurrentConfig()
        {
            return AncientRelicConfigService.ActiveConfig.GetAncientConfig(selectedAncient.Id);
        }

        void RenderChoices()
        {
            Clear(choiceColumn);

            HBoxContainer heading = new()
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            choiceColumn.AddChild(heading);
            heading.AddChild(new Label
            {
                Text = $"{selectedAncient.Title.GetFormattedText()} Choice Preview",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            });

            Button reset = new()
            {
                Text = "Reset to default"
            };
            reset.Pressed += () =>
            {
                AncientRelicConfigService.ResetAncient(selectedAncient.Id);
                showPoolConfiguration(null);
                RenderChoices();
                RenderSlotEditor();
            };
            heading.AddChild(reset);

            foreach (AncientOptionConfig slot in CurrentConfig().Options.ToList())
            {
                Button choice = BuildAncientChoicePreview(
                    slot,
                    SelectedPools(slot),
                    payload =>
                    {
                        if (AncientRelicPools.Get(payload.PoolId) != null && !slot.PoolIds.Contains(payload.PoolId))
                        {
                            slot.PoolIds.Add(payload.PoolId);
                            CurrentConfig().IsCustomized = true;
                            RenderChoices();
                        }
                    },
                    () =>
                    {
                        AncientRelicConfig config = CurrentConfig();
                        config.Options.Remove(slot);
                        config.EnsureValidShape();
                        config.IsCustomized = true;
                        RenderChoices();
                    });
                choiceColumn.AddChild(choice);
            }

            if (CurrentConfig().Options.Count == 0)
            {
                choiceColumn.AddChild(new Label
                {
                    Text = "This ancient currently has no options.",
                    Modulate = Colors.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                });
            }

            choiceColumn.AddChild(BuildAddTile(
                new Vector2(790f, 104f),
                "Add option",
                () =>
                {
                    AncientRelicConfig config = CurrentConfig();
                    config.Options.Add(new AncientOptionConfig(config.Options.Count, []));
                    config.IsCustomized = true;
                    RenderChoices();
                },
                expandHorizontal: true));
        }

        void RenderSlotEditor()
        {
            Clear(slotEditor);

            Label section = new()
            {
                Text = "Available pools"
            };
            slotEditor.AddChild(section);

            CheckBox nonNativeToggle = new()
            {
                Text = "Show non-native pools",
                ButtonPressed = showNonNativePools
            };
            nonNativeToggle.Toggled += enabled =>
            {
                showNonNativePools = enabled;
                RenderSlotEditor();
            };
            slotEditor.AddChild(nonNativeToggle);

            CheckBox moddedToggle = new()
            {
                Text = "Show modded pools",
                ButtonPressed = showModdedPools
            };
            moddedToggle.Toggled += enabled =>
            {
                showModdedPools = enabled;
                RenderSlotEditor();
            };
            slotEditor.AddChild(moddedToggle);

            PanelContainer dropArea = new()
            {
                SizeFlagsVertical = Control.SizeFlags.ExpandFill
            };
            slotEditor.AddChild(dropArea);

            ScrollContainer poolScroll = new();
            dropArea.AddChild(poolScroll);

            FlowContainer poolGrid = new()
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill
            };
            poolGrid.AddThemeConstantOverride("h_separation", 14);
            poolGrid.AddThemeConstantOverride("v_separation", 14);
            poolScroll.AddChild(poolGrid);

            foreach (AncientRelicPoolDefinition pool in AncientRelicPools.GetAvailablePools(
                         selectedAncient.Id,
                         showNonNativePools,
                         showModdedPools))
            {
                Control poolIcon = AncientRelicPoolIcon.Create(
                    pool,
                    draggable: true);
                if (pool.IsUserCreated)
                {
                    AddDeleteOverlay(poolIcon, "Delete pool", () =>
                    {
                        if (AncientRelicConfigService.DeleteCustomPool(pool.Id))
                        {
                            showPoolConfiguration(null);
                            RenderChoices();
                            RenderSlotEditor();
                        }
                    });
                }

                poolGrid.AddChild(poolIcon);
            }

            poolGrid.AddChild(BuildAddTile(
                new Vector2(96f, 96f),
                "Add pool",
                () => PromptForPoolName(screen, name =>
                {
                    AncientRelicPoolDefinition pool = AncientRelicConfigService.CreateCustomPool(name);
                    RenderSlotEditor();
                    showPoolConfiguration(pool);
                })));

            AncientPoolDragControls.ConfigureTarget(
                dropArea,
                payload => payload.SourceOption.HasValue,
                payload =>
                {
                    AncientOptionConfig? sourceOption = CurrentConfig().Options
                        .FirstOrDefault(slot => slot.OptionIndex == payload.SourceOption);
                    if (sourceOption != null && sourceOption.PoolIds.Remove(payload.PoolId))
                    {
                        CurrentConfig().IsCustomized = true;
                        RenderChoices();
                    }
                });
            AddDragOverlay(
                dropArea,
                "Drop here to remove pool from option",
                payload => payload.SourceOption.HasValue);
        }

        RenderChoices();
        RenderSlotEditor();
        showPoolConfiguration = FillPoolRelicPanel(
            screen,
            poolRelicPanel,
            () =>
            {
                CurrentConfig().IsCustomized = true;
                RenderChoices();
                RenderSlotEditor();
            },
            (pool, relic) =>
            {
                AncientRelicPoolDefinition customized = AncientRelicConfigService.AddRelicToPool(
                    selectedAncient.Id,
                    pool,
                    relic);
                RenderChoices();
                RenderSlotEditor();
                return customized;
            });

        void RefreshAncientTabs()
        {
            foreach ((AncientEventModel ancient, Button button) in ancientTabButtons)
            {
                bool selected = ancient.Id == selectedAncient.Id;
                button.ButtonPressed = selected;
                button.Modulate = selected ? Colors.White : new Color(1f, 1f, 1f, 0.58f);
            }
        }

        void SelectAncient(AncientEventModel ancient)
        {
            selectedAncient = ancient;
            RefreshAncientTabs();
            showPoolConfiguration(null);
            RenderChoices();
            RenderSlotEditor();
        }

        RefreshAncientTabs();
    }

    private static Button BuildAncientChoicePreview(
        AncientOptionConfig slot,
        IReadOnlyList<AncientRelicPoolDefinition> pools,
        Action<AncientPoolDragData> onPoolDropped,
        Action removeOption)
    {
        Button button = new()
        {
            CustomMinimumSize = new Vector2(790f, 132f),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        AncientPoolDragControls.ConfigureTarget(
            button,
            payload => !slot.PoolIds.Contains(payload.PoolId),
            onPoolDropped);

        MarginContainer margin = new()
        {
            AnchorRight = 1f,
            AnchorBottom = 1f
        };
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_top", 2);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_bottom", 2);
        button.AddChild(margin);

        Button remove = new()
        {
            Text = "\u00D7",
            TooltipText = "Remove option",
            CustomMinimumSize = new Vector2(34f, 34f),
            AnchorLeft = 1f,
            AnchorRight = 1f,
            OffsetLeft = -38f,
            OffsetRight = -4f,
            OffsetTop = 4f,
            OffsetBottom = 38f,
            ZIndex = 50,
            Modulate = Colors.IndianRed
        };
        remove.Pressed += removeOption;
        button.AddChild(remove);

        VBoxContainer column = new()
        {
            AnchorRight = 1f,
            AnchorBottom = 1f
        };
        margin.AddChild(column);

        HBoxContainer titleRow = new()
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        column.AddChild(titleRow);

        Label optionLabel = new()
        {
            Text = $"Option {slot.OptionIndex + 1}",
            CustomMinimumSize = new Vector2(132f, 28f),
            Modulate = Colors.Gold
        };
        titleRow.AddChild(optionLabel);

        Label relicTitle = new()
        {
            Text = slot.PoolIds.Count == 0
                ? "Drag a pool here to add it"
                : $"A {JoinWithOr(slot.PoolIds.Select(poolId => AncientRelicPools.Get(poolId)?.DisplayName ?? poolId))} relic",
            Modulate = slot.PoolIds.Count == 0 ? Colors.Gray : Colors.LightGreen,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        titleRow.AddChild(relicTitle);

        HBoxContainer icons = new()
        {
            CustomMinimumSize = new Vector2(220f, 96f),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        foreach (AncientRelicPoolDefinition pool in pools)
        {
            icons.AddChild(AncientRelicPoolIcon.Create(
                pool,
                draggable: true,
                sourceOption: slot.OptionIndex));
        }
        column.AddChild(icons);

        AddDragOverlay(
            button,
            "Drop here to add pool to option",
            payload => !slot.PoolIds.Contains(payload.PoolId));

        return button;
    }

    private static Button BuildAddTile(
        Vector2 minimumSize,
        string tooltip,
        Action pressed,
        bool expandHorizontal = false)
    {
        Button tile = new()
        {
            Text = "+",
            TooltipText = tooltip,
            CustomMinimumSize = minimumSize,
            SizeFlagsHorizontal = expandHorizontal
                ? Control.SizeFlags.ExpandFill
                : Control.SizeFlags.ShrinkBegin,
            Modulate = new Color(0.78f, 0.84f, 0.86f, 0.9f)
        };
        tile.AddThemeFontSizeOverride("font_size", 34);

        StyleBoxFlat normal = new()
        {
            BgColor = new Color(0.08f, 0.10f, 0.11f, 0.72f),
            BorderColor = new Color(0.42f, 0.49f, 0.51f, 0.72f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        StyleBoxFlat hover = (StyleBoxFlat)normal.Duplicate();
        hover.BgColor = new Color(0.13f, 0.20f, 0.20f, 0.9f);
        hover.BorderColor = new Color(0.47f, 0.88f, 0.72f, 0.95f);
        tile.AddThemeStyleboxOverride("normal", normal);
        tile.AddThemeStyleboxOverride("hover", hover);
        tile.AddThemeStyleboxOverride("pressed", hover);
        tile.Pressed += pressed;
        return tile;
    }

    private static void AddDeleteOverlay(Control target, string tooltip, Action pressed)
    {
        Button delete = new()
        {
            Text = "\u00D7",
            TooltipText = tooltip,
            Flat = true,
            CustomMinimumSize = new Vector2(28f, 28f),
            AnchorLeft = 1f,
            AnchorRight = 1f,
            OffsetLeft = -30f,
            OffsetRight = -2f,
            OffsetTop = 2f,
            OffsetBottom = 30f,
            ZIndex = 60,
            Modulate = Colors.IndianRed
        };
        delete.AddThemeFontSizeOverride("font_size", 24);
        delete.Pressed += pressed;
        target.AddChild(delete);
    }

    private static PanelContainer AddDragOverlay(
        Control target,
        string text,
        Func<AncientPoolDragData, bool> shouldShow)
    {
        PanelContainer overlay = new()
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false,
            ZIndex = 40
        };
        StyleBoxFlat style = new()
        {
            BgColor = new Color(0.08f, 0.45f, 0.32f, 0.72f),
            BorderColor = new Color(0.35f, 1f, 0.68f, 0.95f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        overlay.AddThemeStyleboxOverride("panel", style);
        overlay.AddChild(new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        });
        target.AddChild(overlay);

        AncientPoolDragControls.BindDragState(target, payload =>
        {
            overlay.Visible = payload.HasValue && shouldShow(payload.Value);
        });
        return overlay;
    }

    private static string JoinWithOr(IEnumerable<string> names)
    {
        List<string> list = names.ToList();
        return list.Count switch
        {
            0 => "no",
            1 => list[0],
            2 => $"{list[0]} or {list[1]}",
            _ => $"{string.Join(", ", list.Take(list.Count - 1))}, or {list[^1]}"
        };
    }

    private static PanelContainer BuildPoolRelicPanel(NCharacterSelectScreen screen)
    {
        PanelContainer panel = new()
        {
            CustomMinimumSize = new Vector2(560f, 600f),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        VBoxContainer root = new();
        panel.AddChild(root);
        return panel;
    }

    private static Action<AncientRelicPoolDefinition?> FillPoolRelicPanel(
        NCharacterSelectScreen screen,
        PanelContainer panel,
        Action refreshChoices,
        Func<AncientRelicPoolDefinition, RelicModel, AncientRelicPoolDefinition> addRelicToPool)
    {
        VBoxContainer root = (VBoxContainer)panel.GetChild(0);

        void ClearRoot()
        {
            foreach (Node child in root.GetChildren())
            {
                root.RemoveChild(child);
                child.QueueFree();
            }
        }

        void ShowEmptyState()
        {
            ClearRoot();
            root.AddChild(new Label
            {
                Text = "Drag a pool here to configure it",
                Modulate = Colors.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill
            });
        }

        void ShowPool(AncientRelicPoolDefinition pool)
        {
            ClearRoot();
            root.AddChild(new Label
            {
                Text = $"Pool configuration: {pool.DisplayName}"
            });

            ScrollContainer scroll = new()
            {
                SizeFlagsVertical = Control.SizeFlags.ExpandFill
            };
            root.AddChild(scroll);

            FlowContainer list = new()
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill
            };
            list.AddThemeConstantOverride("h_separation", 14);
            list.AddThemeConstantOverride("v_separation", 14);
            scroll.AddChild(list);

            foreach (RelicModel relic in pool.GetRelicsForConfiguration().DistinctBy(relic => relic.Id).OrderBy(relic => relic.Title.GetRawText()))
            {
                Control cell = BuildRelicToggleCell(screen, relic, refreshChoices);
                list.AddChild(cell);
            }

            list.AddChild(BuildAddTile(
                new Vector2(74f, 74f),
                "Add item",
                () => ShowItemPicker(pool)));
        }

        void ShowItemPicker(AncientRelicPoolDefinition pool)
        {
            ClearRoot();
            HBoxContainer heading = new()
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            root.AddChild(heading);
            heading.AddChild(new Label
            {
                Text = $"Add item to {pool.DisplayName}",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            });

            Button cancel = new()
            {
                Text = "Cancel"
            };
            cancel.Pressed += () => ShowPool(pool);
            heading.AddChild(cancel);

            LineEdit search = new()
            {
                PlaceholderText = "Search relics...",
                ClearButtonEnabled = true,
                CustomMinimumSize = new Vector2(0f, 42f),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            root.AddChild(search);

            ScrollContainer scroll = new()
            {
                SizeFlagsVertical = Control.SizeFlags.ExpandFill
            };
            root.AddChild(scroll);

            FlowContainer list = new()
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill
            };
            list.AddThemeConstantOverride("h_separation", 14);
            list.AddThemeConstantOverride("v_separation", 14);
            scroll.AddChild(list);

            void PopulateItems(string query)
            {
                foreach (Node child in list.GetChildren())
                {
                    list.RemoveChild(child);
                    child.QueueFree();
                }

                HashSet<ModelId> existingIds = pool.GetRelicsForConfiguration()
                    .Select(relic => relic.Id)
                    .ToHashSet();
                string normalizedQuery = query.Trim();
                foreach (RelicModel relic in ModelDb.AllRelics
                             .Where(relic => !existingIds.Contains(relic.Id))
                             .Where(relic => normalizedQuery.Length == 0 ||
                                             relic.Title.GetFormattedText().Contains(
                                                 normalizedQuery,
                                                 StringComparison.OrdinalIgnoreCase) ||
                                             relic.Id.ToString().Contains(
                                                 normalizedQuery,
                                                 StringComparison.OrdinalIgnoreCase))
                             .OrderBy(relic => relic.Title.GetRawText()))
                {
                    Control cell = BuildRelicPickerCell(relic, () =>
                    {
                        AncientRelicPoolDefinition updatedPool = addRelicToPool(pool, relic);
                        ShowPool(updatedPool);
                    });
                    list.AddChild(cell);
                }
            }

            search.TextChanged += PopulateItems;
            PopulateItems(string.Empty);
            search.GrabFocus();
        }

        bool CanConfigurePool(AncientPoolDragData payload)
        {
            return AncientRelicPools.Get(payload.PoolId) != null;
        }

        void ConfigurePool(AncientPoolDragData payload)
        {
            ShowPool(AncientRelicPools.Get(payload.PoolId)!);
        }

        AncientPoolDragControls.ConfigureTarget(panel, CanConfigurePool, ConfigurePool);
        PanelContainer dropOverlay = AddDragOverlay(
            panel,
            "Drop here to configure pool",
            CanConfigurePool);
        dropOverlay.MouseFilter = Control.MouseFilterEnum.Stop;
        AncientPoolDragControls.ConfigureTarget(dropOverlay, CanConfigurePool, ConfigurePool);
        ShowEmptyState();

        return pool =>
        {
            if (pool == null)
            {
                ShowEmptyState();
            }
            else
            {
                ShowPool(pool);
            }
        };
    }

    private static Control BuildRelicPickerCell(RelicModel relic, Action selected)
    {
        Control cell = new()
        {
            CustomMinimumSize = new Vector2(74f, 74f)
        };

        NRelicBasicHolder? holder = NRelicBasicHolder.Create(relic);
        if (holder == null)
        {
            return cell;
        }

        holder.AnchorRight = 1f;
        holder.AnchorBottom = 1f;
        holder.Released += _ => selected();
        cell.AddChild(holder);
        return cell;
    }

    private static void PromptForPoolName(NCharacterSelectScreen screen, Action<string> accepted)
    {
        const string dialogName = "CuztomizableAncientsPoolNameDialog";
        if (screen.HasNode(dialogName))
        {
            return;
        }

        Control overlay = new()
        {
            Name = dialogName,
            AnchorRight = 1f,
            AnchorBottom = 1f,
            ZIndex = 200
        };
        screen.AddChild(overlay);

        ColorRect backdrop = new()
        {
            Color = new Color(0f, 0.015f, 0.025f, 0.78f),
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        overlay.AddChild(backdrop);

        PanelContainer dialog = new()
        {
            AnchorLeft = 0.5f,
            AnchorRight = 0.5f,
            AnchorTop = 0.5f,
            AnchorBottom = 0.5f,
            OffsetLeft = -280f,
            OffsetRight = 280f,
            OffsetTop = -135f,
            OffsetBottom = 135f
        };
        StyleBoxFlat dialogStyle = new()
        {
            BgColor = new Color("18272d"),
            BorderColor = new Color("78919a"),
            BorderWidthLeft = 4,
            BorderWidthTop = 4,
            BorderWidthRight = 4,
            BorderWidthBottom = 4,
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5,
            CornerRadiusBottomRight = 5,
            ShadowColor = new Color(0f, 0f, 0f, 0.65f),
            ShadowSize = 12
        };
        dialog.AddThemeStyleboxOverride("panel", dialogStyle);
        overlay.AddChild(dialog);

        MarginContainer margin = new();
        margin.AddThemeConstantOverride("margin_left", 28);
        margin.AddThemeConstantOverride("margin_top", 22);
        margin.AddThemeConstantOverride("margin_right", 28);
        margin.AddThemeConstantOverride("margin_bottom", 22);
        dialog.AddChild(margin);

        VBoxContainer content = new();
        content.AddThemeConstantOverride("separation", 14);
        margin.AddChild(content);
        content.AddChild(new Label
        {
            Text = "Create custom pool",
            Modulate = Colors.Gold
        });
        content.AddChild(new Label
        {
            Text = "Pool name",
            Modulate = new Color(0.78f, 0.84f, 0.86f)
        });

        LineEdit input = new()
        {
            PlaceholderText = "Custom pool",
            CustomMinimumSize = new Vector2(0f, 52f),
            MaxLength = 64,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        StyleBoxFlat inputStyle = new()
        {
            BgColor = new Color("0d171b"),
            BorderColor = new Color("536a73"),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3,
            ContentMarginLeft = 14f,
            ContentMarginRight = 14f
        };
        StyleBoxFlat inputFocusStyle = (StyleBoxFlat)inputStyle.Duplicate();
        inputFocusStyle.BorderColor = new Color("d8b44a");
        input.AddThemeStyleboxOverride("normal", inputStyle);
        input.AddThemeStyleboxOverride("focus", inputFocusStyle);
        content.AddChild(input);

        HBoxContainer actions = new()
        {
            Alignment = BoxContainer.AlignmentMode.End
        };
        actions.AddThemeConstantOverride("separation", 12);
        content.AddChild(actions);

        Button cancel = new()
        {
            Text = "Cancel",
            CustomMinimumSize = new Vector2(120f, 46f)
        };
        actions.AddChild(cancel);

        Button create = new()
        {
            Text = "Create pool",
            CustomMinimumSize = new Vector2(150f, 46f),
            Disabled = true,
            Modulate = Colors.Gold
        };
        actions.AddChild(create);

        void Submit()
        {
            string name = input.Text.Trim();
            if (name.Length == 0)
            {
                return;
            }

            accepted(name);
            overlay.QueueFree();
        }

        input.TextChanged += text => create.Disabled = string.IsNullOrWhiteSpace(text);
        input.TextSubmitted += _ => Submit();
        create.Pressed += Submit;
        cancel.Pressed += overlay.QueueFree;
        Callable.From(input.GrabFocus).CallDeferred();
    }

    private static Control BuildRelicToggleCell(NCharacterSelectScreen screen, RelicModel relic, Action refreshChoices)
    {
        Control cell = new()
        {
            CustomMinimumSize = new Vector2(74f, 74f)
        };

        NRelicBasicHolder? holder = NRelicBasicHolder.Create(relic);
        if (holder == null)
        {
            return cell;
        }

        Label marker = new()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(22f, 22f),
            AnchorLeft = 1f,
            AnchorRight = 1f,
            OffsetLeft = -24f,
            OffsetRight = -2f,
            OffsetTop = 2f,
            OffsetBottom = 24f,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        holder.AnchorRight = 1f;
        holder.AnchorBottom = 1f;
        holder.Released += _ =>
        {
            if (AncientRelicConfigService.ActiveConfig.DisabledRelics.Contains(relic.Id))
            {
                AncientRelicConfigService.ActiveConfig.DisabledRelics.Remove(relic.Id);
            }
            else
            {
                AncientRelicConfigService.ActiveConfig.DisabledRelics.Add(relic.Id);
            }

            RefreshRelicToggleVisual(holder, marker, relic);
            refreshChoices();
        };
        cell.AddChild(holder);
        cell.AddChild(marker);

        RefreshRelicToggleVisual(holder, marker, relic);
        return cell;
    }

    private static void RefreshRelicToggleVisual(NRelicBasicHolder holder, Label marker, RelicModel relic)
    {
        bool enabled = !AncientRelicConfigService.ActiveConfig.DisabledRelics.Contains(relic.Id);
        holder.Modulate = enabled ? Colors.White : new Color(1f, 1f, 1f, 0.5f);
        marker.Text = enabled ? "✓" : "×";
        marker.Modulate = enabled ? Colors.LimeGreen : Colors.IndianRed;
    }

    private static Texture2D? TryGetAncientChatIcon(AncientEventModel ancient)
    {
        try
        {
            return ancient.RunHistoryIcon;
        }
        catch (Exception exception)
        {
            CuztomizableAncientsMod.Logger.Warn($"Could not load icon for ancient {ancient.Id}: {exception.Message}");
            return null;
        }
    }
}

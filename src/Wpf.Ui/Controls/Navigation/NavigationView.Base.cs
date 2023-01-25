﻿// Based on Windows UI Library
// Copyright(c) Microsoft Corporation.All rights reserved.

// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using Wpf.Ui.Common;

namespace Wpf.Ui.Controls.Navigation;

// https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.navigationview?view=winrt-22621

/// <summary>
/// Represents a container that enables navigation of app content. It has a header, a view for the main content, and a menu pane for navigation commands.
/// </summary>
[ToolboxItem(true)]
[System.Drawing.ToolboxBitmap(typeof(NavigationView), "NavigationView.bmp")]
public partial class NavigationView : System.Windows.Controls.Control, INavigationView
{
    private readonly ObservableCollection<string> _autoSuggestBoxItems = new();
    private readonly ObservableCollection<NavigationViewBreadcrumbItem> _breadcrumbBarItems = new();

    protected Dictionary<string, INavigationViewItem> PageIdOrTargetTagNavigationViewsDictionary = new();
    protected Dictionary<Type, INavigationViewItem> PageTypeNavigationViewsDictionary = new();

    /// <inheritdoc/>
    public INavigationViewItem? SelectedItem { get; private set; }

    /// <summary>
    /// Static constructor which overrides default property metadata.
    /// </summary>
    static NavigationView()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NavigationView), new FrameworkPropertyMetadata(typeof(NavigationView)));
    }

    public NavigationView()
    {
        NavigationParent = this;

        SetValue(MenuItemsProperty, new ObservableCollection<object>());
        SetValue(FooterMenuItemsProperty, new ObservableCollection<object>());

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
    }

    /// <inheritdoc />
    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        if (Header is BreadcrumbBar breadcrumbBar)
        {
            breadcrumbBar.ItemsSource = _breadcrumbBarItems;
            breadcrumbBar.ItemTemplate ??= Application.Current.TryFindResource("NavigationViewItemDataTemplate") as DataTemplate;
            breadcrumbBar.ItemClicked += BreadcrumbBarOnItemClicked;
        }

        if (AutoSuggestBox is not null)
        {
            AutoSuggestBox.ItemsSource = _autoSuggestBoxItems;
            AutoSuggestBox.SuggestionChosen += AutoSuggestBoxOnSuggestionChosen;
        }

        InvalidateArrange();
        InvalidateVisual();
        UpdateLayout();

        UpdateAutoSuggestBoxSuggestions();
        UpdateSelectionForMenuItems();

        AddItemsToDictionaries();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // TODO: Refresh
    }

    /// <summary>
    /// This virtual method is called when this element is detached form a loaded tree.
    /// </summary>
    protected virtual void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        Unloaded -= OnUnloaded;
        SizeChanged -= OnSizeChanged;

        if (AutoSuggestBox is not null)
            AutoSuggestBox.SuggestionChosen -= AutoSuggestBoxOnSuggestionChosen;

        if (Header is BreadcrumbBar breadcrumbBar)
        {
            breadcrumbBar.ItemClicked -= BreadcrumbBarOnItemClicked;
        }
    }

    /// <summary>
    /// This virtual method is called when ActualWidth or ActualHeight (or both) changed.
    /// </summary>
    protected virtual void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // TODO: Update reveal
    }

    /// <summary>
    /// This virtual method is called when <see cref="BackButton"/> is clicked.
    /// </summary>
    protected virtual void OnBackButtonClick(object sender, RoutedEventArgs e) => GoBack();

    /// <summary>
    /// This virtual method is called when <see cref="ToggleButton"/> is clicked.
    /// </summary>
    protected virtual void OnToggleButtonClick(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("Toggle");
    }

    /// <summary>
    /// This virtual method is called when source of the menu items is changed.
    /// </summary>
    protected virtual void OnMenuItemsChanged()
    {
        InvalidateArrange();
        InvalidateVisual();
        UpdateLayout();

        UpdateAutoSuggestBoxSuggestions();
        UpdateSelectionForMenuItems();

        AddItemsToDictionaries(MenuItems);
    }

    /// <summary>
    /// This virtual method is called when source of the footer menu items is changed.
    /// </summary>
    protected virtual void OnFooterMenuItemsChanged()
    {
        UpdateAutoSuggestBoxSuggestions();
        UpdateSelectionForMenuItems();

        AddItemsToDictionaries(FooterMenuItems);
    }

    /// <summary>
    /// This virtual method is called when <see cref="PaneDisplayMode"/> is changed.
    /// </summary>
    protected virtual void OnPaneDisplayModeChanged()
    {
        switch (PaneDisplayMode)
        {
            case NavigationViewPaneDisplayMode.LeftFluent:
                IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;
                IsPaneToggleVisible = false;
                break;
        }
    }

    /// <summary>
    /// This virtual method is called when <see cref="ItemTemplate"/> is changed.
    /// </summary>
    protected virtual void OnItemTemplateChanged()
    {
        UpdateMenuItemsTemplate();
    }

    internal void ToggleAllExpands()
    {
        // TODO: When shift clicked on navigationviewitem
    }

    internal void OnNavigationViewItemClick(NavigationViewItem navigationViewItem)
    {
        OnItemInvoked();

        NavigateInternal(navigationViewItem, null, true, false);
    }

    protected virtual void BreadcrumbBarOnItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs e)
    {
        
    }

    private void UpdateAutoSuggestBoxSuggestions()
    {
        if (AutoSuggestBox == null)
            return;

        _autoSuggestBoxItems.Clear();

        AddItemsToAutoSuggestBoxItems();
    }

    /// <summary>
    /// Navigate to the page after its name is selected in <see cref="AutoSuggestBox"/>.
    /// </summary>
    private void AutoSuggestBoxOnSuggestionChosen(object sender, RoutedEventArgs e)
    {
        if (sender is not AutoSuggestBox { ChosenSuggestion: string selectedSuggestBoxItem })
            return;

        if (string.IsNullOrEmpty(selectedSuggestBoxItem))
            return;

        if (NavigateToMenuItemFromAutoSuggestBox(MenuItems, selectedSuggestBoxItem))
            return;

        NavigateToMenuItemFromAutoSuggestBox(FooterMenuItems, selectedSuggestBoxItem);
    }

    protected virtual void AddItemsToDictionaries(IList? list)
    {
        if (list is null)
            return;

        foreach (var singleMenuItem in list)
        {
            if (singleMenuItem is not INavigationViewItem singleNavigationViewItem)
                continue;

            if (!PageIdOrTargetTagNavigationViewsDictionary.ContainsKey(singleNavigationViewItem.Id))
                PageIdOrTargetTagNavigationViewsDictionary.Add(singleNavigationViewItem.Id, singleNavigationViewItem);

            if (!PageIdOrTargetTagNavigationViewsDictionary.ContainsKey(singleNavigationViewItem.TargetPageTag))
                PageIdOrTargetTagNavigationViewsDictionary.Add(singleNavigationViewItem.TargetPageTag, singleNavigationViewItem);

            if (singleNavigationViewItem.TargetPageType is not null && !PageTypeNavigationViewsDictionary.ContainsKey(singleNavigationViewItem.TargetPageType))
                PageTypeNavigationViewsDictionary.Add(singleNavigationViewItem.TargetPageType, singleNavigationViewItem);


            if (!(singleNavigationViewItem.MenuItems?.Count > 0))
                continue;

            AddItemsToDictionaries(singleNavigationViewItem.MenuItems);
        }
    }

    protected virtual void AddItemsToDictionaries()
    {
        AddItemsToDictionaries(MenuItems);
        AddItemsToDictionaries(FooterMenuItems);
    }

    protected virtual void AddItemsToAutoSuggestBoxItems(IList? list)
    {
        if (list is null)
            return;

        foreach (var singleMenuItem in list)
        {
            if (singleMenuItem is not NavigationViewItem singleNavigationViewItem)
                continue;

            if (singleNavigationViewItem is { Content: string content, TargetPageType: { } } && !string.IsNullOrWhiteSpace(content))
                _autoSuggestBoxItems.Add(content);

            if (!(singleNavigationViewItem.MenuItems?.Count > 0))
                continue;

            AddItemsToAutoSuggestBoxItems(singleNavigationViewItem.MenuItems);
        }
    }

    protected virtual void AddItemsToAutoSuggestBoxItems()
    {
        AddItemsToAutoSuggestBoxItems(MenuItems);
        AddItemsToAutoSuggestBoxItems(FooterMenuItems);
    }

    protected virtual bool NavigateToMenuItemFromAutoSuggestBox(IList? list, string selectedSuggestBoxItem)
    {
        if (list is null)
            return false;

        foreach (var singleMenuItem in list)
        {
            if (singleMenuItem is not NavigationViewItem singleNavigationViewItem)
                continue;

            if (singleNavigationViewItem.Content is string content && content == selectedSuggestBoxItem)
            {
                NavigateInternal(singleNavigationViewItem, null, true, false);
                singleNavigationViewItem.BringIntoView();
                singleNavigationViewItem.Focus();

                return true;
            }

            if (!(singleNavigationViewItem.MenuItems?.Count > 0))
                continue;

            NavigateToMenuItemFromAutoSuggestBox(singleNavigationViewItem.MenuItems, selectedSuggestBoxItem);
        }

        return false;
    }

    protected virtual void UpdateMenuItemsTemplate(IList? list)
    {
        if (list is null)
            return;

        foreach (var singleMenuItem in list)
        {
            if (singleMenuItem is not NavigationViewItem singleNavigationViewItem)
                continue;

            if (ItemTemplate is not null && singleNavigationViewItem.Template != ItemTemplate)
                singleNavigationViewItem.Template = ItemTemplate;
        }
    }

    protected virtual void UpdateMenuItemsTemplate()
    {
        UpdateMenuItemsTemplate(MenuItems);
        UpdateMenuItemsTemplate(FooterMenuItems);
    }

    protected virtual void UpdateSelectionForMenuItems(IList? list)
    {
        if (list is null)
            return;

        foreach (var singleMenuItem in list)
        {
            if (singleMenuItem is not NavigationViewItem navigationViewItem)
                continue;

            if (navigationViewItem == SelectedItem)
            {
                navigationViewItem.IsActive = true;

                if (navigationViewItem.Icon is SymbolIcon symbolIcon && PaneDisplayMode == NavigationViewPaneDisplayMode.LeftFluent)
                    symbolIcon.Filled = true;
            }
            else
            {
                navigationViewItem.IsActive = false;

                if (navigationViewItem.Icon is SymbolIcon symbolIcon && PaneDisplayMode == NavigationViewPaneDisplayMode.LeftFluent)
                    symbolIcon.Filled = false;
            }

            if (navigationViewItem.MenuItems is not IEnumerable enumerableSubMenuItems)
                continue;

            foreach (var singleSubMenuItem in enumerableSubMenuItems)
            {
                if (singleSubMenuItem is not NavigationViewItem navigationViewSubItem)
                    continue;

                if (!navigationViewItem.IsExpanded && navigationViewSubItem == SelectedItem)
                {
                    navigationViewItem.IsExpanded = true;
                    //navigationViewItem.BringIntoView();
                }

                navigationViewSubItem.IsActive = navigationViewSubItem == SelectedItem;
            }
        }
    }

    protected virtual void UpdateSelectionForMenuItems()
    {
        UpdateSelectionForMenuItems(MenuItems);
        UpdateSelectionForMenuItems(FooterMenuItems);
    }
}

﻿// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Based on IconElement created by Yimeng Wu licensed under MIT license.
// https://github.com/Kinnara/ModernWpf/blob/master/ModernWpf/IconElement/IconElement.cs
// Copyright (C) Ivan Dmitryiyev, Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace Wpf.Ui.Controls.IconElements;

/// <summary>
/// Represents the base class for an icon UI element.
/// </summary>
public abstract class IconElement : FrameworkElement
{
    /// <summary>
    /// Property for <see cref="Foreground"/>.
    /// </summary>
    public static readonly DependencyProperty ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner(
            typeof(IconElement),
            new FrameworkPropertyMetadata(SystemColors.ControlTextBrush,
                FrameworkPropertyMetadataOptions.Inherits,
                static (d, args) => ((IconElement)d).OnForegroundPropertyChanged(args)));

    private static readonly DependencyProperty VisualParentForegroundProperty =
        DependencyProperty.Register(
            nameof(VisualParentForeground),
            typeof(Brush),
            typeof(IconElement),
            new PropertyMetadata(null,
                static (d, args) => ((IconElement)d).OnVisualParentForegroundPropertyChanged(args)));

    /// <summary>
    /// Gets or sets a brush that describes the foreground color.
    /// </summary>
    /// <returns>
    /// The brush that paints the foreground of the control.
    /// </returns>
    [Bindable(true), Category("Appearance")]
    public Brush Foreground
    {
        get => (Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    protected Brush VisualParentForeground
    {
        get => (Brush)GetValue(VisualParentForegroundProperty);
        private set => SetValue(VisualParentForegroundProperty, value);
    }

    protected bool ShouldInheritForegroundFromVisualParent
    {
        get => _shouldInheritForegroundFromVisualParent;
        private set
        {
            if (_shouldInheritForegroundFromVisualParent == value)
                return;

            _shouldInheritForegroundFromVisualParent = value;

            if (_shouldInheritForegroundFromVisualParent)
            {
                SetBinding(VisualParentForegroundProperty,
                    new Binding
                    {
                        Path = new PropertyPath(TextElement.ForegroundProperty),
                        Source = VisualParent
                    });
            }
            else
            {
                ClearValue(VisualParentForegroundProperty);
            }

            OnShouldInheritForegroundFromVisualParentChanged();
        }
    }

    protected UIElementCollection Children
    {
        get
        {
            EnsureLayoutRoot();
            return _layoutRoot!.Children;
        }
    }

    protected override int VisualChildrenCount => 1;

    private bool _shouldInheritForegroundFromVisualParent;
    private Grid? _layoutRoot;
    private bool _isForegroundDefaultOrInherited = true;

    #region Protected methods

    protected virtual void OnVisualParentForegroundPropertyChanged(DependencyPropertyChangedEventArgs e) { }

    protected virtual void OnShouldInheritForegroundFromVisualParentChanged() { }

    protected abstract void InitializeChildren();

    #endregion

    private void OnForegroundPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not Brush newForegroundBrush)
            return;

        if (newForegroundBrush == FindResource("TextFillColorPrimaryBrush"))
            return;

        var baseValueSource = DependencyPropertyHelper.GetValueSource(this, e.Property).BaseValueSource;
        _isForegroundDefaultOrInherited = baseValueSource <= BaseValueSource.Inherited;
        UpdateShouldInheritForegroundFromVisualParent();
    }

    private void UpdateShouldInheritForegroundFromVisualParent()
    {
        ShouldInheritForegroundFromVisualParent =
            _isForegroundDefaultOrInherited &&
            Parent != null &&
            VisualParent != null &&
            Parent != VisualParent;
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        SetResourceReference(ForegroundProperty, "TextFillColorPrimaryBrush");
    }

    #region Layout methods

    private void EnsureLayoutRoot()
    {
        if (_layoutRoot != null)
            return;

        _layoutRoot = new Grid
        {
            Background = Brushes.Transparent,
            SnapsToDevicePixels = true,
        };

        InitializeChildren();
        AddVisualChild(_layoutRoot);
    }

    protected override Visual GetVisualChild(int index)
    {
        if (index != 0)
            throw new ArgumentOutOfRangeException(nameof(index));

        EnsureLayoutRoot();
        return _layoutRoot!;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        EnsureLayoutRoot();

        _layoutRoot!.Measure(availableSize);
        return _layoutRoot.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        EnsureLayoutRoot();

        _layoutRoot!.Arrange(new Rect(new Point(), finalSize));
        return finalSize;
    }

    protected override void OnVisualParentChanged(DependencyObject oldParent)
    {
        base.OnVisualParentChanged(oldParent);

        UpdateShouldInheritForegroundFromVisualParent();
    }

    #endregion
}

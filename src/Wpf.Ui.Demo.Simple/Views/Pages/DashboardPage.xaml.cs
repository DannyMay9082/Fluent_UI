﻿// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows;

namespace Wpf.Ui.Demo.Simple.Views.Pages;

/// <summary>
/// Interaction logic for DashboardPage.xaml
/// </summary>
public partial class DashboardPage
{
    private int _counter = 0;

    public DashboardPage()
    {
        InitializeComponent();

        CounterTextBlock.Text = _counter.ToString();
    }

    private void OnBaseButtonClick(object sender, RoutedEventArgs e)
    {
        CounterTextBlock.Text = (++_counter).ToString();
    }
}

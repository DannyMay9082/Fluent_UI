﻿// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Controls.Navigation;
using Wpf.Ui.Gallery.ViewModels.Pages.Navigation;

namespace Wpf.Ui.Gallery.Views.Pages.Navigation;

public partial class NavigationViewPage : INavigableView<NavigationViewViewModel>
{
    public NavigationViewViewModel ViewModel { get; }

    public NavigationViewPage(NavigationViewViewModel viewModel)
    {
        ViewModel = viewModel;

        InitializeComponent();
    }
}

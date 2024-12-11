using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;
using SandTetris.ViewModels;

namespace SandTetris.Views;

public partial class DepartmentPage : ContentPage
{
	public DepartmentPage(DepartmentPageViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is DepartmentPageViewModel viewModel)
        {
            viewModel.OnAppearing();
        }
    }
}
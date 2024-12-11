using SandTetris.ViewModels;

namespace SandTetris.Views;

public partial class DepartmentCheckInPage : ContentPage
{
	public DepartmentCheckInPage(DepartmentCheckInPageViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is DepartmentCheckInPageViewModel viewModel)
        {
            viewModel.OnAppearing(); 
        }
    }
}
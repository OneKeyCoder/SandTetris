using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SandTetris.Entities;
using SandTetris.Interfaces;
using SandTetris.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SandTetris.ViewModels;

public partial class DepartmentCheckInPageViewModel : ObservableObject
{
    [ObservableProperty]
    ObservableCollection<Department> departments = new ObservableCollection<Department>();

    [ObservableProperty]
    string searchbar = "";

    public DepartmentCheckInPageViewModel(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async void OnAppearing()
    {
        await LoadDepartments();
    }

    private async Task LoadDepartments()
    {
        var departmentList = await _departmentRepository.GetDepartmentsAsync();
        Departments = new ObservableCollection<Department>(departmentList);
    }

    private readonly IDepartmentRepository _departmentRepository;
    private Department selectedDepartment = null;

    [RelayCommand]
    public void ItemSelected(Department department)
    {
        selectedDepartment = department;
    }

    [RelayCommand]
    async Task Search()
    {
        var tdepartments = await _departmentRepository.GetDepartmentsAsync();
        if (!string.IsNullOrWhiteSpace(Searchbar))
        {
            tdepartments = tdepartments.Where(d =>
                d.Name.Contains(Searchbar, StringComparison.OrdinalIgnoreCase)
                || d.Id.Contains(Searchbar, StringComparison.OrdinalIgnoreCase));
        }

        Departments.Clear();
        foreach (var department in tdepartments)
        {
            Departments.Add(department);
        }
    }

    [RelayCommand]
    async Task Detail()
    {
        if (selectedDepartment == null)
        {
            await Shell.Current.DisplayAlert("Error", "Please select a department", "OK");
            return;
        }
        await Shell.Current.GoToAsync($"{nameof(CheckInDetailPage)}", new Dictionary<string, object>
        {
            { "departmentId", selectedDepartment.Id }
        });
    }

}
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SandTetris.Entities;
using SandTetris.Interfaces;
using SandTetris.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SandTetris.ViewModels;

public partial class DepartmentPageViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty]
    private string searchbar = "";

    [ObservableProperty]    
    private ObservableCollection<Department> departments = new ObservableCollection<Department>();

    public DepartmentPageViewModel(IDepartmentRepository departmentRepository)
    {
        _idepartmentRepository = departmentRepository;
        OnAppearing();
    }

    public async void OnAppearing()
    {
        await LoadDepartments();
    }

    private readonly IDepartmentRepository _idepartmentRepository;

    private Department selectedDepartment = null!;

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("add"))
        {
            var newDepartment = (Department)query["add"];
            query.Remove("add");
            await _idepartmentRepository.AddDepartmentAsync(newDepartment);
            Departments.Add(newDepartment);
        }
        if (query.ContainsKey("edit"))
        {
            var updatedDepartment = (Department)query["edit"];
            query.Remove("edit");
            var existingDepartment = Departments.FirstOrDefault(d => d.Id == updatedDepartment.Id);
            if (existingDepartment != null)
            {
                await _idepartmentRepository.UpdateDepartmentAsync(updatedDepartment);
                var index = Departments.IndexOf(existingDepartment);
                Departments[index] = updatedDepartment;
            }
        }
    }

    private async Task LoadDepartments()
    { 
        var departments = await _idepartmentRepository.GetDepartmentsAsync();
        Departments = new ObservableCollection<Department>(departments);
    }

    [RelayCommand]
    public void OnItemSelected(Department department)
    {
        selectedDepartment = department;
    }

    [RelayCommand]
    async Task Search()
    {
        var tdepartments = await _idepartmentRepository.GetDepartmentsAsync();
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
    async Task Add()
    {
        await Shell.Current.GoToAsync($"{nameof(AddDepartmentPage)}", new Dictionary<string, object>
        {
            {"command", "add" }
        });
    }

    [RelayCommand]
    async Task Delete()
    {
        if (selectedDepartment == null)
        {
            await Shell.Current.DisplayAlert("Error", "Please select a department", "OK");
            return;
        }

        var accepted = await Shell.Current.DisplayAlert(
            "Confirm delete department", 
            $"Are you sure you want to delete department {selectedDepartment.Name}?", 
            "Delete", "Cancel"
        );
        if (!accepted) return;

        try
        {
            await _idepartmentRepository.DeleteDepartmentAsync(selectedDepartment);
            Departments.Remove(selectedDepartment);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }        
    }

    [RelayCommand]
    async Task Edit()
    {
        if (selectedDepartment == null)
        {
            await Shell.Current.DisplayAlert("Error", "Please select a department", "OK");
            return;
        }
        await Shell.Current.GoToAsync($"{nameof(AddDepartmentPage)}", new Dictionary<string, object>
        {
            {"command", "edit" },
            {"departmentID", selectedDepartment.Id }
        });
    }

    [RelayCommand]
    async Task Detail()
    {
        if (selectedDepartment == null)
        {
            await Shell.Current.DisplayAlert("Error", "Please select a department", "OK");
            return;
        }
        await Shell.Current.GoToAsync($"{nameof(EmployeePage)}", new Dictionary<string, object>
        {
            {"departmentID", selectedDepartment.Id }
        });
    }
}

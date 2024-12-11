using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using SandTetris.Entities;
using SandTetris.Interfaces;
using SandTetris.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SandTetris.Services;

namespace SandTetris.ViewModels;

public partial class ExpenditurePageViewModel : ObservableObject
{
    [ObservableProperty]
    private string searchbar = "";

    [ObservableProperty]
    private long total = 0;

    [ObservableProperty]
    private string selectedMonth = "Now";

    [ObservableProperty]
    private string selectedYear = "Now";

    [ObservableProperty]
    private ObservableCollection<string> months = new();

    [ObservableProperty]
    private ObservableCollection<string> years = new();

    [ObservableProperty]
    private ObservableCollection<SalaryDetailSummary> salaryDetailSummaries = new ObservableCollection<SalaryDetailSummary>();

    private readonly ISalaryService _salaryService;
    private readonly IDepartmentRepository _departmentRepo;
    private readonly ISalaryDetailRepository _salaryDetailRepository;
    private SalaryDetailSummary selectedSalary = new();

    public ExpenditurePageViewModel(ISalaryService salaryService, ISalaryDetailRepository salaryDetailRepository, IDepartmentRepository departmentRepo)
    {
        _salaryService = salaryService;
        _salaryDetailRepository = salaryDetailRepository;
        _departmentRepo = departmentRepo;

        Months.Add("Now");
        for (int i = 1; i <= 12; i++)
        {
            Months.Add(i.ToString());
        }

        Years.Add("Now");
        for (int i = 2020; i <= DateTime.Now.Year; i++)
        {
            Years.Add(i.ToString());
        }

        _departmentRepo = departmentRepo;
    }

    [RelayCommand]
    public void ItemSelected(SalaryDetailSummary salary)
    {
        selectedSalary = salary;
    }

    partial void OnSelectedMonthChanged(string value)
    {
        OnAppearing();
    }

    partial void OnSelectedYearChanged(string value)
    {
        OnAppearing();
    }

    public async void OnAppearing()
    {
        await LoadSalarySummaries();
    }

    private async Task LoadSalarySummaries()
    {
        if (SelectedMonth == "Now" || SelectedYear == "Now")
        {
            var salaryList = await _salaryDetailRepository.GetAllSalaryDetailSummariesAsync();
            if (salaryList.Count() == 0)
            {
                salaryList = await _salaryDetailRepository.AddSalaryDetailSummariesAsync(DateTime.Now.Month, DateTime.Now.Year);
                Total = 0;
                SalaryDetailSummaries = new ObservableCollection<SalaryDetailSummary>(salaryList);
            }
            else
            {
                Total = await _salaryService.GetTotalAll();
                SalaryDetailSummaries = new ObservableCollection<SalaryDetailSummary>(salaryList);
            }
        }
        else
        {
            var salaryList = await _salaryDetailRepository.GetSalaryDetailSummariesAsync(int.Parse(SelectedMonth), int.Parse(SelectedYear));
            Total = await _salaryService.GetTotalSalaryAsync(int.Parse(SelectedMonth), int.Parse(SelectedYear));
            SalaryDetailSummaries = new ObservableCollection<SalaryDetailSummary>(salaryList);
        }
    }

    [RelayCommand]
    public async Task Search()
    {
        
        IEnumerable<SalaryDetailSummary> salaryDetailSummaries;

        if (SelectedMonth == "Now" || SelectedYear == "Now")
        {
            salaryDetailSummaries = await _salaryDetailRepository.GetAllSalaryDetailSummariesAsync();
        }
        else
        {
            salaryDetailSummaries = await _salaryDetailRepository.GetSalaryDetailSummariesAsync(int.Parse(SelectedMonth), int.Parse(SelectedYear));
        }   

        if (!string.IsNullOrWhiteSpace(Searchbar))
        {
            salaryDetailSummaries = salaryDetailSummaries.Where(d =>
                d.DepartmentId.Contains(Searchbar, StringComparison.OrdinalIgnoreCase)
                || d.DepartmentName.Contains(Searchbar, StringComparison.OrdinalIgnoreCase));
        }

        SalaryDetailSummaries.Clear();
        foreach (var salaryDetailSummary in salaryDetailSummaries)
        {
            SalaryDetailSummaries.Add(salaryDetailSummary);
        }
    }

    [RelayCommand]
    async Task Detail()
    {
        if (selectedSalary == null)
        {
            await Shell.Current.DisplayAlert("Error", "Please select a salary", "OK");
            return;
        }
        await Shell.Current.GoToAsync($"{nameof(SalaryPage)}", new Dictionary<string, object>
        {
            { "salarySummary", selectedSalary }
        });
    }

    [ObservableProperty]
    private ObservableCollection<ISeries> serie = new();

    partial void OnSalaryDetailSummariesChanged(ObservableCollection<SalaryDetailSummary> value)
    {
        UpdateSeries();
    }

    private void UpdateSeries()
    {
        Serie.Clear();
        foreach (var summary in SalaryDetailSummaries)
        {
            Serie.Add(new PieSeries<double>
            {
                Values = new[] { (double)summary.TotalSpent },
                Name = summary.DepartmentName
            });
        }
    }

    [RelayCommand]
    async Task Export()
    {
        ExportFilePDF export = new ExportFilePDF(_salaryDetailRepository, _departmentRepo);

        if (int.TryParse(SelectedMonth, out int month) && int.TryParse(SelectedYear, out int year))
            await export.ExportPDF(month, year);
        else
            await export.ExportPDF(DateTime.Now.Month, DateTime.Now.Year);
    }
}

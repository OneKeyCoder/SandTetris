using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SandTetris.Data;
using SandTetris.Entities;
using SandTetris.Interfaces;
using SandTetris.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Views;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using SandTetris.Services;
using CommunityToolkit.Maui.Storage;

namespace SandTetris.ViewModels;

public partial class CheckInDetailPageViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty]
    private string searchBar = "";

    [ObservableProperty]
    private int numberOfEmployees = 0;

    [ObservableProperty]
    private ObservableCollection<CheckInSummary> checkInSummaries = [];

    [ObservableProperty]
    private CheckInSummary selectedCheckInSummary = null;

    [ObservableProperty]
    private string selectedMonth = "Now";

    [ObservableProperty]
    private string selectedYear = "Now";

    [ObservableProperty]
    private bool showLoadingScreen = false;

    partial void OnSelectedMonthChanged(string value)
    {
        OnAppearing();
    }

    partial void OnSelectedYearChanged(string value)
    {
        OnAppearing();
    }

    [ObservableProperty]
    private ObservableCollection<string> months = new();

    [ObservableProperty]
    private ObservableCollection<string> years = new();

    private readonly ICheckInRepository _checkInRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly DatabaseService _databaseService;
    private string departmentId = "";

    public CheckInDetailPageViewModel(ICheckInRepository checkInRepository, IDepartmentRepository departmentRepository, DatabaseService databaseService)
    {
        _checkInRepository = checkInRepository;
        _departmentRepository = departmentRepository;
        _databaseService = databaseService;

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
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        departmentId = (string)query["departmentId"];
        NumberOfEmployees = await _departmentRepository.GetTotalDepartmentEmployees(departmentId);
        OnAppearing();
    }

    private async void OnAppearing()
    {
        await LoadCheckInSummaries();
    }

    private async Task LoadCheckInSummaries()
    {
        if (SelectedMonth == "Now" || SelectedYear == "Now")
        {
            var checkInList = await _checkInRepository.GetAllCheckInSummariesAsync(departmentId);
            CheckInSummaries = new ObservableCollection<CheckInSummary>(checkInList);
        } 
        else
        {
            var checkInList = await _checkInRepository.GetCheckInSummariesAsync(departmentId, int.Parse(SelectedMonth), int.Parse(SelectedYear));
            CheckInSummaries = new ObservableCollection<CheckInSummary>(checkInList);
        }
    }

    [RelayCommand]
    async Task Search()
    {
        IEnumerable<CheckInSummary> checkinSummaries;

        if (SelectedMonth == "Now" || SelectedYear == "Now")
        {
            checkinSummaries = await _checkInRepository.GetAllCheckInSummariesAsync(departmentId);
        }
        else
        {
            checkinSummaries = await _checkInRepository.GetCheckInSummariesAsync(departmentId, int.Parse(SelectedMonth), int.Parse(SelectedYear));
        }

        if (int.TryParse(SearchBar, out int searchValue))
        {
            checkinSummaries = checkinSummaries.Where(d =>
                d.Day == searchValue || d.Month == searchValue || d.Year == searchValue);
        }

        CheckInSummaries.Clear();
        foreach (var checkInSummary in checkinSummaries)
        {
            CheckInSummaries.Add(checkInSummary);
        }
    }


    [RelayCommand]
    public void ItemSelected(CheckInSummary checkIn)
    {
        SelectedCheckInSummary = checkIn;
    }

    [RelayCommand]
    async Task Add()
    {
        if (NumberOfEmployees == 0)
        {
            await Shell.Current.DisplayAlert("Error", "No employee in this department", "OK");
            return;
        }

        bool result = await Shell.Current.DisplayAlert("Hello", "Please select the day for check-in", "Today", "From Past");
        if (result)
        {
            bool isValid = await _checkInRepository.CheckValidDayAsync(departmentId, DateTime.Now.Day, DateTime.Now.Month, DateTime.Now.Year);
            if (!isValid)
            {
                await Shell.Current.DisplayAlert("Error", "This day already exists", "OK");
                return;
            }

            await _checkInRepository.AddCheckInsForDepartmentAsync(departmentId, DateTime.Now.Day, DateTime.Now.Month, DateTime.Now.Year);

            CheckInSummaries.Insert(0, new CheckInSummary
            {
                Day = DateTime.Now.Day,
                Month = DateTime.Now.Month,
                Year = DateTime.Now.Year,
                TotalWorking = 0,
                TotalOnLeave = 0,
                TotalAbsent = NumberOfEmployees
            });
        }
        else
        {
            DateTime? date = await ShowDatePicker();
            if (date.HasValue)
            {
                bool isValid = await _checkInRepository.CheckValidDayAsync(departmentId, date.Value.Day, date.Value.Month, date.Value.Year);
                if (!isValid)
                {
                    await Shell.Current.DisplayAlert("Error", "This day already exists", "OK");
                    return;
                }

                await _checkInRepository.AddCheckInsForDepartmentAsync(departmentId, date.Value.Day, date.Value.Month, date.Value.Year);
                CheckInSummaries.Insert(0, new CheckInSummary
                {
                    Day = date.Value.Day,
                    Month = date.Value.Month,
                    Year = date.Value.Year,
                    TotalWorking = 0,
                    TotalOnLeave = 0,
                    TotalAbsent = NumberOfEmployees
                });
            }
        }

    }

    private async Task<DateTime?> ShowDatePicker()
    {
        var viewModel = new DatePickerPopUpViewModel();
        var popup = new DatePickerPopUp(viewModel);
        var result = await Shell.Current.CurrentPage.ShowPopupAsync(popup);
        return result as DateTime?;
    }

    [RelayCommand]
    async Task Edit()
    {
        if (SelectedCheckInSummary == null)
        {
            await Shell.Current.DisplayAlert("Error", "Please select a check-in", "OK");
            return;
        }
        await Shell.Current.GoToAsync($"{nameof(EmployeeCheckInPage)}", new Dictionary<string, object>
        {
            { "departmentId", departmentId },
            { "CheckInSummary", SelectedCheckInSummary }
        });
    }

    [RelayCommand]
    async Task Delete()
    {
        if (SelectedCheckInSummary == null)
        {
            await Shell.Current.DisplayAlert("Error", "Please select a check-in", "OK");
            return;
        }
        await _checkInRepository.DeleteCheckInForDepartmentAsync(departmentId, SelectedCheckInSummary.Day, SelectedCheckInSummary.Month, SelectedCheckInSummary.Year);
        CheckInSummaries.Remove(SelectedCheckInSummary);
    }

    [RelayCommand]
    public async Task ImportCheckInListAsync()
    {
        var result = await FilePicker.PickAsync(new PickOptions
        {
            PickerTitle = "Select Check-In Excel File",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".xlsx", ".xls" } },
                    { DevicePlatform.macOS, new[] { ".xlsx", ".xls" } },
                    { DevicePlatform.Android, new[] { ".xlsx", ".xls" } },
                    { DevicePlatform.iOS, new[] { ".xlsx", ".xls" } },
                })
        });

        if (result != null)
        {
            var excelFilePath = result.FullPath;

            if (File.Exists(excelFilePath))
            {
                ShowLoadingScreen = true;

                try
                {
                    using (var workbook = new XLWorkbook(excelFilePath))
                    {
                        var worksheet = workbook.Worksheet("CheckIns");
                        var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header

                        foreach (var row in rows)
                        {
                            var checkIn = new CheckIn
                            {
                                EmployeeId = row.Cell(1).GetString(),
                                Day = row.Cell(2).GetValue<int>(),
                                Month = row.Cell(3).GetValue<int>(),
                                Year = row.Cell(4).GetValue<int>(),
                                Status = Enum.Parse<CheckInStatus>(row.Cell(5).GetString()),
                                CheckInTime = row.Cell(6).GetDateTime(),
                                Note = row.Cell(7).GetString()
                            };

                            await _databaseService.AddOrUpdateCheckInAsync(checkIn);
                        }
                    }
                    await LoadCheckInSummaries();
                    await Shell.Current.DisplayAlert("Success", "Check-In data imported successfully.", "OK");
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Error", $"Failed to import Check-In data: {ex.Message}", "OK");
                }
                finally
                {
                    ShowLoadingScreen = false;
                }
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Selected Excel file does not exist.", "OK");
            }
        }
    }

    [RelayCommand]
    public async Task ExportCheckInListAsync()
    {
        try
        {
            ShowLoadingScreen = true;

            var result = await FolderPicker.Default.PickAsync();

            if (result != null && result.Folder != null && !string.IsNullOrEmpty(result.Folder.Path))
            {
                var folderPath = result.Folder.Path;

                // Define the file name. You can customize this as needed.
                string fileName = $"CheckInData_{departmentId}.xlsx";
                string filePath = Path.Combine(folderPath, fileName);

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("CheckIns");
                    // Add headers
                    worksheet.Cell(1, 1).Value = "Mã nhân viên";
                    worksheet.Cell(1, 2).Value = "Ngày";
                    worksheet.Cell(1, 3).Value = "Tháng";
                    worksheet.Cell(1, 4).Value = "Năm";
                    worksheet.Cell(1, 5).Value = "Trạng thái";
                    worksheet.Cell(1, 6).Value = "Thời gian điểm danh";
                    worksheet.Cell(1, 7).Value = "Ghi chú";

                    int row = 2;
                    foreach (var checkIn in _databaseService.DataContext.CheckIns
                            .Where(ci => ci.Employee.DepartmentId == departmentId))
                    {
                        worksheet.Cell(row, 1).Value = checkIn.EmployeeId;
                        worksheet.Cell(row, 2).Value = checkIn.Day;
                        worksheet.Cell(row, 3).Value = checkIn.Month;
                        worksheet.Cell(row, 4).Value = checkIn.Year;
                        worksheet.Cell(row, 5).Value = checkIn.Status.ToString();
                        worksheet.Cell(row, 6).Value = checkIn.CheckInTime;
                        worksheet.Cell(row, 7).Value = checkIn.Note;
                        row++;
                    }

                    worksheet.Columns().AdjustToContents();

                    workbook.SaveAs(filePath);
                }

                await Shell.Current.DisplayAlert("Success", $"Check-In data exported to {filePath} successfully.", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Cancelled", "Export operation was cancelled.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to export Check-In data: {ex.Message}", "OK");
        }
        finally
        {
            ShowLoadingScreen = false;
        }
    }
}
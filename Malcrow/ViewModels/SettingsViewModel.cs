using System.Collections.ObjectModel;
using System.ComponentModel;

public class SettingsViewModel : INotifyPropertyChanged
{
    private ObservableCollection<string> _categories;
    private ObservableCollection<string> _selectedCategories;
    private bool _enableNotifications;

    public ObservableCollection<string> Categories
    {
        get { return _categories; }
        set
        {
            _categories = value;
            OnPropertyChanged(nameof(Categories));
        }
    }

    public ObservableCollection<string> SelectedCategories
    {
        get { return _selectedCategories; }
        set
        {
            _selectedCategories = value;
            OnPropertyChanged(nameof(SelectedCategories));
        }
    }

    public bool EnableNotifications
    {
        get { return _enableNotifications; }
        set
        {
            _enableNotifications = value;
            OnPropertyChanged(nameof(EnableNotifications));
        }
    }

    public SettingsViewModel()
    {
        Categories = new ObservableCollection<string>
        {
            "Debuggers",
            "Decompilers",
            "VirtualMachines",
            "SandboxingTools",
            "SystemMonitoringTools",
            "AntivirusSoftware",
            "Random"
        };
        SelectedCategories = new ObservableCollection<string>();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

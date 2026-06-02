using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ModelingAppWPF;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly List<SceneObject> _selectedObjects = new();
    private SceneObject? _selectedObject;
    private string? _currentProjectPath;

    public MainViewModel()
    {
        NewProjectCommand = new RelayCommand(_ => NewProjectRequested?.Invoke());
        OpenProjectCommand = new RelayCommand(_ => OpenProjectRequested?.Invoke());
        SaveProjectCommand = new RelayCommand(_ => SaveProjectRequested?.Invoke());
        SaveProjectAsCommand = new RelayCommand(_ => SaveProjectAsRequested?.Invoke());
        ImportModelCommand = new RelayCommand(_ => ImportModelRequested?.Invoke());
        SelectAllCommand = new RelayCommand(_ => RequestSelectAll(), _ => SceneObjects.Count > 0);
        DeleteSelectedCommand = new RelayCommand(_ => RequestDeleteSelected(), _ => SelectedObjects.Count > 0);
        DuplicateSelectedCommand = new RelayCommand(_ => RequestDuplicateSelected(), _ => SelectedObjects.Count > 0);
    }

    public ObservableCollection<SceneObject> SceneObjects { get; } = new();

    public IReadOnlyList<SceneObject> SelectedObjects => _selectedObjects;

    public SceneObject? SelectedObject
    {
        get => _selectedObject;
        private set { _selectedObject = value; OnPropertyChanged(); }
    }

    public string? CurrentProjectPath
    {
        get => _currentProjectPath;
        set { _currentProjectPath = value; OnPropertyChanged(); }
    }

    public ICommand NewProjectCommand { get; }
    public ICommand OpenProjectCommand { get; }
    public ICommand SaveProjectCommand { get; }
    public ICommand SaveProjectAsCommand { get; }
    public ICommand ImportModelCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
    public ICommand DuplicateSelectedCommand { get; }

    public event Action? NewProjectRequested;
    public event Action? OpenProjectRequested;
    public event Action? SaveProjectRequested;
    public event Action? SaveProjectAsRequested;
    public event Action? ImportModelRequested;
    public event Action<IReadOnlyList<SceneObject>>? SelectObjectsRequested;
    public event Action<IReadOnlyList<SceneObject>>? DeleteSelectedRequested;
    public event Action<IReadOnlyList<SceneObject>>? DuplicateSelectedRequested;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SetSelectedObjects(IEnumerable<SceneObject> selected)
    {
        _selectedObjects.Clear();
        _selectedObjects.AddRange(selected.Where(SceneObjects.Contains).Distinct());
        SelectedObject = _selectedObjects.Count > 0 ? _selectedObjects[^1] : null;
        OnPropertyChanged(nameof(SelectedObjects));
        RaiseCommandStates();
    }

    public void NotifySceneChanged()
    {
        OnPropertyChanged(nameof(SceneObjects));
        RaiseCommandStates();
    }

    private void RequestSelectAll()
        => SelectObjectsRequested?.Invoke(SceneObjects.ToList());

    private void RequestDeleteSelected()
    {
        if (_selectedObjects.Count > 0)
            DeleteSelectedRequested?.Invoke(_selectedObjects.ToList());
    }

    private void RequestDuplicateSelected()
    {
        if (_selectedObjects.Count > 0)
            DuplicateSelectedRequested?.Invoke(_selectedObjects.ToList());
    }

    private void RaiseCommandStates()
    {
        RaiseCanExecuteChanged(SelectAllCommand);
        RaiseCanExecuteChanged(DeleteSelectedCommand);
        RaiseCanExecuteChanged(DuplicateSelectedCommand);
    }

    private static void RaiseCanExecuteChanged(ICommand command)
    {
        if (command is RelayCommand relay)
            relay.RaiseCanExecuteChanged();
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using glove_e.Models;
using glove_e.Services;

namespace glove_e.ViewModels;

/// <summary>ViewModel del historial: lecturas de distancia y alertas enviadas.</summary>
public partial class HistoryViewModel : ObservableObject
{
    private readonly IDatabaseService _database;

    public ObservableCollection<DistanceReading> Readings { get; } = new();
    public ObservableCollection<AlertEvent> Alerts { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrandoDistancias))]
    private bool mostrandoAlertas;

    public bool MostrandoDistancias => !MostrandoAlertas;

    [ObservableProperty]
    private bool isBusy;

    public HistoryViewModel(IDatabaseService database)
    {
        _database = database;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var lecturas = await _database.GetReadingsAsync();
            Readings.Clear();
            foreach (var r in lecturas)
                Readings.Add(r);

            var alertas = await _database.GetAlertsAsync();
            Alerts.Clear();
            foreach (var a in alertas)
                Alerts.Add(a);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void VerDistancias() => MostrandoAlertas = false;

    [RelayCommand]
    private void VerAlertas() => MostrandoAlertas = true;

    [RelayCommand]
    private async Task ClearReadingsAsync()
    {
        var ok = await Shell.Current.DisplayAlert("Borrar historial",
            "¿Eliminar todas las lecturas de distancia?", "Sí, borrar", "Cancelar");
        if (!ok) return;

        await _database.ClearReadingsAsync();
        Readings.Clear();
    }
}

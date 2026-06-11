using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace SmartComputerControlAvalonia.Views;

public partial class ShutdownConfirmWindow : Window
{
    private DispatcherTimer? _timer;
    private int _secondsLeft;
    private readonly int _totalSeconds;
    private bool _decisionTaken;

    /// <summary>
    /// Risultato del dialog:
    ///   true  → procedi con lo spegnimento (timer scaduto o pulsante "Spegni subito")
    ///   false → annulla (pulsante "Annulla" o finestra chiusa con X)
    /// </summary>
    public bool ShouldShutdown { get; private set; }

    public ShutdownConfirmWindow() : this(30) { }

    public ShutdownConfirmWindow(int secondsBeforeShutdown)
    {
        InitializeComponent();
        _totalSeconds = secondsBeforeShutdown;
        _secondsLeft = secondsBeforeShutdown;
        CountdownBar.Maximum = _totalSeconds;
        CountdownBar.Value = _secondsLeft;
        UpdateText();

        // Quando la finestra viene chiusa con la X = annulla
        Closing += (s, e) =>
        {
            if (!_decisionTaken)
            {
                _decisionTaken = true;
                ShouldShutdown = false;
                StopTimer();
            }
        };
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        StartTimer();
        // Porta la finestra in primo piano (non sempre rispettato da Topmost solo)
        Activate();
    }

    private void StartTimer()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (s, e) =>
        {
            _secondsLeft--;
            CountdownBar.Value = _secondsLeft;
            UpdateText();
            if (_secondsLeft <= 0)
            {
                _decisionTaken = true;
                ShouldShutdown = true;
                StopTimer();
                Close();
            }
        };
        _timer.Start();
    }

    private void StopTimer()
    {
        _timer?.Stop();
        _timer = null;
    }

    private void UpdateText()
    {
        if (_secondsLeft == 1)
            CountdownText.Text = "Il computer si spegnerà tra 1 secondo";
        else if (_secondsLeft <= 0)
            CountdownText.Text = "Spegnimento in corso...";
        else
            CountdownText.Text = $"Il computer si spegnerà tra {_secondsLeft} secondi";
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        _decisionTaken = true;
        ShouldShutdown = false;
        StopTimer();
        Close();
    }

    private void OnShutdownNowClicked(object? sender, RoutedEventArgs e)
    {
        _decisionTaken = true;
        ShouldShutdown = true;
        StopTimer();
        Close();
    }
}

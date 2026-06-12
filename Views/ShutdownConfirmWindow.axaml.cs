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
    private readonly string _actionName;
    private bool _decisionTaken;

    /// <summary>
    /// Risultato del dialog:
    ///   true  → procedi (timer scaduto o pulsante "Esegui subito")
    ///   false → annulla (pulsante "Annulla" o finestra chiusa con X)
    /// </summary>
    public bool ShouldShutdown { get; private set; }

    public ShutdownConfirmWindow() : this(30, null) { }

    public ShutdownConfirmWindow(int secondsBeforeShutdown, Services.ParsedCommand? cmd = null)
    {
        InitializeComponent();
        _totalSeconds = secondsBeforeShutdown;
        _secondsLeft = secondsBeforeShutdown;
        _actionName = cmd?.DisplayName ?? "Spegnimento";

        // Aggiorna le scritte del dialog in base al comando
        this.Title = $"{_actionName} in arrivo";
        if (this.FindControl<TextBlock>("HeaderTitle") is { } h)
            h.Text = _actionName;
        if (this.FindControl<Button>("ShutdownNowButton") is { } b)
            b.Content = cmd?.Type == Services.CommandType.Restart ? "Riavvia subito" : "Spegni subito";

        CountdownBar.Maximum = _totalSeconds;
        CountdownBar.Value = _secondsLeft;
        UpdateText();

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
        var verb = _actionName.ToLowerInvariant();
        if (_secondsLeft == 1)
            CountdownText.Text = $"{_actionName} tra 1 secondo";
        else if (_secondsLeft <= 0)
            CountdownText.Text = $"{_actionName} in corso...";
        else
            CountdownText.Text = $"{_actionName} tra {_secondsLeft} secondi";
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

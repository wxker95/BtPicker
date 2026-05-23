using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using BtPicker.Services;
using BtPicker.Tray;

namespace BtPicker;

public partial class App : System.Windows.Application
{
    private Mutex? _mutex;
    private TrayIconManager? _trayManager;
    private TextWriterTraceListener? _traceListener;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(true, "BtPicker_SingleInstance", out bool isNew);
        if (!isNew)
        {
            Shutdown();
            return;
        }

        InitializeLogging();

        var settingsService = new SettingsService();
        var settings = settingsService.Load();

        if (settings.StartWithWindows)
            AutoStartHelper.SetAutoStart(true);

        var deviceService = new BluetoothDeviceService();
        var connectionService = new BluetoothConnectionService();

        _trayManager = new TrayIconManager(settingsService, deviceService, connectionService);
        _trayManager.Initialize();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayManager?.Dispose();
        Trace.Flush();
        _traceListener?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }

    private void InitializeLogging()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BtPicker");
        Directory.CreateDirectory(logDir);
        var logPath = Path.Combine(logDir, "btpicker.log");

        var stream = new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        _traceListener = new TextWriterTraceListener(stream) { TraceOutputOptions = TraceOptions.None };
        Trace.Listeners.Add(_traceListener);
        Trace.AutoFlush = true;

        Trace.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] BtPicker started");
    }
}

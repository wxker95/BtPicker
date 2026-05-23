using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BtPicker.Models;
using BtPicker.Services;

namespace BtPicker.Tray;

public class TrayIconManager : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly BluetoothDeviceService _deviceService;
    private readonly BluetoothConnectionService _connectionService;
    private readonly NotifyIcon _notifyIcon;
    private AppSettings _settings;

    public TrayIconManager(
        SettingsService settingsService,
        BluetoothDeviceService deviceService,
        BluetoothConnectionService connectionService)
    {
        _settingsService = settingsService;
        _deviceService = deviceService;
        _connectionService = connectionService;
        _settings = _settingsService.Load();
        _notifyIcon = new NotifyIcon();
    }

    public void Initialize()
    {
        _notifyIcon.Icon = CreateBluetoothIcon();
        _notifyIcon.Text = "BtPicker";
        _notifyIcon.Visible = true;
        _notifyIcon.ContextMenuStrip = new ContextMenuStrip();
        _notifyIcon.ContextMenuStrip.Opening += OnMenuOpening;
    }

    private async void OnMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        var menu = _notifyIcon.ContextMenuStrip!;
        menu.Items.Clear();

        List<BluetoothDeviceModel> devices;
        try
        {
            devices = await _deviceService.GetPairedDevicesAsync();
        }
        catch
        {
            menu.Items.Add(new ToolStripMenuItem("Bluetooth unavailable") { Enabled = false });
            AddFooterItems(menu);
            return;
        }

        if (devices.Count == 0)
        {
            menu.Items.Add(new ToolStripMenuItem("No paired devices") { Enabled = false });
        }
        else if (_settings.GroupByType)
        {
            AddGroupedDevices(menu, devices);
        }
        else
        {
            AddFlatDevices(menu, devices);
        }

        AddFooterItems(menu);
    }

    private void AddGroupedDevices(ContextMenuStrip menu, List<BluetoothDeviceModel> devices)
    {
        var groups = new[]
        {
            (Category: DeviceCategory.Audio, Label: "Audio"),
            (Category: DeviceCategory.Input, Label: "Input"),
            (Category: DeviceCategory.Other, Label: "Other"),
        };

        foreach (var (category, label) in groups)
        {
            var categoryDevices = devices
                .Where(d => d.Category == category)
                .OrderBy(d => d.Name)
                .ToList();

            if (categoryDevices.Count == 0) continue;

            var submenu = new ToolStripMenuItem(label);
            foreach (var device in categoryDevices)
            {
                AddDeviceMenuItem(submenu.DropDownItems, device);
            }
            menu.Items.Add(submenu);
        }
    }

    private void AddFlatDevices(ContextMenuStrip menu, List<BluetoothDeviceModel> devices)
    {
        foreach (var device in devices.OrderBy(d => d.Name))
        {
            AddDeviceMenuItem(menu.Items, device);
        }
    }

    private void AddDeviceMenuItem(ToolStripItemCollection parent, BluetoothDeviceModel device)
    {
        var status = device.IsConnected ? "● Connected" : "○ Disconnected";
        var text = $"{device.Name}  {status}";
        var item = new ToolStripMenuItem(text)
        {
            Tag = device
        };
        item.Click += OnDeviceClicked;
        parent.Add(item);

        if (device.BatteryLevel.HasValue)
        {
            var batteryItem = new ToolStripMenuItem($"    \U0001f50b {device.BatteryLevel}%")
            {
                Enabled = false
            };
            parent.Add(batteryItem);
        }
    }

    private async void OnDeviceClicked(object? sender, EventArgs e)
    {
        var menuItem = (ToolStripMenuItem)sender!;
        var device = (BluetoothDeviceModel)menuItem.Tag!;

        var action = device.IsConnected ? "Disconnecting from" : "Connecting to";
        _notifyIcon.ShowBalloonTip(0, "BtPicker", $"{action} {device.Name}...", ToolTipIcon.Info);

        try
        {
            await _connectionService.ToggleConnectionAsync(device);
            var result = device.IsConnected ? "Disconnected from" : "Connected to";
            _notifyIcon.ShowBalloonTip(3000, "BtPicker", $"{result} {device.Name}", ToolTipIcon.Info);
        }
        catch
        {
            var failAction = device.IsConnected ? "disconnect from" : "connect to";
            _notifyIcon.ShowBalloonTip(3000, "BtPicker", $"Failed to {failAction} {device.Name}", ToolTipIcon.Error);
        }
    }

    private void AddFooterItems(ContextMenuStrip menu)
    {
        menu.Items.Add(new ToolStripSeparator());

        var settingsItem = new ToolStripMenuItem("Bluetooth Settings");
        settingsItem.Click += (_, _) =>
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ms-settings:bluetooth",
                UseShellExecute = true
            });
        };
        menu.Items.Add(settingsItem);

        var groupItem = new ToolStripMenuItem("Group by type")
        {
            Checked = _settings.GroupByType,
            CheckOnClick = true
        };
        groupItem.CheckedChanged += (_, _) =>
        {
            _settings.GroupByType = groupItem.Checked;
            _settingsService.Save(_settings);
        };
        menu.Items.Add(groupItem);

        var startupItem = new ToolStripMenuItem("Start with Windows")
        {
            Checked = _settings.StartWithWindows,
            CheckOnClick = true
        };
        startupItem.CheckedChanged += (_, _) =>
        {
            _settings.StartWithWindows = startupItem.Checked;
            _settingsService.Save(_settings);
            AutoStartHelper.SetAutoStart(startupItem.Checked);
        };
        menu.Items.Add(startupItem);

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) =>
        {
            _notifyIcon.Visible = false;
            System.Windows.Application.Current.Shutdown();
        };
        menu.Items.Add(exitItem);
    }

    private static Icon CreateBluetoothIcon()
    {
        var bitmap = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var pen = new Pen(Color.FromArgb(0, 120, 215), 1.5f);
            // Bluetooth rune: vertical line with crossing arrows
            g.DrawLine(pen, 8, 2, 8, 14);     // vertical center
            g.DrawLine(pen, 8, 2, 12, 6);      // top to upper-right
            g.DrawLine(pen, 12, 6, 4, 11);     // upper-right to lower-left
            g.DrawLine(pen, 8, 14, 12, 10);    // bottom to lower-right
            g.DrawLine(pen, 12, 10, 4, 5);     // lower-right to upper-left
        }
        return Icon.FromHandle(bitmap.GetHicon());
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}

public static class AutoStartHelper
{
    private const string AppName = "BtPicker";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static void SetAutoStart(bool enabled)
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key == null) return;

        if (enabled)
        {
            var exePath = Environment.ProcessPath;
            if (exePath != null)
                key.SetValue(AppName, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(AppName, throwOnMissingValue: false);
        }
    }

    public static bool IsAutoStartEnabled()
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(AppName) != null;
    }
}

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BtPicker.Models;
using BtPicker.Native;

namespace BtPicker.Services;

public class BluetoothConnectionService
{
    private static readonly HashSet<Guid> ConnectableProfiles = new()
    {
        new("0000110b-0000-1000-8000-00805f9b34fb"), // A2DP Sink
        new("0000110c-0000-1000-8000-00805f9b34fb"), // A2DP Remote Control Target
        new("0000110e-0000-1000-8000-00805f9b34fb"), // A2DP Remote Control
        new("0000111e-0000-1000-8000-00805f9b34fb"), // Handsfree (HFP)
        new("00001108-0000-1000-8000-00805f9b34fb"), // Headset (HSP)
        new("00001112-0000-1000-8000-00805f9b34fb"), // Headset Audio Gateway
        new("00001124-0000-1000-8000-00805f9b34fb"), // HID
        new("00001812-0000-1000-8000-00805f9b34fb"), // HID over GATT
    };

    public async Task ToggleConnectionAsync(BluetoothDeviceModel device)
    {
        var action = device.IsConnected ? "Disconnect" : "Connect";
        Trace.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {action} requested for '{device.Name}' (address={device.Address:X12}, connected={device.IsConnected})");

        try
        {
            if (device.IsConnected)
                await Task.Run(() => Disconnect(device.Address));
            else
                await Task.Run(() => Connect(device.Address));

            Trace.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {action} completed for '{device.Name}'");
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {action} FAILED for '{device.Name}': {ex}");
            throw;
        }
    }

    private static void Connect(ulong address)
    {
        var hRadio = GetRadioHandle();
        try
        {
            var deviceInfo = MakeDeviceInfo(address);
            var services = GetInstalledServices(hRadio, ref deviceInfo);
            var targets = services.Where(s => ConnectableProfiles.Contains(s)).ToArray();

            if (targets.Length == 0)
            {
                Trace.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] No services enumerated — using all known profiles");
                targets = ConnectableProfiles.ToArray();
            }

            ToggleServices(hRadio, ref deviceInfo, targets, BluetoothInterop.BLUETOOTH_SERVICE_DISABLE, throwOnFailure: false);
            ToggleServices(hRadio, ref deviceInfo, targets, BluetoothInterop.BLUETOOTH_SERVICE_ENABLE, throwOnFailure: true);
        }
        finally
        {
            if (hRadio != IntPtr.Zero)
                BluetoothInterop.CloseHandle(hRadio);
        }
    }

    private static void Disconnect(ulong address)
    {
        var hRadio = GetRadioHandle();
        try
        {
            var addr = address;
            if (!BluetoothInterop.DeviceIoControl(
                    hRadio,
                    BluetoothInterop.IOCTL_BTH_DISCONNECT_DEVICE,
                    ref addr,
                    sizeof(ulong),
                    IntPtr.Zero,
                    0,
                    out _,
                    IntPtr.Zero))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        finally
        {
            if (hRadio != IntPtr.Zero)
                BluetoothInterop.CloseHandle(hRadio);
        }
    }

    private static void ToggleServices(IntPtr hRadio, ref BLUETOOTH_DEVICE_INFO deviceInfo,
        Guid[] targets, uint serviceFlag, bool throwOnFailure = true)
    {
        var flagLabel = serviceFlag == BluetoothInterop.BLUETOOTH_SERVICE_ENABLE ? "ENABLE" : "DISABLE";
        Trace.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Targeting {targets.Length} service(s), flag={flagLabel}");

        int succeeded = 0;
        foreach (var service in targets)
        {
            var guid = service;
            Trace.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] BluetoothSetServiceState {guid} flag={flagLabel}");

            var result = BluetoothInterop.BluetoothSetServiceState(
                hRadio, ref deviceInfo, ref guid, serviceFlag);

            Trace.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] BluetoothSetServiceState result=0x{result:X8}");

            if (result == 0)
                succeeded++;
            else if (result == 0x57)
                Trace.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Skipping {guid} (not toggleable)");
            else
                Trace.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Warning: {guid} failed with 0x{result:X8}");
        }

        Trace.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Toggled {succeeded}/{targets.Length} services");

        if (succeeded == 0 && throwOnFailure)
            throw new InvalidOperationException("None of the device's Bluetooth services could be toggled.");
    }

    private static BLUETOOTH_DEVICE_INFO MakeDeviceInfo(ulong address) => new()
    {
        dwSize = (uint)Marshal.SizeOf<BLUETOOTH_DEVICE_INFO>(),
        Address = address
    };

    private static Guid[] GetInstalledServices(IntPtr hRadio, ref BLUETOOTH_DEVICE_INFO deviceInfo)
    {
        uint serviceCount = 0;
        uint result = BluetoothInterop.BluetoothEnumerateInstalledServices(
            hRadio, ref deviceInfo, ref serviceCount, null);

        Trace.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] BluetoothEnumerateInstalledServices(count): result=0x{result:X8}, serviceCount={serviceCount}");

        if (serviceCount == 0)
            return [];

        var services = new Guid[serviceCount];
        result = BluetoothInterop.BluetoothEnumerateInstalledServices(
            hRadio, ref deviceInfo, ref serviceCount, services);

        Trace.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] BluetoothEnumerateInstalledServices(fetch): result=0x{result:X8}, serviceCount={serviceCount}");

        if (result != 0)
            throw new Win32Exception((int)result);

        return services;
    }

    private static IntPtr GetRadioHandle()
    {
        var findParams = new BLUETOOTH_FIND_RADIO_PARAMS
        {
            dwSize = (uint)Marshal.SizeOf<BLUETOOTH_FIND_RADIO_PARAMS>()
        };

        var hFind = BluetoothInterop.BluetoothFindFirstRadio(ref findParams, out var hRadio);
        if (hFind == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "No Bluetooth radio found.");

        BluetoothInterop.BluetoothFindRadioClose(hFind);
        return hRadio;
    }
}

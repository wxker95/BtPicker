using Windows.Devices.Bluetooth;

namespace BtPicker.Models;

public enum DeviceCategory
{
    Audio,
    Input,
    Other
}

public class BluetoothDeviceModel
{
    public required string Name { get; set; }
    public required string DeviceId { get; set; }
    public ulong Address { get; set; }
    public DeviceCategory Category { get; set; }
    public bool IsConnected { get; set; }
    public int? BatteryLevel { get; set; }

    public static DeviceCategory CategorizeFromMajorClass(BluetoothMajorClass majorClass)
    {
        return majorClass switch
        {
            BluetoothMajorClass.AudioVideo => DeviceCategory.Audio,
            BluetoothMajorClass.Peripheral => DeviceCategory.Input,
            _ => DeviceCategory.Other
        };
    }
}

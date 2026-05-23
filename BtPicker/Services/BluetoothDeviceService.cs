using BtPicker.Models;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace BtPicker.Services;

public class BluetoothDeviceService
{
    public async Task<List<BluetoothDeviceModel>> GetPairedDevicesAsync()
    {
        var models = new List<BluetoothDeviceModel>();
        var selector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
        var devices = await DeviceInformation.FindAllAsync(selector);

        foreach (var deviceInfo in devices)
        {
            try
            {
                var btDevice = await BluetoothDevice.FromIdAsync(deviceInfo.Id);
                if (btDevice == null) continue;

                var model = new BluetoothDeviceModel
                {
                    Name = btDevice.Name,
                    DeviceId = deviceInfo.Id,
                    Address = btDevice.BluetoothAddress,
                    IsConnected = btDevice.ConnectionStatus == BluetoothConnectionStatus.Connected,
                    Category = BluetoothDeviceModel.CategorizeFromMajorClass(
                        btDevice.ClassOfDevice.MajorClass),
                    BatteryLevel = await TryGetBatteryLevelAsync(btDevice.BluetoothAddress)
                };
                models.Add(model);
            }
            catch
            {
                // Skip devices that fail to load
            }
        }

        return models;
    }

    private static async Task<int?> TryGetBatteryLevelAsync(ulong bluetoothAddress)
    {
        try
        {
            var bleDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(bluetoothAddress);
            if (bleDevice == null) return null;

            var gattResult = await bleDevice.GetGattServicesForUuidAsync(
                GattServiceUuids.Battery,
                BluetoothCacheMode.Cached);

            if (gattResult.Status != GattCommunicationStatus.Success ||
                gattResult.Services.Count == 0)
                return null;

            using var batteryService = gattResult.Services[0];
            var charResult = await batteryService.GetCharacteristicsForUuidAsync(
                GattCharacteristicUuids.BatteryLevel);

            if (charResult.Status != GattCommunicationStatus.Success ||
                charResult.Characteristics.Count == 0)
                return null;

            var readResult = await charResult.Characteristics[0].ReadValueAsync(
                BluetoothCacheMode.Cached);

            if (readResult.Status != GattCommunicationStatus.Success)
                return null;

            var reader = DataReader.FromBuffer(readResult.Value);
            return reader.ReadByte();
        }
        catch
        {
            return null;
        }
    }
}

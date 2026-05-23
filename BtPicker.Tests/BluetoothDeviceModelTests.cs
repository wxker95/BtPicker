using BtPicker.Models;
using Windows.Devices.Bluetooth;
using Xunit;

namespace BtPicker.Tests;

public class BluetoothDeviceModelTests
{
    [Theory]
    [InlineData(BluetoothMajorClass.AudioVideo, DeviceCategory.Audio)]
    [InlineData(BluetoothMajorClass.Peripheral, DeviceCategory.Input)]
    [InlineData(BluetoothMajorClass.Computer, DeviceCategory.Other)]
    [InlineData(BluetoothMajorClass.Phone, DeviceCategory.Other)]
    [InlineData(BluetoothMajorClass.Miscellaneous, DeviceCategory.Other)]
    [InlineData(BluetoothMajorClass.NetworkAccessPoint, DeviceCategory.Other)]
    [InlineData(BluetoothMajorClass.Imaging, DeviceCategory.Other)]
    [InlineData(BluetoothMajorClass.Wearable, DeviceCategory.Other)]
    [InlineData(BluetoothMajorClass.Toy, DeviceCategory.Other)]
    public void CategorizeFromMajorClass_ReturnsExpectedCategory(
        BluetoothMajorClass majorClass, DeviceCategory expected)
    {
        var result = BluetoothDeviceModel.CategorizeFromMajorClass(majorClass);
        Assert.Equal(expected, result);
    }
}

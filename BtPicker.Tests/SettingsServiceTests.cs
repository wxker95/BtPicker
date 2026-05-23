using System.IO;
using BtPicker.Models;
using BtPicker.Services;
using Xunit;

namespace BtPicker.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SettingsService _service;

    public SettingsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"BtPicker_Test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _service = new SettingsService(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void Load_NoFile_ReturnsDefaults()
    {
        var settings = _service.Load();
        Assert.True(settings.GroupByType);
        Assert.True(settings.StartWithWindows);
    }

    [Fact]
    public void Save_ThenLoad_RoundTrips()
    {
        var settings = new AppSettings { GroupByType = false, StartWithWindows = false };
        _service.Save(settings);
        var loaded = _service.Load();
        Assert.False(loaded.GroupByType);
        Assert.False(loaded.StartWithWindows);
    }

    [Fact]
    public void Load_CorruptFile_ReturnsDefaults()
    {
        File.WriteAllText(Path.Combine(_tempDir, "settings.json"), "not json{{{");
        var settings = _service.Load();
        Assert.True(settings.GroupByType);
        Assert.True(settings.StartWithWindows);
    }

    [Fact]
    public void Save_CreatesFileOnDisk()
    {
        _service.Save(new AppSettings());
        Assert.True(File.Exists(Path.Combine(_tempDir, "settings.json")));
    }
}

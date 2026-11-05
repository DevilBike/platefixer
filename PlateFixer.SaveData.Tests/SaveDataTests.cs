namespace PlateFixer.SaveData.Tests;

public class SaveDataTests
{
    private static byte[] SampleFile() => TestFiles.UiSave(
        0x4000174B86A9F6, padding: 512,
        (0x00, [1, 2, 3]),
        (UiSaveFile.BannerSectionId, TestFiles.BannerSection(
            TestFiles.Portrait(0, 120, 51, race: 6, checksum: 0xAAAA),
            TestFiles.Portrait(1, 256, 72, race: 4, checksum: 0xBBBB))),
        (0x18, [9]));

    [Fact]
    public void UnchangedFileWritesBackIdentically()
    {
        var raw = SampleFile();
        var file = UiSaveFile.Parse(raw);
        var banner = BannerSection.Parse(file.FindSection(UiSaveFile.BannerSectionId)!.Data);
        file.FindSection(UiSaveFile.BannerSectionId)!.Data = banner.ToBytes();

        Assert.Equal(raw, file.ToBytes());
    }

    [Fact]
    public void ReadsContentIdAndPortraits()
    {
        var file = UiSaveFile.Parse(SampleFile());
        var banner = BannerSection.Parse(file.FindSection(UiSaveFile.BannerSectionId)!.Data);

        Assert.Equal(0x4000174B86A9F6ul, file.ContentId);
        Assert.Equal(3, file.Sections.Count);
        Assert.Equal(2, banner.Portraits.Count);
        Assert.Equal((ushort)256, banner.Portraits[1].Background);
        Assert.Equal("Miqo'te", banner.Portraits[1].Appearance?.RaceName);
        Assert.Equal(0xBBBBu, banner.Portraits[1].GearChecksum);
    }

    [Fact]
    public void EditedPortraitSurvivesSaveAndOnlyThatChanges()
    {
        var file = UiSaveFile.Parse(SampleFile());
        var section = file.FindSection(UiSaveFile.BannerSectionId)!;
        var banner = BannerSection.Parse(section.Data);
        banner.Portraits[0].Pose = 99;
        banner.Portraits[1].CopyLookFrom(banner.Portraits[0]);
        section.Data = banner.ToBytes();

        var reread = UiSaveFile.Parse(file.ToBytes());
        var after = BannerSection.Parse(reread.FindSection(UiSaveFile.BannerSectionId)!.Data);

        Assert.Equal((ushort)99, after.Portraits[0].Pose);
        Assert.Equal((ushort)99, after.Portraits[1].Pose);
        Assert.Equal((ushort)120, after.Portraits[1].Background);
        Assert.Equal(0xBBBBu, after.Portraits[1].GearChecksum);
        Assert.Equal((byte)4, after.Portraits[1].Appearance?.Race);
        Assert.Equal([9], reread.FindSection(0x18)!.Data);
    }

    [Fact]
    public void UnknownFieldIsRefused()
    {
        var data = TestFiles.BannerSection([0, 0, 0, 0, 0x7E, 0x00, 1, 2]);

        Assert.Throws<InvalidDataException>(() => BannerSection.Parse(data));
    }

    [Fact]
    public void TruncatedFileIsRefused()
    {
        var raw = SampleFile();

        Assert.Throws<InvalidDataException>(() => UiSaveFile.Parse(raw.AsSpan(0, 60)));
    }

    /// <summary>Round-trips a real portrait section if one is supplied locally. Real files are never committed.</summary>
    [Fact]
    public void RealPortraitSectionRoundTrips()
    {
        var path = Environment.GetEnvironmentVariable("PLATEFIXER_REAL_BANNER_B64");
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return;

        var data = Convert.FromBase64String(File.ReadAllText(path).Trim());
        var banner = BannerSection.Parse(data);

        Assert.NotEmpty(banner.Portraits);
        Assert.Equal(data, banner.ToBytes());
    }
}

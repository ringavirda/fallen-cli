namespace FCli.Services;

public class DynamicConfig
{
    public virtual string StorageLocation { get; init; }
    public virtual string DefaultStorageFolderName { get; init; } = "Storage";
    public virtual string DefaultLogsFolderPath { get; init; } = "Logs";

    public DynamicConfig()
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            StorageLocation = Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData), "FCli");
        else if (Environment.OSVersion.Platform == PlatformID.Unix)
            StorageLocation = Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.Personal), ".fcli");
        else
            throw new PlatformNotSupportedException(
                "FCli supports only WinNT and Unix based systems.");
    }
}
namespace mark.davison.common.Utility;

[ExcludeFromCodeCoverage]
public class FileOperations : IFileOperations
{

    private readonly ILogger _logger;

    public FileOperations(ILogger<FileOperations> logger)
    {
        _logger = logger;
    }

    public bool FileExists(string file)
    {
        return File.Exists(file);
    }
    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }
    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }
    public void CopyFile(string source, string destination)
    {
        using (_logger.ProfileOperation())
        {
            File.Copy(source, destination, true);
        }
    }
    public void CopyDirectory(string source, string destination)
    {
        using (_logger.ProfileOperation())
        {
            foreach (var file in Directory.GetFiles(source))
            {
                var filename = Path.GetFileName(file);
                CopyFile(file, Path.Combine(destination, filename));
            }
        }
    }
    public void DeleteDirectory(string path)
    {
        Directory.Delete(path, true);
    }
    public void DeleteFile(string file)
    {
        File.Delete(file);
    }
    public string[] GetDirectoryContents(string path)
    {
        return Directory.EnumerateFiles(path).ToArray();
    }
}
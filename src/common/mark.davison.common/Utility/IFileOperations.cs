namespace mark.davison.common.Utility;

public interface IFileOperations
{
    bool FileExists(string file);
    bool DirectoryExists(string path);
    void CreateDirectory(string path);
    void CopyFile(string source, string destination);
    void CopyDirectory(string source, string destination);
    void DeleteDirectory(string path);
    void DeleteFile(string file);
    string[] GetDirectoryContents(string path);
}
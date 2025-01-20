using Apollo.ViewModels.Command;
using Apollo.Service;
using CUE4Parse.UE4.VirtualFileSystem;
using Serilog;

namespace Apollo.ViewModels;

public class BackupViewModel
{
    private string BackupPath { get; set; }
    private LoadCommand LoadCommand { get; set; }

    public BackupViewModel()
    {
        BackupPath = string.Empty;
        LoadCommand = new LoadCommand();
    }
    
    private async Task DownloadBackupAsync()
    {
        var backups = await ApplicationService.Api.FModelApi.GetBackupsAsync();
        var backupPath = Path.Combine(ApplicationService.DataDirectory, backups[^1].FileName);
        Log.Information("Downloading {name}", backups[^1].FileName);
        await ApplicationService.Api.DownloadFileAsync(backups[^1].DownloadUrl, backupPath);
        Log.Information("Downloaded {name} at {path}", backups[^1].FileName, backupPath);
        
        BackupPath = backupPath;
    }

    public async Task LoadNewFilesAsync(List<VfsEntry> entries)
    {
        await DownloadBackupAsync().ConfigureAwait(false);
        await LoadCommand.LoadNewFiles(BackupPath, entries);
    }
}
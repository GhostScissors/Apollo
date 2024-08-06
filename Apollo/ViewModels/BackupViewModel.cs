using Apollo.Service;
using Serilog;

namespace Apollo.ViewModels;

public class BackupViewModel
{
    public async Task DownloadBackup()
    {
        // Downloads the latest backup
        var backups = await ApplicationService.ApiVM.FModelApi.GetBackupsAsync();
        var backupPath = Path.Combine(ApplicationService.DataDirectory, backups![4].FileName);
        Log.Information("Downloading {name}", backups[4].FileName);
        await ApplicationService.ApiVM.DownloadFileAsync(backups[4].DownloadUrl, Path.Combine(ApplicationService.DataDirectory, backups[4].FileName));
        Log.Information("Downloaded {name} at {path}", backups[4].FileName, backupPath);
    }

    public string GetBackup()
    {
        var backupPath = new DirectoryInfo(ApplicationService.DataDirectory).GetFiles("*.fbkp");
        return backupPath.OrderByDescending(f => f.LastWriteTimeUtc).First().FullName;
    }
}
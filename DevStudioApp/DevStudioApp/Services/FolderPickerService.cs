using CommunityToolkit.Maui.Storage;

namespace DevStudioApp.Services;

public interface IFolderPickerService
{
    Task<string?> PickFolderAsync(string? initialPath = null);
}

public class FolderPickerService : IFolderPickerService
{
    public async Task<string?> PickFolderAsync(string? initialPath = null)
    {
        try
        {
            FolderPickerResult result = string.IsNullOrWhiteSpace(initialPath)
                ? await FolderPicker.Default.PickAsync(CancellationToken.None)
                : await FolderPicker.Default.PickAsync(initialPath, CancellationToken.None);

            if (result.IsSuccessful && result.Folder is not null)
            {
                return result.Folder.Path;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}

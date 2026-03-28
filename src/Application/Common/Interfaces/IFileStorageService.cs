namespace SimpleChat.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> StoreAsync(Stream stream, string fileName, CancellationToken ct = default);
    Task DeleteAsync(string path, CancellationToken ct = default);
    Task<Stream> GetStreamAsync(string path, CancellationToken ct = default);
}

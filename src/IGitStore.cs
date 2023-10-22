using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitStoreDotnet
{
    public interface IGitStore
    {
        Task<string> GetTextAsync(string relativePath, Encoding encoding = null, CancellationToken cancellationToken = default);

        Task<IAsyncEnumerable<string>> GetTextLinesAsync(string path, Encoding encoding = null, CancellationToken cancellationToken = default);

        Task<byte[]> GetBytesAsync(string relativePath, CancellationToken cancellationToken = default);

        Task AppendTextAsync(string relativePath, string content, Encoding encoding = null, CancellationToken cancellationToken = default);

        Task InsertOrUpdateAsync(string relativePath, string content, Encoding encoding = null, CancellationToken cancellationToken = default);

        Task InsertOrUpdateAsync(string relativePath, byte[] bytes, CancellationToken cancellationToken = default);

        Task<IEnumerable<string>> SearchFilesAsync(string searchPattern, bool topDirectoryOnly = true, CancellationToken cancellationToken = default);

        Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);

        Task PullFromRemoteAsync(CancellationToken cancellationToken = default);

        Task PushToRemoteAsync(string commitMessage, CancellationToken cancellationToken = default);
    }
}

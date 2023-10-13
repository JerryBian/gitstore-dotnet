using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitStoreDotnet
{
    public interface IGitStore
    {
        Task<string> GetTextAsync(string path, Encoding encoding = null, CancellationToken cancellationToken = default);

        Task<IAsyncEnumerable<string>> GetTextLinesAsync(string path, Encoding encoding = null, CancellationToken cancellationToken = default);

        Task<byte[]> GetBytesAsync(string path, CancellationToken cancellationToken = default);

        Task AppendTextAsync(string path, string content, Encoding encoding = null, CancellationToken cancellationToken = default);

        Task InsertOrUpdateAsync(string path, string content, Encoding encoding = null, CancellationToken cancellationToken = default);

        Task InsertOrUpdateAsync(string path, byte[] bytes, CancellationToken cancellationToken = default);

        Task<IEnumerable<string>> SearchFilesAsync(string path, string searchPattern, bool topDirectoryOnly = true, CancellationToken cancellationToken = default);

        Task DeleteAsync(string path, CancellationToken cancellationToken = default);

        Task PullFromRemoteAsync(CancellationToken cancellationToken = default);

        Task PushToRemoteAsync(string commitMessage, CancellationToken cancellationToken = default);
    }
}

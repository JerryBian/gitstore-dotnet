using LibGit2Sharp;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitStoreDotnet
{
    public class GitStore : IGitStore
    {
        private readonly GitStoreOption _option;
        private readonly SemaphoreSlim _semaphoreSlim;

        public GitStore(IOptions<GitStoreOption> option)
        {
            _option = option.Value;
            _semaphoreSlim = new SemaphoreSlim(1, 1);
        }

        public async Task AppendTextAsync(string path, string content, Encoding encoding = null, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                encoding ??= new UTF8Encoding(false);
                await File.AppendAllTextAsync(path, content, encoding, cancellationToken);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<byte[]> GetBytesAsync(string path, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                return await File.ReadAllBytesAsync(path, cancellationToken);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<string> GetTextAsync(string path, Encoding encoding = null, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                encoding ??= new UTF8Encoding(false);
                return await File.ReadAllTextAsync(path, encoding, cancellationToken);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<IAsyncEnumerable<string>> GetTextLinesAsync(string path, Encoding encoding = null, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                encoding ??= new UTF8Encoding(false);
                return File.ReadLinesAsync(path, encoding, cancellationToken);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task InsertOrUpdateAsync(string path, string content, Encoding encoding = null, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                encoding ??= new UTF8Encoding(false);
                await File.WriteAllTextAsync(path, content, encoding, cancellationToken);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task InsertOrUpdateAsync(string path, byte[] bytes, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                await File.WriteAllBytesAsync(path, bytes, cancellationToken);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task PullFromRemoteAsync(CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                Directory.Delete(_option.LocalDirectory, true);

                Repository.Clone(_option.RemoteGitUrl, _option.LocalDirectory, GetCloneOptions());
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task PushToRemoteAsync(string commitMessage, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                using (var repo = new Repository(_option.LocalDirectory))
                {
                    Commands.Stage(repo, "*");
                    var author = new Signature(_option.UserName, _option.Email, DateTimeOffset.Now);
                    repo.Commit(commitMessage, author, author);
                    repo.Network.Push(repo.Branches[_option.Branch], GetPushOptions());
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<IEnumerable<string>> SearchFilesAsync(string path, string searchPattern, bool topDirectoryOnly = true, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                return Directory.EnumerateFiles(path, searchPattern, topDirectoryOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private CloneOptions GetCloneOptions()
        {
            var cloneOptions = new CloneOptions
            {
                BranchName = _option.Branch
            };

            if (!string.IsNullOrEmpty(_option.Password))
            {
                cloneOptions.CredentialsProvider = (url, user, type) =>
                {
                    return new UsernamePasswordCredentials
                    {
                        Username = _option.UserName,
                        Password = _option.Password
                    };
                };
            }

            return cloneOptions;
        }

        private PushOptions GetPushOptions()
        {
            var pushOptions = new PushOptions();

            if (!string.IsNullOrEmpty(_option.Password))
            {
                pushOptions.CredentialsProvider = (url, user, type) =>
                {
                    return new UsernamePasswordCredentials
                    {
                        Username = _option.UserName,
                        Password = _option.Password
                    };
                };
            }

            return pushOptions;
        }
    }
}

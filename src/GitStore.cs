using LibGit2Sharp;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public async Task AppendTextAsync(string relativePath, string content, Encoding encoding = null, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                ValidateMinimalOption();
                string path = GetFullPath(relativePath, true);
                encoding ??= new UTF8Encoding(false);
                await File.AppendAllTextAsync(path, content, encoding, cancellationToken);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                ValidateMinimalOption();
                string path = GetFullPath(relativePath, false);
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                    return;
                }

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

        public async Task<byte[]> GetBytesAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                ValidateMinimalOption();
                string path = GetFullPath(relativePath, false);
                return !File.Exists(path) ? null : await File.ReadAllBytesAsync(path, cancellationToken);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<string> GetTextAsync(string relativePath, Encoding encoding = null, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                ValidateMinimalOption();
                string path = GetFullPath(relativePath, false);
                if (!File.Exists(path))
                {
                    return null;
                }

                encoding ??= new UTF8Encoding(false);
                return await File.ReadAllTextAsync(path, encoding, cancellationToken);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<IAsyncEnumerable<string>> GetTextLinesAsync(string relativePath, Encoding encoding = null, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                ValidateMinimalOption();
                string path = GetFullPath(relativePath, false);
                if (!File.Exists(path))
                {
                    return null;
                }

                encoding ??= new UTF8Encoding(false);
                return File.ReadLinesAsync(path, encoding, cancellationToken);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task InsertOrUpdateAsync(string relativePath, string content, Encoding encoding = null, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                ValidateMinimalOption();
                string path = GetFullPath(relativePath, true);
                encoding ??= new UTF8Encoding(false);
                await File.WriteAllTextAsync(path, content, encoding, cancellationToken);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task InsertOrUpdateAsync(string relativePath, byte[] bytes, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                ValidateMinimalOption();
                string path = GetFullPath(relativePath, true);
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
                ValidateOption();
                DirectoryHelper.Delete(_option.LocalDirectory);
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
                ValidateOption();
                using Repository repo = new Repository(_option.LocalDirectory);
                if (repo.RetrieveStatus().IsDirty)
                {
                    Commands.Stage(repo, "*");
                    Signature author = new(_option.Author, _option.AuthorEmail, DateTimeOffset.Now);
                    Signature committer = new(_option.Committer, _option.CommitterEmail, DateTimeOffset.Now);
                    repo.Commit(commitMessage, author, committer);
                    repo.Network.Push(repo.Branches[_option.Branch], GetPushOptions());
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<IEnumerable<string>> SearchFilesAsync(string searchPattern, bool topDirectoryOnly = true, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                ValidateMinimalOption();
                return !Directory.Exists(_option.LocalDirectory)
                    ? Enumerable.Empty<string>()
                    : Directory.EnumerateFiles(_option.LocalDirectory, searchPattern, topDirectoryOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private string GetFullPath(string relativePath, bool createDirectory)
        {
            string fullPath = Path.Combine(_option.LocalDirectory, relativePath);

            if (createDirectory)
            {
                DirectoryHelper.EnsureDirectoryExists(fullPath);
            }

            return fullPath;
        }

        private void ValidateMinimalOption()
        {
            if (string.IsNullOrEmpty(_option.LocalDirectory))
            {
                throw new InvalidOperationException("LocalDirectory for GitStoreOption is not assigned.");
            }
        }

        private void ValidateOption()
        {
            ValidateMinimalOption();

            if (string.IsNullOrEmpty(_option.Branch))
            {
                throw new InvalidOperationException("Branch for GitStoreOption is not assigned.");
            }

            if (string.IsNullOrEmpty(_option.RemoteGitUrl))
            {
                throw new InvalidOperationException("RemoteGitUrl for GitStoreOption is not assigned.");
            }

            if (string.IsNullOrEmpty(_option.Committer) && string.IsNullOrEmpty(_option.Author))
            {
                throw new InvalidOperationException("Either Committer or Author for GitStoreOption must be assigned.");
            }

            if (string.IsNullOrEmpty(_option.CommitterEmail) && string.IsNullOrEmpty(_option.AuthorEmail))
            {
                throw new InvalidOperationException("Either CommitterEmail or AuthorEmail for GitStoreOption must be assigned.");
            }

            if (string.IsNullOrEmpty(_option.Committer))
            {
                _option.Committer = _option.Author;
            }

            if (string.IsNullOrEmpty(_option.Author))
            {
                _option.Author = _option.Committer;
            }

            if (string.IsNullOrEmpty(_option.CommitterEmail))
            {
                _option.CommitterEmail = _option.AuthorEmail;
            }

            if (string.IsNullOrEmpty(_option.AuthorEmail))
            {
                _option.AuthorEmail = _option.CommitterEmail;
            }

            _option.Password ??= string.Empty;
        }

        private CloneOptions GetCloneOptions()
        {
            CloneOptions cloneOptions = new CloneOptions
            {
                BranchName = _option.Branch,
                FetchOptions =
                {
                    CredentialsProvider = (url, user, type) => new UsernamePasswordCredentials
                    {
                        Username = _option.UserName,
                        Password = _option.Password
                    }
                }
            };

            return cloneOptions;
        }

        private PushOptions GetPushOptions()
        {
            PushOptions pushOptions = new PushOptions
            {
                CredentialsProvider = (url, user, type) => new UsernamePasswordCredentials
                {
                    Username = _option.UserName,
                    Password = _option.Password
                }
            };

            return pushOptions;
        }
    }
}

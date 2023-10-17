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
        private readonly Lazy<object> _lazyValidateOption;

        public GitStore(IOptions<GitStoreOption> option)
        {
            _option = option.Value;
            _semaphoreSlim = new SemaphoreSlim(1, 1);

            _lazyValidateOption = new Lazy<object>(() =>
            {
                ValidateOption();
                return null;
            }, true);
        }

        public async Task AppendTextAsync(string path, string content, bool isRelativePath = false, Encoding encoding = null, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                path = GetFullPath(path, isRelativePath, true);
                encoding ??= new UTF8Encoding(false);
                await File.AppendAllTextAsync(path, content, encoding, cancellationToken);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task DeleteAsync(string path, bool isRelativePath = false, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                path = GetFullPath(path, isRelativePath, false);
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

        public async Task<byte[]> GetBytesAsync(string path, bool isRelativePath = false, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                path = GetFullPath(path, isRelativePath, false);
                if (!File.Exists(path))
                {
                    return null;
                }

                return await File.ReadAllBytesAsync(path, cancellationToken);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<string> GetTextAsync(string path, bool isRelativePath = false, Encoding encoding = null, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                path = GetFullPath(path, isRelativePath, false);
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

        public async Task<IAsyncEnumerable<string>> GetTextLinesAsync(string path, bool isRelativePath = false, Encoding encoding = null, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                path = GetFullPath(path, isRelativePath, false);
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

        public async Task InsertOrUpdateAsync(string path, string content, bool isRelativePath = false, Encoding encoding = null, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                path = GetFullPath(path, isRelativePath, true);
                encoding ??= new UTF8Encoding(false);
                await File.WriteAllTextAsync(path, content, encoding, cancellationToken);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task InsertOrUpdateAsync(string path, byte[] bytes, bool isRelativePath = false, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                path = GetFullPath(path, isRelativePath, true);
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
                _ = _lazyValidateOption.Value;
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
                _ = _lazyValidateOption.Value;
                using (var repo = new Repository(_option.LocalDirectory))
                {
                    Commands.Stage(repo, "*");
                    var author = new Signature(_option.Author, _option.AuthorEmail, DateTimeOffset.Now);
                    var committer = new Signature(_option.Committer, _option.CommitterEmail, DateTimeOffset.Now);
                    repo.Commit(commitMessage, author, committer);
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
                if (!Directory.Exists(path))
                {
                    return Enumerable.Empty<string>();
                }

                return Directory.EnumerateFiles(path, searchPattern, topDirectoryOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private string GetFullPath(string path, bool isRelativePath, bool createDirectory)
        {
            if(isRelativePath)
            {
                path = Path.Combine(_option.LocalDirectory, path);
            }

            if(createDirectory)
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            
            return path;
        }

        private void ValidateOption()
        {
            if (string.IsNullOrEmpty(_option.Branch))
            {
                throw new Exception("Branch for GitStoreOption is not assigned.");
            }

            if (string.IsNullOrEmpty(_option.LocalDirectory))
            {
                throw new Exception("LocalDirectory for GitStoreOption is not assigned.");
            }

            if (string.IsNullOrEmpty(_option.RemoteGitUrl))
            {
                throw new Exception("RemoteGitUrl for GitStoreOption is not assigned.");
            }

            if (string.IsNullOrEmpty(_option.Committer) && string.IsNullOrEmpty(_option.Author))
            {
                throw new Exception("Neither Committer or Author for GitStoreOption is not assigned.");
            }

            if (string.IsNullOrEmpty(_option.CommitterEmail) && string.IsNullOrEmpty(_option.AuthorEmail))
            {
                throw new Exception("Neither CommitterEmail or AuthorEmail for GitStoreOption is not assigned.");
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

            if (_option.Password == null)
            {
                _option.Password = string.Empty;
            }
        }

        private CloneOptions GetCloneOptions()
        {
            var cloneOptions = new CloneOptions
            {
                BranchName = _option.Branch
            };
            cloneOptions.CredentialsProvider = (url, user, type) =>
            {
                return new UsernamePasswordCredentials
                {
                    Username = _option.UserName,
                    Password = _option.Password
                };
            };

            return cloneOptions;
        }

        private PushOptions GetPushOptions()
        {
            var pushOptions = new PushOptions();
            pushOptions.CredentialsProvider = (url, user, type) =>
            {
                return new UsernamePasswordCredentials
                {
                    Username = _option.UserName,
                    Password = _option.Password
                };
            };

            return pushOptions;
        }
    }
}

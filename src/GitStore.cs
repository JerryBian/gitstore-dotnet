using ExecDotnet;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<GitStore> _logger;
        private readonly SemaphoreSlim _semaphoreSlim;

        public GitStore(IOptions<GitStoreOption> option, ILogger<GitStore> logger)
        {
            _logger = logger;
            _option = option.Value;
            _semaphoreSlim = new SemaphoreSlim(1, 1);

            ValidateOption();
        }

        public async Task AppendTextAsync(string path, string content, Encoding encoding = null, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                EnsureDirectoryExists(path);
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

        public async Task<string> GetTextAsync(string path, Encoding encoding = null, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
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

        public async Task<IAsyncEnumerable<string>> GetTextLinesAsync(string path, Encoding encoding = null, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
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

        public async Task InsertOrUpdateAsync(string path, string content, Encoding encoding = null, CancellationToken cancellationToken = default)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            try
            {
                EnsureDirectoryExists(path);
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
                EnsureDirectoryExists(path);
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
                EnsureDirectoryExists(_option.LocalDirectory);
                DirectoryHelper.DeleteDirectory(_option.LocalDirectory);

                int retryTimes = 0;
                while (retryTimes <= 3 && !Directory.Exists(_option.LocalDirectory))
                {
                    retryTimes++;
                    string command = $"git clone -b {_option.Branch} --single-branch \"{_option.RemoteGitUrl}\" \"{_option.LocalDirectory}\"";
                    command += $" && cd {_option.LocalDirectory}";
                    command += $" && git config --local user.name \"{_option.Committer}\"";
                    command += $" && git config --local user.email \"{_option.CommitterEmail}\"";
                    if (retryTimes > 1)
                    {
                        _logger.LogInformation($"Retry: {retryTimes}... starting to pull DB repo.");
                    }

                    string output = await Exec.RunAsync(command, cancellationToken);
                    if (retryTimes > 1)
                    {
                        _logger.LogInformation($"Retry: {retryTimes}, cmd: {command}{Environment.NewLine}");
                    }

                    _logger.LogInformation(output);
                }

                if (!Directory.Exists(_option.LocalDirectory))
                {
                    throw new Exception($"Failed to pull git repo {_option.RemoteGitUrl} to {_option.LocalDirectory}, app will be termintated.");
                }
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
                if (!Directory.Exists(_option.LocalDirectory))
                {
                    return;
                }

                List<string> commands = new()
                {
                    $"cd \"{_option.LocalDirectory}\"", 
                    "git add .",
                    $"git commit -m \"{commitMessage}\"", 
                    "git push"
                };
                string command =
                    $"{string.Join(" && ", commands)}";
                string output = await Exec.RunAsync(command);
                _logger.LogInformation(output);
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

        private void EnsureDirectoryExists(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
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

            if (string.IsNullOrEmpty(_option.Committer))
            {
                throw new Exception("Committer for GitStoreOption is not assigned.");
            }
            if (string.IsNullOrEmpty(_option.CommitterEmail))
            {
                throw new Exception("CommitterEmail for GitStoreOption is not assigned.");
            }
        }
    }
}

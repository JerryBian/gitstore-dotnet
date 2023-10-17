using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace GitStoreDotnet.Test
{
    public class GitStoreTest
    {
        private readonly ITestOutputHelper _outputHelper;

        public GitStoreTest(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        // https://github.blog/2020-12-15-token-authentication-requirements-for-git-operations/
        // GitHub does not support password git operations 
        [Fact(Skip = "It's not working for GitHub.")]
        public async Task TestPasswordRepository()
        {
            var option = new GitStoreOption
            {
                Branch = "unit-test",
                Committer = "test_committer2",
                Password = Environment.GetEnvironmentVariable("GitHubPassword"),
                AuthorEmail = "test_committer2@test.com",
                UserName = Environment.GetEnvironmentVariable("GitHubUser"),
                LocalDirectory = Path.Combine(Path.GetTempPath(), "gitstore_unittest"),
                RemoteGitUrl = "https://github.com/JerryBian/gitstore-dotnet"
            };
            var store = new GitStore(Options.Create(option));
            await store.PullFromRemoteAsync();

            var content = await store.GetTextAsync(Path.Combine(option.LocalDirectory, "dummy.txt"));
            Assert.Equal("test", content);

            var path1 = Path.Combine(option.LocalDirectory, "data", Path.GetRandomFileName());
            var content1 = Guid.NewGuid().ToString();
            await store.InsertOrUpdateAsync(path1, content1);

            var path2 = Path.Combine(option.LocalDirectory, "data", Path.GetRandomFileName());
            var content2 = Guid.NewGuid().ToByteArray();
            await store.InsertOrUpdateAsync(path2, content2);

            await store.PushToRemoteAsync($"{DateTime.Now.ToString("O")}");
            await store.PullFromRemoteAsync();

            Assert.Equal(File.ReadAllText(path1), content1);
            Assert.True(content2.SequenceEqual(File.ReadAllBytes(path2)));
        }

        [Fact]
        public async Task TestAccessKeyRepository()
        {
            var accessToken = Environment.GetEnvironmentVariable("PersonalAccessToken");
            var option = new GitStoreOption
            {
                Branch = "unit-test",
                Author = "test_committer",
                Password = "",
                CommitterEmail = "test_committer@test.com",
                UserName = accessToken,
                LocalDirectory = Path.Combine(Path.GetTempPath(), "gitstore_unittest"),
                RemoteGitUrl = "https://github.com/JerryBian/gitstore-dotnet"
            };
            var store = new GitStore(Options.Create(option));
            await store.PullFromRemoteAsync();

            var content = await store.GetTextAsync(Path.Combine(option.LocalDirectory, "dummy.txt"));
            Assert.Equal("test", content);

            var path1 = Path.Combine(option.LocalDirectory, "data", Path.GetRandomFileName());
            var content1 = Guid.NewGuid().ToString();
            await store.InsertOrUpdateAsync(path1, content1);

            var path2 = Path.Combine(option.LocalDirectory, "data", Path.GetRandomFileName());
            var content2 = Guid.NewGuid().ToByteArray();
            await store.InsertOrUpdateAsync(path2, content2);

            await store.PushToRemoteAsync($"TestAccessKeyRepository_{Environment.OSVersion}_{DateTime.Now.ToString("O")}");
            await store.PullFromRemoteAsync();

            Assert.Equal(File.ReadAllText(path1), content1);
            Assert.True(content2.SequenceEqual(File.ReadAllBytes(path2)));
        }

        [Fact]
        public async Task TestServiceCollectionExtension()
        {
            Environment.SetEnvironmentVariable("GitStore__Branch", "unit-test");
            Environment.SetEnvironmentVariable("GitStore__Author", "test_committer3");
            Environment.SetEnvironmentVariable("GitStore__Password", "");
            Environment.SetEnvironmentVariable("GitStore__CommitterEmail", "test_committer3@test.com");
            Environment.SetEnvironmentVariable("GitStore__UserName", Environment.GetEnvironmentVariable("PersonalAccessToken"));
            Environment.SetEnvironmentVariable("GitStore__LocalDirectory", Path.Combine(Path.GetTempPath(), "gitstore_unittest"));
            Environment.SetEnvironmentVariable("GitStore__RemoteGitUrl", "https://github.com/JerryBian/gitstore-dotnet");

            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddGitStore();

            using IHost host = builder.Build();
            var store = host.Services.GetRequiredService<IGitStore>();
            var option = host.Services.GetRequiredService<IOptions<GitStoreOption>>().Value;
            await store.PullFromRemoteAsync();

            var content = await store.GetTextAsync(Path.Combine(option.LocalDirectory, "dummy.txt"));
            Assert.Equal("test", content);

            var path1 = Path.Combine(option.LocalDirectory, "data", Path.GetRandomFileName());
            var content1 = Guid.NewGuid().ToString();
            await store.InsertOrUpdateAsync(path1, content1);

            var path2 = Path.Combine(option.LocalDirectory, "data", Path.GetRandomFileName());
            var content2 = Guid.NewGuid().ToByteArray();
            await store.InsertOrUpdateAsync(path2, content2);

            await store.PushToRemoteAsync($"TestServiceCollectionExtension_{Environment.OSVersion}_{DateTime.Now.ToString("O")}");
            await store.PullFromRemoteAsync();

            Assert.Equal(File.ReadAllText(path1), content1);
            Assert.True(content2.SequenceEqual(File.ReadAllBytes(path2)));
        }

        [Theory]
        [InlineData("/abc/def")]
        [InlineData("\\098\\890")]
        [InlineData("../123/456")]
        [InlineData("C:\\")]
        [InlineData("test.json")]
        public void TestPathRelative(string path)
        {
            _outputHelper.WriteLine($"<<< {path} >>>");
            _outputHelper.WriteLine($"IsPathRooted: {Path.IsPathRooted(path)}");
            _outputHelper.WriteLine($"IsPathFullyQualified: {Path.IsPathFullyQualified(path)}");
            _outputHelper.WriteLine($"FullPath: {Path.GetFullPath(path)}");
        }
    }
}

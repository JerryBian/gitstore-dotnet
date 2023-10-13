using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GitStoreDotnet.Test
{
    public class GitStoreTest
    {
        [Fact]
        public async Task TestPasswordRepository()
        {
            var option = new GitStoreOption
            {
                Branch = "unit-test",
                LocalDirectory = Path.Combine(Path.GetTempPath(), "gitstore_unittest"),
                RemoteGitUrl = "https://github.com/JerryBian/gitstore-dotnet"
            };
            var store = new GitStore(Options.Create(option), NullLogger<GitStore>.Instance);
            await store.PullFromRemoteAsync();

            var content = await store.GetTextAsync(Path.Combine(option.LocalDirectory, "dummy.txt"));
            Assert.Equal("test", content);
        }

        [Fact]
        public async Task TestAccessKeyRepository()
        {
            var option = new GitStoreOption
            {
                Branch = "unit-test",
                Committer = "test_committer",
                CommitterEmail = "test_committer@test.com",
                LocalDirectory = Path.Combine(Path.GetTempPath(), "gitstore_unittest"),
                RemoteGitUrl = "https://xxx@github.com/JerryBian/gitstore-dotnet.git"
            };
            var store = new GitStore(Options.Create(option), NullLogger<GitStore>.Instance);
            await store.PullFromRemoteAsync();

            var content = await store.GetTextAsync(Path.Combine(option.LocalDirectory, "dummy.txt"));
            Assert.Equal("test", content);

            var path1 = Path.Combine(option.LocalDirectory, "data", Path.GetRandomFileName());
            var content1 = Guid.NewGuid().ToString();
            await store.InsertOrUpdateAsync(path1, content1);

            var path2 = Path.Combine(option.LocalDirectory, "data", Path.GetRandomFileName());
            var content2 = Guid.NewGuid().ToByteArray();
            await store.InsertOrUpdateAsync(path2, content2);

            await store.PushToRemoteAsync($"{DateTime.Now.ToLongTimeString()}");

            
        }
    }
}

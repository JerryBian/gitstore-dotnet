# GitStore

A .NET library that leverages Git repositories as a data store.

[![master](https://github.com/JerryBian/gitstore-dotnet/actions/workflows/build.yml/badge.svg)](https://github.com/JerryBian/gitstore-dotnet/actions/workflows/build.yml)

**Note**: This is not a replacement for Git operations. It only implements `git clone` and `git push`. The basic concept is to clone a remote Git repository locally, store data as text or binary files, commit the changes, and push them to the remote repository. The core operations are file reads and writes.

## Usage

Install from [NuGet](https://www.nuget.org/packages/gitstore):

```
dotnet add package GitStore
```

This library supports .NET 8 and later versions.

### Basic Usage

Provide the required option parameters manually:

```csharp
var option = new GitStoreOption
{
    Branch = "unit-test",
    Author = "test_committer",
    Password = "",
    CommitterEmail = "test_committer@test.com",
    UserName = "<Personal Access Token>",
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

await store.PushToRemoteAsync("commit by GitStore.");
await store.PullFromRemoteAsync();

Assert.Equal(File.ReadAllText(path1), content1);
Assert.True(content2.SequenceEqual(File.ReadAllBytes(path2)));
```

For GitHub repositories, you can request a Personal Access Token (PAT) [here](https://github.com/settings/tokens). Please note that you cannot use the traditional username/password method to operate on GitHub anymore. See the [announcement](https://github.blog/2020-12-15-token-authentication-requirements-for-git-operations/) for more details.

### Dependency Injection / Options Pattern

For modern applications, you can register GitStore via dependency injection:

```csharp
var builder = Host.CreateApplicationBuilder();
builder.Services.AddGitStore();

using IHost host = builder.Build();
var store = host.Services.GetRequiredService<IGitStore>();
var option = host.Services.GetRequiredService<IOptions<GitStoreOption>>().Value;
await store.PullFromRemoteAsync();
```

The option parameters are configured using the [standard configuration pattern](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-7.0).

For example, set an environment variable as `GitStore__Branch=master`.

## License

[MIT](./LICENSE)
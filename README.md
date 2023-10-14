A dotnet library leverage Git repository as store.

[![master](https://github.com/JerryBian/gitstore-dotnet/actions/workflows/build.yml/badge.svg)](https://github.com/JerryBian/gitstore-dotnet/actions/workflows/build.yml)

Note: it's not a replacement of Git operations, actually it only implements `git clone` and `git push`. Basic idea is clone remoting git repository to local and store data as text or bytes, commit and push to remote. The core operations are file reads and writes.

## Usage

Install from [NuGet](https://www.nuget.org/packages/gitstore)

```
dotnet add package GitStore
```

This library supports .NET 7 and version onwards.

### Basic usage

Provide required option parameters manually.

```csharp
var option = new GitStoreOption
{
    Branch = "unit-test",
    Author = "test_committer",
    Password = "",
    CommitterEmail = "test_committer@test.com",
    UserName = <Personal Access Token>,
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

For GitHub repository, you can request PAT here [here](https://github.com/settings/tokens). Please note you cannot use traditional username/password method to operate GitHub anymore, see the [announcement](https://github.blog/2020-12-15-token-authentication-requirements-for-git-operations/).

### Dependency injection / Options pattern

For modern applications, you can register GitStore via DI.

```csharp
var builder = Host.CreateApplicationBuilder();
builder.Services.AddGitStore();

using IHost host = builder.Build();
var store = host.Services.GetRequiredService<IGitStore>();
var option = host.Services.GetRequiredService<IOptions<GitStoreOption>>().Value;
await store.PullFromRemoteAsync();
```

The option parameters are configured as [standard way](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-7.0).

For example, set environment variable as `GitStore__Branch=master`.

## License

[MIT](./LICENSE)
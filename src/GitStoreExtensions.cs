using Microsoft.Extensions.DependencyInjection;

namespace GitStoreDotnet
{
    public static class GitStoreExtensions
    {
        public static IServiceCollection AddGitStore(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddOptions<GitStoreOption>().BindConfiguration("GitStore");
            serviceCollection.AddSingleton<IGitStore, GitStore>();

            return serviceCollection;
        }
    }
}

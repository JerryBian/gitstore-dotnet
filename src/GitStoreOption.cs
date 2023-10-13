namespace GitStoreDotnet
{
    public class GitStoreOption
    {
        public string RemoteGitUrl { get; set; }

        public string Branch { get; set; }

        public string LocalDirectory { get; set; }

        public string Committer { get; set; }

        public string CommitterEmail { get; set; }
    }
}

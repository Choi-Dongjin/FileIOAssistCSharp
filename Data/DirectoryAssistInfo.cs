namespace FileIOAssist.Data
{
    /// <summary>
    /// 디렉토리 정보
    /// </summary>
    public class DirectoryAssistInfo
    {
        public string OriginalPath { get => _originalPath; }
        public string FullPath { get => _fullPath; }
        public string RootPath { get => _rootPath; }
        public string WorkPath { get => _workPath; }
        public string? DirectoryName => Path.GetDirectoryName(FullPath);
        /// <summary>
        /// 서브 Dir 존제 유무
        /// </summary>
        public bool IsSubDirectoryExist { get => SubDirectoryAssistInfos.Count > 0; }

        private readonly string _originalPath;
        private readonly string _fullPath;
        private readonly string _rootPath;
        private readonly string _workPath;

        /// <summary>
        /// 서브 폴더
        /// </summary>
        public List<DirectoryAssistInfo> SubDirectoryAssistInfos { get; private set; }
        /// <summary>
        /// 파일 정보
        /// </summary>
        public List<FileAssistInfo> Files;

        public DirectoryAssistInfo(string path, string? rootPath = null) : this(path, rootPath, null) { }

        public DirectoryAssistInfo(string originalPath, string? rootPath, string? fullPath = null)
        {
            ArgumentNullException.ThrowIfNull(originalPath);
            _originalPath = originalPath;
            fullPath ??= originalPath;
            _fullPath = StringControl.IsNormalized(originalPath) ? fullPath ?? originalPath : Path.GetFullPath(fullPath);
            _rootPath = rootPath ?? fullPath ?? originalPath;
            _workPath = _fullPath.Replace(_rootPath, "");
            SubDirectoryAssistInfos = new();
            Files = new();
        }
    }
}

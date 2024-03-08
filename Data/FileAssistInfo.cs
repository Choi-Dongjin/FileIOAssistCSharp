namespace FileIOAssist.Data
{
    /// <summary>
    /// 파일 정보
    /// </summary>
    public class FileAssistInfo
    {
        public string OriginalPath { get => _originalPath; }
        public string FullPath { get => _fullPath; }
        public string Name { get => _name; }


        private readonly string _originalPath;
        private readonly string _fullPath;
        private readonly string _name;


        public FileAssistInfo(string fileName) : this(fileName, null, null)
        {
        }

        internal FileAssistInfo(string originalPath, string? fullPath = null, string? fileName = null)
        {
            ArgumentNullException.ThrowIfNull(originalPath);

            _originalPath = originalPath;

            fullPath ??= originalPath;
            _fullPath = StringControl.IsNormalized(originalPath) ? fullPath ?? originalPath : Path.GetFullPath(fullPath);
            _name = fileName ?? Path.GetFileName(originalPath) ?? string.Empty;
        }

        public string? DirectoryName => Path.GetDirectoryName(FullPath);
        public string? Extension => Path.GetExtension(FullPath) ?? Path.GetExtension(OriginalPath);
        public string? OnlyName => Path.GetFileNameWithoutExtension(FullPath) ?? Path.GetFileNameWithoutExtension(OriginalPath);
    }
}

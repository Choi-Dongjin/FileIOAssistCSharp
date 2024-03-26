using FileIOAssist.Data;
using System.Security.Cryptography;

namespace FileIOAssist
{
    public class Extension
    {
        /// <summary>
        /// 바이트로 되어있는 것을 KB, MB, GB 형식으로 변환 해주는 것
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string FormatBytes(long bytes)
        {
            const int scale = 1024;
            string[] orders = new string[] { "GB", "MB", "KB", "Bytes" };
            long max = (long)Math.Pow(scale, orders.Length - 1);

            foreach (string order in orders)
            {
                if (bytes > max)
                    return string.Format("{0:##.##} {1}", decimal.Divide(bytes, max), order);
                max /= scale;
            }
            return "0 Bytes";
        }

        public static string FormatBytesGB(double bytes)
        {
            const int scale = 1024;
            for (int i = 0; i < 3; i++)
            {
                bytes /= scale;
            }
            return String.Format("{0:0.0000}", bytes);
        }

        /// <summary>
        /// 폴더 생성시 적용가능한 이름인지 확인 가능하면 True, 불가능하면 False 반환
        /// </summary>
        /// <param name="folderName"> 확인학 폴더 이름</param>
        /// <returns></returns>
        public static bool DirChackCreateName(string folderName)
        {
            char[] ignoreCharArray = { ',', '\\', '/', ':', '*', '?', '"', '<', '>', '|', '_', '[', ']' };
            foreach (char ignoreCha in ignoreCharArray)
            {
                if (folderName.Any(f => f == ignoreCha))
                {
                    return false;
                }
            }
            return true;
        }

        public enum GetNameType
        {
            Full,
            OnlyName,
            GetFileNameWithoutExtension,
        }

        public enum SubSearch
        {
            None,
            Full
        }

        /// <summary>
        /// 폴더내 파일 검색
        /// </summary>
        /// <param name="dirPath"> 대상 경로 </param>
        /// <param name="getFileNameMode"> "Full", "Name" </param>
        /// <returns></returns>
        public static List<string> DirFileSearch(string dirPath, GetNameType fileNameMode = GetNameType.OnlyName, SubSearch subfoldersSearch = SubSearch.None, CancellationToken mainCT = default)
        {
            List<string> fileList = new List<string>();

            DirectoryInfo di = new DirectoryInfo(dirPath);
            string[] dirs = Directory.GetDirectories(dirPath);

            if (di.Exists)
            {
                switch (fileNameMode)
                {
                    case GetNameType.Full:
                        foreach (FileInfo fileInfo in di.GetFiles())
                            fileList.Add(fileInfo.FullName);
                        break;

                    case GetNameType.OnlyName:
                        foreach (FileInfo fileInfo in di.GetFiles())
                            fileList.Add(fileInfo.Name);
                        break;

                    case GetNameType.GetFileNameWithoutExtension:
                        foreach (FileInfo fileInfo in di.GetFiles())
                            fileList.Add(Path.GetFileNameWithoutExtension(fileInfo.Name));
                        break;
                }
            }

            switch (subfoldersSearch)
            {
                case SubSearch.None:
                    return fileList;

                case SubSearch.Full:
                    //하위 폴더가지 확인 재귀 함수를 이용한 구현
                    if (dirs.Length > 0)
                    {
                        foreach (string dir in dirs)
                        {
                            fileList.AddRange(DirFileSearch(dir, fileNameMode, subfoldersSearch));
                        }
                    }
                    break;
            }
            return fileList;
        }

        /// <summary>
        /// Search files in the specified folder.
        /// </summary>
        /// <param name="dirPath">Folder path</param>
        /// <param name="exts">File extension filters, e.g., "txt"</param>
        /// <param name="fileNameMode">Return value, full name or only name</param>
        /// <param name="subfoldersSearch">Search in subfolders</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>List of file names</returns>
        public static List<string> DirFileSearch(string dirPath, string[]? exts = null, GetNameType fileNameMode = GetNameType.Full, SubSearch subfoldersSearch = SubSearch.None, CancellationToken? ct = null)
        {
            List<string> fileList = new();
            try
            {
                // External cancellation check
                if (ct?.IsCancellationRequested ?? false) return fileList;

                if (Directory.Exists(dirPath))
                {
                    var files = Directory.GetFiles(dirPath, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(s => exts == null || exts.Length == 0 || exts.Contains(Path.GetExtension(s)?.TrimStart('.').ToLower(), StringComparer.OrdinalIgnoreCase))
                     .ToArray();

                    foreach (var fileInfo in files)
                    {
                        // External cancellation check
                        if (ct?.IsCancellationRequested ?? false) break;

                        switch (fileNameMode)
                        {
                            case GetNameType.Full:
                                fileList.Add(fileInfo);
                                break;

                            case GetNameType.OnlyName:
                                fileList.Add(Path.GetFileName(fileInfo));
                                break;

                            case GetNameType.GetFileNameWithoutExtension:
                                fileList.Add(Path.GetFileNameWithoutExtension(fileInfo));
                                break;
                        }
                    }

                    // External cancellation check
                    if (ct?.IsCancellationRequested ?? false) return fileList;

                    switch (subfoldersSearch)
                    {
                        case SubSearch.None:
                            return fileList;

                        case SubSearch.Full:
                            var dirs = Directory.GetDirectories(dirPath);
                            if (dirs.Length > 0)
                            {
                                // Parallelize the search algorithm if needed
                                foreach (var subDir in dirs)
                                {
                                    // External cancellation check
                                    if (ct?.IsCancellationRequested ?? false) return fileList;

                                    fileList.AddRange(DirFileSearch(subDir, exts, fileNameMode, subfoldersSearch, ct));
                                }
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return fileList;
        }

        /// <summary>
        /// 폴더내 파일 검색
        /// </summary>
        /// <param name="dirPath"> 검색 dir </param>
        /// <param name="exts"> 검색 파일 확장자 </param>
        /// <param name="subfoldersSearch"> 서브 폴더 검색 </param>
        /// <param name="ct"> CancellationToken </param>
        /// <param name="rootPath"> 작업 root path </param>
        /// <returns></returns>
        public static DirectoryAssistInfo DirFileSearch(string dirPath, string[]? exts = null, SubSearch subfoldersSearch = SubSearch.None, CancellationToken? ct = null, string? rootPath = null)
        {
            DirectoryAssistInfo directoryAssistInfo = new(dirPath, rootPath);
            DirectoryInfo di = new(dirPath);
            string[] dirs = Directory.GetDirectories(dirPath);

            // 외부에 의한 정지 확인
            if (ct != null && ct.Value.IsCancellationRequested) { return directoryAssistInfo; }

            if (di.Exists)
            {
                FileInfo[] files = di.GetFiles("*.*", SearchOption.TopDirectoryOnly);

                foreach (FileInfo file in files)
                {
                    if (ct != null && ct.Value.IsCancellationRequested) { break; }
                    string fileExtension = Path.GetExtension(file.Name).ToLower();
                    if (exts == null)
                    {
                        directoryAssistInfo.Files.Add(new FileAssistInfo(file.FullName));
                        continue;
                    }
                    if (exts.Any(ext => ext.ToLower().Contains(fileExtension)))
                    {
                        directoryAssistInfo.Files.Add(new FileAssistInfo(file.FullName));
                    }
                }
            }

            if (ct != null && ct.Value.IsCancellationRequested) { return directoryAssistInfo; }

            switch (subfoldersSearch)
            {
                case SubSearch.None:
                    return directoryAssistInfo;

                case SubSearch.Full:
                    //하위 폴더가지 확인 재귀 함수를 이용한 구현
                    if (dirs.Length > 0)
                    {
                        // 병렬 연산을 이용하여 검색 알고리즘 수정 필요?
                        foreach (string dir in dirs)
                        {
                            // 외부에 의한 정지 확인
                            if (ct != null && ct.Value.IsCancellationRequested) { return directoryAssistInfo; }
                            directoryAssistInfo.SubDirectoryAssistInfos.Add(DirFileSearch(dir, exts, subfoldersSearch, ct, rootPath));
                        }
                    }
                    break;
            }
            return directoryAssistInfo;
        }

        /// <summary>
        /// 서브 딕셔너리 가져오기
        /// </summary>
        /// <param name="dirPath"></param>
        /// <param name="subDirs"></param>
        /// <returns></returns>
        public static bool TrySubDirectories(string dirPath, out IReadOnlyCollection<string>? subDirs, GetNameType fileNameMode = GetNameType.Full)
        {
            if (!Directory.Exists(dirPath)) { subDirs = null; return false; }
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(dirPath);
                subDirs = GetSubDirectories(directoryInfo);
            }
            catch
            {

                subDirs = null;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 서브 딕셔너리 가져오기
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        static List<string> GetSubDirectories(DirectoryInfo directory, GetNameType fileNameMode = GetNameType.Full)
        {
            List<string> subDirectories = new List<string>();

            try
            {
                // 디렉토리 안의 디렉토리 리스트 가져오기
                DirectoryInfo[] subDirs = directory.GetDirectories();
                switch (fileNameMode)
                {
                    case GetNameType.Full:
                        // 리스트에 추가
                        foreach (var subDir in subDirs)
                        {
                            subDirectories.Add(subDir.FullName);
                        }
                        break;
                    case GetNameType.OnlyName:
                        // 리스트에 추가
                        foreach (var subDir in subDirs)
                        {
                            subDirectories.Add(subDir.Name);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류 발생: {ex.Message}");
            }

            return subDirectories;
        }

        /// <summary>
        /// 폴더 사이즈 계산
        /// </summary>
        /// <param name="forderName"> 확인 경로 </param>
        /// <returns></returns>
        public static long DirSize(string forderName)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(forderName);
            long size = 0;
            // Add file sizes.
            if (directoryInfo.Exists)
            {
                FileInfo[] fis = directoryInfo.GetFiles();
                foreach (FileInfo fi in fis)
                {
                    size += fi.Length;
                }
                // Add subdirectory sizes.
                DirectoryInfo[] dis = directoryInfo.GetDirectories();
                foreach (DirectoryInfo di in dis)
                {
                    size += DirSize(di.FullName);
                }
            }
            return size;
        }

        /// <summary>
        /// 임의의 파일 이름 만들기 기본 10 자리 이름
        /// </summary>
        /// <param name="number"> 자릿수 </param>
        /// <returns> 랜덤 이름 </returns>
        public static string RandomFileName(int number = 10)
        {
            var characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var Charsarr = new char[number];
            var random = new Random();

            for (int i = 0; i < Charsarr.Length; i++)
            {
                Charsarr[i] = characters[random.Next(characters.Length)];
            }

            var resultString = new String(Charsarr);
            Console.WriteLine(resultString);
            return resultString;
        }

        /// <summary>
        /// 기존 이름들과 비교해서 새로운 이름 만들기
        /// </summary>
        /// <param name="names"> 기존 이름 array </param>
        /// <param name="number"> 자릿수 </param>
        /// <returns></returns>
        public static string RandomFileName(string[] names, int number = 10)
        {
            var characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var Charsarr = new char[number];
            var random = new Random();

            for (int i = 0; i < Charsarr.Length; i++)
            {
                Charsarr[i] = characters[random.Next(characters.Length)];
            }

            var resultString = new String(Charsarr);
            Console.WriteLine(resultString);

            foreach (string name in names)
            {
                if (name == resultString)
                {
                    resultString = RandomFileName(names, number);
                }
            }

            return resultString;
        }

        /// <summary>
        /// 기존 이름들과 비교해서 새로운 이름 만들기
        /// </summary>
        /// <param name="names"> 기존 이름 array </param>
        /// <param name="number"> 자릿수 </param>
        /// <returns></returns>
        public static string RandomFileName(List<string> names, int number = 10)
        {
            var characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var Charsarr = new char[number];
            var random = new Random();

            for (int i = 0; i < Charsarr.Length; i++)
            {
                Charsarr[i] = characters[random.Next(characters.Length)];
            }

            var resultString = new String(Charsarr);
            Console.WriteLine(resultString);

            foreach (string name in names)
            {
                if (name == resultString)
                {
                    resultString = RandomFileName(names, number);
                }
            }

            return resultString;
        }

        /// <summary>
        /// 이미지 검색 확장자 필터 설정
        /// </summary>
        private static readonly string[] imageExts = new[] { "jpg", "bmp", "png" }; // 검색할 확장자 필터

        /// <summary>
        /// 이미지 파일 검색
        /// </summary>
        /// <param name="path"> 폴더 경로</param>
        /// <param name="getFileFullPath"> 파일 전체 경로 출력 여부</param>
        /// <param name="subfoldersSearch"> 하위 폴더 검색 여부</param>
        /// <returns></returns>
        public static List<string> ImageFileSearch(string path, GetNameType fileNameMode = GetNameType.OnlyName, SubSearch subfoldersSearch = SubSearch.None, CancellationToken? CT = null)
        {
            return DirFileSearch(path, imageExts, fileNameMode, subfoldersSearch, CT);
        }

        /// <summary>
        /// 해시(Hash) 값 검증, MD5
        /// </summary>
        /// <param name="filePath"> file path </param>
        /// <param name="originalMD5"></param>
        /// <returns></returns>
        public static bool VerifyMD5(string filePath, string originalMD5)
        {
            string computedMD5 = GetMD5(filePath);
            return originalMD5.ToLower().Equals(computedMD5);
        }

        /// <summary>
        /// Get MD5
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string GetMD5(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            byte[] hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        /// <summary>
        /// 해시(Hash) 값 검증, SHA-256 
        /// </summary>
        /// <param name="filePath"> file path </param>
        /// <param name="originalSHA256"></param>
        /// <returns></returns>
        public static bool VerifySHA256(string filePath, string originalSHA256)
        {
            string computedSHA256 = GetSHA256(filePath);
            return originalSHA256.ToLower().Equals(computedSHA256);
        }

        /// <summary>
        /// Get SHA256
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string GetSHA256(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            byte[] hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        /// <summary>
        /// 파일 수정된 날짜 가져오기
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static DateTime GetFileModificationDate(string filePath)
        {
            FileInfo fileInfo = new(filePath);
            return fileInfo.LastWriteTime;
        }

        public static bool VerifyFileModificationDate(string filePath, DateTime orignalDateTime)
        {
            DateTime computedFileModificationDate = GetFileModificationDate(filePath);
            return computedFileModificationDate.Equals(orignalDateTime);
        }

        // find files in dir.

    }
}
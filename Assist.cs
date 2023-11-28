using FileIOAssist.Data;

namespace FileIOAssist
{
    public class Assist : IDisposable
    {
        #region IDisposable

        // To detect redundant calls
        private bool _disposed = false;

        ~Assist() => Dispose(false);

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // TODO: dispose managed state (managed objects).
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            _disposed = true;
        }

        #endregion IDisposable

        private Task? taskFileCopy;

        private CancellationTokenSource? ctsFileCopy;

        private object locker = new object();

        private long transByte = 0;

        public long TransByte
        {
            get
            {
                long data;
                lock (locker) { data = transByte; }
                return data;
            }
            private set
            {
                lock (locker)
                    transByte = value;
            }
        }

        private long totalByte = 0;

        public long TotalByte
        {
            get
            {
                long data;
                lock (locker) { data = totalByte; }
                return data;
            }
            private set
            {
                lock (locker)
                    totalByte = value;
            }
        }

        public void FileCopy(string source, string arrival)
        {
            if (this.ctsFileCopy == null || this.ctsFileCopy.IsCancellationRequested)
            {
                this.ctsFileCopy = new CancellationTokenSource();
                this.taskFileCopy = Task.Factory.StartNew(() => FileCopyBuffer(source, arrival), this.ctsFileCopy.Token);
            }
        }

        public static bool FileCopy(string source, string arrival, bool overrideFile = true)
        {
            if (!overrideFile && File.Exists(arrival)) { return true; }

            FileInfo fileInfo = new FileInfo(source);
            //버퍼 크기
            int iBufferSize = 5120;
            long lSize = 0;
            //총 파일 크기 얻기
            long lTotalSize = fileInfo.Length;
            //버퍼 크기만큼 바이트 배열 선언
            byte[] bTmp = new byte[iBufferSize];

            long TransByte = lTotalSize;
            bool result = false;

            FileStream fsRead = new FileStream(source, FileMode.Open);
            FileStream fsWrite = new FileStream(arrival, FileMode.Create);

            try
            {
                while (lSize < lTotalSize)
                {
                    int iLen = fsRead.Read(bTmp, 0, bTmp.Length);
                    lSize += iLen;
                    fsWrite.Write(bTmp, 0, iLen);

                    TransByte = lSize;
                }
                TransByte = lTotalSize;

                result = true;
            }
            catch
            {
                result = false;
            }
            finally
            {
                //파일 연결 해제...
                fsWrite.Flush();
                fsWrite.Close();
                fsRead.Close();
            }
            if (!result)
            {
                try
                {
                    File.Delete(source);
                }
                catch { }
            }
            return result;
        }

        public void FileCopyBuffer(string source, string arrival)
        {
            FileInfo fileInfo = new FileInfo(source);
            //버퍼 크기
            int iBufferSize = 5120;
            long lSize = 0;
            //총 파일 크기 얻기
            long lTotalSize = fileInfo.Length;
            //버퍼 크기만큼 바이트 배열 선언
            byte[] bTmp = new byte[iBufferSize];

            TransByte = lTotalSize;

            FileStream fsRead = new FileStream(source, FileMode.Open);
            FileStream fsWrite = new FileStream(arrival + "\\" + fileInfo.Name, FileMode.Create);

            try
            {
                while (lSize < lTotalSize)
                {
                    int iLen = fsRead.Read(bTmp, 0, bTmp.Length);
                    lSize += iLen;
                    fsWrite.Write(bTmp, 0, iLen);

                    TransByte = lSize;
                }
                TransByte = lTotalSize;
            }
            catch
            {
            }
            finally
            {
                //파일 연결 해제...
                fsWrite.Flush();
                fsWrite.Close();
                fsRead.Close();
            }
        }

        public static void FileCopyStreamReader(string source, string arrival)
        {
            using (StreamReader reader = new StreamReader(source))
            {
                // 출력 파일 생성
                using (StreamWriter writer = new StreamWriter(arrival))
                {
                    while (!reader.EndOfStream)
                    {
                        writer.WriteLine(reader.ReadLine());
                    }
                }
            }
        }

        /// <summary>
        /// 폴더 삭제 서브 폴더까지
        /// </summary>
        /// <param name="DeleteDirPath"></param>
        /// <returns></returns>
        public static bool DirDelete(string DeleteDirPath)
        {
            // Directory 파일 삭제
            try
            {
                DirectoryInfo directory = new DirectoryInfo(DeleteDirPath);
                foreach (FileInfo file in directory.EnumerateFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo subDirectory in directory.EnumerateDirectories())
                {
                    subDirectory.Delete(true);
                }
                directory.Delete();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 파일 삭제 성공시 true, 실패시 false
        /// </summary>
        /// <param name="fileNanmePath"> 삭제할 파일 경로</param>
        /// <returns></returns>
        public static bool FileRemove(string fileNanmePath)
        {
            try
            {
                if (File.Exists(fileNanmePath))
                {
                    File.Delete(fileNanmePath);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 폴더 복사
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="arrivalDir"></param>
        /// <returns>성공 여부</returns>
        public static bool DirCopy(string sourceDir, string arrivalDir, Extension.SubSearch subfoldersSearch = Extension.SubSearch.Full)
        {
            int faultFilesCount = 0;
            List<string> sourceFiles = Extension.DirFileSerch(sourceDir, Extension.GetNameType.Full, subfoldersSearch);
            foreach (string sourceFile in sourceFiles)
            {
                try
                {
                    string? sourceFileDirPath = Path.GetDirectoryName(sourceFile);
                    if (!string.IsNullOrEmpty(sourceFileDirPath))
                    {
                        string arrivalFilePath = sourceFile.Replace(sourceDir, arrivalDir);
                        string? arrivalFileDirPath = Path.GetDirectoryName(arrivalFilePath);
                        if (string.IsNullOrEmpty(arrivalFileDirPath))
                            continue;
                        if (!Directory.Exists(arrivalFileDirPath))
                            Directory.CreateDirectory(arrivalFileDirPath);
                        FileCopy(sourceFile, arrivalFilePath);
                    }
                }
                catch
                {
                    faultFilesCount++;
                }
            }
            if (faultFilesCount >= sourceFiles.Count)
                return false;
            else
                return true;
        }

        public static void DirCopy(string arrivalDir, DirectoryAssistInfo subDirectoryAssistInfo, Extension.SubSearch subDirectoryCopy = Extension.SubSearch.Full)
        {
            string subArrivalDir = arrivalDir + subDirectoryAssistInfo.WorkPath;
            if (!Directory.Exists(subArrivalDir)) { Directory.CreateDirectory(subArrivalDir); }
            foreach (var fileInfo in subDirectoryAssistInfo.Files)
            {
                string source = fileInfo.FullPath;
                string arrival = Path.Combine(subArrivalDir, fileInfo.Name);
                if (!FileCopy(source, arrival, false)) { continue; }
            }
            switch (subDirectoryCopy)
            {
                case Extension.SubSearch.None:
                    return;
                case Extension.SubSearch.Full:
                    foreach (var directoryAssistInfo in subDirectoryAssistInfo.SubDirectoryAssistInfos)
                    {
                        DirCopy(arrivalDir, directoryAssistInfo, subDirectoryCopy);
                    }
                    break;
            }
        }
    }
}
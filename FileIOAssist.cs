﻿using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace FileIOAssist
{
    public class FileIOAssist : IDisposable
    {
        #region IDisposable

        // To detect redundant calls
        private bool _disposed = false;

        ~FileIOAssist() => Dispose(false);

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

        public void FileCopyBuffer (string source, string arrival)
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
    }
}
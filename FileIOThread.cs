using System.Collections.Concurrent;
using System.Diagnostics;

namespace FileIOAssist
{
    /// <summary>
    /// 동작 타입
    /// </summary>
    internal enum ActionType
    {
        None = 0,
        Copy = 1,
        Move = 2,
        Delete = 3,
    }

    /// <summary>
    /// 파일의 작업 타입
    /// </summary>
    internal enum FileType
    {
        None = 0,
        File = 1,
        Directory = 2,
    }

    /// <summary> 
    /// 작은 수 = 더 큰 필터링 에러
    /// </summary>
    internal enum WorkerErrorCode
    {
        /// <summary>
        /// 정상
        /// </summary>
        OK = 0,
        /// <summary>
        /// 동작 타입 없음
        /// </summary>
        ActionTypeError,
        /// <summary>
        /// 파일 타입 없음
        /// </summary>
        FileTypeError,
        /// <summary>
        /// 각 동작 함수 확인 필요
        /// </summary>
        ActionFail,
    }

    /// <summary>
    /// 파일 정보
    /// </summary>
    internal class FileSet
    {
        public ActionType ActionType { get; init; } = ActionType.None;
        public FileType FileType { get; init; } = FileType.None;
        public string SourcePath { get; init; } = string.Empty;
        public string DestinationPath { get; init; } = string.Empty;
        public Extension.SubSearch SubFile { get; init; } = Extension.SubSearch.Full;
    }

    /// <summary>
    /// 파일 복사, 삭제 이동등 스래드로 넘기기
    /// </summary>
    public class FileIOThread : IDisposable
    {
        #region IDisposable

        protected bool _disposed = false;

        ~FileIOThread() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) { return; }

            if (disposing) { }
            _cts?.Cancel();
            _disposed = true;
        }

        #endregion IDisposable

        private static readonly Lazy<FileIOThread> _instance = new(() => new FileIOThread());

        public static FileIOThread Instance { get { return _instance.Value; } }

        /// <summary>
        /// 작업 파일 목록
        /// </summary>
        private readonly ConcurrentBag<IEnumerable<FileSet>> _files = new();

        /// <summary>
        /// 작업자 동작 제어
        /// </summary>
        private CancellationTokenSource? _cts;

        private FileIOThread()
        {

        }

        /// <summary>
        /// 파일 복사
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        public bool TryFIleCopyRegistration(string sourcePath, string destinationPath)
        {
            return TryFIleRegistration(new FileSet()
            {
                ActionType = ActionType.Copy,
                FileType = FileType.File,
                SourcePath = sourcePath,
                DestinationPath = destinationPath
            });
        }

        /// <summary>
        /// 파일들 복사
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        public bool TryFIlesCopyRegistration(IEnumerable<(string sourcePath, string destinationPath)> files)
        {
            List<FileSet> fileSets = new();
            foreach ((string sourcePath, string destinationPath) file in files)
            {
                fileSets.Add(new()
                {
                    ActionType = ActionType.Copy,
                    FileType = FileType.File,
                    SourcePath = file.sourcePath,
                    DestinationPath = file.destinationPath,
                });
            }
            if (fileSets.Count < 0) { return false; }
            return TryFIleRegistrations(fileSets);
        }

        /// <summary>
        /// 파일 이동
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        public bool TryFIleMoveRegistration(string sourcePath, string destinationPath)
        {
            return TryFIleRegistration(new FileSet()
            {
                ActionType = ActionType.Move,
                SourcePath = sourcePath,
                DestinationPath = destinationPath
            });
        }

        /// <summary>
        /// 파일 삭제
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <returns></returns>
        public bool TryFIleDeleteRegistration(string sourcePath)
        {
            return TryFIleRegistration(new FileSet()
            {
                ActionType = ActionType.Delete,
                SourcePath = sourcePath
            });
        }

        /// <summary>
        /// 정보 등록
        /// </summary>
        /// <param name="fileSet"></param>
        /// <returns></returns>
        internal bool TryFIleRegistration(FileSet fileSet)
        {
            List<FileSet> files = new()
            {
                fileSet
            };
            try { _files.Add(files); }
            catch (Exception ex) { Debug.WriteLine(ex); return false; }
            StartCheck();
            return true;
        }

        /// <summary>
        /// 정보 등록
        /// </summary>
        /// <param name="fileSets"></param>
        /// <returns></returns>
        internal bool TryFIleRegistrations(IEnumerable<FileSet> fileSets)
        {
            try { _files.Add(fileSets); }
            catch (Exception ex) { Debug.WriteLine(ex); return false; }
            StartCheck();
            return true;
        }

        /// <summary>
        /// 워커 동작 확인
        /// </summary>
        internal void StartCheck()
        {
            if (_cts == null)
            {
                _cts = new();
                _ = StartWorkerAsync(_cts.Token);
                return;
            }
            if (_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                _cts = new();
                _ = StartWorkerAsync(_cts.Token);
                return;
            }
        }

        /// <summary>
        /// 작업 시작
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal async Task<int> StartWorkerAsync(CancellationToken ct)
        {
            int delayCount = 0;
            while (!ct.IsCancellationRequested)
            {
                if (_files.IsEmpty)
                {
                    if (delayCount > 5) { break; }
                    delayCount++;
                    await Task.Delay(1000, ct);
                    continue;
                }
                delayCount = 0;

                if (!_files.TryTake(out IEnumerable<FileSet>? fileSets) && fileSets == null)
                {
                    throw new InvalidOperationException("FileIOThread Error, TryTake Error");
                }
                WorkerFileType(ref fileSets, ct); // 작업 동작
            }
            _cts?.Cancel();
            return 0;
        }

        /// <summary>
        /// 작업 파일 타입 검사
        /// </summary>
        /// <param name="fileSets"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        internal static WorkerErrorCode WorkerFileType(ref IEnumerable<FileSet> fileSets, CancellationToken ct)
        {
            List<WorkerErrorCode> errorCodes = new();
            foreach (FileSet fileSet in fileSets)
            {
                if (ct.IsCancellationRequested) { break; }

                FileSet fileSet1 = fileSet;
                switch (fileSet.FileType)
                {
                    case FileType.None:
                        errorCodes.Add(WorkerErrorCode.FileTypeError);
                        break;
                    case FileType.File:
                        errorCodes.Add(WorkerActionFile(ref fileSet1));
                        break;
                    case FileType.Directory:
                        errorCodes.Add(WorkerActionDirectory(ref fileSet1));
                        break;
                    default:
                        errorCodes.Add(WorkerErrorCode.FileTypeError);
                        break;
                }
            }
            return errorCodes.Max();
        }

        /// <summary>
        /// 파일 동작 타입 검사
        /// </summary>
        /// <param name="fileSet"></param>
        /// <returns></returns>
        internal static WorkerErrorCode WorkerActionFile(ref FileSet fileSet)
        {
            WorkerErrorCode errorCode = WorkerErrorCode.OK;
            switch (fileSet.ActionType)
            {
                case ActionType.None:
                    errorCode = WorkerErrorCode.FileTypeError;
                    break;
                case ActionType.Copy:
                    FileCopy(ref fileSet); // 파일 복사
                    break;
                case ActionType.Move:
                    FileMove(ref fileSet); // 파일 이동 
                    break;
                case ActionType.Delete:
                    FileDelete(ref fileSet); // 파일 삭제
                    break;
                default:
                    errorCode = WorkerErrorCode.FileTypeError;
                    break;
            }
            return errorCode;
        }

        /// <summary>
        /// 폴더 동작 타입 검사
        /// </summary>
        /// <param name="fileSet"></param>
        /// <returns></returns>
        internal static WorkerErrorCode WorkerActionDirectory(ref FileSet fileSet)
        {
            WorkerErrorCode errorCode = WorkerErrorCode.OK;
            switch (fileSet.ActionType)
            {
                case ActionType.None:
                    errorCode = WorkerErrorCode.FileTypeError;
                    break;
                case ActionType.Copy:
                    DirectoryCopy(ref fileSet); // 폴더 복사
                    break;
                case ActionType.Move:
                    DirectoryMove(ref fileSet); // 폴더 이동
                    break;
                case ActionType.Delete:
                    DirectoryDelete(ref fileSet);// 폴더 삭제
                    break;
                default:
                    errorCode = WorkerErrorCode.FileTypeError;
                    break;
            }
            return errorCode;
        }

        /// <summary>
        /// 파일 복사 동작
        /// </summary>
        /// <param name="fileSet"></param>
        /// <returns></returns>
        internal static WorkerErrorCode FileCopy(ref FileSet fileSet)
        {
            if (!Assist.FileCopy(fileSet.SourcePath, fileSet.DestinationPath))
            {
                return WorkerErrorCode.ActionFail;
            }
            return WorkerErrorCode.OK;
        }

        /// <summary>
        /// 파일 이동
        /// </summary>
        /// <param name="fileSet"></param>
        /// <returns></returns>
        internal static WorkerErrorCode FileMove(ref FileSet fileSet)
        {
            try
            {
                File.Move(fileSet.SourcePath, fileSet.DestinationPath, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return WorkerErrorCode.ActionFail;
            }
            return WorkerErrorCode.OK;
        }

        /// <summary>
        /// 파일 삭제
        /// </summary>
        /// <param name="fileSet"></param>
        /// <returns></returns>
        internal static WorkerErrorCode FileDelete(ref FileSet fileSet)
        {
            if (!Assist.FileRemove(fileSet.SourcePath))
            {
                return WorkerErrorCode.ActionFail;
            }
            return WorkerErrorCode.OK;
        }

        /// <summary>
        /// 폴더 복사
        /// </summary>
        /// <param name="fileset"></param>
        /// <returns></returns>
        internal static WorkerErrorCode DirectoryCopy(ref FileSet fileset)
        {
            if (!Assist.DirCopy(fileset.SourcePath, fileset.DestinationPath, fileset.SubFile))
            {
                return WorkerErrorCode.ActionFail;
            }
            return WorkerErrorCode.OK;
        }

        /// <summary>
        /// 폴더 이동
        /// </summary>
        /// <param name="fileset"></param>
        /// <returns></returns>
        internal static WorkerErrorCode DirectoryMove(ref FileSet fileset)
        {
            try
            {
                Directory.Move(fileset.SourcePath, fileset.DestinationPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return WorkerErrorCode.ActionFail;
            }
            return WorkerErrorCode.OK;
        }

        /// <summary>
        /// 폴더 삭제
        /// </summary>
        /// <param name="fileset"></param>
        /// <returns></returns>
        internal static WorkerErrorCode DirectoryDelete(ref FileSet fileset)
        {
            if (!Assist.DirDelete(fileset.SourcePath))
            {
                return WorkerErrorCode.ActionFail;
            }
            return WorkerErrorCode.OK;
        }
    }
}

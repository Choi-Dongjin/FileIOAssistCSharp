using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileIOAssist
{
    public class FileIOAssistExtension
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

        /// <summary>
        /// 폴더내 파일 검색
        /// </summary>
        /// <param name="dirPath"> 대상 경로 </param>
        /// <param name="getFileNameMode"> "Full", "Name" </param>
        /// <returns></returns>
        public static List<string> DirFileSerch(string dirPath, string getFileNameMode)
        {
            List<string> fileList = new List<string>();
            bool fileNameModeBool;

            if (getFileNameMode == "Full")
            {
                fileNameModeBool = true;
            }
            else if (getFileNameMode == "Name")
            {
                fileNameModeBool = false;
            }
            else
            {
                fileNameModeBool = false;
            }

            DirectoryInfo di = new DirectoryInfo(dirPath);
            if (di.Exists)
            {
                if (fileNameModeBool)
                {
                    foreach (FileInfo fileInfo in di.GetFiles())
                    {
                        fileList.Add(fileInfo.FullName);
                    }
                }
                else
                {
                    foreach (FileInfo fileInfo in di.GetFiles())
                    {
                        fileList.Add(fileInfo.Name);
                    }
                }
            }
            return fileList;
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
        private static readonly string[] imageExts = new[] { ".jpg", ".bmp", ".png" }; // 검색할 확장자 필터

        /// <summary>
        /// 이미지 파일 검색
        /// </summary>
        /// <param name="path"> 폴더 경로 </param>
        /// <param name="getFileFullPath"> 파일 전체 경로 출력 여부, true: 전체 경로 출력 - false: 파일 이름 출력</param>
        /// <returns></returns>
        public static List<string> ImageFileFileSearch(string path, bool getFileFullPath)
        {
            List<string> filesList = new List<string>();
            string[] files;
            try
            {
                string[] exts = imageExts; // 검색할 확장자 필터

                string[] dirs = Directory.GetDirectories(path);
                files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly).Where(s => exts.Contains(Path.GetExtension(s), StringComparer.OrdinalIgnoreCase)).ToArray();

                if (getFileFullPath)
                    foreach (string fullFileName in files)
                        filesList.Add(fullFileName);
                else
                    foreach (string fullFileName in files)
                        filesList.Add(Path.GetFileName(fullFileName));
            }
            catch (Exception ex)
            {
                files = null;
                Console.WriteLine(ex);
            }
            return filesList;
        }

        /// <summary>
        /// 이미지 파일 검색
        /// </summary>
        /// <param name="path"> 폴더 경로</param>
        /// <param name="getFileFullPath"> 파일 전체 경로 출력 여부</param>
        /// <param name="subfoldersSearch"> 하위 폴더 검색 여부</param>
        /// <returns></returns>
        public static List<string> ImageFileFileSearch(string path, bool getFileFullPath, bool subfoldersSearch)
        {
            List<string> filesList = new List<string>();
            try
            {
                string[] exts = imageExts; // 검색할 확장자 필터

                string[] dirs = Directory.GetDirectories(path);
                string[] files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly).Where(s => exts.Contains(Path.GetExtension(s), StringComparer.OrdinalIgnoreCase)).ToArray();

                foreach (string FullFileName in files)
                {
                    // 이 곳에 해당 파일을 찾아서 처리할 코드를 삽입하면 된다.
                    // Console.WriteLine($"[{count++}] path - {FullFileName}");

                    // Delete a file by using File class static method...
                    if (System.IO.File.Exists(FullFileName))
                    {
                        // Use a try block to catch IOExceptions, to
                        // handle the case of the file already being
                        // opened by another process.
                        if (getFileFullPath)
                            foreach (string fullFileName in files)
                                filesList.Add(fullFileName);
                        else
                            foreach (string fullFileName in files)
                                filesList.Add(Path.GetFileName(fullFileName));
                    }
                }

                //하위 폴더가지 확인 재귀 함수를 이용한 구현
                if (dirs.Length > 0 && subfoldersSearch)
                {
                    foreach (string dir in dirs)
                    {
                        filesList.AddRange(ImageFileFileSearch(dir, true));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return filesList;
        }
    }
}

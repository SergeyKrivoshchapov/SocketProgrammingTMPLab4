using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Task1GUI.Common;

namespace Task1GUI.Models
{
    public interface IDiskDriver
    {
        List<string> GetAllDisks();

        List<string> GetDirectoryContent(string path);

        string GetFileContent(string path);

        /// <summary>
        /// Получить содержимое текущей директории с отформатированными элементами (без префиксов)
        /// </summary>
        List<string> GetCurrentDirectoryContentFormatted();

        /// <summary>
        /// Навигировать в подпапку
        /// </summary>
        bool NavigateToFolder(string formattedFolderName);

        /// <summary>
        /// Вернуться в родительскую папку
        /// </summary>
        bool NavigateBack();

        /// <summary>
        /// Получить полный путь текущей директории
        /// </summary>
        string GetCurrentPath();

        /// <summary>
        /// Очистить навигацию (вернуться в корень диска)
        /// </summary>
        void ClearNavigation();

        /// <summary>
        /// Получить отформатированное имя элемента (без префикса F| или D|)
        /// </summary>
        string GetFormattedItemName(string rawItem);

        /// <summary>
        /// Получить полное имя элемента с префиксом для отправки на сервер
        /// </summary>
        string GetRawItemName(string formattedItem, bool isFolder);

        /// <summary>
        /// Инициализировать навигацию для выбранного диска
        /// </summary>
        void SetCurrentDrive(string drive);

        /// <summary>
        /// Проверить, можно ли вернуться в родительскую папку
        /// </summary>
        bool CanNavigateBack();
    }

    public class DiskDriver : IDiskDriver
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Reply
        {
            public int status;
            public IntPtr msg;
        }

        private List<string> _disks;
        private Stack<string> _navigationStack = new Stack<string>();
        private string _currentPath = string.Empty;
        private string _currentDrive = string.Empty;

        public DiskDriver(List<string> disks)
        {
            _disks = disks;
        }

        public List<string> GetAllDisks() => _disks;

        /// <summary>
        /// Инициализировать навигацию для выбранного диска
        /// </summary>
        public void SetCurrentDrive(string drive)
        {
            _currentDrive = drive;
            _currentPath = drive.EndsWith("\\") ? drive : drive + "\\";
            _navigationStack.Clear();
        }

        public List<string> GetDirectoryContent(string path)
        {
            IntPtr replyPtr = GetDirCont(path);
            if (replyPtr == IntPtr.Zero)
            {
                return [];
            }

            try
            {
                Reply reply = Marshal.PtrToStructure<Reply>(replyPtr);

                bool status = reply.status == 0;
                string raw = Marshal.PtrToStringUTF8(reply.msg) ?? string.Empty;

                if (!status || string.IsNullOrWhiteSpace(raw))
                {
                    return [];
                }

                return raw.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                          .Select(x => x.Trim())
                          .Where(x => x.Length > 0)
                          .ToList();
            }
            finally
            {
                FreeReply(replyPtr);
            }
        }

        public string GetFileContent(string path)
        {
            IntPtr replyPtr = GetFileCont(path);
            if (replyPtr == IntPtr.Zero)
            {
                return string.Empty;
            }

            try
            {
                Reply reply = Marshal.PtrToStructure<Reply>(replyPtr);

                bool status = reply.status == 0;
                string raw = Marshal.PtrToStringUTF8(reply.msg) ?? string.Empty;

                return status ? raw : string.Empty;
            }
            finally
            {
                FreeReply(replyPtr);
            }
        }

        [DllImport(Constants.dllTask1ClientName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetDirectoryContent")]
        private static extern IntPtr GetDirCont([MarshalAs(UnmanagedType.LPUTF8Str)] string path);

        [DllImport(Constants.dllTask1ClientName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetFileContent")]
        private static extern IntPtr GetFileCont([MarshalAs(UnmanagedType.LPUTF8Str)] string path);

        [DllImport(Constants.dllTask1ClientName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "FreeReply")]
        private static extern void FreeReply(IntPtr reply);

        /// <summary>
        /// Получить содержимое текущей директории с отформатированными элементами (без префиксов)
        /// </summary>
        public List<string> GetCurrentDirectoryContentFormatted()
        {
            var rawContent = GetDirectoryContent(GetCurrentPath());
            return rawContent.Select(item => GetFormattedItemName(item)).ToList();
        }

        /// <summary>
        /// Навигировать в подпапку
        /// </summary>
        public bool NavigateToFolder(string formattedFolderName)
        {
            if (string.IsNullOrEmpty(formattedFolderName))
                return false;

            var newPath = CombineWindowsPath(_currentPath, formattedFolderName);

            var content = GetDirectoryContent(newPath);

            _navigationStack.Push(_currentPath);
            _currentPath = newPath;

            return true;
        }

        /// <summary>
        /// Вернуться в родительскую папку
        /// </summary>
        public bool NavigateBack()
        {
            if (_navigationStack.Count == 0)
                return false;

            // Восстанавливаем предыдущий путь из стека
            _currentPath = _navigationStack.Pop();
            return true;
        }

        /// <summary>
        /// Проверить, можно ли вернуться в родительскую папку
        /// </summary>
        public bool CanNavigateBack()
        {
            return _navigationStack.Count > 0;
        }

        /// <summary>
        /// Получить полный путь текущей директории
        /// </summary>
        public string GetCurrentPath()
        {
            return _currentPath;
        }

        /// <summary>
        /// Очистить навигацию (вернуться в корень диска)
        /// </summary>
        public void ClearNavigation()
        {
            _navigationStack.Clear();
            _currentPath = string.Empty;
            _currentDrive = string.Empty;
        }

        /// <summary>
        /// Получить отформатированное имя элемента (без префикса F| или D|)
        /// </summary>
        public string GetFormattedItemName(string rawItem)
        {
            if (string.IsNullOrEmpty(rawItem) || rawItem.Length < 3)
                return rawItem;

            // Проверяем формат "X|Name"
            if ((rawItem[0] == 'F' || rawItem[0] == 'D') && rawItem[1] == '|')
            {
                return rawItem.Substring(2);
            }

            return rawItem;
        }

        /// <summary>
        /// Получить полное имя элемента с префиксом для отправки на сервер
        /// </summary>
        public string GetRawItemName(string formattedItem, bool isFolder)
        {
            if (string.IsNullOrEmpty(formattedItem))
                return formattedItem;

            var prefix = isFolder ? "D|" : "F|";
            return prefix + formattedItem;
        }

        private static string CombineWindowsPath(string basePath, string childName)
        {
            var normalizedBase = basePath.TrimEnd('\\');
            return normalizedBase + "\\" + childName;
        }
    }
}

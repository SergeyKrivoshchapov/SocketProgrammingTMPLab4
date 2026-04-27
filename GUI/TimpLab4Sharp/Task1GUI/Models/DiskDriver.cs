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
    }

    //public class DiskDriverMock : IDiskDriver
    //{
    //    public List<string> GetAllDisks()
    //    {
    //        return DriveInfo.GetDrives().Select(d => d.Name).ToList();
    //    }

    //    public bool IsDiskReady(string diskName, int timeoutMilliseconds = 2000)
    //    {
    //        try
    //        {
    //            var driveInfo = new DriveInfo(diskName);
    //            if (driveInfo.DriveType != DriveType.Network)
    //                return driveInfo.IsReady;

    //            var task = Task.Run(() => driveInfo.IsReady);
    //            if (task.Wait(timeoutMilliseconds))
    //                return task.Result;
    //            else
    //                return false;
    //        }
    //        catch (Exception)
    //        {
    //            return false;
    //        }
    //    }

    //    public List<string> GetDirectoryContent(string path)
    //    {
    //        return Directory.GetDirectories(path).Concat(Directory.GetFiles(path)).ToList();
    //    }

    //    public bool IsFile(string path)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public bool IsFolder(string path)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    public class DiskDriver : IDiskDriver
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Reply
        {
            public int status;
            public IntPtr msg;
        }

        private List<string> _disks;

        public DiskDriver(List<string> disks)
        {
            _disks = disks;
        }

        public List<string> GetAllDisks() => _disks;

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

    }
}

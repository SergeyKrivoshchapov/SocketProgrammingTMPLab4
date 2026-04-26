using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Task1GUI.Models
{
    public interface IDiskDriver
    {
        List<string> GetAllDisks();

        bool IsDiskReady(string disk, int timeoutMilliseconds = 2000);

        List<string> GetDiskContent(string disk);

        bool IsFile(string path);

        bool IsFolder(string path);
    }

    public class DiskDriver : IDiskDriver
    {
        public List<string> GetAllDisks()
        {
            return DriveInfo.GetDrives().Select(d => d.Name).ToList();
        }

        public bool IsDiskReady(string diskName, int timeoutMilliseconds = 2000)
        {
            try
            {
                var driveInfo = new DriveInfo(diskName);
                if (driveInfo.DriveType != DriveType.Network)
                    return driveInfo.IsReady;

                var task = Task.Run(() => driveInfo.IsReady);
                if (task.Wait(timeoutMilliseconds))
                    return task.Result;
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public List<string> GetDiskContent(string disk)
        {
            return Directory.GetDirectories(disk).Concat(Directory.GetFiles(disk)).ToList();
        }

        public bool IsFile(string path)
        {
            throw new NotImplementedException();
        }

        public bool IsFolder(string path)
        {
            throw new NotImplementedException();
        }
    }
}

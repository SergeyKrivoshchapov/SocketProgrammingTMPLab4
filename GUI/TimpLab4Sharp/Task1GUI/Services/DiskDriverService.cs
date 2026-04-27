using System;
using System.Collections.Generic;
using System.Text;
using Task1GUI.Models;

namespace Task1GUI.Services
{
    public interface IDiskDriverService
    {
        IDiskDriver GetDiskDriver(List<string> disks);
    }

    public class DiskDriverService : IDiskDriverService
    {
        private Func<List<string>, IDiskDriver> _diskDriverCreate;

        public DiskDriverService(Func<List<string>, IDiskDriver> diskDriverCreate)
        {
            _diskDriverCreate = diskDriverCreate;
        }

        public IDiskDriver GetDiskDriver(List<string> disks)
        {
            return _diskDriverCreate.Invoke(disks);
        }
    }
}

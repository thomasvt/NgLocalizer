using System;
using System.Diagnostics;
using System.IO;

namespace NgLocalizer
{
    internal class CodeBaseWatcher
    {
        private readonly string _folder;
        private FileSystemWatcher _fileSystemWatcher;

        public CodeBaseWatcher(string folder)
        {
            _folder = folder;
        }

        public void Initialize()
        {
            _fileSystemWatcher = new FileSystemWatcher(_folder);
            _fileSystemWatcher.Changed += (sender, args) => { ChangeDetected?.Invoke(); };
            _fileSystemWatcher.Created += (sender, args) => { ChangeDetected?.Invoke(); };
            _fileSystemWatcher.Deleted += (sender, args) => { ChangeDetected?.Invoke(); };
            _fileSystemWatcher.Renamed += (sender, args) => { ChangeDetected?.Invoke(); };
            _fileSystemWatcher.Filter = "";
            _fileSystemWatcher.IncludeSubdirectories = true;
            _fileSystemWatcher.NotifyFilter = NotifyFilters.Attributes |
                             NotifyFilters.CreationTime |
                             NotifyFilters.FileName |
                             NotifyFilters.LastAccess |
                             NotifyFilters.LastWrite |
                             NotifyFilters.Size;
            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        public void Enable(bool isEnabled)
        {
            _fileSystemWatcher.EnableRaisingEvents = isEnabled;
        }

        public event Action ChangeDetected;

        public void Dispose()
        {
            _fileSystemWatcher?.Dispose();
        }
    }
}

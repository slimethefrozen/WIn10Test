using System;


namespace Win10Test
{
    public class FileEntry
    {
        public Boolean? Check;
        public string Content;

        public FileEntry(string _path, bool _isSelected)
        {
            Content = _path;
            Check = _isSelected;
        }
    }
}

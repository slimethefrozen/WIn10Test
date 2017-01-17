using System;

public class FileEntry
{
    public bool isSelected;
    public string path;

	public FileEntry(string _path, bool _isSelected)
	{
        path = _path;
        isSelected = _isSelected;
	}
}

// File: Models/FileContentView.cs
public class FileContentView
{
    public string FullPath { get; set; }
    public string Content { get; set; }
    public List<string> Lines { get; set; } // For line numbering
}
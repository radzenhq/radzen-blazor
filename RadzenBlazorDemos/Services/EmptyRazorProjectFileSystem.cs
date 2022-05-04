using Microsoft.AspNetCore.Razor.Language;
using System;
using System.Collections.Generic;
using System.IO;

namespace RadzenBlazorDemos
{
    public class EmptyRazorProjectFileSystem : RazorProjectFileSystem
    {
        public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
        {
            return Array.Empty<RazorProjectItem>();
        }

        [Obsolete]
        public override RazorProjectItem GetItem(string path)
        {
            return new VirtualProjectItem(null, null, null, null, null, null);
        }

        public override RazorProjectItem GetItem(string path, string fileKind)
        {
            return new VirtualProjectItem(null, null, null, null, null, null);
        }
    }

    public class VirtualProjectItem : RazorProjectItem
    {
        public VirtualProjectItem(
            string basePath,
            string filePath,
            string physicalPath,
            string relativePhysicalPath,
            string fileKind,
            byte[] content)
        {
            BasePath = basePath;
            FilePath = filePath;
            PhysicalPath = physicalPath;
            RelativePhysicalPath = relativePhysicalPath;
            Content = content;
            FileKind = fileKind;
        }

        public override string BasePath { get; }

        public override string RelativePhysicalPath { get; }

        public override string FileKind { get; }

        public override string FilePath { get; }

        public override string PhysicalPath { get; }

        public override bool Exists => true;

        public byte[] Content { get; set; }

        public override Stream Read()
        {
            return Content == null ? new MemoryStream() : new MemoryStream(Content);
        }
    }
}

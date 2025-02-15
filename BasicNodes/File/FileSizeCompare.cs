namespace FileFlows.BasicNodes.File
{
    using System.ComponentModel;
    using FileFlows.Plugin;
    using FileFlows.Plugin.Attributes;

    public class FileSizeCompare : Node
    {
        public override int Inputs => 1;
        public override int Outputs => 3;
        public override FlowElementType Type => FlowElementType.Logic;
        public override string Icon => "fas fa-sitemap";

        public override int Execute(NodeParameters args)
        {
            FileInfo fiOriginal = new FileInfo(args.FileName);
            float origSize = 0;
            if (fiOriginal.Exists == false)
            {
                // try get from variables
                if (args.Variables.ContainsKey("file.Orig.Size") && args.Variables["file.Orig.Size"] is long tSize && tSize > 0)
                {
                    origSize = tSize;
                }
                else
                {
                    args.Logger?.ELog("Original file does not exists, cannot check size");
                    return -1;
                }
            }
            else
            {
                origSize = fiOriginal.Length;
            }

            float wfSize = 0;
                FileInfo fiWorkingFile = new FileInfo(args.WorkingFile);
            if (fiWorkingFile.Exists == false)
            {
                if (args.WorkingFileSize > 0)
                {
                    wfSize = args.WorkingFileSize;
                }
                else
                {
                    args.Logger?.ELog("Working file does not exists, cannot check size");
                    return -1;
                }
            }
            else
            {
                wfSize = fiWorkingFile.Length;
            }
            

            args.Logger?.ILog("Original File Size: " + String.Format("{0:n0}", origSize));
            args.Logger?.ILog("Working File Size: " + String.Format("{0:n0}", wfSize));


            if (wfSize > origSize)
            {
                args.Logger?.ILog("Working file is larger than original");
                return 3;
            }
            if (origSize == wfSize)
            {
                args.Logger?.ILog("Working file is same size as the original");
                return 2;
            }
            args.Logger?.ILog("Working file is smaller than original");
            return 1;
        }
    }
}
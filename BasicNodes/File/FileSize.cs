namespace FileFlows.BasicNodes.File
{
    using System.ComponentModel;
    using FileFlows.Plugin;
    using FileFlows.Plugin.Attributes;

    public class FileSize : Node
    {
        public override int Inputs => 1;
        public override int Outputs => 2;
        public override FlowElementType Type => FlowElementType.Logic;
        public override string Icon => "fas fa-balance-scale-right";


        [NumberInt(1)]
        public int Lower { get; set; }

        [NumberInt(2)]
        public int Upper { get; set; }

        public override int Execute(NodeParameters args)
        {
            var fi = new FileInfo(args.WorkingFile);
            if (fi.Exists == false) {
                args.Logger.ELog("File Does not exist: " + args.WorkingFile);
                return -1;
            }

            return TestSize(args, fi.Length);
        }

        public int TestSize(NodeParameters args, long size)
        {
            if (size < (((long)Lower) * 1024 * 1024))
                return 2;
            if (Upper > 0 && size > (((long)Upper) * 1024 * 1024))
                return 2;
            return 1;
        }
    }
}
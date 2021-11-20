namespace FileFlows.VideoNodes
{
    using System.ComponentModel;
    using FileFlows.Plugin;
    using FileFlows.Plugin.Attributes;

    public class FFMPEG : EncodingNode
    {
        public override int Outputs => 1;

        [DefaultValue("-i {WorkingFile} {TempDir}output.mkv")]
        [TextArea(1)]
        public string CommandLine { get; set; }

        public override string Icon => "far fa-file-video";

        private NodeParameters args;

        public override int Execute(NodeParameters args)
        {
            if (string.IsNullOrEmpty(CommandLine))
            {
                args.Logger.ELog("Command Line not set");
                return -1;
            }
            this.args = args;
            try
            {
                VideoInfo videoInfo = GetVideoInfo(args);
                if (videoInfo == null)
                    return -1;

                string ffmpegExe = GetFFMpegExe(args);
                if (string.IsNullOrEmpty(ffmpegExe))
                    return -1;

                string cmd = CommandLine.Replace("{WorkingFile}", "\"" + args.WorkingFile + "\"")
                                        .Replace("{TempDir}", "\"" + args.TempPath + Path.DirectorySeparatorChar + "\"");

                if (Encode(args, ffmpegExe, CommandLine) == false)
                    return -1;

                return 1;
            }
            catch (Exception ex)
            {
                args.Logger.ELog("Failed processing VideoFile: " + ex.Message);
                return -1;
            }
        }
    }
}
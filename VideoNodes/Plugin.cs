namespace FileFlows.VideoNodes;

using System.ComponentModel.DataAnnotations;
using FileFlows.Plugin.Attributes;

public class Plugin : FileFlows.Plugin.IPlugin
{
    public Guid Uid => new Guid("881b486b-4b38-4e66-b39e-fbc0fc9deee1");
    public string Name => "Video Nodes";
    public string MinimumVersion => "0.8.3.1487";

    public void Init()
    {
    }
}

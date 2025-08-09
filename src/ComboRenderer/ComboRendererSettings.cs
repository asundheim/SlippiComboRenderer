using Newtonsoft.Json;
using Slippi.NET.Melee.Types;

namespace ComboRenderer;

[JsonObject(MissingMemberHandling = MissingMemberHandling.Ignore)]
internal class ComboRendererSettings
{
    public string TrackCharacter 
    {
        get; 
        set
        {
            if (field != value)
            {
                field = value;
                CharacterChanged?.Invoke(this, value);
            }
        }
    } = Character.Fox.ToString();
    public event EventHandler<string>? CharacterChanged;
    
    public List<string> ConnectCodes { get; set; } = new List<string>();
    public List<string> DisplayNames { get; set; } = new List<string>();

    public bool EnableDICam { get; set; } = true;

    public bool FollowDolphin
    { 
        get; 
        set
        {
            if (field != value)
            {
                field = value;
                FollowDolphinChanged?.Invoke(this, value);
            }
        }
    } = true;
    public event EventHandler<bool>? FollowDolphinChanged;

    public double ExplicitWidth 
    { 
        get; 
        set
        {
            if (field != value)
            {
                field = value;
                ExplicitWidthChanged?.Invoke(this, value);
            }
        }
    } = 500;
    public event EventHandler<double>? ExplicitWidthChanged;
    
    public double ExplicitHeight 
    { 
        get; 
        set
        {
            if (field != value)
            {
                field = value;
                ExplicitHeightChanged?.Invoke(this, value);
            }
        }
    } = 500;
    public event EventHandler<double>? ExplicitHeightChanged;

    public string TrackWindow
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                TrackWindowChanged?.Invoke(this, value);
            }
        }
    } = "Live";
    public event EventHandler<string>? TrackWindowChanged;

    public string ReplayIsoPath { get; set; } = string.Empty;

    public string ReplayDolphinPath { get; set; } = string.Empty;

    public string OBSAddress { get; set; } = string.Empty;

    public int OBSPort { get; set; } = 4455;
}

using ComboInterpreter.Types;
using Slippi.NET.Melee.Types;
using Slippi.NET.Stats.Types;
using Slippi.NET.Types;

namespace ComboInterpreter.ComboInterpreters;

public class FoxComboInterpreter : SpaciesComboInterpreter
{
    public FoxComboInterpreter(string liveGamePath, params string[] netplayCodesOrNames) 
        : base(Character.Fox, false, -1, liveGamePath, netplayCodesOrNames)
    {
    }

    public FoxComboInterpreter(string replayPath, int startFrame, params string[] netplayCodesOrNames)
        : base(Character.Fox, true, startFrame, replayPath, netplayCodesOrNames)
    {
    }

    protected override Actions ComputeActionFromActionState(ActionState actionState)
    {
        Actions overrideAction = Actions.None; // tracepoint here with {actionState} to log all action states
        switch (actionState)
        {
            case ActionState.FOX_UPB_A_STARTUP:
            case ActionState.FOX_UPB_G_STARTUP:
                overrideAction = Actions.FirefoxStartup;
                break;
            case ActionState.FOX_UPB_A:
            case ActionState.FOX_UPB_G:
                overrideAction = Actions.Firefox;
                break;
            default:
                break;
        }

        if (overrideAction != Actions.None)
        {
            return overrideAction;
        }

        return base.ComputeActionFromActionState(actionState);
    }
}

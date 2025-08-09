using Slippi.NET.Melee.Types;
using Slippi.NET.Stats.Types;
using Slippi.NET.Types;

namespace ComboInterpreter.ComboInterpreters;

public class FalcoComboInterpreter : SpaciesComboInterpreter
{
    public FalcoComboInterpreter(string liveGamePath, params string[] netplayCodesOrNames)
    : base(Character.Falco, false, -1, liveGamePath, netplayCodesOrNames)
    {
    }

    public FalcoComboInterpreter(string replayPath, int startFrame, params string[] netplayCodesOrNames)
        : base(Character.Falco, true, startFrame, replayPath, netplayCodesOrNames)
    {
    }

    protected override Actions ComputeActionFromActionState(ActionState actionState)
    {
        Actions overrideAction = Actions.None;
        switch (actionState)
        {
            case ActionState.FALCO_UPB_A_STARTUP:
            case ActionState.FALCO_UPB_G_STARTUP:
                overrideAction = Actions.FireBirdStartup;
                break;
            case ActionState.FALCO_UPB_A:
            case ActionState.FALCO_UPB_G:
                overrideAction = Actions.FireBird;
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

using ComboInterpreter.Types;
using Slippi.NET.Melee.Types;
using Slippi.NET.Stats.Types;
using Slippi.NET.Types;

namespace ComboInterpreter.ComboInterpreters;
public abstract class SpaciesComboInterpreter : BaseComboInterpreter
{
    public SpaciesComboInterpreter(Character character, bool isReplay, int startFrame, string gamePath, params string[] netplayCodesOrNames)
        : base(character, isReplay, startFrame, gamePath, netplayCodesOrNames)
    {
    }

    protected override void HandleActionEvent(ActionEvent actionEvent)
    {
        switch (actionEvent.Action)
        {
            case Actions.Jab:
                {
                    _pendingBuffer.Add(new PendingAction()
                    {
                        Action = actionEvent,
                        ContinuationIf = static c => c.Action == Actions.USmash || c.Action == Actions.Bair || c.Action == Actions.UAir || c.Action == Actions.DSmash,
                        ActionsLeft = 3,
                        FramesLeft = 20,
                    });

                    break;
                }
            case Actions.Shine:
                {
                    _pendingBuffer.Add(new PendingAction()
                    {
                        Action = actionEvent,
                        ActionsLeft = 5,
                        FramesLeft = 20,
                        ContinuationIf = static (c) => c.Action == Actions.Bair ||
                                                       c.Action == Actions.Nair ||
                                                       c.Action == Actions.DAir ||
                                                       c.Action == Actions.Fair ||
                                                       c.Action == Actions.UAir ||
                                                       c.Action == Actions.USmash ||
                                                       c.Action == Actions.DSmash ||
                                                       c.Action == Actions.FSmash ||
                                                       c.Action == Actions.Jab ||
                                                       c.Action == Actions.Grab ||
                                                       c.Action == Actions.ShineTurnaround ||
                                                       c.Action == Actions.Jump ||
                                                       c.Action == Actions.Wavedash ||
                                                       c.Action == Actions.Waveland,
                        AppendContinuationWithIf = static (c) => c.Action != Actions.Jump &&
                                                                 c.Action != Actions.Wavedash &&
                                                                 c.Action != Actions.USmash &&
                                                                 c.Action != Actions.Grab &&
                                                                 c.Action != Actions.ShineTurnaround,
                        AppendContinuationWith = new ActionEvent()
                        {
                            Action = Actions.JumpCancel,
                            FrameEntry = actionEvent.FrameEntry,
                            HasContinuation = true,
                        },
                    });

                    break;
                }
            case Actions.ShineTurnaround:
                {
                    _pendingBuffer.Add(new PendingAction()
                    {
                        Action = actionEvent,
                        ActionsLeft = 3,
                        ContinuationIf = static (c) => c.Action == Actions.Bair ||
                                                       c.Action == Actions.Nair ||
                                                       c.Action == Actions.DAir ||
                                                       c.Action == Actions.Fair ||
                                                       c.Action == Actions.UAir ||
                                                       c.Action == Actions.USmash ||
                                                       c.Action == Actions.DSmash ||
                                                       c.Action == Actions.FSmash ||
                                                       c.Action == Actions.Jab ||
                                                       c.Action == Actions.Grab ||
                                                       c.Action == Actions.ShineTurnaround ||
                                                       c.Action == Actions.Jump ||
                                                       c.Action == Actions.Wavedash ||
                                                       c.Action == Actions.Waveland
                    });

                    break;
                }
            case Actions.JumpCancel:
                {
                    _pendingBuffer.Add(new PendingAction()
                    {
                        Action = actionEvent,
                        ActionsLeft = 1,
                        ContinuationIf = static (c) => c.Action == Actions.USmash ||
                                                       c.Action == Actions.Grab ||
                                                       c.Action == Actions.Shine,
                        CancelIf = static (c) => c.Action != Actions.USmash &&
                                                 c.Action != Actions.Grab &&
                                                 c.Action != Actions.Shine || c.Action == Actions.JumpCancel,
                    });

                    break;
                }
            case Actions.Jump:
                {
                    if (DidShineRecentlyForJumpCancel())
                    {
                        _pendingBuffer.Add(new PendingAction()
                        {
                            Action = new ActionEvent() { Action = Actions.JumpCancel, FrameEntry = actionEvent.FrameEntry },
                            ActionsLeft = 1,
                            ContinuationIf = static (c) => c.Action == Actions.Bair ||
                                                           c.Action == Actions.Nair ||
                                                           c.Action == Actions.DAir ||
                                                           c.Action == Actions.Fair ||
                                                           c.Action == Actions.UAir ||
                                                           c.Action == Actions.USmash ||
                                                           c.Action == Actions.Grab ||
                                                           c.Action == Actions.Jab,
                            CancelIf = static (c) => c.Action == Actions.Wavedash ||
                                                     c.Action == Actions.Waveland ||
                                                     c.Action == Actions.AirDodge

                        });
                    }
                    else
                    {
                        _pendingBuffer.Add(new PendingAction()
                        {
                            Action = actionEvent,
                            FramesLeft = 8,
                            CancelIf = static (c) => c.Action == Actions.Wavedash ||
                                                     c.Action == Actions.Waveland ||
                                                     c.Action == Actions.AirDodge
                        });
                    }

                    break;
                }
            case Actions.WallJump:
                {
                    _pendingBuffer.Add(new PendingAction()
                    {
                        Action = actionEvent,
                        ActionsLeft = 1,
                        ContinuationIf = static (c) => c.Action == Actions.Bair || c.Action == Actions.Shine
                    });

                    break;
                }
            case Actions.FirefoxStartup:
            case Actions.Firefox:
            case Actions.FireBirdStartup:
            case Actions.FireBird:
            case Actions.Laser:
            case Actions.SideB:
                {
                    InterpretActionEvent(actionEvent);
                    break;
                }
            default:
                {
                    base.HandleActionEvent(actionEvent);
                    break;
                }
        }
    }

    protected override Actions ComputeActionFromActionState(ActionState actionState)
    {
        Actions overrideAction = Actions.None;

        // Fox and Falco share all the same ActionState values, but both are included for clarity.
        // Presumably the compiler can optimize this away.
        switch (actionState)
        {
            case ActionState.FOX_SHINE_A or ActionState.FALCO_SHINE_A:
            case ActionState.FOX_SHINE_G or ActionState.FALCO_SHINE_G:
                overrideAction = Actions.Shine;
                break;
            case ActionState.FOX_LASER_A or ActionState.FALCO_LASER_A:
            case ActionState.FOX_LASER_G or ActionState.FALCO_LASER_G:
                overrideAction = Actions.Laser;
                break;
            case ActionState.FOX_SHINE_TURNAROUND_A or ActionState.FALCO_SHINE_TURNAROUND_A:
            case ActionState.FOX_SHINE_TURNAROUND_G or ActionState.FALCO_SHINE_TURNAROUND_G:
                overrideAction = Actions.ShineTurnaround;
                break;
            case ActionState.FOX_SHINE_END_A or ActionState.FALCO_SHINE_END_A:
            case ActionState.FOX_SHINE_END_G or ActionState.FALCO_SHINE_END_G:
                overrideAction = Actions.ShineEnd;
                break;
            case ActionState.FOX_SIDEB_A or ActionState.FALCO_SIDEB_A:
            case ActionState.FOX_SIDEB_G or ActionState.FALCO_SIDEB_G:
                overrideAction = Actions.SideB;
                break;
        }

        if (overrideAction != Actions.None)
        {
            return overrideAction;
        }

        return base.ComputeActionFromActionState(actionState);
    }

    protected override void InterpretActionEvent(ActionEvent actionEvent)
    {
        SimpleButtons buttons = actionEvent.FrameEntry.Players![_playerIndex]!.Pre!.ToSimpleButtons();
        bool facingLeft = (actionEvent.FrameEntry.Players![_playerIndex]!.Post!.FacingDirection ?? 0) < 0;

        switch (actionEvent.Action)
        {
            case Actions.DAir:
            {
                _combos.Add(new InterpretedCombo()
                {
                    ActionEvent = actionEvent,
                    DisplayName = "drill",
                    HasContinuation = actionEvent.HasContinuation,
                    Buttons = SimpleButtons.CSTICK_DOWN,
                    EndsCombo = false,
                });

                return;
            }
            case Actions.Laser:
            {
                _combos.Add(new InterpretedCombo()
                {
                    ActionEvent = actionEvent,
                    DisplayName = "laser",
                    HasContinuation = false,
                    Buttons = SimpleButtons.B,
                    EndsCombo = _character == Character.Fox,
                });

                return;
            }
            case Actions.SideB:
            {
                _combos.Add(new InterpretedCombo()
                {
                    ActionEvent = actionEvent,
                    DisplayName = "side b",
                    HasContinuation = false,
                    Buttons = Utils.FacingDirectionToStick(facingLeft) | SimpleButtons.B,
                    EndsCombo = true, // TODO teeter cancel
                });

                return;
            }
            case Actions.Shine:
            {
                _combos.Add(new InterpretedCombo()
                {
                    ActionEvent = actionEvent,
                    DisplayName = "shine",
                    HasContinuation = actionEvent.HasContinuation,
                    Buttons = SimpleButtons.STICK_DOWN | SimpleButtons.B,
                    EndsCombo = false,
                });

                return;
            }
            case Actions.ShineTurnaround:
            {
                SimpleButtons direction = Utils.FacingDirectionToStick(facingLeft);
                _combos.Add(new InterpretedCombo()
                {
                    ActionEvent = actionEvent,
                    DisplayName = direction == SimpleButtons.STICK_LEFT ? "↩" : "↪",
                    HasContinuation = actionEvent.HasContinuation,
                    Buttons = direction,
                    EndsCombo = false,
                });

                return;
            }
            case Actions.FirefoxStartup:
            case Actions.FireBirdStartup:
            {
                _combos.Add(new InterpretedCombo()
                {
                    ActionEvent = actionEvent,
                    DisplayName = "up b",
                    HasContinuation = false,
                    Buttons = SimpleButtons.STICK_UP | SimpleButtons.B,
                    EndsCombo = false,
                });

                return;
            }
            case Actions.FireBird:
            case Actions.Firefox:
            {
                SimpleButtons direction = Utils.GetStick(buttons);
                _combos.Add(new InterpretedCombo()
                {
                    ActionEvent = actionEvent,
                    DisplayName = "up b",
                    HasContinuation = false,
                    Buttons = direction | SimpleButtons.B,
                    EndsCombo = true,
                });

                return;
            }
        }

        base.InterpretActionEvent(actionEvent);
    }

    private bool DidShineRecentlyForJumpCancel() => _eventBuffer.Count > 4 &&
                                      _eventBuffer[^1].Action != Actions.JumpCancel &&
                                      _eventBuffer[^2].Action != Actions.JumpCancel &&
                                      (_eventBuffer[^1].Action == Actions.Shine ||
                                       _eventBuffer[^2].Action == Actions.Shine ||
                                       _eventBuffer[^3].Action == Actions.Shine ||
                                       _eventBuffer[^4].Action == Actions.Shine);
}

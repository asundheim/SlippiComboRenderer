using ComboInterpreter.ComboInterpreters;
using Slippi.NET.Melee.Data;
using Slippi.NET.Melee.Types;
using Slippi.NET.Stats.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ComboRenderer;
internal static class Utils
{
    public static void PopInOut(this UIElement animatable)
    {
        animatable.RenderTransformOrigin = new Point(0.5, 0.5);

        ScaleTransform scale = new ScaleTransform();
        animatable.RenderTransform = scale;

        DoubleAnimation popOut = new DoubleAnimation(fromValue: 0, toValue: 1.1, duration: new Duration(TimeSpan.FromMilliseconds(100)), FillBehavior.Stop);
        DoubleAnimation popIn = new DoubleAnimation(fromValue: 1.2, toValue: 1, duration: new Duration(TimeSpan.FromMilliseconds(30)), FillBehavior.Stop);
        popOut.Completed += OnPopOut;

        animatable.RenderTransform.ApplyAnimationClock(ScaleTransform.ScaleXProperty, popOut.CreateClock());
        animatable.RenderTransform.ApplyAnimationClock(ScaleTransform.ScaleYProperty, popOut.CreateClock());

        void OnPopOut(object? sender, EventArgs e)
        {
            popOut.Completed -= OnPopOut;
            animatable.RenderTransform.ApplyAnimationClock(ScaleTransform.ScaleXProperty, popIn.CreateClock());
            animatable.RenderTransform.ApplyAnimationClock(ScaleTransform.ScaleYProperty, popIn.CreateClock());
        }
    }

    public static bool IsRepeatedAction(this Actions previousAction, Actions nextAction)
    {
        if ((previousAction == Actions.FirefoxStartup && nextAction == Actions.Firefox) ||
            (previousAction == Actions.FireBirdStartup && nextAction == Actions.FireBird))
        {
            return true;
        }
        else if (previousAction == nextAction)
        {
            return previousAction switch
            {
                Actions.DashDance or Actions.Wavedash => true,
                _ => false
            };
        }

        return false;
    }

    // some actions have a startup longer than the timeout, but we don't want them to timeout before the next action comes out
    public static bool IsLongStartupAction(this Actions action) => action switch
    {
        Actions.FirefoxStartup or Actions.FireBirdStartup => true,
        _ => false
    };

    public static BaseComboInterpreter GetComboInterpreterForSettings(string gamePath, bool isLive, int startFrame = -1)
    {
        string[] codes = [..SettingsManager.Instance.Settings.ConnectCodes, ..SettingsManager.Instance.Settings.DisplayNames];
        switch (SettingsManager.Instance.Settings.TrackCharacter)
        {
            case "Fox":
            {
                if (isLive)
                {
                    return new FoxComboInterpreter(gamePath, codes);
                }
                else
                {
                    return new FoxComboInterpreter(gamePath, startFrame, codes);
                }
            }
            case "Falco":
            {
                if (isLive)
                {
                    return new FalcoComboInterpreter(gamePath, codes);
                }
                else
                {
                    return new FalcoComboInterpreter(gamePath, startFrame, codes);
                }
            }
        }

        throw new ArgumentException("Settings gives unknown character");
    }
}

namespace MauiTouchBehavior;

using System.Text;
using TouchBehaviorLib;

public partial class MainPage : ContentPage
{
	bool            _captured = false;
    TouchBehavior   _behavior = null;

    public MainPage()
	{
		InitializeComponent();
	}

    private void OnTouchBehaviorActive( object sender, TouchBehaviorLib.TouchActionEventArgs e )
    {
        if ( sender is Label label )
        {
            if ( _behavior is null && label.Behaviors.First() is TouchBehavior behavior )
            { 
                _captured = behavior.Capture;
                _behavior = behavior;
            }
            
            if ( _behavior is not null)
            {
                var sb = new StringBuilder();

                switch (e.Type)
                {
                    case TouchActionType.Entered:
                    {
                        sb.AppendLine( "Entered: " );
                        break;
                    }

                    case TouchActionType.Pressed:
                    {
                        sb.AppendLine( "Pressed: " );
                        _behavior.Capture = true;
                        break;
                    }

                    case TouchActionType.Moved:
                    {
                        sb.AppendLine( "Moved: " );
                        break;
                    }

                   case TouchActionType.Released:
                   {
                        sb.AppendLine( "Released: " );
                        _behavior.Capture = false;
                        break;
                   }

                    case TouchActionType.Exited:
                    {
                        label.Text = "I handle Touch actions! Click or hover!";
                        _behavior = null;
                        return;
                    }

                    case TouchActionType.Cancelled:
                    {
                        sb.AppendLine( "Cancelled: " );
                        _behavior = null;
                        break;
                    }

                    default:
                        break;
                }

                var simpleLocation = $"X={double.Round(e.Location.X)}, Y={double.Round(e.Location.Y)}";

                sb.AppendLine( $"Location: {simpleLocation}" );
                sb.AppendLine( $"IsInContact: {e.IsInContact}" );

                label.Text = sb.ToString();
            }
        }
    }
}


namespace TouchBehaviorLib;

using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Behaviors;

// All the code in this file is included in all platforms.
public partial class TouchBehavior : BaseBehavior<VisualElement>
{
    public delegate void TouchEventHandler( object sender, TouchActionEventArgs args );
    public event         TouchEventHandler? TouchAction;

    public bool Capture { set; get; }

    public void OnTouchAction( object sender, TouchActionEventArgs args ) => TouchAction?.Invoke( sender, args );
}

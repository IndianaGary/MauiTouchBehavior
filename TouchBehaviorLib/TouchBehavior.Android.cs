namespace TouchBehaviorLib;

using Microsoft.Maui.Platform;
using Android.Views;
using Android.Text.Method;
using CommunityToolkit.Maui.Behaviors;

public partial class TouchBehavior : BaseBehavior<VisualElement>
{
    Android.Views.View?             _nativeView;
    VisualElement?                  _boundElement;

    Func<double, double>?           _fromPixels;
    readonly int[]                  _screenLocationArray = new int[2];

    static readonly Dictionary<Android.Views.View, TouchBehavior> _viewToBehaviorCache    = new();
    static readonly Dictionary<int, TouchBehavior>                _idToBehaviorCache      = new();

    protected override void OnAttachedTo( BindableObject sender )
    {
        if ( sender is VisualElement bindable )
        {
            bindable.HandlerChanged += OnHandlerChanged;
            base.OnAttachedTo( bindable );
        }
    }

    protected override void OnDetachingFrom( BindableObject sender )
    {
        if ( sender is VisualElement bindable )
        {
            bindable.HandlerChanged -= OnHandlerChanged;

            if ( _viewToBehaviorCache.ContainsKey( _nativeView! ) )
            {
                _viewToBehaviorCache.Remove( _nativeView! );
                _nativeView!.Touch -= OnTouch;
            }

            base.OnDetachingFrom( bindable );
        }
    }

    void OnHandlerChanged( object? sender, EventArgs e )
    {
        if ( sender is not VisualElement bindable || bindable.Handler is null )
            return;

        var context = bindable.Handler.MauiContext ?? bindable.Parent.Handler.MauiContext;

        if ( context is null )
            throw new NullReferenceException( "MauiContext is null" );

        // Get the Android View corresponding to the VisualElement that the behavior is attached to
        _boundElement = bindable;
        _nativeView     = bindable.ToPlatform( context );

        if ( _nativeView is not null )
        {
            _viewToBehaviorCache.Add( _nativeView, this );
            _fromPixels = _nativeView.Context.FromPixels;

            // Save the method to call for touch events
            _nativeView.Touch += OnTouch;
        }
    }

    void OnTouch( object? sender, Android.Views.View.TouchEventArgs args )
    {
        ArgumentNullException.ThrowIfNull( sender );
        ArgumentNullException.ThrowIfNull( args );
        ArgumentNullException.ThrowIfNull( args.Event );

        // Two objects common to all the events
        var androidView = sender as Android.Views.View;
        var motionEvent = args.Event;

        // Get the pointer index
        var pointerIndex = motionEvent.ActionIndex;

        // Get the id that identifies a finger over the course of its progress
        var id  = motionEvent.GetPointerId( pointerIndex );

        androidView?.GetLocationOnScreen( _screenLocationArray );

        Point screenPointerCoords = new( _screenLocationArray[ 0 ] + motionEvent.GetX( pointerIndex ),
                                         _screenLocationArray[ 1 ] + motionEvent.GetY( pointerIndex ) );

        // Use ActionMasked here rather than Action to reduce the number of possibilities
        //
        switch ( args.Event.ActionMasked )
        {
            case MotionEventActions.Down:
            case MotionEventActions.PointerDown:
                FireEvent( id, TouchActionType.Pressed, screenPointerCoords, true );
                _idToBehaviorCache.Add( id, this );
                break;

            case MotionEventActions.Move:
                // Multiple Move events are bundled, so handle them in a loop
                for ( pointerIndex = 0; pointerIndex < motionEvent.PointerCount; pointerIndex++ )
                {
                    id = motionEvent.GetPointerId( pointerIndex );

                    if ( Capture )
                    {
                        androidView?.GetLocationOnScreen( _screenLocationArray );

                        screenPointerCoords = new Point( _screenLocationArray[ 0 ] + motionEvent.GetX( pointerIndex ),
                                                         _screenLocationArray[ 1 ] + motionEvent.GetY( pointerIndex ) );

                        FireEvent( id, TouchActionType.Moved, screenPointerCoords, true );
                    }
                    else
                    {
                        CheckForBoundaryHop( id, screenPointerCoords );

                        if ( _idToBehaviorCache[ id ] is not null )
                            FireEvent( id, TouchActionType.Moved, screenPointerCoords, true );
                    }
                }

                break;

            case MotionEventActions.Up:
            case MotionEventActions.Pointer1Up:
                if ( Capture )
                {
                    FireEvent( id, TouchActionType.Released, screenPointerCoords, false );
                }
                else
                {
                    CheckForBoundaryHop( id, screenPointerCoords );

                    if ( _idToBehaviorCache[ id ] is not null )
                        FireEvent( id,TouchActionType.Released, screenPointerCoords, false );
                }

                _idToBehaviorCache.Remove( id );
                break;

            case MotionEventActions.Cancel:
                if ( Capture )
                {
                    FireEvent( id, TouchActionType.Cancelled, screenPointerCoords, false );
                }
                else
                {
                    if ( _idToBehaviorCache[ id ] is not null )
                        FireEvent( id, TouchActionType.Cancelled, screenPointerCoords, false );
                }

                _idToBehaviorCache.Remove( id );
                break;
        }
    }

    void CheckForBoundaryHop( int id, Point pointerLocation )
    {
        TouchBehavior? touchHit = null;

        foreach ( var view in _viewToBehaviorCache.Keys )
        {
            // Get the view rectangle
            try
            {
                view.GetLocationOnScreen( _screenLocationArray );
            }
            catch // System.ObjectDisposedException: Cannot access a disposed object.
            {
                continue;
            }

            Rect viewRect = new( _screenLocationArray[0],
                                 _screenLocationArray[1],
                                 view.Width, view.Height );

            if ( viewRect.Contains( pointerLocation ) )
                touchHit = _viewToBehaviorCache[ view ];
        }

        if ( touchHit != _idToBehaviorCache[ id ] )
        {
            if ( _idToBehaviorCache[ id ] is not null )
                FireEvent( id, TouchActionType.Exited, pointerLocation, true );

            if ( touchHit is not null )
                FireEvent( id, TouchActionType.Entered, pointerLocation, true );

            _idToBehaviorCache[ id ] = touchHit!;
        }
    }

    void FireEvent( int id, TouchActionType actionType, Point pointerLocation, bool isInContact )
    {
        // Get the location of the pointer within the view
        _nativeView?.GetLocationOnScreen( _screenLocationArray );

        var x = pointerLocation.X - _screenLocationArray[0];
        var y = pointerLocation.Y - _screenLocationArray[1];

        Point point = new( _fromPixels!(x), _fromPixels(y) );

        // Call the method
        OnTouchAction( _boundElement!, new TouchActionEventArgs( id, actionType, point, isInContact ) );
    }
}

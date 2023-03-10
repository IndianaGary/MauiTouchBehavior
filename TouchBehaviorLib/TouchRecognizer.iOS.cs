namespace TouchBehaviorLib;

using CoreGraphics;

using Foundation;
using UIKit;

public class TouchRecognizer : UIGestureRecognizer
{
    VisualElement    _element;           // Maui _element for firing events
    UIView           _uiView;            // iOS UIView 
    TouchBehavior    _touchBehavior;
    bool             _currentCapture;

    static Dictionary<UIView, TouchRecognizer>   _viewToRecognizerCache  = new();
    static Dictionary<long, TouchRecognizer>     _idToTouchCache         = new();

    public TouchRecognizer( VisualElement element, UIView uiView, TouchBehavior touchBehavior )
    {
        _element        = element;
        _uiView         = uiView;
        _touchBehavior  = touchBehavior;
        _currentCapture = touchBehavior.Capture;

        _viewToRecognizerCache.Add( _uiView, this );
    }

    public void Detach()
    {
        _viewToRecognizerCache.Remove( _uiView );
    }

    // touches = touches of interest; evt = all touches of type UITouch
    public override void TouchesBegan( NSSet touches, UIEvent evt )
    {
        base.TouchesBegan( touches, evt );

        foreach ( UITouch touch in touches.Cast<UITouch>() )
        {
            long id = ((IntPtr)touch.Handle).ToInt64();

            FireEvent( this, id, TouchActionType.Pressed, touch, true );

            if ( !_idToTouchCache.ContainsKey( id ) )
            {
                _idToTouchCache.Add( id, this );
            }
        }

        // Save the setting of the _capture property
        _currentCapture = _touchBehavior.Capture;
    }

    public override void TouchesMoved( NSSet touches, UIEvent evt )
    {
        base.TouchesMoved( touches, evt );

        foreach ( UITouch touch in touches.Cast<UITouch>() )
        {
            long id = ((IntPtr)touch.Handle).ToInt64();

            if ( _currentCapture )
            {
                FireEvent( this, id, TouchActionType.Moved, touch, true );
            }
            else
            {
                CheckForBoundaryHop( touch );

                if ( _idToTouchCache[ id ] is not null )
                {
                    FireEvent( _idToTouchCache[ id ], id, TouchActionType.Moved, touch, true );
                }
            }
        }
    }

    public override void TouchesEnded( NSSet touches, UIEvent evt )
    {
        base.TouchesEnded( touches, evt );

        foreach ( UITouch touch in touches.Cast<UITouch>() )
        {
            long id = ((IntPtr)touch.Handle).ToInt64();

            if ( _currentCapture )
            {
                FireEvent( this, id, TouchActionType.Released, touch, false );
            }
            else
            {
                CheckForBoundaryHop( touch );

                if ( _idToTouchCache[ id ] is not null )
                {
                    FireEvent( _idToTouchCache[ id ], id, TouchActionType.Released, touch, false );
                }
            }
            _idToTouchCache.Remove( id );
        }
    }

    public override void TouchesCancelled( NSSet touches, UIEvent evt )
    {
        base.TouchesCancelled( touches, evt );

        foreach ( UITouch touch in touches.Cast<UITouch>() )
        {
            long id = ((IntPtr)touch.Handle).ToInt64();

            if ( _currentCapture )
            {
                FireEvent( this, id, TouchActionType.Cancelled, touch, false );
            }
            else if ( _idToTouchCache[ id ] is not null )
            {
                FireEvent( _idToTouchCache[ id ], id, TouchActionType.Cancelled, touch, false );
            }
            _idToTouchCache.Remove( id );
        }
    }

    void CheckForBoundaryHop( UITouch touch )
    {
        long id = ((IntPtr)touch.Handle).ToInt64();

        // TODO: Might require converting to a List for multiple hits
        TouchRecognizer? recognizerHit = null;

        foreach ( UIView _uiView in _viewToRecognizerCache.Keys )
        {
            CGPoint location = touch.LocationInView( _uiView );

            if ( new CGRect( new CGPoint(), _uiView.Frame.Size ).Contains( location ) )
            {
                recognizerHit = _viewToRecognizerCache[ _uiView ];
            }
        }

        if ( recognizerHit != _idToTouchCache[ id ] )
        {
            if ( _idToTouchCache[ id ] is not null )
            {
                FireEvent( _idToTouchCache[ id ], id, TouchActionType.Exited, touch, true );
            }

            if ( recognizerHit is not null )
            {
                FireEvent( recognizerHit, id, TouchActionType.Entered, touch, true );
            }

            _idToTouchCache[ id ] = recognizerHit!;
        }
    }

    void FireEvent( TouchRecognizer recognizer, long id, TouchActionType actionType, UITouch touch, bool isInContact )
    {
        // Convert touch location to Xamarin.Forms Point value
        var cgPoint = touch.LocationInView( recognizer.View );
        var point   = new Point( cgPoint.X, cgPoint.Y );

        // Get the method to call for firing events
        var onTouchAction = recognizer._touchBehavior.OnTouchAction;

        // Call that method
        onTouchAction( recognizer._element,
                       new TouchActionEventArgs( id, actionType, point, isInContact ) );
    }
}

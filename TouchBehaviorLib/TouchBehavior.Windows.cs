namespace TouchBehaviorLib;

using CommunityToolkit.Maui.Behaviors;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

public partial class TouchBehavior : BaseBehavior<VisualElement>
{
    Action<VisualElement, TouchActionEventArgs>?    _onTouchAction;
    FrameworkElement?                               _frameworkElement;
    VisualElement?                                  _boundElement;

    protected override void OnAttachedTo( BindableObject sender )
    {
        if ( sender is not VisualElement bindable )
            return;

        bindable.HandlerChanged += OnHandlerChanged;

        base.OnAttachedTo( bindable );
    }

    void OnHandlerChanged( object? sender, EventArgs e )
    {
        if ( sender is not VisualElement bindable || bindable.Handler is null )
            return;

        var context =   bindable.Handler.MauiContext ?? bindable.Parent.Handler.MauiContext;

        if ( context is null )
            throw new NullReferenceException( "MauiContext is null" );

        // Gets the Windows FrameworkElement corresponding to the VisualElement that the Behavior is attached to
        _boundElement = bindable;
        _frameworkElement   = bindable.ToPlatform( context );

        if ( _frameworkElement is not null )
        {
            // Set event handlers on FrameworkElement
            _frameworkElement.PointerEntered    += OnPointerEntered;
            _frameworkElement.PointerPressed    += OnPointerPressed;
            _frameworkElement.PointerMoved      += OnPointerMoved;
            _frameworkElement.PointerReleased   += OnPointerReleased;
            _frameworkElement.PointerExited     += OnPointerExited;
            _frameworkElement.PointerCanceled   += OnPointerCancelled;

            // Record the method to call for touch events
            _onTouchAction = OnTouchAction;
        }
    }

    protected override void OnDetachingFrom( BindableObject sender )
    {
        if ( sender is VisualElement bindable )
        {
            bindable.HandlerChanged -= OnHandlerChanged;

            if ( _frameworkElement is not null && _onTouchAction is not null )
            {
                // Release event handlers on FrameworkElement
                _frameworkElement.PointerEntered    -= OnPointerEntered;
                _frameworkElement.PointerPressed    -= OnPointerPressed;
                _frameworkElement.PointerMoved      -= OnPointerMoved;
                _frameworkElement.PointerReleased   -= OnPointerReleased;
                _frameworkElement.PointerExited     -= OnPointerEntered;
                _frameworkElement.PointerCanceled   -= OnPointerCancelled;
            }
        }

        base.OnDetachingFrom( sender );
    }

    void OnPointerEntered( object sender, PointerRoutedEventArgs args )
        => CommonHandler( sender, TouchActionType.Entered, args );

    void OnPointerMoved( object sender, PointerRoutedEventArgs args )
        => CommonHandler( sender, TouchActionType.Moved, args );

    void OnPointerReleased( object sender, PointerRoutedEventArgs args )
        => CommonHandler( sender, TouchActionType.Released, args );

    void OnPointerExited( object sender, PointerRoutedEventArgs args )
        => CommonHandler( sender, TouchActionType.Exited, args );

    void OnPointerCancelled( object sender, PointerRoutedEventArgs args )
        => CommonHandler( sender, TouchActionType.Cancelled, args );

    void OnPointerPressed( object sender, PointerRoutedEventArgs args )
    {
        CommonHandler( sender, TouchActionType.Pressed, args );

        if ( sender is FrameworkElement frameworkElement )
        {
            // Check setting of Capture property
            if ( Capture )
                frameworkElement.CapturePointer( args.Pointer );
        }
    }

    void CommonHandler( object sender, TouchActionType touchActionType, PointerRoutedEventArgs args )
    {
        if ( sender is FrameworkElement frameworkElement )
        {
            var pointerPoint                    = args.GetCurrentPoint( frameworkElement );
            Windows.Foundation.Point winPoint   = pointerPoint.Position;

            _onTouchAction!( _boundElement!,
                             new TouchActionEventArgs( args.Pointer.PointerId,
                                                       touchActionType,
                                                       new Point( winPoint.X, winPoint.Y ),
                                                       args.Pointer.IsInContact ));
        }
    }
}

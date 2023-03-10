namespace TouchBehaviorLib;

using CommunityToolkit.Maui.Behaviors;
using Microsoft.Maui.Platform;
using UIKit;

public partial class TouchBehavior : BaseBehavior<VisualElement>
{
    UIView?              _nativeView;
    VisualElement?       _boundElement;
    TouchRecognizer?     _recognizer;

    protected override void OnAttachedTo( BindableObject? sender )
    {
        if ( sender is VisualElement bindable )
        {
            bindable.HandlerChanged += OnHandlerChanged;

            base.OnAttachedTo( bindable );

            var mauiContext = bindable?.Handler?.MauiContext ?? bindable?.Parent.Handler.MauiContext;

            if ( mauiContext is null )
                throw new NullReferenceException( "MauiContext is null" );

            _boundElement   = bindable;
            _nativeView     = bindable?.ToPlatform( mauiContext );

            if ( _nativeView is not null )
                _nativeView.UserInteractionEnabled = true;
        }
    }

    void OnHandlerChanged( object? sender, EventArgs e )
    {
        if ( sender is VisualElement bindable && bindable.Handler is not null && _nativeView is not null )
        {
            // Create a TouchRecognizer for this UIView
            _recognizer = new TouchRecognizer( _boundElement!, _nativeView, this );
            _nativeView.AddGestureRecognizer( _recognizer );
        }
    }

    protected override void OnDetachingFrom( BindableObject? sender )
    {
        if ( sender is VisualElement bindable )
        {
            bindable.HandlerChanged -= OnHandlerChanged;

            if ( _recognizer is not null )
            {
                // Clean up the TouchRecognizer object
                _recognizer.Detach();

                // Remove the TouchRecognizer from the UIView
                _nativeView?.RemoveGestureRecognizer( _recognizer );
            }

            base.OnDetachingFrom( bindable );
        }
    }
}

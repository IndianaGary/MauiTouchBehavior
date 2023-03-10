namespace TouchBehaviorLib;

public class TouchActionEventArgs : EventArgs
{
    public long Id              { get; }
    public TouchActionType Type { get; }
    public Point Location       { get; }
    public bool IsInContact     { get; }

    public TouchActionEventArgs( long id, TouchActionType type, Point location, bool isInContact )
    {
        Id          = id;
        Type        = type;
        Location    = location;
        IsInContact = isInContact;
    }

}

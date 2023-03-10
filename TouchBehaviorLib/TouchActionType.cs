namespace TouchBehaviorLib;

public enum TouchActionType
{
    Entered,		//	StartHoverIntereaction
    Pressed,		//	StartInteraction
    Moved,			//	DragInteraction 
    Released,		//	EndInteraction
    Exited,			//	EndHoverInteraction
    Cancelled		//	CancelInteraction
                    //  MoveHoverInteraction ? Not handled yet
}

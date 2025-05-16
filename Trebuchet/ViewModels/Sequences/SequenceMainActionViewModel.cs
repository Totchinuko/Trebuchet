using TrebuchetLib.Sequences;

namespace Trebuchet.ViewModels.Sequences;

[SequenceAction(typeof(SequenceMainAction))]
public class SequenceMainActionViewModel(SequenceMainAction action) : 
    SequenceActionViewModel<SequenceMainActionViewModel, SequenceMainAction>(action)
{
    
}
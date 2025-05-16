using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DynamicData.Binding;
using ReactiveUI;
using tot_lib;
using Trebuchet.Assets;
using TrebuchetLib.Sequences;

namespace Trebuchet.ViewModels.Sequences;

public abstract class SequenceActionViewModel : ReactiveObject
{
    public event EventHandler? ActionChanged;

    public string Label { get; protected set; } = string.Empty;
    public abstract ISequenceAction SequenceAction { get; }
    
    protected virtual void OnActionChanged()
    {
        ActionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetLabel(string label)
    {
        Label = label;
    }
    
    public static SequenceActionViewModel MakeViewModel(ISequenceAction action)
    {
        var type = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
            .FirstOrDefault(x => x.GetCustomAttribute<SequenceActionAttribute>()?.Target == action.GetType());
        if (type is null)
            throw new Exception($@"Type not found for {action.GetType()}");

        var vm = Activator.CreateInstance(type, action);
        if (vm is not SequenceActionViewModel savm)
            throw new Exception($@"Invalid type for {action.GetType()}");
        savm.SetLabel(Resources.ResourceManager.GetString(action.GetType().Name, Resources.Culture) ?? @"INVALID");
        return savm;
    }
}

public abstract class SequenceActionViewModel<T, TSeqAction> : SequenceActionViewModel
    where T : SequenceActionViewModel<T, TSeqAction>
    where TSeqAction : class,ISequenceAction
{
    public SequenceActionViewModel(TSeqAction action)
    {
        Action = action;

        this.WhenAnyPropertyChanged()
            .Select(_ => Unit.Default)
            .InvokeCommand(ReactiveCommand.Create(OnActionChanged));
    }
    
    public TSeqAction Action { get; }

    public override ISequenceAction SequenceAction => Action;
}
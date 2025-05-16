using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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

public class SequenceViewModel : ReactiveObject
{
    public SequenceViewModel(Sequence sequence)
    {
        Sequence = sequence;
        Actions.AddRange(sequence.Actions.Select(MakeActionViewModel));
        ActionTypes.AddRange(GenerateActionTypeList());

        Add = ReactiveCommand.Create(() =>
        {
            PopupOpen = true;
        });

        AddAction = ReactiveCommand.Create<Type>((type) =>
        {
            var value = Activator.CreateInstance(type);
            if (value is not ISequenceAction action) return;
            PopupOpen = false;
            Actions.Add(MakeActionViewModel(action));
        });

        Remove = ReactiveCommand.Create<SequenceActionViewModel>((vm) =>
        {
            Actions.Remove(vm);
        });

        Actions.CollectionChanged += (_,_) => OnActionChanged(this, EventArgs.Empty);
    }

    private bool _popupOpen;

    public bool PopupOpen
    {
        get => _popupOpen;
        set => this.RaiseAndSetIfChanged(ref _popupOpen, value);
    }
    
    public ObservableCollectionExtended<SequenceActionViewModel> Actions { get; } = [];
    public List<SequenceActionTypeViewModel> ActionTypes { get; } = [];
    
    public Sequence Sequence { get; }
    
    public ReactiveCommand<Unit, Unit> Add { get; }
    public ReactiveCommand<Type, Unit> AddAction { get; } 
    public ReactiveCommand<SequenceActionViewModel, Unit> Remove { get; }

    public event EventHandler? SequenceChanged;

    public IEnumerable<ISequenceAction> GetSequence()
    {
        return Actions.Select(x => x.SequenceAction);
    }

    private SequenceActionViewModel MakeActionViewModel(ISequenceAction action)
    {
        var viewModel = SequenceActionViewModel.MakeViewModel(action);
        viewModel.ActionChanged += OnActionChanged;
        return viewModel;
    }

    private IEnumerable<SequenceActionTypeViewModel> GenerateActionTypeList()
    {
        var type = typeof(ISequenceAction);
        return AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
            .Where(x => type.IsAssignableFrom(x) && !x.IsAbstract)
            .Select(x => new SequenceActionTypeViewModel(
                x, 
                Resources.ResourceManager.GetString(x.Name, Resources.Culture) ?? @"INVALID")
            );
    }

    private void OnActionChanged(object? sender, EventArgs args)
    {
        SequenceChanged?.Invoke(this, EventArgs.Empty);
    }
}
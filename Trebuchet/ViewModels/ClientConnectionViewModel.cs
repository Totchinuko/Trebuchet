using System;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using tot_lib;
using TrebuchetLib;

namespace Trebuchet.ViewModels;

public class ClientConnectionViewModel : ReactiveObject
{
    public ClientConnectionViewModel(ClientConnection connection, bool readOnly)
    {
        _ipAddress = connection.IpAddress;
        _port = connection.Port.ToString();
        _password = connection.Password;
        _name = connection.Name;
        _isReadOnly = readOnly;

        _valid = this.WhenAnyValue(x => x.IpAddress, x => x.Port)
            .Select(_ => Validate())
            .ToProperty(this, x => x.Valid);

        var canDelete = this.WhenAnyValue(x => x.IsReadOnly, (iro) => !iro);
        Delete = ReactiveCommand.CreateFromTask(OnDeleted, canDelete);
    }

    private string _name;
    private string _ipAddress;
    private string _port;
    private string _password;
    private bool _isReadOnly;
    private ObservableAsPropertyHelper<bool> _valid;
    
    public ReactiveCommand<Unit,Unit> Delete { get; }

    public event AsyncEventHandler? Deleted;

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }
    
    public string IpAddress
    {
        get => _ipAddress;
        set => this.RaiseAndSetIfChanged(ref _ipAddress, value);
    }

    public string Port
    {
        get => _port;
        set => this.RaiseAndSetIfChanged(ref _port, value);
    }

    public string Password
    {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    public bool IsReadOnly
    {
        get => _isReadOnly;
        set => this.RaiseAndSetIfChanged(ref _isReadOnly, value);
    }

    public bool Valid
    {
        get => _valid.Value;
    }

    public ClientConnection Export()
    {
        return new ClientConnection()
        {
            IpAddress = IpAddress,
            Name = Name,
            Password = Password,
            Port = int.TryParse(Port, out var port) ? port : -1
        };
    }

    private bool Validate()
    {
        if (!string.IsNullOrEmpty(IpAddress) && !IPAddress.TryParse(IpAddress, out _)) return false;
        if(!string.IsNullOrEmpty(Port) && (!int.TryParse(Port, out var port) || port is < 0 or > 65535)) return false;
        return true;
    }

    private async Task OnDeleted()
    {
        if (Deleted is not null)
            await Deleted.Invoke(this, EventArgs.Empty);
    }
}
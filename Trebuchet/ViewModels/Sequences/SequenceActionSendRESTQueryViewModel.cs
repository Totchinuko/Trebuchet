using System;
using ReactiveUI;
using TrebuchetLib.Sequences;

namespace Trebuchet.ViewModels.Sequences;

[SequenceAction(typeof(SequenceActionSendRESTQuery))]
public class SequenceActionSendRESTQueryViewModel(SequenceActionSendRESTQuery action) : 
    SequenceActionViewModel<SequenceActionSendRESTQueryViewModel, SequenceActionSendRESTQuery>(action)
{
    private bool _cancelOnFailure = action.CancelOnFailure;
    private string _url = action.Url;
    private int _httpMethod = action.HttpMethod;
    private string _body = action.Body;
    private string _headers = action.Headers;
    private TimeSpan _timeOut = action.TimeOut;

    public bool CancelOnFailure
    {
        get => _cancelOnFailure;
        set => this.RaiseAndSetIfChanged(ref _cancelOnFailure, value);
    }

    public string Url
    {
        get => _url;
        set => this.RaiseAndSetIfChanged(ref _url, value);
    }

    public int HttpMethod
    {
        get => _httpMethod;
        set => this.RaiseAndSetIfChanged(ref _httpMethod, value);
    }

    public string[] HttpMethodList { get; } =
    [
        @"GET",
        @"POST",
        @"PUT",
        @"PATCH",
        @"TRACE",
        @"DELETE",
    ];

    public string Body
    {
        get => _body;
        set => this.RaiseAndSetIfChanged(ref _body, value);
    }

    public string Headers
    {
        get => _headers;
        set => this.RaiseAndSetIfChanged(ref _headers, value);
    }

    public TimeSpan TimeOut
    {
        get => _timeOut;
        set => this.RaiseAndSetIfChanged(ref _timeOut, value);
    }

    protected override void OnActionChanged()
    {
        Action.CancelOnFailure = CancelOnFailure;
        Action.Url = Url;
        Action.HttpMethod = HttpMethod;
        Action.Body = Body;
        Action.Headers = Headers;
        Action.TimeOut = TimeOut;
        
        base.OnActionChanged();
    }
}
using System.Reactive;
using System.Threading.Tasks;
using Markdig;
using Markdig.Extensions.AutoLinks;
using ReactiveUI;

namespace Trebuchet.ViewModels.InnerContainer;

public class OnBoardingUpdate : TitledDialogue<OnBoardingUpdate>
{
    public OnBoardingUpdate(string title) : base()
    {
        Title = title;
        
        ConfirmCommand = ReactiveCommand.Create(() =>
        {
            Result = true;
            Close();
        });
    }

    public OnBoardingUpdate LoadMarkdownDescription(string markdown)
    {
        var header = TrebuchetUtils.Utils.GetMarkdownHtmlHeader();
        var builder = new MarkdownPipelineBuilder();
        builder.Extensions.Add(new AutoLinkExtension(new AutoLinkOptions()
        {
            OpenInNewWindow = true,
            UseHttpsForWWWLinks = true
        }));
        var result = Markdown.ToHtml(markdown, builder.Build());
        Description = header + result;
        return this;
    }
    
    public bool Result { get; private set; }

    public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }
}
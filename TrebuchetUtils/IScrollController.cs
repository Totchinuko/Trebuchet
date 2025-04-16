using System;

namespace TrebuchetUtils;

public interface IScrollController
{
    event EventHandler ScrollToEnd;
    event EventHandler ScrollToHome;
}
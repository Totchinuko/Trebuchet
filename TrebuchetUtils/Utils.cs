using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace TrebuchetUtils;

public static class Utils
{
    public static bool UseOsChrome()
    {
        return !OperatingSystem.IsWindows();
    }

    /// <summary>
    ///     Subscribe to a message type with the given destination and delivery action.
    ///     All references are held with strong references
    ///     All messages of this type will be delivered.
    /// </summary>
    /// <typeparam name="TMessage">Type of message</typeparam>
    /// <param name="hub"></param>
    /// <param name="recipient">Target object</param>
    /// <returns>TinyMessageSubscription used to unsubscribing</returns>
    public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub,
        ITinyRecipient<TMessage> recipient) where TMessage : class, ITinyMessage
    {
        return hub.Subscribe<TMessage>(recipient.Receive);
    }

    /// <summary>
    ///     Subscribe to a message type with the given destination and delivery action.
    ///     Messages will be delivered via the specified proxy.
    ///     All references (apart from the proxy) are held with strong references
    ///     All messages of this type will be delivered.
    /// </summary>
    /// <typeparam name="TMessage">Type of message</typeparam>
    /// <param name="hub"></param>
    /// <param name="recipient">Target object</param>
    /// <param name="proxy">Proxy to use when delivering the messages</param>
    /// <returns>TinyMessageSubscription used to unsubscribing</returns>
    public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub,
        ITinyRecipient<TMessage> recipient, ITinyMessageProxy proxy) where TMessage : class, ITinyMessage
    {
        return hub.Subscribe<TMessage>(recipient.Receive, proxy);
    }

    /// <summary>
    ///     Subscribe to a message type with the given destination and delivery action.
    ///     All messages of this type will be delivered.
    /// </summary>
    /// <typeparam name="TMessage">Type of message</typeparam>
    /// <param name="hub"></param>
    /// <param name="recipient">Target object</param>
    /// <param name="useStrongReferences">Use strong references to destination and deliveryAction </param>
    /// <returns>TinyMessageSubscription used to unsubscribing</returns>
    public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub,
        ITinyRecipient<TMessage> recipient, bool useStrongReferences) where TMessage : class, ITinyMessage
    {
        return hub.Subscribe<TMessage>(recipient.Receive, useStrongReferences);
    }

    /// <summary>
    ///     Subscribe to a message type with the given destination and delivery action.
    ///     Messages will be delivered via the specified proxy.
    ///     All messages of this type will be delivered.
    /// </summary>
    /// <typeparam name="TMessage">Type of message</typeparam>
    /// <param name="hub"></param>
    /// <param name="recipient">Target object</param>
    /// <param name="useStrongReferences">Use strong references to destination and deliveryAction </param>
    /// <param name="proxy">Proxy to use when delivering the messages</param>
    /// <returns>TinyMessageSubscription used to unsubscribing</returns>
    public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub,
        ITinyRecipient<TMessage> recipient, bool useStrongReferences, ITinyMessageProxy proxy)
        where TMessage : class, ITinyMessage
    {
        return hub.Subscribe<TMessage>(recipient.Receive, useStrongReferences, proxy);
    }

    /// <summary>
    ///     Subscribe to a message type with the given destination and delivery action with the given filter.
    ///     All references are held with WeakReferences
    ///     Only messages that "pass" the filter will be delivered.
    /// </summary>
    /// <typeparam name="TMessage">Type of message</typeparam>
    /// <param name="hub"></param>
    /// <param name="recipient">Target object</param>
    /// <param name="messageFilter"></param>
    /// <returns>TinyMessageSubscription used to unsubscribing</returns>
    public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub,
        ITinyRecipient<TMessage> recipient, Func<TMessage, bool> messageFilter) where TMessage : class, ITinyMessage
    {
        return hub.Subscribe(recipient.Receive, messageFilter);
    }

    /// <summary>
    ///     Subscribe to a message type with the given destination and delivery action with the given filter.
    ///     Messages will be delivered via the specified proxy.
    ///     All references (apart from the proxy) are held with WeakReferences
    ///     Only messages that "pass" the filter will be delivered.
    /// </summary>
    /// <typeparam name="TMessage">Type of message</typeparam>
    /// <param name="hub"></param>
    /// <param name="recipient">Target object</param>
    /// <param name="messageFilter"></param>
    /// <param name="proxy">Proxy to use when delivering the messages</param>
    /// <returns>TinyMessageSubscription used to unsubscribing</returns>
    public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub,
        ITinyRecipient<TMessage> recipient, Func<TMessage, bool> messageFilter, ITinyMessageProxy proxy)
        where TMessage : class, ITinyMessage
    {
        return hub.Subscribe(recipient.Receive, messageFilter, proxy);
    }

    /// <summary>
    ///     Subscribe to a message type with the given destination and delivery action with the given filter.
    ///     All references are held with WeakReferences
    ///     Only messages that "pass" the filter will be delivered.
    /// </summary>
    /// <typeparam name="TMessage">Type of message</typeparam>
    /// <param name="messageFilter"></param>
    /// <param name="useStrongReferences">Use strong references to destination and deliveryAction </param>
    /// <param name="hub"></param>
    /// <param name="recipient"></param>
    /// <returns>TinyMessageSubscription used to unsubscribing</returns>
    public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub,
        ITinyRecipient<TMessage> recipient, Func<TMessage, bool> messageFilter, bool useStrongReferences)
        where TMessage : class, ITinyMessage
    {
        return hub.Subscribe(recipient.Receive, messageFilter, useStrongReferences);
    }

    /// <summary>
    ///     Subscribe to a message type with the given destination and delivery action with the given filter.
    ///     Messages will be delivered via the specified proxy.
    ///     All references are held with WeakReferences
    ///     Only messages that "pass" the filter will be delivered.
    /// </summary>
    /// <typeparam name="TMessage">Type of message</typeparam>
    /// <param name="messageFilter"></param>
    /// <param name="useStrongReferences">Use strong references to destination and deliveryAction </param>
    /// <param name="proxy">Proxy to use when delivering the messages</param>
    /// <param name="hub"></param>
    /// <param name="recipient"></param>
    /// <returns>TinyMessageSubscription used to unsubscribing</returns>
    public static TinyMessageSubscriptionToken Subscribe<TMessage>(this ITinyMessengerHub hub,
        ITinyRecipient<TMessage> recipient, Func<TMessage, bool> messageFilter, bool useStrongReferences,
        ITinyMessageProxy proxy) where TMessage : class, ITinyMessage
    {
        return hub.Subscribe(recipient.Receive, messageFilter, useStrongReferences, proxy);
    }
}
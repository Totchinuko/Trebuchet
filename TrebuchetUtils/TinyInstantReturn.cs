using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace TrebuchetUtils
{
    public interface ITinyInstantReturn<T> : ITinyMessage
    {
        T Value { get; }
        void Respond(T value);
    }

    public class TinyInstantReturn<T> : ITinyInstantReturn<T>
    {
        public TinyInstantReturn(object? sender, ITinyMessengerHub messenger)
        {
            Sender = sender;
            Value = messenger.InstantReturn(this);
        }

        public TinyInstantReturn()
        {
            Sender = null;
            Value = TinyMessengerHub.Default.InstantReturn(this);
        }

        public object? Sender { get; }
        public T Value { get; protected set; }

        public static T InstantReturn(ITinyMessengerHub hub)
        {
            return new TinyInstantReturn<T>(null, hub).Value;
        }

        public static T InstantReturn()
        {
            return new TinyInstantReturn<T>().Value;
        }

        public void Respond(T value)
        {
            Value = value;
        }
    }
}
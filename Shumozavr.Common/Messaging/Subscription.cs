﻿using System.Threading.Channels;

namespace Shumozavr.Common.Messaging;

public class Subscription<TMessage>(ChannelReader<TMessage> messagesReader, Action disposeAction) : IDisposable
{
    public ChannelReader<TMessage> MessagesReader { get; } = messagesReader;

    public void Dispose()
    {
        disposeAction();
    }
}
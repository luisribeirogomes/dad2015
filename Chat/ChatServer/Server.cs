﻿using System;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Net.Sockets;
using CommonTypes;

namespace ChatServer {
    public class Server {

        private IDictionary<String, KeyValuePair<int, IClientRemoteObject>> registry;
        private Queue<KeyValuePair<String, String>> messages;

        public Server() {
            EventHandler<PutMessageEventArgs> putMessageHandler = new EventHandler<PutMessageEventArgs>(PutMessage);
            EventHandler<RegisterClientEventArgs> registerClientHandler = new EventHandler<RegisterClientEventArgs>(RegisterClient);
            EventHandler<UnregisterClientEventArgs> unregisterClientHandler = new EventHandler<UnregisterClientEventArgs>(UnregisterClient);
            ServerRemoteObject chat = new ServerRemoteObject(putMessageHandler, registerClientHandler, unregisterClientHandler);
            registry = new Dictionary<String, KeyValuePair<int, IClientRemoteObject>>();
            messages = new Queue<KeyValuePair<String, String>>();

            TcpChannel channel = new TcpChannel(8086);
            ChannelServices.RegisterChannel(channel, true);
            RemotingServices.Marshal(
                chat,
                "Conversation",
                typeof(ServerRemoteObject));

            Console.WriteLine("Started server");
            Console.Read();
        }


        public void PutMessage(Object sender, PutMessageEventArgs eventArgs) {
            messages.Enqueue(new KeyValuePair<String, String>(eventArgs.nickname, eventArgs.message));
            Console.WriteLine("Message: Nickname -> {0} Message -> {1}", eventArgs.nickname, eventArgs.message);

            foreach (KeyValuePair<String, KeyValuePair<int, IClientRemoteObject>> remoteObject in registry) {
                remoteObject.Value.Value.UpdateChat(eventArgs.nickname + ": " + eventArgs.message);
            }
        }

        public void RegisterClient(Object sender, RegisterClientEventArgs eventArgs) {
            KeyValuePair<int, IClientRemoteObject> dummy;
            //throw new RemotingException();
            if (!registry.TryGetValue(eventArgs.nickname, out dummy)) {
                IClientRemoteObject remoteObject = (IClientRemoteObject)Activator.GetObject(
                    typeof(IServerRemoteObject),
                    "tcp://localhost:" + eventArgs.port + "/Conversation");
                registry.Add(eventArgs.nickname, new KeyValuePair<int, IClientRemoteObject>(eventArgs.port, remoteObject));
                Console.WriteLine("Registered: Nickname -> {0} Port -> {1}", eventArgs.nickname, eventArgs.port);
            }
            else {
                throw new RemotingException();
            }
        }

        public void UnregisterClient(Object sender, UnregisterClientEventArgs eventArgs) {
            registry.Remove(eventArgs.nickname);
            Console.WriteLine("Removed: Nickname -> {0}", eventArgs.nickname);
        }
    }
}

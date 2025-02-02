﻿using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace KlimeAndPsycho.PowerCables.Sync
{
    public class Networking
    {
        public readonly ushort PacketId;

        /// <summary>
        /// <paramref name="packetId"/> must be unique from all other mods that also use packets.
        /// </summary>
        public Networking(ushort packetId)
        {
            PacketId = packetId;
        }

        /// <summary>
        /// Register packet monitoring, not necessary if you don't want the local machine to handle incomming packets.
        /// </summary>
        public void Register()
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(PacketId, ReceivedPacket);
        }

        /// <summary>
        /// This must be called on world unload if you called <see cref="Register"/>.
        /// </summary>
        public void Unregister()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(PacketId, ReceivedPacket);
        }

        private void ReceivedPacket(ushort handlerId, byte[] rawData, ulong senderId, bool fromServer) // executed when a packet is received on this machine
        {
            try
            {
                var packet = MyAPIGateway.Utilities.SerializeFromBinary<PacketBase>(rawData);

                bool relay = false;
                packet.Received(ref relay);

                if (relay)
                    RelayToClients(packet, rawData);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        /// <summary>
        /// Send a packet to the server.
        /// Works from clients and server.
        /// </summary>
        /// <param name="packet"></param>
        public void SendToServer(PacketBase packet)
        {
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(packet);

            MyAPIGateway.Multiplayer.SendMessageToServer(PacketId, bytes);
        }

        /// <summary>
        /// Send a packet to a specific player.
        /// Only works server side.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="steamId"></param>
        public void SendToPlayer(PacketBase packet, ulong steamId)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            var bytes = MyAPIGateway.Utilities.SerializeToBinary(packet);

            MyAPIGateway.Multiplayer.SendMessageTo(PacketId, bytes, steamId);
        }

        /// <summary>
        /// Sends packet (or supplied bytes) to all players except server player and supplied packet's sender.
        /// Only works server side.
        /// </summary>
        public void RelayToClients(PacketBase packet, byte[] rawData = null)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            var tempPlayers = new List<IMyPlayer>(MyAPIGateway.Session.SessionSettings.MaxPlayers);

            MyAPIGateway.Players.GetPlayers(tempPlayers);

            foreach (var p in tempPlayers)
            {
                if (p.SteamUserId == MyAPIGateway.Multiplayer.ServerId)
                    continue;

                if (p.SteamUserId == packet.SenderId)
                    continue;

                if (rawData == null)
                    rawData = MyAPIGateway.Utilities.SerializeToBinary(packet);

                MyAPIGateway.Multiplayer.SendMessageTo(PacketId, rawData, p.SteamUserId);
            }

            tempPlayers.Clear();
        }
    }
}

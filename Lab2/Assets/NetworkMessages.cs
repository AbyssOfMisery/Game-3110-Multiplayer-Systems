using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace NetWork
{
    public static class NetworkMessages
    {
        public enum NetCommands
        {
            CONNECTION_REQUEST,
            PLAYER_CONNECTED,
            LIST_OF_PLAYERS,
            SERVER_UPDATE,
            PLAYER_UPDATE_POS,
            PLAYER_DISCONNECTED,
        };

        [Serializable]
        public class Packet
        {
            public int pktID;
            public NetCommands pktType;
        }

        [Serializable]
        public class ConnectionRequest : Packet
        {
            public ConnectionRequest(int id)
            {
                pktID = id;
                pktType = NetCommands.CONNECTION_REQUEST;
            }
        }

        [Serializable]
        public class PlayerUpdate : Packet
        {
            public Vector3 position;
            public Vector3 rotation;

            public PlayerUpdate(int id, Vector3 pos, Vector3 rot)
            {
                pktID = id;
                position = pos;
                rotation = rot;
                pktType = NetCommands.PLAYER_UPDATE_POS;
            }
        }

        [Serializable]
        public class Player : Packet
        {
            public string id;
            public Vector3 position;
        } 

        [Serializable]
        public class PlayerConnected : Packet
        {
            public Player player;
        }

        [Serializable]
        public class PlayerDisconnected : Packet
        {
            public Player player;
        }

        [Serializable]
        public class ListOfPlayers : Packet
        {
            public Player[] players;
        };

        [Serializable]
        public class GameUpdate : Packet
        {
            public Player[] players;
        }
    }

}

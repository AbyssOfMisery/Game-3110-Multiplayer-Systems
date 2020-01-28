using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using Cube;
using Random = UnityEngine.Random;


namespace NetWork
{
    public class NetworkMan : MonoBehaviour
    {
        public UdpClient udp;
        public string HostName;
        public int port;

        private GameObject Cube;

        public GameObject playerGO;
        public int pktID = 0;

        // Start is called before the first frame update
        void Start()
        {
            Cube = Resources.Load("Cube", typeof(GameObject)) as GameObject;
            udp = new UdpClient(HostName, port);
            Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
            udp.Send(sendBytes, sendBytes.Length);
            udp.BeginReceive(new AsyncCallback(OnReceived), udp);
            InvokeRepeating("HeartBeat", 1, 1);

            pktID = 0;
            Invoke("RequestConnection", 1);
        }

        void RequestConnection()
        {
            NetworkMessages.ConnectionRequest req = new NetworkMessages.ConnectionRequest(pktID++);
            Byte[] sendBytes = Encoding.ASCII.GetBytes(JsonUtility.ToJson(req));
            udp.Send(sendBytes, sendBytes.Length);
        }

        public void SendPosition(Vector3 newPos, Vector3 newRot)
        {
            NetworkMessages.PlayerUpdate req = new NetworkMessages.PlayerUpdate(pktID++, newPos, newRot);
            Byte[] sendBytes = Encoding.ASCII.GetBytes(JsonUtility.ToJson(req));
            udp.Send(sendBytes, sendBytes.Length);
        }

        void OnDestroy()
        {
            udp.Dispose();

        }

        // public enum commands
        // {
        //     NEW_CLIENT,
        //     UPDATE,
        //     SPAWN,// must add SPAWN because server.py line 44
        //     DELETE
        // };

        //[Serializable]
        //public class Message
        //{
        //    public commands cmd;
        //    public Player[] players;
        //}



        [Serializable]
        public class receivedColor
        {
            public float RED;
            public float GREEN;
            public float BLUE;
        }


        [Serializable]
        public class Player
        {
            public string id;
            public receivedColor color;

        }

        public Dictionary<string, GameObject> networkedPlayers = new Dictionary<string, GameObject>();

        // [Serializable]
        // public class GameState
        // {
        //     public Player[] players;
        // }

        //public Message latestMessage;
        // public GameState lastestGameState;

        public Queue<NetworkMessages.Packet> spawnMessages = new Queue<NetworkMessages.Packet>();
        public Queue<NetworkMessages.Packet> updateMessages = new Queue<NetworkMessages.Packet>();
        public Queue<NetworkMessages.Packet> deleteMessages = new Queue<NetworkMessages.Packet>();

        public NetworkMessages.Packet latestNetMessage;

        public object NetworkServer { get; private set; }



        void OnReceived(IAsyncResult result)
        {
            // this is what had been passed into BeginReceive as the second parameter:
            UdpClient socket = result.AsyncState as UdpClient;

            // points towards whoever had sent the message:
            IPEndPoint source = new IPEndPoint(0, 0);

            // get the actual message and fill out the source:
            byte[] message = socket.EndReceive(result, ref source);

            // do what you'd like with `message` here:
            string returnData = Encoding.ASCII.GetString(message);
            Debug.Log(returnData);
            // latestMessage = JsonUtility.FromJson<Message>(returnData);
            latestNetMessage = JsonUtility.FromJson<NetworkMessages.Packet>(returnData);
            //try
            //{
            //    switch (latestMessage.cmd)
            //    {
            //        case commands.NEW_CLIENT:
            //            spawnMessages.Enqueue(latestMessage);
            //            Debug.Log("player connected");
            //            break;
            //        case commands.UPDATE:
            //            updateMessages.Enqueue(latestMessage);
            //            Debug.Log("player info update");
            //            break;
            //        case commands.DELETE:
            //            deleteMessages.Enqueue(latestMessage);
            //            Debug.Log("delete old infos");
            //            break;
            //        case commands.SPAWN:
            //            spawnMessages.Enqueue(latestMessage);
            //            Debug.Log("player spawn");
            //            break;
            //        default:
            //            Debug.Log("not one connected");
            //            break;
            //    }
            //}
            //catch (Exception e)
            //{
            //    Debug.Log(e.ToString());
            //}

            Debug.Log(returnData);
            try
            {
                switch (latestNetMessage.pktType)
                {
                    case NetworkMessages.NetCommands.PLAYER_CONNECTED:
                        // NetworkMessages.NewPlayer latestPlayer = JsonUtility.FromJson<NetworkMessages.NewPlayer>(returnData);
                        break;
                    case NetworkMessages.NetCommands.SERVER_UPDATE:
                        // NetworkMessages.GameUpdate lastestGameState = JsonUtility.FromJson<NetworkMessages.GameUpdate>(returnData);
                        break;
                    case NetworkMessages.NetCommands.CONNECTION_REQUEST:
                        break;
                    case NetworkMessages.NetCommands.LIST_OF_PLAYERS:
                        break;
                    case NetworkMessages.NetCommands.PLAYER_UPDATE_POS:
                        break;
                    case NetworkMessages.NetCommands.PLAYER_DISCONNECTED:
                        break;

                    default:
                        Debug.Log("Error");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }

            // schedule the next receive operation once reading is done:
            socket.BeginReceive(new AsyncCallback(OnReceived), socket);
        }

        /// <summary>
        /// sent massege to server while there is at least one player connect to server
        /// </summary>
        void SpawnPlayers()
        {
            while (spawnMessages.Count > 0)
            {
              
                var spawnMessage = spawnMessages.Dequeue();
                for (int i = 0; i < spawnMessage..Length; i++)
                {
                    GameObject newCube = Instantiate(
                        Cube,
                       new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f)),
                        Quaternion.Euler(0, 0, 0)) as GameObject;
                    newCube.GetComponent<NetworkCube>()
                       .ChangeColor(spawnMessage.players[i].color.RED, spawnMessage.players[i].color.GREEN, spawnMessage.players[i].color.BLUE);
                    networkedPlayers.Add(spawnMessage.players[i].id, newCube);

                }
            }
        }

        void UpdatePlayers()
        {
            while (updateMessages.Count > 0)
            {
                var updateMessage = updateMessages.Dequeue();
                for (int i = 0; i < updateMessage.players.Length; i++)
                {
                    var cubeId = updateMessage.players[i].id;
                    if (networkedPlayers.ContainsKey(cubeId))
                    {
                        networkedPlayers[cubeId].GetComponent<NetworkCube>()
                        .ChangeColor(updateMessage.players[i].color.RED, updateMessage.players[i].color.GREEN, updateMessage.players[i].color.BLUE);
                    }

                }
            }
        }

        void DestroyPlayers()
        {
            while (deleteMessages.Count > 0)
            {
                var deleteMessage = deleteMessages.Dequeue();
                for (int i = 0; i < deleteMessage.players.Length; i++)
                {
                    var cubeId = deleteMessage.players[i].id;
                    if (networkedPlayers.ContainsKey(cubeId))
                    {
                        Destroy(networkedPlayers[cubeId]);
                        networkedPlayers.Remove(cubeId);

                    }

                }
            }

        }

        void HeartBeat()
        {
            Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
            udp.Send(sendBytes, sendBytes.Length);
        }
        void Update()
        {
            SpawnPlayers();
            UpdatePlayers();
            DestroyPlayers();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using Cube;

public class NetworkMan : MonoBehaviour
{
    public UdpClient udp;

    private bool newPlayer = true;

    private string mainId = string.Empty;

    private GameObject Cube;

    public string host;
    public int port;
    // Start is called before the first frame update
    void Start()
    {
        Cube = Resources.Load("Cube", typeof(GameObject)) as GameObject;
        udp = new UdpClient();
        udp.Connect(host, port);
        Debug.Log(((IPEndPoint)udp.Client.LocalEndPoint).Port.ToString());
        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
        udp.Send(sendBytes, sendBytes.Length);
        udp.BeginReceive(new AsyncCallback(OnReceived), udp);
        InvokeRepeating("HeartBeat", 1, 1);
        InvokeRepeating("SendPosition", 0.1f, 1);
    }

    void OnDestroy()
    {
        udp.Close();
        udp.Dispose();
    }

    public enum commands
    {
        NEW_CLIENT,
        UPDATE,
        SPAWN,
        DELETE
    };

    [Serializable]
    public class Message
    {
        public commands cmd;
        public Player[] players;
    }

    public Queue<Message> spawnMessages = new Queue<Message>();
    public object lockSpawn = new object();
    public Queue<Message> updateMessages = new Queue<Message>();
    public object lockUpdate = new object();
    public Queue<Message> deleteMessages = new Queue<Message>();
    public object lockDelete = new object();

    [Serializable]
    public class receivedColor
    {
        public float R;
        public float G;
        public float B;
    }

    [Serializable]
    public class receivedPosition
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class Player
    {
        public string id;
        public receivedColor color;
        public receivedPosition position;
    }

    public Dictionary<string, GameObject> networkedPlayers = new Dictionary<string, GameObject>();

    [Serializable]
    public class GameState
    {
        public Player[] players;
    }

    public Message latestMessage;
    public GameState lastestGameState;
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
        latestMessage = JsonUtility.FromJson<Message>(returnData);

        try
        {
            switch (latestMessage.cmd)
            {
                case commands.NEW_CLIENT:
                    lock (lockSpawn)
                    {
                        spawnMessages.Enqueue(latestMessage);
                    }
                    break;
                case commands.UPDATE:
                    lock (lockUpdate)
                    {
                        updateMessages.Enqueue(latestMessage);
                    }
                    break;
                case commands.SPAWN:
                    lock (lockSpawn)
                    {
                        spawnMessages.Enqueue(latestMessage);
                    }
                    break;
                case commands.DELETE:
                    lock (lockDelete)
                    {
                        deleteMessages.Enqueue(latestMessage);
                    }
                    break;
                default:
                    Debug.Log("Error - no suitable message found!!!!!");
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

    void SpawnPlayers()
    {
        lock (lockSpawn)
        {
            while (spawnMessages.Count > 0)
            {
                var spawnMessage = spawnMessages.Dequeue();
                for (int i = 0; i < spawnMessage.players.Length; i++)
                {
                    int playerCount = networkedPlayers.Count + 1;
                    GameObject newCube = Instantiate(
                        Cube,
                        new Vector3(
                            spawnMessage.players[i].position.x,
                            spawnMessage.players[i].position.y,
                            spawnMessage.players[i].position.z
                        ),
                        Quaternion.Euler(0, 0, 0)) as GameObject;
                    NetworkCube new2Cube = newCube.GetComponent<NetworkCube>();
                    new2Cube.id = spawnMessage.players[i].id;
                    if ((i == spawnMessage.players.Length - 1) && newPlayer)
                    {
                        newPlayer = false;
                        new2Cube.mainCube = true;
                        mainId = new2Cube.id;
                    }
                    new2Cube.ChangeColor(spawnMessage.players[i].color.R, spawnMessage.players[i].color.G, spawnMessage.players[i].color.B);
                    networkedPlayers.Add(spawnMessage.players[i].id, newCube);
                }
            }
        }
    }

    void UpdatePlayers()
    {
        lock (lockUpdate)
        {
            while (updateMessages.Count > 0)
            {
                var updateMessage = updateMessages.Dequeue();
                for (int i = 0; i < updateMessage.players.Length; i++)
                {
                    var cubeId = updateMessage.players[i].id;
                    if (networkedPlayers.ContainsKey(cubeId))
                    {
                        var currentCube = networkedPlayers[cubeId];
                        currentCube.GetComponent<NetworkCube>()
                        .ChangeColor(updateMessage.players[i].color.R,
                                     updateMessage.players[i].color.G,
                                     updateMessage.players[i].color.B);
                        if (cubeId != mainId)
                        {
                            currentCube.transform.position = new Vector3(
                                updateMessage.players[i].position.x,
                                updateMessage.players[i].position.y,
                                updateMessage.players[i].position.z
                            );
                        }
                    }
                }
            }
        }
    }

    float PositionX = 0;
    float PositionY = 0;
    float PositionZ = 0;
    void RecordPosition()
    {
        if (!string.IsNullOrWhiteSpace(mainId))
        {
            PositionX = networkedPlayers[mainId].transform.position.x;
            PositionY = networkedPlayers[mainId].transform.position.y;
            PositionZ = networkedPlayers[mainId].transform.position.z;
        }
    }

    void DestroyPlayers()
    {
        lock (lockDelete)
        {
            while (deleteMessages.Count > 0)
            {
                var deleteMessage = deleteMessages.Dequeue();
                for (int updatePlayerCounter = 0; updatePlayerCounter < deleteMessage.players.Length; updatePlayerCounter++)
                {
                    var cubeId = deleteMessage.players[updatePlayerCounter].id;
                    if (networkedPlayers.ContainsKey(cubeId))
                    {
                        Destroy(networkedPlayers[cubeId]);
                        networkedPlayers.Remove(cubeId);
                    }
                }
            }
        }
    }

    void HeartBeat()
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }
    void SendPosition()
    {
        if (!string.IsNullOrWhiteSpace(mainId))
        {
            string positionMessage = "{\"op\":\"cube_position\", \"position\":{\"x\":" + PositionX + ", \"y\":" + PositionY + ",\"z\":" + PositionZ +"}}";
            Debug.Log("Sending Position Message: " + positionMessage);
            Byte[] sendBytes = Encoding.ASCII.GetBytes(positionMessage);
            udp.Send(sendBytes, sendBytes.Length);
        }
    }

    void Update()
    {
        SpawnPlayers();
        RecordPosition();
        UpdatePlayers();
        DestroyPlayers();
    }
}
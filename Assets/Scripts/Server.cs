using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
public class Server : MonoBehaviour
{
    private const int MAX_CONNECTION = 10;
    private int port = 5805;
    private int hostID;
    private int reliableChannel;
    private int unreliableChannel;
    private bool isStarted = false;
    private byte error;

    List<string> _commands = new List<string>()
    {
        "changename",
        "yield"
    };
    List<int> connectionIDs = new List<int>();
    Dictionary<int, string> _nickNames = new Dictionary<int, string>();

    public void StartServer()
    {
        if (isStarted) return;

        NetworkTransport.Init();//инициаализа€
        ConnectionConfig cc = new ConnectionConfig();
      //cc.ConnectTimeout = 500; //
      //Timeout in ms which library will wait before it will send another connection request.
      // cc.MaxConnectionAttempt = 2;
      //Defines the maximum number of times Unity Multiplayer will attempt
      //to send a connection request without receiving a response before
      //it reports that it cannot establish a connection. Default value = 10.
        reliableChannel = cc.AddChannel(QosType.Reliable);//гарантироованнна€ доставка 
        HostTopology topology = new HostTopology(cc, MAX_CONNECTION);
        hostID = NetworkTransport.AddHost(topology, port);
        isStarted = true;
    }
    public void ShutDownServer()
    {
        if (!isStarted) return;
        NetworkTransport.RemoveHost(hostID);
        NetworkTransport.Shutdown();
        isStarted = false;
    }
    void Update()
    {
        if (!isStarted) return;
        int recHostId;
        int connectionId;
        int channelId;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out
        channelId, recBuffer, bufferSize, out dataSize, out error);
        while (recData != NetworkEventType.Nothing)
        {
            switch (recData)
            {
                case NetworkEventType.Nothing:
                    break;
                case NetworkEventType.ConnectEvent:
                    connectionIDs.Add(connectionId);
                    //SendMessageToAll($"Player {connectionId} has connected.");
                    //Debug.Log($"Player {connectionId} has connected.");
                    break;
                case NetworkEventType.DataEvent:
                    string message = Encoding.Unicode.GetString(recBuffer, 0, dataSize);

                    if (CommandParser(message, connectionId))
                    {
                        return;
                    }

                    if (!_nickNames.ContainsKey(connectionId))
                    {
                        _nickNames.Add(connectionId, message);
                        SendMessageToAll($"Player {message} has connected.");
                    }
                    else
                    {
                        SendMessageToAll($"Player {_nickNames[connectionId]}: {message}"); 
                    }
                    //Debug.Log($"Player {connectionId}: {message}");
                    break;
                case NetworkEventType.DisconnectEvent:
                    connectionIDs.Remove(connectionId);
                    SendMessageToAll($"Player {_nickNames[connectionId]} has disconnected.");
                    _nickNames.Remove(connectionId);
                    //Debug.Log($"Player {connectionId} has disconnected.");
                    break;
                case NetworkEventType.BroadcastEvent:
                    break;
            }
            recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer,
            bufferSize, out dataSize, out error);
        }
    }

    private bool CommandParser(string stringLine, int connectionId)
    {
        if (stringLine[0].ToString() == "/")
        {
            int spaceCounter = 0;
            var commandListChars = new List<char>();
            var firstMessageListChars = new List<char>();
            var secondMessageListChars = new List<char>();
            var otherStringLineChars = new List<char>();

            var activeList = commandListChars;

            for (int i = 1; i < stringLine.Length; i++)
            {
                if (stringLine[i].ToString() == " " && spaceCounter < 3)
                {
                    spaceCounter++;

                    if (spaceCounter == 1)
                    {
                        activeList = firstMessageListChars;
                    }
                    if (spaceCounter == 2)
                    {
                        activeList = secondMessageListChars;
                    }
                    if (spaceCounter == 3)
                    {
                        activeList = otherStringLineChars;
                    }
                }
                else
                {
                    activeList.Add(stringLine[i]);
                }
            }

            var commandArrayChars = commandListChars.ToArray();
            var firstMessageArrayChars = firstMessageListChars.ToArray();
            var secondMessageArrayChars = secondMessageListChars.ToArray();
            var otherStringArrayChars = otherStringLineChars.ToArray();

            var command = new string(commandArrayChars);
            var firstMessage = new string(firstMessageArrayChars);
            var secondMessage = new string(secondMessageArrayChars);
            var otherMessageLine = new string(otherStringArrayChars);

            if (_commands.Contains(command))
            {
                if (command == _commands[0])
                {
                    SendMessageToAll($"Player {_nickNames[connectionId]} has changed name to {firstMessage}.");
                    var oldName = _nickNames[connectionId];
                    _nickNames.Remove(connectionId);
                    _nickNames.Add(connectionId, firstMessage);
                    SendMessageToAll($"/changename {oldName} {firstMessage}");
                }

                if (command == _commands[1])
                {
                    var messageLine = $"{firstMessage} {secondMessage} {otherMessageLine}";
                    SendMessageToAll($"Player {_nickNames[connectionId]}: <color=red>{messageLine}</color>");
                }
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    public void SendMessageToAll(string message)
    {
        for (int i = 0; i < connectionIDs.Count; i++)
        {
            SendMessage(message, connectionIDs[i]);
        }
    }

    public void SendMessage(string message, int connectionID)
    {
        byte[] buffer = Encoding.Unicode.GetBytes(message);
        NetworkTransport.Send(hostID, connectionID, reliableChannel, buffer, message.Length *
        sizeof(char), out error);
        if ((NetworkError)error != NetworkError.Ok) Debug.Log((NetworkError)error);
    }
}

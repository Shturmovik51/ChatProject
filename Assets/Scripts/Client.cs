using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System;

public class Client : MonoBehaviour
{
    public delegate void OnMessageReceive(object message);
    public event OnMessageReceive onMessageReceive;
    public event Action<string> OnChangeNickName;
    private const int MAX_CONNECTION = 10;
    private int port = 0;
    private int serverPort = 5805;
    private int hostID;
    private int reliableChannel;
    private string _userNickName;
    private int _connectionID;
    private bool isConnected = false;
    private byte error;
    List<string> _commands = new List<string>()
    {
        "changename",
        "yield"
    };

    public string UserNickName => _userNickName;

    public void Connect()
    {
        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();
        reliableChannel = cc.AddChannel(QosType.Reliable);
        HostTopology topology = new HostTopology(cc, MAX_CONNECTION);
        hostID = NetworkTransport.AddHost(topology, port);
        _connectionID = NetworkTransport.Connect(hostID, "127.0.0.1", serverPort, 0, out error);
        if ((NetworkError)error == NetworkError.Ok)
            isConnected = true;
        else
            Debug.Log((NetworkError)error);
    }
    public void Disconnect()
    {
        if (!isConnected) return;
        NetworkTransport.Disconnect(hostID, _connectionID, out error);
        isConnected = false;
    }
    void Update()
    {
        if (!isConnected) return;
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
                    onMessageReceive?.Invoke($"You have been connected to server.");
                    SendMessage(_userNickName);
                    _connectionID = connectionId;
                    Debug.Log($"You have been connected to server.");
                    break;
                case NetworkEventType.DataEvent:
                    string message = Encoding.Unicode.GetString(recBuffer, 0, dataSize);

                    if (CommandParser(message))
                    {
                        return;
                    }

                    onMessageReceive?.Invoke(message);
                    Debug.Log(message);
                    break;
                case NetworkEventType.DisconnectEvent:
                    isConnected = false;
                    onMessageReceive?.Invoke($"You have been disconnected from server.");
                    Debug.Log($"You have been disconnected from server.");
                    break;
                case NetworkEventType.BroadcastEvent:
                    break;
            }
            recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer,
            bufferSize, out dataSize, out error);
        }
    }

    private bool CommandParser(string stringLine)
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
                if (stringLine[i].ToString() == " " && spaceCounter < 4)
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
                    if(_userNickName == firstMessage)
                    {
                        SetNickName(secondMessage);
                    }
                }

                if (command == _commands[1])
                {                    
                    onMessageReceive?.Invoke($"{firstMessage} {secondMessage} {otherMessageLine}");                   
                }
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    public void SendMessage(string message)
    {    
        byte[] buffer = Encoding.Unicode.GetBytes(message);
        NetworkTransport.Send(hostID, _connectionID, reliableChannel, buffer, message.Length *
        sizeof(char), out error);
        if ((NetworkError)error != NetworkError.Ok) Debug.Log((NetworkError)error);
    }

    public void SetNickName(string nickName)
    {
        _userNickName = nickName;
        OnChangeNickName?.Invoke(_userNickName);
    }
}
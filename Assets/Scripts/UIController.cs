using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class UIController : MonoBehaviour
{
    [SerializeField] private Button _buttonStartServer;
    [SerializeField] private Button _buttonShutDownServer;
    [SerializeField] private Button _buttonConnectClient;
    [SerializeField] private Button _buttonDisconnectClient;
    [SerializeField] private Button _buttonSendMessage;
    [SerializeField] private Button _serverModeButton;
    [SerializeField] private Button _clientModeButton;
    [SerializeField] private Button _enterNameButton;
    [SerializeField] private TMP_InputField _inputFieldMessage;
    [SerializeField] private TMP_InputField _inputFieldNickName;
    [SerializeField] private TextField _textField;
    [SerializeField] private Server _server;
    [SerializeField] private Client _client;
    [SerializeField] private TextMeshProUGUI _nickNameField;
    private void Start()
    {
        SetFilledClientMode(false);
        SetUnfilledClientMode(false);
        SetServerMode(false);

        _buttonStartServer.onClick.AddListener(() => StartServer());
        _buttonShutDownServer.onClick.AddListener(() => ShutDownServer());
        _buttonConnectClient.onClick.AddListener(() => Connect());
        _buttonDisconnectClient.onClick.AddListener(() => Disconnect());
        _buttonSendMessage.onClick.AddListener(() => SendMessage());
        _client.onMessageReceive += ReceiveMessage;
        _client.OnChangeNickName += (name) => _nickNameField.text = $"User : {name}";
        _clientModeButton.onClick.AddListener(OnClickClientModeButton);
        _serverModeButton.onClick.AddListener(OnClickServerModeButton);
        _enterNameButton.onClick.AddListener(SetNickName);
    }
    private void StartServer()
    {
        _server.StartServer();
    }
    private void ShutDownServer()
    {
        _server.ShutDownServer();
    }
    private void Connect()
    {
        _client.Connect();
    }
    private void Disconnect()
    {
        _client.Disconnect();
    }
    private void SendMessage()
    {
        _client.SendMessage(_inputFieldMessage.text);
        _inputFieldMessage.text = "";
    }
    public void ReceiveMessage(object message)
    {
        _textField.ReceiveMessage(message);
    }

    private void SetNickName()
    {
        SetUnfilledClientMode(false);
        SetFilledClientMode(true);
        _client.SetNickName(_inputFieldNickName.text);
    }

    private void OnClickServerModeButton()
    {
        SetServerMode(true);
        SetFilledClientMode(false);
        SetUnfilledClientMode(false);
    }

    private void SetServerMode(bool status)
    {
        _buttonStartServer.gameObject.SetActive(status);
        _buttonShutDownServer.gameObject.SetActive(status);       
    }

    private void OnClickClientModeButton()
    {
        if(_client.UserNickName != null)
        {
            SetFilledClientMode(true);
        }
        else
        {
            SetUnfilledClientMode(true);
        }

        SetServerMode(false);
    }

    private void SetUnfilledClientMode(bool status)
    {
        _nickNameField.enabled = status;
        _inputFieldNickName.gameObject.SetActive(status);
        _enterNameButton.gameObject.SetActive(status);
    }

    private void SetFilledClientMode(bool status)
    {
        _nickNameField.enabled = status;
        _buttonConnectClient.gameObject.SetActive(status);
        _buttonDisconnectClient.gameObject.SetActive(status);
    }
}
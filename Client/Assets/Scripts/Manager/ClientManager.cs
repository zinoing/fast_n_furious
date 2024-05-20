public class ClientManager : Singleton<ClientManager>
{
    private Client myClient;

    public Client MyClient
    {
        get { return myClient; }
        set { myClient = value; }
    }

    public ClientManager()
    {
        myClient = new Client();
    }
}

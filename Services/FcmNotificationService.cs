using FirebaseAdmin;
using FirebaseAdmin.Messaging;

public class FcmNotificationService
{
    private readonly FirebaseApp _firebaseApp;

    public FcmNotificationService(FirebaseApp firebaseApp)
    {
        _firebaseApp = firebaseApp;
    }

    public async Task<string> SendNotificationAsync(string deviceToken, string title, string body)
    {
        var message = new Message()
        {
            Token = deviceToken,
            Notification = new Notification
            {
                Title = title,
                Body = body,
            }
        };
        string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
        return response; // содержит ID отправленного сообщения
    }
}

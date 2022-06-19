
using System;

[System.Serializable]
public class NotificationData : INotification<DateTime>
{
    public string id;
    public string type;
    public DateTime date;

    public string winStatus;
    public string gameId;
    public string sender;
    public bool isAccepted;

    private INotificationController<DateTime> controller;

    private string key;

    public string Key { get => key; }
    public string Type { get => type; set => type = value; }
    
    public DateTime Date { get => date; set => date = value; }
    public INotificationController<DateTime> Controller { get => controller; set => controller = value; }

    public void OnReacted()
    {
        controller.RemoveNotification(this);
    }

    public void SetController(INotificationController<DateTime> controller)
    {
        this.Controller = controller;
    }

    public void setKey(string key)
    {
        this.key = key;
    }

    public override string ToString()
    {
        return $"Notification : [id={id}, key={key}, type={type}, senderId={sender}, gameStatus={winStatus}]";
    }
}

using System;

namespace Vivu.Application.Interfaces.Notifications
{
    public interface INotificationFactory
    {
        (string Title, string Content) CreateContent(string type);
    }
}

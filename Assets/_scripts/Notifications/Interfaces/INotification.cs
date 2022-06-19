using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface INotification<T> 
{
    string Type { get; set; }
    INotificationController<T> Controller { get; set; }

    void SetController(INotificationController<T> controller);
    void OnReacted(); //refactor. rename
}

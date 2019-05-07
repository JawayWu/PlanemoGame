using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleNotifications : MonoBehaviour
{
    public GameObject notificationsPanel;

    public Sprite icon;
    public Sprite activeIcon;

    private Image image;

    public void Awake() {
        notificationsPanel.SetActive(false);
        image = GetComponent<Image>();
        image.sprite = activeIcon;
    }

    //Turn the notifications panel on and off with clicks
    public void toggleNotifications()
    {
        if (notificationsPanel != null)
        { 
            //Get whether or not the panel is on right now
            bool notificationsPanelActive = notificationsPanel.activeSelf;

            //If on, turn it off. If off, turn it on.
            notificationsPanel.SetActive(!notificationsPanelActive);

            image.sprite = icon;
        }
    }
}

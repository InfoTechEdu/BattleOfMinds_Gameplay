using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UserProfileInfoView : MonoBehaviour
{
    [SerializeField] private Image profilePhoto;
    [SerializeField] private Text nameText;
    [SerializeField] private Text winCountText;

    public void LoadInfo(Sprite profilePhoto, string name, string surname, int winCount)
    {
        this.profilePhoto.sprite = profilePhoto;
        this.nameText.text = name + "\r\n" + surname;

        if (winCountText != null)
            this.winCountText.text = winCount.ToString();
    }


    
}

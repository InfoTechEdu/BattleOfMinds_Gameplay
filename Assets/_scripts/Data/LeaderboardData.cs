using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class LeaderboardData
{
    public UserData[] allUsers;

    public UserData[] AllUsers { get => allUsers; set => allUsers = value; }
    public int Count { get => allUsers.Length; }

    public LeaderboardData(UserData[] allUsers)
    {
        this.allUsers = allUsers;
    }

    public UserData GetUserByName(string name)
    {
        UserData user = null;
        foreach (var u in allUsers)
        {
            if (u.ProgressData.Name == name)
            {
                user = u;
                break;
            }
        }

        return user;
    }

    public void UpdateUserProgress(UserData ud)
    {
        for (int i = 0; i < allUsers.Length; i++)
        {
            if (allUsers[i].ProgressData.Name == ud.ProgressData.Name)
            {
                allUsers[i].ProgressData = ud.ProgressData;
                Debug.Log("Progress of user " + ud.ProgressData.Name + " in leaderboard was successfully updated");
                return;
            }
        }

        Debug.LogWarning("User with name " + ud.ProgressData.Name + " not found!");
    }

    public void UpdateUserStatistics(UserData ud)
    {
        for (int i = 0; i < allUsers.Length; i++)
        {
            if (allUsers[i].Id == ud.Id)
            {
                allUsers[i].Statistics = ud.Statistics;
                Debug.Log("Progress of user " + ud.ProgressData.Name + " in leaderboard was successfully updated");
                return;
            }
        }

        Debug.LogWarning("User with name " + ud.ProgressData.Name + " not found!");
    }

    public void SortDescending()
    {
        List<UserData> usersList = new List<UserData>();
        usersList.AddRange(allUsers);
        List<UserData> sortedUsersList = usersList.OrderByDescending(o => o.ProgressData.Points).ToList();

        allUsers = sortedUsersList.ToArray();
    }
    public override string ToString()
    {
        string result = "LeaderBoard - " + Utils.CollectionUtils.ArrayToString(allUsers);
        return result;
    }
}

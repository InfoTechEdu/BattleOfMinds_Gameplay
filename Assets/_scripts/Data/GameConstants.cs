
using System.Collections.Generic;
using UnityEngine;

public class GameConstants
{
    public class GameStatuses
    {
        public static string LOST = "lost";
        public static string WIN = "win";
        public static string DRAW = "draw";
        public static string RUNNING = "running";
    }

    public static int MAX_SESSION_ROUNDS = 6;
    public static float QUESTION_TIME_LIMIT = 7f;

    public static string[] CATEGORIES = {
        "В мире кино", 
        "Флора и фауна",
        "В здоровом теле",
        "Игры всех сортов",
        "Гранит науки"
    };
}

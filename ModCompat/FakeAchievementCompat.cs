using FakeAchievements;

namespace MetroidvaniaMode.ModCompat;

class FakeAchievementCompat
{
    public static void ShowAchievement(string id) => AchievementsManager.ShowAchievement(id);
}

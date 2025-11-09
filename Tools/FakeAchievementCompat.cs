using FakeAchievements;

namespace MetroidvaniaMode.Tools;

class FakeAchievementCompat
{
    public static void ShowAchievement(string id) => AchievementsManager.ShowAchievement(id);
}

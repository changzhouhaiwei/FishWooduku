using UnityEngine;

namespace GameLogic.Wooduku
{
    /// <summary>
    /// Wooduku 关卡进度。CurrentLevelId 表示当前可挑战的最新关卡。
    /// </summary>
    public static class WoodukuLevelProgress
    {
        public const int FirstLevelId = 1;
        public static int LastLevelId => Mathf.Max(FirstLevelId, WoodukuLevelRepository.TotalLevelCount);

        private const string CurrentLevelKey = "Wooduku_CurrentLevelId";

        public static int CurrentLevelId
        {
            get
            {
                var levelId = PlayerPrefs.GetInt(CurrentLevelKey, FirstLevelId);
                return Mathf.Clamp(levelId, FirstLevelId, LastLevelId);
            }
        }

        public static void AdvanceAfterClear(int clearedLevelId)
        {
            var currentLevelId = CurrentLevelId;
            if (clearedLevelId < currentLevelId)
            {
                return;
            }

            var nextLevelId = Mathf.Clamp(clearedLevelId + 1, FirstLevelId, LastLevelId);
            if (nextLevelId == currentLevelId)
            {
                return;
            }

            PlayerPrefs.SetInt(CurrentLevelKey, nextLevelId);
            PlayerPrefs.Save();
        }
    }
}

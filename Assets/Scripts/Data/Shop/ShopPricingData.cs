using UnityEngine;

namespace Beavermania.Data.Shop
{
    [CreateAssetMenu(fileName = "ShopPricingData", menuName = "Beavermania/Shop/Shop Pricing Data")]
    public class ShopPricingData : ScriptableObject
    {
        public int nutPrice = 3;
        public int applePrice = 5;
        public int accessoryPrice = 60;
        public int goldBrickPrice = 150;
        public int hammerPrice = 40;
        public int bowAndArrowPrice = 120;
        public int arrowBundlePrice = 25;
        public int swordAndShieldPrice = 150;
        public int arrowsPerBundle = 10;
        public int bowStarterArrows = 5;

        void OnValidate()
        {
            if (nutPrice < 0 || applePrice < 0 || accessoryPrice < 0 || goldBrickPrice < 0
                || hammerPrice < 0 || bowAndArrowPrice < 0 || arrowBundlePrice < 0 || swordAndShieldPrice < 0)
                Debug.LogWarning($"{name}: prices must be >= 0.", this);

            if (arrowsPerBundle <= 0 || bowStarterArrows <= 0)
                Debug.LogWarning($"{name}: arrowsPerBundle and bowStarterArrows must be > 0.", this);
        }
    }
}

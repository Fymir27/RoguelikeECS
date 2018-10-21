using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist.Components
{
    class HealthComponent : Component<HealthComponent>
    {
        public float Amount
        {
            get => amount;
            set
            {
                amount = value;
                if (amount > Max && Max > 0 /* for when max isnt't set yet, e.g. initialization*/)
                    amount = Max;
            }
        }
        public float Max { get; set; }
        public float RegenerationAmount { get; set; }
        public float RegenerationProgress {
            get => regenerationProgress;
            set
            {
                regenerationProgress = (float)Math.Round(value, 2);
                if(regenerationProgress >= 1)
                {
                    int regneratedHealth = (int)regenerationProgress;
                    Amount += regneratedHealth;
                    regenerationProgress -= regneratedHealth;
                }
            }
        }

        private float amount;
        private float regenerationProgress;

        public string GetString()
        {
            return "(" + Amount + "|" + Max + ") " + "RegenProgress: " + RegenerationProgress;
        }
    }
}

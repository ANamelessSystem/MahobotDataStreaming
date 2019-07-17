using System;
using System.Collections.Generic;
using System.Text;

namespace Marchen.Model
{
    class ValueLimits
    {
        /// <summary>
        /// 周目上限
        /// </summary>
        public static int RoundLimitMax { get; set; }

        /// <summary>
        /// 伤害上限
        /// </summary>
        public static int DamageLimitMax { get; set; }

        /// <summary>
        /// BOSS编号上限
        /// </summary>
        public static int BossLimitMax { get; set; }
    }

    class CommonVariables
    {
        public static double DouUID { get; set; }
        public static int IntBossCode { get; set; }
        public static int IntRound { get; set; }
        public static int IntEID { get; set; }
        public static int IntDMG { get; set; }
        public static int IntEXT { get; set; }
        //public static int IntSubsType { get; set; }
        public static int IntTimeOutFlag { get; set; }
        public static int IntIsAllFlag { get; set; }
    }
}

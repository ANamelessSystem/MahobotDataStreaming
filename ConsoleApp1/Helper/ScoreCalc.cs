using System;
using System.Collections.Generic;
using System.Text;
using Marchen.DAL;
using System.Data;

namespace Marchen.Helper
{
    class ScoreCalc
    {
        /// <summary>
        /// 根据目前最新的进度算出分数
        /// </summary>
        public static void CalcScoreTotal(int intBC, int intRound,int intHPRemain, out double douScore)
        {
            double douBCNow = double.Parse(intBC.ToString());
            double douRoundNow = double.Parse(intRound.ToString());
            douScore = 0;
            double douHPNow = 0;
            //double douB1NRatio = 1.0;
            if (douRoundNow < 4)
            {
                //血量：     600   ;800   ;1000  ;1200  ;2000
                //难度1分率：B1=1.0;B2=1.0;B3=1.3;B4=1.3;B5=1.5
                douScore = (douRoundNow - 1) * ((6000000 * 1.0) + (8000000 * 1.0) + (10000000 * 1.3) + (12000000 * 1.3) + (20000000 * 1.5));
                if (douBCNow == 1)
                {
                    douScore += (6000000 - douHPNow) * 1;
                }
                else if (douBCNow == 2)
                {
                    douScore += 6000000 * 1;
                    douScore += (8000000 - douHPNow) * 1;
                }
                else if (douBCNow == 3)
                {
                    douScore += 6000000 * 1;
                    douScore += 8000000 * 1;
                    douScore += (10000000 - douHPNow) * 1.3;
                }
                else if (douBCNow == 4)
                {
                    douScore += 6000000 * 1;
                    douScore += 8000000 * 1;
                    douScore += 10000000 * 1.3;
                    douScore += (12000000 - douHPNow) * 1.3;
                }
                else
                {
                    douScore += 6000000 * 1;
                    douScore += 8000000 * 1;
                    douScore += 10000000 * 1.3;
                    douScore += 12000000 * 1.3;
                    douScore += (20000000 - douHPNow) * 1.5;
                }
            }
            else if (douRoundNow < 11)
            {
                //血量：     600   ;800   ;1000  ;1200  ;2000
                //难度2分率：B1=1.4;B2=1.4;B3=1.8;B4=1.8;B5=2.0
                douScore = 3 * ((6000000 * 1.0) + (8000000 * 1.0) + (10000000 * 1.3) + (12000000 * 1.3) + (20000000 * 1.5));
                douScore += (douRoundNow - 4) * ((6000000 * 1.4) + (8000000 * 1.4) + (10000000 * 1.8) + (12000000 * 1.8) + (20000000 * 2.0));
                if (douBCNow == 1)
                {
                    douScore += (6000000 - douHPNow) * 1.4;
                }
                else if (douBCNow == 2)
                {
                    douScore += 6000000 * 1.4;
                    douScore += (8000000 - douHPNow) * 1.4;
                }
                else if (douBCNow == 3)
                {
                    douScore += 6000000 * 1.4;
                    douScore += 8000000 * 1.4;
                    douScore += (10000000 - douHPNow) * 1.8;
                }
                else if (douBCNow == 4)
                {
                    douScore += 6000000 * 1.4;
                    douScore += 8000000 * 1.4;
                    douScore += 10000000 * 1.8;
                    douScore += (12000000 - douHPNow) * 1.8;
                }
                else
                {
                    douScore += 6000000 * 1.4;
                    douScore += 8000000 * 1.4;
                    douScore += 10000000 * 1.8;
                    douScore += 12000000 * 1.8;
                    douScore += (20000000 - douHPNow) * 2.0;
                }
            }
            else
            {
                //血量：     600   ;800   ;1000  ;1200  ;2000
                //难度3分率：B1=2.0;B2=2.0;B3=2.5;B4=2.5;B5=3.0
                douScore = 3 * ((6000000 * 1.0) + (8000000 * 1.0) + (10000000 * 1.3) + (12000000 * 1.3) + (20000000 * 1.5));
                douScore += 7 * ((6000000 * 1.4) + (8000000 * 1.4) + (10000000 * 1.8) + (12000000 * 1.8) + (20000000 * 2.0));
                douScore += (douRoundNow - 11) * ((6000000 * 2.0) + (8000000 * 2.0) + (10000000 * 2.5) + (12000000 * 2.5) + (20000000 * 3.0));
                if (douBCNow == 1)
                {
                    douScore += (6000000 - douHPNow) * 2;
                }
                else if (douBCNow == 2)
                {
                    douScore += 6000000 * 2;
                    douScore += (8000000 - douHPNow) * 2;
                }
                else if (douBCNow == 3)
                {
                    douScore += 6000000 * 2;
                    douScore += 8000000 * 2;
                    douScore += (10000000 - douHPNow) * 2.5;
                }
                else if (douBCNow == 4)
                {
                    douScore += 6000000 * 2;
                    douScore += 8000000 * 2;
                    douScore += 10000000 * 2.5;
                    douScore += (12000000 - douHPNow) * 2.5;
                }
                else
                {
                    douScore += 6000000 * 2;
                    douScore += 8000000 * 2;
                    douScore += 10000000 * 2.5;
                    douScore += 12000000 * 2.5;
                    douScore += (20000000 - douHPNow) * 3;
                }
            }
        }

        public static void GetScoreRatios()
        {

        }
    }
}

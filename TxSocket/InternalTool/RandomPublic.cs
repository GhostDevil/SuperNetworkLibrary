﻿using System;

namespace SuperNetwork.TxSocket.InternalTool
{
    internal class RandomPublic
    {
        private static readonly Random _randomNumber = new Random(500);
        private static readonly Random _randomTime = new Random();
            /// <summary>
            /// 根据指定种子取一个随机数
            /// </summary>
            /// <param name="number">最大值</param>
            /// <returns>随机数</returns>
        internal static int RandomNumber(int number)
        {
            return _randomNumber.Next(number);
        }
        /// <summary>
        /// 根据时间为种子取一个随机数
        /// </summary>
        /// <param name="number">最大值</param>
        /// <returns>随机数</returns>
        internal static int RandomTime(int number)
        {
            return _randomTime.Next(number);
        }
    }
}

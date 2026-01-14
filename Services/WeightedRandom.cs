using System;
using System.Collections.Generic;
using System.Linq;

namespace Services
{
    /// <summary>
    /// 각 항목은 부여된 가중치 만큼의 확률을 가진다.
    /// ex) 입력된 항목이 다음과 같을 때,
    ///     {base: 10, value: A}, {base: 20, value: B}, {base: 30, value: C}, {base: 40, value: D}
    ///     전체 가중치는 100 (10+20+30+40) 이고,
    ///     각 항목의 추출 확률은 아래와 같다.
    ///     A: 10/100, B: 20/100, C: 30/100, D: 40/100
    /// </summary>
    public class WeightedRandom<T>
    {
        private class WeightedItem
        {
            public int Base { get; set; }
            public required T Value { get; set; }
            public int Weight { get; set; }
        }

        private List<WeightedItem> _table;
        private int _weight;
        private Random _random;

        public WeightedRandom()
        {
            _table = new List<WeightedItem>();
            _weight = 0;
            _random = new Random();
        }

        public int Length => _table.Count;

        public void Push(int weight, T value)
        {
            if (weight > 0)
            {
                _table.Add(new WeightedItem
                {
                    Base = _weight,
                    Value = value,
                    Weight = weight
                });
                _weight += weight;
            }
        }

        private int GetRandomIndex()
        {
            var table = _table;
            var weight = (int)Math.Floor(_random.NextDouble() * _weight);
            int low = -1;
            int hi = table.Count;

            while (hi - low > 1)
            {
                int mid = (int)Math.Round((low + hi) / 2.0);
                if (table[mid].Base <= weight)
                {
                    low = mid;
                }
                else
                {
                    hi = mid;
                }
            }

            return low;
        }

        public T Random()
        {
            return _table[GetRandomIndex()].Value;
        }

        public int Weight => _weight;
    }
}

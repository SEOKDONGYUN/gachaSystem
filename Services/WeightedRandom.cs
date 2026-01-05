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
            public T Value { get; set; }
            public int Weight { get; set; }
        }

        private List<WeightedItem> _table;
        private int _weight;
        private int _latestPos;
        private Random _random;

        public WeightedRandom()
        {
            _table = new List<WeightedItem>();
            _weight = 0;
            _latestPos = 0;
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

        public WeightedRandom<T> Slice()
        {
            var obj = new WeightedRandom<T>();
            obj._weight = _weight;
            obj._table = _table.Select(item => new WeightedItem
            {
                Base = item.Base,
                Value = item.Value,
                Weight = item.Weight
            }).ToList();
            return obj;
        }

        private int _Random()
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

            _latestPos = low;
            return low;
        }

        public T Random()
        {
            return _table[_Random()].Value;
        }

        public T Next(bool step = false)
        {
            if (!step)
            {
                _latestPos = (int)Math.Floor(_random.NextDouble() * _table.Count);
            }
            else
            {
                _latestPos++;
            }

            if (_table.Count <= _latestPos)
            {
                _latestPos = 0;
            }

            return _table[_latestPos].Value;
        }

        public int Weight() => _weight;

        public WeightedExtractor<T> Extractor() => new WeightedExtractor<T>(this);

        public class WeightedItemInfo
        {
            public int Base { get; set; }
            public T Value { get; set; }
            public int Weight { get; set; }
        }

        public void ForEach(Action<WeightedItemInfo> callback)
        {
            foreach (var item in _table)
            {
                callback(new WeightedItemInfo
                {
                    Base = item.Base,
                    Value = item.Value,
                    Weight = item.Weight
                });
            }
        }

        public class WeightedExtractor<TValue>
        {
            private WeightedRandom<TValue> _random;

            internal WeightedExtractor(WeightedRandom<TValue> weightedRandom)
            {
                _random = weightedRandom.Slice();
            }

            public int Length => _random._table.Count;

            public void Exclude(TValue value, Func<TValue, TValue, bool> comp = null)
            {
                Exclude(new List<TValue> { value }, comp);
            }

            public void Exclude(List<TValue> values = null, Func<TValue, TValue, bool> comp = null)
            {
                if (values == null)
                {
                    values = new List<TValue>();
                }

                if (comp == null)
                {
                    comp = (a, b) => EqualityComparer<TValue>.Default.Equals(a, b);
                }

                foreach (var value in values)
                {
                    var table = _random._table;
                    int length = table.Count;
                    for (int index = 0; index < length; index++)
                    {
                        if (comp(table[index].Value, value))
                        {
                            _Pop(index);
                            break;
                        }
                    }
                }
            }

            public TValue Random()
            {
                int index = _random._Random();
                var result = _random._table[index];
                _Pop(index);
                return result.Value;
            }

            private void _Pop(int index)
            {
                var table = _random._table;
                int weight = table[index].Base;

                for (; index + 1 < table.Count; index++)
                {
                    var item = table[index] = table[index + 1];
                    item.Base = weight;
                    weight += item.Weight;
                }

                table.RemoveAt(table.Count - 1);
                _random._weight = weight;
            }
        }
    }
}

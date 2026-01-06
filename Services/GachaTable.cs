using GachaSystem.Models;
using System.Text.Json;
using Services;

namespace GachaSystem.Services
{
    /// <summary>
    /// 가챠 테이블 싱글톤 클래스
    /// 서버 시작 시 모든 가챠 데이터를 로드하고 WeightedRandom 객체를 초기화하여 보관
    /// </summary>
    public sealed class GachaTable
    {
        private static readonly Lazy<GachaTable> _instance = new Lazy<GachaTable>(() => new GachaTable());

        private readonly Dictionary<string, List<GachaItem>> _gachaPools;
        private readonly Dictionary<string, WeightedRandom<GachaItem>> _weightedRandomPools;

        public static GachaTable Instance => _instance.Value;

        private GachaTable()
        {
            _gachaPools = InitializeGachaPools();
            _weightedRandomPools = InitializeWeightedRandomPools();
        }

        private Dictionary<string, List<GachaItem>> InitializeGachaPools()
        {
            var pools = new Dictionary<string, List<GachaItem>>();
            var dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

            if (!Directory.Exists(dataDirectory))
            {
                throw new DirectoryNotFoundException($"Data 디렉토리를 찾을 수 없습니다: {dataDirectory}");
            }

            var jsonFiles = Directory.GetFiles(dataDirectory, "gacha-items*.json");

            if (jsonFiles.Length == 0)
            {
                throw new FileNotFoundException($"가챠 아이템 데이터 파일을 찾을 수 없습니다: {dataDirectory}");
            }

            foreach (var jsonFilePath in jsonFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(jsonFilePath);
                var poolName = fileName.Replace("gacha-items-", "");

                var jsonString = File.ReadAllText(jsonFilePath);
                var items = JsonSerializer.Deserialize<List<GachaItem>>(jsonString);

                if (items == null || items.Count == 0)
                {
                    throw new InvalidOperationException($"가챠 아이템 데이터를 로드할 수 없습니다: {jsonFilePath}");
                }

                pools[poolName] = items;
            }

            return pools;
        }

        private Dictionary<string, WeightedRandom<GachaItem>> InitializeWeightedRandomPools()
        {
            var weightedPools = new Dictionary<string, WeightedRandom<GachaItem>>();

            foreach (var poolEntry in _gachaPools)
            {
                var poolName = poolEntry.Key;
                var items = poolEntry.Value;

                var weightedRandom = new WeightedRandom<GachaItem>();
                foreach (var item in items)
                {
                    weightedRandom.Push(item.BaseWeight, item);
                }

                weightedPools[poolName] = weightedRandom;
            }

            return weightedPools;
        }

        public List<GachaItem> GetGachaPool(string poolName = "normal")
        {
            if (!_gachaPools.ContainsKey(poolName))
            {
                throw new ArgumentException($"존재하지 않는 가챠 풀입니다: {poolName}");
            }
            return _gachaPools[poolName];
        }

        public WeightedRandom<GachaItem> GetWeightedRandomPool(string poolName = "normal")
        {
            if (!_weightedRandomPools.ContainsKey(poolName))
            {
                throw new ArgumentException($"존재하지 않는 가챠 풀입니다: {poolName}");
            }
            return _weightedRandomPools[poolName];
        }
    }
}

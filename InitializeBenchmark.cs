using BenchmarkDotNet.Attributes;
namespace BlueNoise
{
    /// <summary>
    /// Benchmark all sampling algorithms
    /// </summary>
    public class InitializeBenchmark {

        /// <summary>
        /// benchmark pach1
        /// </summary>
        [Benchmark]
        public void pach1() => new Pach1(8, 0);

        /// <summary>
        /// benchmark pach2
        /// </summary>
        [Benchmark]
        public void pach2() => new Pach2(8, 0);

        /// <summary>
        /// benchmark pach3
        /// </summary>
        [Benchmark]
        public void pach3() => new Pach3(8, 0);

        /// <summary>
        /// benchmark pach4
        /// </summary>
        [Benchmark]
        public void pach4() => new Pach4(8, 0);
    }
}
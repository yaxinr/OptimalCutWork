using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimalCutWork
{
    public class StaticClass
    {
        static void Main(string[] args)
        {
            ProductBatch[] batches = new ProductBatch[] {
                new ProductBatch("b1",DateTime.Now, 10, 10, 30, new Dictionary<string,int>(){ { "q", 1 } }, 1),
                new ProductBatch("b2",DateTime.Now.AddDays(1), 10, 20, 30, new Dictionary<string,int>(){ { "qa", 1 } }, 1),
                new ProductBatch("b3",DateTime.Now.AddDays(2), 10, 20, 30, new Dictionary<string,int>(){ { "", 1 } }, 1),
            };
            Workcenter[] workcenters = new Workcenter[] {
                new Workcenter("w1"){ maximalDiameter=20, seconds = 20 },
                //new Workcenter("w2"){ maximalDiameter=20, },
            };
            BatchWorkcenter[] batchWorkcenters = GetBatchWorkcenters(batches, workcenters, 8 * 60 * 60);
            foreach (var bw in batchWorkcenters)
            {
                Console.WriteLine("{0}", bw);
            }
            Console.WriteLine("{0}", batchWorkcenters.Length);
            Console.ReadLine();
        }

        public static BatchWorkcenter[] GetBatchWorkcenters(ProductBatch[] batches, Workcenter[] workcenters, int forFirstLevelLimitSeconds)
        {
            List<BatchWorkcenter> batchWorkcenters = new List<BatchWorkcenter>();
            //foreach (var batch in batches.Where(b => b.availabilityLevel == 1).OrderBy(b => b.deadline))
            {
                int sumSeconds = workcenters.Sum(w => w.seconds);
                var availableBatches = batches.Where(b => b.availabilityLevel == 1).ToList();
                ProductBatch batch = null;
                while (true)
                {
                    if (batch == null)
                    {
                        if (sumSeconds > forFirstLevelLimitSeconds) break;
                        batch = availableBatches
                            .OrderBy(b => b.deadline).FirstOrDefault();
                        if (batch == null) break;
                    }
                    ScheduleBatch(workcenters, batchWorkcenters, batch);

                    sumSeconds += batch.seconds;

                    availableBatches.Remove(batch);
                    batch = availableBatches
                        .Where(b => (b.deadline - batch.deadline).TotalDays < 3 && b.materialBatches.Intersect(batch.materialBatches).Any())
                        .OrderBy(b => b.deadline).FirstOrDefault();
                }
            }
            foreach (var batch in batches.Where(b => b.workcenter == null).OrderBy(b => b.deadline))
            {
                ScheduleBatch(workcenters, batchWorkcenters, batch);
            }
            return batchWorkcenters.ToArray();
        }

        private static void ScheduleBatch(Workcenter[] workcenters, List<BatchWorkcenter> batchWorkcenters, ProductBatch batch)
        {
            // check if matBatch on workcenter
            var availableWorkcenters = workcenters
                .Where(w => batch.materialBatches.Keys.Intersect(w.materialBatches).Any());
            if (!availableWorkcenters.Any())
            {
                availableWorkcenters = workcenters
                    .Where(w => w.minimalDiameter <= batch.diameter && w.maximalDiameter >= batch.diameter && batch.billetLength <= w.maximalLenght);
            }
            foreach (Workcenter workcenter in availableWorkcenters.OrderBy(w => w.seconds))
            {
                int startSecond = workcenter.seconds;
                workcenter.seconds += batch.seconds;
                batchWorkcenters.Add(new BatchWorkcenter { batch = batch, workcenter = workcenter, startSecond = startSecond });
                batch.workcenter = workcenter;
                workcenter.materialBatches.AddRange(batch.materialBatches.Keys);
                break;
            }
        }

        public struct BatchWorkcenter
        {
            public ProductBatch batch;
            public Workcenter workcenter;
            public int startSecond;

            public override string ToString()
            {
                return String.Format("{0} {1} {2}", workcenter.id, batch.id, startSecond);
            }
        }

        public class ProductBatch
        {
            public string id;
            public DateTime deadline;
            public int seconds;
            public int diameter;
            public int billetLength;
            public int availabilityLevel;
            public Dictionary<string, int> materialBatches;
            public Workcenter workcenter;

            public ProductBatch(string id, DateTime deadline, int seconds, int diameter, int billetLength, Dictionary<string, int> materialBatches, int availabilityLevel = 0)
            {
                this.deadline = deadline;
                this.seconds = seconds;
                this.diameter = diameter;
                this.billetLength = billetLength;
                this.availabilityLevel = availabilityLevel;
                this.materialBatches = materialBatches;
                this.id = id;
            }
        }

        public struct Task
        {
            public string materialBatchId;
            public ProductBatch productBatch;
            public int quantity;
        }

        public class Workcenter
        {
            public string id;
            public int maximalDiameter = int.MaxValue;
            public int minimalDiameter = int.MinValue;
            public int maximalLenght = int.MaxValue;
            public int seconds = 0;
            public List<string> materialBatches = new List<string>();

            public Workcenter(string id)
            {
                this.id = id;
            }
        }
    }
}

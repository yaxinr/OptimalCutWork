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
                new ProductBatch("b1",DateTime.Now, 10, 10, 30),
                new ProductBatch("b2",DateTime.Now, 10, 20, 30),
            };
            Workcenter[] workcenters = new Workcenter[] {
                new Workcenter("w1"){ maximalDiameter=20, },
                new Workcenter("w2"){ maximalDiameter=20, },
            };
            BatchWorkcenter[] batchWorkcenters = GetBatchWorkcenters(batches, workcenters);
            foreach (var bw in batchWorkcenters)
            {
                Console.WriteLine("{0}", bw);
            }
            Console.WriteLine("{0}", batchWorkcenters.Length);
            Console.ReadLine();
        }

        public static BatchWorkcenter[] GetBatchWorkcenters(ProductBatch[] batches, Workcenter[] workcenters)
        {
            List<BatchWorkcenter> batchWorkcenters = new List<BatchWorkcenter>();
            foreach (var batch in batches.OrderBy(b => b.availabilityLevel).ThenBy(b => b.deadline))
            {
                var availableWorkcenters = workcenters
                    .Where(w => w.materialBatches != null && batch.materialBatches != null && batch.materialBatches.Intersect(w.materialBatches).Any());
                //{
                //    workcenter.seconds += batch.seconds;
                //    batchWorkcenters.Add(new BatchWorkcenter { batch = batch, workcenter = workcenter, startSecond = workcenter.seconds });
                //    batch.workcenter = workcenter;
                //    break;
                //}
                //if (batch.workcenter != null) continue;
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
                    break;
                }
            }
            return batchWorkcenters.ToArray();
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
            public string[] materialBatches;
            public Workcenter workcenter;

            public ProductBatch(string id, DateTime deadline, int seconds, int diameter, int billetLength, int availabilityLevel = 0, string[] materialBatches = null)
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

        public class Workcenter
        {
            public string id;
            public int maximalDiameter = int.MaxValue;
            public int minimalDiameter = int.MinValue;
            public int maximalLenght = int.MaxValue;
            public int seconds = 0;
            public string[] materialBatches;

            public Workcenter(string id)
            {
                this.id = id;
            }
        }
    }
}

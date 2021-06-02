using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OptimalCutWork
{
    public class StaticClass
    {
        static void Main(string[] args)
        {
            ProductBatch[] batches = new ProductBatch[] {
                new ProductBatch("b1", 10,  DateTime.Now, 10, 30, 1),
                new ProductBatch("b2", 10, DateTime.Now.AddDays(1), 20, 30, 1),
                new ProductBatch("b3", 10, DateTime.Now.AddDays(2), 20, 30, 1),
            };
            Workcenter[] workcenters = new Workcenter[] {
                //new Workcenter("w1"){ maximalDiameter=20, seconds = 20 },
                new Workcenter("w2"){ maximalDiameter=20, },
            };
            BatchLink[] batchLinks = new BatchLink[] {
                new BatchLink() { materialBatchId = "q", placeId = 99, productBatch = batches[1], quantity = 1, materialBatchIncomeAt = DateTime.Now },
                new BatchLink() { materialBatchId = "w", placeId = 99, productBatch = batches[0], quantity = 1, materialBatchIncomeAt = DateTime.Now.AddDays(1) },
            };
            Task[] batchWorkcenters = ScheduleTask(batchLinks, workcenters, 8 * 60 * 60, DateTime.Today.AddDays(14));
            foreach (var bw in batchWorkcenters)
            {
                Console.WriteLine("{0}", bw);
            }
            Console.WriteLine("{0}", batchWorkcenters.Length);
            Console.ReadLine();
        }

        public static Task[] ScheduleTask(BatchLink[] batchLinks, Workcenter[] workcenters, int forFirstLevelLimitSeconds, DateTime deadlineLimit, List<Task> tasks = null)
        {
            if (tasks == null) tasks = new List<Task>();
            int sumSeconds = workcenters.Sum(w => w.seconds);
            var prodBatches = batchLinks.Where(bl => bl.productBatch.deadline < deadlineLimit).GroupBy(x => x.productBatch);
            Parallel.ForEach(prodBatches, b =>
            {
                b.Key.materialBatchIncomeAt = b.Min(bl => bl.materialBatchIncomeAt);
            });
            foreach (var pb in prodBatches.Where(x => x.Key.availabilityLevel == 0)
                .OrderBy(x => x.Key.deadline).ToArray())
                foreach (var bl in pb)
                    sumSeconds += ScheduleBatchLink(workcenters, tasks, bl);
            foreach (var pb in prodBatches.Where(x => x.Key.availabilityLevel == 1)
                .OrderBy(x => x.Key.materialBatchIncomeAt)
                .ThenBy(x => x.Key.deadline).ToArray())
                foreach (var bl in pb.OrderBy(bl => bl.materialBatchIncomeAt))
                    sumSeconds += ScheduleBatchLink(workcenters, tasks, bl);
            foreach (var pb in prodBatches.OrderBy(x => x.Key.deadline).ToArray())
                foreach (var bl in pb)
                    if (bl.workcenter == null)
                        sumSeconds += ScheduleBatchLink(workcenters, tasks, bl);
            return tasks.ToArray();
        }
        public static Task[] ScheduleTaskMinimizeMatTrans(BatchLink[] batchLinks, Workcenter[] workcenters, int forFirstLevelLimitSeconds, DateTime deadlineLimit, List<Task> tasks = null)
        {
            var matBatches = batchLinks.GroupBy(x => x.materialBatchId).Select(g => new MaterialBatch()
            {
                id = g.Key,
                //deadline = new DateTime(Math.Max(g.Min(x => x.productBatch.deadline).Ticks, DateTime.Today.Ticks)),
                deadline = g.Min(x => x.productBatch.deadline),
                batchLinks = g.OrderBy(bl => bl.productBatch.deadline).ToArray()
            });

            if (tasks == null) tasks = new List<Task>();
            int sumSeconds = workcenters.Sum(w => w.seconds);
            var orderedMatBatches = matBatches.OrderBy(mb => mb.deadline).ToArray();
            foreach (var mb in orderedMatBatches)
            {
                foreach (var bl in mb.batchLinks)
                {
                    if (bl.productBatch.deadline < deadlineLimit && bl.productBatch.availabilityLevel == 1)
                    {
                        sumSeconds += ScheduleBatchLink(workcenters, tasks, bl);
                    }
                }
                if (sumSeconds > forFirstLevelLimitSeconds) break;
            }
            foreach (var mb in orderedMatBatches)
                foreach (var bl in mb.batchLinks)
                {
                    if (bl.workcenter == null && bl.productBatch.deadline < deadlineLimit)
                    {
                        sumSeconds += ScheduleBatchLink(workcenters, tasks, bl);
                    }
                }
            return tasks.ToArray();
        }

        private static int ScheduleBatchLink(Workcenter[] workcenters, List<Task> tasks, BatchLink batchLink)
        {
            // check if matBatch on workcenter
            var productBatch = batchLink.productBatch;
            var availableWorkcenters = workcenters
                .Where(w => w.materialBatches.Contains(batchLink.materialBatchId) || w.productBatches.Contains(batchLink.productBatch.id));
            if (!availableWorkcenters.Any())
            {
                availableWorkcenters = workcenters
                    .Where(w => w.minimalDiameter <= productBatch.diameter && w.maximalDiameter >= productBatch.diameter && productBatch.billetLength <= w.maximalLenght);
                if (!availableWorkcenters.Any())
                {
                    availableWorkcenters = workcenters;
                }
            }
            foreach (Workcenter workcenter in availableWorkcenters.OrderBy(w => w.seconds))
            {
                int startSecond = workcenter.seconds;
                int batchLinkSeconds = batchLink.quantity * batchLink.productBatch.pieceSeconds;
                workcenter.seconds += batchLinkSeconds;
                tasks.Add(new Task { batchLink = batchLink, workcenter = workcenter, startSecond = startSecond });
                workcenter.materialBatches.Add(batchLink.materialBatchId);
                workcenter.productBatches.Add(batchLink.productBatch.id);
                batchLink.workcenter = workcenter;
                return batchLinkSeconds;
            }
            return -1;
        }

        public struct Task
        {
            public Workcenter workcenter;
            public int startSecond;
            public BatchLink batchLink;

            public override string ToString()
            {
                return String.Format("{0} sec={1} {2}", workcenter.id, startSecond, batchLink.ToString());
            }
        }

        public class ProductBatch
        {
            public string id;
            public DateTime deadline;
            public DateTime materialBatchIncomeAt;
            public int diameter;
            public int billetLength;
            public int availabilityLevel;
            public int pieceSeconds;

            public ProductBatch(string id, int pieceSeconds, DateTime deadline, int diameter, int billetLength, int availabilityLevel = 0)
            {
                this.deadline = deadline;
                this.pieceSeconds = pieceSeconds;
                this.diameter = diameter;
                this.billetLength = billetLength;
                this.availabilityLevel = availabilityLevel;
                this.id = id;
            }
        }

        public struct MaterialBatch
        {
            public string id;
            public DateTime deadline;
            public BatchLink[] batchLinks;
            //public int availabilityLevel;
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class Workcenter
        {
            [JsonProperty]
            public string id;
            public int maximalDiameter = int.MaxValue;
            public int minimalDiameter = int.MinValue;
            public int maximalLenght = int.MaxValue;
            public int seconds = 0;
            public HashSet<string> productBatches = new HashSet<string>();
            public HashSet<string> materialBatches = new HashSet<string>();

            public Workcenter(string id)
            {
                this.id = id;
            }
        }

        public class BatchLink
        {
            public ProductBatch productBatch;
            public string materialBatchId;
            public DateTime materialBatchIncomeAt;
            public int placeId;
            public int quantity;
            public Workcenter workcenter;
            //public int materialBatchLenmm;

            public override string ToString()
            {
                return String.Format("{0}={1} {2}", productBatch.id, quantity, materialBatchId);
            }
        }
    }
}
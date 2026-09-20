using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NUnit.Framework;
using UnityEngine;

namespace WarmBread.Tests
{
    public sealed class EconomyTests
    {
        private ProductData[] catalog;

        [SetUp]
        public void SetUp()
        {
            catalog = ProductCatalog.CreateDefaults();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var product in catalog)
            {
                if (product != null) UnityEngine.Object.DestroyImmediate(product);
            }
        }

        [TestCase(1500, 5000, 3500)]
        [TestCase(300, 1000, 700)]
        [TestCase(70, 100, 30)]
        [TestCase(1200, 1200, 0)]
        public void ExactChange(int total, int paid, int expected)
        {
            Assert.AreEqual(expected, Money.Change(total, paid));
        }

        [Test]
        public void UnderpaymentIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Money.Change(1500, 1000));
        }

        [Test]
        public void NegativeTotalIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Money.Change(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Money.Tender(-1));
        }

        [Test]
        public void StockNeverGoesNegative()
        {
            var stock = new Inventory();
            stock.Add("bread_white", 1, 6f);
            Assert.NotNull(stock.Take("bread_white"));
            Assert.IsNull(stock.Take("bread_white"));
            Assert.AreEqual(0, stock.Count("bread_white"));
        }

        [Test]
        public void InvalidStockIsIgnored()
        {
            var stock = new Inventory();
            stock.Add(null, 10, 6f);
            stock.Add("bread_white", -1, 6f);
            stock.Add("bread_white", 1, float.NaN);
            Assert.AreEqual(0, stock.Batches.Count);
        }

        [Test]
        public void ReturnDoesNotRefreshShelfLife()
        {
            var stock = new Inventory();
            stock.Add("pirozhok_meat", 1, 6f);
            var unit = stock.Take("pirozhok_meat");
            stock.Return(new[] { unit });
            Assert.AreEqual(1, stock.Expire(catalog, 12f));
        }

        [Test]
        public void OldestStockLeavesFirst()
        {
            var stock = new Inventory();
            stock.Add("bread_white", 1, 10f);
            stock.Add("bread_white", 1, 6f);
            Assert.AreEqual(6f, stock.Take("bread_white").bakedAt);
        }

        [Test]
        public void NonPerishableSurvives()
        {
            var stock = new Inventory();
            stock.Add("water", 3, 6f);
            Assert.AreEqual(0, stock.Expire(catalog, 10000f));
            Assert.AreEqual(3, stock.Count("water"));
        }

        [Test]
        public void ExactExpiryBoundary()
        {
            var stock = new Inventory();
            stock.Add("pirozhok_meat", 1, 6f);
            Assert.AreEqual(0, stock.Expire(catalog, 11.999f));
            Assert.AreEqual(1, stock.Expire(catalog, 12f));
        }

        [Test]
        public void OrderRejectsExtraProducts()
        {
            var order = new Order();
            order.Add(catalog[0]);
            Assert.IsFalse(order.Matches(new[]
            {
                new StockBatch("bread_white", 1, 6f),
                new StockBatch("gum", 1, 6f)
            }));
        }

        [Test]
        public void OrderRejectsMissingProducts()
        {
            var order = new Order();
            order.Add(catalog[0]);
            Assert.IsFalse(order.Matches(new StockBatch[0]));
        }

        [Test]
        public void OrderCombinesQuantities()
        {
            var order = new Order();
            order.Add(catalog[0]);
            order.Add(catalog[0]);
            Assert.AreEqual(3000, order.Total);
            Assert.IsTrue(order.Matches(new[] { new StockBatch("bread_white", 2, 6f) }));
        }

        [Test]
        public void OrderItemsAreReadOnly()
        {
            var order = new Order();
            order.Add(catalog[0]);
            Assert.IsInstanceOf<ReadOnlyDictionary<string, int>>(order.Items);
        }

        [Test]
        public void SaveRoundTripKeepsCampaignProgressAndCyrillic()
        {
            var data = new SaveData
            {
                cash = 50123,
                day = 6,
                totalSales = 42,
                totalRevenue = 83200,
                goalsCompleted = 5,
                campaignCompleted = true,
                endingId = "warm-light"
            };
            data.journal.Add("Хлеб любит тишину.");
            data.journalIds.Add("nina");
            data.stock.Add(new StockBatch("bread_white", 4, 6f));

            var result = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
            Assert.AreEqual(SaveData.CurrentVersion, result.version);
            Assert.AreEqual(50123, result.cash);
            Assert.AreEqual(42, result.totalSales);
            Assert.AreEqual(83200, result.totalRevenue);
            Assert.AreEqual(5, result.goalsCompleted);
            Assert.IsTrue(result.campaignCompleted);
            Assert.AreEqual("warm-light", result.endingId);
            Assert.AreEqual("Хлеб любит тишину.", result.journal[0]);
            Assert.AreEqual("nina", result.journalIds[0]);
            Assert.AreEqual(4, result.stock[0].quantity);
        }

        [Test]
        public void ProductIdsAreUnique()
        {
            var ids = new HashSet<string>();
            foreach (var product in catalog)
            {
                Assert.IsTrue(ids.Add(product.Id));
                Assert.Greater(product.Price, 0);
                Assert.GreaterOrEqual(product.Cost, 0);
            }
        }

        [Test]
        public void CustomerIdsAreUniqueAndLateVisitorIsExplicit()
        {
            var people = CustomerData.Defaults();
            try
            {
                var ids = new HashSet<string>();
                var lateVisitors = 0;

                foreach (var person in people)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(person.Id));
                    Assert.IsTrue(ids.Add(person.Id));
                    if (person.LateVisitor) lateVisitors++;
                }

                Assert.AreEqual(1, lateVisitors);
            }
            finally
            {
                foreach (var person in people)
                {
                    if (person != null) UnityEngine.Object.DestroyImmediate(person);
                }
            }
        }

        [Test]
        public void CampaignContainsSevenProgressiveDays()
        {
            Assert.AreEqual(7, CampaignRules.CampaignDays);
            var titles = new HashSet<string>();
            var previousSalesGoal = 0;

            for (var day = 1; day <= CampaignRules.CampaignDays; day++)
            {
                var plan = CampaignRules.Get(day);
                Assert.AreEqual(day, plan.Day);
                Assert.IsTrue(titles.Add(plan.Title));
                Assert.GreaterOrEqual(plan.SalesGoal, previousSalesGoal);
                Assert.Greater(plan.RevenueGoal, 0);
                Assert.Greater(plan.Rent, 0);
                Assert.Greater(plan.GoalBonus, 0);
                Assert.Greater(plan.DeliverySeconds, 0f);
                previousSalesGoal = plan.SalesGoal;
            }
        }

        [Test]
        public void CampaignDemandWeightsFocusProducts()
        {
            var plan = CampaignRules.Get(1);
            var whiteBread = Array.Find(catalog, product => product.Id == "bread_white");
            var gum = Array.Find(catalog, product => product.Id == "gum");
            Assert.Greater(plan.DemandWeight(whiteBread), plan.DemandWeight(gum));
        }

        [TestCase(90, 90000, 60, "home")]
        [TestCase(70, 40000, 40, "warm-light")]
        [TestCase(35, 12000, 15, "hard-autumn")]
        public void CampaignEndingsAreDeterministic(int reputation, int cash, int sales, string expected)
        {
            var ending = CampaignRules.EndingId(reputation, cash, sales);
            Assert.AreEqual(expected, ending);
            Assert.IsFalse(string.IsNullOrWhiteSpace(CampaignRules.EndingTitle(ending)));
            Assert.IsFalse(string.IsNullOrWhiteSpace(CampaignRules.EndingText(ending)));
        }

        [Test]
        public void ProceduralLoafMeshHasValidGeometry()
        {
            var mesh = ProceduralMeshFactory.CreateLoaf("Test loaf", .5f, .25f, .2f);
            try
            {
                Assert.Greater(mesh.vertexCount, 100);
                Assert.Greater(mesh.triangles.Length, 300);
                Assert.Greater(mesh.bounds.size.x, .45f);
                Assert.Greater(mesh.bounds.size.y, .15f);
                Assert.Greater(mesh.bounds.size.z, .2f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void ProceduralLatheAndTorusHaveNormalsAndBounds()
        {
            var bottle = ProceduralMeshFactory.CreateLathe(
                "Bottle",
                new[]
                {
                    new Vector2(.08f, 0f),
                    new Vector2(.08f, .3f),
                    new Vector2(.03f, .4f)
                },
                16);
            var torus = ProceduralMeshFactory.CreateTorus("Ring", .1f, .03f, 16, 8);

            try
            {
                Assert.AreEqual(bottle.vertexCount, bottle.normals.Length);
                Assert.AreEqual(torus.vertexCount, torus.normals.Length);
                Assert.Greater(bottle.bounds.size.y, .35f);
                Assert.Greater(torus.bounds.size.x, .2f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bottle);
                UnityEngine.Object.DestroyImmediate(torus);
            }
        }
    }
}

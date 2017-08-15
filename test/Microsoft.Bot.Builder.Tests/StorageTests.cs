﻿using System;
using System.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Storage;
using System.IO;

namespace Microsoft.Bot.Builder.Tests
{
    [TestClass]
    public class RamStorageTests : StorageTests
    {
        private IStorage storage;

        public RamStorageTests() { }

        [TestInitialize]
        public void initialize()
        {
            storage = new RamStorage();
        }

        [TestMethod]
        public async Task Ram_CreateObjectTest()
        {
            await base.CreateObjectTest(storage);
        }

        [TestMethod]
        public async Task Ram_ReadUnknownTest()
        {
            await base.ReadUnknownTest(storage);
        }

        [TestMethod]
        public async Task Ram_UpdateObjectTest()
        {
            await base.UpdateObjectTest(storage);
        }

        [TestMethod]
        public async Task Ram_DeleteObjectTest()
        {
            await base.DeleteObjectTest(storage);
        }
    }

    [TestClass]
    public class FileStorageTests : StorageTests
    {
        private IStorage storage;
        public FileStorageTests() { }

        [TestInitialize]
        public void initialize()
        {
            string path = Path.Combine(Environment.GetEnvironmentVariable("temp"), "FileStorageTest");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            foreach (var file in Directory.GetFiles(path))
                File.Delete(file);
            storage = new FileStorage(path);
        }

        [TestMethod]
        public async Task File_CreateObjectTest()
        {
            await base.CreateObjectTest(this.storage);
        }

        [TestMethod]
        public async Task File_ReadUnknownTest()
        {
            await base.ReadUnknownTest(this.storage);
        }

        [TestMethod]
        public async Task File_UpdateObjectTest()
        {
            await base.UpdateObjectTest(this.storage);
        }

        [TestMethod]
        public async Task File_DeleteObjectTest()
        {
            await base.DeleteObjectTest(this.storage);
        }
    }


    public class TestItem : StoreItem
    {
        public string Id { get; set; }

        public int Count { get; set; }
    }

    public class StorageTests
    {
        public async Task ReadUnknownTest(IStorage storage)
        {
            var result = await storage.Read(new[] { "unknown" });
            Assert.IsNotNull(result, "result should not be null");
            Assert.IsNull(result["unknown"], "unknown should be null");
        }

        public async Task CreateObjectTest(IStorage storage)
        {
            var storeItems = new StoreItems();
            storeItems["create1"] = new TestItem() { Id = "1" };
            dynamic newItem2 = new TestItem() { Id = "2" };
            newItem2.dyno = "dynamicStuff";
            storeItems["create2"] = newItem2;

            await storage.Write(storeItems);

            dynamic result = await storage.Read(new string[] { "create1", "create2" });
            Assert.IsNotNull(result.create1, "create1 should not be null");
            Assert.AreEqual(result.create1.Id, "1", "strong create1.id should be 1");
            Assert.IsNotNull(result.create2, "create2 should not be null");
            Assert.AreEqual(result.create2.Id, "2", "create2.id should be 2");
            Assert.AreEqual(result.create2.dyno, "dynamicStuff", "create2.dyno should be dynoStuff");
        }

        public async Task UpdateObjectTest(IStorage storage)
        {
            dynamic storeItems = new StoreItems();
            storeItems.update = new TestItem() { Id = "1", Count = 1 };

            //first write should work
            await storage.Write(storeItems);

            dynamic result = await storage.Read("update");
            Assert.IsTrue(!String.IsNullOrEmpty(result.update.eTag), "etag should be set");
            Assert.AreEqual(result.update.Count, 1, "count should be 1");

            // 2nd write should work, because we have new etag
            result.update.Count++;
            await storage.Write(result);

            dynamic result2 = await storage.Read("update");
            Assert.IsTrue(!String.IsNullOrEmpty(result2.update.eTag), "etag should be set on second write too");
            Assert.AreNotEqual(result.update.eTag, result2.update.eTag, "etag should be differnt on new write");
            Assert.AreEqual(result2.update.Count, 2, "Count should be 2");

            // write with old etag should fail
            try
            {
                await storage.Write(result);
                Assert.Fail("Should throw exception on write with old etag");
            }
            catch { }

            dynamic result3 = await storage.Read("update");
            Assert.AreEqual(result3.update.Count, 2, "count should still be be two");

            // write with wildcard etag should work
            result3.update.Count = 100;
            result3.update.eTag = "*";
            await storage.Write(result3);

            dynamic result4 = await storage.Read("update");
            Assert.AreEqual(result4.update.Count, 100, "count should be 100");

            // write with empty etag should not work
            result4.update.Count = 200;
            result4.update.eTag = "";
            try
            {
                await storage.Write(result4);
                Assert.Fail("Should throw exception on write with empty etag");
            }
            catch { }

            dynamic result5 = await storage.Read("update");
            Assert.AreEqual(result5.update.Count, 100, "count should be 100");
        }

        public async Task DeleteObjectTest(IStorage storage)
        {
            dynamic storeItems = new StoreItems();
            storeItems.delete1 = new TestItem() { Id = "1", Count = 1 };

            //first write should work
            await storage.Write(storeItems);

            dynamic result = await storage.Read("delete1");
            Assert.IsTrue(!String.IsNullOrEmpty(result.delete1.eTag), "etag should be set");
            Assert.AreEqual(result.delete1.Count, 1, "count should be 1");

            await storage.Delete("delete1");

            StoreItems result2 = await storage.Read("delete1");
            Assert.IsFalse(result2.ContainsKey("delete1"), "delete1 should be null");
        }
    }
}

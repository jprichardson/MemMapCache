using MemMapCacheLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

/************************************************
 * MemMapCache.exe must be running.
 ***********************************************/ 

namespace TestMemMapCache
{

	[TestClass()]
	public class MemMapCacheTest
	{
		private TestContext testContextInstance;

		public TestContext TestContext {
			get {
				return testContextInstance;
			}
			set {
				testContextInstance = value;
			}
		}

		[Serializable]
		private class Person
		{
			public int Age;
			public string Name;
			public List<Person> Children = new List<Person>();
		}

		#region Additional test attributes
		// 
		//You can use the following additional attributes as you write your tests:
		//
		//Use ClassInitialize to run code before running the first test in the class
		//[ClassInitialize()]
		//public static void MyClassInitialize(TestContext testContext)
		//{
		//}
		//
		//Use ClassCleanup to run code after all tests in a class have run
		//[ClassCleanup()]
		//public static void MyClassCleanup()
		//{
		//}
		//
		//Use TestInitialize to run code before running each test
		//[TestInitialize()]
		//public void MyTestInitialize()
		//{
		//}
		//
		//Use TestCleanup to run code after each test has run
		//[TestCleanup()]
		//public void MyTestCleanup()
		//{
		//}
		//
		#endregion


		[TestMethod()]
		public void TestSetGet() {
			
			var cache = new MemMapCache();
			cache.Connect();

			var dt = DateTime.Now;
			cache.Set("mydt", dt, 32); //only alloc 32 bytes, by default it allocs 10MB per object

			var dt2 = cache.Get<DateTime>("mydt");
			Assert.AreEqual(dt, dt2);

			long n = 2562796233563;
			cache.Set("n", n, 8);
			var n2 = cache.Get<long>("n");
			Assert.AreEqual(n, n2);

			var s = "JP Richardson";
			cache.Set("s", s, s.Length);
			var s2 = cache.Get<string>("s");
			Assert.AreEqual(s, s2);

			var jp = new Person() { Age = 27, Name = "JP Richardson" };
			var chris = new Person() { Age = 9, Name = "Chris Richardson" };
			jp.Children.Add(chris);

			cache.Set("jp", jp);
			var jp2 = cache.Get<Person>("jp");

			Assert.AreEqual(jp.Age, jp2.Age);
			Assert.AreEqual(jp.Name, jp2.Name);
			Assert.AreEqual(chris.Name, jp.Children[0].Name);
			Assert.AreEqual(chris.Age, jp.Children[0].Age);

		}


	}
}

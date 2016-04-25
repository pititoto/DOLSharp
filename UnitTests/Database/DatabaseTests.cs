﻿/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using System.Linq;

using NUnit.Framework;

namespace DOL.Database.Tests
{
	/// <summary>
	/// Basic Database Tests
	/// </summary>
	[TestFixture]
	public class DatabaseTests
	{
		public DatabaseTests()
		{
			Database = DatabaseSetUp.Database;
		}
		
		protected SQLObjectDatabase Database { get; set; }
		
		/// <summary>
		/// Basic Tests For a Test Table
		/// </summary>
		[Test]
		public void TestTable()
		{
			// Prepare and Cleanup
			Database.RegisterDataObject(typeof(TestTable));
			
			var all = Database.SelectAllObjects<TestTable>();
			
			foreach(var obj in all)
				Database.DeleteObject(obj);
			
			var none = Database.SelectAllObjects<TestTable>();
			
			Assert.IsEmpty(none, "Database shouldn't have any record For TestTable.");
			
			var testValues = new [] { "TestValue 1", "TestValue 2", "TestValue 3" };
			
			// Add Some Data
			foreach (var values in testValues)
			{
				var data = new TestTable()
				{
					TestField = values,
				};
				
				var inserted = Database.AddObject(data);
				Assert.IsTrue(inserted, "TestTable Entry not Inserted properly for Value {0}.", values);
			}
			
			var retrieve = Database.SelectAllObjects<TestTable>();
			
			Assert.AreEqual(testValues.Length, retrieve.Count, "Retrieved Test Table Entries Count is not equals to Test Values Count.");
			
			CollectionAssert.AreEquivalent(testValues, retrieve.Select(o => o.TestField), "Retrieved Test Entries and Test Values should be Equivalent.");
			Assert.IsTrue(retrieve.All(o => o.IsPersisted), "All Retrieved Test Entries should be Persisted in database.");
			
			// Modify Some Data
			var modObj = retrieve.First(o => o.TestField == testValues.First());
			modObj.TestField = "TestValue 4";
			
			Assert.IsTrue(modObj.Dirty, "Test Table Object should be Dirty after Modifications.");
			
			var saved = Database.SaveObject(modObj);
			
			Assert.IsTrue(saved, "Test Table Object could not be saved correctly.");
			
			Assert.IsFalse(modObj.Dirty, "Test Table Object should not be Dirty after Object Save.");
			
			testValues = new [] { modObj.TestField, "TestValue 2", "TestValue 3" };

			retrieve = Database.SelectAllObjects<TestTable>();
			
			CollectionAssert.AreEquivalent(testValues, retrieve.Select(o => o.TestField), "Retrieved Test Entries after Modification should be Equivalent to Test Values.");
			
			// Delete Some Data
			
			var delObj = retrieve.First();
			
			var deleted = Database.DeleteObject(delObj);
			
			Assert.IsTrue(deleted, "Test Table Object could not be deleted correctly.");
			Assert.IsTrue(delObj.IsDeleted, "Test Table Deleted Object does not have delete flags set correctly.");
			
			testValues = retrieve.Skip(1).Select(o => o.TestField).ToArray();
			
			retrieve = Database.SelectAllObjects<TestTable>();
			
			CollectionAssert.AreEquivalent(testValues, retrieve.Select(o => o.TestField), "Retrieved Test Entries after Deletion should be Equivalent to Test Values.");
			
			// Find Object By Key
			var keyObject = retrieve.First();
			Assert.IsNotNullOrEmpty(keyObject.ObjectId, "Test Table Retrieved Object should have an Object Id");
			
			var retrieveKeyObj = Database.FindObjectByKey<TestTable>(keyObject.ObjectId);
			Assert.IsNotNull(retrieveKeyObj, "Test Table Retrieved Object by Key should not be null.");
			Assert.AreEqual(retrieveKeyObj.ObjectId, keyObject.ObjectId, "Test Table Key Object and Retrieved Key Object should have same Object Id.");
			Assert.AreEqual(retrieveKeyObj.TestField, keyObject.TestField, "Test Table Key Object and Retrieved Key Object should have same Values.");
		}
		
		/// <summary>
		/// Test Table with Primary Auto Increment
		/// </summary>
		[Test]
		public void TestTableAutoIncrement()
		{
			// Prepare and Cleanup
			Database.RegisterDataObject(typeof(TestTableAutoInc));
			
			var all = Database.SelectAllObjects<TestTableAutoInc>();
			
			foreach(var obj in all)
				Database.DeleteObject(obj);
			
			var none = Database.SelectAllObjects<TestTableAutoInc>();
			
			Assert.IsEmpty(none, "Database shouldn't have any record For TestTableAutoInc.");
			
			var addObj = new TestTableAutoInc() { TestField = "Test AutoInc" };
			
			// Insert a Test Object for guessing last auto increment
			var inserted = Database.AddObject(addObj);
			
			Assert.IsTrue(inserted, "Test Table Auto Inc could not insert a new Entry.");
			
			var autoInc = addObj.PrimaryKey;
			
			Assert.AreNotEqual(autoInc, default(int), "Test Table Auto Inc Primary should not be Default value after insertion.");
			
			// Add Another Object to Check Primary Key Increment
			var otherObj = new TestTableAutoInc() { TestField = "Test AutoInc Other" };
			
			var otherInsert = Database.AddObject(otherObj);
			
			Assert.IsTrue(otherInsert, "Test Table Auto Inc could not insert an other Entry.");
			
			var otherAutoInc = otherObj.PrimaryKey;
			Assert.Greater(otherAutoInc, autoInc, "Newly Inserted Test Table Auto Inc Other Entry should have a Greater Primary Key Increment.");
			
			// Try Deleting and Re-inserting
			var reDeleted = Database.DeleteObject(otherObj);
			Assert.IsTrue(reDeleted, "Test Table Auto Inc could not delete other Entry from Table.");
			Assert.IsTrue(otherObj.IsDeleted, "Test Table Auto Inc other Entry deleted Flag should be true.");
			Assert.IsFalse(otherObj.IsPersisted, "Test Table Auto Inc other Entry Persisted Flag should be false.");
			
			otherObj.PrimaryKey = default(int);
			var reInserted = Database.AddObject(otherObj);
			Assert.IsTrue(reInserted, "Test Table Auto Inc could not insert other Entry in Table again.");
			
			Assert.Greater(otherObj.PrimaryKey, otherAutoInc, "Re-Added Test Table Auto Inc Entry should have a Greater Primary Key Increment.");
			
			// Try modifying to check that Primary Key is Used for Update Where Clause
			otherObj.TestField = "Test AutoInc Other Modified !";
			Assert.IsTrue(otherObj.Dirty, "Test Table Auto Inc Other Object should be Dirty after Modifications.");
			var modified = Database.SaveObject(otherObj);
			Assert.IsTrue(modified, "Test Table Auto Inc Other Object could not be Modified.");
			Assert.IsFalse(otherObj.Dirty, "Test Table Auto Inc Other Object should not be Dirty after save.");
			
			var retrieve = Database.FindObjectByKey<TestTableAutoInc>(otherObj.PrimaryKey);
			Assert.IsNotNull(retrieve, "Test Table Auto Inc Other Object could not be Retrieved through Primary Key.");
			Assert.AreEqual(otherObj.TestField, retrieve.TestField, "Test Table Auto Inc Retrieved Object is different from Other Object.");
		}
		
		/// <summary>
		/// Test Table with Unique Field
		/// </summary>
		[Test]
		public void TestTableUnique()
		{
			// Prepare and Cleanup
			Database.RegisterDataObject(typeof(TestTableUniqueField));
			
			var all = Database.SelectAllObjects<TestTableUniqueField>();
			
			foreach(var obj in all)
				Database.DeleteObject(obj);
			
			var none = Database.SelectAllObjects<TestTableUniqueField>();
			
			Assert.IsEmpty(none, "Database shouldn't have any record For TestTableUniqueField.");
			
			// Test Add
			var uniqueObj = new TestTableUniqueField { TestField = "Test Value Unique", Unique = 1 };
			
			var inserted = Database.AddObject(uniqueObj);
			
			Assert.IsTrue(inserted, "Test Table Unique Field could not insert unique object.");
			
			// Try Adding with unique Value
			var otherUniqueObj = new TestTableUniqueField { TestField = "Test Value Other Unique", Unique = 1 };
			
			var otherInserted = Database.AddObject(otherUniqueObj);
			
			Assert.IsFalse(otherInserted, "Test Table Unique Field with Other Object violating unique constraint should not be inserted.");
			
			// Try Adding with non unique Value
			var otherNonUniqueObj = new TestTableUniqueField { TestField = "Test Value Other Non-Unique", Unique = 2 };
			
			var nonUniqueInserted = Database.AddObject(otherNonUniqueObj);
			
			Assert.IsTrue(nonUniqueInserted, "Test Table Unique Field with Other Non Unique Object could not be inserted");
			
			// Try saving with unique Value
			var retrieved = Database.FindObjectByKey<TestTableUniqueField>(otherNonUniqueObj.ObjectId);
			
			retrieved.Unique = 1;
			
			var saved = Database.SaveObject(retrieved);
			
			Assert.IsFalse(saved, "Test Table Unique Field with Retrieved Object violating unique constraint should not be saved.");
			
			// Delete Previous Unique and Try Reinsert.
			var deleted = Database.DeleteObject(uniqueObj);
			Assert.IsTrue(deleted, "Test Table Unique Field could not delete unique object.");
			Assert.IsTrue(uniqueObj.IsDeleted, "Test Table Unique Field unique object should have delete flag set.");
			
			var retrievedSaved = Database.SaveObject(retrieved);
			
			Assert.IsTrue(retrievedSaved, "Test Table Unique Field Retrieved Object could not be inserted after deleting previous constraint violating object.");
		}
		
		/// <summary>
		/// Test Table with Relation 1-1
		/// </summary>
		[Test]
		public void TestTableRelation()
		{
			// Prepare and Cleanup
			Database.RegisterDataObject(typeof(TestTableRelation));
			Database.RegisterDataObject(typeof(TestTableRelationEntry));
			
			var all = Database.SelectAllObjects<TestTableRelationEntry>();
			
			foreach(var obj in all)
				Database.DeleteObject(obj);
			
			var none = Database.SelectAllObjects<TestTableRelationEntry>();
			
			Assert.IsEmpty(none, "Database shouldn't have any record For TestTableRelationEntry.");
			
			var allrel = Database.SelectAllObjects<TestTableRelation>();
			
			foreach(var obj in allrel)
				Database.DeleteObject(obj);
			
			var nonerel = Database.SelectAllObjects<TestTableRelation>();
			
			Assert.IsEmpty(nonerel, "Database shouldn't have any record For TestTableRelation.");
			
			// Try Add with no Relation
			var noRelObj = new TestTableRelation() { TestField = "RelationTestValue" };
			
			var inserted = Database.AddObject(noRelObj);
			
			Assert.IsTrue(inserted, "Test Table Relation could not insert object with no relation.");
			Assert.IsNull(noRelObj.Entry, "Test Table Relation object with no relation should have null Entry.");
			
			// Try Adding Relation
			var relObj = new TestTableRelationEntry() { TestField = "RelationEntryTestValue", ObjectId = noRelObj.ObjectId };
			
			var relInserted = Database.AddObject(relObj);
			
			Assert.IsTrue(relInserted, "Test Table Relation Entry could not be inserted.");
			
			noRelObj.Entry = relObj;
			
			var saved = Database.SaveObject(noRelObj);
			
			Assert.IsTrue(saved, "Test Table Relation could not save Object with a new relation Added.");
			
			// Try Retrieving Relation
			var retrieve = Database.FindObjectByKey<TestTableRelation>(noRelObj.ObjectId);
			
			Assert.IsNotNull(retrieve, "Test Table Relation could not retrieve relation object by ObjectId.");
			Assert.IsNotNull(retrieve.Entry, "Test Table Relation retrieved object have no entry object.");
			Assert.AreEqual(relObj.TestField, retrieve.Entry.TestField, "Test Table Relation retrieved object Entry Relation is different from created object.");
			
			// Try Deleting Relation
			var deleted = Database.DeleteObject(noRelObj);
			
			Assert.IsTrue(deleted, "Test Table Relation could not delete object with relation.");
			Assert.IsTrue(noRelObj.IsDeleted, "Test Table Relation deleted object should have deleted flag set.");
			
			// Check that Relation was deleted
			var relRetrieve = Database.FindObjectByKey<TestTableRelationEntry>(relObj.ObjectId);
			
			Assert.IsNull(relRetrieve, "Test Table Relation Entry was not auto deleted with relation object.");
			Assert.IsTrue(relObj.IsDeleted, "Test Table Relation Entry should have deleted flag set after auto delete.");
		}

		/// <summary>
		/// Test Table with Relation 1-n
		/// </summary>
		[Test]
		public void TestTableRelations()
		{
			// Prepare and Cleanup
			Database.RegisterDataObject(typeof(TestTableRelations));
			Database.RegisterDataObject(typeof(TestTableRelationsEntries));
			
			var all = Database.SelectAllObjects<TestTableRelationsEntries>();
			
			foreach(var obj in all)
				Database.DeleteObject(obj);
			
			var none = Database.SelectAllObjects<TestTableRelationsEntries>();
			
			Assert.IsEmpty(none, "Database shouldn't have any record For TestTableRelationsEntries.");
			
			var allrel = Database.SelectAllObjects<TestTableRelations>();
			
			foreach(var obj in allrel)
				Database.DeleteObject(obj);
			
			var nonerel = Database.SelectAllObjects<TestTableRelations>();
			
			Assert.IsEmpty(nonerel, "Database shouldn't have any record For TestTableRelations.");
			
			// Try Add With no Relation
			var noRelObj = new TestTableRelations() { TestField = "RelationsTestValue" };
			
			var inserted = Database.AddObject(noRelObj);
			
			Assert.IsTrue(inserted, "Test Table Relations could not insert object with no relation.");
			Assert.IsNull(noRelObj.Entries, "Test Table Relations object with no relation should have null Entry.");
			
			// Try Adding Relation
			var testValues = new[] { "RelationsEntriesTestValue 1", "RelationsEntriesTestValue 2", "RelationsEntriesTestValue 3" };
			
			var relObjs = testValues.Select(val => new TestTableRelationsEntries() { TestField = val, ForeignTestField = noRelObj.ObjectId }).ToArray();
			
			var relInserted = relObjs.Select(o => Database.AddObject(o)).ToArray();
			
			Assert.IsTrue(relInserted.All(res => res), "Test Table Relations Entries could not be inserted.");
			
			noRelObj.Entries = relObjs;
			
			var saved = Database.SaveObject(noRelObj);
			
			Assert.IsTrue(saved, "Test Table Relations could not save Object with a new relations Added.");
			
			// Try Retrieving Relation
			var retrieve = Database.FindObjectByKey<TestTableRelations>(noRelObj.ObjectId);
			
			Assert.IsNotNull(retrieve, "Test Table Relations could not retrieve relations object by ObjectId.");
			Assert.IsNotNull(retrieve.Entries, "Test Table Relations retrieved object have no entries objects.");
			CollectionAssert.AreEquivalent(testValues, retrieve.Entries.Select(o => o.TestField), 
			                               "Test Table Relations retrieved objects Entries Relation are different from created objects.");

			// Try Deleting Relation
			var deleted = Database.DeleteObject(noRelObj);
			
			Assert.IsTrue(deleted, "Test Table Relations could not delete object with relations.");
			Assert.IsTrue(noRelObj.IsDeleted, "Test Table Relations deleted object should have deleted flag set.");
			
			// Check that Relation was deleted
			var relRetrieve = Database.SelectAllObjects<TestTableRelationsEntries>().Where(o => o.ForeignTestField == noRelObj.ObjectId);
			
			Assert.IsEmpty(relRetrieve, "Test Table Relations Entries were not auto deleted with relations object.");
			Assert.IsTrue(relObjs.All(o => o.IsDeleted), "Test Table Relations Entries should have deleted flags set after auto delete.");
		}
		
		/// <summary>
		/// Test Table with Multiple Unique Fields
		/// </summary>
		[Test]
		public void TestTableMultiUnique()
		{
			// Prepare and Cleanup
			Database.RegisterDataObject(typeof(TestTableMultiUnique));
			
			var all = Database.SelectAllObjects<TestTableMultiUnique>();
			
			foreach(var obj in all)
				Database.DeleteObject(obj);
			
			var none = Database.SelectAllObjects<TestTableMultiUnique>();
			
			Assert.IsEmpty(none, "Database shouldn't have any record For TestTableMultiUnique.");
		}
	}
}

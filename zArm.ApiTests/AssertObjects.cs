using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace zArm.ApiTests
{
    [System.Diagnostics.DebuggerStepThrough]
    public static class AssertObjects
    {
        public static void HaveEqualProperties(object expected, object actual, bool deepCompare, params string[] ignoreProperties)
        {

            CompareObjects compare = new CompareObjects();
            compare.MaxDifferences = 20;
            compare.CompareChildren = deepCompare;
            compare.CompareDatesWithSqlDateTime = true;
            if (ignoreProperties != null)
                compare.ElementsToIgnore.AddRange(ignoreProperties);
            bool match = compare.Compare(expected, actual);
            if (!match)
            {
                Assert.Fail(string.Format("Comparison on expected type \"{0}\" was not the equal", expected.GetType().Name) + compare.DifferencesString);
            }

        }
    }




    public class CompareObjects
    {
        #region Class Variables
        private List<String> _differences = new List<String>();
        private List<object> _parents = new List<object>();
        private List<string> _elementsToIgnore = new List<string>();
        private bool _comparePrivateProperties = false;
        private bool _comparePrivateFields = false;
        private bool _compareChildren = true;
        private bool _compareReadOnly = true;
        private bool _compareFields = true;
        private bool _compareProperties = true;
        private int _maxDifferences = 1;
        #endregion

        #region Properties

        public bool CompareDatesWithSqlDateTime { get; set; }

        /// <summary>
        /// Ignore classes, properties, or fields by name during the comparison.
        /// Case sensitive.
        /// </summary>
        public List<string> ElementsToIgnore
        {
            get { return _elementsToIgnore; }
            set { _elementsToIgnore = value; }
        }

        /// <summary>
        /// If true, private properties will be compared. The default is false.
        /// </summary>
        public bool ComparePrivateProperties
        {
            get { return _comparePrivateProperties; }
            set { _comparePrivateProperties = value; }
        }

        /// <summary>
        /// If true, private fields will be compared. The default is false.
        /// </summary>
        public bool ComparePrivateFields
        {
            get { return _comparePrivateFields; }
            set { _comparePrivateFields = value; }
        }

        /// <summary>
        /// If true, child objects will be compared. The default is true. 
        /// If false, and a list or array is compared list items will be compared but not their children.
        /// </summary>
        public bool CompareChildren
        {
            get { return _compareChildren; }
            set { _compareChildren = value; }
        }

        /// <summary>
        /// If true, compare read only properties (only the getter is implemented).
        /// The default is true.
        /// </summary>
        public bool CompareReadOnly
        {
            get { return _compareReadOnly; }
            set { _compareReadOnly = value; }
        }

        /// <summary>
        /// If true, compare fields of a class (see also CompareProperties).
        /// The default is true.
        /// </summary>
        public bool CompareFields
        {
            get { return _compareFields; }
            set { _compareFields = value; }
        }

        /// <summary>
        /// If true, compare properties of a class (see also CompareFields).
        /// The default is true.
        /// </summary>
        public bool CompareProperties
        {
            get { return _compareProperties; }
            set { _compareProperties = value; }
        }

        /// <summary>
        /// The maximum number of differences to detect
        /// </summary>
        /// <remarks>
        /// Default is 1 for performance reasons.
        /// </remarks>
        public int MaxDifferences
        {
            get { return _maxDifferences; }
            set { _maxDifferences = value; }
        }

        /// <summary>
        /// The differences found during the compare
        /// </summary>
        public List<String> Differences
        {
            get { return _differences; }
            set { _differences = value; }
        }

        /// <summary>
        /// The differences found in a string suitable for a textbox
        /// </summary>
        public string DifferencesString
        {
            get
            {
                StringBuilder sb = new StringBuilder(4096);

                sb.Append("\r\nBegin Differences:\r\n");

                foreach (string item in Differences)
                {
                    sb.AppendFormat("{0}\r\n", item);
                }

                sb.AppendFormat("End Differences (Maximum of {0} differences shown).", MaxDifferences);

                return sb.ToString();
            }
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Compare two objects of the same type to each other.
        /// </summary>
        /// <remarks>
        /// Check the Differences or DifferencesString Properties for the differences.
        /// Default MaxDifferences is 1 for performance
        /// </remarks>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <returns>True if they are equal</returns>
        public bool Compare(object expected, object actual)
        {
            string defaultBreadCrumb = string.Empty;

            Differences.Clear();
            Compare(expected, actual, defaultBreadCrumb);

            return Differences.Count == 0;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Compare two objects
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <param name="breadCrumb">Where we are in the object hiearchy</param>
        private void Compare(object expected, object actual, string breadCrumb)
        {
            //If both null return true
            if (expected == null && actual == null)
                return;

            //Check if one of them is null
            if (expected == null)
            {
                Differences.Add(string.Format("expected{0} == null && actual{0} != null ((null),{1})", breadCrumb, cStr(actual)));
                return;
            }

            if (actual == null)
            {
                Differences.Add(string.Format("expected{0} != null && actual{0} == null ({1},(null))", breadCrumb, cStr(expected)));
                return;
            }

            Type t1 = expected.GetType();
            Type t2 = actual.GetType();

            //Objects must be the same type
            if (t1 != t2)
            {
                Differences.Add(string.Format("Different Types:  expected{0}.GetType() != actual{0}.GetType()", breadCrumb));
                return;
            }

            if (IsDataset(t1))
            {
                CompareDataset(expected, actual, breadCrumb);
            }
            else if (IsDataTable(t1))
            {
                CompareDataTable(expected, actual, breadCrumb);
            }
            else if (IsDataRow(t1))
            {
                CompareDataRow(expected, actual, breadCrumb);
            }
            else if (IsIList(t1)) //This will do arrays, multi-dimensional arrays and generic lists
            {
                CompareIList(expected, actual, breadCrumb);
            }
            else if (IsIDictionary(t1))
            {
                CompareIDictionary(expected, actual, breadCrumb);
            }
            else if (IsEnum(t1))
            {
                CompareEnum(expected, actual, breadCrumb);
            }
            else if (IsSimpleType(t1))
            {
                CompareSimpleType(expected, actual, breadCrumb);
            }
            else if (IsClass(t1))
            {
                CompareClass(expected, actual, breadCrumb);
            }
            else if (IsTimespan(t1))
            {
                CompareTimespan(expected, actual, breadCrumb);
            }
            else if (IsStruct(t1))
            {
                CompareStruct(expected, actual, breadCrumb);
            }
            else
            {
                throw new NotImplementedException("Cannot compare object of type " + t1.Name);
            }

        }

        private void CompareDataRow(object expected, object actual, string breadCrumb)
        {
            DataRow dataRow1 = expected as DataRow;
            DataRow dataRow2 = actual as DataRow;

            if (dataRow1 == null) //This should never happen, null check happens one level up
                throw new ArgumentNullException("expected");

            if (dataRow2 == null) //This should never happen, null check happens one level up
                throw new ArgumentNullException("actual");

            for (int i = 0; i < dataRow1.Table.Columns.Count; i++)
            {
                //If we should ignore it, skip it
                if (ElementsToIgnore.Contains(dataRow1.Table.Columns[i].ColumnName))
                    continue;

                //If we should ignore read only, skip it
                if (!CompareReadOnly && dataRow1.Table.Columns[i].ReadOnly)
                    continue;

                //Both are null
                if (dataRow1.IsNull(i) && dataRow2.IsNull(i))
                    continue;

                string currentBreadCrumb = AddBreadCrumb(breadCrumb, string.Empty, string.Empty, dataRow1.Table.Columns[i].ColumnName);

                //Check if one of them is null
                if (dataRow1.IsNull(i))
                {
                    Differences.Add(string.Format("expected{0} == null && actual{0} != null ((null),{1})", currentBreadCrumb, cStr(actual)));
                    return;
                }

                if (dataRow2.IsNull(i))
                {
                    Differences.Add(string.Format("expected{0} != null && actual{0} == null ({1},(null))", currentBreadCrumb, cStr(expected)));
                    return;
                }

                Compare(dataRow1[i], dataRow2[i], currentBreadCrumb);

                if (Differences.Count >= MaxDifferences)
                    return;
            }
        }

        private void CompareDataTable(object expected, object actual, string breadCrumb)
        {
            DataTable dataTable1 = expected as DataTable;
            DataTable dataTable2 = actual as DataTable;

            if (dataTable1 == null) //This should never happen, null check happens one level up
                throw new ArgumentNullException("expected");

            if (dataTable2 == null) //This should never happen, null check happens one level up
                throw new ArgumentNullException("actual");

            //If we should ignore it, skip it
            if (ElementsToIgnore.Contains(dataTable1.TableName))
                return;

            //There must be the same amount of rows in the datatable
            if (dataTable1.Rows.Count != dataTable2.Rows.Count)
            {
                Differences.Add(string.Format("expected{0}.Rows.Count != actual{0}.Rows.Count ({1},{2})", breadCrumb,
                                              dataTable1.Rows.Count, dataTable2.Rows.Count));

                if (Differences.Count >= MaxDifferences)
                    return;
            }

            //There must be the same amount of columns in the datatable
            if (dataTable1.Columns.Count != dataTable2.Columns.Count)
            {
                Differences.Add(string.Format("expected{0}.Columns.Count != actual{0}.Columns.Count ({1},{2})", breadCrumb,
                                              dataTable1.Columns.Count, dataTable2.Columns.Count));

                if (Differences.Count >= MaxDifferences)
                    return;
            }

            for (int i = 0; i < dataTable1.Rows.Count; i++)
            {
                string currentBreadCrumb = AddBreadCrumb(breadCrumb, "Rows", string.Empty, i);

                CompareDataRow(dataTable1.Rows[i], dataTable2.Rows[i], currentBreadCrumb);

                if (Differences.Count >= MaxDifferences)
                    return;
            }
        }

        private void CompareDataset(object expected, object actual, string breadCrumb)
        {
            DataSet dataSet1 = expected as DataSet;
            DataSet dataSet2 = actual as DataSet;

            if (dataSet1 == null) //This should never happen, null check happens one level up
                throw new ArgumentNullException("expected");

            if (dataSet2 == null) //This should never happen, null check happens one level up
                throw new ArgumentNullException("actual");


            //There must be the same amount of tables in the dataset
            if (dataSet1.Tables.Count != dataSet2.Tables.Count)
            {
                Differences.Add(string.Format("expected{0}.Tables.Count != actual{0}.Tables.Count ({1},{2})", breadCrumb,
                                              dataSet1.Tables.Count, dataSet2.Tables.Count));

                if (Differences.Count >= MaxDifferences)
                    return;
            }

            for (int i = 0; i < dataSet1.Tables.Count; i++)
            {
                string currentBreadCrumb = AddBreadCrumb(breadCrumb, "Tables", string.Empty, dataSet1.Tables[i].TableName);

                CompareDataTable(dataSet1.Tables[i], dataSet2.Tables[i], currentBreadCrumb);

                if (Differences.Count >= MaxDifferences)
                    return;
            }
        }

        private bool IsTimespan(Type t)
        {
            return t == typeof(TimeSpan);
        }

        private bool IsEnum(Type t)
        {
            return t.IsEnum;
        }

        private bool IsStruct(Type t)
        {
            return t.IsValueType && !IsSimpleType(t);
        }

        private bool IsSimpleType(Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                t = Nullable.GetUnderlyingType(t);
            }

            return t.IsPrimitive
                || t == typeof(DateTime)
                || t == typeof(decimal)
                || t == typeof(string)
                || t == typeof(Guid);

        }

        private bool ValidStructSubType(Type t)
        {
            return IsSimpleType(t)
                || IsEnum(t)
                || IsArray(t)
                || IsClass(t)
                || IsIDictionary(t)
                || IsTimespan(t)
                || IsIList(t);
        }

        private bool IsArray(Type t)
        {
            return t.IsArray;
        }

        private bool IsClass(Type t)
        {
            return t.IsClass;
        }

        private bool IsIDictionary(Type t)
        {
            return t.GetInterface("System.Collections.IDictionary", true) != null;
        }

        private bool IsDataset(Type t)
        {
            return t == typeof(DataSet);
        }

        private bool IsDataRow(Type t)
        {
            return t == typeof(DataRow);
        }

        private bool IsDataTable(Type t)
        {
            return t == typeof(DataTable);
        }

        private bool IsIList(Type t)
        {
            return t.GetInterface("System.Collections.IList", true) != null;
        }

        private bool IsChildType(Type t)
        {
            return !IsSimpleType(t)
                && (IsClass(t)
                    || IsArray(t)
                    || IsIDictionary(t)
                    || IsIList(t)
                    || IsStruct(t));
        }

        /// <summary>
        /// Compare a timespan struct
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <param name="breadCrumb"></param>
        private void CompareTimespan(object expected, object actual, string breadCrumb)
        {
            if (((TimeSpan)expected).Ticks != ((TimeSpan)actual).Ticks)
            {
                Differences.Add(string.Format("expected{0}.Ticks != actual{0}.Ticks", breadCrumb));
            }
        }

        /// <summary>
        /// Compare an enumeration
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <param name="breadCrumb"></param>
        private void CompareEnum(object expected, object actual, string breadCrumb)
        {
            if (expected.ToString() != actual.ToString())
            {
                string currentBreadCrumb = AddBreadCrumb(breadCrumb, expected.GetType().Name, string.Empty, -1);
                Differences.Add(string.Format("expected{0} != actual{0} ({1},{2})", currentBreadCrumb, expected, actual));
            }
        }

        /// <summary>
        /// Compare a simple type
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <param name="breadCrumb"></param>
        private void CompareSimpleType(object expected, object actual, string breadCrumb)
        {
            if (actual == null) //This should never happen, null check happens one level up
                throw new ArgumentNullException("actual");

            //swap object for SqlDateTime
            if (expected is DateTime && CompareDatesWithSqlDateTime)
            {
                try
                {
                    var expectedNew = new SqlDateTime((DateTime)expected);
                    var actualNew = new SqlDateTime((DateTime)actual);
                    expected = expectedNew;
                    actual = actualNew;
                }
                catch { }
            }

            IComparable valOne = expected as IComparable;

            if (valOne == null) //This should never happen, null check happens one level up
                throw new ArgumentNullException("expected");

            if (valOne.CompareTo(actual) != 0)
            {
                Differences.Add(string.Format("expected{0} != actual{0} ({1},{2})", breadCrumb, expected, actual));
            }
        }



        /// <summary>
        /// Compare a struct
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <param name="breadCrumb"></param>
        private void CompareStruct(object expected, object actual, string breadCrumb)
        {
            string currentCrumb;
            Type t1 = expected.GetType();

            //Compare the fields
            FieldInfo[] currentFields = t1.GetFields();

            foreach (FieldInfo item in currentFields)
            {
                //Only compare simple types within structs (Recursion Problems)
                if (!ValidStructSubType(item.FieldType))
                {
                    continue;
                }

                currentCrumb = AddBreadCrumb(breadCrumb, item.Name, string.Empty, -1);

                Compare(item.GetValue(expected), item.GetValue(actual), currentCrumb);

                if (Differences.Count >= MaxDifferences)
                    return;
            }

        }

        /// <summary>
        /// Compare the properties, fields of a class
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <param name="breadCrumb"></param>
        private void CompareClass(object expected, object actual, string breadCrumb)
        {
            try
            {
                _parents.Add(expected);
                _parents.Add(actual);
                Type t1 = expected.GetType();

                //We ignore the class name
                if (ElementsToIgnore.Contains(t1.Name))
                    return;

                //Compare the properties
                if (CompareProperties)
                    PerformCompareProperties(t1, expected, actual, breadCrumb);

                //Compare the fields
                if (CompareFields)
                    PerformCompareFields(t1, expected, actual, breadCrumb);
            }
            finally
            {
                _parents.Remove(expected);
                _parents.Remove(actual);
            }
        }

        /// <summary>
        /// Compare the fields of a class
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <param name="breadCrumb"></param>
        private void PerformCompareFields(Type t1,
            object expected,
            object actual,
            string breadCrumb)
        {
            object objectValue1;
            object objectValue2;
            string currentCrumb;

            FieldInfo[] currentFields;

            if (ComparePrivateFields)
                currentFields = t1.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            else
                currentFields = t1.GetFields(); //Default is public instance

            foreach (FieldInfo item in currentFields)
            {
                //Skip if this is a shallow compare
                if (!CompareChildren && IsChildType(item.FieldType))
                    continue;

                //If we should ignore it, skip it
                if (ElementsToIgnore.Contains(item.Name))
                    continue;

                objectValue1 = item.GetValue(expected);
                objectValue2 = item.GetValue(actual);

                bool expectedIsParent = objectValue1 != null && (objectValue1 == expected || _parents.Contains(objectValue1));
                bool actualIsParent = objectValue2 != null && (objectValue2 == actual || _parents.Contains(objectValue2));

                //Skip fields that point to the parent
                if (IsClass(item.FieldType)
                    && (expectedIsParent || actualIsParent))
                {
                    continue;
                }

                currentCrumb = AddBreadCrumb(breadCrumb, item.Name, string.Empty, -1);

                Compare(objectValue1, objectValue2, currentCrumb);

                if (Differences.Count >= MaxDifferences)
                    return;
            }
        }


        /// <summary>
        /// Compare the properties of a class
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <param name="breadCrumb"></param>
        private void PerformCompareProperties(Type t1,
            object expected,
            object actual,
            string breadCrumb)
        {
            object objectValue1;
            object objectValue2;
            string currentCrumb;

            PropertyInfo[] currentProperties;

            if (ComparePrivateProperties)
                currentProperties = t1.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            else
                currentProperties = t1.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo info in currentProperties)
            {
                //If we can't read it, skip it
                if (info.CanRead == false)
                    continue;

                //Skip if this is a shallow compare
                if (!CompareChildren && IsChildType(info.PropertyType))
                    continue;

                //If we should ignore it, skip it
                if (ElementsToIgnore.Contains(info.Name))
                    continue;

                //If we should ignore read only, skip it
                if (!CompareReadOnly && info.CanWrite == false)
                    continue;

                if (!IsValidIndexer(info, expected, actual, breadCrumb))
                {
                    objectValue1 = info.GetValue(expected, null);
                    objectValue2 = info.GetValue(actual, null);
                }
                else
                {
                    CompareIndexer(info, expected, actual, breadCrumb);
                    continue; ;
                }

                bool expectedIsParent = objectValue1 != null && (objectValue1 == expected || _parents.Contains(objectValue1));
                bool actualIsParent = objectValue2 != null && (objectValue2 == actual || _parents.Contains(objectValue2));

                //Skip properties where both point to the corresponding parent
                if (IsClass(info.PropertyType)
                    && (expectedIsParent && actualIsParent))
                {
                    continue;
                }

                currentCrumb = AddBreadCrumb(breadCrumb, info.Name, string.Empty, -1);

                Compare(objectValue1, objectValue2, currentCrumb);

                if (Differences.Count >= MaxDifferences)
                    return;
            }
        }

        private bool IsValidIndexer(PropertyInfo info, object expected, object actual, string breadCrumb)
        {
            ParameterInfo[] indexers = info.GetIndexParameters();

            if (indexers.Length == 0)
            {
                return false;
            }
            else if (indexers.Length > 1)
            {
                throw new Exception("Cannot compare objects with more than one indexer for object " + breadCrumb);
            }
            else if (indexers[0].ParameterType != typeof(Int32))
            {
                throw new Exception("Cannot compare objects with a non integer indexer for object " + breadCrumb);
            }
            else if (info.ReflectedType.GetProperty("Count") == null)
            {
                throw new Exception("Indexer must have a corresponding Count property for object " + breadCrumb);
            }
            else if (info.ReflectedType.GetProperty("Count").PropertyType != typeof(Int32))
            {
                throw new Exception("Indexer must have a corresponding Count property that is an integer for object " + breadCrumb);
            }

            return true;
        }
        private void CompareIndexer(PropertyInfo info, object expected, object actual, string breadCrumb)
        {
            string currentCrumb;
            int indexerCount1 = (int)info.ReflectedType.GetProperty("Count").GetGetMethod().Invoke(expected, new object[] { });
            int indexerCount2 = (int)info.ReflectedType.GetProperty("Count").GetGetMethod().Invoke(actual, new object[] { });

            //Indexers must be the same length
            if (indexerCount1 != indexerCount2)
            {
                currentCrumb = AddBreadCrumb(breadCrumb, info.Name, string.Empty, -1);
                Differences.Add(string.Format("expected{0}.Count != actual{0}.Count ({1},{2})", currentCrumb,
                                              indexerCount1, indexerCount2));

                if (Differences.Count >= MaxDifferences)
                    return;
            }

            // Run on indexer
            for (int i = 0; i < indexerCount1; i++)
            {
                currentCrumb = AddBreadCrumb(breadCrumb, info.Name, string.Empty, i);
                object objectValue1 = info.GetValue(expected, new object[] { i });
                object objectValue2 = info.GetValue(actual, new object[] { i });
                Compare(objectValue1, objectValue2, currentCrumb);

                if (Differences.Count >= MaxDifferences)
                    return;
            }
        }

        /// <summary>
        /// Compare a dictionary
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <param name="breadCrumb"></param>
        private void CompareIDictionary(object expected, object actual, string breadCrumb)
        {
            IDictionary iDict1 = expected as IDictionary;
            IDictionary iDict2 = actual as IDictionary;

            if (iDict1 == null) //This should never happen, null check happens one level up
                throw new ArgumentNullException("expected");

            if (iDict2 == null) //This should never happen, null check happens one level up
                throw new ArgumentNullException("actual");

            try
            {
                _parents.Add(expected);
                _parents.Add(actual);

                //Objects must be the same length
                if (iDict1.Count != iDict2.Count)
                {
                    Differences.Add(string.Format("expected{0}.Count != actual{0}.Count ({1},{2})", breadCrumb,
                                                  iDict1.Count, iDict2.Count));

                    if (Differences.Count >= MaxDifferences)
                        return;
                }

                IDictionaryEnumerator enumerator1 = iDict1.GetEnumerator();
                IDictionaryEnumerator enumerator2 = iDict2.GetEnumerator();

                while (enumerator1.MoveNext() && enumerator2.MoveNext())
                {
                    string currentBreadCrumb = AddBreadCrumb(breadCrumb, "Key", string.Empty, -1);

                    Compare(enumerator1.Key, enumerator2.Key, currentBreadCrumb);

                    if (Differences.Count >= MaxDifferences)
                        return;

                    currentBreadCrumb = AddBreadCrumb(breadCrumb, "Value", string.Empty, -1);

                    Compare(enumerator1.Value, enumerator2.Value, currentBreadCrumb);

                    if (Differences.Count >= MaxDifferences)
                        return;
                }
            }
            finally
            {
                _parents.Remove(expected);
                _parents.Remove(actual);
            }
        }

        /// <summary>
        /// Convert an object to a nicely formatted string
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private string cStr(object obj)
        {
            try
            {
                if (obj == null)
                    return "(null)";

                if (obj == DBNull.Value)
                    return "System.DBNull.Value";

                return obj.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }


        /// <summary>
        /// Compare an array or something that implements IList
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <param name="breadCrumb"></param>
        private void CompareIList(object expected, object actual, string breadCrumb)
        {
            IList ilist1 = expected as IList;
            IList ilist2 = actual as IList;

            if (ilist1 == null) //This should never happen, null check happens one level up
                throw new ArgumentNullException("expected");

            if (ilist2 == null) //This should never happen, null check happens one level up
                throw new ArgumentNullException("actual");

            try
            {
                _parents.Add(expected);
                _parents.Add(actual);

                //Objects must be the same length
                if (ilist1.Count != ilist2.Count)
                {
                    Differences.Add(string.Format("expected{0}.Count != actual{0}.Count ({1},{2})", breadCrumb,
                                                  ilist1.Count, ilist2.Count));

                    if (Differences.Count >= MaxDifferences)
                        return;
                }

                IEnumerator enumerator1 = ilist1.GetEnumerator();
                IEnumerator enumerator2 = ilist2.GetEnumerator();
                int count = 0;

                while (enumerator1.MoveNext() && enumerator2.MoveNext())
                {
                    string currentBreadCrumb = AddBreadCrumb(breadCrumb, string.Empty, string.Empty, count);

                    Compare(enumerator1.Current, enumerator2.Current, currentBreadCrumb);

                    if (Differences.Count >= MaxDifferences)
                        return;

                    count++;
                }
            }
            finally
            {
                _parents.Remove(expected);
                _parents.Remove(actual);
            }
        }



        /// <summary>
        /// Add a breadcrumb to an existing breadcrumb
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="name"></param>
        /// <param name="extra"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private string AddBreadCrumb(string existing, string name, string extra, string index)
        {
            bool useIndex = !String.IsNullOrEmpty(index);
            bool useName = name.Length > 0;
            StringBuilder sb = new StringBuilder();

            sb.Append(existing);

            if (useName)
            {
                sb.AppendFormat(".");
                sb.Append(name);
            }

            sb.Append(extra);

            if (useIndex)
            {
                int result = -1;
                if (Int32.TryParse(index, out result))
                {
                    sb.AppendFormat("[{0}]", index);
                }
                else
                {
                    sb.AppendFormat("[\"{0}\"]", index);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Add a breadcrumb to an existing breadcrumb
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="name"></param>
        /// <param name="extra"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private string AddBreadCrumb(string existing, string name, string extra, int index)
        {
            return AddBreadCrumb(existing, name, extra, index >= 0 ? index.ToString() : null);
        }

        #endregion

    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModalLayer.Modal
{
    public static class TypeService
    {

        static TypeService()
        {
            TypeDictionary();
        }
        public static IDictionary<string, MappedData> allSqlTypes = new ConcurrentDictionary<string, MappedData>();
        private static dynamic GetValue(Type NewType, int Index)
        {
            try
            {
                //string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ";
                //string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
                long Ticks = DateTime.Now.Ticks % 27;
                if (NewType == typeof(string))
                {
                    string str = Path.GetRandomFileName(); //This method returns a random file name of 11 characters
                    string result = str.Replace(".", "");
                    return result;
                }
                else if (NewType == typeof(Guid))
                {
                    return Guid.NewGuid().ToString();
                }
                else if (NewType == typeof(DateTime))
                {
                    return DateTime.Now.AddDays(Convert.ToInt32(Index)).AddMinutes(Index * Ticks);
                }
                else if (NewType == typeof(bool))
                {
                    return Ticks % 2 == 0 ? true : false;
                }
                else if (NewType == typeof(byte))
                {
                    return Convert.ToByte(Guid.NewGuid().ToString());
                }
                else if (NewType == typeof(char))
                {
                    int AcurateValue = Convert.ToInt32(Ticks);
                    AcurateValue = AcurateValue > 25 ? 25 : AcurateValue;
                    return Convert.ToChar(65 + AcurateValue);
                }
                else if (NewType == typeof(decimal) || NewType == typeof(double) || NewType == typeof(Single))
                {
                    return 70000 * Ticks;
                }
                else if (NewType == typeof(Int16) || NewType == typeof(Int32) || NewType == typeof(Int64))
                {
                    return Index;
                }
                return "NULL";
            }
            catch (Exception)
            {
                return "NULL";
            }
        }

        public static MappedData GetSqlMappedObject(string Value)
        {
            var AnyValue = (KeyValuePair<string, TypeService.MappedData>)(TypeService.allSqlTypes).FirstOrDefault(x => x.Key == Value);
            if (AnyValue.Value != null)
                return AnyValue.Value;
            return null;
        }

        public static dynamic GetDefaultValue(this Type DataType, int Index)
        {
            try
            {
                return GetValue(DataType, Index);
            }
            catch (Exception)
            {
                return "NULL";
            }
        }
        public static dynamic GetDefaultValue(this string SqlDataType, int Index)
        {
            try
            {
                long Ticks = DateTime.Now.Ticks % 27;
                SqlMappedTypes sqlMappedTypes = new SqlMappedTypes();
                Type NewType = sqlMappedTypes.GetSqlMappedType(SqlDataType);

                var MappedData = TypeService.GetSqlMappedObject(SqlDataType);
                if (!MappedData.IsRealDb)
                {
                    return GetValue(NewType, Index);
                }
                return "NULL";
            }
            catch (Exception)
            {
                return "NULL";
            }
        }

        public class MappedData
        {
            public Type type { set; get; }
            public string DbType { set; get; }
            public Boolean IsVariableLength { set; get; }
            public Boolean IsRealDb { set; get; }
        }

        public static void TypeDictionary()
        {
            allSqlTypes.Add("varchar", new MappedData { type = typeof(string), DbType = "varchar", IsVariableLength = true, IsRealDb = false });
            allSqlTypes.Add("nvarchar", new MappedData { type = typeof(string), DbType = "nvarchar", IsVariableLength = true, IsRealDb = false });
            allSqlTypes.Add("string", new MappedData { type = typeof(string), DbType = "nvarchar", IsVariableLength = true, IsRealDb = false });
            allSqlTypes.Add("text", new MappedData { type = typeof(string), DbType = "text", IsVariableLength = true, IsRealDb = false });
            allSqlTypes.Add("ntext", new MappedData { type = typeof(string), DbType = "ntext", IsVariableLength = true, IsRealDb = false });
            allSqlTypes.Add("name", new MappedData { type = typeof(string), DbType = "varchar", IsVariableLength = true, IsRealDb = true });
            allSqlTypes.Add("email", new MappedData { type = typeof(string), DbType = "varchar", IsVariableLength = true, IsRealDb = true });
            allSqlTypes.Add("mobile", new MappedData { type = typeof(string), DbType = "varchar", IsVariableLength = true, IsRealDb = false });
            allSqlTypes.Add("char", new MappedData { type = typeof(char), DbType = "char", IsVariableLength = false, IsRealDb = false });
            allSqlTypes.Add("nchar", new MappedData { type = typeof(char), DbType = "nchar", IsVariableLength = false, IsRealDb = false });
            allSqlTypes.Add("smallint", new MappedData { type = typeof(Int16), DbType = "smallint", IsVariableLength = false, IsRealDb = false });
            allSqlTypes.Add("int", new MappedData { type = typeof(Int32), DbType = "int", IsVariableLength = false, IsRealDb = false });
            allSqlTypes.Add("bigint", new MappedData { type = typeof(Int64), DbType = "bigint", IsVariableLength = false, IsRealDb = false });
            allSqlTypes.Add("float", new MappedData { type = typeof(double), DbType = "float", IsVariableLength = false, IsRealDb = false });
            allSqlTypes.Add("decimal", new MappedData { type = typeof(decimal), DbType = "decimal", IsVariableLength = false, IsRealDb = false });
            allSqlTypes.Add("money", new MappedData { type = typeof(decimal), DbType = "money", IsVariableLength = false, IsRealDb = false });
            allSqlTypes.Add("date", new MappedData { type = typeof(DateTime), DbType = "date", IsVariableLength = false, IsRealDb = false });
            allSqlTypes.Add("datetime", new MappedData { type = typeof(DateTime), DbType = "datetime", IsVariableLength = false, IsRealDb = false });
            allSqlTypes.Add("real", new MappedData { type = typeof(Single), DbType = "real", IsVariableLength = false, IsRealDb = false });
            allSqlTypes.Add("binary", new MappedData { type = typeof(byte), DbType = "binary", IsVariableLength = true, IsRealDb = false });
            allSqlTypes.Add("bit", new MappedData { type = typeof(bool), DbType = "bit", IsVariableLength = false, IsRealDb = false });
            allSqlTypes.Add("uniqueidentifier", new MappedData { type = typeof(Guid), DbType = "uniqueidentifier", IsVariableLength = false, IsRealDb = false });
            allSqlTypes.Add("number", new MappedData { type = typeof(Int64), DbType = "bigint", IsVariableLength = false, IsRealDb = false });
        }
    }
    public class SqlMappedTypes
    {
        private readonly Dictionary<int, List<string>> RandomDataList;
        public SqlMappedTypes()
        {
            RandomDataList = new Dictionary<int, List<string>>();
            InitRandomData();
        }

        private void InitRandomData()
        {
            List<string> FortyRangeData = new List<string>();
            FortyRangeData.Add("The quick brown fox jumps over the lazy dog"); // > 40
            FortyRangeData.Add("Some content data that required for real time testing"); // > 40 
            FortyRangeData.Add("Do get some testing data. For tester and developer."); // > 40 
            FortyRangeData.Add("Of all the possession a man has, the only thing which has value is his character."); // > 40 
            FortyRangeData.Add("Circumstances reveal a man’s true character."); // > 40 
            FortyRangeData.Add("You can determine a person’s true character by the things he ridicules and laughs at."); // > 40 
            FortyRangeData.Add("Character does not need words from you to shine. It is upon your deeds which will cast light on it."); // > 40 
            FortyRangeData.Add("Knowledge is nothing compared to the value of one’s character."); // > 40 
            FortyRangeData.Add("Character is the most priceless and sturdiest of them all."); // > 40 
            FortyRangeData.Add("Do not pay mind to what other think you are. Who you are is what matters."); // > 40 
            RandomDataList.Add(-1, FortyRangeData);

            List<string> ThirtyRangeData = new List<string>();
            ThirtyRangeData.Add("Some dome content for test."); // > 20 && < 30
            ThirtyRangeData.Add("Play sports, it good for health."); // > 20 && < 30
            ThirtyRangeData.Add("Required for good software."); // > 20 && < 30
            ThirtyRangeData.Add("A weak attitude reflects upon a weak character"); // > 20 && < 30
            ThirtyRangeData.Add("Liar is disqualified from a good character"); // > 20 && < 30
            ThirtyRangeData.Add("You cannot follow a man who you cannot aspire"); // > 20 && < 30
            ThirtyRangeData.Add("What builds a man’s character"); // > 20 && < 30
            ThirtyRangeData.Add("How can you tell if a man has a good character"); // > 20 && < 30
            ThirtyRangeData.Add("Which virtues does he need to possess"); // > 20 && < 30
            ThirtyRangeData.Add("A weak attitude reflects upon a weak character"); // > 20 && < 30
            RandomDataList.Add(30, ThirtyRangeData);

            List<string> TwentyRangeData = new List<string>();
            TwentyRangeData.Add("Our beautify india"); // > 10 && < 20
            TwentyRangeData.Add("Get mobile number"); // > 10 && < 20
            TwentyRangeData.Add("Your demo address"); // > 10 && < 20
            TwentyRangeData.Add("Test user data"); // > 10 && < 20
            TwentyRangeData.Add("Performance data"); // > 10 && < 20
            TwentyRangeData.Add("Generic test data"); // > 10 && < 20
            TwentyRangeData.Add("Something for test"); // > 10 && < 20
            TwentyRangeData.Add("Randome data"); // > 10 && < 20
            TwentyRangeData.Add("Well this is dome"); // > 10 && < 20
            TwentyRangeData.Add("Fine job is done"); // > 10 && < 20
            RandomDataList.Add(20, TwentyRangeData);

            List<string> TenRangeData = new List<string>();
            TenRangeData.Add("Test data"); // > 5 && < 10
            TenRangeData.Add("Some one"); // > 5 && < 10
            TenRangeData.Add("Something"); // > 5 && < 10
            TenRangeData.Add("Request."); // > 5 && < 10
            TenRangeData.Add("Requested"); // > 5 && < 10
            TenRangeData.Add("Go to six"); // > 5 && < 10
            TenRangeData.Add("Mobile"); // > 5 && < 10
            TenRangeData.Add("Network"); // > 5 && < 10
            TenRangeData.Add("My bottle"); // > 5 && < 10
            TenRangeData.Add("Next what."); // > 5 && < 10
            RandomDataList.Add(10, TenRangeData);

            List<string> FiveRangeData = new List<string>();
            FiveRangeData.Add("Job"); // > 1 && < 5
            FiveRangeData.Add("Good"); // > 1 && < 5
            FiveRangeData.Add("First"); // > 1 && < 5
            FiveRangeData.Add("Last"); // > 1 && < 5
            FiveRangeData.Add("Fine"); // > 1 && < 5
            FiveRangeData.Add("Sharp"); // > 1 && < 5
            FiveRangeData.Add("Java"); // > 1 && < 5
            FiveRangeData.Add("Roy"); // > 1 && < 5
            FiveRangeData.Add("Joy"); // > 1 && < 5
            FiveRangeData.Add("Make"); // > 1 && < 5
            RandomDataList.Add(5, FiveRangeData);
        }

        public string GenerateEmail(string DefaultValue, int InnerIndex)
        {
            string Email = string.Empty;
            Email = "demoUser" + InnerIndex + "@gmail.com";
            return Email;
        }

        public string GenerateMobileNo(string DefaultValue, int InnerIndex)
        {
            string MobileNo = string.Empty;
            Random rand = new Random();
            InnerIndex = rand.Next(1, 17);
            if (InnerIndex % 2 != 0)
            {
                InnerIndex = -1 * InnerIndex;
            }
            MobileNo = "90987" + (Convert.ToInt32("86120") + InnerIndex).ToString();
            return MobileNo;
        }

        public dynamic GenerateValue(DataColumn column, int index, int innerIndex)
        {
            dynamic Value = null;
            if (column.DataType == typeof(string))
            {
                Value = RandomString(column.MaxLength, index + innerIndex);
            }
            else if (column.DataType == typeof(char))
                Value = RandomString(1, index);
            else if (column.DataType == typeof(Int16))
                Value = Convert.ToInt16(GenerateRandomNumber(index, true));
            else if (column.DataType == typeof(Int32))
                Value = Convert.ToInt32(GenerateRandomNumber(index, true));
            else if (column.DataType == typeof(Int64))
                Value = Convert.ToInt64(GenerateRandomNumber(index, true));
            else if (column.DataType == typeof(double))
                Value = Convert.ToDouble(GenerateRandomNumber(index, false));
            else if (column.DataType == typeof(decimal))
                Value = Convert.ToDecimal(GenerateRandomNumber(index, false));
            else if (column.DataType == typeof(DateTime))
                Value = DateTime.Now;
            else if (column.DataType == typeof(Single))
                Value = GenerateRandomNumber(index, false);
            else if (column.DataType == typeof(bool))
                Value = false;
            else if (column.DataType == typeof(byte))
                Value = null;
            else if (column.DataType == typeof(Guid))
                Value = Guid.NewGuid();
            return Value;
        }

        public dynamic GetDefaultValue(DataColumn column)
        {
            dynamic Value = null;
            if (column.DataType == typeof(string))
            {
                try
                {
                    Value = column.DefaultValue.ToString();
                }
                catch (Exception)
                {
                    Value = "";
                }
            }
            else if (column.DataType == typeof(char))
            {
                try
                {
                    Value = Convert.ToChar(column.DefaultValue);
                }
                catch (Exception)
                {
                    Value = "A";
                }
            }
            else if (column.DataType == typeof(Int16))
            {
                try
                {
                    Value = Convert.ToInt16(column.DefaultValue);
                }
                catch (Exception)
                {
                    Value = 0;
                }
            }
            else if (column.DataType == typeof(Int32))
            {
                try
                {
                    Value = Convert.ToInt32(column.DefaultValue);
                }
                catch (Exception)
                {
                    Value = 0;
                }
            }
            else if (column.DataType == typeof(Int64))
            {
                try
                {
                    Value = Convert.ToInt64(column.DefaultValue);
                }
                catch (Exception)
                {
                    Value = 0;
                }
            }
            else if (column.DataType == typeof(double))
            {
                try
                {
                    Value = Convert.ToDouble(column.DefaultValue);
                }
                catch (Exception)
                {
                    Value = 0.0;
                }
            }
            else if (column.DataType == typeof(decimal))
            {
                try
                {
                    Value = Convert.ToDecimal(column.DefaultValue);
                }
                catch (Exception)
                {
                    Value = 0.0;
                }
            }
            else if (column.DataType == typeof(DateTime))
            {
                try
                {
                    Value = Convert.ToDateTime(column.DefaultValue);
                }
                catch (Exception)
                {
                    Value = DateTime.Now;
                }
            }
            else if (column.DataType == typeof(Single))
            {
                try
                {
                    Value = Convert.ToSingle(column.DefaultValue);
                }
                catch (Exception)
                {
                    Value = 0;
                }
            }
            else if (column.DataType == typeof(byte))
            {
                try
                {
                    Value = Convert.ToByte(column.DefaultValue);
                }
                catch (Exception)
                {
                    Value = null;
                }
            }
            else if (column.DataType == typeof(bool))
            {
                try
                {
                    Value = Convert.ToBoolean(column.DefaultValue);
                }
                catch (Exception)
                {
                    Value = false;
                }
            }
            else if (column.DataType == typeof(Guid))
            {
                try
                {
                    Value = Guid.Parse(column.DefaultValue.ToString());
                }
                catch (Exception)
                {
                    Value = Guid.NewGuid();
                }
            }
            return Value;
        }

        private Double GenerateRandomNumber(int index, bool IsSequenceFlag)
        {
            Double GeneratedValue = 0;
            if (IsSequenceFlag)
            {
                GeneratedValue = index + 1;
            }
            else
            {
                Random random = new Random();
                int Range = random.Next(500, 100000) * (index + 7);
                GeneratedValue = Range / 293;
            }
            return GeneratedValue;
        }

        private string RandomString(int MaxLength, int index)
        {
            string Value = "Data";
            List<string> quoteList = GetStringRange(MaxLength);
            Random random = new Random();
            if (quoteList != null)
            {
                int ActualIndex = 0;
                if (index > 10)
                    ActualIndex = index % 10;
                Value = quoteList.ElementAt(ActualIndex);
            }

            return Value;
        }

        private List<string> GetStringRange(int Size)
        {
            List<string> quoteList = null;
            if (Size <= 5)
            {
                if (this.RandomDataList.FirstOrDefault(x => x.Key == 5).Value != null)
                    quoteList = this.RandomDataList.FirstOrDefault(x => x.Key == 5).Value;
            }
            else if (Size <= 10)
            {
                if (this.RandomDataList.FirstOrDefault(x => x.Key == 10).Value != null)
                    quoteList = this.RandomDataList.FirstOrDefault(x => x.Key == 10).Value;
            }

            else if (Size <= 20)
            {
                if (this.RandomDataList.FirstOrDefault(x => x.Key == 20).Value != null)
                    quoteList = this.RandomDataList.FirstOrDefault(x => x.Key == 20).Value;
            }

            else if (Size <= 50)
            {
                if (this.RandomDataList.FirstOrDefault(x => x.Key == 30).Value != null)
                    quoteList = this.RandomDataList.FirstOrDefault(x => x.Key == 30).Value;
            }
            else
            {
                if (this.RandomDataList.FirstOrDefault(x => x.Key == -1).Value != null)
                    quoteList = this.RandomDataList.FirstOrDefault(x => x.Key == -1).Value;
            }
            return quoteList;
        }

        public Type GetSqlMappedType(string Value)
        {
            Type type = null;
            var AnyValue = (KeyValuePair<string, TypeService.MappedData>)(TypeService.allSqlTypes).FirstOrDefault(x => x.Key == Value);
            if (AnyValue.Value != null)
                type = AnyValue.Value.type;
            return type;
        }

        public DataTable GenerateValuesAsync(DataTable Table, int RowsCount)
        {
            int TableIndex = 0;
            while (TableIndex < RowsCount)
            {
                DataRow Row = Table.NewRow();
                int ColumnCount = Table.Columns.Count;
                RandomTypeValues randomTypeValues = null;
                Random R = new Random();
                List<string> listData = new List<string>();
                randomTypeValues = new RandomTypeValues();
                int Index = 0;
                while (Index < ColumnCount)
                {
                    if (Table.Columns[Index].DataType == typeof(string))
                        Row[Index] = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Substring(0, 8);
                    else if (Table.Columns[Index].DataType == typeof(int) ||
                        Table.Columns[Index].DataType == typeof(Int16) ||
                        Table.Columns[Index].DataType == typeof(Int32) ||
                        Table.Columns[Index].DataType == typeof(Int64))
                        Row[Index] = TableIndex + 1;
                    else if (Table.Columns[Index].DataType == typeof(float) ||
                        Table.Columns[Index].DataType == typeof(float) ||
                        Table.Columns[Index].DataType == typeof(decimal) ||
                        Table.Columns[Index].DataType == typeof(Single))
                        Row[Index] = Convert.ToDouble(((long)R.Next(0, 100000) * (long)R.Next(0, 100000)).ToString().PadLeft(10, '0'));
                    else if (Table.Columns[Index].DataType == typeof(Boolean))
                        Row[Index] = true;
                    else if (Table.Columns[Index].DataType == typeof(DateTime))
                        Row[Index] = System.DateTime.Now.AddDays(TableIndex + 1);
                    Index++;
                }
                Table.Rows.Add(Row);
                TableIndex++;
            }
            return Table;
        }

        public Boolean IsLengthRequired(string Value, out string SqlDataType)
        {
            Boolean isLengthRequired = false;
            SqlDataType = "varchar";
            var AnyValue = (KeyValuePair<string, TypeService.MappedData>)(TypeService.allSqlTypes).FirstOrDefault(x => x.Key == Value);
            if (AnyValue.Value != null)
            {
                isLengthRequired = AnyValue.Value.IsVariableLength;
                SqlDataType = AnyValue.Value.DbType;
            }
            return isLengthRequired;
        }
    }
}

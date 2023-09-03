using ModalLayer.Modal;
using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Code
{
    public class Publish : IPublish<Publish>
    {
        public (string, string) PublishAsSqlFile(List<SchemaDataResult> schemaDataResults, string FilePath)
        {
            string FileFullPath = "";
            FileStream fs = null;
            try
            {
                string FileName = System.IO.Path.GetRandomFileName().Replace(".", "") + ".sql";
                if (!Directory.Exists(FilePath))
                    Directory.CreateDirectory(FilePath);
                FileFullPath = Path.Combine(FilePath, FileName);

                fs = File.Open(FileFullPath, FileMode.OpenOrCreate);
                using (StreamWriter SW = new StreamWriter(fs))
                {
                    foreach (SchemaDataResult SqlData in schemaDataResults)
                    {
                        if (!string.IsNullOrEmpty(SqlData.Schema))
                        {
                            SW.WriteLine(SqlData.Schema);
                            SW.WriteLine("GO");
                        }
                        if (!string.IsNullOrEmpty(SqlData.Schema))
                        {
                            SW.WriteLine(SqlData.ProcedureSchema);
                            SW.WriteLine("GO");
                        }
                        if (!string.IsNullOrEmpty(SqlData.Schema))
                        {
                            SW.WriteLine(SqlData.InsertQuery);
                            SW.WriteLine("GO");
                        }
                    }
                    SW.Close();
                }
                return (FileFullPath, FileName);
            }
            catch (Exception ex)
            {
                if (fs != null)
                {
                    fs.Close();
                    fs = null;
                }
                throw ex;
            }
        }
    }
}

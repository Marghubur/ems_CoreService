using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace OnlineDataBuilder.ContextHandler
{
    public class PathLocator
    {
        public static string GetProjectDirectoryLocation(string FolderName = null)
        {
            string path = Assembly.GetExecutingAssembly().Location;
            if (path != null)
            {
                var locs = path.Split(@"bin\");
                if (locs != null && locs.Length > 0)
                    if (FolderName != null)
                        path = Path.Combine(locs[0], FolderName);
                    else
                        path = locs[0];
            }
            return path;
        }
    }
}

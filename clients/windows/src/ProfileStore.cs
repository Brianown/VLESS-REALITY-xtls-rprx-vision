using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace XrayLite
{
    public sealed class ProfileStore
    {
        private readonly string path;

        public ProfileStore()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string directory = Path.Combine(appData, "XrayLite");
            Directory.CreateDirectory(directory);
            path = Path.Combine(directory, "profiles.json");
        }

        public List<VlessProfile> Load()
        {
            try
            {
                if (!File.Exists(path))
                    return new List<VlessProfile>();
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                List<VlessProfile> list = serializer.Deserialize<List<VlessProfile>>(File.ReadAllText(path));
                return list ?? new List<VlessProfile>();
            }
            catch (Exception)
            {
                return new List<VlessProfile>();
            }
        }

        public void Save(List<VlessProfile> profiles)
        {
            try
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                File.WriteAllText(path, serializer.Serialize(profiles));
            }
            catch (Exception)
            {
            }
        }
    }
}

using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms; // for Application.UserAppDataPath
using System.Xml;
using System.Xml.Serialization;
using System.Reflection;

namespace SmallHorse.Spider.Craigslist
{
    /// <summary>
    /// Name-Value Pair
    /// </summary>
    public class NameValue
    {
        public string Name;
        public string Value;
        public NameValue() { Name = null; Value = null; }
        public NameValue(string name, string value) { Name = name; Value = value; }
    }

    public class DefaultSettings
    {
        public static NameValue[] Sites = new NameValue[] {
            new NameValue("lafayette(普渡), IN", "http://tippecanoe.craigslist.org/"),
            new NameValue("corvallis/albany, OR", "http://corvallis.craigslist.org/"),
            new NameValue("east oregon, OR", "http://eastoregon.craigslist.org/"),
            new NameValue("eugene, OR", "http://eugene.craigslist.org/"),
            new NameValue("medford-ashland-klamath, OR", "http://medford.craigslist.org/"),
            new NameValue("oregon coast, OR", "http://oregoncoast.craigslist.org/"),
            new NameValue("portland, OR", "http://portland.craigslist.org/"),
            new NameValue("roseburg, OR", "http://roseburg.craigslist.org/"),
            new NameValue("salem, OR", "http://salem.craigslist.org/"),

            new NameValue("SF bay area, CA", "http://sfbay.craigslist.org/"),
            new NameValue("bakersfield, CA", "http://bakersfield.craigslist.org/"),
            new NameValue("chico, CA", "http://chico.craigslist.org/"),
            new NameValue("fresno, CA", "http://fresno.craigslist.org/"),
            new NameValue("gold country, CA", "http://goldcountry.craigslist.org/"),
            new NameValue("humboldt county, CA", "http://humboldt.craigslist.org/"),
            new NameValue("imperial county, CA", "http://imperial.craigslist.org/"),
            new NameValue("inland empire, CA", "http://inlandempire.craigslist.org/"),
            new NameValue("los angeles, CA", "http://losangeles.craigslist.org/"),
            new NameValue("mendocino county, CA", "http://mendocino.craigslist.org/"),
            new NameValue("merced, CA", "http://merced.craigslist.org/"),
            new NameValue("modesto, CA", "http://modesto.craigslist.org/"),
            new NameValue("monterey bay, CA", "http://monterey.craigslist.org/"),
            new NameValue("orange county, CA", "http://orangecounty.craigslist.org/"),
            new NameValue("palm springs, CA", "http://palmsprings.craigslist.org/"),
            new NameValue("redding, CA", "http://redding.craigslist.org/"),
            new NameValue("reno / tahoe, CA", "http://reno.craigslist.org/"),
            new NameValue("sacramento, CA", "http://sacramento.craigslist.org/"),
            new NameValue("san diego, CA", "http://sandiego.craigslist.org/"),
            new NameValue("san luis obispo, CA", "http://slo.craigslist.org/"),
            new NameValue("santa barbara, CA", "http://santabarbara.craigslist.org/"),
            new NameValue("stockton, CA", "http://stockton.craigslist.org/"),
            new NameValue("ventura county, CA", "http://ventura.craigslist.org/"),
            new NameValue("visalia-tulare, CA", "http://visalia.craigslist.org/"),
            new NameValue("yuba-sutter, CA", "http://yubasutter.craigslist.org/"),
        };

        public static NameValue[] Categories = new NameValue[] {
            new NameValue("all for sale", "sss"),
            new NameValue("for sale - art & crafts", "art"),
            new NameValue("for sale - auto parts", "pts"),
            new NameValue("for sale - baby & kid stuff", "bab"),
            new NameValue("for sale - barter", "bar"),
            new NameValue("for sale - bicycles", "bik"),
            new NameValue("for sale - boats", "boa"),
            new NameValue("for sale - books", "bks"),
            new NameValue("for sale - business", "bfs"),
            new NameValue("for sale - cars & trucks - all", "cta"),
            new NameValue("for sale - cars & trucks - by dealer", "ctd"),
            new NameValue("for sale - cars & trucks - by owner", "cto"),
            new NameValue("for sale - cds / dvds / vhs", "emd"),
            new NameValue("for sale - clothing", "clo"),
            new NameValue("for sale - collectibles", "clt"),
            new NameValue("for sale - computers & tech", "sys"),
            new NameValue("for sale - electronics", "ele"),
            new NameValue("for sale - farm & garden", "grd"),
            new NameValue("for sale - free stuff", "zip"),
            new NameValue("for sale - furniture - all", "fua"),
            new NameValue("for sale - furniture - by dealer", "fud"),
            new NameValue("for sale - furniture - by owner", "fuo"),
            new NameValue("for sale - games & toys", "tag"),
            new NameValue("for sale - garage sales", "gms"),
            new NameValue("for sale - general", "for"),
            new NameValue("for sale - household", "hsh"),
            new NameValue("for sale - items wanted", "wan"),
            new NameValue("for sale - jewelry", "jwl"),
            new NameValue("for sale - materials", "mat"),
            new NameValue("for sale - motorcycles/scooters", "mcy"),
            new NameValue("for sale - musical instruments", "msg"),
            new NameValue("for sale - photo/video", "pho"),
            new NameValue("for sale - recreational vehicles", "rvs"),
            new NameValue("for sale - sporting goods", "spo"),
            new NameValue("for sale - tickets", "tix"),
            new NameValue("for sale - tools", "tls"),
            new NameValue("all community", "ccc"),
            new NameValue("all event", "eee"),
            new NameValue("all gigs", "ggg"),
            new NameValue("all housing", "hhh"),
            new NameValue("all jobs", "jjj"),
            new NameValue("all personals", "ppp"),
            new NameValue("all resume", "res"),
            new NameValue("all services offered", "bbb"),
            new NameValue("rooms for rent & shares available", "roo"),
            new NameValue("vacation rentals", "vac"),
        };
    }

    /// <summary>
    /// Persistent settings
    /// </summary>
    /// <remarks>Email password is stored securely on disk using DPAPI.</remarks>
    public sealed class Settings
    {
        const int currentVersion = 1;

        public int FileVersion = 0;
        public Email Email = new Email();
        public Filter Filter = new Filter();
        public List<NameValue> Sites = new List<NameValue>();
        public List<NameValue> Categories = new List<NameValue>();

        // Begin singleton pattern
        private static readonly Settings instance = new Settings();
        static Settings() { }   // do not mark as beforefieldinit
        private Settings() { }
        public static Settings Instance { get { return instance; } }
        // End singleton pattern

        /// <summary>
        /// Save settings to file
        /// </summary>
        public void Save()
        {
            FileVersion = currentVersion;

            // Encrypt the password
            string pw = Email.Password;
            Email.Password = DPAPI.Encrypt(DPAPI.KeyType.UserKey, pw);

            SaveToXml<Settings>(GetPath("settings"), this);

            // Restore password
            Email.Password = pw;
        }

        /// <summary>
        /// Load settings from file
        /// </summary>
        public void Load()
        {
            // since we cannot change the singleton instance,
            // load a new instance and copy the properties
            Settings newSettings = LoadFromXml<Settings>(GetPath("settings"), null);
            if (newSettings != null && newSettings.FileVersion == currentVersion)
            {
                // copy all the properties
                Email = newSettings.Email;
                Filter = newSettings.Filter;
                Sites = newSettings.Sites;
                Categories = newSettings.Categories;

                // Restore password
                Email.Password = DPAPI.Decrypt(Email.Password);
            }

            if (Sites.Count == 0)
            {
                Sites.AddRange(DefaultSettings.Sites);
            }
            if (Categories.Count == 0)
            {
                Categories.AddRange(DefaultSettings.Categories);
            }
        }

        public string GetSiteValue(string site)
        {
            NameValue found = Sites.Find(delegate(NameValue i) { return i.Name == site; });
            if (found != null)
                return found.Value;
            return "";
        }

        public string GetCategoryValue(string cat)
        {
            NameValue found = Categories.Find(delegate(NameValue i) { return i.Name == cat; });
            if (found != null)
                return found.Value;
            return "";
        }

        #region Xml Utilities

        static public string GetPath(string baseName)
        {
            string path = string.Format("{0}\\{1}.XML",
                Application.UserAppDataPath, baseName);
            return path;
        }

        /// <summary>
        /// Save object to Xml file
        /// </summary>
        static public bool SaveToXml<T>(string path, T obj)
        {
            bool result = true;

            try
            {
                StreamWriter sw = new StreamWriter(path);
                new XmlSerializer(obj.GetType()).Serialize(sw, obj);
                sw.Close();
            }
            catch (Exception)
            {
                result = false;
                //MessageBox.Show("Cannot save file\n" + path + "\n", // + ex.ToString(),
                //"Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            return result;
        }

        /// <summary>
        /// Load object from Xml file
        /// </summary>
        static public T LoadFromXml<T>(string path, T defaultObj)
        {
            T obj = defaultObj;

            if (File.Exists(path))
            {
                try
                {
                    StreamReader sr = new StreamReader(path);
                    obj = (T)new XmlSerializer(typeof(T)).Deserialize(sr);
                    sr.Close();
                }
                catch (Exception)
                {
                    //MessageBox.Show("Cannot load file\n" + path + "\n", // + ex.ToString(),
                    //"Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }

            return obj;
        }

        #endregion
    }
}

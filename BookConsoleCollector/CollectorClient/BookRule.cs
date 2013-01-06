using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using Voodoo;
using Voodoo.IO;
namespace CollectorClient
{
    public class BookRule
    {

        public string Name { get; set; }

        public string ConnStr { get; set; }

        public string TargetSiteName { get; set; }

        public string TargetSiteUrl { get; set; }

        public string SrcSite { get; set; }

        public string SiteName { get; set; }

        public string SiteDomain { get; set; }

        public string CharSet { get; set; }

        public string ListUrl { get; set; }

        public string ListUrlNextRule { get; set; }

        public string SearchUrl { get; set; }

        public bool IsPostSearch { get; set; }

        public string ListRule { get; set; }

        public string SearchRule { get; set; }

        public string InfoRule { get; set; }

        public int FaceWidth { get; set; }

        public int FaceHeight { get; set; }

        public string ChapterListUrlRule { get; set; }

        public string ChapterListRule { get; set; }

        public string ContentRule { get; set; }

        public string NextContentRule { get; set; }

        public string NextChapterUrlRule { get; set; }

        public string ImageCheckRule { get; set; }

        #region 获取所有规则
        /// <summary>
        /// 获取所有规则
        /// </summary>
        /// <returns></returns>
        public static List<BookRule> GetAll()
        {
            var result = new List<BookRule>();

            DirectoryInfo dir = new DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory + "Config");
            foreach (var file in dir.GetFiles("*.xml"))
            {
                var r = (BookRule)XML.DeSerialize(typeof(BookRule), Voodoo.IO.File.Read(file.FullName));
                result.Add(r);
            }
            return result;
        }
        #endregion

        #region 保存当前规则
        /// <summary>
        /// 保存当前规则
        /// </summary>
        public void Save()
        {
            string path = System.AppDomain.CurrentDomain.BaseDirectory + "Config\\" + this.Name + ".xml";
            XML.SaveSerialize(this, path);
        } 
        #endregion
    }
}

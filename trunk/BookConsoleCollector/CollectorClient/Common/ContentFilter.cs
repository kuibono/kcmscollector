using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Voodoo;
using Voodoo.Cache;
using Voodoo.IO;
using System.Text.RegularExpressions;
namespace CollectorClient.Common
{
    public class ContentFilter
    {
        #region 过滤
        /// <summary>
        /// 过滤
        /// </summary>
        /// <param name="Content"></param>
        /// <returns></returns>
        public string Filter(string Content)
        {

            //去除特殊字符

            Content = RegexReplace(Content, "[§№☆★○●◎◇◆□■△▲※→←↑↓〓＃＆＠＼＾＿￣―♂♀‘’々～‖＂〃〔〕〈〉《》「」『』〖〗【】（）［｛｝°＄￡￥‰％℃¤￠]{1,}?", "");
            Content = RegexReplace(Content, "[~@#$%^*()_=\\-\\+\\[\\]]{1,}?", "");



            //全角转半角
            Content = Content.ToDBC();

            //英文转小写
            Content = Content.ToLower();

            //删除脚本
            Content = RegexReplace(Content, "<script [\\s\\S]*?</script>", "");

            //删除链接
            Content = RegexReplace(Content, "<a [\\s\\S]*?</a>", "");

            //删除不需要的HTML
            Content = RegexReplace(Content, "<[/]?table>", "");
            Content = RegexReplace(Content, "<[/]?tr>", "");
            Content = RegexReplace(Content, "<[/]?div>", "");
            Content = RegexReplace(Content, "<[/]?td>", "");
            Content = RegexReplace(Content, "<[/]?span>", "");
            Content = RegexReplace(Content, "<[/]?font>", "");
            Content = RegexReplace(Content, "<[/]?p>", "");
            Content = RegexReplace(Content, "<[/]?cc>", "");

            Content = RegexReplace(Content, "<strong>.*?简单介绍:</strong>", "");

            //删除网址
            Content = RegexReplace(Content, "http://", "");
            Content = RegexReplace(Content, "https://", "");
            Content = RegexReplace(Content, "[\\\\\\w\\./。]{3,20}\\.[com|net|org|cn|co|info|us|cc|xxx|tv|ws|hk|tw]+", "");
            //Content = RegexReplace(Content, "", "");

            //根据预先指定的规则进行替换
            var Filter_List =GetFilter();
            foreach (string f in Filter_List)
            {
                string[] pa = f.Split('|');

                if (pa[0].Length == 0)
                {
                    continue;
                }
                if (pa.Length > 1)
                {
                    //Content = Regex.Replace(Content, pa[0], pa[1], RegexOptions.None);
                    //Content = new Regex(pa[0]).Replace(Content, pa[1], 100);
                    Content = RegexReplace(Content, pa[0], pa[1]);
                }
                else
                {
                    //Content = Regex.Replace(Content, pa[0], "", RegexOptions.None);
                    //Content = new Regex(pa[0]).Replace(Content,"", 100);
                    Content = RegexReplace(Content, pa[0], "");
                }

            }


            return Content;
        }
        #endregion 过滤

        #region 正则替换
        public static string RegexReplace(string Content, string parrten, string newvalue)
        {
            while (Regex.IsMatch(Content, parrten))
            {
                Content = Regex.Replace(Content, parrten, newvalue, RegexOptions.IgnoreCase);
            }
            return Content;
        }
        #endregion

        #region 过滤规则
        /// <summary>
        /// 获取过滤规则
        /// </summary>
        /// <returns></returns>
        public static List<string> GetFilter()
        {
            if (Cache.GetCache("Filter") == null)
            {

                Cache.SetCache("Filter", File.Read(System.AppDomain.CurrentDomain.BaseDirectory + "Setting\\Filter.txt"));
            }
            string[] fi = Cache.GetCache("Filter").ToString().Split('\n');
            List<string> result = new List<string>();
            foreach (string f in fi)
            {
                result.Add(f.Replace("\r", ""));
            }
            return result;
        }
        #endregion

        public static List<string> GetBlackList()
        {
            return Voodoo.IO.File.Read(WS.BaseDirectory + "Config\\blacklist.txt").Split('\n').ToList();
        }
    }
}

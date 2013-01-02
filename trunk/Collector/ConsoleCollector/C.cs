using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Voodoo;
using Voodoo.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace ConsoleCollector
{
    public class C
    {
        #region 快捷命令
        private void w(string str)
        {
            Console.WriteLine(str);
        }

        private void red()
        {
            Console.ForegroundColor = ConsoleColor.Red;
        }

        private void green()
        {
            Console.ForegroundColor = ConsoleColor.Green;
        }

        private void yellow()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
        }

        private void blue()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
        }

        private void white()
        {
            Console.ForegroundColor = ConsoleColor.White;
        }
        #endregion

        /// <summary>
        /// 当前目录
        /// </summary>
        public string CurrentFolder = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;

        /// <summary>
        /// 获取采集规则
        /// </summary>
        /// <returns></returns>
        public List<MovieRule> GetRules()
        {
            List<MovieRule> rules = new List<MovieRule>();

            DirectoryInfo dir = new DirectoryInfo(CurrentFolder + "Config");
            foreach (var file in dir.GetFiles())
            {
                rules.Add((MovieRule)Voodoo.IO.XML.DeSerialize(typeof(MovieRule), Voodoo.IO.File.Read(file.FullName)));
            }
            return rules;
        }

        public void Main()
        {
            w("获取规则");

            List<MovieRule> rules = GetRules();
            foreach (MovieRule CurrentRule in rules)
            {
                try
                {
                    OpenList(CurrentRule, CurrentRule.ListPageUrl);
                }
                catch (Exception ex)
                {
                    w(ex.Message + "\n");
                }
            }


            Main();
        }

        public void OpenList(MovieRule r, string url)
        {
            w(string.Format("打开列表：{0}", url));
            string html = Url.GetHtml(url, r.Encoding);

            Match match_list = html.GetMatchGroup(r.ListInfoRule);

            while (match_list.Success)
            {
                green();
                w(string.Format("\n\n列表中获得电影:{0}", match_list.Groups["title"].Value));
                MovieInfo m = new MovieInfo();
                m.ClassName = match_list.Groups["class"].Value;
                m.Title = match_list.Groups["title"].Value;
                m.Location = match_list.Groups["location"].Value;
                m.PublicYear = match_list.Groups["publicyear"].Value;
                m.Actors = match_list.Groups["actors"].Value;
                m.Director = match_list.Groups["director"].Value;
                m.Info = match_list.Groups["intro"].Value;
                m.Intro = match_list.Groups["intro"].Value;
                m.FaceImage = match_list.Groups["image"].Value.IsNullOrEmpty() ? "" : match_list.Groups["image"].Value.AppendToDomain(url);

                GetMovieInfo(r, m, match_list.Groups["url"].Value.AppendToDomain(url));
                match_list = match_list.NextMatch();
            }

            Match match_next = html.GetMatchGroup(r.NextListRule);
            if (match_next.Success)
            {
                OpenList(r, match_next.Groups["key"].Value.AppendToDomain(url));
            }

        }

        #region 获取电影
        public void GetMovieInfo(MovieRule r, MovieInfo m, string InfoUrl)
        {
            red();
            w(string.Format("打开电影《{0}》的信息页:{1}", m.Title, InfoUrl));
            string html = Url.GetHtml(InfoUrl, r.Encoding);

            using (kuaiboyingshiEntities ent = new kuaiboyingshiEntities())
            {
                string short_title = m.Title.TrimEnd('.');
                var movies = (from l in ent.MovieInfo where l.Title.Contains(short_title) select l).ToList();
                Match match = html.GetMatchGroup(r.InfoRule);
                if (movies.Count == 0)
                {


                    if (!match.Success)
                    {
                        w("这个电影无法和规则匹配\n ");
                        return;
                    }

                    

                    m.ClassName = m.ClassName.IsNull(match.Groups["class"].Value).IsNull("其他");
                    m.Title = match.Groups["title"].Value;
                    m.Location = m.Title.IsNull(match.Groups["location"].Value);
                    m.PublicYear = m.PublicYear.IsNull(match.Groups["publicyear"].Value).IsNull("2012"); ;
                    m.Actors = m.Actors.IsNull(match.Groups["actors"].Value).TrimHTML();
                    m.Director = m.Director.IsNull(match.Groups["director"].Value);
                    m.Info = m.Info.IsNull(match.Groups["intro"].Value).Replace(r.SiteName,"") ;
                    m.Intro = m.Intro.IsNull(match.Groups["intro"].Value).Replace(r.SiteName, "");

                    m.FaceImage = m.FaceImage.IsNull(match.Groups["image"].Value.AppendToDomain(InfoUrl));

                    m.Status = 0;
                    m.ClassID = GetClassByName(m.ClassName).ID;
                    m.ClickCount = 0;
                    m.DayClick = 0;
                    m.UpdateTime = DateTime.Now;
                    m.InsertTime = DateTime.Now;
                    m.IsMove = false;
                    m.LastClickTime = DateTime.Now;
                    m.LastDramaID = 0;
                    m.LastDramaTitle = "";
                    m.MonthClick = 0;
                    m.ReplyCount = 0;
                    m.ScoreAvg = 0;
                    m.ScoreTime = 0;
                    m.StandardTitle = StandTitle(m.Title);
                    m.Enable = true;
                    if ((from l in ent.MovieInfo where l.Title == m.Title select l).Count() > 0)
                    {
                        w(string.Format("电影：{0}  类型：{1}  地区：{2}  年代：{3}  演员：{4}  导演：{5}", m.Title, m.ClassName, m.Location, m.PublicYear, m.Actors, m.Director));
                        ent.AddToMovieInfo(m);

                        w("保存电影信息...");
                        ent.SaveChanges();

                        if (!m.FaceImage.IsNullOrEmpty())
                        {
                            w("设置封面");

                            SaveFace(m.FaceImage, m.id);
                            m.FaceImage = string.Format("/u/MovieFace/{0}.jpg", m.id);
                            w("保存封面...");
                            ent.SaveChanges();
                        }
                    }

                }
                else
                {
                    string imageUrl = m.FaceImage.IsNull(match.Groups["image"].Value.AppendToDomain(InfoUrl));
                    m = movies.FirstOrDefault();
                    if (m.FaceImage.IsNullOrEmpty())
                    {
                        SaveFace(imageUrl, m.id);
                    }
                }

                


                //获取百度和快播区域并且开始采集

                #region 百度区域
                yellow();
                Match m_baiduarea = html.GetMatchGroup(r.BaiduAreaRule);
                if (m_baiduarea.Success && !r.BaiduAreaRule.IsNullOrEmpty())
                {
                    w("分析百度区域");
                    Match m_drama = m_baiduarea.Groups["key"].Value.GetMatchGroup(r.BaiduDramaRule);
                    while (m_drama.Success)
                    {
                        string title = m_drama.Groups["title"].Value;
                        string playurl = m_drama.Groups["playurl"].Value;
                        string url = m_drama.Groups["url"].Value;

                        #region 打开播放页面处理
                        if (url.IsNullOrEmpty())
                        {
                            CollectBaiduMovie(m, r, playurl.AppendToDomain(InfoUrl), title);
                            if (!r.SourceBaiduRule.IsNullOrEmpty())
                            {
                                break;
                            }
                        }
                        #endregion

                        else
                        {
                            var dramas = (from l in ent.MovieUrlBaidu where l.MovieID == m.id && l.Title == title select l);
                            if (dramas.Count() == 0)
                            {
                                w(string.Format("在信息页面得到地址:{0}", url));
                                ent.AddToMovieUrlBaidu(new MovieUrlBaidu()
                                {
                                    Enable = true,
                                    MovieID = m.id,
                                    MovieTitle = m.Title,
                                    Title = title,
                                    UpdateTime = DateTime.Now,
                                    Url = url
                                });
                                m.UpdateTime = DateTime.Now;
                                m.LastDramaTitle = title;
                                w("保存");
                                ent.SaveChanges();
                            }
                        }

                        m_drama = m_drama.NextMatch();
                    }
                }
                #endregion

                #region 快播区域
                Match m_kuaiboarea = html.GetMatchGroup(r.KuaibAreaRule);
                if (m_kuaiboarea.Success && !r.KuaibAreaRule.IsNullOrEmpty())
                {
                    w("分析快播区域");
                    Match m_drama = m_kuaiboarea.Groups["key"].Value.GetMatchGroup(r.KuaibDramaRule);
                    while (m_drama.Success)
                    {
                        string title = m_drama.Groups["title"].Value.IsNull("全集");
                        string playurl = m_drama.Groups["playurl"].Value;
                        string url = m_drama.Groups["url"].Value;

                        #region 打开播放页面处理
                        if (url.IsNullOrEmpty())
                        {
                            CollectKuaiboMovie(m, r, playurl.AppendToDomain(InfoUrl), title);
                            if (!r.SourceKuaibRule.IsNullOrEmpty())
                            {
                                break;
                            }
                        }
                        #endregion

                        else
                        {
                            var dramas = (from l in ent.MovieUrlKuaib where l.MovieID == m.id && l.Title == title select l);
                            if (dramas.Count() == 0)
                            {
                                w(string.Format("在信息页面得到地址:{0}", url));
                                ent.AddToMovieUrlKuaib(new MovieUrlKuaib()
                                {
                                    Enable = true,
                                    MovieID = m.id,
                                    MovieTitle = m.Title,
                                    Title = title,
                                    UpdateTime = DateTime.Now,
                                    Url = url
                                });
                                m.UpdateTime = DateTime.Now;
                                m.LastDramaTitle = title;

                                w("保存");
                                ent.SaveChanges();
                            }
                        }

                        m_drama = m_drama.NextMatch();
                    }
                }
                #endregion
            }

        }
        #endregion

        #region 快播剧集
        public void CollectKuaiboMovie(MovieInfo m, MovieRule r, string url, string Title)
        {
            blue();

            w(string.Format("打开《{0}》-({1})快播播放页面", m.Title, Title));
            string html = Url.GetHtml(url, r.Encoding);
            Match match = html.GetMatchGroup(r.KuaibDramaRule);
            if (match.Success && !r.KuaibDramaRule.IsNullOrEmpty())
            {
                w("得到资源地址");
                MovieUrlKuaib drama = new MovieUrlKuaib();
                drama.Enable = true;
                drama.MovieID = m.id;
                drama.MovieTitle = m.Title;
                drama.Title = Title.IsNull(match.Groups["title"].Value);
                drama.UpdateTime = DateTime.Now;
                drama.Url = match.Groups["url"].Value;

                using (kuaiboyingshiEntities ent = new kuaiboyingshiEntities())
                {
                    var dramas = (from l in ent.MovieUrlKuaib where l.MovieID == m.id && l.MovieTitle == drama.Title select l).ToList();
                    if (dramas.Count == 0)
                    {
                        ent.AddToMovieUrlKuaib(drama);
                        ent.SaveChanges();
                    }
                }
            }
            else
            {
                Match m_source = html.GetMatchGroup(r.DramaPageKuaibRule);
                if (m_source.Success)
                {
                    CollectSources(r, m_source.Groups["source"].Value.AppendToDomain(url), m);
                }
            }
        }
        #endregion

        #region 百度剧集
        public void CollectBaiduMovie(MovieInfo m, MovieRule r, string url, string Title)
        {
            blue();

            w(string.Format("打开《{0}》-({1})百度影音播放页面", m.Title, Title));
            string html = Url.GetHtml(url, r.Encoding);
            Match match = html.GetMatchGroup(r.BaiduDramaRule);
            if (match.Success && !r.BaiduDramaRule.IsNullOrEmpty() && !match.Groups["url"].Value.IsNullOrEmpty())
            {
                w("得到资源地址");
                MovieUrlBaidu drama = new MovieUrlBaidu();
                drama.Enable = true;
                drama.MovieID = m.id;
                drama.MovieTitle = m.Title;
                drama.Title = Title.IsNull(match.Groups["title"].Value);
                drama.UpdateTime = DateTime.Now;
                drama.Url = match.Groups["url"].Value;

                using (kuaiboyingshiEntities ent = new kuaiboyingshiEntities())
                {

                    var dramas = (from l in ent.MovieUrlBaidu where l.MovieID == m.id && l.MovieTitle == drama.Title select l).ToList();
                    if (dramas.Count == 0)
                    {
                        ent.AddToMovieUrlBaidu(drama);
                        ent.SaveChanges();
                    }
                }

            }
            else
            {
                Match m_source = html.GetMatchGroup(r.DramaPageBaiduRule);
                if (m_source.Success)
                {
                    CollectSources(r, m_source.Groups["source"].Value.AppendToDomain(url), m);
                }
            }
        }
        #endregion

        public void CollectSources(MovieRule r, string url, MovieInfo m)
        {
            white();
            w("打开资源文件");
            string html = Url.GetHtml(url, r.Encoding);

            w("分析资源内容");
            var dramas = CollectDramas(html, m.id);
            dramas = dramas.OrderBy(p => p.Title).ToList();

            w(string.Format("得到{0}个资源", dramas.Count));
            using (kuaiboyingshiEntities ent = new kuaiboyingshiEntities())
            {
                foreach (var drama in dramas)
                {
                    if (drama.Type == "baidu")
                    {
                        ent.AddToMovieUrlBaidu(new MovieUrlBaidu()
                        {
                            Enable = true,
                            MovieID = m.id,
                            MovieTitle = m.Title,
                            Title = drama.Title,
                            UpdateTime = DateTime.Now,
                            Url = drama.Url
                        });

                    }
                    else
                    {
                        ent.AddToMovieUrlKuaib(new MovieUrlKuaib()
                        {
                            Enable = true,
                            MovieID = m.id,
                            MovieTitle = m.Title,
                            Title = drama.Title,
                            UpdateTime = DateTime.Now,
                            Url = drama.Url
                        });
                    }
                    m.UpdateTime = DateTime.Now;
                    m.LastDramaTitle = drama.Title;
                    w("保存");
                    ent.SaveChanges();
                }
                w("保存");
                ent.SaveChanges();
            }
        }

        #region 从source文件中分析剧集
        /// <summary>
        /// 从source文件中分析剧集
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public List<Drama> CollectDramas(string source, int Movieid)
        {
            kuaiboyingshiEntities ent = new kuaiboyingshiEntities();

            MovieInfo mv = (from l in ent.MovieInfo where l.id == Movieid select l).FirstOrDefault();
            var baidu = (from l in ent.MovieUrlBaidu where l.MovieID == Movieid select l).ToList();
            var kuaibo = (from l in ent.MovieUrlKuaib where l.MovieID == Movieid select l).ToList();

            var result = new List<Drama>();
            source = source.UrlDecode().AsciiToNative();

            List<string> tmp = new List<string>();
            Match m = new Regex("((bdhd://)|(qvod://)).*?((.rmvb)|(.rm)|(.avi)|(.mp4)|(.asf)|(.wmv))+").Match(source);
            while (m.Success)
            {
                string str = m.Groups["0"].Value;
                tmp.Add(str);
                m = m.NextMatch();
            }

            foreach (string str in tmp)
            {
                string title = str.Split('|')[2].Replace(mv.Title.Replace("(", "").Replace(")", "").Replace("国语", "").Replace("粤语", "").Replace("版", ""), "");
                title = title.ToLower();
                title = Regex.Replace(title, "[a-zA-Z0-9\\.]+\\.(com|net|org|co|cn|us|hk|info|gov)", "");
                title = title.ToLower().Replace(".rmvb", "").Replace(".rm", "").Replace(".avi", "").Replace(".mp4", "").Replace(".asf", "").Replace(".wmv", "").Replace(" ", "").Replace(".", "").Replace("_", "");
                title = title.Replace("[", "").Replace("]", "");
                title = title.Replace("(", "").Replace(")", "").Replace("国语", "").Replace("粤语", "").Replace("dvd", "").Replace("bd", "").Replace("版", "");
                title = title.Replace("-", "");
                if (title.IsNumeric())
                {
                    title = string.Format("第{0}集", title);
                }
                if (title.IsNumeric())
                {
                    title = "全集";
                }



                //string title =str.Split('|')[2].ToLower().Replace(".rmvb", "").Replace(".rm", "").Replace(".avi", "").Replace(".mp4", "").Replace(".asf", "").Replace(".wmv", "").Replace(" ", "").Replace(".", "");
                string type = str.StartsWith("bdhd") ? "baidu" : "qvod";
                if (type == "baidu")
                {
                    if (baidu.Where(p => p.Title == title).Count() > 0)
                    {
                        continue;
                    }
                }
                else
                {
                    if (kuaibo.Where(p => p.Title == title).Count() > 0)
                    {
                        continue;
                    }
                }
                try
                {
                    w(string.Format("得到剧集：{0}", title));
                    result.Add(new Drama()
                    {
                        Title = title,
                        Url = str,
                        Type = type
                    });
                }
                catch { }
            }

            ent.Dispose();

            return result.OrderBy(p => p.Title).ToList();
        }
        #endregion

        #region 根据名称获取类
        /// <summary>
        /// 根据名称获取类
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Class GetClassByName(string name)
        {
            using (kuaiboyingshiEntities ent = new kuaiboyingshiEntities())
            {
                w("根据名称获取类");
                var classes = (from l in ent.Class where l.ClassName == name && l.ModelID == 6 select l).ToList();
                if (classes.Count == 0)
                {
                    Class cls = new Class()
                    {
                        ModelID = 6,
                        Alter = name,
                        AutoAudt = true,
                        ClassName = name,
                        ParentID = 0
                    };
                    ent.AddToClass(cls);
                    w(string.Format("新增类：{0} ID为：{1}", cls.ClassName, cls.ID));
                    return cls;
                }
                else
                {
                    w(string.Format("得到ID：{0}", classes.FirstOrDefault().ID));
                    return classes.FirstOrDefault();
                }
            }
        }
        #endregion

        public string StandTitle(string Title)
        {
            string result = Title;
            result = Regex.Replace(result, "[/]{2,}", "/");
            result = result.Replace(":", "_");
            result = result.Replace(">", "");
            result = result.Replace("<", "");
            result = result.Replace("*", "");
            result = result.Replace("?", "");
            result = result.Replace("|", "_");
            return result;
        }

        /// <summary>
        /// 保存封面
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="id"></param>
        public void SaveFace(string Url, int id)
        {
            try
            {
                Voodoo.Net.Url.DownFile(Url, CurrentFolder + "0.jpg");
                Voodoo.IO.ImageHelper.MakeThumbnail(CurrentFolder + "0.jpg", CurrentFolder + "1.jpg", 120, 142, "Cut");
                Voodoo.Net.Url.UpLoadFile(CurrentFolder + "1.jpg", "http://www.kuaiboyingshi.com/e/api/xmlrpc.aspx?a=savemovieface&id=" + id + "&", false);
            }
            catch
            {

            }
        }
    }
}

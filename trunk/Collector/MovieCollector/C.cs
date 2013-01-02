using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Voodoo;
using Voodoo.Net;
using System.Text.RegularExpressions;

namespace MovieCollector
{
    public class C
    {
        /// <summary>
        /// 当前目录
        /// </summary>
        public string CurrentFolder = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;

        /// <summary>
        /// 获取采集规则
        /// </summary>
        /// <returns></returns>
        public MovieRule GetRule()
        {
            return (MovieRule)Voodoo.IO.XML.DeSerialize(typeof(MovieRule), Voodoo.IO.File.Read(CurrentFolder + "Config/Hanyan.xml"));
        }

        public void Main()
        {
            MovieRule CurrentRule = GetRule();
            OpenList(CurrentRule, CurrentRule.ListPageUrl);
        }

        public void OpenList(MovieRule r, string url)
        {
            string html = Url.GetHtml(url, r.Encoding);

            Match match_list = html.GetMatchGroup(r.ListInfoRule);

            while (match_list.Success)
            {

                MovieInfo m = new MovieInfo();
                m.ClassName = match_list.Groups["class"].Value;
                m.Title = match_list.Groups["title"].Value;
                m.Location = match_list.Groups["location"].Value;
                m.PublicYear = match_list.Groups["publicyear"].Value;
                m.Actors = match_list.Groups["actors"].Value;
                m.Director = match_list.Groups["director"].Value;
                m.Info = match_list.Groups["intro"].Value;
                m.Intro = match_list.Groups["intro"].Value;

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
            string html = Url.GetHtml(InfoUrl, r.Encoding);

            using (kuaiboyingshiEntities ent = new kuaiboyingshiEntities())
            {
                var movies = (from l in ent.MovieInfo where l.Title == m.Title select l).ToList();
                if (movies.Count == 0)
                {

                    Match match = html.GetMatchGroup(r.InfoRule);
                    if (!match.Success)
                    {
                        return;
                    }

                    m.ClassName = m.ClassName.IsNull(match.Groups["class"].Value).IsNull("其他");
                    m.Title = match.Groups["title"].Value;
                    m.Location = m.Title.IsNull(match.Groups["location"].Value);
                    m.PublicYear = m.PublicYear.IsNull(match.Groups["publicyear"].Value).IsNull("2012"); ;
                    m.Actors = m.Actors.IsNull(match.Groups["actors"].Value);
                    m.Director = m.Director.IsNull(match.Groups["director"].Value);
                    m.Info = m.Info.IsNull(match.Groups["intro"].Value);
                    m.Intro = m.Intro.IsNull(match.Groups["intro"].Value);
                    m.Status = 0;
                    m.ClassID = GetClassByName(m.ClassName).ID;
                    m.ClickCount = 0;
                    m.DayClick = 0;
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

                    ent.AddToMovieInfo(m);
                    ent.SaveChanges();

                    SaveFace(match.Groups["image"].Value.AppendToDomain(InfoUrl), m.id);
                    m.FaceImage = string.Format("/u/MovieFace/{0}.jpg",m.id);
                    ent.SaveChanges();
                }
                else
                {
                    m = movies.FirstOrDefault();
                }

                //获取百度和快播区域并且开始采集

                #region 百度区域
                Match m_baiduarea = html.GetMatchGroup(r.BaiduAreaRule);
                if (m_baiduarea.Success && !r.BaiduAreaRule.IsNullOrEmpty())
                {
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
                    Match m_drama = m_kuaiboarea.Groups["key"].Value.GetMatchGroup(r.KuaibAreaRule);
                    while (m_drama.Success)
                    {
                        string title = m_drama.Groups["title"].Value;
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
            string html = Url.GetHtml(url, r.Encoding);
            Match match = html.GetMatchGroup(r.KuaibDramaRule);
            if (match.Success && !r.KuaibDramaRule.IsNullOrEmpty())
            {
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
            string html = Url.GetHtml(url, r.Encoding);
            Match match = html.GetMatchGroup(r.BaiduDramaRule);
            if (match.Success && !r.BaiduDramaRule.IsNullOrEmpty() && !match.Groups["url"].Value.IsNullOrEmpty())
            {
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
            string html = Url.GetHtml(url, r.Encoding);

            var dramas = CollectDramas(html, m.id);


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
                }
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
                string title = str.Split('|')[2].Replace(mv.Title, "");
                title=title.ToLower();
                title = Regex.Replace(title, "[a-zA-Z0-9\\.]+\\.(com|net|org|co|cn|us|hk|info|gov)","");
                title = title.ToLower().Replace(".rmvb", "").Replace(".rm", "").Replace(".avi", "").Replace(".mp4", "").Replace(".asf", "").Replace(".wmv", "").Replace(" ", "").Replace(".", "").Replace("_","");
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

            return result;
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
                    return cls;
                }
                else
                {
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

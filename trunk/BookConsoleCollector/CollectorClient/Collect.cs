using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Voodoo;
using Voodoo.Net;
using System.Text.RegularExpressions;
using System.Reflection;
using CollectorClient.Model;
using CollectorClient.Common;
using CollectorClient.Data;
using System.Collections.Specialized;

namespace CollectorClient
{


    /// <summary>
    /// 采集功能
    /// </summary>
    public class Collect
    {
        #region 属性
        public string RootUrl { get; set; }

        public string TargetUrl { get; set; }

        public string ApiUrl { get; set; }

        public string Connstr { get; set; }

        public Book CurBook { get; set; }

        public BookChapter CurChapter { get; set; }


        #endregion

        #region 实用方法
        private void w(string str)
        {
            Console.WriteLine(str);
        }

        private void red()
        {
            Console.ForegroundColor = ConsoleColor.Red;
        }

        private void white()
        {
            Console.ForegroundColor = ConsoleColor.White;
        }
        #endregion



        #region 遍历规则
        /// <summary>
        /// 遍历规则
        /// </summary>
        public void FechRules()
        {
            var rules = BookRule.GetAll();
            foreach (var rule in rules)
            {
                Connstr = rule.ConnStr;
                RootUrl = string.Format("http://{0}/", rule.SiteDomain);
                ApiUrl = string.Format("{0}e/api/xmlrpc.aspx",rule.TargetSiteUrl);
                OpenListPage(rule);
            }

        } 
        #endregion

        #region 打开书籍列表页面
        /// <summary>
        /// 打开书籍列表页面
        /// </summary>
        /// <param name="r"></param>
        /// <param name="url"></param>
        public void OpenListPage(BookRule r, string url = "")
        {
            if (url.IsNullOrEmpty())
            {
                url = r.ListUrl;
            }

            w("打开列表页面：" + url);

            try
            {
                string listHtml = Url.GetHtml(url, r.CharSet);


                var books = Convert<TitleAndUrl>(SetMatchResult(typeof(TitleAndUrl), listHtml, r.ListRule));
                while (books.Count > 0)
                {
                    var book = books.First();
                    
                    try
                    {
                        if (Common.ContentFilter.GetBlackList().Contains(book.title))
                        {
                            red();
                            w(string.Format("黑名单：{0}", book.title));
                            white();
                            books.Remove(book);
                            continue;
                        }

                        book.url = book.url.AppendToDomain(RootUrl);
                        OpenInfoPage(r, book.url);
                        books.Remove(book);
                    }
                    catch (Exception ex)
                    {
                        red();
                        w(string.Format("打开书籍页面失败：{0}", ex.Message));
                        white();
                        books.Remove(book);
                    }
                }

                //列表翻页
                if (!r.ListUrlNextRule.IsNullOrEmpty() &&
                    listHtml.GetMatchGroup(r.ListUrlNextRule).Groups.Count > 0
                    )
                {
                    OpenListPage(r, listHtml.GetMatch(r.ListUrlNextRule).First().AppendToDomain(RootUrl));
                }
            }
            catch (Exception ex)
            {
                red();
                w(string.Format("打开列表页面失败：{0}", ex.Message));
                white();
            }


        } 
        #endregion

        #region 打开书籍信息页面
        /// <summary>
        /// 打开书籍信息页面
        /// </summary>
        /// <param name="r"></param>
        /// <param name="url"></param>
        public void OpenInfoPage(BookRule r, string url)
        {
            w("打开书籍页面："+url);
            string html = Url.GetHtml(url, r.CharSet);
            var info = (BookInfo)SetMatchResult(typeof(BookInfo), html, r.InfoRule).FirstOrDefault();

            ContentFilter f = new ContentFilter();
            info.intro = f.Filter(info.intro);

            CurBook = GetCurrentBook(info);

            //下载设置图片
            if (info.image.IsNullOrEmpty() == false)
            {
                GetImage(r,info.image.AppendToDomain(RootUrl));
                string nPath = string.Format("{0}{1}.jpg", System.AppDomain.CurrentDomain.BaseDirectory, "xxx");
                string upUrl=string.Format("{0}?a=savebookface&id={1}",ApiUrl,CurBook.ID);
                Url.UpLoadFile(nPath, upUrl, false);
            }

            //判断是够需要打开章节列表页面
            if (r.ChapterListUrlRule.IsNullOrEmpty() == false
                && html.GetMatchGroup(r.ChapterListUrlRule).Groups.Count > 0
                )
            {
                string url_ChapterList = html.GetMatch(r.ChapterListUrlRule).FirstOrDefault().AppendToDomain(RootUrl);
                string html_ChapterList = Url.GetHtml(url_ChapterList, r.CharSet);
                OpenChapterList(r, html_ChapterList);
            }
            else
            {
                OpenChapterList(r, html);
            }
        } 
        #endregion

        #region 下载封面
        /// <summary>
        /// 下载封面
        /// </summary>
        /// <param name="r"></param>
        /// <param name="url"></param>
        public void GetImage(BookRule r, string url)
        {
            string path = string.Format("{0}{1}_old.jpg", System.AppDomain.CurrentDomain.BaseDirectory, "xxx");
            string nPath = string.Format("{0}{1}.jpg", System.AppDomain.CurrentDomain.BaseDirectory, "xxx");
            Url.DownFile(url, path);
            Voodoo.IO.ImageHelper.MakeThumbnail(path, nPath, r.FaceWidth, r.FaceHeight);

        } 
        #endregion

        #region 打开章节列表页面
        /// <summary>
        /// 打开章节列表页面
        /// </summary>
        /// <param name="r"></param>
        /// <param name="html"></param>
        public void OpenChapterList(BookRule r, string html)
        {
            var chapters = Convert<TitleAndUrl>(SetMatchResult(typeof(TitleAndUrl), html, r.ChapterListRule));
            while (chapters.Count > 0)
            {
                var c = chapters.First();
                if (c.title.IsNullOrEmpty())
                {
                    break;
                }
                if (CurBook.LastChapterID == 0)
                {
                    //书籍没有章节
                    break;
                }
                if (c.title != CurBook.LastChapterTitle)
                {
                    chapters.Remove(c);
                }
                else
                {
                    chapters.Remove(c);
                    break;
                }
            }

            while (chapters.Count > 0)
            {
                var chapter = chapters.First();

                using (DataEntities ent = new DataEntities())
                {
                    if ((from l in ent.BookChapter where l.ID == CurBook.ID && l.Title == chapter.title select l).Count() > 0)
                    {
                        return;//如果这个章节已经存在，则不采集整个书籍
                    }
                }

                if (chapter.title.IsNullOrEmpty())
                {
                    break;
                }
                try
                {
                    OpenChapterPage(r, chapter.url.AppendToDomain(RootUrl));
                    chapters.Remove(chapter);
                }
                catch (Exception ex)
                {
                    //如果某一章节打开失败，则需要跳过章节的采集
                    red();
                    w(ex.Message);
                    white();
                    break;
                }
            }
            
        } 
        #endregion

        #region 打开章节内容页面
        /// <summary>
        /// 打开章节内容页面
        /// </summary>
        /// <param name="r"></param>
        /// <param name="url"></param>
        public void OpenChapterPage(BookRule r, string url)
        {
            int errorCount = 0;

            begin:
            try
            {
                Console.WriteLine(string.Format("打开章节:{0}", url));
                string html = Url.GetHtml(url, r.CharSet);
                var result = (ChapterContent)SetMatchResult(typeof(ChapterContent), html, r.ContentRule).FirstOrDefault();

                string chapterContent = GetChapterContent(r, html);
                ContentFilter f = new ContentFilter();
                chapterContent = f.Filter(chapterContent);

                chapterContent = chapterContent.HtmlDeCode();

                SaveChapter(result,chapterContent);

                //判断是否翻页
                if (r.NextChapterUrlRule.IsNullOrEmpty() == false &&
                    html.GetMatchGroup(r.NextChapterUrlRule).Groups.Count > 0
                    )
                {
                    //处理下一页
                    OpenChapterPage(r, html.GetMatch(r.NextChapterUrlRule).FirstOrDefault().AppendToDomain(RootUrl));
                }
            }
            catch
            {
                errorCount++;
                if (errorCount < 3)
                {
                    goto begin;
                }
                else
                {
                    throw new Exception("章节打开分析失败。");
                }
            }

        } 
        #endregion

        #region  获取章节正文
        /// <summary>
        /// 获取章节正文
        /// </summary>
        /// <param name="r"></param>
        /// <param name="html"></param>
        /// <returns></returns>
        public string GetChapterContent(BookRule r, string html)
        {
            StringBuilder sb = new StringBuilder();
            var regexResult = (ChapterContent)SetMatchResult(typeof(ChapterContent), html, r.ContentRule).FirstOrDefault();

            sb.Append(regexResult.content);

            if (r.NextContentRule.IsNullOrEmpty() == false
                && html.GetMatchGroup(r.NextContentRule).Groups.Count > 0)
            {
                string nextHtml = Url.GetHtml(html.GetMatch(r.NextContentRule).FirstOrDefault().AppendToDomain(RootUrl));
                sb.Append(GetChapterContent(r,nextHtml));
            }

            return sb.ToS();
        }
        #endregion



        #region 扩展方法
        public static List<TResult> Convert<TResult>(List<object> lo) where TResult : class, new()
        {
            List<TResult> result = new List<TResult>();

            foreach (var o in lo)
            {
                result.Add((TResult)o);
            }

            return result;
        }

        public static List<object> SetMatchResult(Type type, string Content, string Partten)
        {
            List<object> result = new List<object>();
            
            var match = Content.GetMatchGroup(Partten);
            while (match.Success)
            {
                var r = Assembly.Load("CollectorClient").CreateInstance(type.ToS());
                PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                object[] objArr = new object[props.Length];
                for (int i = 0; i < props.Length; i++)
                {
                    props[i].SetValue(r, match.Groups[props[i].Name].Value, null);
                }

                result.Add(r);
                match = match.NextMatch();
            }

            return result;
        } 
        #endregion



        #region 获取当前书籍 如果不存在就添加
        /// <summary>
        /// 获取当前书籍 如果不存在就添加
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        private Book GetCurrentBook(BookInfo b)
        {
            DataEntities ent = new DataEntities();

            Book cb = new Book();
            try
            {
                cb = (from l in ent.Book where l.Title == b.title && l.Author == b.author select l).First();
            }
            catch
            {
                Class cls = GetClassByName(b.cls);

                cb.Addtime = DateTime.UtcNow.AddHours(8);
                cb.Author = b.author;
                cb.ClassID = cls.ID;
                cb.ClassName = cls.ClassName;

                cb.ClickCount = 0;
                cb.CorpusID = 0;
                cb.CorpusTitle = "";
                cb.Enable = true;
                //cb.FaceImage=
                cb.Intro = b.intro;
                cb.IsFirstPost = false;
                cb.IsVip = false;
                cb.LastChapterID = 0;
                cb.LastChapterTitle = "";
                cb.LastVipChapterID = 0;
                cb.LastVipChapterTitle = "";
                cb.Length = b.length.ToInt32();
                cb.ReplyCount = 0;
                cb.SaveCount = 0;
                cb.Status = b.status == "连载中" ? "0".ToByte() : "1".ToByte();
                cb.Title = b.title;
                cb.TjCount = 0;
                cb.UpdateTime = DateTime.UtcNow.AddHours(8);
                cb.VipUpdateTime = cb.UpdateTime;
                cb.ZtID = 0;
                cb.ZtName = "";

                w(string.Format("保存书籍：{0} {1}",cb.Title,cb.Author));
                ent.AddToBook(cb);
                ent.SaveChanges();
            }
            ent.Dispose();
            return cb;
        }
        #endregion

        #region  根据名称获取类别 不存在则添加
        /// <summary>
        /// 根据名称获取类别 不存在则添加
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private Class GetClassByName(string name)
        {
            DataEntities ent = new DataEntities();
            Class cls = new Class();
            try
            {
                cls = (from l in ent.Class where l.ClassName == name select l).First();
            }
            catch
            {
                cls.Alter = name;
                cls.ClassForder = name;
                cls.ClassName = name;
                cls.ClassPageMode = 4;
                cls.ContentModel = 4;
                cls.ContentPageExtName = "htm";
                cls.IsLeafClass = true;
                cls.ModelID = 4;
                cls.ParentID = GetClassByName("其他").ID;
                cls.ShowInNav = true;

                w(string.Format("保存类别：{0} ", cls.ClassName));

                ent.AddToClass(cls);
                ent.SaveChanges();
            }
            ent.Dispose();
            return cls;

        }
        #endregion

        #region 保存章节
        /// <summary>
        /// 保存章节
        /// </summary>
        /// <param name="c"></param>
        private void SaveChapter(ChapterContent c,string Content)
        {
            DataEntities ent = new DataEntities();

            Book b = (from l in ent.Book where l.ID == CurBook.ID select l).First();
            

            BookChapter bc = new BookChapter();
            bc.BookID = CurBook.ID;
            bc.BookTitle = CurBook.Title;
            bc.ChapterIndex = 0;
            bc.ClassID = CurBook.ClassID;
            bc.ClassName = CurBook.ClassName;
            bc.ClickCount = 0;
            bc.Enable = true;
            bc.IsFree = true;
            bc.IsImageChapter = false;
            bc.IsTemp = false;
            bc.IsVipChapter = false;
            bc.TextLength = c.content.Length;
            bc.Title = c.title;
            bc.UpdateTime = DateTime.UtcNow.AddHours(8);
            bc.ValumeID = 0;
            bc.ValumeName = "";

            w(string.Format("保存章节：{0}  {1} ", bc.Title,bc.BookTitle));

            ent.AddToBookChapter(bc);
            ent.SaveChanges();

            b.LastChapterID = bc.ID;
            b.LastChapterTitle = bc.Title;
            b.UpdateTime = DateTime.UtcNow.AddHours(8);
            b.VipUpdateTime = DateTime.UtcNow.AddHours(8);
            ent.SaveChanges();

            w("Saving content..");
            
            NameValueCollection nv=new NameValueCollection();
            nv.Add("chapterid", bc.ID.ToS());
            nv.Add("chaptertitle", bc.Title);
            nv.Add("content", Content);
            nv.Add("isimagechapter", "False");
            nv.Add("istemp", "False");

            Url.Post(nv, ApiUrl + "?a=chapteredit");
        }
        #endregion

    }
}

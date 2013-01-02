using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Voodoo;
namespace CollectorClient
{
    public class TitleHelper
    {
        public string Title { get; set; }

        public bool Success { get; set; }

        /// <summary>
        /// 标准化标题
        /// </summary>
        /// <returns></returns>
        public string ToStandard()
        {
            GetTitle();
            TrimTitle();
            return Title;
        }

        private void GetTitle()
        {
            foreach (string par in RegGroups)
            {
                var g = this.Title.GetMatchGroup(par);
                if (g.Success)
                {
                    Success = true;
                    this.Title = g.Groups[0].Value;
                    break;
                }
            }
            
        }

        private void TrimTitle()
        {
            foreach (string par in TrimGroup)
            {
                this.Title=Regex.Replace(this.Title, par, "");
            }
        }

        private List<string> TrimGroup = new List<string> { 
            "【[紧急\\!！]+】",
            "【[呼救人啊\\!！]+】",
            "【[\\!！]+】",
            "【[\\.]+】",
            "【.*[求粉丝月票订阅]+.*】",
            "\\([^上中下]*?\\)"
        };

        #region 标题识别表达式
        /// <summary>
        /// 标题识别表达式
        /// </summary>
        private List<string> RegGroups = new List<string> { 
        "第[0123456789一二三四五六七八九〇壹贰叁肆伍陆柒捌玖零十百千万亿]+章-第[0123456789一二三四五六七八九〇壹贰叁肆伍陆柒捌玖零十百千万亿]+章",//第1234章-第4567章
        "【第[0123456789一二三四五六七八九〇壹贰叁肆伍陆柒捌玖零十百千万亿]+章】\\s+.*",//【第1234章】 文字文字
        "第[0123456789一二三四五六七八九〇壹贰叁肆伍陆柒捌玖零十百千万亿]+章\\s+.*",//第1234章 文字文字
        "第[0123456789一二三四五六七八九〇壹贰叁肆伍陆柒捌玖零十百千万亿]+章",//第1234章
        "第?[0123456789]+[-]{1}[0123456789]+章?",//1234-4567章 第1234-4567章
        "第?[0123456789]+章?",//1234   第1234 1234章 第1234章
        "[0123456789]+[\\.\\s]+.*",//1234. 文字
        ".*?后记.*",//文字 后记 文字
        ".*?大结局.*",//文字 大结局 文字
        }; 
        #endregion
    }
}
